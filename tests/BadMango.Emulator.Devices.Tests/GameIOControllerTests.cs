// <copyright file="GameIOControllerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="GameIOController"/> class.
/// </summary>
[TestFixture]
public class GameIOControllerTests
{
    private GameIOController controller = null!;
    private IOPageDispatcher dispatcher = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        controller = new();
        dispatcher = new();
        controller.RegisterHandlers(dispatcher);
    }

    /// <summary>
    /// Verifies that Name returns the correct value.
    /// </summary>
    [Test]
    public void Name_ReturnsGameIOController()
    {
        Assert.That(controller.Name, Is.EqualTo("Game I/O Controller"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsGameIO()
    {
        Assert.That(controller.DeviceType, Is.EqualTo("GameIO"));
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
    /// Verifies that SetButton sets button state.
    /// </summary>
    [Test]
    public void SetButton_SetsButtonState()
    {
        controller.SetButton(0, true);

        Assert.That(controller.Buttons[0], Is.True);
    }

    /// <summary>
    /// Verifies that SetButton throws for invalid index.
    /// </summary>
    [Test]
    public void SetButton_InvalidIndex_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.SetButton(4, true));
    }

    /// <summary>
    /// Verifies that reading button returns pressed state.
    /// </summary>
    [Test]
    public void ReadButton_WhenPressed_ReturnsBit7Set()
    {
        controller.SetButton(0, true);
        var context = CreateTestContext(cycle: 0);

        byte result = dispatcher.Read(0x61, in context); // PB0

        Assert.That(result, Is.EqualTo(0x80));
    }

    /// <summary>
    /// Verifies that reading button returns unpressed state.
    /// </summary>
    [Test]
    public void ReadButton_WhenNotPressed_ReturnsZero()
    {
        var context = CreateTestContext(cycle: 0);

        byte result = dispatcher.Read(0x61, in context); // PB0

        Assert.That(result, Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that SetPaddle sets paddle position.
    /// </summary>
    [Test]
    public void SetPaddle_SetsPaddlePosition()
    {
        controller.SetPaddle(0, 128);

        Assert.That(controller.Paddles[0], Is.EqualTo(128));
    }

    /// <summary>
    /// Verifies that SetPaddle throws for invalid index.
    /// </summary>
    [Test]
    public void SetPaddle_InvalidIndex_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => controller.SetPaddle(4, 128));
    }

    /// <summary>
    /// Verifies that SetJoystick maps to paddles 0 and 1.
    /// </summary>
    [Test]
    public void SetJoystick_MapsToPaddles()
    {
        controller.SetJoystick(0f, 0f); // Center position

        Assert.Multiple(() =>
        {
            // 0 maps to 127.5 (center)
            Assert.That(controller.Paddles[0], Is.InRange(127, 128));
            Assert.That(controller.Paddles[1], Is.InRange(127, 128));
        });
    }

    /// <summary>
    /// Verifies that SetJoystick at extremes works correctly.
    /// </summary>
    [Test]
    public void SetJoystick_AtExtremes_MapsCorrectly()
    {
        controller.SetJoystick(-1f, -1f); // Top-left
        Assert.That(controller.Paddles[0], Is.EqualTo(0));
        Assert.That(controller.Paddles[1], Is.EqualTo(0));

        controller.SetJoystick(1f, 1f); // Bottom-right
        Assert.That(controller.Paddles[0], Is.EqualTo(255));
        Assert.That(controller.Paddles[1], Is.EqualTo(255));
    }

    /// <summary>
    /// Verifies that paddle trigger starts timers.
    /// </summary>
    [Test]
    public void ReadC070_TriggersPaddleTimers()
    {
        controller.SetPaddle(0, 100);
        var triggerContext = CreateTestContext(cycle: 1000);

        // Trigger paddles
        _ = dispatcher.Read(0x70, in triggerContext);

        // Read paddle immediately - should have bit 7 set (timer running)
        var readContext = CreateTestContext(cycle: 1001);
        byte result = dispatcher.Read(0x64, in readContext);

        Assert.That(result, Is.EqualTo(0x80)); // Timer still running
    }

    /// <summary>
    /// Verifies that paddle read returns 0 after timer expires.
    /// </summary>
    [Test]
    public void ReadPaddle_AfterTimerExpires_ReturnsZero()
    {
        controller.SetPaddle(0, 10); // Short timer
        var triggerContext = CreateTestContext(cycle: 1000);
        _ = dispatcher.Read(0x70, in triggerContext);

        // Read paddle after timer should have expired (10 * 11 = 110 cycles)
        var readContext = CreateTestContext(cycle: 1200);
        byte result = dispatcher.Read(0x64, in readContext);

        Assert.That(result, Is.EqualTo(0x00)); // Timer expired
    }

    /// <summary>
    /// Verifies that Reset clears all state.
    /// </summary>
    [Test]
    public void Reset_ClearsAllState()
    {
        controller.SetButton(0, true);
        controller.SetPaddle(0, 128);

        controller.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(controller.Buttons[0], Is.False);
            Assert.That(controller.Paddles[0], Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't trigger paddles.
    /// </summary>
    [Test]
    public void ReadC070_WithNoSideEffects_DoesNotTrigger()
    {
        controller.SetPaddle(0, 100);
        var triggerContext = CreateTestContextWithNoSideEffects(cycle: 1000);

        // Try to trigger paddles with no side effects
        _ = dispatcher.Read(0x70, in triggerContext);

        // Read paddle - should return 0 since timers weren't started
        var readContext = CreateTestContext(cycle: 1001);
        byte result = dispatcher.Read(0x64, in readContext);

        Assert.That(result, Is.EqualTo(0x00));
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

    private static BusAccess CreateTestContext(ulong cycle)
    {
        return new(
            Address: 0xC060,
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
            Address: 0xC060,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: cycle,
            Flags: AccessFlags.NoSideEffects);
    }
}