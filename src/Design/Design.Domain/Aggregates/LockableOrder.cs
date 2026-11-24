// =============================================================================
// DESIGN SOURCE OF TRUTH: A Write Rule That Decides From the Stored Record
// =============================================================================
//
// A Locked order cannot be changed. LockableOrder is the aggregate; its rule,
// in LockableOrderAuth.cs, reads the order's STORED status, not the status on
// the object the client sends.
//
// DESIGN DECISION: Status is ordinary, client-settable state
//
// Status crosses the wire like every other property (R-M), so the object a
// Save receives carries whatever status the client put on it. Locking is a
// change the client saves like any other: an Active order saved with Status
// Locked is stored Locked. What the rule refuses is a write to an order whose
// stored row is Locked, whatever the object claims.
//
// CONTRAST WITH: ParamAuthOrder.cs
//
// ParamAuthOrderAuth.CanWrite(target) reads target.Status: the status the client
// sent. That is a rule about the request. It refuses a client that says the
// order is Locked, and passes one that says it is not.
//
// =============================================================================

using DTZero;

namespace Design.Domain.Aggregates;

/// <summary>
/// The statuses a LockableOrder has.
/// </summary>
public static class LockableOrderStatus
{
    public const string Active = "Active";
    public const string Locked = "Locked";
}

/// <summary>
/// Public interface for the LockableOrder aggregate root.
/// </summary>
public interface ILockableOrder : IFactorySaveMeta
{
    Guid Id { get; set; }
    string CustomerName { get; set; }
    string Status { get; set; }
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}

/// <summary>
/// Aggregate root whose writes are refused while its stored row is Locked.
/// </summary>
[Factory]
[AuthorizeFactory<ILockableOrderAuth>]
internal partial class LockableOrder : ILockableOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = LockableOrderStatus.Active;

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
        Id = Guid.NewGuid();
        CustomerName = customerName;
        Status = LockableOrderStatus.Active;
        IsNew = true;
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] ILockableOrderStore store)
    {
        var row = await store.FindAsync(id);
        if (row is null)
            return false;

        Id = row.Id;
        CustomerName = row.CustomerName;
        Status = row.Status;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    internal Task Insert([Service] ILockableOrderStore store)
    {
        store.Add(new LockableOrderRow { Id = Id, CustomerName = CustomerName, Status = Status });
        return Task.CompletedTask;
    }

    [Remote, Update]
    internal async Task Update([Service] ILockableOrderStore store)
    {
        var row = await store.FindAsync(Id)
            ?? throw new InvalidOperationException($"LockableOrder {Id} has no stored row.");
        row.CustomerName = CustomerName;
        row.Status = Status;
    }
}

/// <summary>
/// The stored shape of a LockableOrder.
/// </summary>
public class LockableOrderRow
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = LockableOrderStatus.Active;
}

/// <summary>
/// The server's LockableOrder rows. Server-only: the client never resolves it.
/// </summary>
public interface ILockableOrderStore
{
    Task<LockableOrderRow?> FindAsync(Guid id);
    void Add(LockableOrderRow row);
}

/// <summary>
/// In-memory LockableOrder rows, standing in for a database.
/// </summary>
/// <remarks>
/// Rows returned by FindAsync are the stored instances, so Update's mapping lands in
/// the store directly. Not named for its interface, so RegisterMatchingName (which
/// also runs on clients) leaves it alone; the server registers it explicitly.
/// </remarks>
public class InMemoryLockableOrderStore : ILockableOrderStore
{
    private readonly Dictionary<Guid, LockableOrderRow> rows = [];
    private readonly object gate = new();

    public Task<LockableOrderRow?> FindAsync(Guid id)
    {
        lock (gate)
        {
            return Task.FromResult(rows.TryGetValue(id, out var row) ? row : null);
        }
    }

    public void Add(LockableOrderRow row)
    {
        lock (gate)
        {
            rows[row.Id] = row;
        }
    }
}
