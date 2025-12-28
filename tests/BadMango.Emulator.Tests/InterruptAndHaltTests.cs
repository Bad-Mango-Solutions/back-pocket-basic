// <copyright file="InterruptAndHaltTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;
using Core.Interfaces;

using Emulation.Cpu;
using Emulation.Memory;

/// <summary>
/// Unit tests for CPU interrupt handling and halt state management.
/// </summary>
[TestFixture]
public class InterruptAndHaltTests
{
    private IMemory memory = null!;
    private Cpu65C02 cpu = null!;

    /// <summary>
    /// Sets up the test environment by initializing memory and CPU.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        memory = new BasicMemory();
        cpu = new Cpu65C02(memory);
    }

    #region IRQ Tests

    /// <summary>
    /// Verifies that IRQ is processed when I flag is clear.
    /// </summary>
    [Test]
    public void IRQ_ProcessedWhenIFlagClear()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x58);       // CLI - Clear interrupt disable
        memory.Write(0x1001, 0xEA);       // NOP - where we'll signal IRQ
        memory.Write(0x2000, 0x40);       // RTI at IRQ handler
        cpu.Reset();

        // Act
        cpu.Step(); // Execute CLI
        cpu.SignalIRQ(); // Signal IRQ
        cpu.Step(); // Should process IRQ instead of executing NOP

        // Assert
        var state = cpu.GetState();
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
        Assert.That(state.Registers.P.IsInterruptDisabled(), Is.False, "I flag should be set after IRQ");

        // Verify stack contains pushed PC and P
        byte p = memory.Read(0x01FD);      // P is at top of stack
        byte lo = memory.Read(0x01FE);    // PC low byte
        byte hi = memory.Read(0x01FF);    // PC high byte
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
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x78);       // SEI - Set interrupt disable
        memory.Write(0x1001, 0xEA);       // NOP
        cpu.Reset();

        // Act
        cpu.Step(); // Execute SEI
        cpu.SignalIRQ(); // Signal IRQ (should be masked)
        cpu.Step(); // Should execute NOP normally

        // Assert
        var state = cpu.GetState();
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x1002), "PC should have advanced normally, not jumped to IRQ");
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
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFA, 0x3000); // NMI vector
        memory.Write(0x1000, 0x78);       // SEI - Set interrupt disable
        memory.Write(0x1001, 0xEA);       // NOP
        memory.Write(0x3000, 0x40);       // RTI at NMI handler
        cpu.Reset();

        // Act
        cpu.Step(); // Execute SEI
        cpu.SignalNMI(); // Signal NMI
        cpu.Step(); // Should process NMI even with I flag set

        // Assert
        var state = cpu.GetState();
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector");
    }

    /// <summary>
    /// Verifies that NMI has priority over IRQ.
    /// </summary>
    [Test]
    public void NMI_HasPriorityOverIRQ()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFA, 0x3000); // NMI vector
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x58);       // CLI - Clear interrupt disable
        memory.Write(0x1001, 0xEA);       // NOP
        cpu.Reset();

        // Act
        cpu.Step(); // Execute CLI
        cpu.SignalIRQ(); // Signal IRQ
        cpu.SignalNMI(); // Signal NMI (should take priority)
        cpu.Step(); // Should process NMI, not IRQ

        // Assert
        var state = cpu.GetState();
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector, not IRQ vector");
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
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xCB); // WAI
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        Assert.That(cpu.Halted, Is.True, "CPU should be halted");
        var state = cpu.GetState();
        Assert.That(state.HaltReason, Is.EqualTo(HaltState.Wai), "Halt reason should be WAI");
    }

    /// <summary>
    /// Verifies that WAI resumes on IRQ when I flag is clear.
    /// </summary>
    [Test]
    public void WAI_ResumesOnIRQ()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x58);       // CLI
        memory.Write(0x1001, 0xCB);       // WAI
        memory.Write(0x2000, 0x40);       // RTI at IRQ handler
        cpu.Reset();

        // Act
        cpu.Step(); // Execute CLI
        cpu.Step(); // Execute WAI - CPU halts
        Assert.That(cpu.Halted, Is.True, "CPU should be halted after WAI");

        cpu.SignalIRQ(); // Signal IRQ
        cpu.Step(); // Should resume and process IRQ

        // Assert
        var state = cpu.GetState();
        Assert.That(cpu.Halted, Is.False, "CPU should not be halted after IRQ");
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
        Assert.That(state.HaltReason, Is.EqualTo(HaltState.None), "Halt reason should be None");
    }

    /// <summary>
    /// Verifies that WAI resumes on NMI.
    /// </summary>
    [Test]
    public void WAI_ResumesOnNMI()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFA, 0x3000); // NMI vector
        memory.Write(0x1000, 0xCB);       // WAI
        memory.Write(0x3000, 0x40);       // RTI at NMI handler
        cpu.Reset();

        // Act
        cpu.Step(); // Execute WAI - CPU halts
        Assert.That(cpu.Halted, Is.True, "CPU should be halted after WAI");

        cpu.SignalNMI(); // Signal NMI
        cpu.Step(); // Should resume and process NMI

        // Assert
        var state = cpu.GetState();
        Assert.That(cpu.Halted, Is.False, "CPU should not be halted after NMI");
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x3000), "PC should be at NMI vector");
    }

    /// <summary>
    /// Verifies that WAI does not resume on masked IRQ.
    /// </summary>
    [Test]
    public void WAI_DoesNotResumeOnMaskedIRQ()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x78);       // SEI - Set I flag
        memory.Write(0x1001, 0xCB);       // WAI
        cpu.Reset();

        // Act
        cpu.Step(); // Execute SEI
        cpu.Step(); // Execute WAI - CPU halts
        cpu.SignalIRQ(); // Signal IRQ (should be masked)
        int cycles = cpu.Step(); // Should remain halted

        // Assert
        Assert.That(cpu.Halted, Is.True, "CPU should remain halted");
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
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xDB); // STP
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        Assert.That(cpu.Halted, Is.True, "CPU should be halted");
        var state = cpu.GetState();
        Assert.That(state.HaltReason, Is.EqualTo(HaltState.Stp), "Halt reason should be STP");
    }

    /// <summary>
    /// Verifies that STP does not resume on IRQ.
    /// </summary>
    [Test]
    public void STP_DoesNotResumeOnIRQ()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0x58);       // CLI
        memory.Write(0x1001, 0xDB);       // STP
        cpu.Reset();

        // Act
        cpu.Step(); // Execute CLI
        cpu.Step(); // Execute STP - CPU halts permanently
        cpu.SignalIRQ(); // Signal IRQ
        int cycles = cpu.Step(); // Should remain halted

        // Assert
        Assert.That(cpu.Halted, Is.True, "CPU should remain halted");
        Assert.That(cycles, Is.EqualTo(0), "No cycles should be consumed");
        var state = cpu.GetState();
        Assert.That(state.HaltReason, Is.EqualTo(HaltState.Stp), "Halt reason should still be STP");
    }

    /// <summary>
    /// Verifies that STP does not resume on NMI.
    /// </summary>
    [Test]
    public void STP_DoesNotResumeOnNMI()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xDB); // STP
        cpu.Reset();

        // Act
        cpu.Step(); // Execute STP - CPU halts permanently
        cpu.SignalNMI(); // Signal NMI
        int cycles = cpu.Step(); // Should remain halted

        // Assert
        Assert.That(cpu.Halted, Is.True, "CPU should remain halted");
        Assert.That(cycles, Is.EqualTo(0), "No cycles should be consumed");
    }

    /// <summary>
    /// Verifies that STP can only be resumed by Reset.
    /// </summary>
    [Test]
    public void STP_ResumesOnlyOnReset()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xDB);       // STP
        memory.Write(0x1001, 0xEA);       // NOP (should execute after reset)
        cpu.Reset();

        // Act
        cpu.Step(); // Execute STP
        Assert.That(cpu.Halted, Is.True, "CPU should be halted");

        cpu.Reset(); // Reset should clear halt state

        // Assert
        Assert.That(cpu.Halted, Is.False, "CPU should not be halted after reset");
        var state = cpu.GetState();
        Assert.That(state.HaltReason, Is.EqualTo(HaltState.None), "Halt reason should be None after reset");
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
        memory.WriteWord(0xFFFC, 0x1000);
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x00);       // BRK
        memory.Write(0x2000, 0xEA);       // NOP at IRQ handler
        cpu.Reset();

        // Act
        cpu.Step(); // Execute BRK - jumps to IRQ vector

        // Assert - BRK should not halt, PC should be at IRQ vector
        var state = cpu.GetState();
        Assert.That(cpu.Halted, Is.False, "CPU should not be halted after BRK");
        Assert.That(state.HaltReason, Is.EqualTo(HaltState.None), "Halt reason should be None");
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x2000), "PC should be at IRQ vector");
    }

    /// <summary>
    /// Verifies that BRK sets the B flag in pushed status.
    /// </summary>
    [Test]
    public void BRK_SetsBFlagInPushedStatus()
    {
        // Arrange
        memory.WriteWord(0xFFFC, 0x1000);
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x00);       // BRK
        cpu.Reset();

        // Act
        cpu.Step();

        // Assert
        byte pushedP = memory.Read(0x01FD);
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
        memory.WriteWord(0xFFFC, 0x1000); // Reset vector
        memory.WriteWord(0xFFFE, 0x2000); // IRQ vector
        memory.Write(0x1000, 0x58);       // CLI
        memory.Write(0x1001, 0xA9);       // LDA #$42
        memory.Write(0x1002, 0x42);
        memory.Write(0x2000, 0x40);       // RTI at IRQ handler
        cpu.Reset();

        // Act
        cpu.Step(); // Execute CLI
        cpu.SignalIRQ(); // Signal IRQ
        cpu.Step(); // Process IRQ
        cpu.Step(); // Execute RTI

        // Assert
        var state = cpu.GetState();
        Assert.That(state.Registers.PC.GetWord(), Is.EqualTo(0x1001), "PC should be restored to instruction after CLI");
        Assert.That(state.Registers.P & ProcessorStatusFlags.I, Is.EqualTo((ProcessorStatusFlags)0), "I flag should be restored to clear");
    }

    #endregion
}