# DDD Concepts and DTZero Components

**Plan:** PITCH-002 · **Written:** 2026-11-13 by Claude · **Confirmation:** given 2026-11-13 with two corrections (section 4)

This is the working map later plans write from. It starts from `docs/VisionAlignment.md` section 3, with every "Compiled example" cell re-checked against `src/Design` as it stands today, and adds the column that table lacks: the reason.

The reader is assumed to know DDD. Nothing here explains a concept.

## 1. Each DDD concept, with DTZero

| DDD concept | With DTZero | Why it is this way | Design example |
|---|---|---|---|
| Entity | A `[Factory]` class behind a public interface. `IFactorySaveMeta` gives `Save` its routing. Root and child are not kinds of class. They are roles, and one class can hold both. | DTZero has no root type and no child type to declare. What an entity is in a given graph is decided by which of its factory methods that graph calls, and the author writes those. | `Entities/OrderLine.cs` holds both roles |
| Entity in the root role | A factory method the client calls: `[Remote] internal`, taking what the client has, such as an id. | The client asks the server for this object directly, so this is where a call crosses. One `Save` is one request and one transaction. | `Aggregates/Order.cs`; `Fetch(Guid id)` on `OrderLine` |
| Entity in the child role | A factory method another factory method calls: `internal`, no `[Remote]`, taking what the parent already holds, such as its row. | The call is already on the server when the parent makes it, so there is no crossing to mark. `internal` also lets the trimmer remove the body from the client. | `Fetch(OrderLineRow row)` on `OrderLine` |
| Aggregate loaded and stored as a whole | Factory methods call factory methods. The root's `Fetch` calls the child list's `Fetch`, which calls each child's. The root's `Insert` or `Update` calls the child list's `Save`, which routes each child to its own `Insert` or `Update`. In Design the list removes the row of a deleted line itself. | DDD gives this job to the repository and does not name the mechanism. Here each object reads and writes only its own row and hands the rest to the next factory, so no method knows the whole graph, and only the first call crosses. | `Order.Insert` → `OrderLineList.Update` → `OrderLine.Insert` |
| Child collection | A `[Factory]` list the root holds. It tracks removed items and runs the save cascade. | Someone must decide insert, update or delete for each child. The list knows what was added and removed, so the root does not map its children's rows. | `OrderLineList` |
| Value object | A `record`. No `[Factory]` unless its construction needs a service or authorization. | An immutable value needs no lifecycle, so it crosses as plain data. A factory appears only when building it needs the server. | `ValueObjects/Money.cs` (plain), `ValueObjects/TaxRate.cs` (factory) |
| Factory | The generated `IXxxFactory`. Nothing with `[Factory]` is built with `new`. | The factory is where services are injected, authorization is checked and the call is routed to a tier. `new` would skip all three. | every Design type |
| Repository | Not supplied. An application interface taken as a `[Service]` parameter by a server-run operation. Never an interface factory. | A repository is a server detail. If the client could call it, the persistence model would leak to the client and the aggregate would stop being the boundary. | `IOrderDbContext` in `Aggregates/OrderPersistence.cs` |
| Persistence model | Not supplied. The EF entities, or any other store shape, stay on the server. The object maps itself to its row. | The graph that crosses is the conceptual graph. Keeping the store's shape separate is what lets the two differ. | `OrderRow` in `Aggregates/OrderPersistence.cs` |
| Domain service | An interface with an `internal` implementation. Server-only: a `[Service]`. Callable by the client: an interface factory, whose client side is a generated proxy. | Logic that belongs to no entity still has to be reachable. The interface is the contract on both tiers; only the implementation is server-only. | `Services/CustomerDirectory.cs`, `IProductCatalog` |
| Command or use case | A static `[Factory]` class with `[Remote, Execute]`. A class-level static `[Execute]` when the result is that aggregate. | Some work is a verb with no object for the client to hold. A static command is a call that returns and is done. | `Services/OrderCommands.cs`, `FactoryPatterns/ClassFactoryWithExecute.cs` |
| Context | A plain `[Factory]` class a screen binds to. Its verbs are instance methods that act on the aggregate it holds, and it saves through that root. | A screen often needs more than one aggregate, loaded together and acted on over time. One fetch loads it; no transfer type is invented for the screen. | `Contexts/OrderCheckout.cs` |
| Read model | A plain `[Factory]` class with `[Fetch]` only. | A list or a summary has no lifecycle and no rules. It should not be forced through an aggregate, and it still needs no DTO or controller. | `ReadModels/OrderSummary.cs` |
| Create-or-fetch seam | A class-level `[Remote, Execute]` that calls Fetch, then Create. | `[Create]` claims "new" and `[Fetch]` claims "stored". An operation that may return either is neither, so it is its own verb. | `OpenDraft` on `Order` |
| Domain event | `IFactoryEvents.Raise` inside an operation; a static `[FactoryEventHandler<T>]` method handles it. The phase is Immediate, AfterFlush or AfterCommit. Events can relay to the client. | A side effect across aggregates should not be written into the aggregate that caused it, and whether it runs inside the transaction is a decision, so it is declared. | `FactoryPatterns/FactoryEvent*.cs` |
| Consistency boundary | One root `Save` is one transaction. Across roots: an `[Execute]` seam or an event. | The aggregate is the unit of consistency, so the save is the unit of work. | `Aggregates/Order.cs` |
| Authorization | `[AuthorizeFactory<T>]` or `[AspAuthorize]` on the factory. | One rule class, enforced on the server and asked by the client through `Can*`. No entry point can miss it. | `Aggregates/AuthorizedOrder.cs`, `ParamAuthOrder.cs` |
| Polymorphism in the model | A base class with `[Factory]` subclasses; the wire carries the concrete type. | A model with subtypes must arrive as the subtype, or behaviour is lost at the seam. | `Payments/Payment.cs` |
| Anti-corruption layer, public API | Not supplied. Hand-written endpoints that call the same factories on the server. | A contract for outsiders should be designed, not generated. The logic behind it is still the one copy. | none yet (PITCH-003) |

## 2. Concepts that are not DTZero's job

| DDD concept | Where it lives |
|---|---|
| Invariants and validation rules | The class itself, or Neatoo. DTZero carries the object; it does not judge it. |
| Change tracking, the editing session | Neatoo. A plain class sets `IsNew` and `IsDeleted` itself. |
| Specification | The application. |
| Bounded context, context map | The application's assemblies. DTZero registers by assembly and takes no view on how many there are. |
| Ubiquitous language | The model. DTZero adds attributes, not names. |
| Unit of work | The application's store. DTZero says which operation owns the transaction, not how it is made. |

## 3. Each DTZero component, and why it exists

| Component | What you would write without it | Why it exists |
|---|---|---|
| `[Factory]` | A factory class, its interface and its registration, per type | Marks a type the generator builds a factory for. |
| `[Create]` | A constructor call plus service wiring | States "this object is not stored yet", so its first save inserts. |
| `[Fetch]` | A GET endpoint, a response type, a client call, mapping back | States "this object reflects the store"; `false` means there is none. |
| `[Insert]`, `[Update]`, `[Delete]` with `Save` | Three endpoints, their request types, and client code choosing between them | The object knows its own state, so the caller says `Save` and the routing is not the caller's problem. |
| `[Execute]` | An endpoint and its request and response types | Everything that is not one object's lifecycle: decide, orchestrate, compute. |
| `[Remote]` | The HTTP call, the endpoint and both serializations | Marks where the client's call crosses. Without it a method runs where it is called. |
| `[Service]` | Constructor injection of server services into a class the client also builds | Resolves a service on the tier that runs the method, so a server-only service never has to exist on the client. |
| Interface factory | A service interface, a client implementation over HTTP, a server implementation, an endpoint | Microsoft's "implement the service twice" pattern, with the client half generated. |
| Static factory | A command endpoint and a client wrapper | A verb with no instance. |
| `[AuthorizeFactory<T>]`, `[AspAuthorize]` | `[Authorize]` on actions, record checks inside them, and the same logic again on the client | One rule on both tiers. |
| Factory events | A mediator library, its registration, and transaction-timing code | Side effects with a declared phase, in the caller's scope. |
| Lifecycle hooks (`IFactoryOnStart`, `OnComplete`, `OnCancelled`) | The same lines at the top and bottom of every operation | One place for what surrounds every operation on a type. |
| Ordinal serialization | DTOs | The object is the wire format; the generator writes the reader and writer, so no reflection is needed and trimming works. |
| Factory modes (Server, Remote, Logical) | A second set of client-side stubs, or tests that need a web host | The same assembly behaves as a server, a client or one process, chosen at registration. |
| The `IsServerRuntime` switch | A separate client assembly without the persistence code | Lets the trimmer remove server-only method bodies from the published client. |

## 4. For the user to confirm

1. Are the reasons in sections 1 and 3 the reasons you would give? Correct any by row.
2. Is "Context" a word the public docs should use, and is "the screen needs more than one aggregate" the right reason for it?
3. Is section 2 complete, and is anything listed there that DTZero does in fact take a view on?

### Confirmation

**2026-11-13, Keith:** "It's all very good." Two corrections, both applied to section 1:

- Root and child are not a clear-cut pair of definitions. One entity can be either, and the difference is the factory method's parameters, which the author chooses. The two rows became three: the entity, and its two roles.
- The save cascade needed its own row and a statement of the idea behind it: factory methods call factory methods.

Questions 2 and 3 were not answered separately; "all very good" is taken to cover them. The wording of the two changed rows is still open until Keith has read it.
