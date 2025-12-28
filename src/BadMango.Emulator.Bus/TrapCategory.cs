// <copyright file="TrapCategory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Classification of trap types for diagnostics and filtering.
/// </summary>
/// <remarks>
/// <para>
/// Trap categories allow grouping related ROM routine interceptions for
/// easier management and debugging. For example, all BASIC interpreter
/// traps can be enabled or disabled together.
/// </para>
/// </remarks>
public enum TrapCategory
{
    /// <summary>
    /// Core firmware routines (reset, IRQ handlers).
    /// </summary>
    Firmware,

    /// <summary>
    /// Monitor/debugger routines.
    /// </summary>
    Monitor,

    /// <summary>
    /// BASIC interpreter routines.
    /// </summary>
    BasicInterp,

    /// <summary>
    /// BASIC runtime (math, strings, I/O).
    /// </summary>
    BasicRuntime,

    /// <summary>
    /// DOS/ProDOS entry points.
    /// </summary>
    Dos,

    /// <summary>
    /// Printer output routines.
    /// </summary>
    PrinterDriver,

    /// <summary>
    /// Disk I/O routines.
    /// </summary>
    DiskDriver,

    /// <summary>
    /// User-defined traps.
    /// </summary>
    Custom,
}