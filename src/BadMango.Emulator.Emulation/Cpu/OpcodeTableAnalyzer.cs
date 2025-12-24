// <copyright file="OpcodeTableAnalyzer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Reflection;

using BadMango.Emulator.Core;

/// <summary>
/// Analyzes opcode handlers to extract addressing mode information for debugging.
/// </summary>
/// <remarks>
/// This analyzer uses reflection to inspect opcode handlers and determine operand
/// lengths based on the addressing mode delegates they capture. This is performed
/// once at initialization time, outside of the hot loop, so the reflection cost
/// is acceptable.
/// </remarks>
public static class OpcodeTableAnalyzer
{
    /// <summary>
    /// Maps addressing mode method names to their operand byte lengths.
    /// </summary>
    private static readonly Dictionary<string, byte> AddressingModeOperandLengths = new()
    {
        [nameof(AddressingModes.Implied)] = 0,
        [nameof(AddressingModes.Accumulator)] = 0,
        [nameof(AddressingModes.ImmediateByte)] = 1,
        [nameof(AddressingModes.ZeroPage)] = 1,
        [nameof(AddressingModes.ZeroPageX)] = 1,
        [nameof(AddressingModes.ZeroPageY)] = 1,
        [nameof(AddressingModes.Relative)] = 1,
        [nameof(AddressingModes.IndirectX)] = 1,
        [nameof(AddressingModes.IndirectY)] = 1,
        [nameof(AddressingModes.IndirectYWrite)] = 1,
        [nameof(AddressingModes.Absolute)] = 2,
        [nameof(AddressingModes.AbsoluteX)] = 2,
        [nameof(AddressingModes.AbsoluteY)] = 2,
        [nameof(AddressingModes.AbsoluteXWrite)] = 2,
        [nameof(AddressingModes.AbsoluteYWrite)] = 2,
        [nameof(AddressingModes.Indirect)] = 2,
    };

    /// <summary>
    /// Builds an operand length lookup table by analyzing the opcode handlers.
    /// </summary>
    /// <param name="opcodeTable">The opcode table to analyze.</param>
    /// <returns>An array of 256 bytes where each index is an opcode and the value is the operand length.</returns>
    public static byte[] BuildOperandLengthTable(OpcodeTable opcodeTable)
    {
        ArgumentNullException.ThrowIfNull(opcodeTable);

        var operandLengths = new byte[256];

        for (int i = 0; i < 256; i++)
        {
            operandLengths[i] = GetOperandLengthFromHandler(opcodeTable.GetHandler((byte)i));
        }

        return operandLengths;
    }

    /// <summary>
    /// Extracts the operand length from an opcode handler by inspecting its captured addressing mode delegate.
    /// </summary>
    /// <param name="handler">The opcode handler to analyze.</param>
    /// <returns>The operand length in bytes (0, 1, or 2).</returns>
    private static byte GetOperandLengthFromHandler(OpcodeHandler handler)
    {
        if (handler == null)
        {
            return 0;
        }

        // The handler is a closure - its Target is the compiler-generated display class
        object? target = handler.Target;
        if (target == null)
        {
            return 0; // Static method, no captured state
        }

        // Find the captured AddressingMode delegate field
        var targetType = target.GetType();
        foreach (var field in targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            // Look for a field of type AddressingMode<CpuState>
            if (field.FieldType == typeof(AddressingMode<CpuState>))
            {
                var addressingModeDelegate = field.GetValue(target) as AddressingMode<CpuState>;
                if (addressingModeDelegate != null)
                {
                    // Get the method name and look up operand length
                    string methodName = addressingModeDelegate.Method.Name;
                    if (AddressingModeOperandLengths.TryGetValue(methodName, out byte length))
                    {
                        return length;
                    }
                }
            }
        }

        return 0; // Default for unknown/implied
    }
}