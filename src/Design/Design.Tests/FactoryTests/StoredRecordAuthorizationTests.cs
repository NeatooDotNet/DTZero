// =============================================================================
// DESIGN SOURCE OF TRUTH: A Write Rule That Decides From the Stored Record
// =============================================================================
//
// LockableOrderAuth looks the order up in the server's store by the target's id
// and refuses a write while the stored row is Locked. Each test locks an order
// through the client, then sends the order back claiming otherwise.
//
// The contrast, a rule that reads the Status the client sends, is pinned in
// ParamAuthorizationTests (ParamAuth_Save_DecidesFromTheStatusTheClientSends).
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;
using DTZero;

namespace Design.Tests.FactoryTests;

public class StoredRecordAuthorizationTests
{
    /// <summary>
    /// Creates and saves an order, then locks it and saves that: both writes are
    /// allowed, because no stored row is Locked when they run.
    /// </summary>
    private static async Task<ILockableOrder> SaveLockedOrder(ILockableOrderFactory factory, string customer)
    {
        var order = (await factory.Save(factory.Create(customer)))!;
        order.Status = LockableOrderStatus.Locked;
        return (await factory.Save(order))!;
    }

    /// <summary>
    /// An order stored Locked, sent back with Status Active and a new name, is
    /// refused on the server, and the stored row keeps its status and name.
    /// </summary>
    [Fact]
    public async Task LockedInStore_SentAsActive_SaveIsRefused_AndTheStoredRowIsUnchanged()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ILockableOrderFactory>();
        var store = server.GetRequiredService<ILockableOrderStore>();
        var locked = await SaveLockedOrder(factory, "Lockable_Tamper");
        Assert.Equal(LockableOrderStatus.Locked, (await store.FindAsync(locked.Id))!.Status);

        var sent = (await factory.Fetch(locked.Id))!;
        sent.Status = LockableOrderStatus.Active;   // the client says it is not locked
        sent.CustomerName = "Changed";

        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Save(sent));

        var row = (await store.FindAsync(locked.Id))!;
        Assert.Equal(LockableOrderStatus.Locked, row.Status);
        Assert.Equal("Lockable_Tamper", row.CustomerName);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Marking the stored order new routes its Save to Insert; the rule finds the
    /// stored row by id all the same, and the row is not replaced.
    /// </summary>
    [Fact]
    public async Task LockedInStore_SentAsNew_SaveIsRefused_AndTheStoredRowIsUnchanged()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ILockableOrderFactory>();
        var store = server.GetRequiredService<ILockableOrderStore>();
        var locked = await SaveLockedOrder(factory, "Lockable_AsNew");
        var storedRow = await store.FindAsync(locked.Id);

        locked.IsNew = true;
        locked.Status = LockableOrderStatus.Active;
        locked.CustomerName = "Replaced";

        var result = await factory.TrySave(locked);

        Assert.False(result.HasAccess);
        var row = (await store.FindAsync(locked.Id))!;
        Assert.Same(storedRow, row);
        Assert.Equal(LockableOrderStatus.Locked, row.Status);
        Assert.Equal("Lockable_AsNew", row.CustomerName);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// An order stored Active saves an edit: the rule refuses only a Locked row.
    /// </summary>
    [Fact]
    public async Task ActiveInStore_SaveIsAllowed_AndTheStoredRowChanges()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ILockableOrderFactory>();
        var store = server.GetRequiredService<ILockableOrderStore>();
        var order = (await factory.Save(factory.Create("Lockable_Active")))!;

        order.CustomerName = "Edited";
        var saved = await factory.Save(order);

        Assert.NotNull(saved);
        Assert.Equal("Edited", (await store.FindAsync(order.Id))!.CustomerName);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// CanSave(target) sends the object to the server, which answers from the stored
    /// row: refused for an order stored Locked whatever its Status says, allowed for
    /// one stored Active. CanSave() without the object has no rule to run, since the
    /// only write rule needs the target, and allows.
    /// </summary>
    [Fact]
    public async Task CanSaveTarget_AnswersFromTheStoredRow()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ILockableOrderFactory>();
        var calls = client.GetRequiredService<ServerServiceProvider>();
        var locked = await SaveLockedOrder(factory, "Lockable_CanSave");
        var active = (await factory.Save(factory.Create("Lockable_CanSave_Active")))!;
        locked.Status = LockableOrderStatus.Active;

        var callsBefore = calls.RemoteCallCount;
        var lockedAnswer = await factory.CanSave(locked);
        Assert.Equal(callsBefore + 1, calls.RemoteCallCount);

        Assert.False(lockedAnswer.HasAccess);
        Assert.True((await factory.CanSave(active)).HasAccess);
        Assert.True(factory.CanSave().HasAccess);

        server.Dispose();
        client.Dispose();
    }
}
