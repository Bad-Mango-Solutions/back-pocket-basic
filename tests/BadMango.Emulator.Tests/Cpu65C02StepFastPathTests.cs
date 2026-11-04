// <copyright file="Cpu65C02StepFastPathTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;
using BadMango.Emulator.Emulation.Cpu;

using Core;
using Core.Cpu;

using TestHelpers;

/// <summary>
/// Tests for the <see cref="Cpu65C02.Step"/> fast paths: the pending-interrupt
/// pre-check, the concrete scheduler call, and skipping trap lookups while no
/// traps are registered. Behavior must be identical to the general path.
/// </summary>
[TestFixture]
public class Cpu65C02StepFastPathTests : CpuTestBase
{
    /// <summary>
    /// A level-triggered IRQ asserted while masked stays pending and is taken
    /// after the post-CLI delay window completes.
    /// </summary>
    [Test]
    public void MaskedIrq_RemainsPending_TakenAfterCli()
    {
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000);
        Write(0x1000, 0x78); // SEI
        Write(0x1001, 0xEA); // NOP (IRQ masked)
        Write(0x1002, 0x58); // CLI
        Write(0x1003, 0xEA); // NOP (should be pre-empted by IRQ)
        Cpu.Reset();

        Cpu.Step();        // SEI
        Cpu.SignalIRQ();
        Cpu.Step();        // NOP, masked
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002));

        Cpu.Step();        // CLI
        Cpu.Step();        // NOP executes in the post-CLI delay window
        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1004));

        Cpu.Step();        // IRQ taken on the following boundary

        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000));
    }

    /// <summary>
    /// When the IRQ line is deasserted before the CPU samples it, no interrupt is taken.
    /// </summary>
    [Test]
    public void IrqAssertedThenDeasserted_NotTaken()
    {
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000);
        Write(0x1000, 0x58); // CLI
        Write(0x1001, 0xEA); // NOP
        Cpu.Reset();
        var signals = Cpu.EventContext.Signals;

        Cpu.Step(); // CLI
        signals.Assert(SignalLine.IRQ, 7, Cycle.Zero);
        signals.Deassert(SignalLine.IRQ, 7, Cycle.Zero);
        Cpu.Step(); // NOP

        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002));
    }

    /// <summary>
    /// An NMI edge is serviced exactly once even if the line stays asserted.
    /// </summary>
    [Test]
    public void NmiEdge_ServicedOnce_WhileLineHeld()
    {
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFA, 0x3000);
        Write(0x3000, 0xEA); // NOP in handler
        Write(0x3001, 0xEA); // NOP in handler
        Cpu.Reset();

        Cpu.SignalNMI();
        Cpu.Step(); // NMI taken
        Cpu.Step(); // NOP at $3000 (line still asserted, no new edge)
        Cpu.Step(); // NOP at $3001

        Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x3002));
    }

    /// <summary>
    /// A trap registered after the registry was assigned to the CPU (while it was
    /// empty) still fires, and stops firing once unregistered.
    /// </summary>
    [Test]
    public void TrapRegisteredAfterAssignment_FiresAndStopsAfterUnregister()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0x4C); // JMP $1000 (tight loop)
        Write(0x1001, 0x00);
        Write(0x1002, 0x10);
        Cpu.Reset();

        var registry = new TrapRegistry();
        Cpu.TrapRegistry = registry;
        int hits = 0;

        Cpu.Step(); // JMP, no traps yet
        Assert.That(hits, Is.Zero);

        registry.Register(
            0x1000,
            "test",
            TrapCategory.MonitorRom,
            (_, _, _) =>
            {
                hits++;
                return TrapResult.SuccessWithRedirect(new(3), 0x1000);
            });
        Cpu.Step();
        Assert.That(hits, Is.EqualTo(1), "trap registered after assignment must fire");

        registry.Unregister(0x1000);
        Cpu.Step();
        Assert.That(hits, Is.EqualTo(1), "unregistered trap must not fire");
    }

    /// <summary>
    /// Resetting the trap registry to <see langword="null"/> falls back to
    /// <see cref="NullTrapRegistry"/> and executes normally.
    /// </summary>
    [Test]
    public void NullTrapRegistry_ExecutesNormally()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA);
        Cpu.Reset();
        Cpu.TrapRegistry = null!;

        var result = Cpu.Step();

        Assert.Multiple(() =>
        {
            Assert.That(Cpu.TrapRegistry, Is.SameAs(NullTrapRegistry.Instance));
            Assert.That(result.CyclesConsumed.Value, Is.EqualTo(2UL));
            Assert.That(Cpu.Registers.PC.GetWord(), Is.EqualTo(0x1001));
        });
    }

    /// <summary>
    /// The scheduler is advanced by each instruction's cycles through the fast path.
    /// </summary>
    [Test]
    public void Step_AdvancesSchedulerByInstructionCycles()
    {
        WriteWord(0xFFFC, 0x1000);
        Write(0x1000, 0xEA); // NOP (2)
        Write(0x1001, 0xAD); // LDA $2000 (4)
        Write(0x1002, 0x00);
        Write(0x1003, 0x20);
        Cpu.Reset();
        ulong start = Cpu.EventContext.Now.Value;

        Cpu.Step();
        Cpu.Step();

        Assert.That(Cpu.EventContext.Now.Value - start, Is.EqualTo(6UL));
    }

    /// <summary>
    /// With a non-<see cref="SignalBus"/> implementation the CPU still performs the
    /// full interrupt check every step (no pre-check is available), including the
    /// delayed CLI/SEI/PLP IRQ-mask semantics.
    /// </summary>
    [Test]
    public void CustomSignalBus_InterruptStillTaken()
    {
        var mainBus = (MainBus)Bus;
        var signals = new FakeSignalBus();
        var cpu = new Cpu65C02(new EventContext(new Scheduler(), signals, mainBus));
        WriteWord(0xFFFC, 0x1000);
        WriteWord(0xFFFE, 0x2000);
        Write(0x1000, 0x58); // CLI
        Write(0x1001, 0xEA);
        cpu.Reset();

        cpu.Step(); // CLI
        signals.IrqAsserted = true;
        cpu.Step(); // NOP executes before IRQ is taken
        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x1002));
        cpu.Step(); // IRQ

        Assert.That(cpu.Registers.PC.GetWord(), Is.EqualTo(0x2000));
    }

    /// <summary>
    /// Minimal <see cref="ISignalBus"/> that is not the concrete <see cref="SignalBus"/>.
    /// </summary>
    private sealed class FakeSignalBus : ISignalBus
    {
        /// <inheritdoc/>
        public event Action<SignalLine, bool, int, Cycle>? SignalChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Gets or sets a value indicating whether IRQ reads as asserted.
        /// </summary>
        public bool IrqAsserted { get; set; }

        /// <inheritdoc/>
        public void Assert(SignalLine line, int deviceId, Cycle cycle)
        {
        }

        /// <inheritdoc/>
        public void Deassert(SignalLine line, int deviceId, Cycle cycle)
        {
        }

        /// <inheritdoc/>
        public bool IsAsserted(SignalLine line) => line == SignalLine.IRQ && IrqAsserted;

        /// <inheritdoc/>
        public bool ConsumeNmiEdge() => false;
    }
}