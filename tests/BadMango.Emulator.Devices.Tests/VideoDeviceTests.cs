// <copyright file="VideoDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="VideoDevice"/> class.
/// </summary>
[TestFixture]
public class VideoDeviceTests
{
    private VideoDevice device = null!;
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
    public void Name_ReturnsVideoDevice()
    {
        Assert.That(device.Name, Is.EqualTo("Video Device"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsVideo()
    {
        Assert.That(device.DeviceType, Is.EqualTo("Video"));
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
    /// Verifies that IsTextMode is initially true.
    /// </summary>
    [Test]
    public void IsTextMode_InitiallyTrue()
    {
        Assert.That(device.IsTextMode, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C050 sets graphics mode.
    /// </summary>
    [Test]
    public void ReadC050_SetsGraphicsMode()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x50, in context);

        Assert.That(device.IsTextMode, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C051 sets text mode.
    /// </summary>
    [Test]
    public void ReadC051_SetsTextMode()
    {
        var context = CreateTestContext();

        // First set to graphics mode
        _ = dispatcher.Read(0x50, in context);
        Assert.That(device.IsTextMode, Is.False);

        // Then set back to text mode
        _ = dispatcher.Read(0x51, in context);
        Assert.That(device.IsTextMode, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C052 clears mixed mode.
    /// </summary>
    [Test]
    public void ReadC052_ClearsMixedMode()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x53, in context); // First enable mixed
        Assert.That(device.IsMixedMode, Is.True);

        _ = dispatcher.Read(0x52, in context); // Then disable
        Assert.That(device.IsMixedMode, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C053 sets mixed mode.
    /// </summary>
    [Test]
    public void ReadC053_SetsMixedMode()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x53, in context);

        Assert.That(device.IsMixedMode, Is.True);
    }

    /// <summary>
    /// Verifies that annunciator switches work correctly.
    /// </summary>
    [Test]
    public void Annunciator_SwitchesWorkCorrectly()
    {
        var context = CreateTestContext();

        // $C058 = Annunciator 0 off
        // $C059 = Annunciator 0 on
        _ = dispatcher.Read(0x59, in context); // ANN0 on
        Assert.That(device.Annunciators[0], Is.True);

        _ = dispatcher.Read(0x58, in context); // ANN0 off
        Assert.That(device.Annunciators[0], Is.False);

        // $C05B = Annunciator 1 on
        _ = dispatcher.Read(0x5B, in context);
        Assert.That(device.Annunciators[1], Is.True);
    }

    /// <summary>
    /// Verifies that CurrentMode returns correct mode based on state.
    /// </summary>
    [Test]
    public void CurrentMode_ReturnsCorrectMode()
    {
        Assert.That(device.CurrentMode, Is.EqualTo(VideoMode.Text40));

        device.Set80ColumnMode(true);
        Assert.That(device.CurrentMode, Is.EqualTo(VideoMode.Text80));
    }

    /// <summary>
    /// Verifies that ModeChanged event is raised when mode changes.
    /// </summary>
    [Test]
    public void ModeChanged_RaisedWhenModeChanges()
    {
        VideoMode? changedMode = null;
        device.ModeChanged += mode => changedMode = mode;

        var context = CreateTestContext();
        _ = dispatcher.Read(0x50, in context); // Switch to graphics

        Assert.That(changedMode, Is.EqualTo(VideoMode.LoRes));
    }

    /// <summary>
    /// Verifies that Reset restores default state.
    /// </summary>
    [Test]
    public void Reset_RestoresDefaultState()
    {
        var context = CreateTestContext();
        _ = dispatcher.Read(0x50, in context); // Graphics
        _ = dispatcher.Read(0x53, in context); // Mixed
        _ = dispatcher.Read(0x59, in context); // ANN0 on

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.IsTextMode, Is.True);
            Assert.That(device.IsMixedMode, Is.False);
            Assert.That(device.Annunciators[0], Is.False);
        });
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't change state.
    /// </summary>
    [Test]
    public void Read_WithNoSideEffects_DoesNotChangeState()
    {
        var context = CreateTestContextWithNoSideEffects();

        _ = dispatcher.Read(0x50, in context); // Try to switch to graphics

        Assert.That(device.IsTextMode, Is.True); // Should remain in text mode
    }

    /// <summary>
    /// Verifies that writes also affect video mode.
    /// </summary>
    [Test]
    public void WriteC050_AlsoSetsGraphicsMode()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x50, 0x00, in context);

        Assert.That(device.IsTextMode, Is.False);
    }

    /// <summary>
    /// Verifies that Initialize does not throw.
    /// </summary>
    [Test]
    public void Initialize_DoesNotThrow()
    {
        var mockContext = new Mock<IEventContext>();
        Assert.DoesNotThrow(() => device.Initialize(mockContext.Object));
    }

    private static BusAccess CreateTestContext()
    {
        return new(
            Address: 0xC050,
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
            Address: 0xC050,
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