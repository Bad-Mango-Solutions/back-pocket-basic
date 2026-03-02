// <copyright file="MediaMetrics.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Captures host-observable metrics for a mounted disk image.
/// </summary>
/// <param name="BlockCount">The number of addressable blocks in the media.</param>
/// <param name="BlockSize">The block size, in bytes.</param>
/// <param name="IsReadOnly">A value indicating whether the media is write-protected by format or host policy.</param>
/// <param name="SupportsTrackAccess">A value indicating whether the media can expose track-oriented access.</param>
/// <param name="SupportsNibbleAccess">A value indicating whether the media can expose nibble-level access.</param>
/// <param name="Format">The host format identifier (for example, dsk, po, nib, or 2mg).</param>
public readonly record struct MediaMetrics(
    int BlockCount,
    int BlockSize,
    bool IsReadOnly,
    bool SupportsTrackAccess,
    bool SupportsNibbleAccess,
    string Format)
{
    /// <summary>
    /// Converts metrics to a serializable key/value representation.
    /// </summary>
    /// <returns>A dictionary containing all <see cref="MediaMetrics"/> values.</returns>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            [nameof(this.BlockCount)] = this.BlockCount,
            [nameof(this.BlockSize)] = this.BlockSize,
            [nameof(this.IsReadOnly)] = this.IsReadOnly,
            [nameof(this.SupportsTrackAccess)] = this.SupportsTrackAccess,
            [nameof(this.SupportsNibbleAccess)] = this.SupportsNibbleAccess,
            [nameof(this.Format)] = this.Format,
        };
    }
}