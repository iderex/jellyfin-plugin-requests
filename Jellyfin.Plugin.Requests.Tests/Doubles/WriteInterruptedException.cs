using System;

namespace Jellyfin.Plugin.Requests.Tests.Doubles;

/// <summary>
/// Thrown from inside a request while it is being serialised, so a write stops in the middle of
/// putting bytes on a disk. It stands for the server being killed at that moment, and it is thrown
/// by <see cref="InterruptingProviderIds"/> rather than by anything the store can see coming.
/// </summary>
internal sealed class WriteInterruptedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteInterruptedException"/> class.
    /// </summary>
    public WriteInterruptedException()
        : base("The write was interrupted part way through the record being serialised.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteInterruptedException"/> class.
    /// </summary>
    /// <param name="message">What happened.</param>
    public WriteInterruptedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteInterruptedException"/> class.
    /// </summary>
    /// <param name="message">What happened.</param>
    /// <param name="innerException">What it happened because of.</param>
    public WriteInterruptedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
