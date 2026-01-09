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
        Assert.That(Cpu.Registers.P.IsInterruptDisabled(), Is.False, "I flag should be set after IRQ");

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
    /// Verifies that WAI does not resume on masked IRQ.
    /// </summary>
    [Test]
    public void WAI_DoesNotResumeOnMaskedIRQ()
    {
        // Arrange
        WriteWord(0xFFFC, 0x1000); // Reset vector
        WriteWord(0xFFFE, 0x2000); // IRQ vector
        Write(0x1000, 0x78);       // SEI - Set I flag
        Write(0x1001, 0xCB);       // WAI
        Cpu.Reset();

        // Act
        Cpu.Step(); // Execute SEI
        Cpu.Step(); // Execute WAI - CPU halts
        Cpu.SignalIRQ(); // Signal IRQ (should be masked)
        var result = Cpu.Step();
        int cycles = (int)result.CyclesConsumed.Value; // Should remain halted

        // Assert
        Assert.That(Cpu.Halted, Is.True, "CPU should remain halted");
        Assert.That(cycles, Is.EqualTo(0), "No cycles should be consumed while halted");
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
}