// <copyright file="InterruptAndHaltTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;

using TestHelpers;

/// <summary>
/// Unit tests for CPU interrupt handling and halt state management.
/// </summary>
[TestFixture]
public class InterruptAndHaltTests : CpuTestBase
{
    /// <summary>
    /// Sets up the test environment by initializing memory and CPU.
    /// </summary>
    [SetUp]
    public void Setup()
    {
    }

    #region IRQ Tests

    /// <summary>
    /// Verifies that IRQ is processed when I flag is clear.
    /// </summary>
    [Test]
    public void IRQ_ProcessedWhenIFlagClear()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x58);       // CLI - Clear interrupt disable
        Write(0x1001, 0xEA);       // NOP - where we'll signal IRQ
        Write(0x2000, 0x40);       // RTI at IRQ handler
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute CLI
        Cpu.SignalIRQ(); // Signal IRQ
        Cpu.Step(); // Should process IRQ instead of executing NOP

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
        Assert.That(Cpu.Registers.P.IsInterruptDisabled(), Is.True, "I flag should be set after IRQ");

        // Verify stack contains pushed PC and P
        byte p = Read(0x01FD);      // P is at top of stack
        byte lo = Read(0x01FE);    // PC low byte
        byte hi = Read(0x01FF);    // PC high byte
        Assert.That((hi << 8) | lo, Is.EqualTo(0x1001), "Pushed PC should point to NOP");
        Assert.That(p & 0x10, Is.EqualTo(0), "B flag should be clear in pushed P");
    }

    /// <summary>
    /// Verifies that IRQ is masked when I flag is set.
    /// </summary>
    [Test]
    public void IRQ_MaskedWhenIFlagSet()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x78);       // SEI - Set interrupt disable
        Write(0x1001, 0xEA);       // NOP
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute SEI
        Cpu.SignalIRQ(); // Signal IRQ (should be masked)
        Cpu.Step(); // Should execute NOP normally

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002), "PC should have advanced normally, not jumped to IRQ");
    }

    /// <summary>
    /// Verifies that a held (level-asserted) IRQ line does not re-enter the handler
    /// before RTI or CLI, and that the stack pointer remains stable inside the handler.
    /// </summary>
    [Test]
    public void IRQ_HeldLine_DoesNotReenterHandlerBeforeRtiOrCli()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x58);       // CLI
        Write(0x1001, 0xEA);       // NOP
        Write(0x2000, 0xEA);       // NOP
        Write(0x2001, 0xEA);       // NOP
        Write(0x2002, 0xEA);       // NOP
        Write(0x2003, 0x40);       // RTI
        Cpu.Reset();

        // Act
        Cpu.Step(); // CLI
        Cpu.SignalIRQ(); // IRQ line asserted and never released
        Cpu.Step(); // Enter IRQ handler
        byte spAfterEntry = Cpu.Registers.SP.GetByte();

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
        Assert.That(Cpu.Registers.P.IsInterruptDisabled(), Is.True, "I flag should be set on IRQ entry");

        for (int i = 1; i <= 3; i++)
        {
            Cpu.Step();
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000 + i), $"Handler should not be re-entered (step {i})");
            Assert.That(Cpu.Registers.SP.GetByte(), Is.EqualTo(spAfterEntry), $"SP should be stable inside handler (step {i})");
        }
    }
    #endregion

    #region NMI Tests

    /// <summary>
    /// Verifies that NMI is processed regardless of I flag.
    /// </summary>
    [Test]
    public void NMI_ProcessedRegardlessOfIFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFA, 0x3000); // NMI vector
        Write(0x1000, 0x78);       // SEI - Set interrupt disable
        Write(0x1001, 0xEA);       // NOP
        Write(0x3000, 0x40);       // RTI at NMI handler
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute SEI
        Cpu.SignalNMI(); // Signal NMI
        Cpu.Step(); // Should process NMI even with I flag set

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector");
    }

    /// <summary>
    /// Verifies that NMI has priority over IRQ.
    /// </summary>
    [Test]
    public void NMI_HasPriorityOverIRQ()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFA, 0x3000); // NMI vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x58);       // CLI - Clear interrupt disable
        Write(0x1001, 0xEA);       // NOP
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute CLI
        Cpu.SignalIRQ(); // Signal IRQ
        Cpu.SignalNMI(); // Signal NMI (should take priority)
        Cpu.Step(); // Should process NMI, not IRQ

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector, not IRQ vector");
    }

    /// <summary>
    /// Verifies that NMI sets the I flag on entry.
    /// </summary>
    [Test]
    public void NMI_SetsInterruptDisableFlag()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFA, 0x3000); // NMI vector
        Write(0x1000, 0x58);       // CLI
        Write(0x1001, 0xEA);       // NOP
        Write(0x3000, 0x40);       // RTI
        Cpu.Reset();

        // Act
        Cpu.Step(); // CLI
        Assert.That(Cpu.Registers.P.IsInterruptDisabled(), Is.False, "Precondition: I flag clear");
        Cpu.SignalNMI();
        Cpu.Step(); // Enter NMI handler

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector");
        Assert.That(Cpu.Registers.P.IsInterruptDisabled(), Is.True, "I flag should be set after NMI");
    }
    #endregion

    #region WAI Tests

    /// <summary>
    /// Verifies that WAI instruction halts the CPU.
    /// </summary>
    [Test]
    public void WAI_HaltsCpu()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xCB); // WAI
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Halted, Is.True, "CPU should be halted");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.Wai), "Halt reason should be WAI");
    }

    /// <summary>
    /// Verifies that WAI resumes on IRQ when I flag is clear.
    /// </summary>
    [Test]
    public void WAI_ResumesOnIRQ()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x58);       // CLI
        Write(0x1001, 0xCB);       // WAI
        Write(0x2000, 0x40);       // RTI at IRQ handler
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute CLI
        Cpu.Step(); // Execute WAI - CPU halts
        Assert.That(Cpu.Halted, Is.True, "CPU should be halted after WAI");

        Cpu.SignalIRQ(); // Signal IRQ
        Cpu.Step(); // Should resume and process IRQ

        // Assert
        Assert.That(Cpu.Halted, Is.False, "CPU should not be halted after IRQ");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.None), "Halt reason should be None");
    }

    /// <summary>
    /// Verifies that WAI resumes on NMI.
    /// </summary>
    [Test]
    public void WAI_ResumesOnNMI()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFA, 0x3000); // NMI vector
        Write(0x1000, 0xCB);       // WAI
        Write(0x3000, 0x40);       // RTI at NMI handler
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute WAI - CPU halts
        Assert.That(Cpu.Halted, Is.True, "CPU should be halted after WAI");

        Cpu.SignalNMI(); // Signal NMI
        Cpu.Step(); // Should resume and process NMI

        // Assert
        Assert.That(Cpu.Halted, Is.False, "CPU should not be halted after NMI");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector");
    }

    /// <summary>
    /// Verifies that WAI wakes on an asserted IRQ even when the interrupt mask is set,
    /// but does not vector until the mask is cleared.
    /// </summary>
    [Test]
    public void WAI_WakesOnMaskedIRQWithoutVectoring()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x78);       // SEI - Set I flag
        Write(0x1001, 0xCB);       // WAI
        Write(0x1002, 0xEA);       // NOP
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute SEI
        Cpu.Step(); // Execute WAI - CPU halts, PC is now 0x1002
        Assert.That(Cpu.Halted, Is.True, "CPU should be halted after WAI");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002), "PC should point at the instruction after WAI");

        Cpu.SignalIRQ();
        Cpu.Step(); // Resume execution at the next instruction, not the IRQ vector

        // Assert
        Assert.That(Cpu.Halted, Is.False, "CPU should no longer be halted after the IRQ wake-up");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1003), "PC should advance past NOP after the wake-up");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.Not.EqualTo(0x2000), "IRQ vector should not be taken while I is set");
        Assert.That(Cpu.Registers.P.IsInterruptDisabled(), Is.True, "I flag should remain set");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.None), "Halt reason should be cleared");
    }

    /// <summary>
    /// Verifies that IsWaitingForInterrupt returns true when in WAI state.
    /// </summary>
    [Test]
    public void WAI_SetsIsWaitingForInterrupt()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xCB); // WAI
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.IsWaitingForInterrupt, Is.True, "IsWaitingForInterrupt should be true after WAI");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.Wai), "Halt reason should be WAI");
    }

    /// <summary>
    /// Verifies that IsWaitingForInterrupt returns false when not in WAI state.
    /// </summary>
    [Test]
    public void IsWaitingForInterrupt_FalseWhenNotInWaiState()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA); // NOP
        Cpu.Reset();

        // Assert initial state
        Assert.That(Cpu.IsWaitingForInterrupt, Is.False, "IsWaitingForInterrupt should be false initially");

        // Act
        Cpu.Step();

        // Assert after NOP
        Assert.That(Cpu.IsWaitingForInterrupt, Is.False, "IsWaitingForInterrupt should still be false after NOP");
    }

    /// <summary>
    /// Verifies that IsWaitingForInterrupt becomes false when interrupt wakes CPU from WAI.
    /// </summary>
    [Test]
    public void WAI_IsWaitingForInterruptBecomeFalseOnInterrupt()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        WriteWord(0xFFFA, 0x3000); // NMI vector
        Write(0x1000, 0x58);       // CLI
        Write(0x1001, 0xCB);       // WAI
        Write(0x2000, 0x40);       // RTI at IRQ handler
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute CLI
        Cpu.Step(); // Execute WAI - CPU waits

        Assert.That(Cpu.IsWaitingForInterrupt, Is.True, "CPU should be waiting for interrupt");

        Cpu.SignalIRQ(); // Signal IRQ
        Cpu.Step(); // Process interrupt

        // Assert
        Assert.That(Cpu.IsWaitingForInterrupt, Is.False, "IsWaitingForInterrupt should be false after interrupt");
    }

    #endregion

    #region STP Tests

    /// <summary>
    /// Verifies that STP instruction permanently halts the CPU.
    /// </summary>
    [Test]
    public void STP_HaltsCpu()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xDB); // STP
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        Assert.That(Cpu.Halted, Is.True, "CPU should be halted");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.Stp), "Halt reason should be STP");
    }

    /// <summary>
    /// Verifies that STP does not resume on IRQ.
    /// </summary>
    [Test]
    public void STP_DoesNotResumeOnIRQ()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x58);       // CLI
        Write(0x1001, 0xDB);       // STP
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute CLI
        Cpu.Step(); // Execute STP - CPU halts permanently
        Cpu.SignalIRQ(); // Signal IRQ
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // Should remain halted

        // Assert
        Assert.That(Cpu.Halted, Is.True, "CPU should remain halted");
        Assert.That(cycles, Is.EqualTo(0), "No cycles should be consumed");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.Stp), "Halt reason should still be STP");
    }

    /// <summary>
    /// Verifies that STP does not resume on NMI.
    /// </summary>
    [Test]
    public void STP_DoesNotResumeOnNMI()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xDB); // STP
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute STP - CPU halts permanently
        Cpu.SignalNMI(); // Signal NMI
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // Should remain halted

        // Assert
        Assert.That(Cpu.Halted, Is.True, "CPU should remain halted");
        Assert.That(cycles, Is.EqualTo(0), "No cycles should be consumed");
    }

    /// <summary>
    /// Verifies that STP can only be resumed by Reset.
    /// </summary>
    [Test]
    public void STP_ResumesOnlyOnReset()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xDB);       // STP
        Write(0x1001, 0xEA);       // NOP (should execute after reset)
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute STP
        Assert.That(Cpu.Halted, Is.True, "CPU should be halted");

        Cpu.Reset(); // Reset should clear halt state

        // Assert
        Assert.That(Cpu.Halted, Is.False, "CPU should not be halted after reset");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.None), "Halt reason should be None after reset");
    }

    #endregion

    #region BRK Tests

    /// <summary>
    /// Verifies that BRK does not halt the CPU.
    /// </summary>
    [Test]
    public void BRK_DoesNotHaltCpu()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x00);       // BRK
        Write(0x2000, 0xEA);       // NOP at IRQ handler
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute BRK - jumps to IRQ vector

        // Assert - BRK should not halt, PC should be at IRQ vector
        Assert.That(Cpu.Halted, Is.False, "CPU should not be halted after BRK");
        Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.None), "Halt reason should be None");
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
    }

    /// <summary>
    /// Verifies that BRK sets the B flag in pushed status.
    /// </summary>
    [Test]
    public void BRK_SetsBFlagInPushedStatus()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x00);       // BRK
        Cpu.Reset();

        // Act
        Cpu.Step();

        // Assert
        byte pushedP = Read(0x01FD);
        Assert.That(pushedP & 0x10, Is.EqualTo(0x10), "B flag should be set in pushed status");
    }

    #endregion

    #region RTI Tests

    /// <summary>
    /// Verifies that RTI restores CPU state correctly after IRQ.
    /// </summary>
    [Test]
    public void RTI_RestoresStateAfterIRQ()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x58);       // CLI
        Write(0x1001, 0xA9);       // LDA #$42
        Write(0x1002, 0x42);
        Write(0x2000, 0x40);       // RTI at IRQ handler
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute CLI
        Cpu.SignalIRQ(); // Signal IRQ (asserts on signal bus)
        Cpu.Step(); // Process IRQ (pushes PC and P, jumps to IRQ vector)

        // Deassert IRQ before executing RTI, simulating device acknowledgment
        // In a real system, the interrupt handler would read the device's status
        // register which clears the interrupt. For this test, we manually deassert.
        Cpu.EventContext.Signals.Deassert(Core.Signaling.SignalLine.IRQ, 0, new(Cpu.GetCycles()));

        Cpu.Step(); // Execute RTI

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001), "PC should be restored to instruction after CLI");
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.I, Is.EqualTo((ProcessorStatusFlags)0), "I flag should be restored to clear");
    }

    #endregion

    #region Decimal Flag On Interrupt Entry (65C02)

    /// <summary>
    /// Verifies that the 65C02 clears D on IRQ entry, pushes P with D set, and RTI restores D.
    /// </summary>
    [Test]
    public void IRQ_ClearsDecimalFlag_PushedStatusKeepsD_RtiRestoresD()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000);
        Write(0x1000, 0xF8);       // SED
        Write(0x1001, 0x58);       // CLI
        Write(0x1002, 0xEA);       // NOP
        Write(0x2000, 0x40);       // RTI
        Cpu.Reset();

        // Act
        Cpu.Step(); // SED
        Cpu.Step(); // CLI
        Cpu.SignalIRQ();
        Cpu.Step(); // IRQ entry

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.D, Is.EqualTo((ProcessorStatusFlags)0), "D flag should be cleared on IRQ entry");
        Assert.That(Read(0x01FD) & 0x08, Is.EqualTo(0x08), "Pushed P should retain D flag");

        Cpu.EventContext.Signals.Deassert(Core.Signaling.SignalLine.IRQ, 0, new(Cpu.GetCycles()));
        Cpu.Step(); // RTI

        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002), "PC should be restored");
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.D, Is.EqualTo(ProcessorStatusFlags.D), "RTI should restore D flag");
    }

    /// <summary>
    /// Verifies that the 65C02 clears D on NMI entry, pushes P with D set, and RTI restores D.
    /// </summary>
    [Test]
    public void NMI_ClearsDecimalFlag_PushedStatusKeepsD_RtiRestoresD()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFA, 0x3000);
        Write(0x1000, 0xF8);       // SED
        Write(0x1001, 0xEA);       // NOP
        Write(0x3000, 0x40);       // RTI
        Cpu.Reset();

        // Act
        Cpu.Step(); // SED
        Cpu.SignalNMI();
        Cpu.Step(); // NMI entry

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector");
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.D, Is.EqualTo((ProcessorStatusFlags)0), "D flag should be cleared on NMI entry");
        Assert.That(Read(0x01FD) & 0x08, Is.EqualTo(0x08), "Pushed P should retain D flag");

        Cpu.Step(); // RTI

        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001), "PC should be restored");
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.D, Is.EqualTo(ProcessorStatusFlags.D), "RTI should restore D flag");
    }

    /// <summary>
    /// Verifies that the 65C02 clears D on BRK, pushes P with D and B set, and RTI restores D.
    /// </summary>
    [Test]
    public void BRK_ClearsDecimalFlag_PushedStatusKeepsD_RtiRestoresD()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000);
        Write(0x1000, 0xF8);       // SED
        Write(0x1001, 0x00);       // BRK
        Write(0x1002, 0xEA);       // signature byte
        Write(0x1003, 0xEA);       // NOP (return address)
        Write(0x2000, 0x40);       // RTI
        Cpu.Reset();

        // Act
        Cpu.Step(); // SED
        Cpu.Step(); // BRK

        // Assert
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ/BRK vector");
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.D, Is.EqualTo((ProcessorStatusFlags)0), "D flag should be cleared on BRK");
        byte pushed = Read(0x01FD);
        Assert.That(pushed & 0x08, Is.EqualTo(0x08), "Pushed P should retain D flag");
        Assert.That(pushed & 0x10, Is.EqualTo(0x10), "Pushed P should have B flag set");

        Cpu.Step(); // RTI

        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1003), "PC should return past BRK signature byte");
        Assert.That(Cpu.Registers.P & ProcessorStatusFlags.D, Is.EqualTo(ProcessorStatusFlags.D), "RTI should restore D flag");
    }

    #endregion
}