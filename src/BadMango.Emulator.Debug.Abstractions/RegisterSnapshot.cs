// <copyright file="RegisterSnapshot.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// A snapshot of the machine's CPU registers.
/// </summary>
/// <param name="CpuType">The CPU architecture identifier.</param>
/// <param name="ProgramCounter">The current program counter.</param>
/// <param name="Accumulator">The accumulator register.</param>
/// <param name="XRegister">The X register.</param>
/// <param name="YRegister">The Y register.</param>
/// <param name="StackPointer">The stack pointer.</param>
/// <param name="StatusRegister">The processor status flags register.</param>
/// <param name="DirectPage">The direct-page register for 65816/65832 systems.</param>
/// <param name="DataBank">The data bank register for 65816/65832 systems.</param>
/// <param name="ExtraBank">The extra bank register for 65816/65832 systems.</param>
/// <param name="KRegister">The K register for 65832 systems.</param>
/// <param name="BRegister">The B register for 65832 systems.</param>
public sealed record RegisterSnapshot(
    string CpuType,
    ulong ProgramCounter,
    ulong Accumulator,
    ulong XRegister,
    ulong YRegister,
    ulong StackPointer,
    ulong StatusRegister,
    ulong? DirectPage = null,
    ulong? DataBank = null,
    ulong? ExtraBank = null,
    ulong? KRegister = null,
    ulong? BRegister = null);