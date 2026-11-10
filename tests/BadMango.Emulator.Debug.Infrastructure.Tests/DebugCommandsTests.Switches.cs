// <copyright file="DebugCommandsTests.Switches.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <content>
/// Unit tests for <see cref="SwitchesCommand"/>, <see cref="DeviceMapCommand"/>,
/// <see cref="FaultCommand"/>, and <see cref="BusLogCommand"/>.
/// </content>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that DeviceMapCommand has correct name.
    /// </summary>
    [Test]
    public void DeviceMapCommand_HasCorrectName()
    {
        var command = new DeviceMapCommand();
        Assert.That(command.Name, Is.EqualTo("devicemap"));
    }

    /// <summary>
    /// Verifies that DeviceMapCommand has correct aliases.
    /// </summary>
    [Test]
    public void DeviceMapCommand_HasCorrectAliases()
    {
        var command = new DeviceMapCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "devices", "devmap" }));
    }

    /// <summary>
    /// Verifies that FaultCommand has correct name.
    /// </summary>
    [Test]
    public void FaultCommand_HasCorrectName()
    {
        var command = new FaultCommand();
        Assert.That(command.Name, Is.EqualTo("fault"));
    }

    /// <summary>
    /// Verifies that FaultCommand displays fault status.
    /// </summary>
    [Test]
    public void FaultCommand_DisplaysFaultStatus()
    {
        var command = new FaultCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Bus Fault Status"));
        });
    }

    /// <summary>
    /// Verifies that SwitchesCommand has correct name.
    /// </summary>
    [Test]
    public void SwitchesCommand_HasCorrectName()
    {
        var command = new SwitchesCommand();
        Assert.That(command.Name, Is.EqualTo("switches"));
    }

    /// <summary>
    /// Verifies that SwitchesCommand has correct aliases.
    /// </summary>
    [Test]
    public void SwitchesCommand_HasCorrectAliases()
    {
        var command = new SwitchesCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "sw", "softswitch" }));
    }

    /// <summary>
    /// Verifies that SwitchesCommand displays soft switch states from providers.
    /// </summary>
    [Test]
    public void SwitchesCommand_DisplaysSwitchStates_WhenMachineHasProviders()
    {
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();
        var mockProvider = new Mock<ISoftSwitchProvider>();

        // Set up the provider to return some test states
        mockProvider.Setup(p => p.ProviderName).Returns("Test Provider");
        mockProvider.Setup(p => p.GetSoftSwitchStates()).Returns(new List<SoftSwitchState>
        {
            new("TEST_SWITCH", 0xC000, true, "Test switch on"),
            new("OTHER_SWITCH", 0xC001, false, "Test switch off"),
        });

        // Set up the machine to return the provider and bus
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        mockMachine.Setup(m => m.GetComponents<ISoftSwitchProvider>())
            .Returns(new List<ISoftSwitchProvider> { mockProvider.Object });

        // Set up debug context with the mock machine
        debugContext.AttachMachine(mockMachine.Object);

        var command = new SwitchesCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("Test Provider"));
            Assert.That(outputWriter.ToString(), Does.Contain("TEST_SWITCH"));
            Assert.That(outputWriter.ToString(), Does.Contain("ON"));
            Assert.That(outputWriter.ToString(), Does.Contain("OTHER_SWITCH"));
            Assert.That(outputWriter.ToString(), Does.Contain("OFF"));
        });
    }

    /// <summary>
    /// Verifies that SwitchesCommand shows no providers message when machine has none.
    /// </summary>
    [Test]
    public void SwitchesCommand_ShowsNoProvidersMessage_WhenMachineHasNoProviders()
    {
        var mockBus = new Mock<IMemoryBus>();
        var mockMachine = new Mock<IMachine>();

        // Set up the machine to return no providers, but with a valid bus
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        mockMachine.Setup(m => m.GetComponents<ISoftSwitchProvider>())
            .Returns(new List<ISoftSwitchProvider>());

        // Set up debug context with the mock machine
        debugContext.AttachMachine(mockMachine.Object);

        var command = new SwitchesCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("No soft switch providers found"));
        });
    }

    /// <summary>
    /// Verifies that SwitchesCommand returns error when no bus attached.
    /// </summary>
    [Test]
    public void SwitchesCommand_ReturnsError_WhenNoBusAttached()
    {
        var contextWithoutBus = new DebugContext(dispatcher, outputWriter, errorWriter);

        var command = new SwitchesCommand();
        var result = command.Execute(contextWithoutBus, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No bus attached"));
        });
    }

    /// <summary>
    /// Verifies that BusLogCommand has correct name.
    /// </summary>
    [Test]
    public void BusLogCommand_HasCorrectName()
    {
        var command = new BusLogCommand();
        Assert.That(command.Name, Is.EqualTo("buslog"));
    }

    /// <summary>
    /// Verifies that BusLogCommand has correct aliases.
    /// </summary>
    [Test]
    public void BusLogCommand_HasCorrectAliases()
    {
        var command = new BusLogCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "bl" }));
    }

    /// <summary>
    /// Verifies that bus fault recording is off until the <c>buslog</c>
    /// command is first used, and that using it enables recording.
    /// </summary>
    [Test]
    public void BusLogCommand_FirstUse_EnablesFaultRecording()
    {
        var ringBus = new MainBus(16, faultRecorder: new BusFaultRing());
        var context = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, ringBus, disassembler);
        Assert.That(ringBus.FaultRecordingEnabled, Is.False, "Recording must be off by default");

        var result = new BusLogCommand().Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ringBus.FaultRecordingEnabled, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("recording enabled"));
        });
    }

    /// <summary>
    /// Verifies that <c>buslog off</c> disables recording and <c>buslog on</c>
    /// re-enables it, and that faults are captured only while enabled.
    /// </summary>
    [Test]
    public void BusLogCommand_OnOff_TogglesFaultRecording()
    {
        var ring = new BusFaultRing();
        var ringBus = new MainBus(16, faultRecorder: ring);
        var context = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, ringBus, disassembler);
        var command = new BusLogCommand();
        var access = new BusAccess(
            Address: 0x5000,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);

        var on = command.Execute(context, ["on"]);
        ringBus.TryRead8(access);
        var off = command.Execute(context, ["off"]);
        bool enabledAfterOff = ringBus.FaultRecordingEnabled;
        ringBus.TryRead8(access);

        Assert.Multiple(() =>
        {
            Assert.That(on.Success, Is.True);
            Assert.That(off.Success, Is.True);
            Assert.That(enabledAfterOff, Is.False);
            Assert.That(ring.Count, Is.EqualTo(1), "Only the fault raised while recording was on is captured");
        });
    }

    /// <summary>
    /// Verifies that the <c>fault</c> command enables bus fault recording.
    /// </summary>
    [Test]
    public void FaultCommand_FirstUse_EnablesFaultRecording()
    {
        var ringBus = new MainBus(16, faultRecorder: new BusFaultRing());
        var context = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, ringBus, disassembler);

        var result = new FaultCommand().Execute(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ringBus.FaultRecordingEnabled, Is.True);
            Assert.That(outputWriter.ToString(), Does.Contain("No fault recorded"));
        });
    }
}