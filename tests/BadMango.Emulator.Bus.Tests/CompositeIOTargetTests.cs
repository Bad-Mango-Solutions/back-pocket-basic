// <copyright file="CompositeIOTargetTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="CompositeIOTarget"/> class.
/// </summary>
[TestFixture]
public class CompositeIOTargetTests
{
    private const int PageSize = 4096;
    private const byte FloatingBusValue = 0xFF;

    private IOPageDispatcher softSwitches = null!;
    private Mock<ISlotManager> mockSlotManager = null!;
    private CompositeIOTarget ioPage = null!;

    /// <summary>
    /// Sets up the test fixture before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        softSwitches = new();
        mockSlotManager = new(MockBehavior.Strict);
        ioPage = new("Test I/O Page", softSwitches, mockSlotManager.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when name is null.
    /// </summary>
    [Test]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeIOTarget(null!, softSwitches, mockSlotManager.Object));
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when softSwitches is null.
    /// </summary>
    [Test]
    public void Constructor_NullSoftSwitches_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeIOTarget("Test", null!, mockSlotManager.Object));
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when slotManager is null.
    /// </summary>
    [Test]
    public void Constructor_NullSlotManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeIOTarget("Test", softSwitches, null!));
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that Name property returns expected value.
    /// </summary>
    [Test]
    public void Name_ReturnsExpectedValue()
    {
        Assert.That(ioPage.Name, Is.EqualTo("Test I/O Page"));
    }

    /// <summary>
    /// Verifies that Capabilities includes HasSideEffects, TimingSensitive, and base class capabilities.
    /// </summary>
    [Test]
    public void Capabilities_IncludesExpectedFlags()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ioPage.Capabilities.HasFlag(TargetCaps.HasSideEffects), Is.True);
            Assert.That(ioPage.Capabilities.HasFlag(TargetCaps.TimingSensitive), Is.True);
            Assert.That(ioPage.Capabilities.HasFlag(TargetCaps.SupportsPeek), Is.True, "Should include base class SupportsPeek");
            Assert.That(ioPage.Capabilities.HasFlag(TargetCaps.SupportsPoke), Is.True, "Should include base class SupportsPoke");
        });
    }

    #endregion

    #region Soft Switch Region Tests ($x000-$x0FF)

    /// <summary>
    /// Verifies that Read8 dispatches to soft switches for $x000-$x0FF.
    /// </summary>
    [Test]
    public void Read8_SoftSwitchRegion_DispatchesToIOPageDispatcher()
    {
        softSwitches.RegisterRead(0x30, (offset, in ctx) => 0x42);
        var access = CreateTestAccess(0xC030, AccessIntent.DataRead);

        byte result = ioPage.Read8(0x030, in access);

        Assert.That(result, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that Write8 dispatches to soft switches for $x000-$x0FF.
    /// </summary>
    [Test]
    public void Write8_SoftSwitchRegion_DispatchesToIOPageDispatcher()
    {
        bool writeCalled = false;
        softSwitches.RegisterWrite(0x10, (offset, value, in ctx) => writeCalled = true);
        var access = CreateTestAccess(0xC010, AccessIntent.DataWrite);

        ioPage.Write8(0x010, 0x55, in access);

        Assert.That(writeCalled, Is.True);
    }

    /// <summary>
    /// Verifies that unregistered soft switch read returns floating bus value.
    /// </summary>
    [Test]
    public void Read8_UnregisteredSoftSwitch_ReturnsFloatingBus()
    {
        var access = CreateTestAccess(0xC000, AccessIntent.DataRead);

        byte result = ioPage.Read8(0x000, in access);

        Assert.That(result, Is.EqualTo(FloatingBusValue));
    }

    #endregion

    #region Slot ROM Region Tests ($x100-$x7FF)

    /// <summary>
    /// Verifies that Read8 triggers expansion ROM selection for slot 6.
    /// </summary>
    [Test]
    public void Read8_SlotRom_TriggersExpansionRomSelection()
    {
        mockSlotManager.Setup(m => m.SelectExpansionSlot(6));
        mockSlotManager.Setup(m => m.GetSlotRomRegion(6)).Returns((IBusTarget?)null);

        var access = CreateTestAccess(0xC600, AccessIntent.DataRead);
        ioPage.Read8(0x600, in access);

        mockSlotManager.Verify(m => m.SelectExpansionSlot(6), Times.Once);
    }

    /// <summary>
    /// Verifies that Read8 returns ROM data from slot's ROM region.
    /// </summary>
    [Test]
    public void Read8_SlotRom_ReturnsRomData()
    {
        var memory = new PhysicalMemory(256, "SlotRom");
        memory.Fill(0xAA);
        var romTarget = new RomTarget(memory.Slice(0, 256));

        mockSlotManager.Setup(m => m.SelectExpansionSlot(6));
        mockSlotManager.Setup(m => m.GetSlotRomRegion(6)).Returns(romTarget);

        var access = CreateTestAccess(0xC600, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x600, in access);

        Assert.That(result, Is.EqualTo(0xAA));
    }

    /// <summary>
    /// Verifies that Read8 returns floating bus when slot has no ROM.
    /// </summary>
    [Test]
    public void Read8_SlotRomEmpty_ReturnsFloatingBus()
    {
        mockSlotManager.Setup(m => m.SelectExpansionSlot(1));
        mockSlotManager.Setup(m => m.GetSlotRomRegion(1)).Returns((IBusTarget?)null);

        var access = CreateTestAccess(0xC100, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x100, in access);

        Assert.That(result, Is.EqualTo(FloatingBusValue));
    }

    /// <summary>
    /// Verifies that Write8 to slot ROM still triggers expansion ROM selection.
    /// </summary>
    [Test]
    public void Write8_SlotRom_TriggersExpansionRomSelection()
    {
        mockSlotManager.Setup(m => m.SelectExpansionSlot(6));

        var access = CreateTestAccess(0xC600, AccessIntent.DataWrite);
        ioPage.Write8(0x600, 0x42, in access);

        mockSlotManager.Verify(m => m.SelectExpansionSlot(6), Times.Once);
    }

    /// <summary>
    /// Verifies that slot number is correctly extracted from various addresses.
    /// </summary>
    /// <param name="offset">The offset within the I/O page.</param>
    /// <param name="expectedSlot">The expected slot number to be selected.</param>
    [TestCase((ushort)0x100, 1)]
    [TestCase((ushort)0x200, 2)]
    [TestCase((ushort)0x300, 3)]
    [TestCase((ushort)0x400, 4)]
    [TestCase((ushort)0x500, 5)]
    [TestCase((ushort)0x600, 6)]
    [TestCase((ushort)0x700, 7)]
    public void Read8_SlotRom_ExtractsCorrectSlotNumber(ushort offset, int expectedSlot)
    {
        mockSlotManager.Setup(m => m.SelectExpansionSlot(expectedSlot));
        mockSlotManager.Setup(m => m.GetSlotRomRegion(expectedSlot)).Returns((IBusTarget?)null);

        var access = CreateTestAccess((Addr)(0xC000 + offset), AccessIntent.DataRead);
        ioPage.Read8(offset, in access);

        mockSlotManager.Verify(m => m.SelectExpansionSlot(expectedSlot), Times.Once);
    }

    #endregion

    #region Expansion ROM Region Tests ($x800-$xFFF)

    /// <summary>
    /// Verifies that Read8 from $xFFF triggers expansion ROM deselection.
    /// </summary>
    [Test]
    public void Read8_CFFF_DeselectsExpansionRom()
    {
        mockSlotManager.Setup(m => m.DeselectExpansionSlot());

        var access = CreateTestAccess(0xCFFF, AccessIntent.DataRead);
        ioPage.Read8(0xFFF, in access);

        mockSlotManager.Verify(m => m.DeselectExpansionSlot(), Times.Once);
    }

    /// <summary>
    /// Verifies that Read8 from $xFFF returns floating bus.
    /// </summary>
    [Test]
    public void Read8_CFFF_ReturnsFloatingBus()
    {
        mockSlotManager.Setup(m => m.DeselectExpansionSlot());

        var access = CreateTestAccess(0xCFFF, AccessIntent.DataRead);
        byte result = ioPage.Read8(0xFFF, in access);

        Assert.That(result, Is.EqualTo(FloatingBusValue));
    }

    /// <summary>
    /// Verifies that Write8 to $xFFF triggers expansion ROM deselection.
    /// </summary>
    [Test]
    public void Write8_CFFF_DeselectsExpansionRom()
    {
        mockSlotManager.Setup(m => m.DeselectExpansionSlot());

        var access = CreateTestAccess(0xCFFF, AccessIntent.DataWrite);
        ioPage.Write8(0xFFF, 0x42, in access);

        mockSlotManager.Verify(m => m.DeselectExpansionSlot(), Times.Once);
    }

    /// <summary>
    /// Verifies that Read8 returns expansion ROM data from selected slot.
    /// </summary>
    [Test]
    public void Read8_ExpansionRom_ReturnsDataFromSelectedSlot()
    {
        var memory = new PhysicalMemory(2048, "ExpansionRom");
        memory.Fill(0xBB);
        var expRomTarget = new RomTarget(memory.Slice(0, 2048));

        mockSlotManager.Setup(m => m.ActiveExpansionSlot).Returns(6);
        mockSlotManager.Setup(m => m.GetExpansionRomRegion(6)).Returns(expRomTarget);

        var access = CreateTestAccess(0xC800, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x800, in access);

        Assert.That(result, Is.EqualTo(0xBB));
    }

    /// <summary>
    /// Verifies that Read8 returns floating bus when no expansion ROM is selected.
    /// </summary>
    [Test]
    public void Read8_ExpansionRom_NoSlotSelected_ReturnsFloatingBus()
    {
        mockSlotManager.Setup(m => m.ActiveExpansionSlot).Returns((int?)null);

        var access = CreateTestAccess(0xC800, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x800, in access);

        Assert.That(result, Is.EqualTo(FloatingBusValue));
    }

    /// <summary>
    /// Verifies that Read8 returns floating bus when selected slot has no expansion ROM.
    /// </summary>
    [Test]
    public void Read8_ExpansionRom_SlotHasNoExpRom_ReturnsFloatingBus()
    {
        mockSlotManager.Setup(m => m.ActiveExpansionSlot).Returns(6);
        mockSlotManager.Setup(m => m.GetExpansionRomRegion(6)).Returns((IBusTarget?)null);

        var access = CreateTestAccess(0xC800, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x800, in access);

        Assert.That(result, Is.EqualTo(FloatingBusValue));
    }

    /// <summary>
    /// Verifies that expansion ROM offset is correctly calculated.
    /// </summary>
    [Test]
    public void Read8_ExpansionRom_CorrectOffsetCalculation()
    {
        var memory = new PhysicalMemory(2048, "ExpansionRom");
        memory.AsSpan()[0x100] = 0xCC; // Offset 0x100 into expansion ROM
        var expRomTarget = new RomTarget(memory.Slice(0, 2048));

        mockSlotManager.Setup(m => m.ActiveExpansionSlot).Returns(6);
        mockSlotManager.Setup(m => m.GetExpansionRomRegion(6)).Returns(expRomTarget);

        // $C900 = offset 0x900 in I/O page, which is offset 0x100 in expansion ROM (0x900 - 0x800)
        var access = CreateTestAccess(0xC900, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x900, in access);

        Assert.That(result, Is.EqualTo(0xCC));
    }

    #endregion

    #region INTCXROM Tests

    /// <summary>
    /// Verifies that INTCXROM overrides slot ROM with internal ROM.
    /// </summary>
    [Test]
    public void Read8_IntCxRomEnabled_ReturnsInternalRomForSlotRom()
    {
        var internalMemory = new PhysicalMemory(PageSize, "InternalRom");
        internalMemory.AsSpan()[0x600] = 0xDD;
        var internalRom = new RomTarget(internalMemory.Slice(0, PageSize));

        ioPage.SetInternalRom(internalRom);
        ioPage.SetIntCxRom(true);

        // Should not call slot manager when INTCXROM is enabled
        var access = CreateTestAccess(0xC600, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x600, in access);

        Assert.That(result, Is.EqualTo(0xDD));
        mockSlotManager.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that INTCXROM overrides expansion ROM with internal ROM.
    /// </summary>
    [Test]
    public void Read8_IntCxRomEnabled_ReturnsInternalRomForExpansionRom()
    {
        var internalMemory = new PhysicalMemory(PageSize, "InternalRom");
        internalMemory.AsSpan()[0x800] = 0xEE;
        var internalRom = new RomTarget(internalMemory.Slice(0, PageSize));

        ioPage.SetInternalRom(internalRom);
        ioPage.SetIntCxRom(true);

        var access = CreateTestAccess(0xC800, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x800, in access);

        Assert.That(result, Is.EqualTo(0xEE));
        mockSlotManager.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that INTCXROM prevents expansion ROM selection on writes.
    /// </summary>
    [Test]
    public void Write8_IntCxRomEnabled_DoesNotSelectExpansionRom()
    {
        var internalMemory = new PhysicalMemory(PageSize, "InternalRom");
        var internalRom = new RomTarget(internalMemory.Slice(0, PageSize));

        ioPage.SetInternalRom(internalRom);
        ioPage.SetIntCxRom(true);

        var access = CreateTestAccess(0xC600, AccessIntent.DataWrite);
        ioPage.Write8(0x600, 0x42, in access);

        mockSlotManager.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that disabling INTCXROM restores normal slot ROM access.
    /// </summary>
    [Test]
    public void Read8_IntCxRomDisabled_ReturnsSlotRom()
    {
        var slotMemory = new PhysicalMemory(256, "SlotRom");
        slotMemory.Fill(0x77);
        var slotRom = new RomTarget(slotMemory.Slice(0, 256));

        mockSlotManager.Setup(m => m.SelectExpansionSlot(6));
        mockSlotManager.Setup(m => m.GetSlotRomRegion(6)).Returns(slotRom);

        ioPage.SetIntCxRom(false);

        var access = CreateTestAccess(0xC600, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x600, in access);

        Assert.That(result, Is.EqualTo(0x77));
    }

    #endregion

    #region INTC3ROM Tests

    /// <summary>
    /// Verifies that INTC3ROM defaults to enabled.
    /// </summary>
    [Test]
    public void IntC3Rom_DefaultsToEnabled()
    {
        var internalMemory = new PhysicalMemory(PageSize, "InternalRom");
        internalMemory.AsSpan()[0x300] = 0x88;
        var internalRom = new RomTarget(internalMemory.Slice(0, PageSize));

        ioPage.SetInternalRom(internalRom);

        // Slot 3 should use internal ROM by default (INTC3ROM = ON)
        var access = CreateTestAccess(0xC300, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x300, in access);

        Assert.That(result, Is.EqualTo(0x88));
        mockSlotManager.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that INTC3ROM only affects slot 3.
    /// </summary>
    [Test]
    public void Read8_IntC3RomEnabled_OnlyAffectsSlot3()
    {
        var internalMemory = new PhysicalMemory(PageSize, "InternalRom");
        var internalRom = new RomTarget(internalMemory.Slice(0, PageSize));

        var slotMemory = new PhysicalMemory(256, "SlotRom");
        slotMemory.Fill(0x99);
        var slotRom = new RomTarget(slotMemory.Slice(0, 256));

        ioPage.SetInternalRom(internalRom);
        ioPage.SetIntC3Rom(true); // Default

        // Slot 6 should still use slot ROM
        mockSlotManager.Setup(m => m.SelectExpansionSlot(6));
        mockSlotManager.Setup(m => m.GetSlotRomRegion(6)).Returns(slotRom);

        var access = CreateTestAccess(0xC600, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x600, in access);

        Assert.That(result, Is.EqualTo(0x99));
        mockSlotManager.Verify(m => m.SelectExpansionSlot(6), Times.Once);
    }

    /// <summary>
    /// Verifies that disabling INTC3ROM allows slot 3 ROM to be visible.
    /// </summary>
    [Test]
    public void Read8_IntC3RomDisabled_ReturnsSlot3Rom()
    {
        var slotMemory = new PhysicalMemory(256, "Slot3Rom");
        slotMemory.Fill(0xAA);
        var slotRom = new RomTarget(slotMemory.Slice(0, 256));

        mockSlotManager.Setup(m => m.SelectExpansionSlot(3));
        mockSlotManager.Setup(m => m.GetSlotRomRegion(3)).Returns(slotRom);

        ioPage.SetIntC3Rom(false);

        var access = CreateTestAccess(0xC300, AccessIntent.DataRead);
        byte result = ioPage.Read8(0x300, in access);

        Assert.That(result, Is.EqualTo(0xAA));
        mockSlotManager.Verify(m => m.SelectExpansionSlot(3), Times.Once);
    }

    /// <summary>
    /// Verifies that INTC3ROM prevents expansion ROM selection for slot 3 writes.
    /// </summary>
    [Test]
    public void Write8_IntC3RomEnabled_DoesNotSelectSlot3ExpansionRom()
    {
        var internalMemory = new PhysicalMemory(PageSize, "InternalRom");
        var internalRom = new RomTarget(internalMemory.Slice(0, PageSize));

        ioPage.SetInternalRom(internalRom);
        ioPage.SetIntC3Rom(true);

        var access = CreateTestAccess(0xC300, AccessIntent.DataWrite);
        ioPage.Write8(0x300, 0x42, in access);

        mockSlotManager.VerifyNoOtherCalls();
    }

    #endregion

    #region SetInternalRom Tests

    /// <summary>
    /// Verifies that SetInternalRom throws ArgumentNullException for null.
    /// </summary>
    [Test]
    public void SetInternalRom_NullRom_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ioPage.SetInternalRom(null!));
    }

    #endregion

    #region GetSubRegionTag Tests

    /// <summary>
    /// Verifies GetSubRegionTag returns Io for soft switch region.
    /// </summary>
    [Test]
    public void GetSubRegionTag_SoftSwitchRegion_ReturnsIo()
    {
        Assert.That(ioPage.GetSubRegionTag(0x030), Is.EqualTo(RegionTag.Io));
    }

    /// <summary>
    /// Verifies GetSubRegionTag returns Slot for slot ROM region.
    /// </summary>
    [Test]
    public void GetSubRegionTag_SlotRomRegion_ReturnsSlot()
    {
        Assert.That(ioPage.GetSubRegionTag(0x600), Is.EqualTo(RegionTag.Slot));
    }

    /// <summary>
    /// Verifies GetSubRegionTag returns Rom for expansion ROM region.
    /// </summary>
    [Test]
    public void GetSubRegionTag_ExpansionRomRegion_ReturnsRom()
    {
        Assert.That(ioPage.GetSubRegionTag(0x800), Is.EqualTo(RegionTag.Rom));
    }

    #endregion

    #region ResolveTarget Tests

    /// <summary>
    /// Verifies ResolveTarget returns self for soft switch region.
    /// </summary>
    /// <remarks>
    /// The soft switch region (offsets 0-0xFF) is handled directly by the
    /// CompositeIOTarget's Read8/Write8 methods via the IOPageDispatcher.
    /// ResolveTarget returns <c>this</c> so the bus calls Read8/Write8 on
    /// the CompositeIOTarget, which then dispatches to the appropriate handler.
    /// </remarks>
    [Test]
    public void ResolveTarget_SoftSwitchRegion_ReturnsSelf()
    {
        IBusTarget? target = ioPage.ResolveTarget(0x030, AccessIntent.DataRead);
        Assert.That(target, Is.SameAs(ioPage), "Soft switch region should return self");
    }

    /// <summary>
    /// Verifies ResolveTarget returns slot ROM target when available.
    /// </summary>
    [Test]
    public void ResolveTarget_SlotRomRegion_ReturnsSlotRom()
    {
        var slotMemory = new PhysicalMemory(256, "SlotRom");
        var slotRom = new RomTarget(slotMemory.Slice(0, 256));

        mockSlotManager.Setup(m => m.GetSlotRomRegion(6)).Returns(slotRom);

        IBusTarget? target = ioPage.ResolveTarget(0x600, AccessIntent.DataRead);
        Assert.That(target, Is.SameAs(slotRom));
    }

    /// <summary>
    /// Verifies ResolveTarget returns 'this' when INTCXROM is enabled.
    /// This allows the CompositeIOTarget to handle address translation for debug writes.
    /// </summary>
    [Test]
    public void ResolveTarget_IntCxRomEnabled_ReturnsSelfForDebugWriteSupport()
    {
        var internalMemory = new PhysicalMemory(PageSize, "InternalRom");
        var internalRom = new RomTarget(internalMemory.Slice(0, PageSize));

        ioPage.SetInternalRom(internalRom);
        ioPage.SetIntCxRom(true);

        IBusTarget? target = ioPage.ResolveTarget(0x600, AccessIntent.DataRead);

        // Returns 'this' (CompositeIOTarget) to handle debug writes properly
        Assert.That(target, Is.SameAs(ioPage));
    }

    /// <summary>
    /// Verifies ResolveTarget returns expansion ROM target when slot is selected.
    /// </summary>
    [Test]
    public void ResolveTarget_ExpansionRomRegion_ReturnsExpansionRom()
    {
        var expMemory = new PhysicalMemory(2048, "ExpansionRom");
        var expRom = new RomTarget(expMemory.Slice(0, 2048));

        mockSlotManager.Setup(m => m.ActiveExpansionSlot).Returns(6);
        mockSlotManager.Setup(m => m.GetExpansionRomRegion(6)).Returns(expRom);

        IBusTarget? target = ioPage.ResolveTarget(0x800, AccessIntent.DataRead);
        Assert.That(target, Is.SameAs(expRom));
    }

    /// <summary>
    /// Verifies ResolveTarget returns null for $xFFF.
    /// </summary>
    [Test]
    public void ResolveTarget_xFFF_ReturnsNull()
    {
        IBusTarget? target = ioPage.ResolveTarget(0xFFF, AccessIntent.DataRead);
        Assert.That(target, Is.Null);
    }

    #endregion

    #region Initialize Tests

    /// <summary>
    /// Verifies that Initialize does not throw.
    /// </summary>
    [Test]
    public void Initialize_DoesNotThrow()
    {
        var mockContext = new Mock<IEventContext>();
        Assert.DoesNotThrow(() => ioPage.Initialize(mockContext.Object));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test bus access structure.
    /// </summary>
    private static BusAccess CreateTestAccess(Addr address, AccessIntent intent)
    {
        return new(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: intent,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
    }

    #endregion
}