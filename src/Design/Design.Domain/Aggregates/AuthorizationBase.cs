// =============================================================================
// DESIGN SOURCE OF TRUTH: A Shared Authorization Base
// =============================================================================
//
// The broad read and write checks are declared once, here. Each type's auth
// class inherits this base and adds only the checks of its own; it restates
// neither broad check. QuoteAuth (QuoteAuth.cs) is the example.
//
// DESIGN DECISION: An abstract class, taking the user by constructor
//
// The base takes ICurrentUser by constructor, like every Design auth class, and
// exposes it to derived classes. It is abstract: it is never an auth type on
// its own, and it is never registered. The generator registers only the auth
// type a factory names, and resolves every check, inherited ones included,
// through that type.
//
// DESIGN DECISION: The factory names the derived CLASS
//
// AuthorizedOrder, ParamAuthOrder and LockableOrder name an auth interface in
// [AuthorizeFactory<T>]. A check declared on a class is enforced only when the
// factory names that class or a class derived from it: an interface's base
// types are interfaces, so checks on this base would be out of reach of an
// interface the factory named. Quote therefore names QuoteAuth.
//
// DESIGN DECISION: The checks are public and read only ICurrentUser
//
// Both tiers register ICurrentUser, so the generated Can* methods run these
// checks on the client with no server call. The server runs them again for
// Quote's [Remote] Fetch and Save, so a client cannot skip them.
//
// =============================================================================

using Design.Domain.Services;
using DTZero;

namespace Design.Domain.Aggregates;

/// <summary>
/// The broad checks every auth class inherits: signed in to read, Sales to write.
/// </summary>
#region authorization-base
public abstract class AuthorizationBase(ICurrentUser user)
{
    protected ICurrentUser User { get; } = user;

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool IsSignedIn() => User.IsAuthenticated;

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite() => User.IsInRole(OrderRoles.Sales);
}
#endregion
