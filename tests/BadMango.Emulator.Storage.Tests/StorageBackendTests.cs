// <copyright file="StorageBackendTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Tests;

/// <summary>
/// Unit tests covering <see cref="RamStorageBackend"/>, <see cref="FileStorageBackend"/>,
/// and <see cref="RamCachedStorageBackend"/> (PRD §6.1 FR-S1, FR-S7).
/// </summary>
[TestFixture]
public class StorageBackendTests
{
    /// <summary>
    /// Verifies that <see cref="RamStorageBackend"/> round-trips writes through reads.
    /// </summary>
    [Test]
    public void RamBackend_RoundTrip()
    {
        using var backend = new RamStorageBackend(1024);
        Assert.That(backend.Length, Is.EqualTo(1024));
        Assert.That(backend.CanWrite, Is.True);

        var payload = new byte[] { 1, 2, 3, 4, 5 };
        backend.Write(100, payload);

        var readBack = new byte[5];
        backend.Read(100, readBack);
        Assert.That(readBack, Is.EqualTo(payload));
    }

    /// <summary>
    /// Verifies that a read-only RAM backend rejects writes.
    /// </summary>
    [Test]
    public void RamBackend_ReadOnly_RejectsWrites()
    {
        using var backend = new RamStorageBackend(64, canWrite: false);
        Assert.That(backend.CanWrite, Is.False);
        Assert.Throws<InvalidOperationException>(() => backend.Write(0, new byte[] { 1 }));
    }

    /// <summary>
    /// Verifies that out-of-range reads / writes throw.
    /// </summary>
    [Test]
    public void RamBackend_OutOfRange_Throws()
    {
        using var backend = new RamStorageBackend(16);
        Assert.Throws<ArgumentOutOfRangeException>(() => backend.Read(15, new byte[2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => backend.Write(15, new byte[2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => backend.Read(-1, new byte[1]));
    }

    /// <summary>
    /// Verifies that <see cref="FileStorageBackend"/> writes durably and is readable.
    /// </summary>
    [Test]
    public void FileBackend_RoundTripAndFlush()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bms-storage-{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllBytes(path, new byte[256]);
            using (var backend = new FileStorageBackend(path))
            {
                Assert.That(backend.CanWrite, Is.True);
                Assert.That(backend.Length, Is.EqualTo(256));
                backend.Write(10, new byte[] { 0xAA, 0xBB, 0xCC });
                backend.Flush();
            }

            var bytes = File.ReadAllBytes(path);
            Assert.That(bytes[10], Is.EqualTo(0xAA));
            Assert.That(bytes[11], Is.EqualTo(0xBB));
            Assert.That(bytes[12], Is.EqualTo(0xCC));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that opening a missing file throws.
    /// </summary>
    [Test]
    public void FileBackend_MissingFile_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bms-missing-{Guid.NewGuid():N}.bin");
        Assert.Throws<FileNotFoundException>(() => new FileStorageBackend(path));
    }

    /// <summary>
    /// Verifies that a write-through cached backend forwards writes immediately.
    /// </summary>
    [Test]
    public void CachedBackend_WriteThrough_ForwardsImmediately()
    {
        var inner = new RamStorageBackend(1024);
        using var cache = new RamCachedStorageBackend(inner, StorageCacheMode.WriteThrough, blockSize: 256, ownsInner: true);

        Assert.That(cache.Length, Is.EqualTo(1024));
        Assert.That(cache.BlockCount, Is.EqualTo(4));
        Assert.That(cache.Mode, Is.EqualTo(StorageCacheMode.WriteThrough));

        cache.Write(0, new byte[] { 1, 2, 3 });

        var snapshot = inner.ToArray();
        Assert.That(snapshot[0], Is.EqualTo(1));
        Assert.That(snapshot[1], Is.EqualTo(2));
        Assert.That(snapshot[2], Is.EqualTo(3));
        Assert.That(cache.DirtyBlockCount(), Is.EqualTo(0), "Write-through must not retain dirty state.");
    }

    /// <summary>
    /// Verifies that a write-back cached backend defers writes and tracks dirty blocks.
    /// </summary>
    [Test]
    public void CachedBackend_WriteBack_DefersUntilFlush()
    {
        var inner = new RamStorageBackend(1024);
        using var cache = new RamCachedStorageBackend(inner, StorageCacheMode.WriteBack, blockSize: 256);

        cache.Write(0, new byte[] { 0xDE, 0xAD });
        cache.Write(600, new byte[] { 0xBE, 0xEF });

        // Inner backend untouched until Flush.
        var snapshotBefore = inner.ToArray();
        Assert.That(snapshotBefore[0], Is.EqualTo(0));
        Assert.That(snapshotBefore[600], Is.EqualTo(0));

        // Two distinct blocks marked dirty (block 0 and block 2).
        Assert.That(cache.IsDirty(0), Is.True);
        Assert.That(cache.IsDirty(1), Is.False);
        Assert.That(cache.IsDirty(2), Is.True);
        Assert.That(cache.IsDirty(3), Is.False);
        Assert.That(cache.DirtyBlockCount(), Is.EqualTo(2));

        cache.Flush();

        var snapshotAfter = inner.ToArray();
        Assert.That(snapshotAfter[0], Is.EqualTo(0xDE));
        Assert.That(snapshotAfter[1], Is.EqualTo(0xAD));
        Assert.That(snapshotAfter[600], Is.EqualTo(0xBE));
        Assert.That(snapshotAfter[601], Is.EqualTo(0xEF));
        Assert.That(cache.DirtyBlockCount(), Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that a write spanning two cache blocks marks both blocks dirty.
    /// </summary>
    [Test]
    public void CachedBackend_WriteBack_SpanningBlocks_MarksBothDirty()
    {
        var inner = new RamStorageBackend(1024);
        using var cache = new RamCachedStorageBackend(inner, StorageCacheMode.WriteBack, blockSize: 256);

        cache.Write(255, new byte[] { 0x11, 0x22 });

        Assert.That(cache.IsDirty(0), Is.True);
        Assert.That(cache.IsDirty(1), Is.True);

        cache.Flush();

        var snap = inner.ToArray();
        Assert.That(snap[255], Is.EqualTo(0x11));
        Assert.That(snap[256], Is.EqualTo(0x22));
    }

    /// <summary>
    /// Verifies that a cached backend over a read-only inner reports read-only and rejects writes.
    /// </summary>
    [Test]
    public void CachedBackend_PropagatesReadOnly()
    {
        var inner = new RamStorageBackend(64, canWrite: false);
        using var cache = new RamCachedStorageBackend(inner, StorageCacheMode.WriteBack, blockSize: 32);
        Assert.That(cache.CanWrite, Is.False);
        Assert.Throws<InvalidOperationException>(() => cache.Write(0, new byte[] { 1 }));
    }

    /// <summary>
    /// Verifies that the cached backend reads back the inner's contents on construction.
    /// </summary>
    [Test]
    public void CachedBackend_PrefillsCacheFromInner()
    {
        var inner = new RamStorageBackend(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2 });
        using var cache = new RamCachedStorageBackend(inner, blockSize: 4);
        var buf = new byte[8];
        cache.Read(0, buf);
        Assert.That(buf, Is.EqualTo(new byte[] { 9, 8, 7, 6, 5, 4, 3, 2 }));
    }
}