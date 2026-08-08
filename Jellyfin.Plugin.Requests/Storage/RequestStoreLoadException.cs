using System;
using System.Globalization;

namespace Jellyfin.Plugin.Requests.Storage;

/// <summary>
/// The persisted requests could not be read, so the store refuses to open rather than reporting
/// whatever part of the file it managed to parse.
/// <para>
/// The alternative is the failure this type exists against. A loader that swallows a parse error and
/// returns the records it read before it stopped hands the plugin a queue that is quietly shorter
/// than the one on disk, and the first write afterwards persists that shorter queue over the longer
/// one. Nobody sees a failure and the requests are gone. Refusing is loud, keeps the file, and
/// leaves an operator something to look at.
/// </para>
/// <para>
/// This says the bytes could not be read as a set of requests. It does not say they are the bytes
/// this store wrote: a value changed inside a string is still well-formed and this store carries
/// nothing that would notice.
/// </para>
/// </summary>
public class RequestStoreLoadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestStoreLoadException"/> class.
    /// </summary>
    /// <param name="filePath">The file that could not be read.</param>
    /// <param name="detail">What is wrong with it, in a sentence an operator can act on.</param>
    public RequestStoreLoadException(string filePath, string detail)
        : base(Describe(filePath, detail))
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestStoreLoadException"/> class.
    /// </summary>
    /// <param name="filePath">The file that could not be read.</param>
    /// <param name="detail">What is wrong with it, in a sentence an operator can act on.</param>
    /// <param name="innerException">What the reader threw.</param>
    public RequestStoreLoadException(string filePath, string detail, Exception innerException)
        : base(Describe(filePath, detail), innerException)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestStoreLoadException"/> class.
    /// </summary>
    public RequestStoreLoadException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestStoreLoadException"/> class.
    /// </summary>
    /// <param name="message">What happened.</param>
    public RequestStoreLoadException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestStoreLoadException"/> class.
    /// </summary>
    /// <param name="message">What happened.</param>
    /// <param name="innerException">What it happened because of.</param>
    public RequestStoreLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the file that could not be read, or <see langword="null"/> where this was constructed
    /// without one.
    /// </summary>
    public string? FilePath { get; }

    private static string Describe(string filePath, string detail)
        => string.Format(
            CultureInfo.InvariantCulture,
            "The stored requests in {0} could not be read: {1} Nothing has been changed on disk.",
            filePath,
            detail);
}
