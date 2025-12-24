// <copyright file="ComprehensiveDebugFunctionalTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core;
using Emulation.Cpu;
using Emulation.Memory;

/// <summary>
/// Comprehensive functional tests that exercise the debug infrastructure with complex code.
/// </summary>
/// <remarks>
/// This test suite validates that the debug step listener receives accurate information
/// about instruction execution, including opcodes, operands, addressing modes, register
/// states, cycle counts, and halt conditions.
/// </remarks>
[TestFixture]
public class ComprehensiveDebugFunctionalTests
{
    private IMemory memory = null!;
    private Cpu65C02 cpu = null!;
    private RecordingDebugListener listener = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        memory = new BasicMemory();
        cpu = new Cpu65C02(memory);
        listener = new RecordingDebugListener();
    }

    /// <summary>
    /// Exercises a complex program that computes factorial of 5 using a loop,
    /// validates all debug output, and terminates with STP.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The program computes 5! = 120 using a multiplication loop.
    /// Initialize accumulator with 1.
    /// Initialize X with 5 (the number to compute factorial of).
    /// Loop: multiply by X, decrement X, repeat until X is 0.
    /// Store result in memory.
    /// Terminate with STP.
    /// </para>
    /// <para>
    /// This test exercises:
    /// Immediate addressing (LDA #, LDX #, ADC #, SBC #).
    /// Zero page addressing (STA $, LDA $).
    /// Branch instructions (BNE).
    /// Implied addressing (DEX, SEC, CLC).
    /// STP instruction for termination.
    /// </para>
    /// </remarks>
    [Test]
    public void FactorialProgram_ValidatesDebugOutput()
    {
        // Program memory layout: $1000 is main entry point
        // $10-$13: Zero page variables (result, multiplier, counter, temp)
        ushort startAddr = 0x1000;
        byte resultAddr = 0x10;
        byte multiplierAddr = 0x11;
        byte counterAddr = 0x12;

        // Program to compute 5! = 120 using repeated addition
        byte[] program =
        [
            0xA9, 0x01,         // $1000: LDA #$01      ; A = 1 (initial result)
            0x85, resultAddr,   // $1002: STA $10      ; result = 1
            0xA9, 0x05,         // $1004: LDA #$05      ; A = 5 (factorial of 5)
            0x85, counterAddr,  // $1006: STA $12      ; counter = 5
            0xA5, counterAddr,  // $1008: LDA $12      ; A = counter (multiply_loop)
            0xF0, 0x12,         // $100A: BEQ +18      ; if counter == 0, done (to $101E)
            0x85, multiplierAddr, // $100C: STA $11    ; multiplier = counter
            0xA9, 0x00,         // $100E: LDA #$00      ; A = 0 (result_new = 0)
            0x18,               // $1010: CLC           ; clear carry (add_loop)
            0x65, resultAddr,   // $1011: ADC $10      ; A += result
            0xC6, multiplierAddr, // $1013: DEC $11    ; multiplier--
            0xD0, 0xF9,         // $1015: BNE -7       ; loop back to $1010
            0x85, resultAddr,   // $1017: STA $10      ; result = A
            0xC6, counterAddr,  // $1019: DEC $12      ; counter--
            0x4C, 0x08, 0x10,   // $101B: JMP multiply_loop
            0xA5, resultAddr,   // $101E: LDA $10      ; Load final result (done)
            0xDB,               // $1020: STP           ; Stop processor
        ];

        // Load program into memory
        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        // Set reset vector
        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        // Step through until STP
        int maxSteps = 500; // Safety limit
        int stepCount = 0;

        while (!cpu.Halted && stepCount < maxSteps)
        {
            cpu.Step();
            stepCount++;
        }

        // Verify the program halted on STP
        Assert.That(cpu.Halted, Is.True, "CPU should be halted");
        Assert.That(cpu.GetState().HaltReason, Is.EqualTo(HaltState.Stp), "Should halt due to STP");

        // Verify final result: 5! = 120 = $78
        Assert.That(memory.Read(resultAddr), Is.EqualTo(120), "5! should equal 120");
        Assert.That(cpu.GetRegisters().A.GetByte(), Is.EqualTo(120), "Accumulator should contain 120");

        // Verify debug listener recorded all steps
        Assert.That(listener.StepRecords.Count, Is.EqualTo(stepCount), "Should have recorded all steps");

        // Verify first instruction was LDA #$01
        var firstStep = listener.StepRecords[0];
        Assert.Multiple(() =>
        {
            Assert.That(firstStep.AfterStep.Instruction, Is.EqualTo(CpuInstructions.LDA));
            Assert.That(firstStep.AfterStep.AddressingMode, Is.EqualTo(CpuAddressingModes.Immediate));
            Assert.That(firstStep.AfterStep.Registers.A.GetByte(), Is.EqualTo(0x01));
        });

        // Verify last instruction was STP
        var lastStep = listener.StepRecords[^1];
        Assert.Multiple(() =>
        {
            Assert.That(lastStep.AfterStep.Instruction, Is.EqualTo(CpuInstructions.STP));
            Assert.That(lastStep.AfterStep.Halted, Is.True);
            Assert.That(lastStep.AfterStep.HaltReason, Is.EqualTo(HaltState.Stp));
        });
    }

    /// <summary>
    /// Tests a program that uses subroutine calls (JSR/RTS) and validates debug output.
    /// </summary>
    [Test]
    public void SubroutineProgram_ValidatesDebugOutput()
    {
        // Program that calls a subroutine to double a value
        ushort mainAddr = 0x1000;
        ushort subroutineAddr = 0x1100;

        byte[] mainProgram =
        [
            0xA9, 0x15,         // $1000: LDA #$15      ; A = 21
            0x20, 0x00, 0x11,   // $1002: JSR $1100    ; Call double subroutine
            0x8D, 0x00, 0x02,   // $1005: STA $0200    ; Store result
            0xDB,               // $1008: STP           ; Stop
        ];

        byte[] subroutine =
        [
            0x0A,               // $1100: ASL A        ; A = A * 2
            0x60,               // $1101: RTS           ; Return
        ];

        // Load programs
        for (int i = 0; i < mainProgram.Length; i++)
        {
            memory.Write((Addr)(mainAddr + i), mainProgram[i]);
        }

        for (int i = 0; i < subroutine.Length; i++)
        {
            memory.Write((Addr)(subroutineAddr + i), subroutine[i]);
        }

        memory.WriteWord(0xFFFC, mainAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        // Step through until halted
        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Verify result: 21 * 2 = 42
        Assert.That(memory.Read(0x0200), Is.EqualTo(42));

        // Find the JSR instruction in the records
        var jsrRecordIndex = listener.StepRecords.FindIndex(r => r.AfterStep.Instruction == CpuInstructions.JSR);
        Assert.That(jsrRecordIndex, Is.GreaterThanOrEqualTo(0), "Should have recorded JSR");
        Assert.That(listener.StepRecords[jsrRecordIndex].AfterStep.AddressingMode, Is.EqualTo(CpuAddressingModes.Absolute));

        // Find the ASL instruction (in subroutine)
        var aslRecordIndex = listener.StepRecords.FindIndex(r => r.AfterStep.Instruction == CpuInstructions.ASL);
        Assert.That(aslRecordIndex, Is.GreaterThanOrEqualTo(0), "Should have recorded ASL");
        Assert.That(listener.StepRecords[aslRecordIndex].AfterStep.Registers.A.GetByte(), Is.EqualTo(42), "A should be doubled");

        // Find the RTS instruction
        var rtsRecordIndex = listener.StepRecords.FindIndex(r => r.AfterStep.Instruction == CpuInstructions.RTS);
        Assert.That(rtsRecordIndex, Is.GreaterThanOrEqualTo(0), "Should have recorded RTS");
    }

    /// <summary>
    /// Tests a program that exercises all branch conditions and validates debug output.
    /// </summary>
    [Test]
    public void BranchConditions_ValidatesDebugOutput()
    {
        ushort startAddr = 0x1000;

        // Program that tests various branch conditions
        byte[] program =
        [
            0xA9, 0x00,         // $1000: LDA #$00      ; A = 0, Z flag set
            0xF0, 0x02,         // $1002: BEQ +2       ; Branch taken (BEQ test)
            0xA9, 0xFF,         // $1004: LDA #$FF      ; (skipped)
            0xA9, 0x01,         // $1006: LDA #$01      ; A = 1, Z flag clear
            0xD0, 0x02,         // $1008: BNE +2       ; Branch taken (BNE test)
            0xA9, 0xFE,         // $100A: LDA #$FE      ; (skipped)
            0x38,               // $100C: SEC           ; Set carry
            0xB0, 0x02,         // $100D: BCS +2       ; Branch taken (BCS test)
            0xA9, 0xFD,         // $100F: LDA #$FD      ; (skipped)
            0x18,               // $1011: CLC           ; Clear carry (BCC test)
            0x90, 0x02,         // $1012: BCC +2       ; Branch taken
            0xA9, 0xFC,         // $1014: LDA #$FC      ; (skipped)
            0xA9, 0x80,         // $1016: LDA #$80      ; A = 128, N flag set
            0x30, 0x02,         // $1018: BMI +2       ; Branch taken (BMI test)
            0xA9, 0xFB,         // $101A: LDA #$FB      ; (skipped)
            0xA9, 0x7F,         // $101C: LDA #$7F      ; A = 127, N flag clear
            0x10, 0x02,         // $101E: BPL +2       ; Branch taken (BPL test)
            0xA9, 0xFA,         // $1020: LDA #$FA      ; (skipped)
            0xDB,               // $1022: STP           ; Stop
        ];

        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Verify all branches were taken by checking we never loaded $FF, $FE, $FD, $FC, $FB, $FA
        var ldaRecords = listener.StepRecords
            .Where(r => r.AfterStep.Instruction == CpuInstructions.LDA)
            .Select(r => r.AfterStep.Registers.A.GetByte())
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(ldaRecords, Does.Contain((byte)0x00), "Should have loaded 0x00");
            Assert.That(ldaRecords, Does.Contain((byte)0x01), "Should have loaded 0x01");
            Assert.That(ldaRecords, Does.Contain((byte)0x80), "Should have loaded 0x80");
            Assert.That(ldaRecords, Does.Contain((byte)0x7F), "Should have loaded 0x7F");
            Assert.That(ldaRecords, Does.Not.Contain((byte)0xFF), "Should NOT have loaded 0xFF");
            Assert.That(ldaRecords, Does.Not.Contain((byte)0xFE), "Should NOT have loaded 0xFE");
            Assert.That(ldaRecords, Does.Not.Contain((byte)0xFD), "Should NOT have loaded 0xFD");
            Assert.That(ldaRecords, Does.Not.Contain((byte)0xFC), "Should NOT have loaded 0xFC");
            Assert.That(ldaRecords, Does.Not.Contain((byte)0xFB), "Should NOT have loaded 0xFB");
            Assert.That(ldaRecords, Does.Not.Contain((byte)0xFA), "Should NOT have loaded 0xFA");
        });

        // Verify branch instructions were recorded with Relative addressing mode
        var branchRecords = listener.StepRecords
            .Where(r => r.AfterStep.Instruction is CpuInstructions.BEQ or CpuInstructions.BNE
                or CpuInstructions.BCS or CpuInstructions.BCC
                or CpuInstructions.BMI or CpuInstructions.BPL)
            .ToList();

        Assert.That(branchRecords, Has.Count.EqualTo(6), "Should have 6 branch instructions");
        Assert.That(
            branchRecords.All(r => r.AfterStep.AddressingMode == CpuAddressingModes.Relative),
            Is.True,
            "All branches should use Relative addressing");
    }

    /// <summary>
    /// Tests stack operations (PHA/PLA, PHX/PLX, PHY/PLY) and validates debug output.
    /// </summary>
    [Test]
    public void StackOperations_ValidatesDebugOutput()
    {
        ushort startAddr = 0x1000;

        byte[] program =
        [
            0xA9, 0x11,         // $1000: LDA #$11
            0xA2, 0x22,         // $1002: LDX #$22
            0xA0, 0x33,         // $1004: LDY #$33

            0x48,               // $1006: PHA           ; Push A ($11)
            0xDA,               // $1007: PHX           ; Push X ($22)
            0x5A,               // $1008: PHY           ; Push Y ($33)

            0xA9, 0x00,         // $1009: LDA #$00      ; Clear registers
            0xA2, 0x00,         // $100B: LDX #$00
            0xA0, 0x00,         // $100D: LDY #$00

            0x7A,               // $100F: PLY           ; Pull Y (should be $33)
            0xFA,               // $1010: PLX           ; Pull X (should be $22)
            0x68,               // $1011: PLA           ; Pull A (should be $11)

            0xDB,               // $1012: STP
        ];

        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Verify final register values
        var regs = cpu.GetRegisters();
        Assert.Multiple(() =>
        {
            Assert.That(regs.A.GetByte(), Is.EqualTo(0x11), "A should be restored to $11");
            Assert.That(regs.X.GetByte(), Is.EqualTo(0x22), "X should be restored to $22");
            Assert.That(regs.Y.GetByte(), Is.EqualTo(0x33), "Y should be restored to $33");
        });

        // Verify push/pull instructions were recorded
        var pushRecords = listener.StepRecords
            .Where(r => r.AfterStep.Instruction is CpuInstructions.PHA or CpuInstructions.PHX or CpuInstructions.PHY)
            .ToList();

        var pullRecords = listener.StepRecords
            .Where(r => r.AfterStep.Instruction is CpuInstructions.PLA or CpuInstructions.PLX or CpuInstructions.PLY)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(pushRecords, Has.Count.EqualTo(3), "Should have 3 push instructions");
            Assert.That(pullRecords, Has.Count.EqualTo(3), "Should have 3 pull instructions");
            Assert.That(
                pushRecords.All(r => r.AfterStep.AddressingMode == CpuAddressingModes.Implied),
                Is.True,
                "Push instructions should use Implied addressing");
            Assert.That(
                pullRecords.All(r => r.AfterStep.AddressingMode == CpuAddressingModes.Implied),
                Is.True,
                "Pull instructions should use Implied addressing");
        });
    }

    /// <summary>
    /// Tests shift and rotate operations and validates debug output.
    /// </summary>
    [Test]
    public void ShiftRotateOperations_ValidatesDebugOutput()
    {
        ushort startAddr = 0x1000;

        byte[] program =
        [
            0xA9, 0x40,         // $1000: LDA #$40      ; A = 01000000
            0x0A,               // $1002: ASL A        ; A = 10000000 (N flag set)
            0x0A,               // $1003: ASL A        ; A = 00000000, C = 1 (wrapped)
            0x2A,               // $1004: ROL A        ; A = 00000001 (rotated carry in)
            0x6A,               // $1005: ROR A        ; A = 00000000, C = 1
            0xA9, 0x80,         // $1006: LDA #$80      ; A = 10000000
            0x4A,               // $1008: LSR A        ; A = 01000000
            0xDB,               // $1009: STP
        ];

        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Verify final accumulator value
        Assert.That(cpu.GetRegisters().A.GetByte(), Is.EqualTo(0x40));

        // Verify shift/rotate instructions were recorded
        var shiftRecords = listener.StepRecords
            .Where(r => r.AfterStep.Instruction is CpuInstructions.ASL or CpuInstructions.LSR
                or CpuInstructions.ROL or CpuInstructions.ROR)
            .ToList();

        Assert.That(shiftRecords, Has.Count.EqualTo(5), "Should have 5 shift/rotate instructions");
        Assert.That(
            shiftRecords.All(r => r.AfterStep.AddressingMode == CpuAddressingModes.Accumulator),
            Is.True,
            "All should use Accumulator addressing");
    }

    /// <summary>
    /// Tests comparison instructions and validates flag changes in debug output.
    /// </summary>
    [Test]
    public void ComparisonInstructions_ValidatesDebugOutput()
    {
        ushort startAddr = 0x1000;

        byte[] program =
        [
            0xA9, 0x50,         // $1000: LDA #$50
            0xC9, 0x50,         // $1002: CMP #$50      ; A == operand, Z=1, C=1
            0xC9, 0x40,         // $1004: CMP #$40      ; A > operand, Z=0, C=1
            0xC9, 0x60,         // $1006: CMP #$60      ; A < operand, Z=0, C=0

            0xA2, 0x20,         // $1008: LDX #$20
            0xE0, 0x20,         // $100A: CPX #$20      ; X == operand

            0xA0, 0x30,         // $100C: LDY #$30
            0xC0, 0x30,         // $100E: CPY #$30      ; Y == operand

            0xDB,               // $1010: STP
        ];

        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Find CMP, CPX, CPY instructions
        var cmpRecords = listener.StepRecords.Where(r => r.AfterStep.Instruction == CpuInstructions.CMP).ToList();
        var cpxRecords = listener.StepRecords.Where(r => r.AfterStep.Instruction == CpuInstructions.CPX).ToList();
        var cpyRecords = listener.StepRecords.Where(r => r.AfterStep.Instruction == CpuInstructions.CPY).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(cmpRecords, Has.Count.EqualTo(3), "Should have 3 CMP instructions");
            Assert.That(cpxRecords, Has.Count.EqualTo(1), "Should have 1 CPX instruction");
            Assert.That(cpyRecords, Has.Count.EqualTo(1), "Should have 1 CPY instruction");
        });

        // Verify first CMP set Zero flag (A == operand)
        var firstCmp = cmpRecords[0];
        Assert.That(firstCmp.AfterStep.Registers.P.IsZeroSet(), Is.True, "First CMP should set Z flag");
    }

    /// <summary>
    /// Tests logical operations (AND, ORA, EOR) and validates debug output.
    /// </summary>
    [Test]
    public void LogicalOperations_ValidatesDebugOutput()
    {
        ushort startAddr = 0x1000;

        byte[] program =
        [
            0xA9, 0xFF,         // $1000: LDA #$FF      ; A = 11111111
            0x29, 0x0F,         // $1002: AND #$0F      ; A = 00001111
            0x09, 0xF0,         // $1004: ORA #$F0      ; A = 11111111
            0x49, 0xAA,         // $1006: EOR #$AA      ; A = 01010101
            0xDB,               // $1008: STP
        ];

        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Verify final value: $FF AND $0F = $0F, $0F ORA $F0 = $FF, $FF EOR $AA = $55
        Assert.That(cpu.GetRegisters().A.GetByte(), Is.EqualTo(0x55));

        // Verify logical instructions were recorded
        var andRecord = listener.StepRecords.First(r => r.AfterStep.Instruction == CpuInstructions.AND);
        var oraRecord = listener.StepRecords.First(r => r.AfterStep.Instruction == CpuInstructions.ORA);
        var eorRecord = listener.StepRecords.First(r => r.AfterStep.Instruction == CpuInstructions.EOR);

        Assert.Multiple(() =>
        {
            Assert.That(andRecord.AfterStep.Registers.A.GetByte(), Is.EqualTo(0x0F));
            Assert.That(oraRecord.AfterStep.Registers.A.GetByte(), Is.EqualTo(0xFF));
            Assert.That(eorRecord.AfterStep.Registers.A.GetByte(), Is.EqualTo(0x55));
        });
    }

    /// <summary>
    /// Tests that cycle counting is accurate in debug output.
    /// </summary>
    [Test]
    public void CycleCounting_IsAccurateInDebugOutput()
    {
        ushort startAddr = 0x1000;

        byte[] program =
        [
            0xEA,               // $1000: NOP (2 cycles)
            0xA9, 0x42,         // $1001: LDA #$42 (2 cycles)
            0x8D, 0x00, 0x02,   // $1003: STA $0200 (4 cycles)
            0xDB,               // $1006: STP
        ];

        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Verify instruction cycles were tracked
        var records = listener.StepRecords.Take(3).ToList();

        Assert.Multiple(() =>
        {
            // NOP = 2 cycles
            Assert.That(records[0].AfterStep.InstructionCycles, Is.EqualTo(2), "NOP should be 2 cycles");

            // LDA immediate = 2 cycles
            Assert.That(records[1].AfterStep.InstructionCycles, Is.EqualTo(2), "LDA # should be 2 cycles");

            // STA absolute = 4 cycles
            Assert.That(records[2].AfterStep.InstructionCycles, Is.EqualTo(4), "STA abs should be 4 cycles");
        });

        // Verify total cycles add up
        var totalCycles = cpu.GetState().Cycles;
        var sumOfInstructionCycles = listener.StepRecords.Sum(r => r.AfterStep.InstructionCycles);
        Assert.That(totalCycles, Is.EqualTo(sumOfInstructionCycles), "Total cycles should match sum of instruction cycles");
    }

    /// <summary>
    /// Tests that effective addresses are correctly reported for various addressing modes.
    /// </summary>
    [Test]
    public void EffectiveAddresses_AreCorrectlyReported()
    {
        ushort startAddr = 0x1000;

        // Set up zero page pointer at $20-$21 pointing to $0300
        memory.Write(0x20, 0x00);
        memory.Write(0x21, 0x03);
        memory.Write(0x0300, 0x99); // Value at effective address

        byte[] program =
        [
            0xA9, 0x42,         // $1000: LDA #$42      ; Immediate
            0x85, 0x10,         // $1002: STA $10      ; Zero page
            0x8D, 0x00, 0x02,   // $1004: STA $0200    ; Absolute
            0xA2, 0x05,         // $1007: LDX #$05
            0xB5, 0x10,         // $1009: LDA $10,X    ; Zero page,X (effective: $15)
            0xBD, 0x00, 0x02,   // $100B: LDA $0200,X  ; Absolute,X (effective: $0205)
            0xDB,               // $100E: STP
        ];

        for (int i = 0; i < program.Length; i++)
        {
            memory.Write((Addr)(startAddr + i), program[i]);
        }

        memory.WriteWord(0xFFFC, startAddr);
        cpu.Reset();
        cpu.AttachDebugger(listener);

        while (!cpu.Halted)
        {
            cpu.Step();
        }

        // Check effective addresses were recorded
        var zpRecord = listener.StepRecords.First(r =>
            r.AfterStep.Instruction == CpuInstructions.STA &&
            r.AfterStep.AddressingMode == CpuAddressingModes.ZeroPage);

        var absRecord = listener.StepRecords.First(r =>
            r.AfterStep.Instruction == CpuInstructions.STA &&
            r.AfterStep.AddressingMode == CpuAddressingModes.Absolute);

        Assert.Multiple(() =>
        {
            Assert.That(zpRecord.AfterStep.EffectiveAddress, Is.EqualTo(0x10), "ZP effective address should be $10");
            Assert.That(absRecord.AfterStep.EffectiveAddress, Is.EqualTo(0x0200), "Abs effective address should be $0200");
        });
    }

    /// <summary>
    /// Represents a single step record with before and after data.
    /// </summary>
    private struct StepRecord
    {
        /// <summary>
        /// Gets or sets the debug data before the step.
        /// </summary>
        public DebugStepEventArgs BeforeStep;

        /// <summary>
        /// Gets or sets the debug data after the step.
        /// </summary>
        public DebugStepEventArgs AfterStep;
    }

    /// <summary>
    /// Recording debug listener that captures all step events.
    /// </summary>
    private sealed class RecordingDebugListener : IDebugStepListener
    {
        /// <summary>
        /// Gets the list of recorded step data.
        /// </summary>
        public List<StepRecord> StepRecords { get; } = [];

        /// <inheritdoc/>
        public void OnBeforeStep(in DebugStepEventArgs eventData)
        {
            // Start a new record
            StepRecords.Add(new StepRecord { BeforeStep = eventData });
        }

        /// <inheritdoc/>
        public void OnAfterStep(in DebugStepEventArgs eventData)
        {
            // Complete the current record
            if (StepRecords.Count > 0)
            {
                var last = StepRecords[^1];
                last.AfterStep = eventData;
                StepRecords[^1] = last;
            }
        }
    }
}