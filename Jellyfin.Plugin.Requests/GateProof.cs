namespace Jellyfin.Plugin.Requests;

/// <summary>
/// One warning, so the gate can be seen refusing one. Deliberate, and not merged.
/// </summary>
internal static class GateProof
{
    /// <summary>
    /// Declares a local nobody reads, which is CS0219.
    /// </summary>
    internal static void Unused()
    {
        int neverRead = 1;
    }
}
