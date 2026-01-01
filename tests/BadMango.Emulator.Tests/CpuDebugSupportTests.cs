// <copyright file="CpuDebugSupportTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;
using Core.Debugger;
using Core.Interfaces;
using Core.Interfaces.Cpu;
using Core.Interfaces.Debugging;

using Emulation.Cpu;
using Emulation.Memory;

/// <summary>
/// Unit tests for CPU debug introspection and control methods.
/// </summary>
[TestFixture]
public class CpuDebugSupportTests
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

    #region Debugger Attachment Tests

    /// <summary>
    /// Verifies that IsDebuggerAttached is false by default.
    /// </summary>
    [Test]
    public void IsDebuggerAttached_IsFalseByDefault()
    {
        Assert.That(cpu.IsDebuggerAttached, Is.False);
    }

    /// <summary>
    /// Verifies that AttachDebugger sets IsDebuggerAttached to true.
    /// </summary>
    [Test]
    public void AttachDebugger_SetsIsDebuggerAttachedToTrue()
    {
        var listener = new TestDebugListener();

        cpu.AttachDebugger(listener);

        Assert.That(cpu.IsDebuggerAttached, Is.True);
    }

    /// <summary>
    /// Verifies that DetachDebugger sets IsDebuggerAttached to false.
    /// </summary>
    [Test]
    public void DetachDebugger_SetsIsDebuggerAttachedToFalse()
    {
        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.DetachDebugger();

        Assert.That(cpu.IsDebuggerAttached, Is.False);
    }

    /// <summary>
    /// Verifies that AttachDebugger throws when passed null.
    /// </summary>
    [Test]
    public void AttachDebugger_ThrowsOnNull()
    {
        Assert.That(() => cpu.AttachDebugger(null!), Throws.ArgumentNullException);
    }

    #endregion

    #region Debug Step Event Tests

    /// <summary>
    /// Verifies that OnBeforeStep is called when debugger is attached.
    /// </summary>
    [Test]
    public void Step_CallsOnBeforeStep_WhenDebuggerAttached()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xEA); // NOP
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.That(listener.BeforeStepCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that OnAfterStep is called when debugger is attached.
    /// </summary>
    [Test]
    public void Step_CallsOnAfterStep_WhenDebuggerAttached()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xEA); // NOP
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.That(listener.AfterStepCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that no step events are called when debugger is not attached.
    /// </summary>
    [Test]
    public void Step_DoesNotCallStepEvents_WhenDebuggerNotAttached()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xEA); // NOP
        cpu.Reset();

        // Create listener without attaching to verify no events are emitted when debugger not attached
        var listener = new TestDebugListener();
        cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(listener.BeforeStepCount, Is.EqualTo(0));
            Assert.That(listener.AfterStepCount, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that step event data contains correct PC value.
    /// </summary>
    [Test]
    public void StepEventData_ContainsCorrectPC()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xEA); // NOP
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.That(listener.LastBeforeStepData.PC, Is.EqualTo(0x1000));
    }

    /// <summary>
    /// Verifies that step event data contains correct opcode.
    /// </summary>
    [Test]
    public void StepEventData_ContainsCorrectOpcode()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.That(listener.LastBeforeStepData.Opcode, Is.EqualTo(0xA9));
    }

    /// <summary>
    /// Verifies that step event data contains operand bytes for immediate mode.
    /// </summary>
    [Test]
    public void StepEventData_ContainsOperandBytes_ImmediateMode()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(listener.LastAfterStepData.Instruction, Is.EqualTo(CpuInstructions.LDA));
            Assert.That(listener.LastAfterStepData.AddressingMode, Is.EqualTo(CpuAddressingModes.Immediate));
            Assert.That(listener.LastAfterStepData.OperandSize, Is.EqualTo(1));
            Assert.That(listener.LastAfterStepData.Operands[0], Is.EqualTo(0x42));
        });
    }

    /// <summary>
    /// Verifies that step event data contains operand bytes for absolute mode.
    /// </summary>
    [Test]
    public void StepEventData_ContainsOperandBytes_AbsoluteMode()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xAD); // LDA $2000
        memory.Write(0x1001, 0x00);
        memory.Write(0x1002, 0x20);
        memory.Write(0x2000, 0x55); // Value at $2000
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(listener.LastAfterStepData.Instruction, Is.EqualTo(CpuInstructions.LDA));
            Assert.That(listener.LastAfterStepData.AddressingMode, Is.EqualTo(CpuAddressingModes.Absolute));
            Assert.That(listener.LastAfterStepData.OperandSize, Is.EqualTo(2));
            Assert.That(listener.LastAfterStepData.Operands[0], Is.EqualTo(0x00));
            Assert.That(listener.LastAfterStepData.Operands[1], Is.EqualTo(0x20));
        });
    }

    /// <summary>
    /// Verifies that after step event contains updated register state.
    /// </summary>
    [Test]
    public void AfterStepEventData_ContainsUpdatedRegisters()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.That(listener.LastAfterStepData.Registers.A.GetByte(), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that after step event shows halted state when STP executed.
    /// </summary>
    [Test]
    public void AfterStepEventData_ShowsHaltedState_WhenSTPExecuted()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xDB); // STP
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(listener.LastAfterStepData.Halted, Is.True);
            Assert.That(listener.LastAfterStepData.HaltReason, Is.EqualTo(HaltState.Stp));
        });
    }

    /// <summary>
    /// Verifies that after step event shows halted state when WAI executed.
    /// </summary>
    [Test]
    public void AfterStepEventData_ShowsHaltedState_WhenWAIExecuted()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xCB); // WAI
        cpu.Reset();

        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);

        cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(listener.LastAfterStepData.Halted, Is.True);
            Assert.That(listener.LastAfterStepData.HaltReason, Is.EqualTo(HaltState.Wai));
        });
    }

    #endregion

    #region PC Manipulation Tests

    /// <summary>
    /// Verifies that SetPC changes the program counter.
    /// </summary>
    [Test]
    public void SetPC_ChangesTheProgramCounter()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        cpu.Reset();

        cpu.SetPC(0x2000);

        Assert.That(cpu.GetPC(), Is.EqualTo(0x2000));
    }

    /// <summary>
    /// Verifies that GetPC returns the current program counter.
    /// </summary>
    [Test]
    public void GetPC_ReturnsCurrentProgramCounter()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        cpu.Reset();

        Addr pc = cpu.GetPC();

        Assert.That(pc, Is.EqualTo(0x1000));
    }

    /// <summary>
    /// Verifies that SetPC allows execution to continue from new address.
    /// </summary>
    [Test]
    public void SetPC_AllowsExecutionFromNewAddress()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x2000, 0xA9); // LDA #$99
        memory.Write(0x2001, 0x99);
        cpu.Reset();

        cpu.SetPC(0x2000);
        cpu.Step();

        Assert.That(cpu.GetRegisters().A.GetByte(), Is.EqualTo(0x99));
    }

    #endregion

    #region Stop Request Tests

    /// <summary>
    /// Verifies that IsStopRequested is false by default.
    /// </summary>
    [Test]
    public void IsStopRequested_IsFalseByDefault()
    {
        Assert.That(cpu.IsStopRequested, Is.False);
    }

    /// <summary>
    /// Verifies that RequestStop sets IsStopRequested to true.
    /// </summary>
    [Test]
    public void RequestStop_SetsIsStopRequestedToTrue()
    {
        cpu.RequestStop();

        Assert.That(cpu.IsStopRequested, Is.True);
    }

    /// <summary>
    /// Verifies that ClearStopRequest sets IsStopRequested to false.
    /// </summary>
    [Test]
    public void ClearStopRequest_SetsIsStopRequestedToFalse()
    {
        cpu.RequestStop();

        cpu.ClearStopRequest();

        Assert.That(cpu.IsStopRequested, Is.False);
    }

    /// <summary>
    /// Verifies that Execute stops when stop is requested.
    /// </summary>
    [Test]
    public void Execute_StopsWhenStopRequested()
    {
        // Set up an infinite loop
        memory.Write(0x1000, 0x80); // BRA $1000 (branch always to self)
        memory.Write(0x1001, 0xFE); // -2 offset

        var listener = new StopAfterNStepsListener(cpu, 5);
        cpu.AttachDebugger(listener);

        cpu.Execute(0x1000);

        // Should have stopped after 5 steps, not stuck in infinite loop
        Assert.That(listener.StepCount, Is.EqualTo(5));
    }

    /// <summary>
    /// Verifies that Reset clears the stop request.
    /// </summary>
    [Test]
    public void Reset_ClearsStopRequest()
    {
        cpu.RequestStop();
        memory.WriteWord(0xFFFC, 0x1000);

        cpu.Reset();

        Assert.That(cpu.IsStopRequested, Is.False);
    }

    #endregion

    #region STP and WAI End Run Tests

    /// <summary>
    /// Verifies that Execute stops when STP is encountered.
    /// </summary>
    [Test]
    public void Execute_StopsOnSTP()
    {
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        memory.Write(0x1002, 0xDB); // STP

        cpu.Execute(0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(cpu.Halted, Is.True);
            Assert.That(cpu.HaltReason, Is.EqualTo(HaltState.Stp));
        });
    }

    /// <summary>
    /// Verifies that Execute stops when WAI is encountered.
    /// </summary>
    [Test]
    public void Execute_StopsOnWAI()
    {
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        memory.Write(0x1002, 0xCB); // WAI

        cpu.Execute(0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(cpu.Halted, Is.True);
            Assert.That(cpu.HaltReason, Is.EqualTo(HaltState.Wai));
        });
    }

    #endregion

    #region Debug Activity Does Not Cost Cycles

    /// <summary>
    /// Verifies that attaching a debugger does not affect cycle counting.
    /// </summary>
    [Test]
    public void DebugActivity_DoesNotAffectCycles()
    {
        memory.WriteWord(0xFFFC, 0x1000);
        memory.Write(0x1000, 0xA9); // LDA #$42
        memory.Write(0x1001, 0x42);
        memory.Write(0x1002, 0xEA); // NOP
        cpu.Reset();

        // Execute without debugger
        int cyclesWithout = (int)cpu.Step().CyclesConsumed.Value + (int)cpu.Step().CyclesConsumed.Value;
        ulong totalCyclesWithout = cpu.GetCycles();

        // Reset and execute with debugger
        cpu.Reset();
        var listener = new TestDebugListener();
        cpu.AttachDebugger(listener);
        int cyclesWith = (int)cpu.Step().CyclesConsumed.Value + (int)cpu.Step().CyclesConsumed.Value;
        ulong totalCyclesWith = cpu.GetCycles();

        Assert.Multiple(() =>
        {
            Assert.That(cyclesWith, Is.EqualTo(cyclesWithout), "Step return value should match");
            Assert.That(totalCyclesWith, Is.EqualTo(totalCyclesWithout), "Total cycles should match");
        });
    }

    #endregion

    /// <summary>
    /// Test debug listener for capturing step events.
    /// </summary>
    private sealed class TestDebugListener : IDebugStepListener
    {
        public int BeforeStepCount { get; private set; }

        public int AfterStepCount { get; private set; }

        public DebugStepEventArgs LastBeforeStepData { get; private set; }

        public DebugStepEventArgs LastAfterStepData { get; private set; }

        public void OnBeforeStep(in DebugStepEventArgs eventData)
        {
            BeforeStepCount++;
            LastBeforeStepData = eventData;
        }

        public void OnAfterStep(in DebugStepEventArgs eventData)
        {
            AfterStepCount++;
            LastAfterStepData = eventData;
        }
    }

    /// <summary>
    /// Debug listener that requests stop after N steps.
    /// </summary>
    private sealed class StopAfterNStepsListener : IDebugStepListener
    {
        private readonly ICpu cpu;
        private readonly int stopAfter;

        public StopAfterNStepsListener(ICpu cpu, int stopAfter)
        {
            this.cpu = cpu;
            this.stopAfter = stopAfter;
        }

        public int StepCount { get; private set; }

        public void OnBeforeStep(in DebugStepEventArgs eventData)
        {
            // Do nothing before
        }

        public void OnAfterStep(in DebugStepEventArgs eventData)
        {
            StepCount++;
            if (StepCount >= stopAfter)
            {
                cpu.RequestStop();
            }
        }
    }
}