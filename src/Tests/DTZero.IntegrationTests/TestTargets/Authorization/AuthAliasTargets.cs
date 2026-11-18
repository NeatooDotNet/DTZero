using DTZero;
using Op = DTZero.AuthorizeFactoryOperation;

namespace DTZeroTests.IntegrationTests.TestTargets.Authorization;

// ============================================================================
// A restated check whose base declaration names its operation through a `using` alias (PITCH-009).
//
// Operations are read from the bound attribute constant, so `Op.Fetch` counts as
// AuthorizeFactoryOperation.Fetch. Before, the base declaration's operations were read from the
// argument's text, found nothing, and Fetch went unchecked. Kept in its own file because the
// generator copies a target file's using directives into its generated factories.
// ============================================================================

public abstract class AliasedBaseAuth(AuthInheritanceLog log)
{
    protected AuthInheritanceLog Log { get; } = log;

    [AuthorizeFactory(Op.Fetch)]
    public virtual bool CanAliased(string key)
    {
        Log.Record($"AliasedBaseAuth.CanAliased:{key}");
        return true;
    }
}

/// <summary>Restates the base's aliased Fetch check for Create; the check runs for both.</summary>
public class AliasedDerivedAuth(AuthInheritanceLog log) : AliasedBaseAuth(log)
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public override bool CanAliased(string key)
    {
        Log.Record($"AliasedDerivedAuth.CanAliased:{key}");
        return key != AuthInheritanceKeys.Denied;
    }
}

[Factory]
[AuthorizeFactory<AliasedDerivedAuth>]
public partial class AliasedCheckTarget
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
