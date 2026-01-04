// <copyright file="KeyboardControllerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="KeyboardController"/> class.
/// </summary>
[TestFixture]
public class KeyboardControllerTests
{
    private KeyboardController controller = null!;
    private IOPageDispatcher dispatcher = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        controller = new KeyboardController();
        dispatcher = new IOPageDispatcher();
        controller.RegisterHandlers(dispatcher);
    }

    /// <summary>
    /// Verifies that Name returns the correct value.
    /// </summary>
    [Test]
    public void Name_ReturnsKeyboardController()
    {
        Assert.That(controller.Name, Is.EqualTo("Keyboard Controller"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsKeyboard()
    {
        Assert.That(controller.DeviceType, Is.EqualTo("Keyboard"));
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
    /// Verifies that KeyDown sets the key data and strobe.
    /// </summary>
    [Test]
    public void KeyDown_SetsKeyDataAndStrobe()
    {
        controller.KeyDown(0x41); // 'A'

        Assert.Multiple(() =>
        {
            Assert.That(controller.HasKeyDown, Is.True);
            Assert.That(controller.KeyData, Is.EqualTo(0xC1)); // 0x41 | 0x80 (strobe)
        });
    }

    /// <summary>
    /// Verifies that KeyUp clears the key down state but preserves key data.
    /// </summary>
    [Test]
    public void KeyUp_ClearsKeyDownButPreservesKeyData()
    {
        controller.KeyDown(0x41);
        controller.KeyUp();

        Assert.Multiple(() =>
        {
            Assert.That(controller.HasKeyDown, Is.False);
            Assert.That(controller.KeyData, Is.EqualTo(0xC1)); // Key data still has strobe
        });
    }

    /// <summary>
    /// Verifies that reading $C000 returns the key data.
    /// </summary>
    [Test]
    public void ReadC000_ReturnsKeyData()
    {
        controller.KeyDown(0x42); // 'B'
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
        controller.KeyDown(0x41);
        var context = CreateTestContext();

        // Read $C010 to clear strobe
        _ = dispatcher.Read(0x10, in context);

        Assert.That(controller.KeyData, Is.EqualTo(0x41)); // Strobe cleared
    }

    /// <summary>
    /// Verifies that reading $C010 returns key down status.
    /// </summary>
    [Test]
    public void ReadC010_ReturnsKeyDownStatus()
    {
        controller.KeyDown(0x41);
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
        controller.KeyDown(0x41);
        var context = CreateTestContext();

        dispatcher.Write(0x10, 0x00, in context);

        Assert.That(controller.KeyData, Is.EqualTo(0x41)); // Strobe cleared
    }

    /// <summary>
    /// Verifies that Reset clears all state.
    /// </summary>
    [Test]
    public void Reset_ClearsAllState()
    {
        controller.KeyDown(0x41);
        controller.SetModifiers(KeyboardModifiers.Shift);

        controller.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(controller.HasKeyDown, Is.False);
            Assert.That(controller.KeyData, Is.EqualTo(0x00));
            Assert.That(controller.Modifiers, Is.EqualTo(KeyboardModifiers.None));
        });
    }

    /// <summary>
    /// Verifies that SetModifiers sets the modifier state.
    /// </summary>
    [Test]
    public void SetModifiers_SetsModifierState()
    {
        controller.SetModifiers(KeyboardModifiers.Control | KeyboardModifiers.OpenApple);

        Assert.That(controller.Modifiers, Is.EqualTo(KeyboardModifiers.Control | KeyboardModifiers.OpenApple));
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't clear strobe.
    /// </summary>
    [Test]
    public void ReadC010_WithNoSideEffects_DoesNotClearStrobe()
    {
        controller.KeyDown(0x41);
        var context = CreateTestContextWithNoSideEffects();

        _ = dispatcher.Read(0x10, in context);

        Assert.That(controller.KeyData, Is.EqualTo(0xC1)); // Strobe still set
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

    private static BusAccess CreateTestContext()
    {
        return new BusAccess(
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
        return new BusAccess(
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