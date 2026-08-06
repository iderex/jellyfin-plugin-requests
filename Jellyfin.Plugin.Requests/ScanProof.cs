using System.IO;

namespace Jellyfin.Plugin.Requests;

/// <summary>
/// One planted finding, copied byte for byte in shape from a call the scan already reports in the
/// test project, so that a result here separates a leg that sees this project from a leg that does
/// not. Deliberate, and not for merge.
/// </summary>
internal static class ScanProof
{
    /// <summary>
    /// Joins a name onto a rooted path, which is the shape the scan reports elsewhere in this tree.
    /// </summary>
    /// <param name="name">The name to join.</param>
    /// <returns>The joined path.</returns>
    internal static string Under(string name) => Path.Combine(Path.GetTempPath(), name);
}
