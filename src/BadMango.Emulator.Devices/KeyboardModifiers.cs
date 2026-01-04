// <copyright file="KeyboardModifiers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Modifier key states for the keyboard device.
/// </summary>
[Flags]
public enum KeyboardModifiers
{
    /// <summary>
    /// No modifiers are active.
    /// </summary>
    None = 0,

    /// <summary>
    /// Shift key is pressed.
    /// </summary>
    Shift = 1 << 0,

    /// <summary>
    /// Control key is pressed.
    /// </summary>
    Control = 1 << 1,

    /// <summary>
    /// Open Apple key is pressed (also Button 0 on game port).
    /// </summary>
    OpenApple = 1 << 2,

    /// <summary>
    /// Closed Apple key is pressed (also Button 1 on game port).
    /// </summary>
    ClosedApple = 1 << 3,

    /// <summary>
    /// Caps Lock is active.
    /// </summary>
    CapsLock = 1 << 4,
}