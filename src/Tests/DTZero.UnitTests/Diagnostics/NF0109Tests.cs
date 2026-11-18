using Microsoft.CodeAnalysis;
using DTZeroTests.UnitTests.TestContainers;

namespace DTZeroTests.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0109 error: the operations of an in-source [AuthorizeFactory(...)] declaration could
/// not be read from its bound attribute constant (PITCH-009).
/// </summary>
/// <remarks>
/// Operations are read from the bound constant, so an alias, a const or a cast reads reliably
/// (InheritedAuthEmissionTests). What remains unreadable is an argument that binds to no constant;
/// these fixtures use an undefined name, so they do not compile, which the generator harness
/// allows. Before NF0109, a restated declaration in this state contributed no operations and the
/// sole declaration made the generator throw.
/// </remarks>
public class NF0109Tests
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

    private static string LocatedText(string source, Diagnostic diagnostic) =>
        source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);

    [Fact]
    public void NF0109_RestatedDeclarationOperationsUnreadable_ReportsError()
    {
        var source = Fixture(@"
    public interface IChecks
    {
        [AuthorizeFactory(NoSuchOperation)]
        bool CanRead(string key);
    }
    public class Auth : IChecks
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanRead(string key) => true;
    }
");

        var diagnostic = Assert.Single(DiagnosticTestHelper.GetDiagnosticsById(source, "NF0109"));

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            "The operations of authorization check 'CanRead' declared on 'TestNamespace.IChecks' (auth type 'Auth') could not be read, "
            + "so the check cannot be generated. Pass a constant AuthorizeFactoryOperation value to [AuthorizeFactory(...)].",
            diagnostic.GetMessage());
        Assert.Equal("AuthorizeFactory(NoSuchOperation)", LocatedText(source, diagnostic));
    }

    [Fact]
    public void NF0109_SoleDeclarationOperationsUnreadable_ReportsError_AndGeneratesNoCall()
    {
        var source = Fixture(@"
    public class Auth
    {
        [AuthorizeFactory(NoSuchOperation)]
        public bool CanRead(string key) => true;
    }
");

        var (diagnostics, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "NF0109");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("'CanRead' declared on 'TestNamespace.Auth'", diagnostic.GetMessage(), StringComparison.Ordinal);

        var factory = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TargetFactory"))
            ?.GetText()
            ?.ToString();
        Assert.NotNull(factory);
        Assert.Contains("LocalCreate", factory);
        Assert.DoesNotContain("CanRead", factory);
    }
}
