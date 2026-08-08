using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.Requests.Storage;
using Jellyfin.Plugin.Requests.Tests.Doubles;

namespace Jellyfin.Plugin.Requests.Tests.Storage;

/// <summary>
/// The conformance suite run against the store this plugin ships. It adds no assertion of its own,
/// for the reason the in-memory one gives: a promise only one implementation keeps is not a
/// contract. What the durable store owes on top of the contract is
/// <see cref="FileRequestStoreDurabilityTests"/>.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1515:Consider making public types internal",
    Justification = "The rule exempts a class that declares a test method and this one only inherits them, so it reads as an unused public type. xUnit discovers tests on public classes only, so internal would silently run nothing.")]
public sealed class FileRequestStoreTests : RequestStoreContract, IDisposable
{
    private readonly List<FileRequestStore> _stores = [];
    private readonly List<string> _directories = [];

    /// <summary>
    /// Removes every store this test made and the directory each one wrote in.
    /// </summary>
    public void Dispose()
    {
        foreach (var store in _stores)
        {
            store.Dispose();
        }

        foreach (var directory in _directories)
        {
            TestRunDirectory.Remove(directory);
        }
    }

    /// <inheritdoc />
    protected override IRequestStore NewStore()
    {
        var directory = TestRunDirectory.CreateSubdirectory();
        _directories.Add(directory);

        var store = new FileRequestStore(directory);
        _stores.Add(store);
        return store;
    }
}
