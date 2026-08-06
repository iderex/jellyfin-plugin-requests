using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Jellyfin.Plugin.Requests;

/// <summary>
/// One planted finding, so the C# leg of the scan can be seen finding one.
/// Deliberate, and not for merge.
/// </summary>
internal static class ScanProof
{
    /// <summary>
    /// A credential written into the source, which is what the query is about.
    /// </summary>
    private const string Password = "hunter2-not-a-real-password";

    /// <summary>
    /// Uses the credential, so it is not merely an unread constant.
    /// </summary>
    /// <param name="client">The client to authenticate.</param>
    internal static void Authenticate(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var pair = Convert.ToBase64String(Encoding.UTF8.GetBytes("verify:" + Password));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pair);
    }
}
