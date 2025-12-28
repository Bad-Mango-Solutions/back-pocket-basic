// <copyright file="Registers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Represents the CPU's register set, containing various registers used for
/// arithmetic, logical, addressing, and control operations.
/// </summary>
/// <remarks>
/// This struct is designed to model the internal state of the CPU's registers,
/// including general-purpose registers, index registers, and control flags.
/// It is laid out sequentially in memory with a packing size of 1 to ensure
/// compatibility with low-level emulation requirements.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Registers
{
    /// <summary>Represents a constant zero register.</summary>
    public readonly DWord ZR;

    /// <summary>Represents the Accumulator register within the CPU's register set.</summary>
    /// <remarks>
    /// The Accumulator register is used for arithmetic and logical operations
    /// and supports multiple size views, including 8-bit, 16-bit, and 32-bit values.
    /// </remarks>
    public RegisterAccumulator A;

    /// <summary>Represents the X index register within the CPU's register set.</summary>
    /// <remarks>
    /// The X index register is used for indexed addressing modes and supports multiple size views.
    /// </remarks>
    public RegisterIndex X;

    /// <summary>Represents the Y index register within the CPU's register set.</summary>
    /// <remarks>
    /// The Y index register is used for indexed addressing modes and supports multiple size views.
    /// </remarks>
    public RegisterIndex Y;

    /// <summary>Represents the Direct Page register within the CPU's register set.</summary>
    /// <remarks>
    /// The Direct Page register is used for the direct addressing mode and supports multiple size views.
    /// </remarks>
    public RegisterDirectPage D;

    /// <summary>Represents the Stack Pointer register within the CPU's register set.</summary>
    /// <remarks>
    /// The Stack Pointer register points to the current top of the stack. It supports multiple size views.
    /// </remarks>
    public RegisterStackPointer SP;

    /// <summary>Represents the Program Counter register within the CPU's register set.</summary>
    /// <remarks>
    /// The Program Counter register holds the address of the next instruction to be executed.
    /// </remarks>
    public RegisterProgramCounter PC;

    /// <summary>Represents the Processor Status register within the CPU's register set.</summary>
    public ProcessorStatusFlags P;

    /// <summary>Indicates whether the CPU is in 65C02 Emulation mode.</summary>
    public bool E;

    /// <summary>Indicates whether the CPU is in Native mode (false) or Compatibility mode (true).</summary>
    public bool CP;

    /// <summary>Represents the Data Bank Register (DBR) used for banked memory access.</summary>
    public byte DBR;

    /// <summary>Represents the Program Bank Register (PBR) used for banked memory access.</summary>
    public byte PBR;

    /// <summary>Represents general-purpose register R0.</summary>
    public DWord R0;

    /// <summary>Represents general-purpose register R1.</summary>
    public DWord R1;

    /// <summary>Represents general-purpose register R2.</summary>
    public DWord R2;

    /// <summary>Represents general-purpose register R3.</summary>
    public DWord R3;

    /// <summary>Represents general-purpose register R4.</summary>
    public DWord R4;

    /// <summary>Represents general-purpose register R5.</summary>
    public DWord R5;

    /// <summary>Represents general-purpose register R6.</summary>
    public DWord R6;

    /// <summary>Represents general-purpose register R7.</summary>
    /// <remarks>
    /// R7 serves as the canonical Frame Pointer (FP) in the 65832 calling convention
    /// when a function requires a stable stack frame for debugging or local variable access.
    /// </remarks>
    public DWord R7;

    /// <summary>System/privileged registers (kernel/hypervisor mode only).</summary>
    /// <remarks>
    /// These registers are only accessible when the CPU is in Kernel (K) or Hypervisor (H) mode.
    /// Access from User mode will trigger a privilege violation exception.
    /// </remarks>
    public SystemRegisters System;

    /// <summary>Initializes a new instance of the <see cref="Registers"/> struct with default values.</summary>
    /// <remarks>
    /// This constructor sets all fields of the <see cref="Registers"/> struct to their default values:
    /// <list type="bullet">
    /// <item><description>Registers such as <see cref="A"/>, <see cref="X"/>, <see cref="Y"/>, <see cref="D"/>, <see cref="SP"/>, and <see cref="PC"/> are initialized to their default values.</description></item>
    /// <item><description>The <see cref="P"/> field is set to <c>0</c> (default <see cref="ProcessorStatusFlags"/>).</description></item>
    /// <item><description>Boolean fields <see cref="E"/> and <see cref="CP"/> are set to <c>false</c>.</description></item>
    /// <item><description>Byte fields <see cref="DBR"/> and <see cref="PBR"/> are set to <c>0</c>.</description></item>
    /// <item><description>Fields <see cref="R0"/> through <see cref="R7"/> are set to <c>0</c>.</description></item>
    /// </list>
    /// </remarks>
    public Registers()
    {
        A = default;
        X = default;
        Y = default;
        D = default;
        SP = default;
        PC = default;
        P = (ProcessorStatusFlags)0;
        E = false;
        CP = false;
        DBR = 0;
        PBR = 0;
        R0 = 0;
        R1 = 0;
        R2 = 0;
        R3 = 0;
        R4 = 0;
        R5 = 0;
        R6 = 0;
        R7 = 0;
        ZR = 0;
        System = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Registers"/> struct with the specified compatibility mode
    /// and reset vector.
    /// </summary>
    /// <param name="compat">
    /// A boolean value indicating whether the processor should operate in compatibility mode.
    /// If <c>true</c>, the processor is initialized in compatibility mode.
    /// </param>
    /// <param name="resetVector">
    /// The initial value to set for the program counter (<see cref="RegisterProgramCounter"/>).
    /// </param>
    public Registers(bool compat, Word resetVector)
        : this()
    {
        if (!compat) { return; }

        PC.SetWord(resetVector);
        P = ProcessorStatusFlags.Reset;
        CP = true;
        E = true;
        SP.SetByte(0xFF);
    }
}

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable SA1307 // Disabled by jpactor to allow short type names and field names for clarity and brevity.
/// <summary>Represents the Accumulator register with multiple size views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterAccumulator
{
    /// <summary>Represents the double word (32-bit) value of the register.</summary>
    /// <remarks>Only available in 65832 native mode.</remarks>
    public DWord acc;

    /// <summary>Gets the 8-bit (byte) value of the accumulator.</summary>
    /// <returns>The low byte of the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte GetByte() => (byte)(acc & 0xFF);

    /// <summary>Gets the 16-bit (word) value of the accumulator.</summary>
    /// <returns>The low word of the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Word GetWord() => (Word)(acc & 0xFFFF);

    /// <summary>Gets the 32-bit (double word) value of the accumulator.</summary>
    /// <returns>The full 32-bit value of the accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DWord GetDWord() => acc;

    /// <summary>Sets the 8-bit (byte) value of the accumulator, preserving the upper bytes.</summary>
    /// <param name="value">The byte value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetByte(byte value) => acc = (acc & 0xFFFFFF00) | value;

    /// <summary>Sets the 16-bit (word) value of the accumulator, preserving the upper word.</summary>
    /// <param name="value">The word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWord(Word value) => acc = (acc & 0xFFFF0000) | value;

    /// <summary>Sets the 32-bit (double word) value of the accumulator.</summary>
    /// <param name="value">The double word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDWord(DWord value) => acc = value;
}

/// <summary>Represents the X index register with multiple size views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterIndex
{
    /// <summary>Represents the double word (32-bit) value of the register.</summary>
    public DWord index;

    /// <summary>Gets the 8-bit (byte) value of the index register.</summary>
    /// <returns>The low byte of the index register.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte GetByte() => (byte)(index & 0xFF);

    /// <summary>Gets the 16-bit (word) value of the index register.</summary>
    /// <returns>The low word of the index register.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Word GetWord() => (Word)(index & 0xFFFF);

    /// <summary>Gets the 32-bit (double word) value of the index register.</summary>
    /// <returns>The full 32-bit value of the index register.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DWord GetDWord() => index;

    /// <summary>Sets the 8-bit (byte) value of the index register, preserving the upper bytes.</summary>
    /// <param name="value">The byte value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetByte(byte value) => index = (index & 0xFFFFFF00) | value;

    /// <summary>Sets the 16-bit (word) value of the index register, preserving the upper word.</summary>
    /// <param name="value">The word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWord(Word value) => index = (index & 0xFFFF0000) | value;

    /// <summary>Sets the 32-bit (double word) value of the index register.</summary>
    /// <param name="value">The double word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDWord(DWord value) => index = value;
}

/// <summary>Represents the Stack Pointer register with multiple size views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterStackPointer
{
    /// <summary>Represents the double word (32-bit) value of the register.</summary>
    public DWord stack;

    /// <summary>Gets the 8-bit (byte) value of the stack pointer.</summary>
    /// <returns>The low byte of the stack pointer.</returns>
    public readonly byte GetByte() => (byte)(stack & 0xFF);

    /// <summary>Gets the 16-bit (word) value of the stack pointer.</summary>
    /// <returns>The low word of the stack pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Word GetWord() => (Word)(stack & 0xFFFF);

    /// <summary>Gets the 32-bit (double word) value of the stack pointer.</summary>
    /// <returns>The full 32-bit value of the stack pointer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DWord GetDWord() => stack;

    /// <summary>Sets the 8-bit (byte) value of the stack pointer, preserving the upper bytes.</summary>
    /// <param name="value">The byte value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetByte(byte value) => stack = (stack & 0xFFFFFF00) | value;

    /// <summary>Sets the 16-bit (word) value of the stack pointer, preserving the upper word.</summary>
    /// <param name="value">The word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWord(Word value) => stack = (stack & 0xFFFF0000) | value;

    /// <summary>Sets the 32-bit (double word) value of the stack pointer.</summary>
    /// <param name="value">The double word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDWord(DWord value) => stack = value;
}

/// <summary>Represents the Direct Page register with multiple size views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterDirectPage
{
    /// <summary>Represents the address value of the direct page register.</summary>
    public Addr direct;

    /// <summary>Gets the 16-bit (word) value of the direct page register.</summary>
    /// <returns>The low word of the direct page register.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Word GetWord() => (Word)(direct & 0xFFFF);

    /// <summary>Gets the full address value of the direct page register.</summary>
    /// <returns>The full 32-bit address value of the direct page register.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Addr GetAddr() => (Addr)(direct & 0xFFFFFFFF);

    /// <summary>Sets the 16-bit (word) value of the direct page register, preserving the upper word.</summary>
    /// <param name="value">The word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWord(Word value) => direct = (direct & 0xFFFF0000) | value;

    /// <summary>Sets the full address value of the direct page register.</summary>
    /// <param name="value">The address value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAddr(Addr value) => direct = value;
}

/// <summary>Represents the Program Counter register with multiple size views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterProgramCounter
{
    /// <summary>Represents the double word (32-bit) value of the register.</summary>
    public Addr addr;

    /// <summary>Gets the full address value of the program counter.</summary>
    /// <returns>The full 32-bit address value of the program counter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Addr GetAddr() => addr;

    /// <summary>Gets the 16-bit (word) value of the program counter.</summary>
    /// <returns>The low word of the program counter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Word GetWord() => (Word)(addr & 0xFFFF);

    /// <summary>Gets the bank byte of the program counter.</summary>
    /// <returns>The bank byte (bits 16-23) of the program counter.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte GetBank() => (byte)((addr >> 16) & 0xFF);

    /// <summary>Sets the 16-bit (word) value of the program counter, preserving the bank byte.</summary>
    /// <param name="value">The word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWord(Word value) => addr = (addr & 0xFFFF0000) | value;

    /// <summary>Sets the bank byte of the program counter, preserving the low 24 bits.</summary>
    /// <param name="value">The bank byte value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBank(byte value) => addr = (addr & 0x00FFFFFF) | ((Addr)value << 16);

    /// <summary>Sets both the bank byte and the word value of the program counter.</summary>
    /// <param name="bank">The bank byte value to set.</param>
    /// <param name="value">The word value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBankAndWord(byte bank, Word value) => addr = ((Addr)bank << 16) | value;

    /// <summary>Sets the full address value of the program counter.</summary>
    /// <param name="value">The address value to set.</param>
    public void SetAddr(Addr value) => addr = value;

    /// <summary>Advances the program counter by one byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance() => addr++;

    /// <summary>Advances the program counter by the specified number of bytes.</summary>
    /// <param name="count">The number of bytes to advance.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => addr = (Addr)(addr + count);
}