using System;
using System.Net;

namespace Jellyfin.Plugin.Requests;

/// <summary>
/// One planted finding, so the C# leg of the scan can be seen finding one.
/// Deliberate, and not for merge.
/// </summary>
internal static class ScanProof
{
    /// <summary>
    /// Builds a credential from a password written into the source, which is what the query is
    /// about.
    /// </summary>
    /// <returns>The credential.</returns>
    internal static NetworkCredential Credential()
    {
        return new NetworkCredential("verify", "hunter2");
    }
}
