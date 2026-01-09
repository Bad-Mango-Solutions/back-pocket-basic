// <copyright file="KeyboardDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="KeyboardDevice"/> class.
/// </summary>
[TestFixture]
public class KeyboardDeviceTests
{
    private KeyboardDevice device = null!;
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
    /// Verifies that Name returns the correct value.
    /// </summary>
    [Test]
    public void Name_ReturnsKeyboard()
    {
        Assert.That(device.Name, Is.EqualTo("Keyboard"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsKeyboard()
    {
        Assert.That(device.DeviceType, Is.EqualTo("Keyboard"));
    }

    /// <summary>
    /// Verifies that Kind returns Motherboard.
    /// </summary>
    [Test]
    public void Kind_ReturnsMotherboard()
    {
        Assert.That(device.Kind, Is.EqualTo(PeripheralKind.Motherboard));
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
    /// Verifies that Reset clears all state.
    /// </summary>
    [Test]
    public void Reset_ClearsAllState()
    {
        device.KeyDown(0x41);
        device.SetModifiers(KeyboardModifiers.Shift);

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.HasKeyDown, Is.False);
            Assert.That(device.KeyData, Is.EqualTo(0x00));
            Assert.That(device.Modifiers, Is.EqualTo(KeyboardModifiers.None));
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
    /// Verifies that KeyboardDevice implements ISoftSwitchProvider.
    /// </summary>
    [Test]
    public void KeyboardDevice_ImplementsISoftSwitchProvider()
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
            Assert.That(states, Has.Count.EqualTo(2));
            Assert.That(states.Any(s => s.Name == "KBD" && s.Address == 0xC000), Is.True);
            Assert.That(states.Any(s => s.Name == "KBDSTRB" && s.Address == 0xC010), Is.True);
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