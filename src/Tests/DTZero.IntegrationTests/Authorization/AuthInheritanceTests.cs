using Microsoft.Extensions.DependencyInjection;
using DTZero;
using DTZeroTests.IntegrationTests.TestContainers;
using DTZeroTests.IntegrationTests.TestTargets.Authorization;

namespace DTZeroTests.IntegrationTests.Authorization;

/// <summary>
/// Authorization checks an auth type inherits are enforced as checks it declares (PITCH-009).
/// </summary>
/// <remarks>
/// Before the fix, a check on a base interface was never read (the method walk followed
/// BaseType only, and an interface has none), so the operation ran unchecked; and a check on
/// a base class was resolved as the base class, so an abstract base failed the container at
/// startup. Each test builds its own containers with a per-container
/// <see cref="AuthInheritanceLog"/>, so the log shows which checks ran on which tier.
/// </remarks>
public class AuthInheritanceTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) Scopes()
    {
        static void Configure(IServiceCollection services) => services.AddSingleton<AuthInheritanceLog>();
        return ClientServerContainers.Scopes(configureClient: Configure, configureServer: Configure, configureLocal: Configure);
    }

    private static AuthInheritanceLog Log(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<AuthInheritanceLog>();

    private static int RemoteCalls(IServiceScope client) =>
        client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count;

    private static string NewKey() => Guid.NewGuid().ToString("N");

    // ========================================================================
    // A check declared on a base interface refuses an operation on a type
    // whose auth interface only inherits it.
    // ========================================================================

    [Fact]
    public async Task InterfaceChain_InheritedCheck_RefusesFetch()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IChainedCheckTargetFactory>();

        var result = await factory.Fetch(AuthInheritanceKeys.Denied);

        Assert.Null(result);
        Assert.Equal(["ChainedCheckAuth.CanReadKey:denied"], Log(server).Calls);
        Assert.Empty(Log(client).Calls);
    }

    [Fact]
    public async Task InterfaceChain_InheritedCheck_RefusesCreate()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IChainedCheckTargetFactory>();

        var result = await factory.Create(AuthInheritanceKeys.Denied);

        Assert.Null(result);
        Assert.Equal(["ChainedCheckAuth.CanReadKey:denied"], Log(server).Calls);
    }

    [Fact]
    public async Task InterfaceChain_InheritedCheck_AllowsFetch()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IChainedCheckTargetFactory>();
        var key = NewKey();

        var result = await factory.Fetch(key);

        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        Assert.Equal([$"ChainedCheckAuth.CanReadKey:{key}"], Log(server).Calls);
    }

    // ========================================================================
    // The same inherited check is asked by the client through Can* and
    // answers as the server does.
    // ========================================================================

    [Fact]
    public async Task InterfaceChain_InheritedCheck_ClientCanFetch_AnswersAsServer()
    {
        var (client, server, _) = Scopes();
        var clientFactory = client.ServiceProvider.GetRequiredService<IChainedCheckTargetFactory>();
        var serverFactory = server.ServiceProvider.GetRequiredService<IChainedCheckTargetFactory>();
        var allowedKey = NewKey();

        var clientDenied = await clientFactory.CanFetch(AuthInheritanceKeys.Denied);
        var clientAllowed = await clientFactory.CanFetch(allowedKey);
        var remoteCalls = RemoteCalls(client);

        var serverDenied = await serverFactory.CanFetch(AuthInheritanceKeys.Denied);
        var serverAllowed = await serverFactory.CanFetch(allowedKey);

        Assert.False(clientDenied.HasAccess);
        Assert.True(clientAllowed.HasAccess);
        Assert.Equal(serverDenied.HasAccess, clientDenied.HasAccess);
        Assert.Equal(serverAllowed.HasAccess, clientAllowed.HasAccess);

        // The check is [Remote] on the base interface: the client asked the server, and the
        // check never ran on the client.
        Assert.Equal(2, remoteCalls);
        Assert.Empty(Log(client).Calls);
        Assert.Equal(2, Log(server).CallsFor(allowedKey).Count);
    }

    [Fact]
    public async Task InterfaceChain_InheritedCheck_ClientCanCreate_AnswersAsServer()
    {
        var (client, server, _) = Scopes();
        var clientFactory = client.ServiceProvider.GetRequiredService<IChainedCheckTargetFactory>();
        var serverFactory = server.ServiceProvider.GetRequiredService<IChainedCheckTargetFactory>();
        var allowedKey = NewKey();

        var clientDenied = await clientFactory.CanCreate(AuthInheritanceKeys.Denied);
        var clientAllowed = await clientFactory.CanCreate(allowedKey);
        var serverDenied = await serverFactory.CanCreate(AuthInheritanceKeys.Denied);
        var serverAllowed = await serverFactory.CanCreate(allowedKey);

        Assert.False(clientDenied.HasAccess);
        Assert.True(clientAllowed.HasAccess);
        Assert.Equal(serverDenied.HasAccess, clientDenied.HasAccess);
        Assert.Equal(serverAllowed.HasAccess, clientAllowed.HasAccess);
        Assert.Empty(Log(client).Calls);
    }

    // ========================================================================
    // A check declared on a base class is enforced when the factory names the
    // derived auth type, with only the derived type registered.
    // ========================================================================

    [Fact]
    public async Task BaseClass_InheritedCheck_RefusesFetch()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IBaseClassCheckTargetFactory>();

        var result = await factory.Fetch(AuthInheritanceKeys.Denied);

        Assert.Null(result);
        Assert.Contains("BaseClassCheckAuth.CanReadKey:denied", Log(server).Calls);
    }

    [Fact]
    public void BaseClass_InheritedCheck_RefusesCreate_OnTheCallingTier()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IBaseClassCheckTargetFactory>();

        var result = factory.Create(AuthInheritanceKeys.Denied);

        Assert.Null(result);
        Assert.Equal(["BaseClassCheckAuth.CanReadKey:denied"], Log(client).Calls);
        Assert.Empty(Log(server).Calls);
    }

    [Fact]
    public void BaseClass_InheritedCheck_ClientCanCreateAndCanFetch()
    {
        var (client, _, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IBaseClassCheckTargetFactory>();
        var allowedKey = NewKey();

        Assert.False(factory.CanCreate(AuthInheritanceKeys.Denied).HasAccess);
        Assert.True(factory.CanCreate(allowedKey).HasAccess);
        Assert.False(factory.CanFetch(AuthInheritanceKeys.Denied).HasAccess);
        Assert.True(factory.CanFetch(allowedKey).HasAccess);
    }

    [Fact]
    public async Task BaseClass_OnlyTheDerivedAuthTypeIsRegistered_AndTheFactoryWorks()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IBaseClassCheckTargetFactory>();
        var key = NewKey();

        // The generated registrar names the derived type only. Before the fix it also
        // registered the abstract base, and building the container failed.
        Assert.Null(server.ServiceProvider.GetService<BaseClassCheckAuth>());
        Assert.Null(client.ServiceProvider.GetService<BaseClassCheckAuth>());
        Assert.NotNull(server.ServiceProvider.GetService<DerivedClassCheckAuth>());

        var fetched = await factory.Fetch(key);
        var created = factory.Create(key);

        Assert.NotNull(fetched);
        Assert.Equal(key, fetched.Key);
        Assert.NotNull(created);
        Assert.Equal(key, created.Key);
    }

    // ========================================================================
    // A check reachable through two inheritance paths runs once per operation.
    // ========================================================================

    [Fact]
    public async Task Diamond_CheckRunsOnce_PerFetch()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IDiamondCheckTargetFactory>();
        var key = NewKey();

        var result = await factory.Fetch(key);

        Assert.NotNull(result);
        Assert.Equal([$"DiamondCheckAuth.CanReadDiamond:{key}"], Log(server).CallsFor(key));
    }

    [Fact]
    public void Diamond_CheckRunsOnce_PerCreateAndPerCanCreate()
    {
        var (client, _, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IDiamondCheckTargetFactory>();
        var createKey = NewKey();
        var canKey = NewKey();

        var created = factory.Create(createKey);
        var can = factory.CanCreate(canKey);

        Assert.NotNull(created);
        Assert.True(can.HasAccess);
        Assert.Equal([$"DiamondCheckAuth.CanReadDiamond:{createKey}"], Log(client).CallsFor(createKey));
        Assert.Equal([$"DiamondCheckAuth.CanReadDiamond:{canKey}"], Log(client).CallsFor(canKey));
    }

    [Fact]
    public async Task Diamond_Check_Refuses()
    {
        var (client, _, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IDiamondCheckTargetFactory>();

        Assert.Null(await factory.Fetch(AuthInheritanceKeys.Denied));
        Assert.Null(factory.Create(AuthInheritanceKeys.Denied));
        Assert.False(factory.CanFetch(AuthInheritanceKeys.Denied).HasAccess);
    }

    // ========================================================================
    // A check a derived auth class overrides runs the derived version.
    // ========================================================================

    [Fact]
    public async Task Override_WithoutAttribute_RunsTheDerivedVersion()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IBaseClassCheckTargetFactory>();

        // The base version allows every key; only the derived override refuses this one.
        var result = await factory.Fetch(AuthInheritanceKeys.DeniedByOverride);

        Assert.Null(result);
        var calls = Log(server).CallsFor(AuthInheritanceKeys.DeniedByOverride);
        Assert.Contains("DerivedClassCheckAuth.CanFetchOverridden:denied-by-override", calls);
        Assert.DoesNotContain("BaseClassCheckAuth.CanFetchOverridden:denied-by-override", calls);
    }

    [Fact]
    public async Task Override_WithRestatedAttribute_RunsTheDerivedVersion()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IBaseClassCheckTargetFactory>();

        var result = await factory.Fetch(AuthInheritanceKeys.DeniedByRestatement);

        Assert.Null(result);
        var calls = Log(server).CallsFor(AuthInheritanceKeys.DeniedByRestatement);
        Assert.Contains("DerivedClassCheckAuth.CanFetchRestated:denied-by-restatement", calls);
        Assert.DoesNotContain("BaseClassCheckAuth.CanFetchRestated:denied-by-restatement", calls);
    }

    [Fact]
    public async Task Overrides_EachCheckRunsOnce_AsTheDerivedVersion_PerFetch()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IBaseClassCheckTargetFactory>();
        var key = NewKey();

        var result = await factory.Fetch(key);

        Assert.NotNull(result);
        string[] expected =
        [
            $"BaseClassCheckAuth.CanReadKey:{key}",
            $"DerivedClassCheckAuth.CanFetchOverridden:{key}",
            $"DerivedClassCheckAuth.CanFetchRestated:{key}",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), Log(server).CallsFor(key).Order(StringComparer.Ordinal));
    }

    // ========================================================================
    // The interface-factory and static-factory renderers resolve and register
    // the auth type the same way; their inherited checks are enforced too.
    // ========================================================================

    [Fact]
    public async Task InterfaceFactory_InheritedInterfaceCheck_IsEnforced()
    {
        var (client, server, _) = Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInheritedCheckServiceFactory>();
        var key = NewKey();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Run(AuthInheritanceKeys.Denied));
        Assert.Equal($"ran: {key}", await factory.Run(key));
        Assert.Equal([$"ExecuteCheckAuth.CanRun:{key}"], Log(server).CallsFor(key));
    }

    [Fact]
    public async Task StaticFactory_InheritedBaseClassCheck_IsEnforced()
    {
        var (client, server, _) = Scopes();
        var run = client.ServiceProvider.GetRequiredService<InheritedCheckCommands.RunInherited>();
        var key = NewKey();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => run(AuthInheritanceKeys.Denied));
        Assert.Equal($"ran: {key}", await run(key));
        Assert.Equal([$"ExecuteCheckBase.CanRun:{key}"], Log(server).CallsFor(key));
        Assert.Null(server.ServiceProvider.GetService<ExecuteCheckBase>());
    }
}
