using System;
using System.Text.Json.Serialization;

namespace DTZero.Internal;

/// <summary>
/// Bridges STJ's <see cref="ReferenceHandler"/> API to the existing
/// <see cref="DTZeroReferenceResolver.Current"/> AsyncLocal. This enables
/// STJ's built-in converters (for Dictionary, List, plain classes, etc.)
/// to participate in the same reference tracking used by Neatoo's custom
/// converters, preserving shared-instance identity across the full
/// serialization graph.
/// </summary>
/// <remarks>
/// <para>
/// STJ calls <see cref="CreateResolver"/> once at the start of each
/// top-level serialize/deserialize operation. By returning
/// <see cref="DTZeroReferenceResolver.Current"/>, both STJ built-in
/// converters and Neatoo custom converters share the same resolver
/// instance and ID counter.
/// </para>
/// <para>
/// The resolver lifecycle is managed by <see cref="DTZeroJsonSerializer"/>:
/// it creates a new <see cref="DTZeroReferenceResolver"/>, sets
/// <see cref="DTZeroReferenceResolver.Current"/>, calls
/// <c>JsonSerializer.Serialize/Deserialize</c>, then clears Current
/// in a finally block. This handler simply returns whatever Current
/// is at the time STJ calls <see cref="CreateResolver"/>.
/// </para>
/// </remarks>
internal sealed class DTZeroPreserveReferenceHandler : ReferenceHandler
{
	public override ReferenceResolver CreateResolver()
	{
		return DTZeroReferenceResolver.Current
			?? throw new InvalidOperationException(
				"DTZeroReferenceResolver.Current is null. " +
				"A DTZeroReferenceResolver must be created and set as Current before serialization. " +
				"This handler should only be used with DTZeroJsonSerializer, which manages the resolver lifecycle.");
	}
}
