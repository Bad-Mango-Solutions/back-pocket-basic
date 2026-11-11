// <copyright file="AppleIIeKeyboardTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

using Moq;

/// <summary>
/// Unit tests for the <see cref="AppleIIeKeyboard"/> class.
/// </summary>
[TestFixture]
public class AppleIIeKeyboardTests
{
    private AppleIIeKeyboard device = null!;
    private IOPageDispatcher dispatcher = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        device = new();
        dispatcher = new();
        device.RegisterHandlers(dispatcher);
    }

    /// <summary>
    /// Verifies that KeyDown sets the key data and strobe.
    /// </summary>
    [Test]
    public void KeyDown_SetsKeyDataAndStrobe()
    {
        device.KeyDown(0x41); // 'A'

        Assert.Multiple(() =>
        {
            Assert.That(device.HasKeyDown, Is.True);
            Assert.That(device.KeyData, Is.EqualTo(0xC1)); // 0x41 | 0x80 (strobe)
        });
    }

    /// <summary>
    /// Verifies that KeyUp clears the key down state but preserves key data.
    /// </summary>
    [Test]
    public void KeyUp_ClearsKeyDownButPreservesKeyData()
    {
        device.KeyDown(0x41);
        device.KeyUp();

        Assert.Multiple(() =>
        {
            Assert.That(device.HasKeyDown, Is.False);
            Assert.That(device.KeyData, Is.EqualTo(0xC1)); // Key data still has strobe
        });
    }

    /// <summary>
    /// Verifies that reading $C000 returns the key data.
    /// </summary>
    [Test]
    public void ReadC000_ReturnsKeyData()
    {
        device.KeyDown(0x42); // 'B'
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x00, in context);

        Assert.That(result, Is.EqualTo(0xC2)); // 0x42 | 0x80
    }

    /// <summary>
    /// Verifies that reading $C010 clears the strobe.
    /// </summary>
    [Test]
    public void ReadC010_ClearsStrobe()
    {
        device.KeyDown(0x41);
        var context = CreateTestContext();

        // Read $C010 to clear strobe
        _ = dispatcher.Read(0x10, in context);

        Assert.That(device.KeyData, Is.EqualTo(0x41)); // Strobe cleared
    }

    /// <summary>
    /// Verifies that reading $C010 returns key down status.
    /// </summary>
    [Test]
    public void ReadC010_ReturnsKeyDownStatus()
    {
        device.KeyDown(0x41);
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x10, in context);

        Assert.That(result, Is.EqualTo(0x80)); // Key is down
    }

    /// <summary>
    /// Verifies that writing to $C010 clears the strobe.
    /// </summary>
    [Test]
    public void WriteC010_ClearsStrobe()
    {
        device.KeyDown(0x41);
        var context = CreateTestContext();

        dispatcher.Write(0x10, 0x00, in context);

        Assert.That(device.KeyData, Is.EqualTo(0x41)); // Strobe cleared
    }

    /// <summary>
    /// Verifies that Reset clears transient key strobe state while preserving held modifiers.
    /// </summary>
    [Test]
    public void Reset_ClearsStrobeStateButPreservesHeldModifiers()
    {
        device.KeyDown(0x41);
        device.SetModifiers(KeyboardModifiers.Shift | KeyboardModifiers.OpenApple);

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.HasKeyDown, Is.False);
            Assert.That(device.KeyData, Is.EqualTo(0x00));
            Assert.That(device.Modifiers, Is.EqualTo(KeyboardModifiers.Shift | KeyboardModifiers.OpenApple));
        });
    }

    /// <summary>
    /// Verifies that SetModifiers sets the modifier state.
    /// </summary>
    [Test]
    public void SetModifiers_SetsModifierState()
    {
        device.SetModifiers(KeyboardModifiers.Control | KeyboardModifiers.OpenApple);

        Assert.That(device.Modifiers, Is.EqualTo(KeyboardModifiers.Control | KeyboardModifiers.OpenApple));
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't clear strobe.
    /// </summary>
    [Test]
    public void ReadC010_WithNoSideEffects_DoesNotClearStrobe()
    {
        device.KeyDown(0x41);
        var context = CreateTestContextWithNoSideEffects();

        _ = dispatcher.Read(0x10, in context);

        Assert.That(device.KeyData, Is.EqualTo(0xC1)); // Strobe still set
    }

    /// <summary>
    /// Verifies that Initialize does not throw.
    /// </summary>
    [Test]
    public void Initialize_DoesNotThrow()
    {
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Scheduler).Returns(Mock.Of<IScheduler>());

        Assert.DoesNotThrow(() => device.Initialize(mockContext.Object));
    }

    /// <summary>
    /// Verifies that AppleIIeKeyboard implements ISoftSwitchProvider.
    /// </summary>
    [Test]
    public void AppleIIeKeyboard_ImplementsISoftSwitchProvider()
    {
        Assert.That(device, Is.InstanceOf<ISoftSwitchProvider>());
    }

    /// <summary>
    /// Verifies that ProviderName returns Keyboard.
    /// </summary>
    [Test]
    public void ProviderName_ReturnsKeyboard()
    {
        var provider = (ISoftSwitchProvider)device;
        Assert.That(provider.ProviderName, Is.EqualTo("Keyboard"));
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns keyboard soft switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsKeyboardSwitches()
    {
        var provider = (ISoftSwitchProvider)device;

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            Assert.That(states, Has.Count.EqualTo(5));
            Assert.That(states.Any(s => s.Name == "KBD" && s.Address == 0xC000), Is.True);
            Assert.That(states.Any(s => s.Name == "KBDSTRB" && s.Address == 0xC010), Is.True);
            Assert.That(states.Any(s => s.Name == "PB0" && s.Address == 0xC061), Is.True);
            Assert.That(states.Any(s => s.Name == "PB1" && s.Address == 0xC062), Is.True);
            Assert.That(states.Any(s => s.Name == "PB2" && s.Address == 0xC063), Is.True);
        });
    }

    /// <summary>
    /// Verifies that KBD switch reflects strobe state when key is pressed.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_KBD_ReflectsStrobeState_WhenKeyPressed()
    {
        var provider = (ISoftSwitchProvider)device;

        // Initially no key pressed
        var initialStates = provider.GetSoftSwitchStates();
        var kbdState = initialStates.First(s => s.Name == "KBD");
        Assert.That(kbdState.Value, Is.False);

        // Press a key
        device.KeyDown(0x41);
        var pressedStates = provider.GetSoftSwitchStates();
        var kbdAfterPress = pressedStates.First(s => s.Name == "KBD");
        Assert.That(kbdAfterPress.Value, Is.True);
    }

    /// <summary>
    /// Verifies that KBDSTRB switch reflects key down state.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_KBDSTRB_ReflectsKeyDownState()
    {
        var provider = (ISoftSwitchProvider)device;

        // Initially no key down
        var initialStates = provider.GetSoftSwitchStates();
        var strbState = initialStates.First(s => s.Name == "KBDSTRB");
        Assert.That(strbState.Value, Is.False);

        // Press a key
        device.KeyDown(0x41);
        var pressedStates = provider.GetSoftSwitchStates();
        var strbAfterPress = pressedStates.First(s => s.Name == "KBDSTRB");
        Assert.That(strbAfterPress.Value, Is.True);

        // Release the key
        device.KeyUp();
        var releasedStates = provider.GetSoftSwitchStates();
        var strbAfterRelease = releasedStates.First(s => s.Name == "KBDSTRB");
        Assert.That(strbAfterRelease.Value, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C061 (PB0) returns $00 when Open Apple is not pressed.
    /// </summary>
    [Test]
    public void ReadC061_WhenOpenAppleNotPressed_ReturnsZero()
    {
        device.SetModifiers(KeyboardModifiers.None);
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x61, in context);

        Assert.That(result, Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that reading $C061 (PB0) returns $80 when Open Apple is pressed.
    /// </summary>
    [Test]
    public void ReadC061_WhenOpenApplePressed_Returns80()
    {
        device.SetModifiers(KeyboardModifiers.OpenApple);
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x61, in context);

        Assert.That(result, Is.EqualTo(0x80));
    }

    /// <summary>
    /// Verifies that reading $C062 (PB1) returns $00 when Closed Apple is not pressed.
    /// </summary>
    [Test]
    public void ReadC062_WhenClosedAppleNotPressed_ReturnsZero()
    {
        device.SetModifiers(KeyboardModifiers.None);
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x62, in context);

        Assert.That(result, Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that reading $C062 (PB1) returns $80 when Closed Apple is pressed.
    /// </summary>
    [Test]
    public void ReadC062_WhenClosedApplePressed_Returns80()
    {
        device.SetModifiers(KeyboardModifiers.ClosedApple);
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x62, in context);

        Assert.That(result, Is.EqualTo(0x80));
    }

    /// <summary>
    /// Verifies that reading $C063 (PB2) returns $00 when Shift is not pressed.
    /// </summary>
    [Test]
    public void ReadC063_WhenShiftNotPressed_ReturnsZero()
    {
        device.SetModifiers(KeyboardModifiers.None);
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x63, in context);

        Assert.That(result, Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that reading $C063 (PB2) returns $80 when Shift is pressed.
    /// </summary>
    [Test]
    public void ReadC063_WhenShiftPressed_Returns80()
    {
        device.SetModifiers(KeyboardModifiers.Shift);
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x63, in context);

        Assert.That(result, Is.EqualTo(0x80));
    }

    /// <summary>
    /// Verifies that both Open Apple and Closed Apple can be pressed simultaneously.
    /// </summary>
    [Test]
    public void PushButtons_BothAppleKeysPressed_BothReturnPressed()
    {
        device.SetModifiers(KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple);
        var context = CreateTestContext();

        byte pb0 = dispatcher.Read(0x61, in context);
        byte pb1 = dispatcher.Read(0x62, in context);

        Assert.Multiple(() =>
        {
            Assert.That(pb0, Is.EqualTo(0x80), "PB0 (Open Apple) should be pressed");
            Assert.That(pb1, Is.EqualTo(0x80), "PB1 (Closed Apple) should be pressed");
        });
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates includes PB0, PB1, and PB2 states.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_IncludesPushButtonStates()
    {
        var provider = (ISoftSwitchProvider)device;
        device.SetModifiers(KeyboardModifiers.OpenApple);

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            Assert.That(states, Has.Count.EqualTo(5));
            Assert.That(states.Any(s => s.Name == "PB0" && s.Address == 0xC061), Is.True, "Should have PB0 state");
            Assert.That(states.Any(s => s.Name == "PB1" && s.Address == 0xC062), Is.True, "Should have PB1 state");
            Assert.That(states.Any(s => s.Name == "PB2" && s.Address == 0xC063), Is.True, "Should have PB2 state");
        });
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates reflects correct PB0 state.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_PB0_ReflectsOpenAppleState()
    {
        var provider = (ISoftSwitchProvider)device;

        // Initially not pressed
        device.SetModifiers(KeyboardModifiers.None);
        var initialStates = provider.GetSoftSwitchStates();
        var pb0Initial = initialStates.First(s => s.Name == "PB0");
        Assert.That(pb0Initial.Value, Is.False, "PB0 should be false when Open Apple not pressed");

        // Now press Open Apple
        device.SetModifiers(KeyboardModifiers.OpenApple);
        var pressedStates = provider.GetSoftSwitchStates();
        var pb0Pressed = pressedStates.First(s => s.Name == "PB0");
        Assert.That(pb0Pressed.Value, Is.True, "PB0 should be true when Open Apple pressed");
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates reflects correct PB1 state.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_PB1_ReflectsClosedAppleState()
    {
        var provider = (ISoftSwitchProvider)device;

        // Initially not pressed
        device.SetModifiers(KeyboardModifiers.None);
        var initialStates = provider.GetSoftSwitchStates();
        var pb1Initial = initialStates.First(s => s.Name == "PB1");
        Assert.That(pb1Initial.Value, Is.False, "PB1 should be false when Closed Apple not pressed");

        // Now press Closed Apple
        device.SetModifiers(KeyboardModifiers.ClosedApple);
        var pressedStates = provider.GetSoftSwitchStates();
        var pb1Pressed = pressedStates.First(s => s.Name == "PB1");
        Assert.That(pb1Pressed.Value, Is.True, "PB1 should be true when Closed Apple pressed");
    }

    /// <summary>
    /// Verifies that Reset preserves held modifiers so the ROM can still sample them.
    /// </summary>
    [Test]
    public void Reset_PreservesHeldModifiers_PushButtonsStayPressed()
    {
        device.SetModifiers(KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple | KeyboardModifiers.Shift);
        var context = CreateTestContext();

        Assert.That(dispatcher.Read(0x61, in context), Is.EqualTo(0x80), "PB0 should be pressed before reset");

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.Modifiers, Is.EqualTo(KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple | KeyboardModifiers.Shift));
            Assert.That(dispatcher.Read(0x61, in context), Is.EqualTo(0x80), "PB0 should stay pressed during reset sampling");
            Assert.That(dispatcher.Read(0x62, in context), Is.EqualTo(0x80), "PB1 should stay pressed during reset sampling");
            Assert.That(dispatcher.Read(0x63, in context), Is.EqualTo(0x80), "PB2 should stay pressed during reset sampling");
        });
    }

    /// <summary>
    /// Verifies that pressing reset without Control does not assert RESET when required.
    /// </summary>
    [Test]
    public void PressResetKey_WithoutControl_DoesNotAssertWhenControlRequired()
    {
        var mockSignals = new Mock<ISignalBus>();
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Scheduler).Returns(Mock.Of<IScheduler>());
        mockContext.Setup(c => c.Signals).Returns(mockSignals.Object);
        device.Initialize(mockContext.Object);
        device.RequireControlForReset = true;
        device.SetModifiers(KeyboardModifiers.None);

        device.PressResetKey();

        mockSignals.Verify(s => s.Assert(SignalLine.Reset, It.IsAny<int>(), It.IsAny<Cycle>()), Times.Never);
    }

    /// <summary>
    /// Verifies that pressing reset with Control asserts and releasing deasserts RESET.
    /// </summary>
    [Test]
    public void PressResetKey_WithControl_AssertsAndDeassertsReset()
    {
        var mockSignals = new Mock<ISignalBus>();
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Scheduler).Returns(Mock.Of<IScheduler>());
        mockContext.Setup(c => c.Signals).Returns(mockSignals.Object);
        device.Initialize(mockContext.Object);
        device.RequireControlForReset = true;
        device.SetModifiers(KeyboardModifiers.Control);

        device.PressResetKey();
        device.ReleaseResetKey();

        mockSignals.Verify(s => s.Assert(SignalLine.Reset, It.IsAny<int>(), It.IsAny<Cycle>()), Times.Once);
        mockSignals.Verify(s => s.Deassert(SignalLine.Reset, It.IsAny<int>(), It.IsAny<Cycle>()), Times.Once);
    }

    /// <summary>
    /// Verifies that reset can assert without Control when configured.
    /// </summary>
    [Test]
    public void PressResetKey_WithoutControl_AssertsWhenControlNotRequired()
    {
        var mockSignals = new Mock<ISignalBus>();
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Scheduler).Returns(Mock.Of<IScheduler>());
        mockContext.Setup(c => c.Signals).Returns(mockSignals.Object);
        device.Initialize(mockContext.Object);
        device.RequireControlForReset = false;
        device.SetModifiers(KeyboardModifiers.None);

        device.PressResetKey();

        mockSignals.Verify(s => s.Assert(SignalLine.Reset, It.IsAny<int>(), It.IsAny<Cycle>()), Times.Once);
    }

    /// <summary>
    /// Verifies that reset signal assert clears strobe/type-ahead but preserves held input state.
    /// </summary>
    [Test]
    public void OnResetAsserted_ClearsStrobeButPreservesHeldState()
    {
        device.KeyDown(0x41);
        device.SetModifiers(KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple);

        device.OnResetAsserted(0);

        Assert.Multiple(() =>
        {
            Assert.That(device.HasKeyDown, Is.True);
            Assert.That(device.KeyData, Is.EqualTo(0x41), "Strobe should be cleared while key value is preserved");
            Assert.That(device.Modifiers, Is.EqualTo(KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple));
        });
    }

    private static BusAccess CreateTestContext()
    {
        return new(
            Address: 0xC000,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
    }

    private static BusAccess CreateTestContextWithNoSideEffects()
    {
        return new(
            Address: 0xC000,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);
    }
}