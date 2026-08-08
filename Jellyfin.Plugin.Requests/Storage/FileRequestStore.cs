using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Requests.Model;

namespace Jellyfin.Plugin.Requests.Storage;

/// <summary>
/// The store this plugin ships: every request in one file in the plugin's own data directory,
/// serialised with the JSON support the runtime already provides. The medium and the two that were
/// rejected are in <c>docs/storage.md</c>.
/// <para>
/// <b>What an interruption may cost.</b> A server is stopped, a container is killed, a disk fills.
/// After any of those the store loads, and at most the one record being written is lost. That is a
/// property of how a write is made rather than of how carefully the caller shuts down: nothing is
/// ever written into the file the loader reads. A write serialises the whole set into
/// <see cref="PendingFileName"/>, flushes it to the disk, and only then replaces
/// <see cref="FileName"/> with it in one step. So the file the loader reads is a file some write
/// finished, and a process that dies at any moment leaves either the set before the write or the
/// set after it.
/// </para>
/// <para>
/// Two things that step rests on are the platform's and not this file's, and neither is measured
/// here. The replace is atomic because the runtime maps it onto the operating system's own rename,
/// and a rename is not durable until the directory entry reaches the disk, for which the runtime
/// offers no call. What that leaves is a window in which a completed write can be lost by a power
/// cut, which is the ordinary bound for this medium. It does not widen what an interruption can
/// cost: whichever of the two sets survives, it is a whole one.
/// </para>
/// <para>
/// <b>A leftover pending file is ignored, never read.</b> It is the wreckage of an interrupted
/// write, it is the one file that can be half a document, and the loader does not look at it. It is
/// not deleted on load either, because loading is a read: the next write truncates it.
/// </para>
/// <para>
/// <b>What is not survivable.</b> If the file the loader reads cannot be parsed as a set of
/// requests, the store refuses to open with <see cref="RequestStoreLoadException"/> rather than
/// returning the part of it that did parse. A short read that looks like a successful one is how a
/// queue silently loses records and then has the loss written back over the file that still held
/// them.
/// </para>
/// <para>
/// <b>Reads do not wait for writes.</b> The set is held in memory as one value that is replaced
/// whole, so a reader takes the current one and never blocks and never sees half of a change. It is
/// replaced only after the disk has taken the same set, so what the plugin reports and what survives
/// a restart cannot disagree.
/// </para>
/// </summary>
public sealed class FileRequestStore : IRequestStore, IDisposable
{
    /// <summary>
    /// The file the loader reads. It is only ever created by replacing it whole.
    /// </summary>
    public const string FileName = "requests.json";

    /// <summary>
    /// The file a write is built in before it replaces <see cref="FileName"/>. This is the only file
    /// that can be found half written, and nothing ever reads it.
    /// </summary>
    public const string PendingFileName = "requests.json.writing";

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false
    };

    private readonly string _directoryPath;
    private readonly string _filePath;
    private readonly string _pendingFilePath;

    /// <summary>
    /// Held for the whole of a write, so no two writes are building the pending file at once and no
    /// write decides against a set another write has already replaced.
    /// </summary>
    private readonly SemaphoreSlim _writes = new SemaphoreSlim(1, 1);

    /// <summary>
    /// What the store holds, or null before the first call has read the file. Replaced whole and
    /// never edited, which is what lets a reader take it without waiting for anything.
    /// </summary>
    private Dictionary<Guid, StoredRequest>? _held;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileRequestStore"/> class.
    /// </summary>
    /// <param name="directoryPath">
    /// The directory the file lives in. Nothing is created until the first write, so constructing a
    /// store against a directory that does not exist yet is not an error.
    /// </param>
    public FileRequestStore(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);

        _directoryPath = directoryPath;
        _filePath = Path.Combine(directoryPath, FileName);
        _pendingFilePath = Path.Combine(directoryPath, PendingFileName);
    }

    /// <summary>
    /// Gets the file the requests are kept in.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Gets the file a write is built in before it replaces <see cref="FilePath"/>.
    /// </summary>
    public string PendingFilePath => _pendingFilePath;

    /// <inheritdoc />
    public async Task<StoredRequest?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var held = await HeldAsync(cancellationToken).ConfigureAwait(false);
        return held.TryGetValue(id, out var stored) ? stored : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoredRequest>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var held = await HeldAsync(cancellationToken).ConfigureAwait(false);
        return held.Values.ToArray();
    }

    /// <inheritdoc />
    public async Task<StoredRequest> AddAsync(MediaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        await _writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var held = LoadedWhileHoldingTheWriteLock();

            if (held.ContainsKey(request.Id))
            {
                throw new DuplicateRequestException(request.Id);
            }

            var stored = new StoredRequest(request, 1);
            var next = new Dictionary<Guid, StoredRequest>(held) { [request.Id] = stored };

            await PersistAsync(next, cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _held, next);
            return stored;
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <inheritdoc />
    public async Task<StoredRequest> ReplaceAsync(MediaRequest request, long expectedRevision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        await _writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var held = LoadedWhileHoldingTheWriteLock();
            var current = held.TryGetValue(request.Id, out var stored) ? stored : (StoredRequest?)null;

            if (current is null || current.Value.Revision != expectedRevision)
            {
                throw new RequestConcurrencyException(request.Id, expectedRevision, current);
            }

            var written = new StoredRequest(request, expectedRevision + 1);
            var next = new Dictionary<Guid, StoredRequest>(held) { [request.Id] = written };

            await PersistAsync(next, cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _held, next);
            return written;
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid id, long expectedRevision, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var held = LoadedWhileHoldingTheWriteLock();

            if (!held.TryGetValue(id, out var stored))
            {
                return false;
            }

            if (stored.Revision != expectedRevision)
            {
                throw new RequestConcurrencyException(id, expectedRevision, stored);
            }

            var next = new Dictionary<Guid, StoredRequest>(held);
            next.Remove(id);

            await PersistAsync(next, cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _held, next);
            return true;
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _writes.Dispose();
    }

    /// <summary>
    /// Reads the file into a set, refusing anything it cannot read whole.
    /// </summary>
    /// <param name="filePath">The file to read.</param>
    /// <returns>What the file holds, and an empty set where there is no file yet.</returns>
    private static Dictionary<Guid, StoredRequest> Read(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        PersistedRequest?[]? persisted;

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            persisted = JsonSerializer.Deserialize<PersistedRequest?[]>(stream, SerializerOptions);
        }
        catch (JsonException reason)
        {
            throw new RequestStoreLoadException(
                filePath,
                "it is not the document this store writes, which is what an interruption in the middle of the file would leave if a write were made in place.",
                reason);
        }

        if (persisted is null)
        {
            throw new RequestStoreLoadException(filePath, "it holds a JSON null where the list of requests should be.");
        }

        var held = new Dictionary<Guid, StoredRequest>();

        foreach (var entry in persisted)
        {
            if (entry?.Request is null)
            {
                throw new RequestStoreLoadException(filePath, "one of the entries carries no request.");
            }

            if (entry.Revision < 1)
            {
                throw new RequestStoreLoadException(filePath, "one of the entries carries a revision below the one an added request starts at.");
            }

            if (!held.TryAdd(entry.Request.Id, new StoredRequest(entry.Request, entry.Revision)))
            {
                throw new RequestStoreLoadException(filePath, "two entries carry one identifier, so which of them the store holds is not decidable.");
            }
        }

        return held;
    }

    /// <summary>
    /// The set, reading the file on the first call that needs it. A caller that finds it already
    /// read takes it without waiting for anything, which is what
    /// <see cref="IRequestStore.GetAsync"/> promises.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the first read.</param>
    /// <returns>What the store holds.</returns>
    private async Task<Dictionary<Guid, StoredRequest>> HeldAsync(CancellationToken cancellationToken)
    {
        var held = Volatile.Read(ref _held);

        if (held is not null)
        {
            return held;
        }

        await _writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return LoadedWhileHoldingTheWriteLock();
        }
        finally
        {
            _writes.Release();
        }
    }

    /// <summary>
    /// The set, read from the file if this is the first call. The caller has to be holding
    /// <see cref="_writes"/>, which is what stops two of them reading the file at once and what
    /// stops a write deciding against a set that was read before another write replaced it.
    /// </summary>
    /// <returns>What the store holds.</returns>
    private Dictionary<Guid, StoredRequest> LoadedWhileHoldingTheWriteLock()
    {
        var held = _held;

        if (held is not null)
        {
            return held;
        }

        held = Read(_filePath);
        Volatile.Write(ref _held, held);
        return held;
    }

    /// <summary>
    /// Puts a set on the disk, whole or not at all.
    /// <para>
    /// Every failure here reaches the caller. There is no catch in this method and none around its
    /// call sites, so a disk that is full, a directory that cannot be written to and a file the
    /// platform refuses to replace are all reported rather than absorbed, and the in-memory set is
    /// left as it was because it is only replaced after this returns.
    /// </para>
    /// </summary>
    /// <param name="held">The set to write.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the set is on the disk.</returns>
    private async Task PersistAsync(Dictionary<Guid, StoredRequest> held, CancellationToken cancellationToken)
    {
        var persisted = held.Values
            .Select(stored => new PersistedRequest { Revision = stored.Revision, Request = stored.Request })
            .ToArray();

        Directory.CreateDirectory(_directoryPath);

        // Write-through, so the bytes are asked to reach the device rather than a cache the replace
        // below would then overtake. What that is worth against a power cut is the platform's
        // answer and is not measured by this repository's suite; what the suite does measure is the
        // property that does not depend on it, which is that the file the loader reads is never a
        // partial one.
        var pending = new FileStream(
            _pendingFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough);

        await using (pending.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(pending, persisted, SerializerOptions, cancellationToken).ConfigureAwait(false);
            await pending.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Move(_pendingFilePath, _filePath, overwrite: true);
    }
}
