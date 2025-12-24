// <copyright file="Cpu65C02Constants.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

/// <summary>
/// Constants for the 65C02 CPU.
/// </summary>
internal static class Cpu65C02Constants
{
    /// <summary>
    /// Processor status flags.
    /// </summary>
    internal const byte FlagC = 0x01; // Carry flag

    /// <summary>
    /// Zero flag.
    /// </summary>
    internal const byte FlagZ = 0x02; // Zero flag

    /// <summary>
    /// Interrupt disable flag.
    /// </summary>
    internal const byte FlagI = 0x04; // Interrupt disable flag

    /// <summary>
    /// Decimal mode flag.
    /// </summary>
    internal const byte FlagD = 0x08; // Decimal mode flag

    /// <summary>
    /// Break flag.
    /// </summary>
    internal const byte FlagB = 0x10; // Break flag

    /// <summary>
    /// Unused flag (always 1).
    /// </summary>
    internal const byte FlagU = 0x20; // Unused (always 1)

    /// <summary>
    /// Overflow flag.
    /// </summary>
    internal const byte FlagV = 0x40; // Overflow flag

    /// <summary>
    /// Negative flag.
    /// </summary>
    internal const byte FlagN = 0x80; // Negative flag

    /// <summary>
    /// Stack base address.
    /// </summary>
    internal const Addr StackBase = 0x0100;

    /// <summary>
    /// NMI interrupt vector address.
    /// </summary>
    internal const Addr NmiVector = 0xFFFA;

    /// <summary>
    /// RESET vector address.
    /// </summary>
    internal const Addr ResetVector = 0xFFFC;

    /// <summary>
    /// IRQ/BRK interrupt vector address.
    /// </summary>
    internal const Addr IrqVector = 0xFFFE;
}