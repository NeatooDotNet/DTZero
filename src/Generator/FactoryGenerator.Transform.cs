// FactoryGenerator.Transform.cs
// Contains methods that transform Roslyn syntax trees and semantic models into
// the data model types (TypeInfo, TypeFactoryMethodInfo, etc.).
// These methods are called during the IncrementalGenerator's transform phase.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using DTZero.FactoryGenerator;

namespace DTZero.Generator;

public partial class Factory
{
	/// <summary>
	/// Checks if a class or any of its base classes has the specified attribute.
	/// </summary>
	private static AttributeData? ClassOrBaseClassHasAttribute(INamedTypeSymbol namedTypeSymbol, string attributeName)
	{
		var attribute = namedTypeSymbol.GetAttributes().FirstOrDefault(a => (a.AttributeClass?.Name ?? "") == attributeName);

		if (attribute != null)
		{
			return attribute;
		}
		if (namedTypeSymbol.BaseType != null)
		{
			return ClassOrBaseClassHasAttribute(namedTypeSymbol.BaseType, attributeName);
		}
		return null;
	}

	/// <summary>
	/// Transforms a class or record declaration with [Factory] attribute into a TypeInfo model.
	/// Handles both ClassDeclarationSyntax and RecordDeclarationSyntax.
	/// </summary>
	private static TypeInfo TransformTypeFactory(TypeDeclarationSyntax syntax, SemanticModel semanticModel)
	{
		var symbol = semanticModel.GetDeclaredSymbol(syntax) ?? throw new Exception($"Cannot get named symbol for {syntax}");

		return new TypeInfo(syntax, symbol, semanticModel);
	}

	/// <summary>
	/// Transforms an interface declaration with [Factory] attribute into a TypeInfo model.
	/// </summary>
	private static TypeInfo TransformInterfaceFactory(InterfaceDeclarationSyntax interfaceSyntax, SemanticModel semanticModel)
	{
		return new TypeInfo(interfaceSyntax, semanticModel.GetDeclaredSymbol(interfaceSyntax) ?? throw new Exception($"Cannot get named symbol for {interfaceSyntax}"), semanticModel);
	}

	/// <summary>
	/// Gets all methods from a type symbol and its base types recursively.
	/// </summary>
	/// <param name="symbol">The type symbol to get methods from.</param>
	/// <param name="includeConst">Whether to include constructors (only included for the top-level type).</param>
	private static List<IMethodSymbol> GetMethodsRecursive(INamedTypeSymbol? symbol, bool includeConst = true)
	{
		var methods = symbol?.GetMembers().OfType<IMethodSymbol>()
						.Where(m => includeConst || m.MethodKind != MethodKind.Constructor) // Only include top-level constructors
						.ToList() ?? [];
		if (symbol?.BaseType != null)
		{
			methods.AddRange(GetMethodsRecursive(symbol.BaseType, false));
		}
		return methods;
	}

	/// <summary>
	/// Gets the methods of an authorization type named by <c>[AuthorizeFactory&lt;T&gt;]</c>,
	/// including every method it inherits.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A class contributes its own methods and those of each base class (<see cref="GetMethodsRecursive"/>).
	/// An interface has no base type, so it contributes its own methods and those of every interface
	/// it inherits, through <see cref="ITypeSymbol.AllInterfaces"/>. That list holds each interface once,
	/// so a check reachable through two inheritance paths (a diamond) is read once.
	/// </para>
	/// <para>
	/// Both walks list the most-derived declarations first: the class walk by construction, the
	/// interface walk because <c>AllInterfaces</c> lists an interface before the interfaces it inherits.
	/// </para>
	/// <para>
	/// A class also contributes the checks declared on every interface it implements, directly or
	/// through a base class (<c>AllInterfaces</c> again), after the class hierarchy.
	/// </para>
	/// <para>
	/// A check declared with <c>[AuthorizeFactory]</c> at more than one place — an override or
	/// <c>new</c> member restating it, a derived interface restating it, a class restating an
	/// interface's check, or two unrelated interfaces declaring it — is one check (same name and
	/// parameter types). It is returned once, as its most-derived declaration in source, and the
	/// other declarations are returned in <paramref name="restatements"/>: the caller generates one
	/// call, through the auth type, for the union of every declaration's operations. A restatement
	/// adds operations and never removes one (user ruling, PITCH-009). The call binds to the
	/// auth type's member, so it runs the most-derived implementation once.
	/// </para>
	/// <para>
	/// A check the generated factory cannot call through the auth type is not returned but added to
	/// <paramref name="notCallable"/> (NF0108): a check whose attribute sits on an explicit interface
	/// implementation, and an interface check the class implements explicitly or not at all (a
	/// default interface implementation). A call to either through the auth type does not compile.
	/// </para>
	/// </remarks>
	private static List<IMethodSymbol> GetAuthMethodsRecursive(
		INamedTypeSymbol? authSymbol,
		List<string> messages,
		List<IMethodSymbol>? notCallable = null,
		Dictionary<IMethodSymbol, List<IMethodSymbol>>? restatements = null)
	{
		if (authSymbol == null)
		{
			return [];
		}

		var isInterface = authSymbol.TypeKind == TypeKind.Interface;

		var declared = isInterface
			? authSymbol.GetMembers().OfType<IMethodSymbol>().ToList()
			: GetMethodsRecursive(authSymbol);

		var inherited = new List<IMethodSymbol>();
		foreach (var inheritedInterface in authSymbol.AllInterfaces)
		{
			// A class reads only the checks on its interfaces; an interface keeps every member, as before.
			inherited.AddRange(inheritedInterface.GetMembers().OfType<IMethodSymbol>()
				.Where(m => isInterface || IsAuthorizeFactoryMethod(m)));
		}

		// Each entry is either a method without [AuthorizeFactory] (kept as before, for the debug
		// messages the caller writes) or the group of [AuthorizeFactory] declarations of one check,
		// placed where that check is first declared, most-derived declaration first.
		var entries = new List<List<IMethodSymbol>>();
		var groups = new Dictionary<string, List<IMethodSymbol>>();

		void AddCheck(IMethodSymbol method)
		{
			var signature = AuthMethodSignature(method);
			if (groups.TryGetValue(signature, out var group))
			{
				messages.Add($"Combining {method.ContainingType.Name}.{method.Name} with a more-derived declaration of the same check on {authSymbol.Name}");
				group.Add(method);
				return;
			}

			group = [method];
			groups.Add(signature, group);
			entries.Add(group);
		}

		// Interface members whose explicit implementation already carries the attribute and has been
		// reported, so the interface's own declaration of the same check is not reported again.
		var reportedInterfaceMembers = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

		foreach (var method in declared)
		{
			if (!IsAuthorizeFactoryMethod(method))
			{
				entries.Add([method]);
				continue;
			}

			if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				notCallable?.Add(method);
				foreach (var interfaceMember in method.ExplicitInterfaceImplementations)
				{
					reportedInterfaceMembers.Add(interfaceMember);
				}
				continue;
			}

			AddCheck(method);
		}

		foreach (var method in inherited)
		{
			if (!IsAuthorizeFactoryMethod(method))
			{
				entries.Add([method]);
				continue;
			}

			if (reportedInterfaceMembers.Contains(method))
			{
				continue;
			}

			// Callability first: an interface check the class does not implement implicitly is
			// NF0108 even when a class method of the same signature is a check, because the call
			// through the auth type would run that method, not this interface's implementation.
			if (!isInterface)
			{
				var implementation = authSymbol.FindImplementationForInterfaceMember(method) as IMethodSymbol;
				if (implementation == null
					|| implementation.MethodKind == MethodKind.ExplicitInterfaceImplementation
					|| implementation.ContainingType.TypeKind == TypeKind.Interface)
				{
					notCallable?.Add(method);
					continue;
				}
			}

			AddCheck(method);
		}

		// One method per entry. For a check, the representative is the most-derived declaration in
		// source (its syntax is what the check is generated from); the other declarations of the
		// same check are handed back in restatements so their operations can be combined with it.
		var methods = new List<IMethodSymbol>();
		foreach (var entry in entries)
		{
			if (entry.Count == 1)
			{
				methods.Add(entry[0]);
				continue;
			}

			var representative = entry.FirstOrDefault(m => m.DeclaringSyntaxReferences.Length > 0) ?? entry[0];
			methods.Add(representative);
			restatements?.Add(representative, entry.Where(m => !SymbolEqualityComparer.Default.Equals(m, representative)).ToList());
		}

		return methods;
	}

	/// <summary>
	/// Name and parameter types: two declarations with the same signature are one check. Nullable
	/// reference annotations are left out (<c>string?</c> and <c>string</c> are one check);
	/// nullable value types stay distinct, since <c>int?</c> is a different type.
	/// </summary>
	private static string AuthMethodSignature(IMethodSymbol method) =>
		$"{method.Name}({string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))})";

	/// <summary>
	/// The operations an <c>[AuthorizeFactory]</c> declaration names, read from its bound attribute
	/// constant: the source of truth for every declaration, in source or referenced, whatever form
	/// the argument is written in (<c>AuthorizeFactoryOperation.X</c>, a <c>using</c> alias, a
	/// <c>const</c>, a cast). Null when it cannot be read, which the caller reports (NF0109, or NF0107
	/// for a declaration not in source).
	/// </summary>
	private static AuthorizeFactoryOperation? ReadAuthorizeOperations(IMethodSymbol method) =>
		ReadAuthorizeOperations(method.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "AuthorizeFactoryAttribute"));

	private static AuthorizeFactoryOperation? ReadAuthorizeOperations(AttributeData? attribute)
	{
		if (attribute == null || attribute.ConstructorArguments.Length != 1)
		{
			return null;
		}

		var argument = attribute.ConstructorArguments[0];
		return argument.Kind != TypedConstantKind.Error && argument.Value is int value
			? (AuthorizeFactoryOperation)value
			: null;
	}

	/// <summary>
	/// Whether every factory operation <paramref name="named"/> applies to is also checked by
	/// <paramref name="covering"/>, by the same matching the factory methods use.
	/// </summary>
	private static bool CoversEveryOperation(AuthorizeFactoryOperation covering, AuthorizeFactoryOperation named)
	{
		FactoryOperation[] operations =
		[
			FactoryOperation.Create, FactoryOperation.Fetch, FactoryOperation.Insert,
			FactoryOperation.Update, FactoryOperation.Delete, FactoryOperation.Execute
		];

		return operations.All(o => ((int)named & (int)o) == 0 || ((int)covering & (int)o) != 0);
	}

	private static bool HasRemoteAttribute(IMethodSymbol method) =>
		method.GetAttributes().Any(a => a.AttributeClass?.Name == "RemoteAttribute");

	/// <summary>
	/// The interface member a not-callable check stands for, as <c>IChecks.CanRead</c>.
	/// </summary>
	private static string InterfaceCheckDisplay(IMethodSymbol method)
	{
		var member = method.MethodKind == MethodKind.ExplicitInterfaceImplementation
			? method.ExplicitInterfaceImplementations.FirstOrDefault() ?? method
			: method;
		return $"{member.ContainingType.Name}.{member.Name}";
	}

	/// <summary>
	/// Whether the method carries <c>[AuthorizeFactory(...)]</c>, i.e. is an authorization check.
	/// </summary>
	private static bool IsAuthorizeFactoryMethod(IMethodSymbol method) =>
		method.GetAttributes().Any(a => a.AttributeClass?.Name == "AuthorizeFactoryAttribute");

	/// <summary>
	/// Extracts factory method information from a type's methods.
	/// Identifies methods with factory operation attributes (Create, Fetch, Insert, Update, Delete, Execute)
	/// and validates their signatures.
	/// </summary>
	private static List<TypeFactoryMethodInfo> TypeFactoryMethods(INamedTypeSymbol serviceSymbol, List<IMethodSymbol> methods, List<FactoryOperation> defaultFactoryOperations, List<TypeAuthMethodInfo> authMethods, List<string> messages, List<DiagnosticInfo> diagnostics)
	{
		var callFactoryMethods = new List<TypeFactoryMethodInfo>();

		foreach (var methodSymbol in methods)
		{
			var methodType = methodSymbol.ReturnType.ToDisplayString();

			if (methodType.Contains(@"Task<"))
			{
				methodType = Regex.Match(methodType, @"Task<(.*?)>").Groups[1].Value;
			}

			if (methodType.EndsWith("?"))
			{
				methodType = methodType.Substring(0, methodType.Length - 1);
			}

			if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not BaseMethodDeclarationSyntax methodSyntax)
			{
				messages.Add($"No BaseMethodDeclarationSyntax for {methodSymbol.Name}");
				continue;
			}

			TypeFactoryMethodInfo factoryMethod;

			var attributes = methodSymbol.GetAttributes().ToList();
			var attributeNames = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).ToList();

			// Snapshot real source-placed attribute names (before adding synthetic defaults)
			// so NF0106 only fires for attributes the user actually wrote.
			var realAttributeNames = new HashSet<string>(attributeNames.Where(a => a != null)!);

			attributeNames.AddRange(defaultFactoryOperations.Select(o => o.ToString()));

			var aspAuthorizeAttributes = attributes.Where(a => a.AttributeClass?.Name == "AspAuthorizeAttribute").ToList();
			List<AspAuthorizeInfo> aspAuthorizeCalls = [];

			foreach (var aspAuthorizeAttribute in aspAuthorizeAttributes)
			{
				aspAuthorizeCalls.Add(new AspAuthorizeInfo(aspAuthorizeAttribute));
				attributes.Remove(aspAuthorizeAttribute);
			}

			// Track if this method has any factory operation attribute for NF0301
			bool hasFactoryOperationAttribute = false;

			foreach (var attributeName in attributeNames.Where(a => a != null))
			{
				if (Enum.TryParse<FactoryOperation>(attributeName, out var factoryOperation))
				{
					hasFactoryOperationAttribute = true;

					// NF0106: Factory-operation attribute on an interface factory method.
					// Interface factories have no CRUD/Execute operation semantics — every method is
					// implicitly remote. Operation attributes on interface methods cause duplicate
					// codegen (class-factory path alongside interface-factory path). Emit diagnostic
					// and skip this attribute so the interface-factory path owns the method cleanly.
					//
					// Use methodSymbol.ContainingType to detect whether the method is DECLARED on an
					// interface — serviceSymbol can be reassigned to the paired interface for class
					// factories (class + matching I-prefixed interface), which would misfire NF0106.
					//
					// Only fire for REAL user-source attributes, not the synthetic Execute default
					// that the generator adds to every interface method.
					if (methodSymbol.ContainingType?.TypeKind == TypeKind.Interface
						&& realAttributeNames.Contains(attributeName!))
					{
						var methodLocation = methodSyntax switch
						{
							MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
							ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
							_ => methodSyntax.GetLocation()
						};
						var lineSpan = methodLocation.GetLineSpan();
						diagnostics.Add(new DiagnosticInfo(
							"NF0106",
							lineSpan.Path ?? "",
							lineSpan.StartLinePosition.Line,
							lineSpan.StartLinePosition.Character,
							lineSpan.EndLinePosition.Line,
							lineSpan.EndLinePosition.Character,
							methodLocation.SourceSpan.Start,
							methodLocation.SourceSpan.Length,
							methodSymbol.Name,
							attributeName!));
						messages.Add($"NF0106: factory-operation attribute '[{attributeName}]' on interface method '{methodSymbol.Name}'; attribute ignored.");
						continue;
					}

					// Execute check runs BEFORE the return-type check.
					// This prevents Execute methods that return the target type from being
					// misrouted into the NF0204/NF0201 logic for Create/Fetch.
					if (factoryOperation == FactoryOperation.Execute
								&& serviceSymbol.TypeKind != TypeKind.Interface)
					{
						if (!methodSymbol.IsStatic)
						{
							// NF0103: Execute method must be a static method
							var methodLocation = methodSyntax switch
							{
								MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
								ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
								_ => methodSyntax.GetLocation()
							};
							var lineSpan = methodLocation.GetLineSpan();
							diagnostics.Add(new DiagnosticInfo(
								"NF0103",
								lineSpan.Path ?? "",
								lineSpan.StartLinePosition.Line,
								lineSpan.StartLinePosition.Character,
								lineSpan.EndLinePosition.Line,
								lineSpan.EndLinePosition.Character,
								methodLocation.SourceSpan.Start,
								methodLocation.SourceSpan.Length,
								methodSymbol.Name));
							messages.Add($"Ignoring {methodSymbol.Name}. Execute Operations must be a static method");
							continue;
						}
					}
					else if (methodSymbol.ReturnType.ToDisplayString().Contains(serviceSymbol.Name))
					{
						if (methodType == serviceSymbol.ToDisplayString())
						{
							if (((int?)factoryOperation & (int)AuthorizeFactoryOperation.Read) == 0)
							{
								// NF0204: Write operation should not return target type
								var methodLocation = methodSyntax switch
								{
									MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
									ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
									_ => methodSyntax.GetLocation()
								};
								var lineSpan = methodLocation.GetLineSpan();
								diagnostics.Add(new DiagnosticInfo(
									"NF0204",
									lineSpan.Path ?? "",
									lineSpan.StartLinePosition.Line,
									lineSpan.StartLinePosition.Character,
									lineSpan.EndLinePosition.Line,
									lineSpan.EndLinePosition.Character,
									methodLocation.SourceSpan.Start,
									methodLocation.SourceSpan.Length,
									methodSymbol.Name,
									factoryOperation.ToString(),
									serviceSymbol.Name));
								messages.Add($"Ignoring {methodSymbol.Name}, Only Fetch and Create methods can return the target type");
								continue;
							}

							if (!methodSymbol.IsStatic)
							{
								// NF0201: Factory method returning target type must be static
								var methodLocation = methodSyntax switch
								{
									MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
									ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
									_ => methodSyntax.GetLocation()
								};
								var lineSpan = methodLocation.GetLineSpan();
								diagnostics.Add(new DiagnosticInfo(
									"NF0201",
									lineSpan.Path ?? "",
									lineSpan.StartLinePosition.Line,
									lineSpan.StartLinePosition.Character,
									lineSpan.EndLinePosition.Line,
									lineSpan.EndLinePosition.Character,
									methodLocation.SourceSpan.Start,
									methodLocation.SourceSpan.Length,
									methodSymbol.Name,
									serviceSymbol.Name));
								messages.Add($"Ignoring {methodSymbol.Name}; it must be static. Only static factories are allowed.");
								continue;
							}
						}
					}

					factoryMethod = new TypeFactoryMethodInfo(factoryOperation, methodSymbol, methodSyntax, authMethods);
				}
				else
				{
					messages.Add($"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a FactoryOperation attribute.");
					continue;
				}

				foreach (var targetParam in methodSymbol.Parameters.Where(p => p.Type == serviceSymbol))
				{
					factoryMethod.Parameters.Where(p => p.Name == targetParam.Name).ToList().ForEach(p => p.IsTarget = true);
				}

				factoryMethod.AspAuthorizeCalls = new EquatableArray<AspAuthorizeInfo>([.. aspAuthorizeCalls]);

				callFactoryMethods.Add(factoryMethod);
			}

			// NF0301: Report for public methods without factory operation attributes (opt-in)
			// Only report for:
			// - Public methods (skip private/internal helpers)
			// - Instance methods or static methods that look like they could be factory methods
			// - Not constructors (they require explicit [Create] attribute)
			// - Not compiler-generated (like property getters/setters)
			if (!hasFactoryOperationAttribute
				&& methodSymbol.DeclaredAccessibility == Accessibility.Public
				&& methodSymbol.MethodKind == MethodKind.Ordinary
				&& !methodSymbol.IsImplicitlyDeclared)
			{
				var methodLocation = methodSyntax switch
				{
					MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
					ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
					_ => methodSyntax.GetLocation()
				};
				var lineSpan = methodLocation.GetLineSpan();
				diagnostics.Add(new DiagnosticInfo(
					"NF0301",
					lineSpan.Path ?? "",
					lineSpan.StartLinePosition.Line,
					lineSpan.StartLinePosition.Character,
					lineSpan.EndLinePosition.Line,
					lineSpan.EndLinePosition.Character,
					methodLocation.SourceSpan.Start,
					methodLocation.SourceSpan.Length,
					methodSymbol.Name,
					serviceSymbol.Name));
			}
		}
		return callFactoryMethods;
	}

	/// <summary>
	/// Extracts authorization method information from a type that has [AuthorizeFactory] attribute.
	/// </summary>
	private static EquatableArray<TypeAuthMethodInfo> TypeAuthMethods(SemanticModel semanticModel, INamedTypeSymbol typeSymbol, INamedTypeSymbol serviceSymbol, List<string> messages, List<DiagnosticInfo> diagnostics)
	{

		var authorizeAttribute = ClassOrBaseClassHasAttribute(typeSymbol, "AuthorizeFactoryAttribute");
		var callAuthMethods = new List<TypeAuthMethodInfo>();

		if (authorizeAttribute != null)
		{
			var authorizationRuleType = authorizeAttribute.AttributeClass?.TypeArguments[0];

			// If the auth type is an interface, find the concrete implementing class
			string? concreteClassName = null;
			if (authorizationRuleType is INamedTypeSymbol authNamedType && authNamedType.TypeKind == TypeKind.Interface)
			{
				concreteClassName = FindConcreteImplementation(semanticModel.Compilation, authNamedType);
			}

			// NF0107: a check in the auth type's hierarchy that cannot be generated is reported, never
			// dropped with only a debug message. Reported at the [AuthorizeFactory<T>] usage; when
			// that is not in source (inherited from a referenced base), at the factory type.
			var authTypeName = authorizationRuleType?.Name ?? "";
			var reportLocation = authorizeAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
				?? typeSymbol.Locations.FirstOrDefault(l => l.IsInSource);

			void ReportAt(Location? location, string diagnosticId, params string[] args)
			{
				if (location == null)
				{
					return;
				}

				var lineSpan = location.GetLineSpan();
				diagnostics.Add(new DiagnosticInfo(
					diagnosticId,
					lineSpan.Path ?? "",
					lineSpan.StartLinePosition.Line,
					lineSpan.StartLinePosition.Character,
					lineSpan.EndLinePosition.Line,
					lineSpan.EndLinePosition.Character,
					location.SourceSpan.Start,
					location.SourceSpan.Length,
					args));
			}

			void ReportAtAuthorizeFactory(string diagnosticId, params string[] args) =>
				ReportAt(reportLocation, diagnosticId, args);

			// NF0109: the operations of an in-source [AuthorizeFactory] declaration could not be read
			// from its bound constant. Reported at that declaration's attribute; no check is generated
			// from it, and the build fails rather than leaving its operations unchecked.
			void ReportOperationsNotReadable(IMethodSymbol methodSymbol, Location attributeLocation)
			{
				messages.Add($"NF0109: operations of {methodSymbol.ContainingType.Name}.{methodSymbol.Name} could not be read");
				ReportAt(attributeLocation, "NF0109", authTypeName, methodSymbol.ContainingType.ToDisplayString(), methodSymbol.Name);
			}

			void ReportCheckNotInSource(IMethodSymbol methodSymbol)
			{
				messages.Add($"NF0107: authorization check {methodSymbol.ContainingType.Name}.{methodSymbol.Name} has no source declaration; no check generated for {authTypeName}");
				ReportAtAuthorizeFactory("NF0107", authTypeName, methodSymbol.ContainingType.ToDisplayString(), methodSymbol.Name);
			}

			// NF0108: a check the generated factory cannot call through the auth type (an explicit
			// interface implementation, or none on the class). No call is generated for it.
			var notCallable = new List<IMethodSymbol>();

			// Other declarations of the same check, keyed by the declaration it is generated from.
			var restatements = new Dictionary<IMethodSymbol, List<IMethodSymbol>>(SymbolEqualityComparer.Default);

			void ReportNotCallable()
			{
				foreach (var methodSymbol in notCallable)
				{
					var check = InterfaceCheckDisplay(methodSymbol);
					messages.Add($"NF0108: authorization check {check} is not callable on {authTypeName}; no check generated");
					ReportAtAuthorizeFactory("NF0108", authTypeName, check);
				}
			}

			if (authorizationRuleType?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is TypeDeclarationSyntax syntax)
			{
				var authSemanticModel = semanticModel.Compilation.GetSemanticModel(syntax.SyntaxTree);
				var authSymbol = authSemanticModel.GetDeclaredSymbol(syntax);

				// Checks the auth type inherits (from base classes or base interfaces) are read
				// as well as the ones it declares.
				var methodSymbols = GetAuthMethodsRecursive(authSymbol, messages, notCallable, restatements);
				ReportNotCallable();

				foreach (var methodSymbol in methodSymbols)
				{
					var methodType = methodSymbol.ReturnType.ToDisplayString();

					if (methodType.Contains(@"Task<"))
					{
						methodType = Regex.Match(methodType, @"Task<(.*?)>").Groups[1].Value;
					}

					if (methodType.EndsWith("?"))
					{
						methodType = methodType.Substring(0, methodType.Length - 1);
					}

					var attributes = methodSymbol.GetAttributes().ToList();

					if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
					{
						messages.Add($"No MethodDeclarationSyntax for {methodSymbol.Name}");
						if (IsAuthorizeFactoryMethod(methodSymbol))
						{
							// The representative is the most-derived declaration in source when there is
							// one, so here no declaration of this check is in source.
							ReportCheckNotInSource(methodSymbol);
							if (restatements.TryGetValue(methodSymbol, out var otherDeclarations))
							{
								otherDeclarations.ForEach(ReportCheckNotInSource);
							}
						}
						continue;
					}

					foreach (var attribute in attributes)
					{
						var attributeName = attribute.AttributeClass?.Name.Replace("Attribute", "");

						if (attributeName == "AuthorizeFactory")
						{
							if (attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax)
							{
								messages.Add($"No AttributeSyntax for {methodSymbol.Name} {attribute.ToString()}");
								ReportCheckNotInSource(methodSymbol);
								continue;
							}

							// Read from the bound attribute constant, not the argument's text, so an alias, a
							// const or a cast reads as reliably as AuthorizeFactoryOperation.X.
							var representativeOperations = ReadAuthorizeOperations(attribute);
							if (representativeOperations == null)
							{
								ReportOperationsNotReadable(methodSymbol, attributeSyntax.GetLocation());
								continue;
							}

							var authorizeFactoryOperation = representativeOperations.Value;
							var isRemoteByRestatement = false;

							// The same check declared elsewhere in the hierarchy: one call, for the union of
							// every declaration's operations. A restatement adds operations, never removes one.
							if (restatements.TryGetValue(methodSymbol, out var otherDeclarations))
							{
								var notInSource = new List<IMethodSymbol>();

								foreach (var other in otherDeclarations)
								{
									// [Remote] on any declaration makes the combined check [Remote]: it then
									// runs on the server and its Can* asks the server, so combining never
									// moves a check off the server.
									isRemoteByRestatement |= HasRemoteAttribute(other);

									var otherSyntax = other.GetAttributes()
										.FirstOrDefault(a => a.AttributeClass?.Name == "AuthorizeFactoryAttribute")
										?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

									if (otherSyntax == null)
									{
										notInSource.Add(other);
										continue;
									}

									var otherOperations = ReadAuthorizeOperations(other);
									if (otherOperations == null)
									{
										// Never a silent zero: operations nobody can read would go unchecked.
										ReportOperationsNotReadable(other, otherSyntax.GetLocation());
										continue;
									}

									authorizeFactoryOperation |= otherOperations.Value;
								}

								messages.Add($"Combined {otherDeclarations.Count + 1} declarations of {methodSymbol.Name} on {authTypeName}: {authorizeFactoryOperation}");

								// A declaration with no source syntax is NF0107 unless the source
								// declarations already check every operation it names.
								foreach (var other in notInSource)
								{
									var named = ReadAuthorizeOperations(other);
									if (named == null || !CoversEveryOperation(authorizeFactoryOperation, named.Value))
									{
										ReportCheckNotInSource(other);
									}
								}
							}

							if (!(methodType == "bool" || methodType == "string" || methodType == "string?"))
							{
								// NF0202: Authorization method has invalid return type
								var methodLocation = methodSyntax.Identifier.GetLocation();
								var lineSpan = methodLocation.GetLineSpan();
								diagnostics.Add(new DiagnosticInfo(
									"NF0202",
									lineSpan.Path ?? "",
									lineSpan.StartLinePosition.Line,
									lineSpan.StartLinePosition.Character,
									lineSpan.EndLinePosition.Line,
									lineSpan.EndLinePosition.Character,
									methodLocation.SourceSpan.Start,
									methodLocation.SourceSpan.Length,
									methodSymbol.Name,
									methodSymbol.ReturnType.ToDisplayString()));
								messages.Add($"Ignoring {methodSymbol.Name}; wrong return type of {methodType} for an AuthorizeFactory method");
								continue;
							}

							var authMethodInfo = new TypeAuthMethodInfo(authorizeFactoryOperation, methodSymbol, methodSyntax, concreteClassName);

							// The check is resolved and called through the auth type the factory names,
							// wherever the method is declared. MethodInfo records the declaring type, which
							// for an inherited check is a base class or base interface: resolving that
							// would make the base a second service to register (an abstract base cannot be
							// registered at all). For a check T declares itself this is the same name.
							authMethodInfo.ClassName = authSymbol!.Name;

							if (isRemoteByRestatement)
							{
								authMethodInfo.MarkRemote();
							}

							// Mark auth method parameters whose type matches the entity type as IsTarget.
							// This enables: (1) passing the target entity to auth methods on write operations,
							// (2) suppressing CanXxx generation when auth needs the target.
							// Compare by display string since auth type symbols come from a different semantic model.
							var serviceTypeName = serviceSymbol.ToDisplayString();
							var typeSymbolName = typeSymbol.ToDisplayString();
							foreach (var targetParam in methodSymbol.Parameters.Where(p =>
								p.Type.ToDisplayString() == serviceTypeName || p.Type.ToDisplayString() == typeSymbolName))
							{
								authMethodInfo.Parameters.Where(p => p.Name == targetParam.Name).ToList().ForEach(p => p.IsTarget = true);
							}

							callAuthMethods.Add(authMethodInfo);
						}
						else
						{
							messages.Add($"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a AuthorizeFactoryOperation attribute.");
							continue;
						}
					}
				}
			}
			else
			{
				messages.Add($"No TypeDeclarationSyntax for {authorizeAttribute}");

				// The auth type itself is not in source (a referenced assembly): none of its checks
				// can be generated, so each one it carries is reported.
				foreach (var methodSymbol in GetAuthMethodsRecursive(authorizationRuleType as INamedTypeSymbol, messages, notCallable, restatements))
				{
					if (IsAuthorizeFactoryMethod(methodSymbol))
					{
						ReportCheckNotInSource(methodSymbol);
						if (restatements.TryGetValue(methodSymbol, out var otherDeclarations))
						{
							otherDeclarations.ForEach(ReportCheckNotInSource);
						}
					}
				}
				ReportNotCallable();
			}
		}
		else
		{
			messages.Add("No AuthorizeFactoryAttribute");
		}

		return new EquatableArray<TypeAuthMethodInfo>([.. callAuthMethods]);
	}

	/// <summary>
	/// Finds a concrete class implementing the given interface within the compilation.
	/// Returns the simple name of the first implementing class found, or null if none.
	/// </summary>
	private static string? FindConcreteImplementation(Compilation compilation, INamedTypeSymbol interfaceSymbol)
	{
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			var model = compilation.GetSemanticModel(syntaxTree);
			var root = syntaxTree.GetRoot();

			foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
			{
				var classSymbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
				if (classSymbol != null && !classSymbol.IsAbstract && classSymbol.AllInterfaces.Contains(interfaceSymbol))
				{
					return classSymbol.Name;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Collects using statements from a type declaration and its base types.
	/// </summary>
	public static void UsingStatements(List<string> usingDirectives, TypeDeclarationSyntax syntax, SemanticModel semanticModel, string namespaceName, List<string> messages)
	{
		var parentSyntax = syntax.Parent as TypeDeclarationSyntax;
		var parentClassUsingText = "";

		while (parentSyntax != null)
		{
			messages.Add("Parent class: " + parentSyntax.Identifier.Text);
			parentClassUsingText = $"{parentSyntax.Identifier.Text}.{parentClassUsingText}";
			parentSyntax = parentSyntax.Parent as TypeDeclarationSyntax;
		}

		if (!string.IsNullOrEmpty(parentClassUsingText))
		{
			usingDirectives.Add($"using static {namespaceName}.{parentClassUsingText.TrimEnd('.')};");
		}

		var recurseClassDeclaration = syntax;

		while (recurseClassDeclaration != null)
		{
			var compilationUnitSyntax = recurseClassDeclaration.SyntaxTree.GetCompilationUnitRoot();
			foreach (var using_ in compilationUnitSyntax.Usings)
			{
				if (!usingDirectives.Contains(using_.ToString()))
				{
					usingDirectives.Add(using_.ToString());
				}
			}
			recurseClassDeclaration = GetBaseTypeDeclarationSyntax(semanticModel, recurseClassDeclaration, messages);
		}
	}

	/// <summary>
	/// Gets the base type's declaration syntax for a given type declaration.
	/// Used to traverse the inheritance hierarchy when collecting using statements.
	/// </summary>
	private static TypeDeclarationSyntax? GetBaseTypeDeclarationSyntax(SemanticModel semanticModel, TypeDeclarationSyntax syntax, List<string> messages)
	{
		try
		{
			var correctSemanticModel = semanticModel.Compilation.GetSemanticModel(syntax.SyntaxTree);

			var classSymbol = correctSemanticModel.GetDeclaredSymbol(syntax) as INamedTypeSymbol;

			if (classSymbol?.BaseType == null)
			{
				return null;
			}

			var baseTypeSymbol = classSymbol.BaseType;
			var baseTypeSyntaxReference = baseTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault();

			if (baseTypeSyntaxReference == null)
			{
				return null;
			}

			var baseTypeSyntaxNode = baseTypeSyntaxReference.GetSyntax() as TypeDeclarationSyntax;

			return baseTypeSyntaxNode;
		}
		catch (Exception ex)
		{
			messages.Add(ex.Message);
			return null;
		}
	}
}
