// <copyright file="DebugCommandsTests.Run.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Cpu;

using Moq;

/// <content>
/// Unit tests for <see cref="RunCommand"/>.
/// </content>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that RunCommand has correct name.
    /// </summary>
    [Test]
    public void RunCommand_HasCorrectName()
    {
        var command = new RunCommand();
        Assert.That(command.Name, Is.EqualTo("run"));
    }

    /// <summary>
    /// Verifies that RunCommand has correct aliases.
    /// </summary>
    [Test]
    public void RunCommand_HasCorrectAliases()
    {
        var command = new RunCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "g", "go" }));
    }

    /// <summary>
    /// Verifies that RunCommand runs until CPU halts.
    /// </summary>
    [Test]
    public void RunCommand_RunsUntilHalt()
    {
        // Write a few NOPs then STP
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0xEA); // NOP
        WriteByte(bus, 0x1002, 0xDB); // STP
        cpu.Reset();

        var command = new RunCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CPU halted"));
            Assert.That(cpu.Halted, Is.True);
        });
    }

    /// <summary>
    /// Verifies that RunCommand respects instruction limit.
    /// </summary>
    [Test]
    public void RunCommand_RespectsInstructionLimit()
    {
        // Write infinite NOP loop
        WriteByte(bus, 0x1000, 0xEA); // NOP
        WriteByte(bus, 0x1001, 0x4C); // JMP $1000
        WriteByte(bus, 0x1002, 0x00);
        WriteByte(bus, 0x1003, 0x10);
        cpu.Reset();

        var command = new RunCommand();
        var result = command.Execute(debugContext, ["10"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("instruction limit"));
        });
    }

    /// <summary>
    /// Verifies that RunCommand advances scheduler time while waiting for interrupt when machine is attached.
    /// </summary>
    [Test]
    public void RunCommand_AdvancesSchedulerDuringWai_WhenMachineAttached()
    {
        WriteByte(bus, 0x1000, 0xCB); // WAI
        cpu.Reset();

        var mockScheduler = new Mock<IScheduler>();
        mockScheduler.Setup(s => s.JumpToNextEventAndDispatch()).Returns(true);

        var mockMachine = new Mock<IMachine>();
        mockMachine.SetupGet(m => m.Cpu).Returns(cpu);
        mockMachine.SetupGet(m => m.Bus).Returns(bus);
        mockMachine.SetupGet(m => m.Scheduler).Returns(mockScheduler.Object);
        mockMachine.SetupGet(m => m.State).Returns(MachineState.Stopped);
        mockMachine.SetupSequence(m => m.Step())
            .Returns(new CpuStepResult(CpuRunState.WaitingForInterrupt, Cycle.Zero))
            .Returns(new CpuStepResult(CpuRunState.Halted, Cycle.Zero));
        debugContext.AttachMachine(mockMachine.Object);

        var command = new RunCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            mockMachine.Verify(m => m.Step(), Times.Exactly(2));
            mockScheduler.Verify(s => s.JumpToNextEventAndDispatch(), Times.Once);
        });
    }

    /// <summary>
    /// Verifies that RunCommand exits cleanly when CPU-only execution enters WAI with no machine scheduler.
    /// </summary>
    [Test]
    public void RunCommand_CpuOnlyWai_StopsWithoutSpinning()
    {
        WriteByte(bus, 0x1000, 0xCB); // WAI
        cpu.Reset();

        var command = new RunCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("CPU waiting for interrupt"));
        });
    }
}