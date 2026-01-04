// <copyright file="PeripheralKind.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Classification of peripheral devices by their integration pattern.
/// </summary>
public enum PeripheralKind
{
    /// <summary>
    /// Motherboard device that registers soft switches but has no slot.
    /// Examples: Keyboard, Speaker, Video Controller, Game I/O.
    /// </summary>
    Motherboard,

    /// <summary>
    /// Slot-based expansion card with I/O space and optional ROM.
    /// Examples: Disk II, Super Serial Card, Mockingboard, Thunderclock.
    /// </summary>
    SlotCard,

    /// <summary>
    /// Internal expansion that may combine slot presence with motherboard integration.
    /// Examples: 80-column card (slot 3 + aux memory), Memory expansion.
    /// </summary>
    Internal,
}