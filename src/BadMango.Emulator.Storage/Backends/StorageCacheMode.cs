// <copyright file="StorageCacheMode.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Backends;

/// <summary>
/// Caching strategy for <see cref="RamCachedStorageBackend"/>.
/// </summary>
public enum StorageCacheMode
{
    /// <summary>
    /// Writes are forwarded to the underlying backend immediately. The cache is kept in
    /// sync but never holds dirty data. This is the default for file-backed images per
    /// PRD §6.1 FR-S7.
    /// </summary>
    WriteThrough = 0,

    /// <summary>
    /// Writes mutate the in-RAM cache only and mark the affected blocks dirty. Dirty
    /// blocks are committed to the underlying backend on an explicit <see cref="IStorageBackend.Flush"/>.
    /// </summary>
    WriteBack = 1,
}