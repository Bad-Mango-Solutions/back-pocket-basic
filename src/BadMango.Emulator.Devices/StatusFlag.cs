// <copyright file="StatusFlag.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Status flag identifiers for Apple IIe soft switch reads at $C011-$C01F.
/// </summary>
/// <remarks>
/// <para>
/// These flags provide read-back of various system states through the
/// I/O page soft switches. Reading these addresses returns bit 7 set
/// if the corresponding feature is enabled.
/// </para>
/// </remarks>
public enum StatusFlag
{
    /// <summary>
    /// $C011: Language Card bank 2 selected for $D000-$DFFF.
    /// </summary>
    RDLCBNK2,

    /// <summary>
    /// $C012: Language Card RAM read enabled (ROM disabled).
    /// </summary>
    RDLCRAM,

    /// <summary>
    /// $C013: Reads from $0200-$BFFF come from auxiliary RAM.
    /// </summary>
    RAMRD,

    /// <summary>
    /// $C014: Writes to $0200-$BFFF go to auxiliary RAM.
    /// </summary>
    RAMWRT,

    /// <summary>
    /// $C015: Internal C1-C7 ROM enabled (slot ROM disabled).
    /// </summary>
    INTCXROM,

    /// <summary>
    /// $C016: Alternate zero page/stack ($0000-$01FF from auxiliary RAM).
    /// </summary>
    ALTZP,

    /// <summary>
    /// $C017: Slot 3 ROM enabled (internal C3 ROM disabled).
    /// </summary>
    SLOTC3ROM,

    /// <summary>
    /// $C018: 80STORE mode enabled (PAGE2 controls aux memory for display).
    /// </summary>
    STORE80,

    /// <summary>
    /// $C019: Vertical blanking in progress.
    /// </summary>
    VERTBLANK,

    /// <summary>
    /// $C01A: Text mode enabled.
    /// </summary>
    TEXT,

    /// <summary>
    /// $C01B: Mixed mode enabled (4 lines of text at bottom).
    /// </summary>
    MIXED,

    /// <summary>
    /// $C01C: Page 2 selected (video page or 80STORE auxiliary).
    /// </summary>
    PAGE2,

    /// <summary>
    /// $C01D: Hi-res mode enabled.
    /// </summary>
    HIRES,

    /// <summary>
    /// $C01E: Alternate character set enabled.
    /// </summary>
    ALTCHARSET,

    /// <summary>
    /// $C01F: 80-column mode enabled.
    /// </summary>
    COL80,
}