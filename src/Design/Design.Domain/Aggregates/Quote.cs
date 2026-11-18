// =============================================================================
// DESIGN SOURCE OF TRUTH: An Aggregate Authorized Through a Shared Base
// =============================================================================
//
// Quote's factory names QuoteAuth, whose broad read and write checks are
// inherited from AuthorizationBase. Nothing on Quote or QuoteAuth restates
// them, and the generated factory enforces them.
//
// DESIGN DECISION: Same shape as AuthorizedOrder
//
// Internal class with a public interface, a local Create (D14), and [Remote]
// internal Fetch and writes. Only the auth type differs, so the tests show the
// inherited checks and nothing else.
//
// =============================================================================

using DTZero;

namespace Design.Domain.Aggregates;

/// <summary>
/// Public interface for the Quote aggregate root.
/// </summary>
public interface IQuote : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
    decimal Amount { get; set; }
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}

/// <summary>
/// Aggregate root whose authorization comes from a shared base.
/// </summary>
#region authorization-base-apply
[Factory]
[AuthorizeFactory<QuoteAuth>]
internal partial class Quote : IQuote, IFactorySaveMeta, IFactoryOnCompleteAsync
#endregion
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert)
        {
            IsNew = false;
        }
        return Task.CompletedTask;
    }

    [Create]
    public void Create(string customerName)
    {
        CustomerName = customerName;
        IsNew = true;
    }

    [Remote, Fetch]
    internal void Fetch(int id)
    {
        // Simulate loading from database
        Id = id;
        CustomerName = $"Customer_{id}";
        Amount = 250.00m;
        IsNew = false;
    }

    [Remote, Insert]
    internal Task Insert()
    {
        // Simulate database insert with ID assignment
        Id = Random.Shared.Next(1000, 9999);
        return Task.CompletedTask;
    }

    [Remote, Update]
    internal Task Update()
    {
        // Simulate database update
        return Task.CompletedTask;
    }

    [Remote, Delete]
    internal Task Delete()
    {
        // Simulate database delete
        return Task.CompletedTask;
    }
}
