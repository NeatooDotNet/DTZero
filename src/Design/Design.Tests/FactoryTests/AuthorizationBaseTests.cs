// =============================================================================
// DESIGN SOURCE OF TRUTH: Broad Checks Declared Once, on a Shared Base
// =============================================================================
//
// Quote's factory names QuoteAuth. QuoteAuth inherits IsSignedIn (Read) and
// CanWrite (Write) from AuthorizationBase, restates neither, and adds
// CanDelete (Manager). These tests pin that the inherited checks are enforced
// on Quote's operations, and that the derived check still refuses on its own.
//
// DESIGN DECISION: Through the client container
//
// Each test calls the client's factory. Fetch and Save cross to the server,
// which runs the checks there: the client's Remote Fetch and Save do not check
// before sending. The Can* methods run on the client with no server call, and
// each test pins that their answer matches what the server then does.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;
using DTZero;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class AuthorizationBaseTests
{
    /// <summary>
    /// A caller who is not signed in is refused by the base's read check, which
    /// QuoteAuth does not restate: the client's CanFetch and CanCreate say no, and
    /// the server returns no quote.
    /// </summary>
    [Fact]
    public async Task SignedOut_IsRefusedFetch_ByTheInheritedReadCheck()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IQuoteFactory>();
        var calls = client.GetRequiredService<ServerServiceProvider>();
        client.GetRequiredService<TestUser>().IsAuthenticated = false;

        Assert.False(factory.CanFetch().HasAccess);
        Assert.False(factory.CanCreate().HasAccess);
        Assert.Equal(0, calls.RemoteCallCount);

        var quote = await factory.Fetch(7);

        Assert.Null(quote);
        Assert.Equal(1, calls.RemoteCallCount);   // refused on the server
        Assert.Null(factory.Create("Signed Out"));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A caller without Sales passes the base's read check and fails its write
    /// check: the quote is fetched, and its Save is refused on the server.
    /// CanFetch says yes and CanUpdate and CanSave say no, as the server does.
    /// </summary>
    [Fact]
    public async Task ReadOnlyCaller_CanFetch_ButIsRefusedSave_ByTheInheritedWriteCheck()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IQuoteFactory>();
        client.GetRequiredService<TestUser>().Roles.Remove(OrderRoles.Sales);

        Assert.True(factory.CanFetch().HasAccess);
        Assert.False(factory.CanUpdate().HasAccess);
        Assert.False(factory.CanSave().HasAccess);

        var quote = await factory.Fetch(7);
        Assert.NotNull(quote);
        Assert.Equal(7, quote.Id);

        quote.Amount = 1.00m;
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Save(quote));
        Assert.False((await factory.TrySave(quote)).HasAccess);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A caller with Sales and without Manager passes both inherited checks and
    /// fails QuoteAuth's own CanDelete: an edit saves, a delete is refused.
    /// CanUpdate says yes, CanDelete no, and CanSave, which runs every write
    /// check, no.
    /// </summary>
    [Fact]
    public async Task NonManager_SavesAnEdit_ButIsRefusedDelete_ByTheDerivedClassCheck()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IQuoteFactory>();
        client.GetRequiredService<TestUser>().Roles.Remove(OrderRoles.Manager);

        Assert.True(factory.CanFetch().HasAccess);
        Assert.True(factory.CanUpdate().HasAccess);
        Assert.False(factory.CanDelete().HasAccess);
        Assert.False(factory.CanSave().HasAccess);

        var quote = (await factory.Fetch(7))!;
        quote.Amount = 300.00m;
        var saved = await factory.Save(quote);
        Assert.NotNull(saved);
        Assert.Equal(300.00m, saved.Amount);

        saved.IsDeleted = true;
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Save(saved));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A signed-in Sales Manager passes every check: the inherited checks do not
    /// refuse a caller they should allow.
    /// </summary>
    [Fact]
    public async Task SalesManager_PassesEveryCheck()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IQuoteFactory>();

        Assert.True(factory.CanCreate().HasAccess);
        Assert.True(factory.CanFetch().HasAccess);
        Assert.True(factory.CanSave().HasAccess);

        var created = factory.Create("New Quote");
        Assert.NotNull(created);
        var inserted = await factory.Save(created);
        Assert.NotNull(inserted);
        Assert.False(inserted.IsNew);

        var fetched = (await factory.Fetch(inserted.Id))!;
        fetched.IsDeleted = true;
        Assert.NotNull(await factory.Save(fetched));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// The generator registers the auth type the factory names, on both tiers, and
    /// not the base it inherits from.
    /// </summary>
    [Fact]
    public void OnlyTheNamedAuthClass_IsRegistered()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();

        foreach (var scope in new[] { server, client })
        {
            Assert.NotNull(scope.ServiceProvider.GetService<QuoteAuth>());
            Assert.Null(scope.ServiceProvider.GetService<AuthorizationBase>());
        }

        server.Dispose();
        client.Dispose();
    }
}
