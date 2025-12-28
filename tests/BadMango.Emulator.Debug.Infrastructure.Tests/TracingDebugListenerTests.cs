// <copyright file="TracingDebugListenerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Core;

using Core.Cpu;

/// <summary>
/// Unit tests for the <see cref="TracingDebugListener"/> class.
/// </summary>
[TestFixture]
public class TracingDebugListenerTests
{
    /// <summary>
    /// Verifies that the listener is disabled by default.
    /// </summary>
    [Test]
    public void IsEnabled_DefaultsFalse()
    {
        var listener = new TracingDebugListener();
        Assert.That(listener.IsEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that the listener can be enabled and disabled.
    /// </summary>
    [Test]
    public void IsEnabled_CanBeToggled()
    {
        var listener = new TracingDebugListener();

        listener.IsEnabled = true;
        Assert.That(listener.IsEnabled, Is.True);

        listener.IsEnabled = false;
        Assert.That(listener.IsEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that OnAfterStep increments InstructionCount when enabled.
    /// </summary>
    [Test]
    public void OnAfterStep_IncrementsInstructionCount_WhenEnabled()
    {
        var listener = new TracingDebugListener
        {
            IsEnabled = true,
            BufferOutput = true,
        };

        var eventData = CreateTestEventData();
        listener.OnAfterStep(in eventData);

        Assert.That(listener.InstructionCount, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that OnAfterStep does not increment InstructionCount when disabled.
    /// </summary>
    [Test]
    public void OnAfterStep_DoesNotIncrementInstructionCount_WhenDisabled()
    {
        var listener = new TracingDebugListener
        {
            IsEnabled = false,
        };

        var eventData = CreateTestEventData();
        listener.OnAfterStep(in eventData);

        Assert.That(listener.InstructionCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that OnAfterStep buffers records when BufferOutput is true.
    /// </summary>
    [Test]
    public void OnAfterStep_BuffersRecords_WhenBufferOutputIsTrue()
    {
        var listener = new TracingDebugListener
        {
            IsEnabled = true,
            BufferOutput = true,
        };

        var eventData = CreateTestEventData();
        listener.OnAfterStep(in eventData);

        var records = listener.GetBufferedRecords();
        Assert.That(records, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Verifies that ClearBuffer removes all buffered records.
    /// </summary>
    [Test]
    public void ClearBuffer_RemovesAllRecords()
    {
        var listener = new TracingDebugListener
        {
            IsEnabled = true,
            BufferOutput = true,
        };

        var eventData = CreateTestEventData();
        listener.OnAfterStep(in eventData);
        listener.OnAfterStep(in eventData);

        listener.ClearBuffer();

        var records = listener.GetBufferedRecords();
        Assert.That(records, Is.Empty);
    }

    /// <summary>
    /// Verifies that ResetInstructionCount resets the count to zero.
    /// </summary>
    [Test]
    public void ResetInstructionCount_SetsCountToZero()
    {
        var listener = new TracingDebugListener
        {
            IsEnabled = true,
            BufferOutput = true,
        };

        var eventData = CreateTestEventData();
        listener.OnAfterStep(in eventData);
        listener.OnAfterStep(in eventData);

        listener.ResetInstructionCount();

        Assert.That(listener.InstructionCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that GetRecentRecords returns the correct number of records.
    /// </summary>
    [Test]
    public void GetRecentRecords_ReturnsCorrectCount()
    {
        var listener = new TracingDebugListener
        {
            IsEnabled = true,
            BufferOutput = true,
        };

        var eventData = CreateTestEventData();
        for (int i = 0; i < 10; i++)
        {
            listener.OnAfterStep(in eventData);
        }

        var recent = listener.GetRecentRecords(5);
        Assert.That(recent, Has.Count.EqualTo(5));
    }

    /// <summary>
    /// Verifies that buffer respects MaxBufferedRecords limit.
    /// </summary>
    [Test]
    public void OnAfterStep_RespectsMaxBufferedRecords()
    {
        var listener = new TracingDebugListener
        {
            IsEnabled = true,
            BufferOutput = true,
            MaxBufferedRecords = 100, // Minimum allowed value
        };

        var eventData = CreateTestEventData();
        for (int i = 0; i < 150; i++)
        {
            listener.OnAfterStep(in eventData);
        }

        var records = listener.GetBufferedRecords();
        Assert.That(records, Has.Count.LessThanOrEqualTo(100));
    }

    /// <summary>
    /// Verifies that FormatTraceRecord produces expected output format.
    /// </summary>
    [Test]
    public void FormatTraceRecord_ProducesExpectedFormat()
    {
        var record = new TraceRecord
        {
            PC = 0x1000,
            Opcode = 0xA9,
            Instruction = CpuInstructions.LDA,
            AddressingMode = CpuAddressingModes.Immediate,
            OperandSize = 1,
            Operands = new OperandBuffer { [0] = 0x42 },
            A = 0x42,
            X = 0x00,
            Y = 0x00,
            SP = 0xFF,
            P = ProcessorStatusFlags.I,
            Cycles = 2,
            InstructionCycles = 2,
        };

        string formatted = TracingDebugListener.FormatTraceRecord(record);

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Does.Contain("$1000"));
            Assert.That(formatted, Does.Contain("A9 42"));
            Assert.That(formatted, Does.Contain("LDA"));
            Assert.That(formatted, Does.Contain("#$42"));
            Assert.That(formatted, Does.Contain("; A=42"));
            Assert.That(formatted, Does.Contain("SP=FF"));
        });
    }

    /// <summary>
    /// Verifies that FormatTraceRecord handles implied addressing mode.
    /// </summary>
    [Test]
    public void FormatTraceRecord_HandlesImpliedAddressingMode()
    {
        var record = new TraceRecord
        {
            PC = 0x1000,
            Opcode = 0xEA,
            Instruction = CpuInstructions.NOP,
            AddressingMode = CpuAddressingModes.Implied,
            OperandSize = 0,
            A = 0x00,
            X = 0x00,
            Y = 0x00,
            SP = 0xFF,
            P = ProcessorStatusFlags.I,
            Cycles = 2,
            InstructionCycles = 2,
        };

        string formatted = TracingDebugListener.FormatTraceRecord(record);

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Does.Contain("$1000"));
            Assert.That(formatted, Does.Contain("EA"));
            Assert.That(formatted, Does.Contain("NOP"));
        });
    }

    /// <summary>
    /// Verifies that FormatTraceRecord handles relative branches.
    /// </summary>
    [Test]
    public void FormatTraceRecord_HandlesRelativeBranch()
    {
        var record = new TraceRecord
        {
            PC = 0x1000,
            Opcode = 0xF0,
            Instruction = CpuInstructions.BEQ,
            AddressingMode = CpuAddressingModes.Relative,
            OperandSize = 1,
            Operands = new OperandBuffer { [0] = 0x10 }, // +16 forward
            A = 0x00,
            X = 0x00,
            Y = 0x00,
            SP = 0xFF,
            P = ProcessorStatusFlags.Z,
            Cycles = 3,
            InstructionCycles = 3,
        };

        string formatted = TracingDebugListener.FormatTraceRecord(record);

        // Target = $1000 + 2 + 16 = $1012
        Assert.That(formatted, Does.Contain("$1012"));
    }

    /// <summary>
    /// Verifies that FormatTraceRecord shows halt state.
    /// </summary>
    [Test]
    public void FormatTraceRecord_ShowsHaltState()
    {
        var record = new TraceRecord
        {
            PC = 0x1000,
            Opcode = 0xDB,
            Instruction = CpuInstructions.STP,
            AddressingMode = CpuAddressingModes.Implied,
            OperandSize = 0,
            A = 0x00,
            X = 0x00,
            Y = 0x00,
            SP = 0xFF,
            P = ProcessorStatusFlags.I,
            Cycles = 3,
            InstructionCycles = 3,
            Halted = true,
            HaltReason = HaltState.Stp,
        };

        string formatted = TracingDebugListener.FormatTraceRecord(record);

        Assert.That(formatted, Does.Contain("HALT:Stp"));
    }

    /// <summary>
    /// Verifies that console output works when configured.
    /// </summary>
    [Test]
    public void OnAfterStep_WritesToConsole_WhenConfigured()
    {
        using var outputWriter = new StringWriter();
        var listener = new TracingDebugListener
        {
            IsEnabled = true,
            BufferOutput = false,
        };
        listener.SetConsoleOutput(outputWriter);

        var eventData = CreateTestEventData();
        listener.OnAfterStep(in eventData);

        string output = outputWriter.ToString();
        Assert.That(output, Is.Not.Empty);
        Assert.That(output, Does.Contain("LDA"));
    }

    private static DebugStepEventArgs CreateTestEventData()
    {
        return new DebugStepEventArgs
        {
            PC = 0x1000,
            Opcode = 0xA9,
            Instruction = CpuInstructions.LDA,
            AddressingMode = CpuAddressingModes.Immediate,
            OperandSize = 1,
            Operands = new OperandBuffer { [0] = 0x42 },
            EffectiveAddress = 0,
            Registers = new Registers
            {
                A = new RegisterAccumulator { acc = 0x42 },
                X = new RegisterIndex { index = 0 },
                Y = new RegisterIndex { index = 0 },
                SP = new RegisterStackPointer { stack = 0xFF },
                P = ProcessorStatusFlags.I,
            },
            Cycles = 2,
            InstructionCycles = 2,
            Halted = false,
        };
    }
}