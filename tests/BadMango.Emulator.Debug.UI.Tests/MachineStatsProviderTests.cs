// <copyright file="MachineStatsProviderTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Debug.UI.StatusMonitor;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Unit tests for the <see cref="MachineStatsProvider"/> class.
/// </summary>
[TestFixture]
public class MachineStatsProviderTests
{
    /// <summary>
    /// Verifies that the constructor throws when machine is null.
    /// </summary>
    [Test]
    public void Constructor_NullMachine_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MachineStatsProvider(null!));
    }

    /// <summary>
    /// Verifies that Machine property returns the machine passed to constructor.
    /// </summary>
    [Test]
    public void Machine_ReturnsMachineFromConstructor()
    {
        var machine = CreateMockMachine();

        var provider = new MachineStatsProvider(machine.Object);

        Assert.That(provider.Machine, Is.SameAs(machine.Object));
    }

    /// <summary>
    /// Verifies that State returns the machine state.
    /// </summary>
    [Test]
    public void State_ReturnsMachineState()
    {
        var machine = CreateMockMachine();
        machine.Setup(m => m.State).Returns(MachineState.Running);

        var provider = new MachineStatsProvider(machine.Object);

        Assert.That(provider.State, Is.EqualTo(MachineState.Running));
    }

    /// <summary>
    /// Verifies that IsWaitingForInterrupt returns CPU WAI state.
    /// </summary>
    [Test]
    public void IsWaitingForInterrupt_ReturnsCpuWaiState()
    {
        var machine = CreateMockMachine(isWai: true);

        var provider = new MachineStatsProvider(machine.Object);

        Assert.That(provider.IsWaitingForInterrupt, Is.True);
    }

    /// <summary>
    /// Verifies that Registers returns CPU registers.
    /// </summary>
    [Test]
    public void Registers_ReturnsCpuRegisters()
    {
        var machine = CreateMockMachine();
        var expectedRegs = new Registers();

        var provider = new MachineStatsProvider(machine.Object);
        var result = provider.Registers;

        Assert.That(result.A.GetByte(), Is.EqualTo(expectedRegs.A.GetByte()));
    }

    /// <summary>
    /// Verifies that TotalCycles returns CPU cycle count.
    /// </summary>
    [Test]
    public void TotalCycles_ReturnsCpuCycles()
    {
        const ulong expectedCycles = 12345;
        var machine = CreateMockMachine(cycles: expectedCycles);

        var provider = new MachineStatsProvider(machine.Object);

        Assert.That(provider.TotalCycles, Is.EqualTo(expectedCycles));
    }

    /// <summary>
    /// Verifies that TargetMHz returns expected Apple IIe clock speed.
    /// </summary>
    [Test]
    public void TargetMHz_ReturnsAppleIIeClockSpeed()
    {
        var machine = CreateMockMachine();

        var provider = new MachineStatsProvider(machine.Object);

        // Apple IIe runs at approximately 1.0227 MHz
        Assert.That(provider.TargetMHz, Is.EqualTo(1.0227).Within(0.001));
    }

    /// <summary>
    /// Verifies that SchedulerQueueDepth returns scheduler pending event count.
    /// </summary>
    [Test]
    public void SchedulerQueueDepth_ReturnsSchedulerPendingEventCount()
    {
        const int expectedCount = 5;
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(s => s.PendingEventCount).Returns(expectedCount);

        var machine = CreateMockMachine(scheduler: scheduler.Object);

        var provider = new MachineStatsProvider(machine.Object);

        Assert.That(provider.SchedulerQueueDepth, Is.EqualTo(expectedCount));
    }

    /// <summary>
    /// Verifies that AnnunciatorStates returns video device annunciators.
    /// </summary>
    [Test]
    public void AnnunciatorStates_ReturnsVideoDeviceAnnunciators()
    {
        var videoDevice = new Mock<IVideoDevice>();
        videoDevice.Setup(v => v.Annunciators).Returns([true, false, true, false]);

        var machine = CreateMockMachine(videoDevice: videoDevice.Object);

        var provider = new MachineStatsProvider(machine.Object);

        Assert.Multiple(() =>
        {
            Assert.That(provider.AnnunciatorStates[0], Is.True);
            Assert.That(provider.AnnunciatorStates[1], Is.False);
            Assert.That(provider.AnnunciatorStates[2], Is.True);
            Assert.That(provider.AnnunciatorStates[3], Is.False);
        });
    }

    /// <summary>
    /// Verifies that AnnunciatorStates returns all false when no video device.
    /// </summary>
    [Test]
    public void AnnunciatorStates_NoVideoDevice_ReturnsAllFalse()
    {
        var machine = CreateMockMachine();

        var provider = new MachineStatsProvider(machine.Object);

        Assert.Multiple(() =>
        {
            Assert.That(provider.AnnunciatorStates, Has.Count.EqualTo(4));
            Assert.That(provider.AnnunciatorStates.All(a => !a), Is.True);
        });
    }

    /// <summary>
    /// Verifies that RegisterExtension adds extension to list.
    /// </summary>
    [Test]
    public void RegisterExtension_AddsToExtensionsList()
    {
        var machine = CreateMockMachine();
        var extension = new Mock<IStatusWindowExtension>();
        extension.Setup(e => e.Order).Returns(100);

        var provider = new MachineStatsProvider(machine.Object);
        provider.RegisterExtension(extension.Object);

        Assert.That(provider.Extensions, Does.Contain(extension.Object));
    }

    /// <summary>
    /// Verifies that RegisterExtension throws when extension is null.
    /// </summary>
    [Test]
    public void RegisterExtension_NullExtension_ThrowsArgumentNullException()
    {
        var machine = CreateMockMachine();
        var provider = new MachineStatsProvider(machine.Object);

        Assert.Throws<ArgumentNullException>(() => provider.RegisterExtension(null!));
    }

    /// <summary>
    /// Verifies that extensions are sorted by order.
    /// </summary>
    [Test]
    public void RegisterExtension_ExtensionsSortedByOrder()
    {
        var machine = CreateMockMachine();

        var ext1 = new Mock<IStatusWindowExtension>();
        ext1.Setup(e => e.Name).Returns("Ext1");
        ext1.Setup(e => e.Order).Returns(200);

        var ext2 = new Mock<IStatusWindowExtension>();
        ext2.Setup(e => e.Name).Returns("Ext2");
        ext2.Setup(e => e.Order).Returns(50);

        var provider = new MachineStatsProvider(machine.Object);
        provider.RegisterExtension(ext1.Object);
        provider.RegisterExtension(ext2.Object);

        Assert.That(provider.Extensions[0].Name, Is.EqualTo("Ext2"));
        Assert.That(provider.Extensions[1].Name, Is.EqualTo("Ext1"));
    }

    /// <summary>
    /// Verifies that Sample updates metrics without throwing.
    /// </summary>
    [Test]
    public void Sample_DoesNotThrow()
    {
        var machine = CreateMockMachine();
        var provider = new MachineStatsProvider(machine.Object);

        Assert.DoesNotThrow(() => provider.Sample());
    }

    /// <summary>
    /// Verifies that ResetStats clears metrics.
    /// </summary>
    [Test]
    public void ResetStats_ClearsMetrics()
    {
        var machine = CreateMockMachine();
        var provider = new MachineStatsProvider(machine.Object);

        provider.ResetStats();

        Assert.Multiple(() =>
        {
            Assert.That(provider.InstructionsPerSecond, Is.EqualTo(0));
            Assert.That(provider.ActualMHz, Is.EqualTo(0));
            Assert.That(provider.CpuUtilization, Is.EqualTo(0));
            Assert.That(provider.TotalInstructions, Is.EqualTo(0UL));
        });
    }

    /// <summary>
    /// Verifies that WaiDuration returns zero when not in WAI state.
    /// </summary>
    [Test]
    public void WaiDuration_NotInWaiState_ReturnsZero()
    {
        var machine = CreateMockMachine(isWai: false);

        var provider = new MachineStatsProvider(machine.Object);

        Assert.That(provider.WaiDuration, Is.EqualTo(TimeSpan.Zero));
    }

    /// <summary>
    /// Verifies that NextEventCycles returns null when no events pending.
    /// </summary>
    [Test]
    public void NextEventCycles_NoEventsPending_ReturnsNull()
    {
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(s => s.PeekNextDue()).Returns((Core.Cycle?)null);

        var machine = CreateMockMachine(scheduler: scheduler.Object);

        var provider = new MachineStatsProvider(machine.Object);

        Assert.That(provider.NextEventCycles, Is.Null);
    }

    private static Mock<IMachine> CreateMockMachine(
        MachineState state = MachineState.Stopped,
        bool isWai = false,
        ulong cycles = 0,
        IScheduler? scheduler = null,
        IVideoDevice? videoDevice = null)
    {
        var mockCpu = new Mock<ICpu>();
        mockCpu.Setup(c => c.GetRegisters()).Returns(new Registers());
        mockCpu.Setup(c => c.IsWaitingForInterrupt).Returns(isWai);
        mockCpu.Setup(c => c.GetCycles()).Returns(cycles);

        Mock<IScheduler> mockScheduler;
        if (scheduler != null)
        {
            mockScheduler = Mock.Get(scheduler);
        }
        else
        {
            mockScheduler = new Mock<IScheduler>();
            mockScheduler.Setup(s => s.PendingEventCount).Returns(0);
        }

        var machine = new Mock<IMachine>();
        machine.Setup(m => m.Cpu).Returns(mockCpu.Object);
        machine.Setup(m => m.Scheduler).Returns(mockScheduler.Object);
        machine.Setup(m => m.State).Returns(state);
        machine.Setup(m => m.Now).Returns(new Core.Cycle(cycles));

        if (videoDevice != null)
        {
            machine.Setup(m => m.GetComponent<IVideoDevice>()).Returns(videoDevice);
        }

        return machine;
    }
}