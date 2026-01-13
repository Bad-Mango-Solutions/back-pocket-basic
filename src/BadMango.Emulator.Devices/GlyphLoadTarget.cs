// <copyright file="GlyphLoadTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Target location for hot-loaded glyph data.
/// </summary>
public enum GlyphLoadTarget
{
    /// <summary>
    /// Load into character ROM (replaces base glyphs).
    /// </summary>
    Rom,

    /// <summary>
    /// Load into glyph RAM bank 1 (overlay for primary set).
    /// </summary>
    GlyphRamBank1,

    /// <summary>
    /// Load into glyph RAM bank 2 (overlay for alternate set).
    /// </summary>
    GlyphRamBank2,
}