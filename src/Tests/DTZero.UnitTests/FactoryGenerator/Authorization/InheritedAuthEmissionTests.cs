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

        // Every check in these fixtures is declared in source, so none may be reported as
        // impossible to generate (NF0107, see NF0107Tests).
        Assert.DoesNotContain(diagnostics, d => d.Id == "NF0107");

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
