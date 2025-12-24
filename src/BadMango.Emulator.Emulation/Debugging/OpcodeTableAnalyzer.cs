// <copyright file="OpcodeTableAnalyzer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Debugging;

using System.Reflection;

using Core;

using Cpu;

/// <summary>
/// Analyzes opcode handlers to extract addressing mode information for debugging.
/// </summary>
/// <remarks>
/// This analyzer uses reflection to inspect opcode handlers and determine operand
/// lengths based on the addressing mode delegates they capture. This is performed
/// once at initialization time, outside the hot loop, so the reflection cost
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
        [nameof(AddressingModes.Immediate)] = 1,
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
    /// Maps addressing mode method names to their <see cref="CpuAddressingModes"/> enum values.
    /// </summary>
    private static readonly Dictionary<string, CpuAddressingModes> AddressingModeEnumMap = new()
    {
        [nameof(AddressingModes.Implied)] = CpuAddressingModes.Implied,
        [nameof(AddressingModes.Accumulator)] = CpuAddressingModes.Accumulator,
        [nameof(AddressingModes.Immediate)] = CpuAddressingModes.Immediate,
        [nameof(AddressingModes.ZeroPage)] = CpuAddressingModes.ZeroPage,
        [nameof(AddressingModes.ZeroPageX)] = CpuAddressingModes.ZeroPageX,
        [nameof(AddressingModes.ZeroPageY)] = CpuAddressingModes.ZeroPageY,
        [nameof(AddressingModes.Relative)] = CpuAddressingModes.Relative,
        [nameof(AddressingModes.IndirectX)] = CpuAddressingModes.IndirectX,
        [nameof(AddressingModes.IndirectY)] = CpuAddressingModes.IndirectY,
        [nameof(AddressingModes.IndirectYWrite)] = CpuAddressingModes.IndirectY, // Write variant maps to same mode
        [nameof(AddressingModes.Absolute)] = CpuAddressingModes.Absolute,
        [nameof(AddressingModes.AbsoluteX)] = CpuAddressingModes.AbsoluteX,
        [nameof(AddressingModes.AbsoluteY)] = CpuAddressingModes.AbsoluteY,
        [nameof(AddressingModes.AbsoluteXWrite)] = CpuAddressingModes.AbsoluteX, // Write variant maps to same mode
        [nameof(AddressingModes.AbsoluteYWrite)] = CpuAddressingModes.AbsoluteY, // Write variant maps to same mode
        [nameof(AddressingModes.Indirect)] = CpuAddressingModes.Indirect,
    };

    /// <summary>
    /// Maps instruction method names to their <see cref="CpuInstructions"/> enum values.
    /// </summary>
    private static readonly Dictionary<string, CpuInstructions> InstructionEnumMap = CreateInstructionEnumMap();

    /// <summary>
    /// Builds an operand length lookup table by analyzing the opcode handlers.
    /// </summary>
    /// <param name="opcodeTable">The opcode table to analyze.</param>
    /// <returns>An array of 256 bytes where each index is an opcode and the value is the operand length.</returns>
    /// <remarks>
    /// This method uses <see cref="BuildOpcodeInfoArray"/> internally to avoid duplicate reflection work.
    /// </remarks>
    public static byte[] BuildOperandLengthTable(OpcodeTable opcodeTable)
    {
        ArgumentNullException.ThrowIfNull(opcodeTable);

        var opcodeInfoArray = BuildOpcodeInfoArray(opcodeTable);
        var operandLengths = new byte[256];

        for (int i = 0; i < 256; i++)
        {
            operandLengths[i] = opcodeInfoArray[i].OperandLength;
        }

        return operandLengths;
    }

    /// <summary>
    /// Builds a complete opcode information table by analyzing the opcode handlers.
    /// </summary>
    /// <param name="opcodeTable">The opcode table to analyze.</param>
    /// <returns>
    /// A dictionary mapping opcode bytes (0x00-0xFF) to <see cref="OpcodeInfo"/> structures
    /// containing the instruction mnemonic, addressing mode, and operand length.
    /// Invalid or unimplemented opcodes will have <see cref="CpuInstructions.None"/> as the instruction.
    /// </returns>
    /// <remarks>
    /// This method analyzes the opcode table dynamically using reflection, ensuring the
    /// disassembler does not rely on statically created arrays for opcode/instruction mode mapping.
    /// The analysis is performed once at initialization time, outside of any hot execution paths.
    /// </remarks>
    public static Dictionary<byte, OpcodeInfo> BuildOpcodeInfoTable(OpcodeTable opcodeTable)
    {
        ArgumentNullException.ThrowIfNull(opcodeTable);

        var opcodeInfoTable = new Dictionary<byte, OpcodeInfo>(256);

        for (int i = 0; i < 256; i++)
        {
            var opcode = (byte)i;
            var info = GetOpcodeInfoFromHandler(opcodeTable.GetHandler(opcode));
            opcodeInfoTable[opcode] = info;
        }

        return opcodeInfoTable;
    }

    /// <summary>
    /// Builds a complete opcode information table as an array by analyzing the opcode handlers.
    /// </summary>
    /// <param name="opcodeTable">The opcode table to analyze.</param>
    /// <returns>
    /// An array of 256 <see cref="OpcodeInfo"/> structures where each index is an opcode (0x00-0xFF).
    /// Invalid or unimplemented opcodes will have <see cref="CpuInstructions.None"/> as the instruction.
    /// </returns>
    /// <remarks>
    /// This method provides an alternative to <see cref="BuildOpcodeInfoTable"/> that returns
    /// an array for potentially faster O(1) lookups. The analysis is performed dynamically
    /// using reflection, ensuring the disassembler does not rely on statically created arrays.
    /// </remarks>
    public static OpcodeInfo[] BuildOpcodeInfoArray(OpcodeTable opcodeTable)
    {
        ArgumentNullException.ThrowIfNull(opcodeTable);

        var opcodeInfoArray = new OpcodeInfo[256];

        for (int i = 0; i < 256; i++)
        {
            opcodeInfoArray[i] = GetOpcodeInfoFromHandler(opcodeTable.GetHandler((byte)i));
        }

        return opcodeInfoArray;
    }

    /// <summary>
    /// Creates the instruction enum map mapping method names to enum values.
    /// </summary>
    /// <returns>A dictionary mapping method names to instruction enum values.</returns>
    private static Dictionary<string, CpuInstructions> CreateInstructionEnumMap()
    {
        var map = new Dictionary<string, CpuInstructions>(StringComparer.Ordinal);

        // Map all instruction names from the enum
        foreach (CpuInstructions instruction in Enum.GetValues<CpuInstructions>().Where(i => i != CpuInstructions.None))
        {
            var name = instruction.ToString();
            map[name] = instruction;
        }

        // Add variants for accumulator operations (e.g., ASLa -> ASL)
        map["ASLa"] = CpuInstructions.ASL;
        map["LSRa"] = CpuInstructions.LSR;
        map["ROLa"] = CpuInstructions.ROL;
        map["RORa"] = CpuInstructions.ROR;

        return map;
    }

    /// <summary>
    /// Extracts complete opcode information from an opcode handler by inspecting its captured delegates.
    /// </summary>
    /// <param name="handler">The opcode handler to analyze.</param>
    /// <returns>
    /// An <see cref="OpcodeInfo"/> structure containing the instruction, addressing mode, and operand length.
    /// Returns a default <see cref="OpcodeInfo"/> with <see cref="CpuInstructions.None"/> if the handler
    /// cannot be analyzed.
    /// </returns>
    private static OpcodeInfo GetOpcodeInfoFromHandler(OpcodeHandler? handler)
    {
        // The handler is a closure - its Target is the compiler-generated display class
        object? target = handler?.Target;
        if (target is null)
        {
            // Static method (like IllegalOpcode) - no captured state
            return default;
        }

        // Extract the instruction from the handler's declaring type name
        // The handler is generated from Instructions.LDA, Instructions.STA, etc.
        var instruction = GetInstructionFromHandler(handler!); // We already checked for null above.

        // Find the captured AddressingModeHandler delegate field
        var (addressingMode, operandLength) = GetAddressingModeFromTarget(target);

        return new(instruction, addressingMode, operandLength);
    }

    /// <summary>
    /// Extracts the instruction mnemonic from an opcode handler.
    /// </summary>
    /// <param name="handler">The opcode handler to analyze.</param>
    /// <returns>The instruction mnemonic, or <see cref="CpuInstructions.None"/> if not found.</returns>
    private static CpuInstructions GetInstructionFromHandler(OpcodeHandler handler)
    {
        // The handler's Method is the lambda, but we need the method that created the lambda
        // For closures created by Instructions.LDA, the closure's type name contains the method name
        var target = handler.Target;
        if (target == null)
        {
            return CpuInstructions.None;
        }

        // Get the closure type name which includes the parent method name
        // Format is like <>c__DisplayClass0_0 or similar, but method info is in the handler
        var method = handler.Method;

        // The method name will be something like "<LDA>b__0" for a lambda inside LDA
        string methodName = method.Name;

        // Extract the instruction name from the lambda naming pattern.
        // Pattern: "<InstructionName>b__X" where X is a number
        if (methodName.Length > 2 && methodName[0] == '<')
        {
            int endIndex = methodName.IndexOf('>', 1);
            if (endIndex > 1)
            {
                // Extract instruction name and look it up directly
                var instructionName = methodName.Substring(1, endIndex - 1);
                if (InstructionEnumMap.TryGetValue(instructionName, out var instruction))
                {
                    return instruction;
                }
            }
        }

        return CpuInstructions.None;
    }

    /// <summary>
    /// Extracts the addressing mode and operand length from the closure's captured state.
    /// </summary>
    /// <param name="target">The closure target containing captured variables.</param>
    /// <returns>A tuple containing the addressing mode and operand length.</returns>
    private static (CpuAddressingModes AddressingMode, byte OperandLength) GetAddressingModeFromTarget(object target)
    {
        var targetType = target.GetType();
        var addressingModeDelegate = targetType
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.FieldType == typeof(AddressingModeHandler<CpuState>))
            .Select(f => f.GetValue(target) as AddressingModeHandler<CpuState>)
            .FirstOrDefault(d => d is not null);

        if (addressingModeDelegate is not null)
        {
            string methodName = addressingModeDelegate.Method.Name;

            var addressingMode = CpuAddressingModes.None;
            byte operandLength = 0;

            if (AddressingModeEnumMap.TryGetValue(methodName, out var mode))
            {
                addressingMode = mode;
            }

            if (AddressingModeOperandLengths.TryGetValue(methodName, out var length))
            {
                operandLength = length;
            }

            return (addressingMode, operandLength);
        }

        return (CpuAddressingModes.None, 0);
    }
}