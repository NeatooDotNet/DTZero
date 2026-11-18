// =============================================================================
// DESIGN SOURCE OF TRUTH: An Auth Class That Inherits the Broad Checks
// =============================================================================
//
// QuoteAuth inherits IsSignedIn (Read) and CanWrite (Write) from
// AuthorizationBase and restates neither. Its one check of its own: only a
// Manager deletes a quote. Delete is a Write, so it runs CanWrite from the base
// and CanDelete from here; both must pass.
//
// What runs for each Quote operation:
// - Create, Fetch: IsSignedIn
// - Insert, Update: CanWrite
// - Delete:         CanWrite + CanDelete
// - CanSave:        CanWrite + CanDelete (the distinct set across the writes)
//
// =============================================================================

using Design.Domain.Services;
using DTZero;

namespace Design.Domain.Aggregates;

/// <summary>
/// Quote's rules: the base's broad checks, and Manager to delete.
/// </summary>
#region authorization-base-derived
public class QuoteAuth(ICurrentUser user) : AuthorizationBase(user)
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    public bool CanDelete() => User.IsInRole(OrderRoles.Manager);
}
#endregion
