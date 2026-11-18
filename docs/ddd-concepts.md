# DDD Concepts in DTZero

Where each concept lands, and why. The examples are files in `src/Design/Design.Domain`, the compiled and tested example set.

## Concepts DTZero carries

| Concept | With DTZero | Why it is this way | Example |
|---|---|---|---|
| Entity | A `[Factory]` class behind a public interface. `IFactorySaveMeta` gives `Save` its routing. Root and child are roles, and one class can hold both. | There is no root type or child type to declare. What an entity is in a given graph is decided by which of its factory methods that graph calls, and you write those. | `Entities/OrderLine.cs` holds both roles |
| Entity in the root role | A factory method the client calls: `[Remote] internal`, taking what the client has, such as an id. | The client asks the server for this object directly, so this is where a call crosses. One `Save` is one request and one transaction. | `Aggregates/Order.cs` |
| Entity in the child role | A factory method another factory method calls: `internal`, no `[Remote]`, taking what the parent already holds, such as its row. | The call is already on the server when the parent makes it, so there is no crossing to mark. `internal` also lets the trimmer remove the body from the client. | `Fetch(OrderLineRow row)` on `OrderLine` |
| Aggregate loaded and stored as a whole | Factory methods call factory methods. The root's `Fetch` calls the child list's `Fetch`, which calls each child's. The root's write calls the child list's `Save`, which calls each child's. | Each object reads and writes only its own row and hands the rest on, so no method knows the whole graph, and only the first call crosses. | `Order.Insert`, then `OrderLineList.Update`, then `OrderLine.Insert` |
| Child collection | A `[Factory]` list the root holds. It tracks removed items and saves each child through the child's factory. | Someone must decide insert, update or delete for each child. The list knows what was added and removed, so the root does not map its children's rows. | `OrderLineList` in `Entities/OrderLine.cs` |
| Value object | A `record`. No `[Factory]` unless building it needs a service or authorization. | An immutable value has no lifecycle, so it crosses as plain data. | `ValueObjects/Money.cs`, `ValueObjects/TaxRate.cs` |
| Factory | The generated `IXxxFactory`. Nothing with `[Factory]` is built with `new`. | The factory is where services are injected, authorization is checked and the call is routed to a tier. `new` would skip all three. | every Design type |
| Repository | Not supplied. An application interface taken as a `[Service]` parameter by an operation that runs on the server. | A repository is a server detail. If the client could call it, the persistence model would leak to the client and the aggregate would stop being the boundary. | `IOrderDbContext` in `Aggregates/OrderPersistence.cs` |
| Persistence model | Not supplied. The EF entities, or any other store shape, stay on the server. Each object maps itself to its row. | The graph that crosses is the conceptual graph. Keeping the store's shape separate is what lets the two differ. | `OrderRow` in `Aggregates/OrderPersistence.cs` |
| Domain service | An interface with an `internal` implementation. Server-only: a `[Service]`. Callable by the client: an interface factory, whose client side is a generated proxy. | Logic that belongs to no entity still has to be reachable. The interface is the contract on both tiers; only the implementation is server-only. | `Services/CustomerDirectory.cs` |
| Command | A static `[Factory]` class with `[Remote, Execute]`. A class-level static `[Execute]` when the result is that aggregate. | Some work is a verb with no object for the client to hold. | `Services/OrderCommands.cs` |
| Context | A plain `[Factory]` class a screen binds to. Its verbs act on the aggregate it holds, and it saves through that root. | A screen often needs more than one aggregate, loaded together and acted on over time. One fetch loads it, and no transfer type is invented for the screen. | `Contexts/OrderCheckout.cs` |
| Read model | A plain `[Factory]` class with `[Fetch]` only. | A list or a summary has no lifecycle and no rules. It should not be forced through an aggregate, and it still needs no DTO or controller. | `ReadModels/OrderSummary.cs` |
| Create-or-fetch | A class-level `[Remote, Execute]` that calls Fetch, then Create. | `[Create]` claims "new" and `[Fetch]` claims "stored". An operation that may return either is neither, so it is its own verb. | `OpenDraft` on `Order` |
| Domain event | `IFactoryEvents.Raise` inside an operation; a static `[FactoryEventHandler<T>]` method handles it, in a declared phase. | A side effect across aggregates should not be written into the aggregate that caused it, and whether it runs inside the transaction is a decision, so it is declared. | `FactoryPatterns/FactoryEvent*.cs` |
| Consistency boundary | One root `Save` is one transaction. Across roots: an `[Execute]` or an event. | The aggregate is the unit of consistency, so the save is the unit of work. | `Aggregates/Order.cs` |
| Authorization | `[AuthorizeFactory<T>]` or `[AspAuthorize]` on the factory. | One rule class, enforced on the server and asked by the client through `Can*`. | `Aggregates/AuthorizedOrder.cs` |
| Polymorphism | A base class with `[Factory]` subclasses; the wire carries the concrete type. | A model with subtypes must arrive as the subtype, or behavior is lost at the seam. | `Payments/Payment.cs` |
| Anti-corruption layer, public API | Not supplied. Hand-written endpoints that call the same factories on the server. | A contract for outsiders should be designed, not generated. The logic behind it is still the one copy. | [A Public API Over the Same Model](public-api.md) |

## Concepts that are not DTZero's job

| Concept | Where it lives |
|---|---|
| Invariants and validation rules | The class itself, or a library such as Neatoo. DTZero carries the object; it does not judge it. |
| Change tracking, the editing session | A library such as Neatoo. A plain class sets `IsNew` and `IsDeleted` itself. |
| Specification | The application. |
| Bounded context, context map | The application's assemblies. DTZero registers by assembly and takes no view on how many there are. |
| Ubiquitous language | The model. DTZero adds attributes, not names. |
| Unit of work | The application's store. DTZero says which operation owns the transaction, not how it is made. |

## Each component, and what it replaces

| Component | What you would write without it | Why it exists |
|---|---|---|
| `[Factory]` | A factory class, its interface and its registration, per type | Marks a type the generator builds a factory for. |
| `[Create]` | A constructor call plus service wiring | States "this object is not stored yet", so its first save inserts. |
| `[Fetch]` | A GET endpoint, a response type, a client call, mapping back | States "this object reflects the store"; `false` means there is none. |
| `[Insert]`, `[Update]`, `[Delete]` with `Save` | Three endpoints, their request types, and client code choosing between them | The object knows its own state, so the caller says `Save`. |
| `[Execute]` | An endpoint and its request and response types | Everything that is not one object's lifecycle: decide, orchestrate, compute. |
| `[Remote]` | The HTTP call, the endpoint and both serializations | Marks where the client's call crosses. Without it a method runs where it is called. |
| `[Service]` | Constructor injection of server services into a class the client also builds | Resolves a service on the tier that runs the method, so a server-only service never has to exist on the client. |
| Interface factory | A service interface, a client implementation over HTTP, a server implementation, an endpoint | The "implement the service twice" pattern, with the client half generated. |
| Static factory | A command endpoint and a client wrapper | A verb with no instance. |
| `[AuthorizeFactory<T>]`, `[AspAuthorize]` | `[Authorize]` on actions, record checks inside them, and the same logic again on the client | One rule on both tiers. |
| Factory events | A mediator library, its registration, and transaction-timing code | Side effects with a declared phase, in the caller's scope. |
| Lifecycle hooks | The same lines at the top and bottom of every operation | One place for what surrounds every operation on a type. |
| Ordinal serialization | DTOs | The object is the wire format, and the generator writes its reader and writer. |
| Factory modes | A second set of client-side stubs, or tests that need a web host | The same assembly behaves as a server, a client or one process, chosen at registration. |
| The `IsServerRuntime` switch | A separate client assembly without the persistence code | Lets the trimmer remove server-only method bodies from the published client. |

## Next steps

- [Client-Server Architecture](client-server-architecture.md)
- [Decision Guide](decision-guide.md)
- [Factory Operations](factory-operations.md)
