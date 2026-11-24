# A Public API Over the Same Model

DTZero's endpoint, `/api/dtzero`, is for your own application's client. It is not a public API. Its wire format is the object graph itself, and it changes whenever the model does. That is fine for a client that ships with the server, and wrong for a caller you do not control.

For outside callers you write an API by hand. This page is about what sits behind it.

## Why this matters

The common outcome in an application with both a UI and a public API is two copies of the logic. The application can do everything. The API can do a few things. The rules for those few things are written once for the application and once more against the API's DTOs, and they drift.

With DTZero the domain model already runs on the server, with its rules in it. A public endpoint is server code like any other: it takes the generated factory by injection and calls the same methods the application's client calls.

## The contract is designed

The type an outside caller sees is one you design for them. It is small, it is stable, and it is not the domain object:

<!-- snippet: public-api-contract -->
<a id='snippet-public-api-contract'></a>
```cs
public sealed record PublicOrder(int Id, string Customer, string Status, decimal Total, string Currency, int LineCount)
{
    public static PublicOrder From(IOrder order) => new(
        order.Id, order.CustomerName, order.Status.ToString(),
        order.Total.Amount, order.Total.Currency, order.Lines.Count);
}
```
<sup><a href='/src/Design/Design.Server/PublicApi/PublicOrderApi.cs#L101-L108' title='Snippet source file'>snippet source</a> | <a href='#snippet-public-api-contract' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Returning `IOrder` itself would tie outside callers to every change in the model, which is the coupling a public contract exists to prevent.

## The logic is the one copy

The endpoints load the aggregate through its factory, use the domain's own verdict and verb, and save through the factory:

<!-- snippet: public-api-endpoints -->
<a id='snippet-public-api-endpoints'></a>
```cs
public static IEndpointRouteBuilder MapPublicOrderApi(this IEndpointRouteBuilder app)
{
    var orders = app.MapGroup("/api/public/orders");

    orders.MapGet("/{id}", async (int id, IOrderFactory factory, CancellationToken ct) =>
        await factory.Fetch(id, ct) is { } order
            ? Results.Ok(PublicOrder.From(order))
            : Results.NotFound());

    orders.MapPost("/{id}/submit", async (int id, IOrderFactory factory, CancellationToken ct) =>
    {
        var order = await factory.Fetch(id, ct);
        if (order is null)
            return Results.NotFound();
        if (!order.CanSubmit)   // the domain's rule, not a copy of it
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "The order cannot be submitted.");

        order.Submit();
        var saved = await factory.Save(order, ct)
            ?? throw new InvalidOperationException("Saving the order returned nothing.");
        return Results.Ok(PublicOrder.From(saved));
    });

    return app;
}
```
<sup><a href='/src/Design/Design.Server/PublicApi/PublicOrderApi.cs#L69-L95' title='Snippet source file'>snippet source</a> | <a href='#snippet-public-api-endpoints' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The endpoint does not restate when an order can be submitted. It reads `CanSubmit`, the same property a screen reads, and calls `Submit()`, the same method a screen calls. If the rule changes, it changes in one place.

On the server, a factory call is an ordinary method call. The `[Service]` parameters of `Fetch` and of the write resolve from the request's scope, with nothing extra to register.

## Mapping it

The endpoints are mapped beside DTZero's own, in the server's `Program`:

<!-- snippet: getting-started-server-program -->
<a id='snippet-getting-started-server-program'></a>
```cs
var builder = WebApplication.CreateBuilder(args);

// DTZero plus the server-only services [Service] parameters resolve (ServerServices.cs)
builder.Services.AddDesignServer();

var app = builder.Build();

// Serve the hosted Blazor WASM client
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Sign the caller in before the endpoint runs: the authorization checks read HttpContext.User
app.UseAuthentication();
app.UseAuthorization();

// The DTZero endpoint: one route for every factory call, no controllers
app.UseDTZero();

// A public API for outside callers: hand-written endpoints over the same factories (PublicApi/)
app.MapPublicOrderApi();
app.MapPublicAuthorizedOrderApi();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```
<sup><a href='/src/Design/Design.Server/Program.cs#L66-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Authorization comes with the factory

An aggregate's authorization rules are on its factory, so a public endpoint that uses the factory is under them. The endpoint does not restate who may read an order:

<!-- snippet: public-api-authorized -->
<a id='snippet-public-api-authorized'></a>
```cs
public static IEndpointRouteBuilder MapPublicAuthorizedOrderApi(this IEndpointRouteBuilder app)
{
    var orders = app.MapGroup("/api/public/authorized-orders");

    orders.MapGet("/{id}", async (int id, IAuthorizedOrderFactory factory, CancellationToken ct) =>
    {
        if (!factory.CanFetch(ct).HasAccess)   // the order's rules, asked of its factory
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "The caller may not read this order.");

        return await factory.Fetch(id, ct) is { } order
            ? Results.Ok(PublicAuthorizedOrder.From(order))
            : Results.NotFound();
    });

    return app;
}
```
<sup><a href='/src/Design/Design.Server/PublicApi/PublicOrderApi.cs#L134-L153' title='Snippet source file'>snippet source</a> | <a href='#snippet-public-api-authorized' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To server code a refused `Fetch` returns `null`, the same as "not found". The endpoint asks `CanFetch` first so that it can answer 403. [Authorization](authorization.md) covers the rules themselves.

## Things to know

- **Authentication, rate limiting and versioning are the application's.** The sample adds none. Apply them to the public endpoints as you would to any ASP.NET Core API. The factory's rules read the caller that authentication established.
- **Route constraints and the WebAssembly fallback.** In a server that hosts a Blazor WebAssembly client, a request that matches no endpoint is answered with `index.html`. A route such as `/{id:int}` does not match a non-numeric id, so that request gets the page and a 200. The sample leaves the constraint off; a bad id then reaches the endpoint and gets a 400.
- **Read models work the same way.** A list or summary type with a `[Fetch]` can back a public query endpoint through its factory.

## When to leave it out

If nothing outside your application calls it, you need no public API and no contract types. The application's own client goes through `/api/dtzero`.

## Next steps

- [Why DTZero](the-problem.md)
- [ASP.NET Core Integration](aspnetcore-integration.md)
- [Authorization](authorization.md)
