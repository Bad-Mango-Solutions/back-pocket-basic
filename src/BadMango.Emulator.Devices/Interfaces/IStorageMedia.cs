// <copyright file="IStorageMedia.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Host-side abstraction for disk media used by storage devices.
/// </summary>
/// <remarks>
/// <para>
/// This API follows the repository disk abstractions and represents media-level properties common to all storage media types.
/// </para>
/// <para>
/// Reference specifications:
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Disk%20Image%20Abstraction%20API%20Spec.md">Disk Image Abstraction API Spec</see>,
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Unified%20Block%20Device%20Backing%20API%20for%20Apple%20II%20Emulator.md">Unified Block Device Backing API for Apple II Emulator</see>.
/// </para>
/// <para>
/// Related open-source references include AppleWin, MAME, OpenEmulator, KEGS, CiderPress2, and DiskM8.
/// </para>
/// </remarks>
public interface IStorageMedia
{
    /// <summary>
    /// Raised when media content or host-observable media state changes.
    /// </summary>
    event EventHandler? MediaChanged;

    /// <summary>
    /// Gets the total media size, in bytes.
    /// </summary>
    long SizeBytes { get; }

    /// <summary>
    /// Gets a value indicating whether writes are disallowed.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets the media format identifier.
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets format-specific metadata for host inspection and tooling.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}