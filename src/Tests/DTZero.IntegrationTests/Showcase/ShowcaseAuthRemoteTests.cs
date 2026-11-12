using Microsoft.Extensions.DependencyInjection;
using DTZero;
using DTZeroTests.IntegrationTests.Shared;
using DTZeroTests.IntegrationTests.TestContainers;

namespace DTZeroTests.IntegrationTests.Showcase;

#region Remote Authorization

/// <summary>
/// Remote authorization interface - the authorization check happens on the server.
/// </summary>
public interface IAuthRemote
{
    [Remote]
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool Create();
}

/// <summary>
/// Remote authorization implementation with server-only service dependency.
/// </summary>
internal class AuthServerOnly : IAuthRemote
{
    public AuthServerOnly([Service] IServerOnlyService service)
    {
        Assert.NotNull(service);
    }

    public bool Create()
    {
        return true;
    }
}

#endregion

#region Target Class

/// <summary>
/// Interface for ShowcaseAuthRemoteMode.
/// </summary>
public interface IShowcaseAuthRemoteMode
{
    List<int> IntList { get; set; }
}

/// <summary>
/// Target class with remote authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IAuthRemote>]
internal class ShowcaseAuthRemoteMode : IShowcaseAuthRemoteMode
{
    public ShowcaseAuthRemoteMode()
    {
        IntList = default!;
    }

    public List<int> IntList { get; set; }

    [Create]
    public void Create(List<int> intList)
    {
        IntList = intList;
    }
}

#endregion

/// <summary>
/// Integration tests for remote authorization scenarios.
/// </summary>
public class ShowcaseAuthRemoteTests
{
    private readonly IServiceScope _clientScope;
    private readonly IShowcaseAuthRemoteModeFactory _factory;

    public ShowcaseAuthRemoteTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services =>
            {
                // Auth is remote - no client registration needed
            },
            configureServer: services =>
            {
                services.AddScoped<IAuthRemote, AuthServerOnly>();
            });

        _clientScope = client;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IShowcaseAuthRemoteModeFactory>();
    }

    [Fact]
    public async Task ShowcaseAuthRemoteTest_Create()
    {
        var intList = new List<int> { 1, 2, 3 };
        var result = await _factory.Create(intList);
        Assert.NotNull(result);
        Assert.Equal(intList, result.IntList);
    }

    [Fact]
    public async Task ShowcaseAuthRemoteTest_CanCreate()
    {
        Assert.True(await _factory.CanCreate());
    }
}
