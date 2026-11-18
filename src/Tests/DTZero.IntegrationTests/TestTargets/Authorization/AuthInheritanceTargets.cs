using System.Collections.Concurrent;
using DTZero;

namespace DTZeroTests.IntegrationTests.TestTargets.Authorization;

// ============================================================================
// Authorization checks an auth type inherits rather than declares (PITCH-009).
//
// Every check records itself in the AuthInheritanceLog of the container that resolved it,
// so a test can read which checks ran, on which tier, which implementation ran, and how
// many times. Each test registers its own log per container, so tests running in
// parallel never see each other's calls. Refusals are decided from the key argument,
// for the same reason: no static switch.
// ============================================================================

/// <summary>
/// Keys the checks below refuse.
/// </summary>
public static class AuthInheritanceKeys
{
    /// <summary>Refused by the check the auth type inherits.</summary>
    public const string Denied = "denied";

    /// <summary>Refused only by the derived override of a check whose base version allows it.</summary>
    public const string DeniedByOverride = "denied-by-override";

    /// <summary>Refused only by the derived restatement (override plus attribute) of a check whose base version allows it.</summary>
    public const string DeniedByRestatement = "denied-by-restatement";

    /// <summary>Refused by a check declared on an interface a base class of the auth class implements.</summary>
    public const string DeniedViaBaseInterface = "denied-via-base-interface";
}

/// <summary>
/// Which authorization checks ran in one container, in order. Registered as a singleton
/// per container by each test.
/// </summary>
public sealed class AuthInheritanceLog
{
    private readonly ConcurrentQueue<string> _calls = new();

    public IReadOnlyList<string> Calls => [.. _calls];

    public void Record(string call) => _calls.Enqueue(call);

    /// <summary>The calls recorded for one key, which each test makes unique.</summary>
    public IReadOnlyList<string> CallsFor(string key) => [.. _calls.Where(c => c.EndsWith($":{key}", StringComparison.Ordinal))];
}

#region Interface chain - the check is declared two interfaces up

/// <summary>
/// Declares the check. [Remote], so the factory's Create and Fetch, and their Can* methods,
/// are asked of the server.
/// </summary>
public interface IChainedCheckRoot
{
    [Remote]
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanReadKey(string key);
}

public interface IChainedCheckMiddle : IChainedCheckRoot { }

/// <summary>
/// The auth type the factory names. It declares nothing; its only check is inherited.
/// </summary>
public interface IChainedCheckAuth : IChainedCheckMiddle { }

public class ChainedCheckAuth(AuthInheritanceLog log) : IChainedCheckAuth
{
    public bool CanReadKey(string key)
    {
        log.Record($"ChainedCheckAuth.CanReadKey:{key}");
        return key != AuthInheritanceKeys.Denied;
    }
}

[Factory]
[AuthorizeFactory<IChainedCheckAuth>]
public partial class ChainedCheckTarget
{
    public string Key { get; set; } = "";

    [Create]
    public void Create(string key)
    {
        Key = key;
    }

    [Remote, Fetch]
    internal void Fetch(string key)
    {
        Key = key;
    }
}

#endregion

#region Interface diamond - one check reachable through two paths

public interface IDiamondCheckRoot
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanReadDiamond(string key);
}

public interface IDiamondCheckLeft : IDiamondCheckRoot { }

public interface IDiamondCheckRight : IDiamondCheckRoot { }

/// <summary>
/// Reaches IDiamondCheckRoot.CanReadDiamond through both IDiamondCheckLeft and IDiamondCheckRight.
/// </summary>
public interface IDiamondCheckAuth : IDiamondCheckLeft, IDiamondCheckRight { }

public class DiamondCheckAuth(AuthInheritanceLog log) : IDiamondCheckAuth
{
    public bool CanReadDiamond(string key)
    {
        log.Record($"DiamondCheckAuth.CanReadDiamond:{key}");
        return key != AuthInheritanceKeys.Denied;
    }
}

/// <summary>
/// The check is not [Remote]: Create and CanCreate run it on the calling tier, and the
/// [Remote] Fetch runs it on the server.
/// </summary>
[Factory]
[AuthorizeFactory<IDiamondCheckAuth>]
public partial class DiamondCheckTarget
{
    public string Key { get; set; } = "";

    [Create]
    public void Create(string key)
    {
        Key = key;
    }

    [Remote, Fetch]
    internal void Fetch(string key)
    {
        Key = key;
    }
}

#endregion

#region Base class - checks declared on an abstract base, two of them overridden

/// <summary>
/// Abstract, so it cannot be resolved as a service: the factory must resolve the derived type.
/// </summary>
public abstract class BaseClassCheckAuth(AuthInheritanceLog log)
{
    protected AuthInheritanceLog Log { get; } = log;

    /// <summary>Inherited unchanged by the derived auth type.</summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanReadKey(string key)
    {
        Log.Record($"BaseClassCheckAuth.CanReadKey:{key}");
        return key != AuthInheritanceKeys.Denied;
    }

    /// <summary>Allows everything; the derived type overrides it without restating the attribute.</summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    public virtual bool CanFetchOverridden(string key)
    {
        Log.Record($"BaseClassCheckAuth.CanFetchOverridden:{key}");
        return true;
    }

    /// <summary>Allows everything; the derived type overrides it and restates the attribute.</summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    public virtual bool CanFetchRestated(string key)
    {
        Log.Record($"BaseClassCheckAuth.CanFetchRestated:{key}");
        return true;
    }
}

/// <summary>
/// The auth type the factory names, and the only one registered.
/// </summary>
public class DerivedClassCheckAuth(AuthInheritanceLog log) : BaseClassCheckAuth(log)
{
    public override bool CanFetchOverridden(string key)
    {
        Log.Record($"DerivedClassCheckAuth.CanFetchOverridden:{key}");
        return key != AuthInheritanceKeys.DeniedByOverride;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    public override bool CanFetchRestated(string key)
    {
        Log.Record($"DerivedClassCheckAuth.CanFetchRestated:{key}");
        return key != AuthInheritanceKeys.DeniedByRestatement;
    }
}

[Factory]
[AuthorizeFactory<DerivedClassCheckAuth>]
public partial class BaseClassCheckTarget
{
    public string Key { get; set; } = "";

    [Create]
    public void Create(string key)
    {
        Key = key;
    }

    [Remote, Fetch]
    internal void Fetch(string key)
    {
        Key = key;
    }
}

#endregion

#region Class auth type - checks declared on interfaces the class implements

/// <summary>
/// Implemented by the auth class directly. CanFetchRestatedOnClass is declared here for Fetch, and
/// the class restates it with [AuthorizeFactory] for Create: the check runs for both.
/// </summary>
public interface IImplementedChecks
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanReadImplemented(string key);

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetchRestatedOnClass(string key);
}

/// <summary>
/// Implemented by the auth class's base class, and listed again on the auth class, so its check
/// is reachable two ways.
/// </summary>
public interface IBaseImplementedChecks
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetchViaBase(string key);
}

public abstract class ImplementedChecksBase(AuthInheritanceLog log) : IBaseImplementedChecks
{
    protected AuthInheritanceLog Log { get; } = log;

    public bool CanFetchViaBase(string key)
    {
        Log.Record($"ImplementedChecksBase.CanFetchViaBase:{key}");
        return key != AuthInheritanceKeys.DeniedViaBaseInterface;
    }
}

/// <summary>
/// The auth type the factory names: a class. Its checks are declared on the interfaces it
/// implements, apart from the one it restates.
/// </summary>
public class ImplementedChecksAuth(AuthInheritanceLog log) : ImplementedChecksBase(log), IImplementedChecks, IBaseImplementedChecks
{
    public bool CanReadImplemented(string key)
    {
        Log.Record($"ImplementedChecksAuth.CanReadImplemented:{key}");
        return key != AuthInheritanceKeys.Denied;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public bool CanFetchRestatedOnClass(string key)
    {
        Log.Record($"ImplementedChecksAuth.CanFetchRestatedOnClass:{key}");
        return key != AuthInheritanceKeys.DeniedByRestatement;
    }
}

[Factory]
[AuthorizeFactory<ImplementedChecksAuth>]
public partial class ImplementedChecksTarget
{
    public string Key { get; set; } = "";

    [Create]
    public void Create(string key)
    {
        Key = key;
    }

    [Remote, Fetch]
    internal void Fetch(string key)
    {
        Key = key;
    }
}

#endregion

#region Restated checks - declarations of one check combine their operations

// Each target below declares one check, CanRestated(string key), at two places in the auth
// type's hierarchy: one declaration for Create, the other for Fetch. The check runs once for
// both operations; neither declaration removes the other's operation.

public interface IRestatedRootChecks
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanRestated(string key);
}

/// <summary>Derived interface restating the base interface's check for another operation.</summary>
public interface IRestatedInterfaceAuth : IRestatedRootChecks
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    new bool CanRestated(string key);
}

public class RestatedInterfaceAuth(AuthInheritanceLog log) : IRestatedInterfaceAuth
{
    public bool CanRestated(string key)
    {
        log.Record($"RestatedInterfaceAuth.CanRestated:{key}");
        return key != AuthInheritanceKeys.Denied;
    }
}

[Factory]
[AuthorizeFactory<IRestatedInterfaceAuth>]
public partial class RestatedInterfaceTarget
{
    public string Key { get; set; } = "";

    [Create]
    public void Create(string key)
    {
        Key = key;
    }

    [Remote, Fetch]
    internal void Fetch(string key)
    {
        Key = key;
    }
}

public abstract class RestatedOverrideBase(AuthInheritanceLog log)
{
    protected AuthInheritanceLog Log { get; } = log;

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public virtual bool CanRestated(string key)
    {
        Log.Record($"RestatedOverrideBase.CanRestated:{key}");
        return true;
    }
}

/// <summary>Override restating the abstract base's check for another operation.</summary>
public class RestatedOverrideAuth(AuthInheritanceLog log) : RestatedOverrideBase(log)
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    public override bool CanRestated(string key)
    {
        Log.Record($"RestatedOverrideAuth.CanRestated:{key}");
        return key != AuthInheritanceKeys.Denied;
    }
}

[Factory]
[AuthorizeFactory<RestatedOverrideAuth>]
public partial class RestatedOverrideTarget
{
    public string Key { get; set; } = "";

    [Create]
    public void Create(string key)
    {
        Key = key;
    }

    [Remote, Fetch]
    internal void Fetch(string key)
    {
        Key = key;
    }
}

public interface ISiblingCreateChecks
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanRestated(string key);
}

public interface ISiblingFetchChecks
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanRestated(string key);
}

/// <summary>
/// Implements two unrelated interfaces that declare the same check, with one public method.
/// </summary>
public class SiblingChecksAuth(AuthInheritanceLog log) : ISiblingCreateChecks, ISiblingFetchChecks
{
    public bool CanRestated(string key)
    {
        log.Record($"SiblingChecksAuth.CanRestated:{key}");
        return key != AuthInheritanceKeys.Denied;
    }
}

[Factory]
[AuthorizeFactory<SiblingChecksAuth>]
public partial class SiblingChecksTarget
{
    public string Key { get; set; } = "";

    [Create]
    public void Create(string key)
    {
        Key = key;
    }

    [Remote, Fetch]
    internal void Fetch(string key)
    {
        Key = key;
    }
}

#endregion

#region Interface factory and static factory - same inheritance, the other two renderers

public interface IExecuteCheckRoot
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanRun(string input);
}

/// <summary>Declares nothing; its only check is inherited.</summary>
public interface IExecuteCheckAuth : IExecuteCheckRoot { }

public class ExecuteCheckAuth(AuthInheritanceLog log) : IExecuteCheckAuth
{
    public bool CanRun(string input)
    {
        log.Record($"ExecuteCheckAuth.CanRun:{input}");
        return input != AuthInheritanceKeys.Denied;
    }
}

/// <summary>
/// Interface factory whose auth interface only inherits its check.
/// </summary>
[Factory]
[AuthorizeFactory<IExecuteCheckAuth>]
public interface IInheritedCheckService
{
    Task<string> Run(string input);
}

public class InheritedCheckService : IInheritedCheckService
{
    public Task<string> Run(string input) => Task.FromResult($"ran: {input}");
}

public abstract class ExecuteCheckBase(AuthInheritanceLog log)
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanRun(string input)
    {
        log.Record($"ExecuteCheckBase.CanRun:{input}");
        return input != AuthInheritanceKeys.Denied;
    }
}

/// <summary>Declares nothing; its only check is on the abstract base.</summary>
public class ExecuteCheckDerived(AuthInheritanceLog log) : ExecuteCheckBase(log) { }

/// <summary>
/// Static-factory command whose auth class only inherits its check.
/// </summary>
[Factory]
[AuthorizeFactory<ExecuteCheckDerived>]
public static partial class InheritedCheckCommands
{
    [Remote, Execute]
    private static Task<string> _RunInherited(string input)
    {
        return Task.FromResult($"ran: {input}");
    }
}

#endregion
