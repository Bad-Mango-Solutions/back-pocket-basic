// <copyright file="Disassembler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Debugging;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Core;
using Core.Cpu;
using Core.Interfaces;

/// <summary>
/// Disassembles machine code from memory into a list of structured instruction representations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to walk through memory regions and decode machine code
/// into <see cref="DisassembledInstruction"/> objects. It uses the <see cref="OpcodeTableAnalyzer"/>
/// to dynamically extract opcode information from the CPU's opcode table, ensuring the
/// disassembler stays in sync with the actual instruction implementations.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
/// var disassembler = new Disassembler(opcodeTable, bus);
/// var instructions = disassembler.Disassemble(0x1000, 16);
/// foreach (var instr in instructions)
/// {
///     Console.WriteLine($"${instr.Address:X4}: {instr.FormatBytes(),-12} {instr.FormatInstruction()}");
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class Disassembler : IDisassembler
{
    private readonly Func<uint, byte> readByte;
    private readonly OpcodeInfo[] opcodeInfoTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="Disassembler"/> class using an <see cref="IMemoryBus"/>.
    /// </summary>
    /// <param name="opcodeTable">The opcode table to use for disassembly.</param>
    /// <param name="bus">The memory bus to read from.</param>
    /// <exception cref="ArgumentNullException">Thrown when opcodeTable or bus is null.</exception>
    public Disassembler(OpcodeTable opcodeTable, IMemoryBus bus)
        : this(BuildOpcodeInfoArray(opcodeTable), CreateReadFunc(bus))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Disassembler"/> class with a pre-built opcode info array.
    /// </summary>
    /// <param name="opcodeInfoArray">The pre-built opcode info array (256 elements).</param>
    /// <param name="bus">The memory bus to read from.</param>
    /// <exception cref="ArgumentNullException">Thrown when opcodeInfoArray or bus is null.</exception>
    /// <exception cref="ArgumentException">Thrown when opcodeInfoArray is not exactly 256 elements.</exception>
    public Disassembler(OpcodeInfo[] opcodeInfoArray, IMemoryBus bus)
        : this(opcodeInfoArray, CreateReadFunc(bus))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Disassembler"/> class with a custom read function.
    /// </summary>
    /// <param name="opcodeInfoArray">The pre-built opcode info array (256 elements).</param>
    /// <param name="readByte">A function that reads a byte from the given address.</param>
    /// <exception cref="ArgumentNullException">Thrown when opcodeInfoArray or readByte is null.</exception>
    /// <exception cref="ArgumentException">Thrown when opcodeInfoArray is not exactly 256 elements.</exception>
    private Disassembler(OpcodeInfo[] opcodeInfoArray, Func<uint, byte> readByte)
    {
        ArgumentNullException.ThrowIfNull(opcodeInfoArray);
        ArgumentNullException.ThrowIfNull(readByte);

        if (opcodeInfoArray.Length != 256)
        {
            throw new ArgumentException("Opcode info array must have exactly 256 entries.", nameof(opcodeInfoArray));
        }

        this.readByte = readByte;
        opcodeInfoTable = opcodeInfoArray;
    }

    /// <inheritdoc />
    public IReadOnlyList<DisassembledInstruction> Disassemble(uint startAddress, int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

        var instructions = new List<DisassembledInstruction>();
        uint currentAddress = startAddress;
        uint endAddress = startAddress + (uint)byteCount;

        while (currentAddress < endAddress)
        {
            var instruction = DisassembleInstruction(currentAddress);
            instructions.Add(instruction);
            currentAddress += (uint)instruction.TotalLength;
        }

        return instructions;
    }

    /// <inheritdoc />
    public IReadOnlyList<DisassembledInstruction> DisassembleInstructions(uint startAddress, int instructionCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(instructionCount);

        var instructions = new List<DisassembledInstruction>(instructionCount);
        uint currentAddress = startAddress;

        for (int i = 0; i < instructionCount; i++)
        {
            var instruction = DisassembleInstruction(currentAddress);
            instructions.Add(instruction);
            currentAddress += (uint)instruction.TotalLength;
        }

        return instructions;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="OperandBuffer"/> to avoid heap allocation for operand bytes.
    /// </remarks>
    public DisassembledInstruction DisassembleInstruction(uint address)
    {
        byte opcode = readByte(address);
        var opcodeInfo = opcodeInfoTable[opcode];

        // Read operand bytes into the fixed-size buffer
        OperandBuffer operandBuffer = default;
        for (int i = 0; i < opcodeInfo.OperandLength; i++)
        {
            operandBuffer[i] = readByte(address + 1 + (uint)i);
        }

        return new DisassembledInstruction(
            address,
            opcode,
            operandBuffer,
            opcodeInfo.OperandLength,
            opcodeInfo.Instruction,
            opcodeInfo.AddressingMode);
    }

    /// <inheritdoc />
    public IReadOnlyList<DisassembledInstruction> DisassembleRange(uint startAddress, uint endAddress) =>
        endAddress < startAddress
            ? throw new ArgumentException("End address must be greater than or equal to start address.", nameof(endAddress))
            : Disassemble(startAddress, (int)(endAddress - startAddress));

    private static OpcodeInfo[] BuildOpcodeInfoArray(OpcodeTable opcodeTable)
    {
        ArgumentNullException.ThrowIfNull(opcodeTable);
        return OpcodeTableAnalyzer.BuildOpcodeInfoArray(opcodeTable);
    }

    private static Func<uint, byte> CreateReadFunc(IMemoryBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return address =>
        {
            var access = new BusAccess(
                Address: address,
                Value: 0,
                WidthBits: 8,
                Mode: BusAccessMode.Decomposed,
                EmulationFlag: true,
                Intent: AccessIntent.DebugRead,
                SourceId: 0,
                Cycle: 0,
                Flags: AccessFlags.NoSideEffects);

            var result = bus.TryRead8(access);
            return result.Ok ? result.Value : (byte)0xFF;
        };
    }
}