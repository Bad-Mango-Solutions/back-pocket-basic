// <copyright file="TrapCategory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Classification of trap types for diagnostics and filtering.
/// </summary>
/// <remarks>
/// Traps can be enabled or disabled by category, allowing users to selectively
/// enable performance optimizations or force ROM execution for compatibility testing.
/// </remarks>
public enum TrapCategory
{
    /// <summary>
    /// Monitor ROM routines (HOME, COUT, RDKEY, etc.).
    /// </summary>
    MonitorRom,

    /// <summary>
    /// BASIC interpreter routines (parsing, execution, math functions, etc.).
    /// </summary>
    BasicInterpreter,

    /// <summary>
    /// Operating system routines (file system, disk access, etc.).
    /// </summary>
    OperatingSystem,

    /// <summary>
    /// Slot firmware (peripheral card ROMs).
    /// </summary>
    SlotFirmware,

    /// <summary>
    /// Onboard device routines (video, sound, keyboard, etc.).
    /// </summary>
    OnboardDevice,

    /// <summary>
    /// User-defined traps for custom extensions.
    /// </summary>
    UserDefined,
}