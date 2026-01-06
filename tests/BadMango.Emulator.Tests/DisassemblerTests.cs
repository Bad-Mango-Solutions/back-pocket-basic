// <copyright file="DisassemblerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Bus.Interfaces;

using Core;
using Core.Cpu;
using TestHelpers;

using Emulation.Cpu;
using Emulation.Debugging;


/// <summary>
/// Unit tests for the disassembler helper functionality.
/// </summary>
[TestFixture]
public class DisassemblerTests : CpuTestBase
{
    
    private OpcodeTable opcodeTable = null!;
    private Disassembler disassembler = null!;

    /// <summary>
    /// Sets up test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        
        opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        disassembler = new Disassembler(opcodeTable, Bus);
    }

    #region OpcodeInfo Tests

    /// <summary>
    /// Verifies that OpcodeInfo correctly represents a valid instruction.
    /// </summary>
    [Test]
    public void OpcodeInfo_ValidInstruction_HasCorrectProperties()
    {
        // Arrange & Act
        var info = new OpcodeInfo(CpuInstructions.LDA, CpuAddressingModes.Immediate, 1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.Instruction, Is.EqualTo(CpuInstructions.LDA));
            Assert.That(info.AddressingMode, Is.EqualTo(CpuAddressingModes.Immediate));
            Assert.That(info.OperandLength, Is.EqualTo(1));
            Assert.That(info.IsValid, Is.True);
            Assert.That(info.TotalLength, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Verifies that OpcodeInfo correctly identifies invalid instructions.
    /// </summary>
    [Test]
    public void OpcodeInfo_InvalidInstruction_IsValidReturnsFalse()
    {
        // Arrange & Act
        var info = new OpcodeInfo(CpuInstructions.None, CpuAddressingModes.None, 0);

        // Assert
        Assert.That(info.IsValid, Is.False);
    }

    /// <summary>
    /// Verifies that OpcodeInfo TotalLength includes opcode and operands.
    /// </summary>
    /// <param name="mode">The addressing mode to test.</param>
    /// <param name="operandLength">The expected operand length.</param>
    /// <param name="expectedTotal">The expected total instruction length.</param>
    [TestCase(CpuAddressingModes.Implied, (byte)0, 1)]
    [TestCase(CpuAddressingModes.Immediate, (byte)1, 2)]
    [TestCase(CpuAddressingModes.Absolute, (byte)2, 3)]
    public void OpcodeInfo_TotalLength_IncludesOpcodeAndOperands(CpuAddressingModes mode, byte operandLength, int expectedTotal)
    {
        // Arrange & Act
        var info = new OpcodeInfo(CpuInstructions.LDA, mode, operandLength);

        // Assert
        Assert.That(info.TotalLength, Is.EqualTo(expectedTotal));
    }

    #endregion

    #region OpcodeTableAnalyzer Tests

    /// <summary>
    /// Verifies that BuildOpcodeInfoTable returns a dictionary with 256 entries.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoTable_ReturnsCompleteTable()
    {
        // Act
        var table = OpcodeTableAnalyzer.BuildOpcodeInfoTable(opcodeTable);

        // Assert
        Assert.That(table.Count, Is.EqualTo(256));
    }

    /// <summary>
    /// Verifies that BuildOpcodeInfoArray returns an array with 256 entries.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoArray_ReturnsCompleteArray()
    {
        // Act
        var array = OpcodeTableAnalyzer.BuildOpcodeInfoArray(opcodeTable);

        // Assert
        Assert.That(array.Length, Is.EqualTo(256));
    }

    /// <summary>
    /// Verifies that LDA Immediate opcode (0xA9) is correctly identified.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoTable_LdaImmediate_HasCorrectInfo()
    {
        // Act
        var table = OpcodeTableAnalyzer.BuildOpcodeInfoTable(opcodeTable);
        var info = table[0xA9];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.Instruction, Is.EqualTo(CpuInstructions.LDA));
            Assert.That(info.AddressingMode, Is.EqualTo(CpuAddressingModes.Immediate));
            Assert.That(info.OperandLength, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that STA Absolute opcode (0x8D) is correctly identified.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoTable_StaAbsolute_HasCorrectInfo()
    {
        // Act
        var table = OpcodeTableAnalyzer.BuildOpcodeInfoTable(opcodeTable);
        var info = table[0x8D];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.Instruction, Is.EqualTo(CpuInstructions.STA));
            Assert.That(info.AddressingMode, Is.EqualTo(CpuAddressingModes.Absolute));
            Assert.That(info.OperandLength, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Verifies that NOP opcode (0xEA) is correctly identified.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoTable_Nop_HasCorrectInfo()
    {
        // Act
        var table = OpcodeTableAnalyzer.BuildOpcodeInfoTable(opcodeTable);
        var info = table[0xEA];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.Instruction, Is.EqualTo(CpuInstructions.NOP));
            Assert.That(info.AddressingMode, Is.EqualTo(CpuAddressingModes.Implied));
            Assert.That(info.OperandLength, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that branch instructions have relative addressing mode.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoTable_BranchInstructions_HaveRelativeMode()
    {
        // Act
        var table = OpcodeTableAnalyzer.BuildOpcodeInfoTable(opcodeTable);

        // Assert - BEQ (0xF0) and BNE (0xD0)
        Assert.Multiple(() =>
        {
            Assert.That(table[0xF0].Instruction, Is.EqualTo(CpuInstructions.BEQ));
            Assert.That(table[0xF0].AddressingMode, Is.EqualTo(CpuAddressingModes.Relative));
            Assert.That(table[0xD0].Instruction, Is.EqualTo(CpuInstructions.BNE));
            Assert.That(table[0xD0].AddressingMode, Is.EqualTo(CpuAddressingModes.Relative));
        });
    }

    /// <summary>
    /// Verifies that ASL Accumulator opcode (0x0A) is correctly identified.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoTable_AslAccumulator_HasCorrectInfo()
    {
        // Act
        var table = OpcodeTableAnalyzer.BuildOpcodeInfoTable(opcodeTable);
        var info = table[0x0A];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.Instruction, Is.EqualTo(CpuInstructions.ASL));
            Assert.That(info.AddressingMode, Is.EqualTo(CpuAddressingModes.Accumulator));
            Assert.That(info.OperandLength, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that illegal opcodes have None instruction.
    /// </summary>
    [Test]
    public void BuildOpcodeInfoTable_IllegalOpcode_HasNoneInstruction()
    {
        // Act
        var table = OpcodeTableAnalyzer.BuildOpcodeInfoTable(opcodeTable);

        // 0x02 is an illegal opcode on 65C02
        var info = table[0x02];

        // Assert
        Assert.That(info.Instruction, Is.EqualTo(CpuInstructions.None));
    }

    #endregion

    #region DisassembledInstruction Tests

    /// <summary>
    /// Verifies that DisassembledInstruction correctly formats immediate addressing.
    /// </summary>
    [Test]
    public void DisassembledInstruction_ImmediateMode_FormatsCorrectly()
    {
        // Arrange
        var operandBuffer = new OperandBuffer { [0] = 0x42 };
        var instruction = new DisassembledInstruction(
            0x1000,
            0xA9,
            operandBuffer,
            1,
            CpuInstructions.LDA,
            CpuAddressingModes.Immediate);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(instruction.FormatOperand(), Is.EqualTo("#$42"));
            Assert.That(instruction.FormatInstruction(), Is.EqualTo("LDA #$42"));
            Assert.That(instruction.FormatBytes(), Is.EqualTo("A9 42"));
            Assert.That(instruction.TotalLength, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Verifies that DisassembledInstruction correctly formats absolute addressing.
    /// </summary>
    [Test]
    public void DisassembledInstruction_AbsoluteMode_FormatsCorrectly()
    {
        // Arrange
        var operandBuffer = new OperandBuffer { [0] = 0x00, [1] = 0x02 };
        var instruction = new DisassembledInstruction(
            0x1002,
            0x8D,
            operandBuffer,
            2,
            CpuInstructions.STA,
            CpuAddressingModes.Absolute);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(instruction.FormatOperand(), Is.EqualTo("$0200"));
            Assert.That(instruction.FormatInstruction(), Is.EqualTo("STA $0200"));
            Assert.That(instruction.FormatBytes(), Is.EqualTo("8D 00 02"));
            Assert.That(instruction.TotalLength, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// Verifies that DisassembledInstruction correctly formats implied addressing.
    /// </summary>
    [Test]
    public void DisassembledInstruction_ImpliedMode_FormatsCorrectly()
    {
        // Arrange
        var instruction = new DisassembledInstruction(
            0x1005,
            0xEA,
            default,
            0,
            CpuInstructions.NOP,
            CpuAddressingModes.Implied);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(instruction.FormatOperand(), Is.EqualTo(string.Empty));
            Assert.That(instruction.FormatInstruction(), Is.EqualTo("NOP"));
            Assert.That(instruction.FormatBytes(), Is.EqualTo("EA"));
            Assert.That(instruction.TotalLength, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that DisassembledInstruction correctly formats relative addressing.
    /// </summary>
    [Test]
    public void DisassembledInstruction_RelativeMode_FormatsCorrectly()
    {
        // Arrange - Branch forward by 10 bytes from address 0x1000
        var operandBuffer = new OperandBuffer { [0] = 0x0A };
        var instruction = new DisassembledInstruction(
            0x1000,
            0xF0,
            operandBuffer,
            1,
            CpuInstructions.BEQ,
            CpuAddressingModes.Relative);

        // Assert - Target should be 0x1000 + 2 (instruction length) + 10 = 0x100C
        Assert.Multiple(() =>
        {
            Assert.That(instruction.FormatOperand(), Is.EqualTo("$100C"));
            Assert.That(instruction.FormatInstruction(), Is.EqualTo("BEQ $100C"));
        });
    }

    /// <summary>
    /// Verifies that DisassembledInstruction correctly formats negative relative addressing.
    /// </summary>
    [Test]
    public void DisassembledInstruction_RelativeModeNegative_FormatsCorrectly()
    {
        // Arrange - Branch backward by 6 bytes from address 0x1010
        var operandBuffer = new OperandBuffer { [0] = 0xFA }; // -6 as signed byte
        var instruction = new DisassembledInstruction(
            0x1010,
            0xD0,
            operandBuffer,
            1,
            CpuInstructions.BNE,
            CpuAddressingModes.Relative);

        // Assert - Target should be 0x1010 + 2 - 6 = 0x100C
        Assert.Multiple(() =>
        {
            Assert.That(instruction.FormatOperand(), Is.EqualTo("$100C"));
            Assert.That(instruction.FormatInstruction(), Is.EqualTo("BNE $100C"));
        });
    }

    /// <summary>
    /// Verifies that DisassembledInstruction correctly formats indexed addressing.
    /// </summary>
    [Test]
    public void DisassembledInstruction_IndexedModes_FormatCorrectly()
    {
        // Test ZeroPage,X
        var zpxBuffer = new OperandBuffer { [0] = 0x50 };
        var zpx = new DisassembledInstruction(0x1000, 0xB5, zpxBuffer, 1, CpuInstructions.LDA, CpuAddressingModes.ZeroPageX);
        Assert.That(zpx.FormatOperand(), Is.EqualTo("$50,X"));

        // Test Absolute,Y
        var abyBuffer = new OperandBuffer { [0] = 0x00, [1] = 0x20 };
        var aby = new DisassembledInstruction(0x1000, 0xB9, abyBuffer, 2, CpuInstructions.LDA, CpuAddressingModes.AbsoluteY);
        Assert.That(aby.FormatOperand(), Is.EqualTo("$2000,Y"));

        // Test (Indirect,X)
        var indxBuffer = new OperandBuffer { [0] = 0x40 };
        var indx = new DisassembledInstruction(0x1000, 0xA1, indxBuffer, 1, CpuInstructions.LDA, CpuAddressingModes.IndirectX);
        Assert.That(indx.FormatOperand(), Is.EqualTo("($40,X)"));

        // Test (Indirect),Y
        var indyBuffer = new OperandBuffer { [0] = 0x40 };
        var indy = new DisassembledInstruction(0x1000, 0xB1, indyBuffer, 1, CpuInstructions.LDA, CpuAddressingModes.IndirectY);
        Assert.That(indy.FormatOperand(), Is.EqualTo("($40),Y"));
    }

    /// <summary>
    /// Verifies that DisassembledInstruction GetAllBytes returns correct bytes.
    /// </summary>
    [Test]
    public void DisassembledInstruction_GetAllBytes_ReturnsOpcodeAndOperands()
    {
        // Arrange
        var operandBuffer = new OperandBuffer { [0] = 0x00, [1] = 0x02 };
        var instruction = new DisassembledInstruction(
            0x1000,
            0x8D,
            operandBuffer,
            2,
            CpuInstructions.STA,
            CpuAddressingModes.Absolute);

        // Act
        var bytes = instruction.GetAllBytes();

        // Assert
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x8D, 0x00, 0x02 }));
    }

    /// <summary>
    /// Verifies that DisassembledInstruction Metadata dictionary is available.
    /// </summary>
    [Test]
    public void DisassembledInstruction_Metadata_IsExtensible()
    {
        // Arrange
        var operandBuffer = new OperandBuffer { [0] = 0x42 };
        var instruction = new DisassembledInstruction(
            0x1000,
            0xA9,
            operandBuffer,
            1,
            CpuInstructions.LDA,
            CpuAddressingModes.Immediate);

        // Act
        instruction.Metadata["Comment"] = "Load value 0x42 into accumulator";
        instruction.Metadata["RegisterA"] = (byte)0x42;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(instruction.Metadata["Comment"], Is.EqualTo("Load value 0x42 into accumulator"));
            Assert.That(instruction.Metadata["RegisterA"], Is.EqualTo((byte)0x42));
        });
    }

    #endregion

    #region Disassembler Tests

    /// <summary>
    /// Verifies that Disassembler can disassemble a single instruction.
    /// </summary>
    [Test]
    public void Disassembler_DisassembleInstruction_DecodesCorrectly()
    {
        // Arrange
        Write(0x1000, 0xA9); // LDA #
        Write(0x1001, 0x42); // $42

        // Act
        var instruction = disassembler.DisassembleInstruction(0x1000);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(instruction.Address, Is.EqualTo(0x1000u));
            Assert.That(instruction.Opcode, Is.EqualTo(0xA9));
            Assert.That(instruction.OperandLength, Is.EqualTo(1));
            Assert.That(instruction.Operands[0], Is.EqualTo(0x42));
            Assert.That(instruction.Instruction, Is.EqualTo(CpuInstructions.LDA));
            Assert.That(instruction.AddressingMode, Is.EqualTo(CpuAddressingModes.Immediate));
        });
    }

    /// <summary>
    /// Verifies that Disassembler can disassemble multiple instructions.
    /// </summary>
    [Test]
    public void Disassembler_Disassemble_DecodesMultipleInstructions()
    {
        // Arrange - Write a simple program
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);

        Write(0x1002, 0x8D); // STA $0200
        Write(0x1003, 0x00);
        Write(0x1004, 0x02);

        Write(0x1005, 0xA2); // LDX #$10
        Write(0x1006, 0x10);

        // Act
        var instructions = disassembler.Disassemble(0x1000, 7);

        // Assert
        Assert.That(instructions.Count, Is.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(instructions[0].Instruction, Is.EqualTo(CpuInstructions.LDA));
            Assert.That(instructions[0].Address, Is.EqualTo(0x1000u));
            Assert.That(instructions[1].Instruction, Is.EqualTo(CpuInstructions.STA));
            Assert.That(instructions[1].Address, Is.EqualTo(0x1002u));
            Assert.That(instructions[2].Instruction, Is.EqualTo(CpuInstructions.LDX));
            Assert.That(instructions[2].Address, Is.EqualTo(0x1005u));
        });
    }

    /// <summary>
    /// Verifies that DisassembleInstructions returns exact count of instructions.
    /// </summary>
    [Test]
    public void Disassembler_DisassembleInstructions_ReturnsExactCount()
    {
        // Arrange
        Write(0x1000, 0xEA); // NOP
        Write(0x1001, 0xEA); // NOP
        Write(0x1002, 0xEA); // NOP
        Write(0x1003, 0xEA); // NOP
        Write(0x1004, 0xEA); // NOP

        // Act
        var instructions = disassembler.DisassembleInstructions(0x1000, 3);

        // Assert
        Assert.That(instructions.Count, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that DisassembleRange handles address range correctly.
    /// </summary>
    [Test]
    public void Disassembler_DisassembleRange_HandlesRangeCorrectly()
    {
        // Arrange
        Write(0x1000, 0xA9); // LDA #
        Write(0x1001, 0x42);
        Write(0x1002, 0xEA); // NOP

        // Act
        var instructions = disassembler.DisassembleRange(0x1000, 0x1003);

        // Assert
        Assert.That(instructions.Count, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that Disassembler handles implied mode instructions.
    /// </summary>
    [Test]
    public void Disassembler_ImpliedModeInstruction_HasNoOperands()
    {
        // Arrange
        Write(0x1000, 0x18); // CLC

        // Act
        var instruction = disassembler.DisassembleInstruction(0x1000);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(instruction.Instruction, Is.EqualTo(CpuInstructions.CLC));
            Assert.That(instruction.AddressingMode, Is.EqualTo(CpuAddressingModes.Implied));
            Assert.That(instruction.OperandLength, Is.EqualTo(0));
            Assert.That(instruction.TotalLength, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that Disassembler handles illegal opcodes.
    /// </summary>
    [Test]
    public void Disassembler_IllegalOpcode_HasNoneInstruction()
    {
        // Arrange
        Write(0x1000, 0x02); // Illegal opcode

        // Act
        var instruction = disassembler.DisassembleInstruction(0x1000);

        // Assert
        Assert.That(instruction.Instruction, Is.EqualTo(CpuInstructions.None));
    }

    /// <summary>
    /// Verifies that Disassembler correctly handles the complete program from the issue.
    /// </summary>
    [Test]
    public void Disassembler_ExampleFromIssue_FormatsCorrectly()
    {
        // Arrange - Program from the issue description
        // $1000: A9 42    LDA #$42
        // $1002: 8D 00 02 STA $0200
        // $1005: A2 10    LDX #$10
        Write(0x1000, 0xA9);
        Write(0x1001, 0x42);
        Write(0x1002, 0x8D);
        Write(0x1003, 0x00);
        Write(0x1004, 0x02);
        Write(0x1005, 0xA2);
        Write(0x1006, 0x10);

        // Act
        var instructions = disassembler.Disassemble(0x1000, 7);

        // Assert - Verify we can format like the issue example
        Assert.That(instructions.Count, Is.EqualTo(3));

        var line1 = $"${instructions[0].Address:X4}: {instructions[0].FormatBytes(),-9}{instructions[0].FormatInstruction()}";
        var line2 = $"${instructions[1].Address:X4}: {instructions[1].FormatBytes(),-9}{instructions[1].FormatInstruction()}";
        var line3 = $"${instructions[2].Address:X4}: {instructions[2].FormatBytes(),-9}{instructions[2].FormatInstruction()}";

        Assert.Multiple(() =>
        {
            Assert.That(line1, Is.EqualTo("$1000: A9 42    LDA #$42"));
            Assert.That(line2, Is.EqualTo("$1002: 8D 00 02 STA $0200"));
            Assert.That(line3, Is.EqualTo("$1005: A2 10    LDX #$10"));
        });
    }

    /// <summary>
    /// Verifies that Disassembler constructor validates null arguments.
    /// </summary>
    [Test]
    public void Disassembler_NullOpcodeTable_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Disassembler((OpcodeTable)null!, Bus));
    }

    /// <summary>
    /// Verifies that Disassembler constructor validates null memory bus.
    /// </summary>
    [Test]
    public void Disassembler_NullMemoryBus_ThrowsArgumentNullException()
    {
        // Act & Assert
        IMemoryBus? nullBus = null;
        Assert.Throws<ArgumentNullException>(() => new Disassembler(opcodeTable, nullBus!));
    }

    /// <summary>
    /// Verifies that DisassembleRange validates end address.
    /// </summary>
    [Test]
    public void Disassembler_DisassembleRange_InvalidRange_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => disassembler.DisassembleRange(0x1010, 0x1000));
    }

    #endregion
}