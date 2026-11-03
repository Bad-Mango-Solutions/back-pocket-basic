// <copyright file="DebugContextDebuggerAttachmentTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Debugger;
using BadMango.Emulator.Core.Interfaces.Debugging;

/// <summary>
/// Verifies that <see cref="DebugContext"/> attaches its composite step listener to
/// the CPU only while a debugging feature (tracing, watchpoints, or a custom
/// listener) is actually in use.
/// </summary>
[TestFixture]
public class DebugContextDebuggerAttachmentTests
{
    private CommandDispatcher dispatcher = null!;
    private StringWriter outputWriter = null!;
    private StringWriter errorWriter = null!;
    private DebugContext context = null!;
    private TracingDebugListener tracing = null!;
    private IMachine machine = null!;

    /// <summary>
    /// Creates a fresh machine wired into a debug context the way the console does.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        dispatcher = new();
        outputWriter = new();
        errorWriter = new();
        context = new DebugContext(dispatcher, outputWriter, errorWriter);
        (machine, tracing) = AttachNewMachine(context);
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        context.Dispose();
        outputWriter.Dispose();
        errorWriter.Dispose();
    }

    /// <summary>
    /// With tracing off and no watchpoints, no debugger is attached even though
    /// the composite exists (and even if a bare listener was attached beforehand).
    /// </summary>
    [Test]
    public void AttachMachine_NothingActive_DebuggerNotAttached()
    {
        Assert.Multiple(() =>
        {
            Assert.That(context.StepListener, Is.Not.Null);
            Assert.That(context.StepListener!.Count, Is.EqualTo(2));
            Assert.That(context.StepListener.HasActiveListeners, Is.False);
            Assert.That(machine.Cpu.IsDebuggerAttached, Is.False);
        });
    }

    /// <summary>
    /// Enabling tracing attaches the debugger; disabling it detaches again.
    /// </summary>
    [Test]
    public void Tracing_Toggle_AttachesAndDetaches()
    {
        tracing.IsEnabled = true;
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.True, "trace on should attach");

        tracing.IsEnabled = false;
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.False, "trace off should detach");
    }

    /// <summary>
    /// The <c>trace on/off</c> command drives attachment.
    /// </summary>
    [Test]
    public void TraceCommand_OnOff_AttachesAndDetaches()
    {
        var trace = new TraceCommand();

        trace.Execute(context, ["on"]);
        bool attachedWhileOn = machine.Cpu.IsDebuggerAttached;
        trace.Execute(context, ["off"]);

        Assert.Multiple(() =>
        {
            Assert.That(attachedWhileOn, Is.True);
            Assert.That(machine.Cpu.IsDebuggerAttached, Is.False);
        });
    }

    /// <summary>
    /// Adding a watchpoint attaches the debugger; removing the last one detaches.
    /// Disabling (but keeping) a watchpoint keeps the debugger attached.
    /// </summary>
    [Test]
    public void Watchpoints_AddRemove_AttachesAndDetaches()
    {
        context.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);
        context.Watchpoints.Add(0x2001, WatchAccess.Write, stopOnHit: false);
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.True, "watch add should attach");

        context.Watchpoints.SetEnabled(0x2000, false);
        context.Watchpoints.Remove(0x2001);
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.True, "a registered (disabled) watchpoint keeps it attached");

        context.Watchpoints.Remove(0x2000);
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.False, "removing the last watchpoint should detach");

        context.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);
        context.Watchpoints.Clear();
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.False, "clear should detach");
    }

    /// <summary>
    /// Tracing and watchpoints are independent consumers: the debugger stays
    /// attached until both are inactive.
    /// </summary>
    [Test]
    public void TracingAndWatchpoints_DetachOnlyWhenBothInactive()
    {
        tracing.IsEnabled = true;
        context.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);

        tracing.IsEnabled = false;
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.True);

        context.Watchpoints.Remove(0x2000);
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.False);
    }

    /// <summary>
    /// A custom (non-activatable) listener added to the composite is always
    /// active, so it attaches the debugger until removed.
    /// </summary>
    [Test]
    public void CustomListener_AddedToStepListener_AttachesUntilRemoved()
    {
        var custom = new CountingListener();

        context.StepListener!.Add(custom);
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.True);

        machine.Cpu.Step();
        Assert.That(custom.AfterCount, Is.EqualTo(1));

        context.StepListener.Remove(custom);
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.False);
    }

    /// <summary>
    /// End to end: a watchpoint still fires once added, and tracing records
    /// instructions only while enabled.
    /// </summary>
    [Test]
    public void EndToEnd_WatchpointAndTraceStillWork()
    {
        // $1000: LDA $2000 ; $1003: NOP ; $1004: NOP
        machine.Cpu.Poke8(0x1000, 0xAD);
        machine.Cpu.Poke8(0x1001, 0x00);
        machine.Cpu.Poke8(0x1002, 0x20);
        machine.Cpu.Poke8(0x1003, 0xEA);
        machine.Cpu.Poke8(0x1004, 0xEA);
        machine.Cpu.Poke8(0x2000, 0x5A);
        machine.Cpu.SetPC(0x1000);

        context.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);
        tracing.BufferOutput = true;
        tracing.IsEnabled = true;

        machine.Cpu.Step(); // LDA $2000 -> watch hit, traced
        machine.Cpu.Step(); // NOP -> traced

        tracing.IsEnabled = false;
        context.Watchpoints.Clear();
        machine.Cpu.Step(); // NOP -> not traced, debugger detached

        Assert.Multiple(() =>
        {
            Assert.That(context.Watchpoints.LastHitAddress, Is.EqualTo(0x2000u));
            Assert.That(context.Watchpoints.LastHitValue, Is.EqualTo((byte)0x5A));
            Assert.That(tracing.GetBufferedRecords(), Has.Count.EqualTo(2));
            Assert.That(machine.Cpu.IsDebuggerAttached, Is.False);
        });
    }

    /// <summary>
    /// After re-attaching a new machine (profile reload), activity changes must
    /// affect only the new CPU; the old composite must not re-attach to anything.
    /// </summary>
    [Test]
    public void Reattach_OldCompositeNoLongerDrivesAttachment()
    {
        var oldMachine = machine;
        var oldComposite = context.StepListener!;

        (machine, tracing) = AttachNewMachine(context);
        context.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);

        Assert.Multiple(() =>
        {
            Assert.That(context.StepListener, Is.Not.SameAs(oldComposite));
            Assert.That(oldComposite.Count, Is.Zero, "old composite should release managed listeners");
            Assert.That(oldMachine.Cpu.IsDebuggerAttached, Is.False);
            Assert.That(machine.Cpu.IsDebuggerAttached, Is.True);
        });
    }

    /// <summary>
    /// After <see cref="DebugContext.DetachSystem"/>, toggling activity has no effect
    /// on the old CPU and does not throw.
    /// </summary>
    [Test]
    public void DetachSystem_ThenActivityChanges_DoNotReattach()
    {
        var oldTracing = tracing;
        context.Watchpoints.Add(0x2000, WatchAccess.Read, stopOnHit: false);
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.True);

        context.DetachSystem();
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.False);

        Assert.DoesNotThrow(() =>
        {
            oldTracing.IsEnabled = true;
            context.Watchpoints.Add(0x3000, WatchAccess.Read, stopOnHit: false);
        });
        Assert.That(machine.Cpu.IsDebuggerAttached, Is.False);
    }

    private static (IMachine Machine, TracingDebugListener Tracing) AttachNewMachine(DebugContext target)
    {
        var (m, disassembler, info) = MachineFactory.CreateDebugSystem(CreateSimple65C02Profile());
        var listener = new TracingDebugListener();

        // Mirror ProfileCommand: a bare tracing listener is attached before AttachMachine.
        m.Cpu.AttachDebugger(listener);
        target.AttachMachine(m, disassembler, info, listener);
        return (m, listener);
    }

    private static MachineProfile CreateSimple65C02Profile()
    {
        return new()
        {
            Name = "test-65c02",
            DisplayName = "Test 65C02 System",
            Cpu = new()
            {
                Type = "65C02",
                ClockSpeed = 1000000,
            },
            AddressSpace = 16,
            Memory = new()
            {
                Physical =
                [
                    new()
                    {
                        Name = "main-ram-64k",
                        Size = "0x10000",
                        Fill = "0x00",
                    },
                ],
                Regions =
                [
                    new()
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0x10000",
                        Permissions = "rwx",
                        Source = "main-ram-64k",
                        SourceOffset = "0x0000",
                    },
                ],
            },
        };
    }

    /// <summary>
    /// Plain listener that counts after-step callbacks.
    /// </summary>
    private sealed class CountingListener : IDebugStepListener
    {
        /// <summary>
        /// Gets the number of after-step callbacks received.
        /// </summary>
        public int AfterCount { get; private set; }

        /// <inheritdoc/>
        public void OnBeforeStep(in DebugStepEventArgs eventData)
        {
        }

        /// <inheritdoc/>
        public void OnAfterStep(in DebugStepEventArgs eventData) => AfterCount++;
    }
}