using Jellyfin.Plugin.Requests.Model;

namespace Jellyfin.Plugin.Requests.Storage;

/// <summary>
/// One entry as it is written to disk: the request, and the revision the store held it at.
/// <para>
/// It exists so that what is serialised is a shape this file controls rather than whatever
/// <see cref="StoredRequest"/> happens to look like. The two carry the same two values today, and a
/// change to the public type is not automatically a change to the bytes on somebody's disk.
/// </para>
/// <para>
/// This is the whole of the on-disk shape as #46 needed it, and no more. Giving it a version field
/// and stating what may be changed about it without stranding an existing install is #47.
/// </para>
/// </summary>
internal sealed record PersistedRequest
{
    /// <summary>
    /// Gets the revision the store held this request at.
    /// </summary>
    public long Revision { get; init; }

    /// <summary>
    /// Gets the request. Null only in a file that has been damaged, which the loader refuses rather
    /// than reads around.
    /// </summary>
    public MediaRequest? Request { get; init; }
}
