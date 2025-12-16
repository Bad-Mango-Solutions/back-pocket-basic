// <copyright file="Cpu6502EmulationTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Emulation;

using Microsoft.Extensions.Logging;

using Moq;

/// <summary>
/// Contains unit tests for verifying the behavior and functionality of the emulation components,
/// including memory operations and CPU instructions, within the Applesoft BASIC emulator.
/// </summary>
[TestFixture]
public class Cpu6502EmulationTests
{
    private AppleMemory memory = null!;
    private Cpu6502 cpu = null!;

    /// <summary>
    /// Sets up the test environment by initializing the emulated memory and CPU components
    /// required for the Applesoft BASIC emulator tests.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        var cpuLogger = new Mock<ILogger<Cpu6502>>();

        memory = new(memoryLogger.Object);
        cpu = new(memory, cpuLogger.Object);
    }

    /// <summary>
    /// Verifies that the <see cref="AppleMemory"/> class correctly handles basic read and write operations
    /// to the emulated memory space.
    /// </summary>
    /// <remarks>
    /// This test writes a byte value to a specific memory address and ensures that the same value
    /// can be read back from the same address, validating the integrity of memory operations.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the value read from memory does not match the value written to it.
    /// </exception>
    [Test]
    public void Memory_ReadWrite_WorksCorrectly()
    {
        memory.Write(0x300, 0x42);

        Assert.That(memory.Read(0x300), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that the <see cref="AppleMemory"/> class correctly handles reading and writing
    /// 16-bit words to and from memory, ensuring data integrity and proper byte order.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="AppleMemory.WriteWord(int, ushort)"/> method writes
    /// the lower and higher bytes of a 16-bit word to consecutive memory addresses, and that
    /// the <see cref="AppleMemory.ReadWord(int)"/> method correctly reconstructs the word from
    /// these bytes. Additionally, it validates that individual byte reads return the expected values.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the written and read values do not match, or if the individual byte reads
    /// return incorrect values.
    /// </exception>
    [Test]
    public void Memory_ReadWriteWord_WorksCorrectly()
    {
        memory.WriteWord(0x300, 0x1234);

        Assert.That(memory.ReadWord(0x300), Is.EqualTo(0x1234));
        Assert.That(memory.Read(0x300), Is.EqualTo(0x34)); // Low byte
        Assert.That(memory.Read(0x301), Is.EqualTo(0x12)); // High byte
    }

    /// <summary>
    /// Verifies that attempting to access memory outside the valid address range
    /// in the Applesoft BASIC emulator throws a <see cref="MemoryAccessException"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="AppleMemory"/> class correctly enforces
    /// memory bounds by throwing exceptions when invalid addresses are accessed.
    /// </remarks>
    /// <exception cref="MemoryAccessException">
    /// Thrown when attempting to read from or write to an address outside the valid memory range.
    /// </exception>
    [Test]
    public void Memory_OutOfBounds_ThrowsException()
    {
        Assert.Throws<MemoryAccessException>(() => memory.Read(0x10000));
        Assert.Throws<MemoryAccessException>(() => memory.Write(0x10000, 0));
        Assert.Throws<MemoryAccessException>(() => memory.Read(-1));
    }

    /// <summary>
    /// Verifies that the <see cref="AppleMemory.Clear"/> method correctly sets all memory bytes to zero.
    /// </summary>
    /// <remarks>
    /// This test ensures that after invoking the <see cref="AppleMemory.Clear"/> method, all previously written values
    /// in the memory are reset to zero, validating the proper functionality of the memory clearing operation.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the memory at the specified address does not contain the expected value of zero after clearing.
    /// </exception>
    [Test]
    public void Memory_Clear_ZerosMemory()
    {
        memory.Write(0x300, 0xFF);
        memory.Clear();

        Assert.That(memory.Read(0x300), Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that the <see cref="AppleMemory.LoadData(int, byte[])"/> method correctly writes
    /// the provided data to the specified memory addresses.
    /// </summary>
    /// <remarks>
    /// This test ensures that each byte in the input data array is written to consecutive memory
    /// locations starting from the specified address.
    /// </remarks>
    [Test]
    public void Memory_LoadData_WritesCorrectly()
    {
        byte[] data = { 0x01, 0x02, 0x03, 0x04 };
        memory.LoadData(0x300, data);

        Assert.That(memory.Read(0x300), Is.EqualTo(0x01));
        Assert.That(memory.Read(0x301), Is.EqualTo(0x02));
        Assert.That(memory.Read(0x302), Is.EqualTo(0x03));
        Assert.That(memory.Read(0x303), Is.EqualTo(0x04));
    }

    /// <summary>
    /// Verifies that the <see cref="Cpu6502.Reset"/> method correctly initializes the CPU registers
    /// to their default values.
    /// </summary>
    /// <remarks>
    /// This test ensures that after invoking the <see cref="Cpu6502.Reset"/> method, the CPU registers
    /// are set to their expected initial states:
    /// <list type="bullet">
    /// <item><description>The accumulator register (<see cref="Cpu6502Registers.A"/>) is set to 0.</description></item>
    /// <item><description>The X register (<see cref="Cpu6502Registers.X"/>) is set to 0.</description></item>
    /// <item><description>The Y register (<see cref="Cpu6502Registers.Y"/>) is set to 0.</description></item>
    /// <item><description>The stack pointer (<see cref="Cpu6502Registers.SP"/>) is set to 0xFF.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if any of the CPU registers are not initialized to their expected values after reset.
    /// </exception>
    [Test]
    public void Cpu_Reset_InitializesRegisters()
    {
        cpu.Reset();

        Assert.That(cpu.Registers.A, Is.EqualTo(0));
        Assert.That(cpu.Registers.X, Is.EqualTo(0));
        Assert.That(cpu.Registers.Y, Is.EqualTo(0));
        Assert.That(cpu.Registers.SP, Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Tests the LDA (Load Accumulator) immediate instruction of the 6502 CPU emulation.
    /// </summary>
    /// <remarks>
    /// This test verifies that the LDA immediate instruction correctly loads a specified value
    /// into the accumulator register (A). The test writes the instruction and operand into memory,
    /// sets the program counter (PC) to the instruction's address, and executes a CPU step.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the value loaded into the accumulator does not match the expected value.
    /// </exception>
    [Test]
    public void Cpu_LdaImmediate_LoadsValue()
    {
        // LDA #$42
        memory.Write(0x0300, 0xA9); // LDA immediate
        memory.Write(0x0301, 0x42); // Value
        memory.Write(0x0302, 0x00); // BRK

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(cpu.Registers.A, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Tests the STA (Store Accumulator) instruction with a zero-page addressing mode
    /// to ensure it correctly stores the value from the accumulator into the specified memory address.
    /// </summary>
    /// <remarks>
    /// This test verifies that the STA instruction:
    /// - Stores the value of the accumulator register (A) into the memory address specified by the operand.
    /// - Uses the zero-page addressing mode, which targets addresses in the range 0x0000 to 0x00FF.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the value stored in memory does not match the expected value from the accumulator.
    /// </exception>
    [Test]
    public void Cpu_StaZeroPage_StoresValue()
    {
        // STA $50
        cpu.Registers.A = 0x42;
        memory.Write(0x0300, 0x85); // STA zero page
        memory.Write(0x0301, 0x50); // Address

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(memory.Read(0x50), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Tests the behavior of the INX (Increment X Register) instruction in the 6502 CPU emulation.
    /// </summary>
    /// <remarks>
    /// This test verifies that the X register is correctly incremented by 1 when the INX instruction is executed.
    /// It sets up the CPU state, writes the INX opcode to memory, and steps the CPU to execute the instruction.
    /// </remarks>
    /// <example>
    /// The test initializes the X register to 0x41, executes the INX instruction, and asserts that the X register
    /// is incremented to 0x42.
    /// </example>
    [Test]
    public void Cpu_Inx_IncrementsX()
    {
        cpu.Registers.X = 0x41;
        memory.Write(0x0300, 0xE8); // INX

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(cpu.Registers.X, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Tests the <c>DEX</c> (Decrement X Register) instruction of the 6502 CPU emulation.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <c>DEX</c> instruction correctly decrements the value of the X register
    /// and updates the program counter appropriately.
    /// </remarks>
    /// <example>
    /// The test initializes the X register to <c>0x43</c>, writes the <c>DEX</c> opcode (<c>0xCA</c>) to memory,
    /// sets the program counter to the memory location, and executes a CPU step. It then asserts that the X register
    /// is decremented to <c>0x42</c>.
    /// </example>
    [Test]
    public void Cpu_Dex_DecrementsX()
    {
        cpu.Registers.X = 0x43;
        memory.Write(0x0300, 0xCA); // DEX

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(cpu.Registers.X, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Tests the ADC (Add with Carry) instruction in immediate addressing mode for the 6502 CPU emulation.
    /// </summary>
    /// <remarks>
    /// This test verifies that the ADC instruction correctly adds the accumulator value and the immediate value,
    /// taking into account the carry flag. It ensures the result is stored in the accumulator register.
    /// </remarks>
    /// <example>
    /// Given:
    /// - Accumulator (A) = 0x10
    /// - Carry flag = false
    /// - Immediate value = 0x20
    /// After executing the ADC instruction, the accumulator should hold the value 0x30.
    /// </example>
    [Test]
    public void Cpu_AdcImmediate_AddsWithCarry()
    {
        cpu.Registers.A = 0x10;
        cpu.Registers.Carry = false;
        memory.Write(0x0300, 0x69); // ADC immediate
        memory.Write(0x0301, 0x20);

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(cpu.Registers.A, Is.EqualTo(0x30));
    }

    /// <summary>
    /// Tests the behavior of the BNE (Branch if Not Equal) instruction in the 6502 CPU emulation.
    /// Verifies that the program counter (PC) branches correctly when the Zero flag is not set.
    /// </summary>
    /// <remarks>
    /// This test sets up a scenario where the Zero flag is cleared (false) and a BNE instruction
    /// is executed with a positive offset. It ensures that the program counter is updated correctly
    /// to reflect the branch.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the program counter does not match the expected value after executing the instruction.
    /// </exception>
    [Test]
    public void Cpu_BneRelative_BranchesWhenNotZero()
    {
        cpu.Registers.Zero = false;
        memory.Write(0x0300, 0xD0); // BNE
        memory.Write(0x0301, 0x05); // Offset (+5)

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(cpu.Registers.PC, Is.EqualTo(0x0307)); // 0x302 + 5
    }

    /// <summary>
    /// Tests the behavior of the BEQ (Branch if Equal) instruction when the Zero flag is not set.
    /// </summary>
    /// <remarks>
    /// This test verifies that the program counter (PC) does not branch to the target address
    /// when the Zero flag is false, ensuring correct emulation of the BEQ instruction.
    /// </remarks>
    /// <example>
    /// Given the Zero flag is false and the BEQ instruction is executed:
    /// - The PC should advance to the next instruction instead of branching.
    /// </example>
    [Test]
    public void Cpu_BeqRelative_DoesNotBranchWhenNotZero()
    {
        cpu.Registers.Zero = false;
        memory.Write(0x0300, 0xF0); // BEQ
        memory.Write(0x0301, 0x05); // Offset

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(cpu.Registers.PC, Is.EqualTo(0x0302)); // Just past instruction
    }

    /// <summary>
    /// Verifies that the CPU flags (Zero and Negative) are set correctly after executing the LDA (Load Accumulator) instruction.
    /// </summary>
    /// <remarks>
    /// This test ensures that:
    /// - The Zero flag is set when the loaded value is zero.
    /// - The Negative flag is set when the loaded value has the most significant bit set.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the CPU flags do not match the expected state after executing the LDA instruction.
    /// </exception>
    [Test]
    public void Cpu_FlagsSetCorrectly_AfterLda()
    {
        memory.Write(0x0300, 0xA9); // LDA immediate
        memory.Write(0x0301, 0x00);

        cpu.Registers.PC = 0x0300;
        cpu.Step();

        Assert.That(cpu.Registers.Zero, Is.True);
        Assert.That(cpu.Registers.Negative, Is.False);

        memory.Write(0x0302, 0xA9);
        memory.Write(0x0303, 0x80); // Negative value

        cpu.Step();

        Assert.That(cpu.Registers.Zero, Is.False);
        Assert.That(cpu.Registers.Negative, Is.True);
    }

    /// <summary>
    /// Verifies that the <see cref="Cpu6502Registers.Carry"/> property correctly reflects the state of the carry flag
    /// in the processor status register (<see cref="Cpu6502Registers.P"/>).
    /// </summary>
    /// <remarks>
    /// This test ensures that setting the <see cref="Cpu6502Registers.Carry"/> property to <c>true</c> or <c>false</c>
    /// updates the corresponding bit in the processor status register correctly, and that the property
    /// accurately reflects the current state of the carry flag.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the carry flag state or the corresponding bit in the processor status register
    /// does not match the expected value.
    /// </exception>
    [Test]
    public void CpuRegisters_CarryFlag_WorksCorrectly()
    {
        var registers = new Cpu6502Registers();

        registers.Carry = true;
        Assert.That(registers.Carry, Is.True);
        Assert.That(registers.P & 0x01, Is.EqualTo(0x01));

        registers.Carry = false;
        Assert.That(registers.Carry, Is.False);
        Assert.That(registers.P & 0x01, Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that the <see cref="Cpu6502Registers.Zero"/> property correctly reflects the state of the zero flag
    /// in the 6502 CPU registers.
    /// </summary>
    /// <remarks>
    /// This test ensures that setting the <see cref="Cpu6502Registers.Zero"/> property to <c>true</c> or <c>false</c>
    /// updates the zero flag correctly and that the property returns the expected value.
    /// </remarks>
    /// <exception cref="AssertionException">
    /// Thrown if the <see cref="Cpu6502Registers.Zero"/> property does not behave as expected.
    /// </exception>
    [Test]
    public void CpuRegisters_ZeroFlag_WorksCorrectly()
    {
        var registers = new Cpu6502Registers();

        registers.Zero = true;
        Assert.That(registers.Zero, Is.True);

        registers.Zero = false;
        Assert.That(registers.Zero, Is.False);
    }

    /// <summary>
    /// Verifies that the <see cref="Cpu6502Registers.SetNZ(byte)"/> method correctly sets the Negative (N)
    /// and Zero (Z) flags based on the provided value.
    /// </summary>
    /// <remarks>
    /// This test ensures that:
    /// <list type="bullet">
    /// <item>The Zero (Z) flag is set when the value is zero.</item>
    /// <item>The Negative (N) flag is set when the most significant bit (MSB) of the value is 1.</item>
    /// </list>
    /// </remarks>
    [Test]
    public void CpuRegisters_SetNZ_SetsFlags()
    {
        var registers = new Cpu6502Registers();

        registers.SetNZ(0);
        Assert.That(registers.Zero, Is.True);
        Assert.That(registers.Negative, Is.False);

        registers.SetNZ(0x80);
        Assert.That(registers.Zero, Is.False);
        Assert.That(registers.Negative, Is.True);

        registers.SetNZ(0x42);
        Assert.That(registers.Zero, Is.False);
        Assert.That(registers.Negative, Is.False);
    }
}