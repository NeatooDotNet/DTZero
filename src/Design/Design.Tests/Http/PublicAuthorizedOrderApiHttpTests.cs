// =============================================================================
// DESIGN SOURCE OF TRUTH: An Authorized Aggregate Through the Public API
// =============================================================================
//
// GET /api/public/authorized-orders/{id} (PublicApi/PublicOrderApi.cs) takes
// IAuthorizedOrderFactory, so AuthorizedOrderAuth's rules run over the request's
// signed-in user, as they do for the application's own client. Each test signs
// the request in as a named user (DesignHttpHost's test scheme). What the tests
// pin:
// - a caller the rules refuse is refused through the public endpoint, and the
//   application's own client, as the same user, gets null from Fetch;
// - an anonymous caller is refused by the broad HasAccess check;
// - a caller the rules allow gets the designed contract.
//
// =============================================================================

using System.Net;
using System.Text.Json;
using Design.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.Http;

[Collection(AuthorizedOrderSwitches.Name)]
public class PublicAuthorizedOrderApiHttpTests(DesignHttpHost host)
{
    private static readonly Uri OrderSeven = new("/api/public/authorized-orders/7", UriKind.Relative);

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;

    /// <summary>
    /// CanFetch needs Sales or Support. A Manager alone is answered 403, with no
    /// contract, and the application's own client, signed in as the same Manager,
    /// gets null from the same factory's Fetch.
    /// </summary>
    [Fact]
    public async Task Get_CallerTheRulesRefuse_Is403_LikeTheApplicationsFetch()
    {
        using var http = host.CreateDefaultClient(new AsUser(OrderRoles.Manager));

        using var response = await http.GetAsync(OrderSeven, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False((await ReadJson(response)).TryGetProperty("customer", out _));

        using var client = host.CreateClientScope(new AsUser(OrderRoles.Manager));
        Assert.Null(await client.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>()
            .Fetch(7, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// HasAccess, the broad check, needs a signed-in user: an anonymous caller is
    /// answered 403 too.
    /// </summary>
    [Fact]
    public async Task Get_AnonymousCaller_Is403()
    {
        using var http = host.CreateDefaultClient(new AsUser(TestUserScheme.Anonymous));

        using var response = await http.GetAsync(OrderSeven, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// A Support user passes HasAccess and CanFetch, and gets the designed contract.
    /// </summary>
    [Fact]
    public async Task Get_CallerTheRulesAllow_GetsTheContract()
    {
        using var http = host.CreateDefaultClient(new AsUser(OrderRoles.Support));

        using var response = await http.GetAsync(OrderSeven, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal(
            ["customer", "id", "total"],
            body.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal(7, body.GetProperty("id").GetInt32());
        Assert.Equal("Customer_7", body.GetProperty("customer").GetString());
        Assert.Equal(100.00m, body.GetProperty("total").GetDecimal());
    }
}
