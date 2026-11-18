// =============================================================================
// DESIGN SOURCE OF TRUTH: A Write Rule That Reads the Store
// =============================================================================
//
// LockableOrderAuth takes the server's store by constructor and looks the order
// up by the target's id. The object the client sent supplies the id and nothing
// else the rule decides from.
//
// DESIGN DECISION: The id, not IsNew, decides whether there is a stored row
//
// IsNew crosses the wire like Status does. A client that marks a stored order
// new routes its Save to Insert, and the rule still finds the stored row by id.
//
// DESIGN DECISION: [Remote] internal on the auth method
//
// The store is server-only, so the auth class cannot be built on the client.
// The writes are [Remote], so Save runs this rule on the server whatever the
// declaration. The declaration decides the Can* methods:
// - public: CanSave(target) would run on the client and fail resolving the store;
// - internal: CanSave(target) gets the server-only guard; the client cannot ask;
// - [Remote] internal: CanSave(target) sends the object to the server, which
//   answers from the stored row. That is the one the client can ask.
//
// =============================================================================

using DTZero;

namespace Design.Domain.Aggregates;

#region authorization-stored-record
public interface ILockableOrderAuth
{
    // [Remote] internal: the store is server-only, and CanSave(target) asks the server
    [Remote]
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    internal Task<bool> CanWrite(ILockableOrder target);
}

public class LockableOrderAuth(ILockableOrderStore store) : ILockableOrderAuth
{
    // The target supplies the id; the stored row supplies the status.
    public async Task<bool> CanWrite(ILockableOrder target)
        => await store.FindAsync(target.Id) is not { Status: LockableOrderStatus.Locked };
}
#endregion
