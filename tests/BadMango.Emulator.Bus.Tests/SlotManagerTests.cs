// <copyright file="SlotManagerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="SlotManager"/> class.
/// </summary>
[TestFixture]
public class SlotManagerTests
{
    private IOPageDispatcher dispatcher = null!;
    private SlotManager slotManager = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        dispatcher = new();
        slotManager = new(dispatcher);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException for null dispatcher.
    /// </summary>
    [Test]
    public void Constructor_NullDispatcher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SlotManager(null!));
    }

    /// <summary>
    /// Verifies that Slots is empty initially.
    /// </summary>
    [Test]
    public void Slots_Initially_IsEmpty()
    {
        Assert.That(slotManager.Slots, Is.Empty);
    }

    /// <summary>
    /// Verifies that ActiveExpansionSlot is null initially.
    /// </summary>
    [Test]
    public void ActiveExpansionSlot_Initially_IsNull()
    {
        Assert.That(slotManager.ActiveExpansionSlot, Is.Null);
    }

    /// <summary>
    /// Verifies that Install stores card in Slots dictionary.
    /// </summary>
    [Test]
    public void Install_ValidSlotAndCard_StoresCardInSlots()
    {
        var card = CreateMockPeripheral();

        slotManager.Install(1, card.Object);

        Assert.That(slotManager.Slots, Contains.Key(1));
        Assert.That(slotManager.Slots[1], Is.SameAs(card.Object));
    }

    /// <summary>
    /// Verifies that Install sets card's SlotNumber property.
    /// </summary>
    [Test]
    public void Install_ValidSlotAndCard_SetsCardSlotNumber()
    {
        var card = CreateMockPeripheral();

        slotManager.Install(3, card.Object);

        card.VerifySet(c => c.SlotNumber = 3, Times.Once);
    }

    /// <summary>
    /// Verifies that Install registers I/O handlers with dispatcher.
    /// </summary>
    [Test]
    public void Install_CardWithIOHandlers_RegistersWithDispatcher()
    {
        var handlers = new SlotIOHandlers();
        handlers.Set(0x00, (offset, in ctx) => 0x42, null);

        var card = CreateMockPeripheral();
        card.Setup(c => c.IOHandlers).Returns(handlers);

        slotManager.Install(6, card.Object);

        // Verify by reading from the dispatcher at slot 6's offset (0xE0)
        var context = CreateTestContext();
        byte result = dispatcher.Read(0xE0, in context);
        Assert.That(result, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that Install throws for slot 0.
    /// </summary>
    [Test]
    public void Install_Slot0_ThrowsArgumentOutOfRangeException()
    {
        var card = CreateMockPeripheral();

        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.Install(0, card.Object));
    }

    /// <summary>
    /// Verifies that Install throws for slot 8.
    /// </summary>
    [Test]
    public void Install_Slot8_ThrowsArgumentOutOfRangeException()
    {
        var card = CreateMockPeripheral();

        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.Install(8, card.Object));
    }

    /// <summary>
    /// Verifies that Install throws for negative slot.
    /// </summary>
    [Test]
    public void Install_NegativeSlot_ThrowsArgumentOutOfRangeException()
    {
        var card = CreateMockPeripheral();

        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.Install(-1, card.Object));
    }

    /// <summary>
    /// Verifies that Install throws for null card.
    /// </summary>
    [Test]
    public void Install_NullCard_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => slotManager.Install(1, null!));
    }

    /// <summary>
    /// Verifies that Install throws if slot is already occupied.
    /// </summary>
    [Test]
    public void Install_SlotOccupied_ThrowsInvalidOperationException()
    {
        var card1 = CreateMockPeripheral();
        var card2 = CreateMockPeripheral();

        slotManager.Install(1, card1.Object);

        Assert.Throws<InvalidOperationException>(() => slotManager.Install(1, card2.Object));
    }

    /// <summary>
    /// Verifies that Install works for all valid slots 1-7.
    /// </summary>
    [Test]
    public void Install_AllValidSlots_Succeeds()
    {
        for (int slot = 1; slot <= 7; slot++)
        {
            var card = CreateMockPeripheral();
            Assert.DoesNotThrow(() => slotManager.Install(slot, card.Object));
        }

        Assert.That(slotManager.Slots, Has.Count.EqualTo(7));
    }

    /// <summary>
    /// Verifies that Remove clears card from Slots dictionary.
    /// </summary>
    [Test]
    public void Remove_InstalledCard_RemovesFromSlots()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(1, card.Object);

        slotManager.Remove(1);

        Assert.That(slotManager.Slots, Does.Not.ContainKey(1));
    }

    /// <summary>
    /// Verifies that Remove clears I/O handlers from dispatcher.
    /// </summary>
    [Test]
    public void Remove_InstalledCard_ClearsDispatcherHandlers()
    {
        var handlers = new SlotIOHandlers();
        handlers.Set(0x00, (offset, in ctx) => 0x42, null);

        var card = CreateMockPeripheral();
        card.Setup(c => c.IOHandlers).Returns(handlers);

        slotManager.Install(6, card.Object);
        slotManager.Remove(6);

        // Verify by reading from the dispatcher at slot 6's offset (0xE0)
        var context = CreateTestContext();
        byte result = dispatcher.Read(0xE0, in context);
        Assert.That(result, Is.EqualTo(0xFF)); // Floating bus value
    }

    /// <summary>
    /// Verifies that Remove on empty slot is a no-op.
    /// </summary>
    [Test]
    public void Remove_EmptySlot_NoOp()
    {
        Assert.DoesNotThrow(() => slotManager.Remove(1));
    }

    /// <summary>
    /// Verifies that Remove throws for invalid slot.
    /// </summary>
    [Test]
    public void Remove_InvalidSlot_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.Remove(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.Remove(8));
    }

    /// <summary>
    /// Verifies that Remove deselects expansion ROM if slot was active.
    /// </summary>
    [Test]
    public void Remove_ActiveExpansionSlot_DeselectsAndNotifiesCard()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(3, card.Object);
        slotManager.SelectExpansionSlot(3);

        slotManager.Remove(3);

        card.Verify(c => c.OnExpansionROMDeselected(), Times.Once);
        Assert.That(slotManager.ActiveExpansionSlot, Is.Null);
    }

    /// <summary>
    /// Verifies that GetCard returns installed card.
    /// </summary>
    [Test]
    public void GetCard_InstalledSlot_ReturnsCard()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(1, card.Object);

        var result = slotManager.GetCard(1);

        Assert.That(result, Is.SameAs(card.Object));
    }

    /// <summary>
    /// Verifies that GetCard returns null for empty slot.
    /// </summary>
    [Test]
    public void GetCard_EmptySlot_ReturnsNull()
    {
        var result = slotManager.GetCard(1);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that GetCard throws for invalid slot.
    /// </summary>
    [Test]
    public void GetCard_InvalidSlot_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.GetCard(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.GetCard(8));
    }

    /// <summary>
    /// Verifies that GetSlotRomRegion returns card's ROM region.
    /// </summary>
    [Test]
    public void GetSlotRomRegion_InstalledCard_ReturnsROMRegion()
    {
        var romRegion = new Mock<IBusTarget>();
        var card = CreateMockPeripheral();
        card.Setup(c => c.ROMRegion).Returns(romRegion.Object);
        slotManager.Install(1, card.Object);

        var result = slotManager.GetSlotRomRegion(1);

        Assert.That(result, Is.SameAs(romRegion.Object));
    }

    /// <summary>
    /// Verifies that GetSlotRomRegion returns null for empty slot.
    /// </summary>
    [Test]
    public void GetSlotRomRegion_EmptySlot_ReturnsNull()
    {
        var result = slotManager.GetSlotRomRegion(1);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that GetExpansionRomRegion returns card's expansion ROM region.
    /// </summary>
    [Test]
    public void GetExpansionRomRegion_InstalledCard_ReturnsExpansionROMRegion()
    {
        var expansionRom = new Mock<IBusTarget>();
        var card = CreateMockPeripheral();
        card.Setup(c => c.ExpansionROMRegion).Returns(expansionRom.Object);
        slotManager.Install(1, card.Object);

        var result = slotManager.GetExpansionRomRegion(1);

        Assert.That(result, Is.SameAs(expansionRom.Object));
    }

    /// <summary>
    /// Verifies that GetExpansionRomRegion returns null for empty slot.
    /// </summary>
    [Test]
    public void GetExpansionRomRegion_EmptySlot_ReturnsNull()
    {
        var result = slotManager.GetExpansionRomRegion(1);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that SelectExpansionSlot sets ActiveExpansionSlot.
    /// </summary>
    [Test]
    public void SelectExpansionSlot_ValidSlot_SetsActiveExpansionSlot()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(3, card.Object);

        slotManager.SelectExpansionSlot(3);

        Assert.That(slotManager.ActiveExpansionSlot, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that SelectExpansionSlot notifies card of selection.
    /// </summary>
    [Test]
    public void SelectExpansionSlot_ValidSlot_NotifiesCardOfSelection()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(3, card.Object);

        slotManager.SelectExpansionSlot(3);

        card.Verify(c => c.OnExpansionROMSelected(), Times.Once);
    }

    /// <summary>
    /// Verifies that SelectExpansionSlot deselects previous slot.
    /// </summary>
    [Test]
    public void SelectExpansionSlot_PreviousSlotActive_NotifiesPreviousCardOfDeselection()
    {
        var card1 = CreateMockPeripheral();
        var card2 = CreateMockPeripheral();
        slotManager.Install(1, card1.Object);
        slotManager.Install(2, card2.Object);

        slotManager.SelectExpansionSlot(1);
        slotManager.SelectExpansionSlot(2);

        card1.Verify(c => c.OnExpansionROMDeselected(), Times.Once);
        card2.Verify(c => c.OnExpansionROMSelected(), Times.Once);
    }

    /// <summary>
    /// Verifies that selecting the same slot again does not deselect it.
    /// </summary>
    [Test]
    public void SelectExpansionSlot_SameSlot_DoesNotDeselect()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(3, card.Object);

        slotManager.SelectExpansionSlot(3);
        slotManager.SelectExpansionSlot(3);

        // Should only notify once for each selection, not trigger deselection
        card.Verify(c => c.OnExpansionROMDeselected(), Times.Never);
        card.Verify(c => c.OnExpansionROMSelected(), Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that SelectExpansionSlot works for empty slot (sets active but no notification).
    /// </summary>
    [Test]
    public void SelectExpansionSlot_EmptySlot_SetsActiveButNoNotification()
    {
        slotManager.SelectExpansionSlot(3);

        Assert.That(slotManager.ActiveExpansionSlot, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that SelectExpansionSlot throws for invalid slot.
    /// </summary>
    [Test]
    public void SelectExpansionSlot_InvalidSlot_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.SelectExpansionSlot(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => slotManager.SelectExpansionSlot(8));
    }

    /// <summary>
    /// Verifies that DeselectExpansionSlot sets ActiveExpansionSlot to null.
    /// </summary>
    [Test]
    public void DeselectExpansionSlot_ActiveSlot_SetsActiveToNull()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(3, card.Object);
        slotManager.SelectExpansionSlot(3);

        slotManager.DeselectExpansionSlot();

        Assert.That(slotManager.ActiveExpansionSlot, Is.Null);
    }

    /// <summary>
    /// Verifies that DeselectExpansionSlot notifies card of deselection.
    /// </summary>
    [Test]
    public void DeselectExpansionSlot_ActiveSlot_NotifiesCardOfDeselection()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(3, card.Object);
        slotManager.SelectExpansionSlot(3);

        slotManager.DeselectExpansionSlot();

        card.Verify(c => c.OnExpansionROMDeselected(), Times.Once);
    }

    /// <summary>
    /// Verifies that DeselectExpansionSlot is no-op when nothing selected.
    /// </summary>
    [Test]
    public void DeselectExpansionSlot_NoActiveSlot_NoOp()
    {
        Assert.DoesNotThrow(() => slotManager.DeselectExpansionSlot());
        Assert.That(slotManager.ActiveExpansionSlot, Is.Null);
    }

    /// <summary>
    /// Verifies that HandleSlotROMAccess selects correct slot from address.
    /// </summary>
    /// <param name="address">The address to access.</param>
    /// <param name="expectedSlot">The expected slot to be selected.</param>
    [TestCase(0xC100U, 1)]
    [TestCase(0xC1FFU, 1)]
    [TestCase(0xC200U, 2)]
    [TestCase(0xC300U, 3)]
    [TestCase(0xC400U, 4)]
    [TestCase(0xC500U, 5)]
    [TestCase(0xC600U, 6)]
    [TestCase(0xC6FFU, 6)]
    [TestCase(0xC700U, 7)]
    [TestCase(0xC7FFU, 7)]
    public void HandleSlotROMAccess_ValidAddress_SelectsCorrectSlot(uint address, int expectedSlot)
    {
        slotManager.HandleSlotROMAccess(address);

        Assert.That(slotManager.ActiveExpansionSlot, Is.EqualTo(expectedSlot));
    }

    /// <summary>
    /// Verifies that HandleSlotROMAccess ignores slot 0 address.
    /// </summary>
    [Test]
    public void HandleSlotROMAccess_Slot0Address_DoesNothing()
    {
        slotManager.HandleSlotROMAccess(0xC000);

        Assert.That(slotManager.ActiveExpansionSlot, Is.Null);
    }

    /// <summary>
    /// Verifies that Reset deselects active expansion ROM.
    /// </summary>
    [Test]
    public void Reset_ActiveExpansionSlot_Deselects()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(3, card.Object);
        slotManager.SelectExpansionSlot(3);

        slotManager.Reset();

        Assert.That(slotManager.ActiveExpansionSlot, Is.Null);
    }

    /// <summary>
    /// Verifies that Reset calls Reset on all installed cards.
    /// </summary>
    [Test]
    public void Reset_InstalledCards_CallsResetOnAllCards()
    {
        var card1 = CreateMockPeripheral();
        var card2 = CreateMockPeripheral();
        var card3 = CreateMockPeripheral();

        slotManager.Install(1, card1.Object);
        slotManager.Install(3, card2.Object);
        slotManager.Install(7, card3.Object);

        slotManager.Reset();

        card1.Verify(c => c.Reset(), Times.Once);
        card2.Verify(c => c.Reset(), Times.Once);
        card3.Verify(c => c.Reset(), Times.Once);
    }

    /// <summary>
    /// Verifies that Reset works when no cards installed.
    /// </summary>
    [Test]
    public void Reset_NoCardsInstalled_NoOp()
    {
        Assert.DoesNotThrow(() => slotManager.Reset());
    }

    /// <summary>
    /// Verifies that Slots returns a read-only dictionary that reflects installed cards.
    /// </summary>
    [Test]
    public void Slots_ReflectsInstalledCards()
    {
        var card = CreateMockPeripheral();
        slotManager.Install(1, card.Object);

        var slots = slotManager.Slots;

        Assert.That(slots, Has.Count.EqualTo(1));
        Assert.That(slots.ContainsKey(1), Is.True);
    }

    /// <summary>
    /// Verifies that RegisterInternalExpansionRom stores the expansion ROM.
    /// </summary>
    [Test]
    public void RegisterInternalExpansionRom_ValidSlot_StoresExpansionRom()
    {
        var expRom = new Mock<IBusTarget>();

        slotManager.RegisterInternalExpansionRom(3, expRom.Object);

        // GetExpansionRomRegion should return the internal ROM
        var result = slotManager.GetExpansionRomRegion(3);
        Assert.That(result, Is.SameAs(expRom.Object));
    }

    /// <summary>
    /// Verifies that RegisterInternalExpansionRom can clear the expansion ROM with null.
    /// </summary>
    [Test]
    public void RegisterInternalExpansionRom_Null_ClearsExpansionRom()
    {
        var expRom = new Mock<IBusTarget>();

        slotManager.RegisterInternalExpansionRom(3, expRom.Object);
        slotManager.RegisterInternalExpansionRom(3, null);

        // GetExpansionRomRegion should return null (no card installed)
        var result = slotManager.GetExpansionRomRegion(3);
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that RegisterInternalExpansionRom throws for invalid slot.
    /// </summary>
    [Test]
    public void RegisterInternalExpansionRom_InvalidSlot_ThrowsArgumentOutOfRangeException()
    {
        var expRom = new Mock<IBusTarget>();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            slotManager.RegisterInternalExpansionRom(0, expRom.Object));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            slotManager.RegisterInternalExpansionRom(8, expRom.Object));
    }

    /// <summary>
    /// Verifies that internal expansion ROM takes precedence over physical card.
    /// </summary>
    [Test]
    public void GetExpansionRomRegion_InternalRomTakesPrecedenceOverCard()
    {
        var cardExpRom = new Mock<IBusTarget>();
        var internalExpRom = new Mock<IBusTarget>();

        var card = CreateMockPeripheral();
        card.Setup(c => c.ExpansionROMRegion).Returns(cardExpRom.Object);

        slotManager.Install(3, card.Object);
        slotManager.RegisterInternalExpansionRom(3, internalExpRom.Object);

        var result = slotManager.GetExpansionRomRegion(3);
        Assert.That(result, Is.SameAs(internalExpRom.Object), "Internal ROM should take precedence");
    }

    /// <summary>
    /// Verifies that clearing internal expansion ROM restores physical card's ROM.
    /// </summary>
    [Test]
    public void GetExpansionRomRegion_ClearingInternalRomRestoresCardRom()
    {
        var cardExpRom = new Mock<IBusTarget>();
        var internalExpRom = new Mock<IBusTarget>();

        var card = CreateMockPeripheral();
        card.Setup(c => c.ExpansionROMRegion).Returns(cardExpRom.Object);

        slotManager.Install(3, card.Object);
        slotManager.RegisterInternalExpansionRom(3, internalExpRom.Object);
        slotManager.RegisterInternalExpansionRom(3, null); // Clear internal

        var result = slotManager.GetExpansionRomRegion(3);
        Assert.That(result, Is.SameAs(cardExpRom.Object), "Should fall back to card ROM");
    }

    /// <summary>
    /// Verifies that SetDefaultExpansionRom stores the default ROM.
    /// </summary>
    [Test]
    public void SetDefaultExpansionRom_ValidRom_StoresDefaultRom()
    {
        var defaultRom = new Mock<IBusTarget>();

        slotManager.SetDefaultExpansionRom(defaultRom.Object);

        // GetVisibleExpansionRom should return the default ROM when no slot is selected
        var result = slotManager.GetVisibleExpansionRom();
        Assert.That(result, Is.SameAs(defaultRom.Object));
    }

    /// <summary>
    /// Verifies that SetDefaultExpansionRom can clear the default ROM with null.
    /// </summary>
    [Test]
    public void SetDefaultExpansionRom_Null_ClearsDefaultRom()
    {
        var defaultRom = new Mock<IBusTarget>();

        slotManager.SetDefaultExpansionRom(defaultRom.Object);
        slotManager.SetDefaultExpansionRom(null);

        // GetVisibleExpansionRom should return null (floating bus)
        var result = slotManager.GetVisibleExpansionRom();
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that GetVisibleExpansionRom returns selected slot's ROM when a slot is selected.
    /// </summary>
    [Test]
    public void GetVisibleExpansionRom_SlotSelected_ReturnsSlotRom()
    {
        var defaultRom = new Mock<IBusTarget>();
        var slotRom = new Mock<IBusTarget>();

        var card = CreateMockPeripheral();
        card.Setup(c => c.ExpansionROMRegion).Returns(slotRom.Object);

        slotManager.SetDefaultExpansionRom(defaultRom.Object);
        slotManager.Install(6, card.Object);
        slotManager.SelectExpansionSlot(6);

        var result = slotManager.GetVisibleExpansionRom();
        Assert.That(result, Is.SameAs(slotRom.Object), "Should return slot's ROM, not default");
    }

    /// <summary>
    /// Verifies that GetVisibleExpansionRom returns default ROM after deselection.
    /// </summary>
    [Test]
    public void GetVisibleExpansionRom_AfterDeselect_ReturnsDefaultRom()
    {
        var defaultRom = new Mock<IBusTarget>();
        var slotRom = new Mock<IBusTarget>();

        var card = CreateMockPeripheral();
        card.Setup(c => c.ExpansionROMRegion).Returns(slotRom.Object);

        slotManager.SetDefaultExpansionRom(defaultRom.Object);
        slotManager.Install(6, card.Object);
        slotManager.SelectExpansionSlot(6);
        slotManager.DeselectExpansionSlot();

        var result = slotManager.GetVisibleExpansionRom();
        Assert.That(result, Is.SameAs(defaultRom.Object), "Should return default ROM after deselection");
    }

    /// <summary>
    /// Verifies that GetVisibleExpansionRom returns null when no default and no slot selected.
    /// </summary>
    [Test]
    public void GetVisibleExpansionRom_NoDefaultNoSlot_ReturnsNull()
    {
        var result = slotManager.GetVisibleExpansionRom();
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that internal expansion ROM takes precedence over default for selected slot.
    /// </summary>
    [Test]
    public void GetVisibleExpansionRom_InternalRomTakesPrecedenceOverCard()
    {
        var defaultRom = new Mock<IBusTarget>();
        var cardRom = new Mock<IBusTarget>();
        var internalRom = new Mock<IBusTarget>();

        var card = CreateMockPeripheral();
        card.Setup(c => c.ExpansionROMRegion).Returns(cardRom.Object);

        slotManager.SetDefaultExpansionRom(defaultRom.Object);
        slotManager.Install(3, card.Object);
        slotManager.RegisterInternalExpansionRom(3, internalRom.Object);
        slotManager.SelectExpansionSlot(3);

        var result = slotManager.GetVisibleExpansionRom();
        Assert.That(result, Is.SameAs(internalRom.Object), "Internal ROM takes precedence over card ROM");
    }

    /// <summary>
    /// Creates a mock slot card for testing.
    /// </summary>
    /// <returns>A mock slot card instance.</returns>
    private static Mock<ISlotCard> CreateMockPeripheral()
    {
        var mock = new Mock<ISlotCard>();
        mock.Setup(c => c.Name).Returns("Test Card");
        mock.Setup(c => c.DeviceType).Returns("TestDevice");
        mock.Setup(c => c.Kind).Returns(PeripheralKind.SlotCard);
        mock.SetupProperty(c => c.SlotNumber);
        mock.Setup(c => c.IOHandlers).Returns((SlotIOHandlers?)null);
        mock.Setup(c => c.ROMRegion).Returns((IBusTarget?)null);
        mock.Setup(c => c.ExpansionROMRegion).Returns((IBusTarget?)null);
        return mock;
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