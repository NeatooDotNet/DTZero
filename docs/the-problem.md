# Why DTZero

## The problem is older than the tools

A domain model could not run in a JavaScript client. So for a generation of web applications the server flattened its objects to JSON and the client rebuilt something like them in another language. That is where the layers came from:

- **DTOs**, to carry state across
- **API controllers**, to expose each operation as an endpoint
- **Mapping code**, between the DTOs and the domain objects
- **Client-side copies of the rules**, because the screen needs them too

Every property exists in several places. A new field is a change in three or four files. The rule that decides whether an order can be submitted is written on the server, and again, slightly differently, in the client.

None of this was a design choice. It was a workaround for a language boundary. With Blazor WebAssembly the boundary is gone: the assembly that defines `Order` loads in the browser and on the server. WPF and other .NET clients never had the boundary. The layers are still built, out of habit.

## What DTZero does instead

You write the domain model once, in a library both tiers load. DTZero is a Roslyn source generator that reads the attributes on your classes and generates what goes between them and the wire: a factory per type, the serialization, and one endpoint for every call.

The client calls the factory:

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

The operation is a method on the domain class. `[Remote]` marks it as an entry point from the client; its `[Service]` parameters are resolved on the server, so the store is something the client never sees:

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

`Save` picks Insert, Update or Delete from the object's own state. The compiler checks the call on both sides, because both sides are the same code.

## The graph that crosses is the model, not the tables

The objects DTZero carries are your conceptual model: the aggregate as the domain describes it. They are not your storage model. The EF entities, or whatever shape your store has, stay on the server, behind a `[Service]` parameter. Each object maps itself to its own row, and the two shapes are free to differ.

So DTZero does not put Entity Framework on the client, and it does not ask your entities to match your tables. It supplies no repository and no persistence model. Those remain yours, and remain on the server.

## The one idea: factory methods call factory methods

The client calls one factory method. That call crosses. From there, factory methods call factory methods on the server, and nothing else crosses.

```text
client                      server
------                      ------
orderFactory.Save(order) -> Order.Insert                 writes the order's row
                              lineListFactory.Save       walks the lines
                                lineFactory.Save         each line writes its own row

orderFactory.Fetch(id)   -> Order.Fetch                  reads the order's row
                              lineListFactory.Fetch      walks the line rows
                                lineFactory.Fetch(row)   each line reads its own row
```

In other designs, loading and storing an aggregate as a whole is the repository's work, and one class has to know the entire graph. Here each object knows only its own row and hands the rest on. An ORM would call the save half of this a cascade; here it is plain method calls that you can read and step through.

Root and child follow from the same idea. They are roles, not kinds of class. An entity is a root where the client calls its factory method, and a child where another factory method does. The parameters of the method are the whole difference, and one class can have both. [Client-Server Architecture](client-server-architecture.md) shows an entity in both roles.

## What about callers you do not control?

Your own client ships with your server and is built from the same source, so it needs no separate contract. An outside caller does. For that you still design an API by hand, and its endpoints call the same factories and the same domain methods. The contract is designed; the logic exists once. See [A Public API Over the Same Model](public-api.md).

## Who it is for

Teams building an application with a .NET client, where the client and the server are built and deployed together, and the domain has behavior worth keeping in one place. It is used in production with Blazor WebAssembly and with WPF.

It works with any class: an aggregate, a read model, a static command, a record. No base class is required. [DDD Concepts in DTZero](ddd-concepts.md) shows where each concept lands.

## When not to use it

- **Your only client is not .NET.** It cannot load your assembly. Design an HTTP API.
- **Blazor Server only, with no plan for a client tier.** There is no seam to remove.
- **Thin CRUD.** Forms over tables with no behavior gain little from a domain model on the client.
- **Client and server are versioned and deployed apart.** DTZero assumes both tiers come from the same source. If old clients must work against new servers for long periods, you need a versioned contract.

## Coming from CSLA?

DTZero owes the idea to [CSLA .NET](https://cslanet.com), which has moved business objects between tiers for over twenty years. The operations will look familiar. The difference is that DTZero asks for no base class and works with any type, with the factories and serialization generated at compile time.

## Working with an AI assistant

An assistant trained on mainstream .NET reaches for a DTO and a controller, and will do so in a DTZero project unless told otherwise. The repository ships a skill, `skills/DTZero`, that teaches the model: where a call crosses, who writes a child's row, what `[Remote]` means. It is self-contained and can be copied into your own project. It is part of the library, kept in step with the same compiled samples as these pages.

## Next steps

- [Getting Started](getting-started.md): build the first application
- [Client-Server Architecture](client-server-architecture.md): where calls cross, and where they do not
- [DDD Concepts in DTZero](ddd-concepts.md): each concept and where it lands
- [Decision Guide](decision-guide.md): which piece to use when
