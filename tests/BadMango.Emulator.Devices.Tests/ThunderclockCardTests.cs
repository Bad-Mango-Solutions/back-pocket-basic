// <copyright file="ThunderclockCardTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="ThunderclockCard"/> class.
/// </summary>
[TestFixture]
public class ThunderclockCardTests
{
    private ThunderclockCard card = null!;

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
    public void Name_ReturnsThunderclockPlus()
    {
        Assert.That(card.Name, Is.EqualTo("Thunderclock Plus"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsThunderclock()
    {
        Assert.That(card.DeviceType, Is.EqualTo("Thunderclock"));
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
        card.SlotNumber = 4;
        Assert.That(card.SlotNumber, Is.EqualTo(4));
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
    /// Verifies that ExpansionROMRegion is null.
    /// </summary>
    [Test]
    public void ExpansionROMRegion_IsNull()
    {
        Assert.That(card.ExpansionROMRegion, Is.Null);
    }

    /// <summary>
    /// Verifies that UseHostTime defaults to true.
    /// </summary>
    [Test]
    public void UseHostTime_DefaultsToTrue()
    {
        Assert.That(card.UseHostTime, Is.True);
    }

    /// <summary>
    /// Verifies that CurrentTime returns host time when UseHostTime is true.
    /// </summary>
    [Test]
    public void CurrentTime_WhenUseHostTime_ReturnsApproximateHostTime()
    {
        var before = DateTime.Now;
        var cardTime = card.CurrentTime;
        var after = DateTime.Now;

        Assert.That(cardTime, Is.GreaterThanOrEqualTo(before));
        Assert.That(cardTime, Is.LessThanOrEqualTo(after));
    }

    /// <summary>
    /// Verifies that SetFixedTime sets UseHostTime to false.
    /// </summary>
    [Test]
    public void SetFixedTime_SetsUseHostTimeToFalse()
    {
        var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0);
        card.SetFixedTime(fixedTime);

        Assert.That(card.UseHostTime, Is.False);
    }

    /// <summary>
    /// Verifies that CurrentTime returns fixed time when UseHostTime is false.
    /// </summary>
    [Test]
    public void CurrentTime_WhenFixedTime_ReturnsFixedTime()
    {
        var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0);
        card.SetFixedTime(fixedTime);

        Assert.That(card.CurrentTime, Is.EqualTo(fixedTime));
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
    /// Verifies that Reset does not throw.
    /// </summary>
    [Test]
    public void Reset_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => card.Reset());
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
    /// Verifies that ROMRegion reads correct identification bytes.
    /// </summary>
    [Test]
    public void ROMRegion_ReadsCorrectIdentificationBytes()
    {
        var rom = card.ROMRegion!;
        var context = CreateTestContext();

        // Check standard identification bytes
        Assert.That(rom.Read8(0x05, in context), Is.EqualTo(0x38)); // SEC
        Assert.That(rom.Read8(0x07, in context), Is.EqualTo(0x18)); // CLC
        Assert.That(rom.Read8(0x0B, in context), Is.EqualTo(0x01)); // Device type (clock)
        Assert.That(rom.Read8(0x0C, in context), Is.EqualTo(0x20)); // JSR
    }

    /// <summary>
    /// Verifies that ROMRegion ignores writes.
    /// </summary>
    [Test]
    public void ROMRegion_IgnoresWrites()
    {
        var rom = card.ROMRegion!;
        var context = CreateTestContext();

        // Read original value
        byte original = rom.Read8(0x00, in context);

        // Try to write
        rom.Write8(0x00, 0xAA, in context);

        // Verify value unchanged
        Assert.That(rom.Read8(0x00, in context), Is.EqualTo(original));
    }

    /// <summary>
    /// Verifies that card implements IClockDevice.
    /// </summary>
    [Test]
    public void Card_ImplementsIClockDevice()
    {
        Assert.That(card, Is.InstanceOf<IClockDevice>());
    }

    /// <summary>
    /// Verifies that card implements ISlotCard.
    /// </summary>
    [Test]
    public void Card_ImplementsISlotCard()
    {
        Assert.That(card, Is.InstanceOf<ISlotCard>());
    }

    /// <summary>
    /// Helper method to create a test bus access context.
    /// </summary>
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
}