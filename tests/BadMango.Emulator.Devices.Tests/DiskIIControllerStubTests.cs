// <copyright file="DiskIIControllerStubTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="DiskIIControllerStub"/> class.
/// </summary>
[TestFixture]
public class DiskIIControllerStubTests
{
    private DiskIIControllerStub card = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        card = new();
    }

    /// <summary>
    /// Verifies that Name returns the correct value.
    /// </summary>
    [Test]
    public void Name_ReturnsDiskIIControllerStub()
    {
        Assert.That(card.Name, Is.EqualTo("Disk II Controller (Stub)"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsDiskII()
    {
        Assert.That(card.DeviceType, Is.EqualTo("DiskII"));
    }

    /// <summary>
    /// Verifies that Kind returns SlotCard.
    /// </summary>
    [Test]
    public void Kind_ReturnsSlotCard()
    {
        Assert.That(card.Kind, Is.EqualTo(PeripheralKind.SlotCard));
    }

    /// <summary>
    /// Verifies that SlotNumber can be set and retrieved.
    /// </summary>
    [Test]
    public void SlotNumber_CanBeSetAndRetrieved()
    {
        card.SlotNumber = 6;
        Assert.That(card.SlotNumber, Is.EqualTo(6));
    }

    /// <summary>
    /// Verifies that IOHandlers is not null.
    /// </summary>
    [Test]
    public void IOHandlers_IsNotNull()
    {
        Assert.That(card.IOHandlers, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that ROMRegion is not null.
    /// </summary>
    [Test]
    public void ROMRegion_IsNotNull()
    {
        Assert.That(card.ROMRegion, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that ExpansionROMRegion is not null.
    /// </summary>
    [Test]
    public void ExpansionROMRegion_IsNotNull()
    {
        Assert.That(card.ExpansionROMRegion, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that ROMRegion has correct identification bytes.
    /// </summary>
    [Test]
    public void ROMRegion_HasCorrectIdentificationBytes()
    {
        var rom = card.ROMRegion!;
        var context = CreateTestContext();

        Assert.Multiple(() =>
        {
            Assert.That(rom.Read8(0x05, in context), Is.EqualTo(0x38)); // SEC
            Assert.That(rom.Read8(0x07, in context), Is.EqualTo(0x18)); // CLC
            Assert.That(rom.Read8(0x0B, in context), Is.EqualTo(0x00)); // Device type
            Assert.That(rom.Read8(0x0C, in context), Is.EqualTo(0x20)); // JSR
        });
    }

    /// <summary>
    /// Verifies that motor control works.
    /// </summary>
    [Test]
    public void MotorOn_SetsMotorState()
    {
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, card.IOHandlers!);
        var context = CreateTestContext();

        // Read motor on ($C0E9)
        _ = dispatcher.Read(0xE9, in context);

        Assert.That(card.IsMotorOn, Is.True);
    }

    /// <summary>
    /// Verifies that motor off works.
    /// </summary>
    [Test]
    public void MotorOff_ClearsMotorState()
    {
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, card.IOHandlers!);
        var context = CreateTestContext();

        // Turn motor on first
        _ = dispatcher.Read(0xE9, in context);
        Assert.That(card.IsMotorOn, Is.True);

        // Turn motor off ($C0E8)
        _ = dispatcher.Read(0xE8, in context);
        Assert.That(card.IsMotorOn, Is.False);
    }

    /// <summary>
    /// Verifies that drive selection works.
    /// </summary>
    [Test]
    public void DriveSelect_SetsSelectedDrive()
    {
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, card.IOHandlers!);
        var context = CreateTestContext();

        // Select drive 2 ($C0EB)
        _ = dispatcher.Read(0xEB, in context);
        Assert.That(card.SelectedDrive, Is.EqualTo(2));

        // Select drive 1 ($C0EA)
        _ = dispatcher.Read(0xEA, in context);
        Assert.That(card.SelectedDrive, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that phase control updates current phase.
    /// </summary>
    [Test]
    public void PhaseControl_UpdatesCurrentPhase()
    {
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, card.IOHandlers!);
        var context = CreateTestContext();

        // Phase 0 on ($C0E1)
        _ = dispatcher.Read(0xE1, in context);
        Assert.That(card.CurrentPhase, Is.EqualTo(0));

        // Phase 2 on ($C0E5)
        _ = dispatcher.Read(0xE5, in context);
        Assert.That(card.CurrentPhase, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that Q6/Q7 state changes work.
    /// </summary>
    [Test]
    public void Q6Q7_StateChangesWork()
    {
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, card.IOHandlers!);
        var context = CreateTestContext();

        // Q6H ($C0ED)
        _ = dispatcher.Read(0xED, in context);
        Assert.That(card.IsQ6High, Is.True);

        // Q6L ($C0EC)
        _ = dispatcher.Read(0xEC, in context);
        Assert.That(card.IsQ6High, Is.False);

        // Q7H ($C0EF)
        _ = dispatcher.Read(0xEF, in context);
        Assert.That(card.IsQ7High, Is.True);

        // Q7L ($C0EE)
        _ = dispatcher.Read(0xEE, in context);
        Assert.That(card.IsQ7High, Is.False);
    }

    /// <summary>
    /// Verifies that Reset clears all state.
    /// </summary>
    [Test]
    public void Reset_ClearsAllState()
    {
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, card.IOHandlers!);
        var context = CreateTestContext();

        // Set some state
        _ = dispatcher.Read(0xE9, in context); // Motor on
        _ = dispatcher.Read(0xEB, in context); // Drive 2
        _ = dispatcher.Read(0xE5, in context); // Phase 2

        card.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(card.IsMotorOn, Is.False);
            Assert.That(card.SelectedDrive, Is.EqualTo(1));
            Assert.That(card.CurrentPhase, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't change state.
    /// </summary>
    [Test]
    public void Read_WithNoSideEffects_DoesNotChangeState()
    {
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, card.IOHandlers!);
        var context = CreateTestContextWithNoSideEffects();

        // Try to turn motor on with no side effects
        _ = dispatcher.Read(0xE9, in context);

        Assert.That(card.IsMotorOn, Is.False);
    }

    /// <summary>
    /// Verifies that Initialize does not throw.
    /// </summary>
    [Test]
    public void Initialize_DoesNotThrow()
    {
        var mockContext = new Mock<IEventContext>();
        Assert.DoesNotThrow(() => card.Initialize(mockContext.Object));
    }

    /// <summary>
    /// Verifies that OnExpansionROMSelected does not throw.
    /// </summary>
    [Test]
    public void OnExpansionROMSelected_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => card.OnExpansionROMSelected());
    }

    /// <summary>
    /// Verifies that OnExpansionROMDeselected does not throw.
    /// </summary>
    [Test]
    public void OnExpansionROMDeselected_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => card.OnExpansionROMDeselected());
    }

    /// <summary>
    /// Verifies that card implements ISlotCard.
    /// </summary>
    [Test]
    public void Card_ImplementsISlotCard()
    {
        Assert.That(card, Is.InstanceOf<ISlotCard>());
    }

    private static BusAccess CreateTestContext()
    {
        return new(
            Address: 0xC0E0,
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
            Address: 0xC0E0,
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