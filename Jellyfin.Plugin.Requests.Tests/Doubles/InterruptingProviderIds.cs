using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Requests.Tests.Doubles;

/// <summary>
/// A request's provider identifiers that cannot be read. Put on one request in a store that already
/// holds others, it stops a write in the middle of the bytes rather than before them: the records
/// ahead of it are already in the stream and on the disk when it throws.
/// <para>
/// This is what makes the interruption real rather than reconstructed. The alternative is to write
/// a half file by hand and assert the store copes with it, which proves the loader and says nothing
/// about whether a write can actually be stopped where the proof assumes it can.
/// </para>
/// </summary>
internal sealed class InterruptingProviderIds : IReadOnlyDictionary<string, string>
{
    /// <inheritdoc />
    public IEnumerable<string> Keys => throw new WriteInterruptedException();

    /// <inheritdoc />
    public IEnumerable<string> Values => throw new WriteInterruptedException();

    /// <summary>
    /// Gets a count that reads as an ordinary one, so nothing decides to skip this property before
    /// reaching the point where the interruption happens.
    /// </summary>
    public int Count => 1;

    /// <inheritdoc />
    public string this[string key] => throw new WriteInterruptedException();

    /// <inheritdoc />
    public bool ContainsKey(string key) => throw new WriteInterruptedException();

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1065:Do not raise exceptions in unexpected locations",
        Justification = "Throwing here is the whole point: this double exists to stop a write in the middle of serialising a record.")]
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new WriteInterruptedException();

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => throw new WriteInterruptedException();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => throw new WriteInterruptedException();
}
