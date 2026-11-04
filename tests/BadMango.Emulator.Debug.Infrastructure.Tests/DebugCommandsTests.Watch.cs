// <copyright file="DebugCommandsTests.Watch.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for watchpoint hit value tracking.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that LastHitValue is populated after a watched address is read.
    /// </summary>
    [Test]
    public void WatchpointManager_LastHitValue_IsSet_AfterReadHit()
    {
        // LDA $2000 (absolute) = AD 00 20
        WriteByte(bus, 0x1000, 0xAD);
        WriteByte(bus, 0x1001, 0x00);
        WriteByte(bus, 0x1002, 0x20);
        WriteByte(bus, 0x2000, 0xBE); // value to be read
        cpu.Reset();

        debugContext.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);
        debugContext.Watchpoints.AttachWithBus(cpu, bus);
        cpu.AttachDebugger(debugContext.Watchpoints);

        var step = new StepCommand();
        step.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(debugContext.Watchpoints.LastHitAddress, Is.EqualTo(0x2000u));
            Assert.That(debugContext.Watchpoints.LastHitAccess, Is.EqualTo(WatchAccess.Read));
            Assert.That(debugContext.Watchpoints.LastHitValue, Is.EqualTo((byte)0xBE));
        });
    }

    /// <summary>
    /// Verifies that LastHitValue is populated after a watched address is written.
    /// </summary>
    [Test]
    public void WatchpointManager_LastHitValue_IsSet_AfterWriteHit()
    {
        // STA $2000 (absolute) = 8D 00 20 — value stored is the accumulator value
        // LDA #$42 (immediate) at 0x1000, then STA at 0x1002
        WriteByte(bus, 0x1000, 0xA9); // LDA #imm
        WriteByte(bus, 0x1001, 0x42); // #$42
        WriteByte(bus, 0x1002, 0x8D); // STA abs
        WriteByte(bus, 0x1003, 0x00);
        WriteByte(bus, 0x1004, 0x20);
        cpu.Reset();

        debugContext.Watchpoints.Add(0x2000, WatchAccess.Write, stopOnHit: false);
        debugContext.Watchpoints.AttachWithBus(cpu, bus);
        cpu.AttachDebugger(debugContext.Watchpoints);

        var step = new StepCommand();
        step.Execute(debugContext, ["2"]); // step twice: LDA then STA

        Assert.Multiple(() =>
        {
            Assert.That(debugContext.Watchpoints.LastHitAddress, Is.EqualTo(0x2000u));
            Assert.That(debugContext.Watchpoints.LastHitAccess, Is.EqualTo(WatchAccess.Write));
            Assert.That(debugContext.Watchpoints.LastHitValue, Is.EqualTo((byte)0x42));
        });
    }

    /// <summary>
    /// Verifies that ResetLastHit clears the LastHitValue.
    /// </summary>
    [Test]
    public void WatchpointManager_ResetLastHit_ClearsLastHitValue()
    {
        WriteByte(bus, 0x1000, 0xAD);
        WriteByte(bus, 0x1001, 0x00);
        WriteByte(bus, 0x1002, 0x20);
        WriteByte(bus, 0x2000, 0x99);
        cpu.Reset();

        debugContext.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);
        debugContext.Watchpoints.AttachWithBus(cpu, bus);
        cpu.AttachDebugger(debugContext.Watchpoints);
        new StepCommand().Execute(debugContext, []);

        debugContext.Watchpoints.ResetLastHit();

        Assert.Multiple(() =>
        {
            Assert.That(debugContext.Watchpoints.LastHitAddress, Is.Null);
            Assert.That(debugContext.Watchpoints.LastHitValue, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that the watch list command includes the last-hit value in its output.
    /// </summary>
    [Test]
    public void WatchCommand_List_ShowsLastHitValue_WhenWatchpointFired()
    {
        WriteByte(bus, 0x1000, 0xAD);
        WriteByte(bus, 0x1001, 0x00);
        WriteByte(bus, 0x1002, 0x20);
        WriteByte(bus, 0x2000, 0xAB);
        cpu.Reset();

        new WatchCommand().Execute(debugContext, ["add", "$2000", "r"]);
        debugContext.Watchpoints.AttachWithBus(cpu, bus);
        cpu.AttachDebugger(debugContext.Watchpoints);
        new StepCommand().Execute(debugContext, []);
        outputWriter.GetStringBuilder().Clear();

        new WatchCommand().Execute(debugContext, ["list"]);

        var output = outputWriter.ToString();
        Assert.That(output, Does.Contain("$AB").Or.Contain("AB"));
    }
}