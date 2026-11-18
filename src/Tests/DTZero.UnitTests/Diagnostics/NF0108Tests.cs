using Microsoft.CodeAnalysis;
using DTZeroTests.UnitTests.TestContainers;

namespace DTZeroTests.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0108 error: an authorization check cannot be called through the auth type, because
/// the auth class implements it explicitly, or not at all (PITCH-009).
/// </summary>
/// <remarks>
/// Before NF0108 the generator emitted a call such as <c>auth.TestNamespace.IChecks.CanRead(key)</c>
/// for an explicit implementation carrying the attribute, which failed with CS1061 inside generated
/// code. No call is generated now; each test also asserts the factory contains none.
/// </remarks>
public class NF0108Tests
{
    private static string Fixture(string authDeclarations) => @"
using DTZero;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
" + authDeclarations + @"
    [Factory]
    [AuthorizeFactory<Auth>]
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

    private static (List<Diagnostic> NF0108, string Factory) Run(string source)
    {
        var (diagnostics, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var factory = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TargetFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(factory);
        return (diagnostics.Where(d => d.Id == "NF0108").ToList(), factory);
    }

    private static string LocatedText(string source, Diagnostic diagnostic) =>
        source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);

    [Fact]
    public void NF0108_AttributedInterfaceMethod_ImplementedExplicitly_ReportsError_AndGeneratesNoCall()
    {
        var source = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        bool IChecks.CanRead(string key) => true;
    }
");

        var (nf0108, factory) = Run(source);

        var diagnostic = Assert.Single(nf0108);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            "Authorization check 'IChecks.CanRead' cannot be called through auth type 'Auth': 'Auth' implements it explicitly, or not at all. "
            + "An authorization check must be callable on the auth type; implement 'IChecks.CanRead' implicitly as a public method of 'Auth', "
            + "or name the interface in [AuthorizeFactory<T>].",
            diagnostic.GetMessage());
        Assert.Equal("AuthorizeFactory<Auth>", LocatedText(source, diagnostic));

        Assert.Contains("LocalCreate", factory);
        Assert.DoesNotContain("CanRead", factory);
    }

    [Fact]
    public void NF0108_AttributeOnExplicitImplementation_ReportsError_AndGeneratesNoCall()
    {
        var source = Fixture(@"
    public interface IChecks
    {
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool IChecks.CanRead(string key) => true;
    }
");

        var (nf0108, factory) = Run(source);

        var diagnostic = Assert.Single(nf0108);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("'IChecks.CanRead' cannot be called through auth type 'Auth'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal("AuthorizeFactory<Auth>", LocatedText(source, diagnostic));

        Assert.Contains("LocalCreate", factory);
        Assert.DoesNotContain("CanRead", factory);
    }

    [Fact]
    public void NF0108_AttributeOnBothInterfaceAndExplicitImplementation_ReportsOnce()
    {
        var source = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool IChecks.CanRead(string key) => true;
    }
");

        var (nf0108, factory) = Run(source);

        Assert.Single(nf0108);
        Assert.DoesNotContain("CanRead", factory);
    }

    [Fact]
    public void NF0108_AttributedInterfaceMethod_LeftToDefaultImplementation_ReportsError()
    {
        var source = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key) => true;
    }
    public class Auth : IChecks { }
");

        var (nf0108, factory) = Run(source);

        Assert.Single(nf0108);
        Assert.DoesNotContain("CanRead", factory);
    }

    [Fact]
    public void NF0108_AttributedInterfaceMethod_ImplementedImplicitly_NoDiagnostic()
    {
        var source = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        public bool CanRead(string key) => true;
    }
");

        var (diagnostics, _, _) = DiagnosticTestHelper.RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Id is "NF0107" or "NF0108");
        Assert.Contains("authorized = auth.CanRead(key);", Run(source).Factory);
    }
}
