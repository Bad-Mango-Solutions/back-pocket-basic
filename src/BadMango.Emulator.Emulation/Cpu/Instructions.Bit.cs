// <copyright file="Instructions.Bit.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Emulation.Cpu;

using Core.Cpu;

/// <summary>
/// Rockwell/WDC bit manipulation instructions (RMB, SMB, BBR, BBS).
/// </summary>
/// <remarks>
/// These instructions are specific to the Rockwell R65C02 and WDC W65C02S variants.
/// They are present on the Apple IIe Enhanced, IIc, and IIc+.
/// </remarks>
public static partial class Instructions
{
    /// <summary>
    /// RMB - Reset Memory Bit instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be ZeroPage).</param>
    /// <param name="bit">The bit number to reset (0-7).</param>
    /// <returns>An opcode handler that executes RMB with the given bit number.</returns>
public static OpcodeHandler RMB(AddressingModeHandler addressingMode, int bit)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte mask = (byte)~(1 << bit);
            value &= mask;

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.RMB };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// SMB - Set Memory Bit instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be ZeroPage).</param>
    /// <param name="bit">The bit number to set (0-7).</param>
    /// <returns>An opcode handler that executes SMB with the given bit number.</returns>
public static OpcodeHandler SMB(AddressingModeHandler addressingMode, int bit)
    {
        return cpu =>
        {
            byte opCycles = 0;
            Addr address = addressingMode(cpu);
            byte value = cpu.Read8(address);
            opCycles++; // Memory read

            byte mask = (byte)(1 << bit);
            value |= mask;

            cpu.Write8(address, value);
            opCycles += 2; // Memory write + internal operation

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.SMB };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// BBR - Branch if Bit Reset instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be ZeroPageRelative).</param>
    /// <param name="bit">The bit number to test (0-7).</param>
    /// <returns>An opcode handler that executes BBR with the given bit number.</returns>
    /// <remarks>
    /// 3-byte instruction: opcode, zp address, signed relative offset.
    /// Base 3 cycles (addressing mode handles 2); +1 if branch taken; +1 more if page cross on branch.
    /// No flag changes.
    /// </remarks>
public static OpcodeHandler BBR(AddressingModeHandler addressingMode, int bit)
    {
        return cpu =>
        {
            byte opCycles = 0;

            // AddressingMode (ZeroPageRelative) fetches both ZP address and relative offset,
            // advances PC by 2, adds 2 cycles, and encodes both values in return:
            // low byte = ZP address, next byte = relative offset
            Addr encoded = addressingMode(cpu);
            byte zpAddress = (byte)(encoded & 0xFF);
            sbyte offset = (sbyte)((encoded >> 8) & 0xFF);

            byte zpValue = cpu.Read8(zpAddress);
            opCycles++; // Memory read

            // Branch decision cycle (always taken, even if branch not taken)
            opCycles++;

            byte mask = (byte)(1 << bit);
            bool bitIsReset = (zpValue & mask) == 0;

            if (bitIsReset)
            {
                // Branch taken. Use 16-bit PC math so the target wraps
                // $FFFF -> $0000 like the 65C02 (mirrors Instructions.Branch.cs).
                Word oldPC = cpu.Registers.PC.GetWord();
                Word newPC = (Word)(oldPC + offset);

                // Check for page crossing
                if ((oldPC & 0xFF00) != (newPC & 0xFF00))
                {
                    opCycles++; // Page cross penalty
                }

                cpu.Registers.PC.SetWord(newPC);
                opCycles++; // Branch taken cycle
            }

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.BBR };
            }

            cpu.Registers.TCU += opCycles;
        };
    }

    /// <summary>
    /// BBS - Branch if Bit Set instruction.
    /// </summary>
    /// <param name="addressingMode">The addressing mode function to use (must be ZeroPageRelative).</param>
    /// <param name="bit">The bit number to test (0-7).</param>
    /// <returns>An opcode handler that executes BBS with the given bit number.</returns>
    /// <remarks>
    /// 3-byte instruction: opcode, zp address, signed relative offset.
    /// Base 3 cycles (addressing mode handles 2); +1 if branch taken; +1 more if page cross on branch.
    /// No flag changes.
    /// </remarks>
public static OpcodeHandler BBS(AddressingModeHandler addressingMode, int bit)
    {
        return cpu =>
        {
            byte opCycles = 0;

            // AddressingMode (ZeroPageRelative) fetches both ZP address and relative offset,
            // advances PC by 2, adds 2 cycles, and encodes both values in return:
            // low byte = ZP address, next byte = relative offset
            Addr encoded = addressingMode(cpu);
            byte zpAddress = (byte)(encoded & 0xFF);
            sbyte offset = (sbyte)((encoded >> 8) & 0xFF);

            byte zpValue = cpu.Read8(zpAddress);
            opCycles++; // Memory read

            // Branch decision cycle (always taken, even if branch not taken)
            opCycles++;

            byte mask = (byte)(1 << bit);
            bool bitIsSet = (zpValue & mask) != 0;

            if (bitIsSet)
            {
                // Branch taken. Use 16-bit PC math so the target wraps
                // $FFFF -> $0000 like the 65C02 (mirrors Instructions.Branch.cs).
                Word oldPC = cpu.Registers.PC.GetWord();
                Word newPC = (Word)(oldPC + offset);

                // Check for page crossing
                if ((oldPC & 0xFF00) != (newPC & 0xFF00))
                {
                    opCycles++; // Page cross penalty
                }

                cpu.Registers.PC.SetWord(newPC);
                opCycles++; // Branch taken cycle
            }

            if (cpu.IsDebuggerAttached)
            {
                cpu.Trace = cpu.Trace with { Instruction = CpuInstructions.BBS };
            }

            cpu.Registers.TCU += opCycles;
        };
    }
}