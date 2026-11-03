// <copyright file="SpeakerControllerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="SpeakerController"/> class.
/// </summary>
[TestFixture]
public class SpeakerControllerTests
{
    private SpeakerController controller = null!;
    private IOPageDispatcher dispatcher = null!;
    private RecordingAudioOutput audioOutput = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        audioOutput = new();
        controller = new(audioOutput, startFlushTimer: false);
        dispatcher = new();
        controller.RegisterHandlers(dispatcher);
    }

    /// <summary>
    /// Cleans up test fixtures after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        controller.Dispose();
    }

    /// <summary>
    /// Verifies that Name returns the correct value.
    /// </summary>
    [Test]
    public void Name_ReturnsSpeakerController()
    {
        Assert.That(controller.Name, Is.EqualTo("Speaker Controller"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsSpeaker()
    {
        Assert.That(controller.DeviceType, Is.EqualTo("Speaker"));
    }

    /// <summary>
    /// Verifies that Kind returns Motherboard.
    /// </summary>
    [Test]
    public void Kind_ReturnsMotherboard()
    {
        Assert.That(controller.Kind, Is.EqualTo(PeripheralKind.Motherboard));
    }

    /// <summary>
    /// Verifies that State is initially false.
    /// </summary>
    [Test]
    public void State_InitiallyFalse()
    {
        Assert.That(controller.State, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C030 toggles the speaker.
    /// </summary>
    [Test]
    public void ReadC030_TogglesSpeaker()
    {
        var context = CreateTestContext(cycle: 100);

        _ = dispatcher.Read(0x30, in context);

        Assert.That(controller.State, Is.True);
    }

    /// <summary>
    /// Verifies that multiple reads toggle the speaker back and forth.
    /// </summary>
    [Test]
    public void MultipleReads_ToggleSpeakerBackAndForth()
    {
        var context1 = CreateTestContext(cycle: 100);
        var context2 = CreateTestContext(cycle: 200);
        var context3 = CreateTestContext(cycle: 300);

        _ = dispatcher.Read(0x30, in context1);
        Assert.That(controller.State, Is.True);

        _ = dispatcher.Read(0x30, in context2);
        Assert.That(controller.State, Is.False);

        _ = dispatcher.Read(0x30, in context3);
        Assert.That(controller.State, Is.True);
    }

    /// <summary>
    /// Verifies that writing to $C030 also toggles the speaker.
    /// </summary>
    [Test]
    public void WriteC030_TogglesSpeaker()
    {
        var context = CreateTestContext(cycle: 100);

        dispatcher.Write(0x30, 0x00, in context);

        Assert.That(controller.State, Is.True);
    }

    /// <summary>
    /// Verifies that toggle events are recorded with cycle timestamps.
    /// </summary>
    [Test]
    public void Toggle_RecordsEventsWithCycleTimestamps()
    {
        var context1 = CreateTestContext(cycle: 100);
        var context2 = CreateTestContext(cycle: 200);

        _ = dispatcher.Read(0x30, in context1);
        _ = dispatcher.Read(0x30, in context2);

        Assert.That(controller.PendingToggles, Has.Count.EqualTo(2));
        Assert.That(controller.PendingToggles[0], Is.EqualTo((100UL, true)));
        Assert.That(controller.PendingToggles[1], Is.EqualTo((200UL, false)));
    }

    /// <summary>
    /// Verifies that DrainToggles returns and clears pending toggles.
    /// </summary>
    [Test]
    public void DrainToggles_ReturnsAndClearsPendingToggles()
    {
        var context = CreateTestContext(cycle: 100);
        _ = dispatcher.Read(0x30, in context);

        var drained = controller.DrainToggles();

        Assert.Multiple(() =>
        {
            Assert.That(drained, Has.Count.EqualTo(1));
            Assert.That(drained[0], Is.EqualTo((100UL, true)));
            Assert.That(controller.PendingToggles, Is.Empty);
        });
    }

    /// <summary>
    /// Verifies that the Toggled event is raised.
    /// </summary>
    [Test]
    public void Toggle_RaisesToggledEvent()
    {
        bool eventRaised = false;
        ulong eventCycle = 0;
        bool eventState = false;

        controller.Toggled += (cycle, state) =>
        {
            eventRaised = true;
            eventCycle = cycle;
            eventState = state;
        };

        var context = CreateTestContext(cycle: 150);
        _ = dispatcher.Read(0x30, in context);

        Assert.Multiple(() =>
        {
            Assert.That(eventRaised, Is.True);
            Assert.That(eventCycle, Is.EqualTo(150UL));
            Assert.That(eventState, Is.True);
        });
    }

    /// <summary>
    /// Verifies that Reset clears all state.
    /// </summary>
    [Test]
    public void Reset_ClearsAllState()
    {
        var context = CreateTestContext(cycle: 100);
        _ = dispatcher.Read(0x30, in context);

        controller.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(controller.State, Is.False);
            Assert.That(controller.PendingToggles, Is.Empty);
        });
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't toggle.
    /// </summary>
    [Test]
    public void ReadC030_WithNoSideEffects_DoesNotToggle()
    {
        var context = CreateTestContextWithNoSideEffects(cycle: 100);

        _ = dispatcher.Read(0x30, in context);

        Assert.That(controller.State, Is.False);
    }

    /// <summary>
    /// Verifies that Initialize does not throw.
    /// </summary>
    [Test]
    public void Initialize_DoesNotThrow()
    {
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Scheduler).Returns(Mock.Of<IScheduler>());

        Assert.DoesNotThrow(() => controller.Initialize(mockContext.Object));
    }

    /// <summary>
    /// Verifies cycle-based synthesis produces deterministic sample counts independent of host timing.
    /// </summary>
    [Test]
    public void Toggle_WithKnownCycleDelta_GeneratesExpectedSampleCount()
    {
        var first = CreateTestContext(cycle: 0);
        var second = CreateTestContext(cycle: 510);

        _ = dispatcher.Read(0x30, in first);
        _ = dispatcher.Read(0x30, in second);
        controller.Flush();

        Assert.That(audioOutput.TotalSamplesPlayed, Is.EqualTo(22));
    }

    /// <summary>
    /// Verifies cycle-based synthesis produces alternating square-wave levels.
    /// </summary>
    [Test]
    public void Toggle_WithAlternatingState_GeneratesPositiveThenNegativeSamples()
    {
        var first = CreateTestContext(cycle: 0);
        var second = CreateTestContext(cycle: 510);
        var third = CreateTestContext(cycle: 1020);

        _ = dispatcher.Read(0x30, in first);
        _ = dispatcher.Read(0x30, in second);
        _ = dispatcher.Read(0x30, in third);
        controller.Flush();

        Assert.That(audioOutput.LastPlayedSamples, Has.Length.EqualTo(44));
        Assert.That(audioOutput.LastPlayedSamples.Take(22).All(static sample => sample > 0), Is.True);
        Assert.That(audioOutput.LastPlayedSamples.Skip(22).All(static sample => sample < 0), Is.True);
    }

    /// <summary>
    /// Verifies the buffered audio backlog stays bounded for large cycle deltas.
    /// </summary>
    [Test]
    public void Toggle_WithLargeCycleDelta_KeepsAudioBufferBounded()
    {
        var first = CreateTestContext(cycle: 0);
        var second = CreateTestContext(cycle: 25_000_000);

        _ = dispatcher.Read(0x30, in first);
        _ = dispatcher.Read(0x30, in second);
        controller.Flush();

        Assert.That(audioOutput.LastPlayedSamples, Has.Length.EqualTo(88_200));
    }

    /// <summary>
    /// Verifies the cycle-to-sample conversion follows the machine's configured clock rate.
    /// </summary>
    [Test]
    public void Toggle_WithDoubledMachineClock_HalvesSamplesPerCycle()
    {
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(s => s.Now).Returns(1020UL);
        var machine = new Mock<IMachine>();
        machine.Setup(m => m.Scheduler).Returns(scheduler.Object);
        machine.Setup(m => m.ClockSpeedHz).Returns(2_040_968);
        controller.Initialize(machine.Object);

        var first = CreateTestContext(cycle: 0);
        var second = CreateTestContext(cycle: 1020);

        _ = dispatcher.Read(0x30, in first);
        _ = dispatcher.Read(0x30, in second);
        controller.Flush();

        // 1020 cycles at 2.040968 MHz is 0.5 ms, i.e. 22 samples at 44.1 kHz
        // (the default 1.020484 MHz clock would yield 44).
        Assert.That(audioOutput.TotalSamplesPlayed, Is.EqualTo(22));
    }

    /// <summary>
    /// Verifies the default Apple IIe clock rate is used when the machine reports no clock speed.
    /// </summary>
    [Test]
    public void Toggle_WithUnthrottledMachineClock_UsesDefaultClockRate()
    {
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(s => s.Now).Returns(1020UL);
        var machine = new Mock<IMachine>();
        machine.Setup(m => m.Scheduler).Returns(scheduler.Object);
        machine.Setup(m => m.ClockSpeedHz).Returns(0);
        controller.Initialize(machine.Object);

        var first = CreateTestContext(cycle: 0);
        var second = CreateTestContext(cycle: 1020);

        _ = dispatcher.Read(0x30, in first);
        _ = dispatcher.Read(0x30, in second);
        controller.Flush();

        Assert.That(audioOutput.TotalSamplesPlayed, Is.EqualTo(44));
    }

    /// <summary>
    /// Verifies a cycle delta large enough to overflow 64-bit sample math still yields a full, bounded buffer.
    /// </summary>
    [Test]
    public void Toggle_WithHugeCycleDelta_ClampsWithoutOverflow()
    {
        var first = CreateTestContext(cycle: 0);
        var second = CreateTestContext(cycle: ulong.MaxValue - 1);

        _ = dispatcher.Read(0x30, in first);
        _ = dispatcher.Read(0x30, in second);
        controller.Flush();

        Assert.That(audioOutput.LastPlayedSamples, Has.Length.EqualTo(88_200));
    }

    /// <summary>
    /// Verifies a speaker left untoggled relaxes toward zero instead of holding a DC offset.
    /// </summary>
    [Test]
    public void Toggle_FollowedByLongSilence_DecaysToZero()
    {
        ulong now = 0;
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(s => s.Now).Returns(() => now);
        var machine = new Mock<IMachine>();
        machine.Setup(m => m.Scheduler).Returns(scheduler.Object);
        controller.Initialize(machine.Object);

        var first = CreateTestContext(cycle: 0);
        _ = dispatcher.Read(0x30, in first);

        now = 1_020_484; // One second of emulated time.
        controller.Flush();

        short[] samples = audioOutput.LastPlayedSamples;
        Assert.Multiple(() =>
        {
            Assert.That(samples, Has.Length.EqualTo(44_100));
            Assert.That(samples[0], Is.GreaterThan(0));
            Assert.That(samples[1], Is.LessThan(samples[0]));
            Assert.That(samples.Skip(44_100 - 4_410).All(static sample => sample == 0), Is.True);
        });
    }

    /// <summary>
    /// Verifies the decay state carries across flushes so split rendering matches a single render.
    /// </summary>
    [Test]
    public void Decay_IsContinuousAcrossFlushes()
    {
        short[] single = RenderToggleThenSilence(flushPoints: [2_000]);
        short[] split = RenderToggleThenSilence(flushPoints: [700, 1_300, 2_000]);

        Assert.That(split, Is.EqualTo(single));
    }

    private static short[] RenderToggleThenSilence(ulong[] flushPoints)
    {
        var output = new RecordingAudioOutput();
        using var speaker = new SpeakerController(output, startFlushTimer: false);
        var io = new IOPageDispatcher();
        speaker.RegisterHandlers(io);

        ulong now = 0;
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(s => s.Now).Returns(() => now);
        var machine = new Mock<IMachine>();
        machine.Setup(m => m.Scheduler).Returns(scheduler.Object);
        speaker.Initialize(machine.Object);

        var first = CreateTestContext(cycle: 0);
        var second = CreateTestContext(cycle: 300);
        _ = io.Read(0x30, in first);
        _ = io.Read(0x30, in second);

        foreach (ulong point in flushPoints)
        {
            now = point;
            speaker.Flush();
        }

        return [.. output.AllSamples];
    }

    private static BusAccess CreateTestContext(ulong cycle)
    {
        return new(
            Address: 0xC030,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: cycle,
            Flags: AccessFlags.None);
    }

    private static BusAccess CreateTestContextWithNoSideEffects(ulong cycle)
    {
        return new(
            Address: 0xC030,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: cycle,
            Flags: AccessFlags.NoSideEffects);
    }

    private sealed class RecordingAudioOutput : IAudioOutput
    {
        /// <summary>
        /// Gets the last sample buffer played.
        /// </summary>
        public short[] LastPlayedSamples { get; private set; } = [];

        /// <summary>
        /// Gets the total number of samples played across all calls.
        /// </summary>
        public int TotalSamplesPlayed { get; private set; }

        /// <summary>
        /// Gets every sample played, in order, across all calls.
        /// </summary>
        public List<short> AllSamples { get; } = [];

        /// <inheritdoc />
        public void Play(short[] samples)
        {
            LastPlayedSamples = samples;
            AllSamples.AddRange(samples);
            TotalSamplesPlayed += samples.Length;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}