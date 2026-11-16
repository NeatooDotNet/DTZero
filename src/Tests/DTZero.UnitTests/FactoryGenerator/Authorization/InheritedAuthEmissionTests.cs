using DTZeroTests.UnitTests.TestContainers;

namespace DTZeroTests.UnitTests.FactoryGenerator.Authorization;

/// <summary>
/// Pins the generator side of PITCH-009: a check an auth type inherits generates exactly what
/// the same check declared on the auth type generates.
/// </summary>
/// <remarks>
/// Each test runs the generator over two fixtures that differ only in where the check is
/// declared, and compares the generated factory text. Equal text means the inherited check is
/// read, is called once, and is resolved and registered through the auth type the factory
/// names, never through the type that declares it. The runtime behaviour is pinned on the
/// client/server containers by <c>AuthInheritanceTests</c> in the integration tests.
///
/// The fixtures declare their own System usings; see NotAuthorizedContextEmissionTests.
/// </remarks>
public class InheritedAuthEmissionTests
{
    private const string Usings = @"
using DTZero;
using System;
using System.Threading;
using System.Threading.Tasks;
";

    private const string ClassTarget = @"
    [Factory]
    [AuthorizeFactory<{0}>]
    public partial class Target
    {{
        public string Key {{ get; set; }} = """";

        [Create]
        public void Create(string key) {{ Key = key; }}

        [Remote, Fetch]
        internal void Fetch(string key) {{ Key = key; }}
    }}
";

    private static string Fixture(string authDeclarations, string authTypeName) =>
        Usings
        + "namespace TestNamespace\n{\n"
        + authDeclarations
        + string.Format(ClassTarget, authTypeName)
        + "}\n";

    private static string GeneratedFactory(string source, string factoryFileFragment)
    {
        // The output compilation is not asserted error-free: this harness does not reference
        // System.Text.Json, which every generated class factory uses. That the generated code
        // compiles and runs is pinned by AuthInheritanceTests, whose targets are built by the
        // integration test project.
        var (diagnostics, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        // Every check in these fixtures is declared in source and callable on the auth type, so
        // none may be reported as impossible to generate (NF0107, NF0108; see their tests).
        Assert.DoesNotContain(diagnostics, d => d.Id == "NF0107");
        Assert.DoesNotContain(diagnostics, d => d.Id == "NF0108");

        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains(factoryFileFragment))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generated);
        return generated;
    }

    [Fact]
    public void InterfaceChain_InheritedCheck_GeneratesAsDeclaredCheck()
    {
        var declared = Fixture(@"
    public interface IAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        var inherited = Fixture(@"
    public interface IAuthRoot
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public interface IAuthMiddle : IAuthRoot { }
    public interface IAuth : IAuthMiddle { }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        var expected = GeneratedFactory(declared, "TargetFactory");
        var actual = GeneratedFactory(inherited, "TargetFactory");

        // Positive anchors first, so equality cannot hold between two trees that lack the check.
        Assert.Contains("IAuth iauth = ServiceProvider.GetRequiredService<IAuth>();", expected);
        Assert.Contains("authorized = iauth.CanRead(key);", expected);
        Assert.Contains("services.TryAddTransient<IAuth, Auth>();", expected);
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("IAuthRoot", actual);
        Assert.DoesNotContain("IAuthMiddle", actual);
    }

    [Fact]
    public void InterfaceDiamond_CheckReachedTwice_GeneratesAsDeclaredOnce()
    {
        var declared = Fixture(@"
    public interface IAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        var diamond = Fixture(@"
    public interface IAuthRoot
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public interface IAuthLeft : IAuthRoot { }
    public interface IAuthRight : IAuthRoot { }
    public interface IAuth : IAuthLeft, IAuthRight { }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        var expected = GeneratedFactory(declared, "TargetFactory");
        var actual = GeneratedFactory(diamond, "TargetFactory");

        Assert.Contains("authorized = iauth.CanRead(key);", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AbstractBaseClass_InheritedCheck_GeneratesAsDeclaredCheck_ResolvingTheDerivedType()
    {
        var declared = Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var inherited = Fixture(@"
    public abstract class AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string key) => true;
    }
    public class Auth : AuthBase { }
", "Auth");

        var expected = GeneratedFactory(declared, "TargetFactory");
        var actual = GeneratedFactory(inherited, "TargetFactory");

        Assert.Contains("Auth auth = ServiceProvider.GetRequiredService<Auth>();", expected);
        Assert.Contains("services.TryAddTransient<Auth>();", expected);
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("AuthBase", actual);
    }

    [Fact]
    public void OverriddenCheck_WithOrWithoutRestatedAttribute_GeneratesAsDeclaredOnce()
    {
        var declared = Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var overridden = Fixture(@"
    public abstract class AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public virtual bool CanRead(string key) => false;
    }
    public class Auth : AuthBase { public override bool CanRead(string key) => true; }
", "Auth");

        var restated = Fixture(@"
    public abstract class AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public virtual bool CanRead(string key) => false;
    }
    public class Auth : AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public override bool CanRead(string key) => true;
    }
", "Auth");

        var expected = GeneratedFactory(declared, "TargetFactory");

        Assert.Contains("authorized = auth.CanRead(key);", expected);
        Assert.Equal(expected, GeneratedFactory(overridden, "TargetFactory"));
        Assert.Equal(expected, GeneratedFactory(restated, "TargetFactory"));
    }

    [Fact]
    public void ClassImplementingAttributedInterface_GeneratesAsCheckDeclaredOnClass()
    {
        var declared = Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var implemented = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public class Auth : IChecks { public bool CanRead(string key) => true; }
", "Auth");

        // Implemented through a base class, and listed again on the class: still one check.
        var implementedViaBase = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public abstract class AuthBase : IChecks { public bool CanRead(string key) => true; }
    public class Auth : AuthBase, IChecks { }
", "Auth");

        var expected = GeneratedFactory(declared, "TargetFactory");

        Assert.Contains("Auth auth = ServiceProvider.GetRequiredService<Auth>();", expected);
        Assert.Contains("authorized = auth.CanRead(key);", expected);
        Assert.Equal(expected, GeneratedFactory(implemented, "TargetFactory"));
        Assert.Equal(expected, GeneratedFactory(implementedViaBase, "TargetFactory"));
    }

    [Fact]
    public void RestatedCheck_GeneratesAsOneDeclarationNamingTheUnionOfOperations()
    {
        // One check, declared once for Create and once for Fetch, in each shape a restatement
        // takes. Every shape must generate what one declaration naming both operations generates.
        var union = Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var createOnly = Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var classRestatesInterface = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var overrideRestated = Fixture(@"
    public abstract class AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        public virtual bool CanRead(string key) => false;
    }
    public class Auth : AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public override bool CanRead(string key) => true;
    }
", "Auth");

        var newMemberRestated = Fixture(@"
    public class AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        public bool CanRead(string key) => false;
    }
    public class Auth : AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public new bool CanRead(string key) => true;
    }
", "Auth");

        var siblingInterfaces = Fixture(@"
    public interface ICreateChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        bool CanRead(string key);
    }
    public interface IFetchChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        bool CanRead(string key);
    }
    public class Auth : ICreateChecks, IFetchChecks { public bool CanRead(string key) => true; }
", "Auth");

        var expected = GeneratedFactory(union, "TargetFactory");

        // The comparison discriminates: dropping Fetch would generate different code.
        Assert.NotEqual(expected, GeneratedFactory(createOnly, "TargetFactory"));

        Assert.Equal(expected, GeneratedFactory(classRestatesInterface, "TargetFactory"));
        Assert.Equal(expected, GeneratedFactory(overrideRestated, "TargetFactory"));
        Assert.Equal(expected, GeneratedFactory(newMemberRestated, "TargetFactory"));
        Assert.Equal(expected, GeneratedFactory(siblingInterfaces, "TargetFactory"));
    }

    // ------------------------------------------------------------------------
    // Operations are read from the bound attribute constant, so every form an
    // argument can take counts. Before, they were read from the argument's text:
    // on a restated (non-representative) declaration an alias, a const or a cast
    // contributed nothing, and on a sole declaration the alias threw.
    // ------------------------------------------------------------------------

    private const string AliasUsing = "using Op = DTZero.AuthorizeFactoryOperation;\n";

    private const string OpsConstants = @"
    public static class Ops
    {
        public const AuthorizeFactoryOperation FetchOp = AuthorizeFactoryOperation.Fetch;
    }
";

    /// <summary>
    /// The interface declares the check for Fetch in the given form; the class restates it for
    /// Create, so the class's declaration is the representative and the interface's is not.
    /// </summary>
    private static string RestatedWithInterfaceForm(string fetchArgument, string extraDeclarations = "") => Fixture(extraDeclarations + @"
    public interface IChecks
    {
        [AuthorizeFactory(" + fetchArgument + @")]
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanRead(string key) => true;
    }
", "Auth");

    private static string UnionDeclaredOnce(string extraDeclarations = "") => Fixture(extraDeclarations + @"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        public bool CanRead(string key) => true;
    }
", "Auth");

    [Fact]
    public void RestatedCheck_NonRepresentativeOperationsThroughUsingAlias_JoinTheUnion()
    {
        var expected = GeneratedFactory(AliasUsing + UnionDeclaredOnce(), "TargetFactory");
        var actual = GeneratedFactory(AliasUsing + RestatedWithInterfaceForm("Op.Fetch"), "TargetFactory");

        Assert.Contains("authorized = auth.CanRead(key);", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RestatedCheck_NonRepresentativeOperationsThroughConst_JoinTheUnion()
    {
        var expected = GeneratedFactory(UnionDeclaredOnce(OpsConstants), "TargetFactory");
        var actual = GeneratedFactory(RestatedWithInterfaceForm("Ops.FetchOp", OpsConstants), "TargetFactory");

        Assert.Contains("authorized = auth.CanRead(key);", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RestatedCheck_NonRepresentativeOperationsThroughCast_JoinTheUnion()
    {
        var expected = GeneratedFactory(UnionDeclaredOnce(), "TargetFactory");
        var actual = GeneratedFactory(RestatedWithInterfaceForm("(AuthorizeFactoryOperation)2"), "TargetFactory");

        Assert.Contains("authorized = auth.CanRead(key);", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SoleDeclaration_OperationsThroughUsingAlias_GeneratesAsTheNamedOperation()
    {
        var declared = AliasUsing + Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var aliased = AliasUsing + Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(Op.Read)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var expected = GeneratedFactory(declared, "TargetFactory");

        Assert.Contains("authorized = auth.CanRead(key);", expected);
        Assert.Equal(expected, GeneratedFactory(aliased, "TargetFactory"));
    }

    [Fact]
    public void RestatedCheck_DerivedInterfaceRestatesBaseInterface_GeneratesAsOneDeclarationNamingTheUnion()
    {
        var union = Fixture(@"
    public interface IAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        bool CanRead(string key);
    }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        var restated = Fixture(@"
    public interface IAuthRoot
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        bool CanRead(string key);
    }
    public interface IAuth : IAuthRoot
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        new bool CanRead(string key);
    }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        Assert.Equal(GeneratedFactory(union, "TargetFactory"), GeneratedFactory(restated, "TargetFactory"));
    }

    [Fact]
    public void RestatedCheck_RemoteOnAnyDeclaration_MakesTheCombinedCheckRemote()
    {
        var remoteUnion = Fixture(@"
    public class Auth
    {
        [Remote]
        [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var localUnion = Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        // [Remote] only on the interface's declaration; the class restates the check without it.
        var remoteOnInterface = Fixture(@"
    public interface IChecks
    {
        [Remote]
        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanRead(string key) => true;
    }
", "Auth");

        var expected = GeneratedFactory(remoteUnion, "TargetFactory");

        Assert.NotEqual(expected, GeneratedFactory(localUnion, "TargetFactory"));
        Assert.Equal(expected, GeneratedFactory(remoteOnInterface, "TargetFactory"));
    }

    [Fact]
    public void RestatedCheck_DifferingOnlyInNullableAnnotation_IsOneCheck()
    {
        static string Source(string authDeclarations) => Usings + @"
#nullable enable
namespace TestNamespace
{
" + authDeclarations + @"
    [Factory]
    [AuthorizeFactory<Auth>]
    public partial class Target
    {
        public string Key { get; set; } = """";

        [Create]
        public void Create(string? key) { Key = key ?? """"; }
    }
}
";

        var declared = Source(@"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string? key) => true;
    }
");

        var restated = Source(@"
    public abstract class AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public virtual bool CanRead(string key) => false;
    }
    public class Auth : AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public override bool CanRead(string? key) => true;
    }
");

        var expected = GeneratedFactory(declared, "TargetFactory");
        var actual = GeneratedFactory(restated, "TargetFactory");

        Assert.Contains("authorized = auth.CanRead(key);", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InterfaceFactory_InheritedCheck_GeneratesAsDeclaredCheck()
    {
        const string service = @"
    [Factory]
    [AuthorizeFactory<IAuth>]
    public interface IThingService
    {
        Task<string> Run(string input);
    }
";
        var declared = Usings + "namespace TestNamespace\n{\n" + @"
    public interface IAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        bool CanRun(string input);
    }
    public class Auth : IAuth { public bool CanRun(string input) => true; }
" + service + "}\n";

        var inherited = Usings + "namespace TestNamespace\n{\n" + @"
    public interface IAuthRoot
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        bool CanRun(string input);
    }
    public interface IAuth : IAuthRoot { }
    public class Auth : IAuth { public bool CanRun(string input) => true; }
" + service + "}\n";

        var expected = GeneratedFactory(declared, "IThingServiceFactory");
        var actual = GeneratedFactory(inherited, "IThingServiceFactory");

        Assert.Contains("iauth.CanRun(input)", expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StaticFactory_InheritedCheck_GeneratesAsDeclaredCheck()
    {
        const string commands = @"
    [Factory]
    [AuthorizeFactory<Auth>]
    public static partial class Commands
    {
        [Remote, Execute]
        private static Task<string> _Run(string input) => Task.FromResult(input);
    }
";
        var declared = Usings + "namespace TestNamespace\n{\n" + @"
    public class Auth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        public bool CanRun(string input) => true;
    }
" + commands + "}\n";

        var inherited = Usings + "namespace TestNamespace\n{\n" + @"
    public abstract class AuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        public bool CanRun(string input) => true;
    }
    public class Auth : AuthBase { }
" + commands + "}\n";

        var expected = GeneratedFactory(declared, "Commands");
        var actual = GeneratedFactory(inherited, "Commands");

        Assert.Contains(".CanRun(input)", expected);
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("AuthBase", actual);
    }
}
