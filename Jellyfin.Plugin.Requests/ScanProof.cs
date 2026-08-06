using System;
using System.Diagnostics;
using System.Net.Http;
using System.Xml;

namespace Jellyfin.Plugin.Requests;

/// <summary>
/// Three planted findings, each with a source in front of the sink rather than a constant.
/// Deliberate, and not for merge.
/// </summary>
internal static class ScanProof
{
    /// <summary>
    /// Runs a shell command built out of an environment variable.
    /// </summary>
    /// <param name="name">The variable to take the command from.</param>
    /// <returns>The process.</returns>
    internal static Process? Run(string name) => Process.Start("sh", "-c " + Environment.GetEnvironmentVariable(name));

    /// <summary>
    /// Fetches a url built out of an environment variable.
    /// </summary>
    /// <param name="name">The variable to take the url from.</param>
    /// <returns>The response.</returns>
    internal static HttpResponseMessage Fetch(string name)
    {
        using var client = new HttpClient();
        return client.GetAsync(new Uri(Environment.GetEnvironmentVariable(name)!)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Reads an xml document with an external resolver left in place.
    /// </summary>
    /// <param name="xml">The document text.</param>
    /// <returns>The document.</returns>
    internal static XmlDocument Parse(string xml)
    {
        var document = new XmlDocument { XmlResolver = new XmlUrlResolver() };
        document.LoadXml(xml);
        return document;
    }
}
