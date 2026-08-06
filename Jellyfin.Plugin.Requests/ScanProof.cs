using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace Jellyfin.Plugin.Requests;

/// <summary>
/// Four planted findings of four different shapes, so the C# leg of the scan can be seen finding
/// one of them. Deliberate, and not for merge.
/// </summary>
internal static class ScanProof
{
    /// <summary>
    /// A credential built from a password written into the source.
    /// </summary>
    /// <returns>The credential.</returns>
    internal static NetworkCredential Credential() => new NetworkCredential("verify", "hunter2");

    /// <summary>
    /// A file path taken from the environment and read without being checked.
    /// </summary>
    /// <param name="name">The variable to read the path out of.</param>
    /// <returns>The contents of that file.</returns>
    internal static string ReadFromEnvironment(string name) => File.ReadAllText(Environment.GetEnvironmentVariable(name)!);

    /// <summary>
    /// A key too short for the algorithm it is for.
    /// </summary>
    /// <returns>The key.</returns>
    internal static RSA WeakKey() => RSA.Create(1024);

    /// <summary>
    /// A token taken from a generator that is not for anything anybody has to guess.
    /// </summary>
    /// <returns>The token.</returns>
    internal static string Token() => new Random().Next().ToString(System.Globalization.CultureInfo.InvariantCulture);
}
