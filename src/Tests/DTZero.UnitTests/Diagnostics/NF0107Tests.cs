using Microsoft.CodeAnalysis;
using DTZeroTests.UnitTests.TestContainers;

namespace DTZeroTests.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0107 error: an authorization check in the auth type's hierarchy has no declaration
/// in this compilation's source, so no check can be generated for it (PITCH-009).
/// </summary>
/// <remarks>
/// Each declaration the generator cannot see is compiled into a separate in-memory assembly and
/// passed as a metadata reference, as a shared auth base in another library would be. Before
/// NF0107 these checks were dropped with only a debug message and the operation ran unchecked.
/// The in-source inheritance fixtures in InheritedAuthEmissionTests assert they raise no NF0107.
/// </remarks>
public class NF0107Tests
{
    private const string Usings = @"
using DTZero;
using System;
using System.Threading;
using System.Threading.Tasks;
";

    private const string SharedInterface = Usings + @"
namespace SharedAuth
{
    public interface ISharedReadAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
}
";

    private const string SharedAbstractBase = Usings + @"
namespace SharedAuth
{
    public abstract class SharedAuthBase
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string key) => true;
    }
}
";

    private const string SharedAuthClass = Usings + @"
namespace SharedAuth
{
    public class SharedAuthClass
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead(string key) => true;
    }
}
";

    private static string ClassTarget(string authDeclarations, string authTypeName) => Usings + @"
namespace TestNamespace
{
" + authDeclarations + @"
    [Factory]
    [AuthorizeFactory<" + authTypeName + @">]
    public partial class Target
    {
        public string Key { get; set; } = """";

        [Create]
        public void Create(string key) { Key = key; }

        [Remote, Fetch]
        internal void Fetch(string key) { Key = key; }
    }
}
";

    private static List<Diagnostic> NF0107(string source, MetadataReference reference)
    {
        var (diagnostics, _, _) = DiagnosticTestHelper.RunGeneratorWithReferences(source, reference);
        return diagnostics.Where(d => d.Id == "NF0107").ToList();
    }

    private static string LocatedText(string source, Diagnostic diagnostic) =>
        source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);

    [Fact]
    public void NF0107_CheckInheritedFromInterfaceInReferencedAssembly_ReportsError()
    {
        var reference = DiagnosticTestHelper.EmitReference(SharedInterface, "SharedAuthInterface");
        var source = ClassTarget(@"
    public interface IAuth : SharedAuth.ISharedReadAuth { }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        var diagnostic = Assert.Single(NF0107(source, reference));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            "Authorization check 'CanRead' declared on 'SharedAuth.ISharedReadAuth' cannot be generated for auth type 'IAuth': "
            + "its declaration is not in this compilation's source. Declare the check on 'IAuth' (or another type in this project) so it can be enforced.",
            diagnostic.GetMessage());
        Assert.Equal("AuthorizeFactory<IAuth>", LocatedText(source, diagnostic));
    }

    [Fact]
    public void NF0107_CheckInheritedFromAbstractBaseClassInReferencedAssembly_ReportsError()
    {
        var reference = DiagnosticTestHelper.EmitReference(SharedAbstractBase, "SharedAuthBase");
        var source = ClassTarget(@"
    public class Auth : SharedAuth.SharedAuthBase { }
", "Auth");

        var diagnostic = Assert.Single(NF0107(source, reference));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("'CanRead' declared on 'SharedAuth.SharedAuthBase'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("auth type 'Auth'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("not in this compilation's source", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal("AuthorizeFactory<Auth>", LocatedText(source, diagnostic));
    }

    [Fact]
    public void NF0107_AuthTypeItselfInReferencedAssembly_ReportsError()
    {
        var reference = DiagnosticTestHelper.EmitReference(SharedAuthClass, "SharedAuthClass");
        var source = ClassTarget("", "SharedAuth.SharedAuthClass");

        var diagnostic = Assert.Single(NF0107(source, reference));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("'CanRead' declared on 'SharedAuth.SharedAuthClass'", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void NF0107_InterfaceFactory_CheckInheritedFromReferencedAssembly_ReportsError()
    {
        var reference = DiagnosticTestHelper.EmitReference(Usings + @"
namespace SharedAuth
{
    public interface ISharedRunAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        bool CanRun(string input);
    }
}
", "SharedRunAuth");
        var source = Usings + @"
namespace TestNamespace
{
    public interface IAuth : SharedAuth.ISharedRunAuth { }
    public class Auth : IAuth { public bool CanRun(string input) => true; }

    [Factory]
    [AuthorizeFactory<IAuth>]
    public interface IThingService
    {
        Task<string> Run(string input);
    }
}
";

        var diagnostic = Assert.Single(NF0107(source, reference));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("'CanRun' declared on 'SharedAuth.ISharedRunAuth'", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void NF0107_CheckRestatedOnTheAuthType_NoError()
    {
        // The remedy the message names: restating the check on the auth type in source. The
        // restatement is the most-derived declaration, so it is the one generated and the
        // referenced one is not needed.
        var reference = DiagnosticTestHelper.EmitReference(SharedInterface, "SharedAuthInterfaceRestated");
        var source = ClassTarget(@"
    public interface IAuth : SharedAuth.ISharedReadAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        new bool CanRead(string key);
    }
    public class Auth : IAuth { public bool CanRead(string key) => true; }
", "IAuth");

        Assert.Empty(NF0107(source, reference));
    }
}
