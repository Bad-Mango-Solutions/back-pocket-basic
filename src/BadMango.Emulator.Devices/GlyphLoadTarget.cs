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
    /// Load into glyph RAM (overlay for custom glyphs).
    /// </summary>
    GlyphRam,
}