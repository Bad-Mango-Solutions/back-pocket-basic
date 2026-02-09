// <copyright file="VideoDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;

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
    /// Verifies that reading $C054 clears page 2 (selects page 1).
    /// </summary>
    [Test]
    public void ReadC054_ClearsPage2()
    {
        var context = CreateTestContext();

        // First set page 2
        _ = dispatcher.Read(0x55, in context);
        Assert.That(device.IsPage2, Is.True);

        // Then clear it by reading $C054
        _ = dispatcher.Read(0x54, in context);
        Assert.That(device.IsPage2, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C055 sets page 2.
    /// </summary>
    [Test]
    public void ReadC055_SetsPage2()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x55, in context);

        Assert.That(device.IsPage2, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C056 clears hi-res mode (selects lo-res).
    /// </summary>
    [Test]
    public void ReadC056_ClearsHiRes()
    {
        var context = CreateTestContext();

        // First set hi-res
        _ = dispatcher.Read(0x57, in context);
        Assert.That(device.IsHiRes, Is.True);

        // Then clear it by reading $C056
        _ = dispatcher.Read(0x56, in context);
        Assert.That(device.IsHiRes, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C057 sets hi-res mode.
    /// </summary>
    [Test]
    public void ReadC057_SetsHiRes()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x57, in context);

        Assert.That(device.IsHiRes, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C057 also sets hi-res mode.
    /// </summary>
    [Test]
    public void WriteC057_AlsoSetsHiRes()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x57, 0x00, in context);

        Assert.That(device.IsHiRes, Is.True);
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

    /// <summary>
    /// Verifies that VideoDevice implements ISoftSwitchProvider.
    /// </summary>
    [Test]
    public void VideoDevice_ImplementsISoftSwitchProvider()
    {
        Assert.That(device, Is.InstanceOf<ISoftSwitchProvider>());
    }

    /// <summary>
    /// Verifies that ProviderName returns Video.
    /// </summary>
    [Test]
    public void ProviderName_ReturnsVideo()
    {
        var provider = (ISoftSwitchProvider)device;
        Assert.That(provider.ProviderName, Is.EqualTo("Video"));
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns video mode switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsVideoModeSwitches()
    {
        var provider = (ISoftSwitchProvider)device;

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            // Check for core video mode switches
            Assert.That(states.Any(s => s.Name == "TXTCLR" && s.Address == 0xC050), Is.True);
            Assert.That(states.Any(s => s.Name == "TXTSET" && s.Address == 0xC051), Is.True);
            Assert.That(states.Any(s => s.Name == "MIXCLR" && s.Address == 0xC052), Is.True);
            Assert.That(states.Any(s => s.Name == "MIXSET" && s.Address == 0xC053), Is.True);
            Assert.That(states.Any(s => s.Name == "LOWSCR" && s.Address == 0xC054), Is.True);
            Assert.That(states.Any(s => s.Name == "HISCR" && s.Address == 0xC055), Is.True);
            Assert.That(states.Any(s => s.Name == "LORES" && s.Address == 0xC056), Is.True);
            Assert.That(states.Any(s => s.Name == "HIRES" && s.Address == 0xC057), Is.True);
        });
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns annunciator switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsAnnunciatorSwitches()
    {
        var provider = (ISoftSwitchProvider)device;

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            // Check for annunciator switches (off and on for each)
            Assert.That(states.Any(s => s.Name == "AN0OFF" && s.Address == 0xC058), Is.True);
            Assert.That(states.Any(s => s.Name == "AN0ON" && s.Address == 0xC059), Is.True);
            Assert.That(states.Any(s => s.Name == "AN1OFF" && s.Address == 0xC05A), Is.True);
            Assert.That(states.Any(s => s.Name == "AN1ON" && s.Address == 0xC05B), Is.True);
            Assert.That(states.Any(s => s.Name == "AN2OFF" && s.Address == 0xC05C), Is.True);
            Assert.That(states.Any(s => s.Name == "AN2ON" && s.Address == 0xC05D), Is.True);
            Assert.That(states.Any(s => s.Name == "AN3OFF" && s.Address == 0xC05E), Is.True);
            Assert.That(states.Any(s => s.Name == "AN3ON" && s.Address == 0xC05F), Is.True);
        });
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns status read switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsStatusReadSwitches()
    {
        var provider = (ISoftSwitchProvider)device;

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            Assert.That(states.Any(s => s.Name == "RDVBL" && s.Address == 0xC019), Is.True);
            Assert.That(states.Any(s => s.Name == "RDTEXT" && s.Address == 0xC01A), Is.True);
            Assert.That(states.Any(s => s.Name == "RDMIXED" && s.Address == 0xC01B), Is.True);
            Assert.That(states.Any(s => s.Name == "RDPAGE2" && s.Address == 0xC01C), Is.True);
            Assert.That(states.Any(s => s.Name == "RDHIRES" && s.Address == 0xC01D), Is.True);

            // Note: RDALTCHAR ($C01E) is now handled by CharacterDevice
            Assert.That(states.Any(s => s.Name == "RD80COL" && s.Address == 0xC01F), Is.True);
        });
    }

    /// <summary>
    /// Verifies that reading $C019 returns correct vertical blanking status.
    /// </summary>
    [Test]
    public void ReadC019_ReturnsVerticalBlankingStatus()
    {
        var context = CreateTestContext();

        // Not in VBL - bit 7 should be set
        device.IsVerticalBlanking = false;
        byte notInVbl = dispatcher.Read(0x19, in context);
        Assert.That(notInVbl & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when NOT in VBL");

        // In VBL - bit 7 should be clear (inverted from other status reads)
        device.IsVerticalBlanking = true;
        byte inVbl = dispatcher.Read(0x19, in context);
        Assert.That(inVbl & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when IN VBL");
    }

    /// <summary>
    /// Verifies that reading $C01A returns correct text mode status.
    /// </summary>
    [Test]
    public void ReadC01A_ReturnsTextModeStatus()
    {
        var context = CreateTestContext();

        // Initially in text mode - bit 7 should be set
        byte textModeOn = dispatcher.Read(0x1A, in context);
        Assert.That(textModeOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set in text mode");

        // Switch to graphics mode - bit 7 should be clear
        _ = dispatcher.Read(0x50, in context);
        byte textModeOff = dispatcher.Read(0x1A, in context);
        Assert.That(textModeOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear in graphics mode");
    }

    /// <summary>
    /// Verifies that reading $C01B returns correct mixed mode status.
    /// </summary>
    [Test]
    public void ReadC01B_ReturnsMixedModeStatus()
    {
        var context = CreateTestContext();

        // Initially not in mixed mode - bit 7 should be clear
        byte mixedOff = dispatcher.Read(0x1B, in context);
        Assert.That(mixedOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when mixed mode off");

        // Enable mixed mode - bit 7 should be set
        _ = dispatcher.Read(0x53, in context);
        byte mixedOn = dispatcher.Read(0x1B, in context);
        Assert.That(mixedOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when mixed mode on");
    }

    /// <summary>
    /// Verifies that reading $C01C returns correct page 2 status.
    /// </summary>
    [Test]
    public void ReadC01C_ReturnsPage2Status()
    {
        var context = CreateTestContext();

        // Initially page 1 - bit 7 should be clear
        byte page1 = dispatcher.Read(0x1C, in context);
        Assert.That(page1 & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear for page 1");

        // SetPage2 is now public via IVideoDevice interface
        device.SetPage2(true);

        byte page2 = dispatcher.Read(0x1C, in context);
        Assert.That(page2 & 0x80, Is.EqualTo(0x80), "Bit 7 should be set for page 2");
    }

    /// <summary>
    /// Verifies that reading $C01D returns correct hi-res mode status.
    /// </summary>
    [Test]
    public void ReadC01D_ReturnsHiResModeStatus()
    {
        var context = CreateTestContext();

        // Initially lo-res - bit 7 should be clear
        byte lores = dispatcher.Read(0x1D, in context);
        Assert.That(lores & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear in lo-res mode");

        // SetHiRes is now public via IVideoDevice interface
        device.SetHiRes(true);

        byte hires = dispatcher.Read(0x1D, in context);
        Assert.That(hires & 0x80, Is.EqualTo(0x80), "Bit 7 should be set in hi-res mode");
    }

    // Note: $C01E (RDALTCHAR) is now handled by CharacterDevice

    /// <summary>
    /// Verifies that reading $C01F returns correct 80-column mode status.
    /// </summary>
    [Test]
    public void ReadC01F_Returns80ColumnModeStatus()
    {
        var context = CreateTestContext();

        // Initially 40-column - bit 7 should be clear
        byte col40 = dispatcher.Read(0x1F, in context);
        Assert.That(col40 & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear for 40-column mode");

        // Enable 80-column mode
        device.Set80ColumnMode(true);
        byte col80 = dispatcher.Read(0x1F, in context);
        Assert.That(col80 & 0x80, Is.EqualTo(0x80), "Bit 7 should be set for 80-column mode");
    }

    /// <summary>
    /// Verifies that status reads do not have side effects.
    /// </summary>
    [Test]
    public void StatusReads_HaveNoSideEffects()
    {
        var context = CreateTestContext();

        // Ensure initial state
        bool initialText = device.IsTextMode;
        bool initialMixed = device.IsMixedMode;

        // Read all status registers
        _ = dispatcher.Read(0x19, in context);
        _ = dispatcher.Read(0x1A, in context);
        _ = dispatcher.Read(0x1B, in context);
        _ = dispatcher.Read(0x1C, in context);
        _ = dispatcher.Read(0x1D, in context);
        _ = dispatcher.Read(0x1E, in context);
        _ = dispatcher.Read(0x1F, in context);

        // State should be unchanged
        Assert.Multiple(() =>
        {
            Assert.That(device.IsTextMode, Is.EqualTo(initialText));
            Assert.That(device.IsMixedMode, Is.EqualTo(initialMixed));
        });
    }

    /// <summary>
    /// Verifies that DefaultCharacterRom can load the embedded ROM data.
    /// </summary>
    [Test]
    public void DefaultCharacterRom_GetRomData_Returns4KBData()
    {
        var romData = DefaultCharacterRom.GetRomData();

        Assert.That(romData.Length, Is.EqualTo(4096));
    }

    /// <summary>
    /// Verifies that DefaultCharacterRom contains non-zero character data.
    /// </summary>
    [Test]
    public void DefaultCharacterRom_ContainsNonZeroData()
    {
        var romData = DefaultCharacterRom.GetRomData();

        // Check that at least some character data is non-zero
        // Character 'A' (0x41) should have visible pixels
        int offsetA = 0x41 * 8;
        bool hasNonZero = false;
        for (int i = 0; i < 8; i++)
        {
            if (romData[offsetA + i] != 0)
            {
                hasNonZero = true;
                break;
            }
        }

        Assert.That(hasNonZero, Is.True, "Character 'A' should have non-zero pixel data");
    }

    /// <summary>
    /// Verifies that Initialize schedules a VBlank event on the scheduler.
    /// </summary>
    [Test]
    public void Initialize_SchedulesVBlankEvent()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        Assert.That(scheduler.PendingEventCount, Is.GreaterThan(0), "VBlank event should be scheduled after initialization");
    }

    /// <summary>
    /// Verifies that VBlank event fires at the correct cycle (CyclesPerFrame - VBlankDurationCycles).
    /// </summary>
    [Test]
    public void VBlank_FiresAtCorrectCycle()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        bool vblankFired = false;
        device.VBlankOccurred += () => vblankFired = true;

        // Advance to just before VBlank should fire
        ulong vblankStartCycle = VideoDevice.CyclesPerFrame - VideoDevice.VBlankDurationCycles;
        scheduler.Advance(new Core.Cycle(vblankStartCycle - 1));
        Assert.That(vblankFired, Is.False, "VBlank should not fire before its scheduled cycle");

        // Advance one more cycle to trigger VBlank
        scheduler.Advance(new Core.Cycle(1));
        Assert.That(vblankFired, Is.True, "VBlank should fire at the scheduled cycle");
    }

    /// <summary>
    /// Verifies that VBlankOccurred event is raised when VBlank fires.
    /// </summary>
    [Test]
    public void VBlank_RaisesVBlankOccurredEvent()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        int vblankCount = 0;
        device.VBlankOccurred += () => vblankCount++;

        // Advance past VBlank start
        ulong vblankStartCycle = VideoDevice.CyclesPerFrame - VideoDevice.VBlankDurationCycles;
        scheduler.Advance(new Core.Cycle(vblankStartCycle));

        Assert.That(vblankCount, Is.EqualTo(1), "VBlankOccurred should fire exactly once");
    }

    /// <summary>
    /// Verifies that IsVerticalBlanking is set to true when VBlank starts.
    /// </summary>
    [Test]
    public void VBlank_SetsVerticalBlankingTrue()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        Assert.That(device.IsVerticalBlanking, Is.False, "Should not be in VBL initially");

        // Advance to VBlank start
        ulong vblankStartCycle = VideoDevice.CyclesPerFrame - VideoDevice.VBlankDurationCycles;
        scheduler.Advance(new Core.Cycle(vblankStartCycle));

        Assert.That(device.IsVerticalBlanking, Is.True, "Should be in VBL after VBlank start fires");
    }

    /// <summary>
    /// Verifies that IsVerticalBlanking is cleared when VBlank ends.
    /// </summary>
    [Test]
    public void VBlank_ClearsVerticalBlankingWhenEnds()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        // Advance to VBlank start
        ulong vblankStartCycle = VideoDevice.CyclesPerFrame - VideoDevice.VBlankDurationCycles;
        scheduler.Advance(new Core.Cycle(vblankStartCycle));
        Assert.That(device.IsVerticalBlanking, Is.True);

        // Advance through VBlank duration
        scheduler.Advance(new Core.Cycle(VideoDevice.VBlankDurationCycles));
        Assert.That(device.IsVerticalBlanking, Is.False, "VBL should be cleared after VBlank duration");
    }

    /// <summary>
    /// Verifies that VBlank reschedules itself for continuous operation.
    /// </summary>
    [Test]
    public void VBlank_ReschedulesAfterCompletion()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        int vblankCount = 0;
        device.VBlankOccurred += () => vblankCount++;

        // Advance through a complete frame (VBlank start + VBlank end + next VBlank start)
        scheduler.Advance(new Core.Cycle(VideoDevice.CyclesPerFrame));

        // First VBlank should have fired
        Assert.That(vblankCount, Is.EqualTo(1), "First VBlank should have fired");

        // Advance through another complete frame
        scheduler.Advance(new Core.Cycle(VideoDevice.CyclesPerFrame));

        Assert.That(vblankCount, Is.EqualTo(2), "Second VBlank should have fired after rescheduling");
    }

    /// <summary>
    /// Verifies that multiple VBlank cycles maintain correct timing.
    /// </summary>
    [Test]
    public void VBlank_MaintainsCorrectTimingOverMultipleFrames()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        int vblankCount = 0;
        device.VBlankOccurred += () => vblankCount++;

        // Run through 5 complete frames
        const int frameCount = 5;
        scheduler.Advance(new Core.Cycle(VideoDevice.CyclesPerFrame * frameCount));

        Assert.That(vblankCount, Is.EqualTo(frameCount), $"Should have {frameCount} VBlank events over {frameCount} frames");
    }

    /// <summary>
    /// Verifies that RDVBL status register correctly reflects scheduler-driven VBlank state.
    /// </summary>
    [Test]
    public void ReadC019_ReflectsSchedulerDrivenVBlankState()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        var busContext = CreateTestContext();

        // Initially not in VBL (bit 7 = 1)
        byte beforeVbl = dispatcher.Read(0x19, in busContext);
        Assert.That(beforeVbl & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when NOT in VBL");

        // Advance to VBlank start
        ulong vblankStartCycle = VideoDevice.CyclesPerFrame - VideoDevice.VBlankDurationCycles;
        scheduler.Advance(new Core.Cycle(vblankStartCycle));

        // Now in VBL (bit 7 = 0)
        byte duringVbl = dispatcher.Read(0x19, in busContext);
        Assert.That(duringVbl & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear during VBL");

        // Advance past VBlank end
        scheduler.Advance(new Core.Cycle(VideoDevice.VBlankDurationCycles));

        // No longer in VBL (bit 7 = 1)
        byte afterVbl = dispatcher.Read(0x19, in busContext);
        Assert.That(afterVbl & 0x80, Is.EqualTo(0x80), "Bit 7 should be set after VBL ends");
    }

    /// <summary>
    /// Verifies that Reset cancels pending VBlank events and reschedules.
    /// </summary>
    [Test]
    public void Reset_ReschedulesVBlankEvents()
    {
        var (scheduler, eventContext) = CreateSchedulerContext();
        device.Initialize(eventContext);

        int vblankCount = 0;
        device.VBlankOccurred += () => vblankCount++;

        // Advance partway through a frame
        scheduler.Advance(new Core.Cycle(5000));

        // Reset the device
        device.Reset();

        // VBlank should still be schedulable after reset
        Assert.That(scheduler.PendingEventCount, Is.GreaterThan(0), "VBlank should be rescheduled after reset");
    }

    /// <summary>
    /// Verifies that the scheduler observer reports VBlank event scheduling.
    /// </summary>
    [Test]
    public void VBlank_IsObservableViaSchedulerEvents()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<Core.Interfaces.Signaling.ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();
        var eventContext = new EventContext(scheduler, mockSignals.Object, mockBus.Object);
        scheduler.SetEventContext(eventContext);

        var scheduledEvents = new List<ScheduledEventKind>();
        scheduler.EventScheduled += (handle, cycle, kind, priority, tag) => scheduledEvents.Add(kind);

        device.Initialize(eventContext);

        Assert.That(scheduledEvents, Has.Some.EqualTo(ScheduledEventKind.VideoBlank), "Scheduler should observe VideoBlank event scheduling");
    }

    /// <summary>
    /// Verifies that the scheduler observer reports VBlank event consumption.
    /// </summary>
    [Test]
    public void VBlank_ConsumptionIsObservableViaSchedulerEvents()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<Core.Interfaces.Signaling.ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();
        var eventContext = new EventContext(scheduler, mockSignals.Object, mockBus.Object);
        scheduler.SetEventContext(eventContext);

        var consumedEvents = new List<ScheduledEventKind>();
        scheduler.EventConsumed += (handle, cycle, kind) => consumedEvents.Add(kind);

        device.Initialize(eventContext);

        // Advance to trigger VBlank
        ulong vblankStartCycle = VideoDevice.CyclesPerFrame - VideoDevice.VBlankDurationCycles;
        scheduler.Advance(new Core.Cycle(vblankStartCycle));

        Assert.That(consumedEvents, Has.Some.EqualTo(ScheduledEventKind.VideoBlank), "Scheduler should observe VideoBlank event consumption");
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

    private static (Scheduler Scheduler, IEventContext Context) CreateSchedulerContext()
    {
        var scheduler = new Scheduler();
        var mockSignals = new Mock<Core.Interfaces.Signaling.ISignalBus>();
        var mockBus = new Mock<IMemoryBus>();
        var eventContext = new EventContext(scheduler, mockSignals.Object, mockBus.Object);
        scheduler.SetEventContext(eventContext);
        return (scheduler, eventContext);
    }
}