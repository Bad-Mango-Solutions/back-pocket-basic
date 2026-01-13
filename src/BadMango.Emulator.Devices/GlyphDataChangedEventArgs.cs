// <copyright file="GlyphDataChangedEventArgs.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Event arguments for glyph data changes.
/// </summary>
/// <remarks>
/// This event is raised when character ROM/RAM data changes, allowing
/// the video renderer to refresh its glyph cache.
/// </remarks>
public sealed class GlyphDataChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the target that was modified.
    /// </summary>
    public required GlyphLoadTarget Target { get; init; }

    /// <summary>
    /// Gets or sets the specific character code that changed, or null if the entire set changed.
    /// </summary>
    public byte? CharacterCode { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this affects the alternate character set.
    /// </summary>
    public bool IsAlternateSet { get; init; }
}