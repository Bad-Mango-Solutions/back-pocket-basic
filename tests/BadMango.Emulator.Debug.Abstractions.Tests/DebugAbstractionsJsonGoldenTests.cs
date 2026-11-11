// <copyright file="DebugAbstractionsJsonGoldenTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions.Tests;

using System.Text.Json;

using BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// Golden-JSON tests for the debugger abstraction contracts.
/// </summary>
[TestFixture]
public class DebugAbstractionsJsonGoldenTests
{
    /// <summary>
    /// Confirms the register snapshot serializes with the stable debug schema.
    /// </summary>
    [Test]
    public void RegisterSnapshot_SerializesWithExpectedJson()
    {
        var value = new RegisterSnapshot(
            CpuType: "65C02",
            ProgramCounter: 0x1234,
            Accumulator: 0xA5,
            XRegister: 0x10,
            YRegister: 0x20,
            StackPointer: 0xFD,
            StatusRegister: 0x24,
            DirectPage: 0x0000,
            DataBank: 0x00,
            ExtraBank: 0x00,
            KRegister: null,
            BRegister: null);

        var json = JsonSerializer.Serialize(value, DebugSessionJsonContext.Default.RegisterSnapshot);

        Assert.That(json, Is.EqualTo("{
  "cpuType": "65C02",
  "programCounter": 4660,
  "accumulator": 165,
  "xRegister": 16,
  "yRegister": 32,
  "stackPointer": 253,
  "statusRegister": 36,
  "directPage": 0,
  "dataBank": 0,
  "extraBank": 0
}"));
    }

    /// <summary>
    /// Confirms memory blocks are emitted as arrays rather than base64 payloads.
    /// </summary>
    [Test]
    public void MemoryBlock_SerializesWithExpectedJson()
    {
        var value = new MemoryBlock(0xC000, 3, [0x01, 0x02, 0x03], "boot page");

        var json = JsonSerializer.Serialize(value, DebugSessionJsonContext.Default.MemoryBlock);

        Assert.That(json, Is.EqualTo("{
  "address": 49152,
  "length": 3,
  "data": [
    1,
    2,
    3
  ],
  "label": "boot page"
}"));
    }

    /// <summary>
    /// Confirms the disassembly line records render a compact instruction summary.
    /// </summary>
    [Test]
    public void DisassemblyLine_SerializesWithExpectedJson()
    {
        var value = new DisassemblyLine(0xF000, "LDA", "#$42", 2, "A9 42", "load immediate");

        var json = JsonSerializer.Serialize(value, DebugSessionJsonContext.Default.DisassemblyLine);

        Assert.That(json, Is.EqualTo("{
  "address": 61440,
  "mnemonic": "LDA",
  "operand": "#$42",
  "size": 2,
  "bytes": "A9 42",
  "comment": "load immediate"
}"));
    }

    /// <summary>
    /// Confirms run conditions are emitted as named enum values.
    /// </summary>
    [Test]
    public void RunCondition_SerializesWithExpectedJson()
    {
        var value = new RunCondition(
            Kind: RunConditionKind.ProgramCounter,
            Address: 0x1000,
            Label: "entry");

        var json = JsonSerializer.Serialize(value, DebugSessionJsonContext.Default.RunCondition);

        Assert.That(json, Is.EqualTo("{
  "kind": "ProgramCounter",
  "address": 4096,
  "label": "entry"
}"));
    }

    /// <summary>
    /// Confirms run budgets use the stable cycle and wall-clock fields.
    /// </summary>
    [Test]
    public void RunBudget_SerializesWithExpectedJson()
    {
        var value = new RunBudget(5000, TimeSpan.FromSeconds(2));

        var json = JsonSerializer.Serialize(value, DebugSessionJsonContext.Default.RunBudget);

        Assert.That(json, Is.EqualTo("{
  "maxCycles": 5000,
  "wallClock": "00:00:02"
}"));
    }

    /// <summary>
    /// Confirms run results serialize with string enums and final PC metadata.
    /// </summary>
    [Test]
    public void RunResult_SerializesWithExpectedJson()
    {
        var value = new RunResult(
            StopReason: StopReason.Breakpoint,
            InstructionCount: 42,
            CycleCount: 128,
            Elapsed: TimeSpan.FromMilliseconds(75),
            ProgramCounter: 0x400,
            Message: "hit breakpoint");

        var json = JsonSerializer.Serialize(value, DebugSessionJsonContext.Default.RunResult);

        Assert.That(json, Is.EqualTo("{
  "stopReason": "Breakpoint",
  "instructionCount": 42,
  "cycleCount": 128,
  "elapsed": "00:00:00.0750000",
  "programCounter": 1024,
  "message": "hit breakpoint"
}"));
    }

    /// <summary>
    /// Confirms the JSON envelope is versioned and carries the payload wrapper.
    /// </summary>
    [Test]
    public void DebugEnvelope_SerializesWithSchemaVersion()
    {
        var value = new DebugEnvelope<RunResult>(
            new RunResult(
                StopReason: StopReason.Halted,
                InstructionCount: 11,
                CycleCount: 29,
                Elapsed: TimeSpan.FromMilliseconds(12),
                ProgramCounter: 0x200,
                Message: "done"),
            timestamp: new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero),
            correlationId: "session-42");

        var json = JsonSerializer.Serialize(value, DebugSessionJsonContext.Default.DebugEnvelopeRunResult);

        Assert.That(json, Does.Contain(""schema": "emudbg/v1""));
        Assert.That(json, Does.Contain(""correlationId": "session-42""));
        Assert.That(json, Does.Contain(""payload":"));
    }
}