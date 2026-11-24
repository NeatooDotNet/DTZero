// =============================================================================
// DESIGN SOURCE OF TRUTH: A Public API Over the Same Domain Graph
// =============================================================================
//
// Each test calls Design.Server's hand-written public endpoints
// (PublicApi/PublicOrderApi.cs) over HTTP, as an outside caller would, and reads
// the stored order back through the application's own path: a Remote-mode
// DTZero client. What the tests pin:
// - the caller gets the designed contract, not the domain object;
// - a submit through the public API changes the order the application sees;
// - the public API refuses what the domain refuses, with the domain's own rule,
//   and agrees with the command the application's client sends.
//
// =============================================================================

using System.Net;
using System.Text.Json;
using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.Http;

public class PublicOrderApiHttpTests(DesignHttpHost host)
{
    private static async Task<IOrder> SaveNewOrder(IServiceScope client, string customer, int lines)
    {
        var orderFactory = client.ServiceProvider.GetRequiredService<IOrderFactory>();
        var lineFactory = client.ServiceProvider.GetRequiredService<IOrderLineFactory>();
        var order = orderFactory.Create(customer);
        for (var i = 0; i < lines; i++)
            order.AddLine(lineFactory.Create($"Widget P{i}", 4.00m, 3));
        return (await orderFactory.Save(order, TestContext.Current.CancellationToken))!;
    }

    private static async Task<IOrder> FetchThroughTheApplication(IServiceScope client, int id)
        => (await client.ServiceProvider.GetRequiredService<IOrderFactory>().Fetch(id, TestContext.Current.CancellationToken))!;

    private static Uri PublicOrderUri(int id, string action = "")
        => new($"/api/public/orders/{id}{action}", UriKind.Relative);

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;

    /// <summary>
    /// GET returns the designed contract: exactly its six fields, none of IOrder's
    /// (no IsNew, IsDeleted or line graph), the status by name.
    /// </summary>
    [Fact]
    public async Task Get_StoredOrder_ReturnsTheDesignedContract()
    {
        using var client = host.CreateClientScope();
        var saved = await SaveNewOrder(client, "Public_Get", lines: 2);
        using var http = host.CreateClient();

        using var response = await http.GetAsync(PublicOrderUri(saved.Id), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJson(response);
        Assert.Equal(
            ["currency", "customer", "id", "lineCount", "status", "total"],
            body.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal(saved.Id, body.GetProperty("id").GetInt32());
        Assert.Equal("Public_Get", body.GetProperty("customer").GetString());
        Assert.Equal("Draft", body.GetProperty("status").GetString());
        Assert.Equal(24.00m, body.GetProperty("total").GetDecimal());
        Assert.Equal("USD", body.GetProperty("currency").GetString());
        Assert.Equal(2, body.GetProperty("lineCount").GetInt32());
    }

    /// <summary>
    /// The factory's Fetch returns null for an order that is not stored; the endpoint
    /// answers 404 with no contract.
    /// </summary>
    [Fact]
    public async Task Get_MissingOrder_Is404()
    {
        using var http = host.CreateClient();

        using var response = await http.GetAsync(PublicOrderUri(987654), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
    }

    /// <summary>
    /// An id that is not a number reaches the endpoint and is a 400; it does not fall
    /// through to the server's SPA fallback (the Blazor client's index.html).
    /// </summary>
    [Fact]
    public async Task Get_NonNumericId_Is400()
    {
        using var http = host.CreateClient();

        using var response = await http.GetAsync(new Uri("/api/public/orders/abc", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Submit through the public API runs Order.Submit and saves through the factory:
    /// the application's own client then fetches the order as Submitted.
    /// </summary>
    [Fact]
    public async Task Submit_DraftWithLines_ChangesTheStoredOrder()
    {
        using var client = host.CreateClientScope();
        var saved = await SaveNewOrder(client, "Public_Submit", lines: 1);
        using var http = host.CreateClient();

        using var response = await http.PostAsync(PublicOrderUri(saved.Id, "/submit"), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Submitted", (await ReadJson(response)).GetProperty("status").GetString());
        var stored = await FetchThroughTheApplication(client, saved.Id);
        Assert.Equal(OrderStatus.Submitted, stored.Status);
        Assert.Single(stored.Lines);
    }

    /// <summary>
    /// An order with no lines cannot be submitted: the public API answers 409 and the
    /// stored order stays a Draft. The application's own command, sent by its client,
    /// refuses the same order: one rule, Order.CanSubmit, behind both.
    /// </summary>
    [Fact]
    public async Task Submit_EmptyDraft_IsRefused_LikeTheApplicationsCommand_AndStaysADraft()
    {
        using var client = host.CreateClientScope();
        var saved = await SaveNewOrder(client, "Public_Empty", lines: 0);
        using var http = host.CreateClient();

        using var response = await http.PostAsync(PublicOrderUri(saved.Id, "/submit"), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(OrderStatus.Draft, (await FetchThroughTheApplication(client, saved.Id)).Status);

        var submitOrder = client.ServiceProvider.GetRequiredService<OrderCommands.SubmitOrder>();
        Assert.Equal(SubmitOutcome.NotSubmittable, await submitOrder(saved.Id, TestContext.Current.CancellationToken));
        Assert.Equal(OrderStatus.Draft, (await FetchThroughTheApplication(client, saved.Id)).Status);
    }

    /// <summary>
    /// An order already submitted (here by the application's own command) is refused
    /// by the public API, and the stored order is unchanged.
    /// </summary>
    [Fact]
    public async Task Submit_AlreadySubmitted_IsRefused_AndTheStoredOrderIsUnchanged()
    {
        using var client = host.CreateClientScope();
        var saved = await SaveNewOrder(client, "Public_Twice", lines: 2);
        var submitOrder = client.ServiceProvider.GetRequiredService<OrderCommands.SubmitOrder>();
        Assert.Equal(SubmitOutcome.Submitted, await submitOrder(saved.Id, TestContext.Current.CancellationToken));
        using var http = host.CreateClient();

        using var response = await http.PostAsync(PublicOrderUri(saved.Id, "/submit"), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var stored = await FetchThroughTheApplication(client, saved.Id);
        Assert.Equal(OrderStatus.Submitted, stored.Status);
        Assert.Equal(2, stored.Lines.Count);
    }

    /// <summary>
    /// Submitting an order that is not stored is a 404, as GET is.
    /// </summary>
    [Fact]
    public async Task Submit_MissingOrder_Is404()
    {
        using var http = host.CreateClient();

        using var response = await http.PostAsync(PublicOrderUri(987655, "/submit"), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
