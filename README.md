# DTZero

**Zero DTOs.**

DTOs and API controllers exist because the browser ran JavaScript and the server ran .NET. With WebAssembly, the browser runs .NET too. DTZero lets one domain model run on both sides.

It is a Roslyn source generator. You write the domain model once, in a library both tiers load. DTZero generates the factories and the serialization that carry your objects, and the calls on them, between client and server through a single endpoint. You write no DTOs, no controllers and no mapping code for your own application's client.

## Why DTOs and controllers existed

Your domain model could not run in a JavaScript client. So the server flattened it to JSON, and the client rebuilt something like it in another language. Every operation was written four times: the domain method, the DTO, the controller action and the client call. Rules that the screen needed were written again in the client, and the two copies drifted.

That was a workaround for a language boundary, and it became habit. Blazor WebAssembly removed the boundary: the same assembly loads in the browser and on the server. WPF and other .NET clients never had the boundary at all. Many teams still build the DTOs and controllers anyway, because that is how it has been done.

DTZero is what you build when you stop. The `Order` the screen edits is the `Order` the server saves. A rule written on it runs on both tiers, and the compiler checks every call that crosses.

If you are choosing a client technology, this is a reason to choose WebAssembly.

## What it looks like

Client code calls a generated factory:

<!-- snippet: readme-client-usage -->
<a id='snippet-readme-client-usage'></a>
```cs
// The customer's draft: the stored one, or a new one. One server call.
IOrder? order = await orderFactory.OpenDraft("Customer_42");

// Build a line on the client and add it. No server call.
order.AddLine(lineFactory.Create("Widget C", 12.50m, 2));

// Save sends the whole order; keep what it returns.
order = await orderFactory.Save(order);
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/OrderUsageTests.cs#L31-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-client-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The method behind a call that crosses is ordinary code on the domain class. `[Remote]` marks it as an entry point from the client, and each `[Service]` parameter is resolved on the server:

<!-- snippet: operations-fetch-instance -->
<a id='snippet-operations-fetch-instance'></a>
```cs
/// <summary>
/// Loads an existing Order and its lines; false when there is none.
/// </summary>
[Remote, Fetch]
internal async Task<bool> Fetch(
    int id,
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindAsync(id);
    if (row == null)
        return false;

    LoadFrom(row, lineListFactory);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L251-L268' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-instance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

There is no controller for this and no transfer type. The server maps one endpoint, `/api/dtzero`, and every factory call goes through it.

## The one idea: factory methods call factory methods

The client calls one factory method. That call crosses to the server. From there, factory methods call factory methods, and nothing else crosses.

```text
client                      server
------                      ------
orderFactory.Save(order) -> Order.Insert                 writes the order's row
                              lineListFactory.Save       walks the lines
                                lineFactory.Save         each line writes its own row
```

<!-- snippet: operations-insert -->
<a id='snippet-operations-insert'></a>
```cs
/// <summary>
/// Inserts a new Order and its lines.
/// </summary>
[Remote, Insert]
internal async Task Insert(
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = new OrderRow();
    MapTo(row);
    db.Add(row);
    lineListFactory.Save(Lines, row.Lines);
    await db.SaveChangesAsync();
    Id = row.Id;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L270-L286' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each object reads and writes its own row and hands the rest to the next factory. In other designs this is the work a repository does for the whole aggregate; here no method has to know the whole graph. Loading works the same way in the other direction.

It also means root and child are not kinds of class. They are roles. An entity is a root where the client calls its factory method, and a child where another factory method does. The method's parameters are the whole difference, and you write them.

## "But..."

These are the objections most readers have, in the order they tend to arrive.

**"This puts persistence in my entities."** The object graph that crosses is your conceptual model, not your storage model. The EF entities, or whatever shape your store has, stay on the server behind a `[Service]` parameter the client never sees. The two shapes are free to differ, and usually should. DTZero supplies no repository and no persistence model; those are yours.

**"This only matters if I already use WebAssembly."** It matters when you decide whether to. A JavaScript client commits you to the DTO and controller layer for the life of the application. A .NET client does not.

**"DTOs do more than cross a language boundary."** They do. They also fix a contract for callers you do not control. Your own client is not such a caller: it ships with the server and is built from the same source. For callers outside the application, you still design a contract by hand, and you should. What changes is what sits behind it. The public endpoint calls the same factories and the same domain methods, so the logic exists once:

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

The usual outcome otherwise is an application that can do everything, a public API that can do a few things, and the rules for those few things written twice.

Whatever arrives from a client is input. Authorization runs on the server for every call, and the server's own methods decide what is stored.

## Authorization

Authorization is a class, with its dependencies injected, written once:

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
public class AuthorizedOrderAuth(ICurrentUser user) : IAuthorizedOrderAuth
{
    public bool HasAccess() => user.IsAuthenticated;

    public bool CanCreate() => user.IsInRole(OrderRoles.Sales);

    public bool CanFetch() => user.IsInRole(OrderRoles.Sales) || user.IsInRole(OrderRoles.Support);

    public bool CanDelete() => user.IsInRole(OrderRoles.Manager);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs#L122-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
[Factory]
[AuthorizeFactory<IAuthorizedOrderAuth>]
internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs#L99-L103' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The server enforces these checks on every operation they cover. The client asks the same questions through generated `Can*` methods, so a screen can hide what the user may not do without a second copy of the rule. ASP.NET Core policies work too, through `[AspAuthorize]`. See [Authorization](docs/authorization.md).

## Who it is for

Teams building an application, not only an API, with a .NET client: Blazor WebAssembly or WPF. It fits best where the domain has real behavior, the client and server are built and deployed together, and you want the model to be the design.

It works with any class: an aggregate, a read model, a static command, a record. No base class is required.

## When not to use it

- **Your only client is not .NET.** A JavaScript or native mobile client cannot load your assembly. A designed HTTP API is the right tool.
- **Blazor Server only, with no plan for a client tier.** There is no seam to remove.
- **Thin CRUD.** If the application is forms over tables with no behavior, scaffolding is less to learn.
- **Client and server are versioned and deployed apart.** DTZero assumes both tiers are built from the same source. If old clients must keep working against new servers for long periods, you need a versioned contract.

## Where it comes from

The idea is not new. [CSLA .NET](https://cslanet.com) has moved business objects between tiers for over twenty years, and DTZero owes it the idea. The difference is that DTZero asks for no base class and works with any type, and everything it needs is generated at compile time.

It is used in production. An application from [revmedsystems.com](https://revmedsystems.com) is built on it: 74 factory types and 233 factory operations, with 3 hand-written controllers in the whole application (sign-in, PDFs and images).

There is a real cost to know about. Trimming a published WebAssembly client takes configuration, and trimming faults show up only in published builds. [IL Trimming](docs/trimming.md) covers it.

## Working with AI

A coding assistant trained on mainstream .NET will reach for a DTO and a controller. So the repository ships a skill, in `skills/DTZero`, that teaches an assistant the model: where a call crosses, who writes a child's row, what `[Remote]` does and does not mean. It is self-contained, so you can copy it into your own project. One model and one set of rules is also less for an assistant to keep consistent than four layers.

## Installation

**Server project:**
```bash
dotnet add package DTZero
dotnet add package DTZero.AspNetCore
```

**Client project and the shared domain library:**
```bash
dotnet add package DTZero
```

## Getting started

**Server (ASP.NET Core):** register DTZero with the assembly that holds your factories:

<!-- snippet: readme-server-setup -->
<a id='snippet-readme-server-setup'></a>
```cs
services.AddDTZeroAspNetCore(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L74-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-server-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

In the example set that call sits in `AddDesignServer()`, beside the server-only services the factory methods take as `[Service]` parameters. `Program` calls it and adds the endpoint to the pipeline:

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

app.MapFallbackToFile("index.html");

await app.RunAsync();
```
<sup><a href='/src/Design/Design.Server/Program.cs#L66-L91' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Client (Blazor WebAssembly):**

<!-- snippet: getting-started-client-program -->
<a id='snippet-getting-started-client-program'></a>
```cs
// The factory proxies: calls marked [Remote] go to the server
builder.Services.AddDTZero(DTZeroFactory.Remote, typeof(IOrder).Assembly);

// The HttpClient DTZero sends them with: the server that hosts this client
builder.Services.AddKeyedScoped(
    DTZeroServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L65-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

[Getting Started](docs/getting-started.md) has the complete walkthrough.

## Documentation

- [What Problem Does DTZero Solve?](docs/the-problem.md)
- [Getting Started](docs/getting-started.md)
- [Decision Guide](docs/decision-guide.md): when to use what
- [Factory Operations](docs/factory-operations.md): Create, Fetch, Insert, Update, Delete, Execute
- [Save Operation](docs/save-operation.md): how `Save` routes
- [Service Injection](docs/service-injection.md): the `[Service]` attribute
- [Authorization](docs/authorization.md)
- [Factory Events](docs/factory-events.md): handlers, dispatch phases and the client relay
- [Factory Modes](docs/factory-modes.md): Server, Remote and Logical
- [Serialization](docs/serialization.md)
- [IL Trimming](docs/trimming.md)
- [ASP.NET Core Integration](docs/aspnetcore-integration.md)
- [Attributes Reference](docs/attributes-reference.md)
- [Interfaces Reference](docs/interfaces-reference.md)

## Examples

The compiled, tested source of every sample on this page is `src/Design` (the Order domain). Complete applications are in `src/Examples/`:

- **Person**: a simple Blazor WebAssembly application
- **OrderEntry**: order entry with aggregate roots

## Supported frameworks

.NET 9.0 and .NET 10.0.

## License

MIT. See [LICENSE](LICENSE).

## Links

- [NuGet: DTZero](https://nuget.org/packages/DTZero)
- [NuGet: DTZero.AspNetCore](https://nuget.org/packages/DTZero.AspNetCore)
- [Release Notes](docs/release-notes/index.md)
