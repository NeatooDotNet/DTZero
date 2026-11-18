// =============================================================================
// DESIGN SOURCE OF TRUTH: A Public API Over the Same Domain Graph
// =============================================================================
//
// /api/dtzero is the application's endpoint: its client is the application's own
// .NET client, holding the same domain library. A caller outside the application
// (a partner, a script, another language) needs a contract it can rely on, and
// that contract is designed by hand. What it does not need is a second copy of
// the logic behind it.
//
// DESIGN DECISION: Hand-written endpoints that call the generated factory
//
// The endpoints below take IOrderFactory by DI, exactly as any server code does.
// On the server the factory runs locally: Fetch loads the order, the order's own
// verb changes it, and Save writes it through the order's own Insert/Update. The
// endpoint is the same three steps as OrderCommands.SubmitOrder, the command the
// application's client sends -- one Order, one Submit, one CanSubmit.
//
// DESIGN DECISION: The contract is a designed type, not the domain object
//
// PublicOrder is the shape outsiders get: what an outsider needs to read, named
// for them. Status is the enum's name, so renumbering OrderStatus does not move
// the contract. Lines are a count; a partner who needs lines gets an endpoint
// designed for that. The contract versions on its own schedule; the domain
// changes on the application's.
//
// DID NOT DO THIS: Return IOrder to outsiders
//
// IOrder carries IsNew, IsDeleted and the line list's object graph, and it
// changes whenever the application does. Serialized by the web framework, any
// change to the domain would change the public contract with it.
//
// DID NOT DO THIS: Expose /api/dtzero as the public API
//
// Its requests are the generated factories' calls in DTZero's own wire format,
// shaped for a client that has the domain library loaded. It is not a contract
// anyone else can call, and it is not meant to be one.
//
// DID NOT DO THIS: Copy the submit rule into the endpoint
//
// "A draft with at least one line" lives in Order.CanSubmit. The endpoint asks
// the order and turns its answer into a 409; it never checks the status or the
// lines itself, so the rule cannot drift between the application and the API.
//
// DID NOT DO THIS: A route constraint on the id ("/{id:int}")
//
// A request the constraint rejects matches no endpoint, so Program.cs's
// MapFallbackToFile answers it with the Blazor client's index.html and a 200.
// Unconstrained, "/orders/abc" reaches the endpoint, fails to bind, and the
// caller gets a 400.
//
// Authentication, rate limiting and versioning are the application's, as on any
// endpoint it maps; this sample adds none.
//
// =============================================================================

using Design.Domain.Aggregates;

namespace Design.Server.PublicApi;

/// <summary>
/// The public order endpoints: hand-written, over the generated <see cref="IOrderFactory"/>.
/// </summary>
public static class PublicOrderApi
{
    /// <summary>
    /// Maps GET /api/public/orders/{id} and POST /api/public/orders/{id}/submit.
    /// </summary>
    #region public-api-endpoints
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
    #endregion
}

/// <summary>
/// An order as the public API presents it.
/// </summary>
#region public-api-contract
public sealed record PublicOrder(int Id, string Customer, string Status, decimal Total, string Currency, int LineCount)
{
    public static PublicOrder From(IOrder order) => new(
        order.Id, order.CustomerName, order.Status.ToString(),
        order.Total.Amount, order.Total.Currency, order.Lines.Count);
}
#endregion

/// <summary>
/// The public endpoint over AuthorizedOrder, an aggregate with [AuthorizeFactory&lt;T&gt;].
/// </summary>
/// <remarks>
/// DESIGN DECISION: The factory's rules refuse here, as on /api/dtzero
///
/// The endpoint takes IAuthorizedOrderFactory, so Fetch runs AuthorizedOrderAuth over
/// the request's ICurrentUser, as the application's own client's Fetch does. A
/// refused Fetch returns null, the same as a missing order. To answer a refusal as
/// 403 rather than 404, the endpoint asks CanFetch first: the same rules Fetch runs,
/// asked of the factory, not restated here. Fetch still checks them.
///
/// DESIGN DECISION: 403 for every refusal, anonymous callers included
///
/// The factory says that the caller is refused, not which rule refused, so the
/// endpoint does not split "not signed in" (401) from "not allowed" (403): that
/// would restate HasAccess. Results.Problem, not Results.Forbid: Forbid runs the
/// application's authentication scheme, which Design.Server leaves to the application.
/// </remarks>
public static class PublicAuthorizedOrderApi
{
    /// <summary>
    /// Maps GET /api/public/authorized-orders/{id}.
    /// </summary>
    #region public-api-authorized
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
    #endregion
}

/// <summary>
/// An AuthorizedOrder as the public API presents it.
/// </summary>
public sealed record PublicAuthorizedOrder(int Id, string Customer, decimal Total)
{
    public static PublicAuthorizedOrder From(IAuthorizedOrder order) => new(order.Id, order.CustomerName, order.Total);
}
