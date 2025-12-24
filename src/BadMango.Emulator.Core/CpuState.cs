// <copyright file="CpuState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Represents the complete state of a 65C02 CPU.
/// </summary>
/// <remarks>
/// This structure captures all CPU registers and execution state for
/// save states, debugging, and state inspection purposes.
/// Uses explicit layout for optimal memory packing.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CpuState
{
    /// <summary>
    /// Gets or sets the CPU registers.
    /// </summary>
    public Registers Registers;

    /// <summary>
    /// Gets or sets the total number of cycles executed.
    /// </summary>
    public ulong Cycles;

    /// <summary>Indicates whether a debugger is currently attached to the CPU.</summary>
    public bool IsDebuggerAttached;

    /// <summary>The program counter at the start of the instruction.</summary>
    public Addr StartPC;

    /// <summary>The opcode of the currently executing instruction.</summary>
    public byte Opcode;

    /// <summary>The sub-opcode or extension byte for the current instruction, if applicable.</summary>
    public byte SubOpcode;

    /// <summary>The instruction mnemonic for the current instruction.</summary>
    public CpuInstructions Instruction;

    /// <summary>The addressing mode used by the current instruction.</summary>
    public CpuAddressingModes AddressingMode;

    /// <summary>The size of the operands for the current instruction in bytes.</summary>
    public byte OperandSize;

    /// <summary>The operands for the current instruction (up to 4 bytes).</summary>
    public OperandBuffer Operands;

    /// <summary>The effective address calculated for the current instruction.</summary>
    public Addr EffectiveAddress;

    /// <summary>The number of cycles the current instruction took to execute.</summary>
    public byte InstructionCycles;

    /// <summary>Initializes a new instance of the <see cref="CpuState"/> struct.</summary>
    public CpuState()
    {
        Registers = default;
        Cycles = 0;
        ClearDebugStateInformation();
        HaltReason = HaltState.None;
    }

    /// <summary>
    /// Gets a value indicating whether the CPU is halted.
    /// </summary>
    /// <remarks>
    /// This property returns true if the CPU is in any halt state (Brk, Wai, or Stp).
    /// For more granular halt state information, use <see cref="HaltReason"/>.
    /// </remarks>
    public readonly bool Halted => HaltReason != HaltState.None;

    /// <summary>
    /// Gets or sets the reason the CPU is halted.
    /// </summary>
    /// <remarks>
    /// Distinguishes between different halt states:
    /// - None: CPU is running
    /// - Brk: Halted by BRK instruction (software interrupt)
    /// - Wai: Halted by WAI instruction (wait for interrupt)
    /// - Stp: Halted by STP instruction (permanent halt until reset).
    /// </remarks>
    public HaltState HaltReason { get; set; }

    /// <summary>
    /// Resets the debug-related state information of the CPU.
    /// </summary>
    /// <remarks>
    /// This method clears all debug-related fields, including the program counter at the start of the instruction,
    /// opcode, sub-opcode, instruction, operand size, effective address, instruction cycles, and operand data.
    /// It is intended to prepare the CPU state for the next instruction or debugging session.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearDebugStateInformation()
    {
        StartPC = Registers.PC.addr;
        Opcode = 0;
        SubOpcode = 0;
        Instruction = 0;
        OperandSize = 0;
        EffectiveAddress = 0;
        InstructionCycles = 0;
        Operands = default;
    }
}