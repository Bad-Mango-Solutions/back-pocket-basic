// <copyright file="CpuDebugSupportTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;
using Core.Debugger;
using TestHelpers;
using Core.Interfaces.Cpu;
using Core.Interfaces.Debugging;

using Emulation.Cpu;


/// <summary>
/// Unit tests for CPU debug introspection and control methods.
/// </summary>
[TestFixture]
public class CpuDebugSupportTests : CpuTestBase
{
    
    

    /// <summary>
    /// Sets up the test environment by initializing memory and CPU.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        
        
    }

    #region Debugger Attachment Tests

    /// <summary>
    /// Verifies that IsDebuggerAttached is false by default.
    /// </summary>
    [Test]
    public void IsDebuggerAttached_IsFalseByDefault()
    {
        Assert.That(Cpu.IsDebuggerAttached, Is.False);
    }

    /// <summary>
    /// Verifies that AttachDebugger sets IsDebuggerAttached to true.
    /// </summary>
    [Test]
    public void AttachDebugger_SetsIsDebuggerAttachedToTrue()
    {
        var listener = new TestDebugListener();

        Cpu.AttachDebugger(listener);

        Assert.That(Cpu.IsDebuggerAttached, Is.True);
    }

    /// <summary>
    /// Verifies that DetachDebugger sets IsDebuggerAttached to false.
    /// </summary>
    [Test]
    public void DetachDebugger_SetsIsDebuggerAttachedToFalse()
    {
        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.DetachDebugger();

        Assert.That(Cpu.IsDebuggerAttached, Is.False);
    }

    /// <summary>
    /// Verifies that AttachDebugger throws when passed null.
    /// </summary>
    [Test]
    public void AttachDebugger_ThrowsOnNull()
    {
        Assert.That(() => Cpu.AttachDebugger(null!), Throws.ArgumentNullException);
    }

    #endregion

    #region Debug Step Event Tests

    /// <summary>
    /// Verifies that OnBeforeStep is called when debugger is attached.
    /// </summary>
    [Test]
    public void Step_CallsOnBeforeStep_WhenDebuggerAttached()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA); // NOP
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

        Assert.That(listener.BeforeStepCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that OnAfterStep is called when debugger is attached.
    /// </summary>
    [Test]
    public void Step_CallsOnAfterStep_WhenDebuggerAttached()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA); // NOP
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

        Assert.That(listener.AfterStepCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that no step events are called when debugger is not attached.
    /// </summary>
    [Test]
    public void Step_DoesNotCallStepEvents_WhenDebuggerNotAttached()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA); // NOP
        Cpu.Reset();

        // Create listener without attaching to verify no events are emitted when debugger not attached
        var listener = new TestDebugListener();
        Cpu.Step();

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
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA); // NOP
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

        Assert.That(listener.LastBeforeStepData.PC, Is.EqualTo(0x1000));
    }

    /// <summary>
    /// Verifies that step event data contains correct opcode.
    /// </summary>
    [Test]
    public void StepEventData_ContainsCorrectOpcode()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

        Assert.That(listener.LastBeforeStepData.Opcode, Is.EqualTo(0xA9));
    }

    /// <summary>
    /// Verifies that step event data contains operand bytes for immediate mode.
    /// </summary>
    [Test]
    public void StepEventData_ContainsOperandBytes_ImmediateMode()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

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
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xAD); // LDA $2000
        Write(0x1001, 0x00);
        Write(0x1002, 0x20);
        Write(0x2000, 0x55); // Value at $2000
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

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
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

        Assert.That(listener.LastAfterStepData.Registers.A.GetByte(), Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that after step event shows halted state when STP executed.
    /// </summary>
    [Test]
    public void AfterStepEventData_ShowsHaltedState_WhenSTPExecuted()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xDB); // STP
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

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
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xCB); // WAI
        Cpu.Reset();

        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);

        Cpu.Step();

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
        WriteWord(0xFFFC, 0x1000);
        Cpu.Reset();

        Cpu.SetPC(0x2000);

        Assert.That(Cpu.GetPC(), Is.EqualTo(0x2000));
    }

    /// <summary>
    /// Verifies that GetPC returns the current program counter.
    /// </summary>
    [Test]
    public void GetPC_ReturnsCurrentProgramCounter()
    {
        WriteWord(0xFFFC, 0x1000);
        Cpu.Reset();

        Addr pc = Cpu.GetPC();

        Assert.That(pc, Is.EqualTo(0x1000));
    }

    /// <summary>
    /// Verifies that SetPC allows execution to continue from new address.
    /// </summary>
    [Test]
    public void SetPC_AllowsExecutionFromNewAddress()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x2000, 0xA9); // LDA #$99
        Write(0x2001, 0x99);
        Cpu.Reset();

        Cpu.SetPC(0x2000);
        Cpu.Step();

        Assert.That(Cpu.GetRegisters().A.GetByte(), Is.EqualTo(0x99));
    }

    #endregion

    #region Stop Request Tests

    /// <summary>
    /// Verifies that IsStopRequested is false by default.
    /// </summary>
    [Test]
    public void IsStopRequested_IsFalseByDefault()
    {
        Assert.That(Cpu.IsStopRequested, Is.False);
    }

    /// <summary>
    /// Verifies that RequestStop sets IsStopRequested to true.
    /// </summary>
    [Test]
    public void RequestStop_SetsIsStopRequestedToTrue()
    {
        Cpu.RequestStop();

        Assert.That(Cpu.IsStopRequested, Is.True);
    }

    /// <summary>
    /// Verifies that ClearStopRequest sets IsStopRequested to false.
    /// </summary>
    [Test]
    public void ClearStopRequest_SetsIsStopRequestedToFalse()
    {
        Cpu.RequestStop();

        Cpu.ClearStopRequest();

        Assert.That(Cpu.IsStopRequested, Is.False);
    }

    /// <summary>
    /// Verifies that Execute stops when stop is requested.
    /// </summary>
    [Test]
    public void Execute_StopsWhenStopRequested()
    {
        // Set up an infinite loop
        Write(0x1000, 0x80); // BRA $1000 (branch always to self)
        Write(0x1001, 0xFE); // -2 offset

        var listener = new StopAfterNStepsListener(Cpu, 5);
        Cpu.AttachDebugger(listener);

        Cpu.Execute(0x1000);

        // Should have stopped after 5 steps, not stuck in infinite loop
        Assert.That(listener.StepCount, Is.EqualTo(5));
    }

    /// <summary>
    /// Verifies that Reset clears the stop request.
    /// </summary>
    [Test]
    public void Reset_ClearsStopRequest()
    {
        Cpu.RequestStop();
        WriteWord(0xFFFC, 0x1000);

        Cpu.Reset();

        Assert.That(Cpu.IsStopRequested, Is.False);
    }

    #endregion

    #region STP and WAI End Run Tests

    /// <summary>
    /// Verifies that Execute stops when STP is encountered.
    /// </summary>
    [Test]
    public void Execute_StopsOnSTP()
    {
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Write(0x1002, 0xDB); // STP

        Cpu.Execute(0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Halted, Is.True);
            Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.Stp));
        });
    }

    /// <summary>
    /// Verifies that Execute stops when WAI is encountered.
    /// </summary>
    [Test]
    public void Execute_StopsOnWAI()
    {
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Write(0x1002, 0xCB); // WAI

        Cpu.Execute(0x1000);

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.Halted, Is.True);
            Assert.That(Cpu.HaltReason, Is.EqualTo(HaltState.Wai));
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
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xA9); // LDA #$42
        Write(0x1001, 0x42);
        Write(0x1002, 0xEA); // NOP
        Cpu.Reset();

        // Execute without debugger
        int cyclesWithout = (int)Cpu.Step().CyclesConsumed.Value + (int)Cpu.Step().CyclesConsumed.Value;
        ulong totalCyclesWithout = Cpu.GetCycles();

        // Reset and execute with debugger
        Cpu.Reset();
        var listener = new TestDebugListener();
        Cpu.AttachDebugger(listener);
        int cyclesWith = (int)Cpu.Step().CyclesConsumed.Value + (int)Cpu.Step().CyclesConsumed.Value;
        ulong totalCyclesWith = Cpu.GetCycles();

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
        private readonly ICpu Cpu;
        private readonly int stopAfter;

        public StopAfterNStepsListener(ICpu Cpu, int stopAfter)
        {
            this.Cpu = Cpu;
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
                Cpu.RequestStop();
            }
        }
    }
}