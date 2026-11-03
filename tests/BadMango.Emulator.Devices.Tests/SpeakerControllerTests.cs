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

        /// <inheritdoc />
        public void Play(short[] samples)
        {
            LastPlayedSamples = samples;
            TotalSamplesPlayed += samples.Length;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}