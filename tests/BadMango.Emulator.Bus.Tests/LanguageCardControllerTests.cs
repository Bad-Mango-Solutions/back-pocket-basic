// <copyright file="LanguageCardControllerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Bus.Interfaces;
using Moq;

/// <summary>
/// Unit tests for the <see cref="LanguageCardController"/> class.
/// </summary>
[TestFixture]
public class LanguageCardControllerTests
{
    private const int PageSize = 4096;

    /// <summary>
    /// Verifies that the controller is created with correct initial state.
    /// </summary>
    [Test]
    public void Constructor_CreatesWithCorrectInitialState()
    {
        var controller = new LanguageCardController();

        Assert.Multiple(() =>
        {
            Assert.That(controller.Name, Is.EqualTo("Language Card Controller"));
            Assert.That(controller.IOHandlers, Is.Not.Null);
        });
    }

    /// <summary>
    /// Verifies that Initialize throws when context is null.
    /// </summary>
    [Test]
    public void Initialize_NullContext_ThrowsArgumentNullException()
    {
        var controller = new LanguageCardController();

        Assert.Throws<ArgumentNullException>(() => controller.Initialize(null!));
    }

    /// <summary>
    /// Verifies that Initialize throws when swap group is not found.
    /// </summary>
    [Test]
    public void Initialize_SwapGroupNotFound_ThrowsInvalidOperationException()
    {
        var controller = new LanguageCardController();
        var bus = new MainBus();
        var context = CreateMockEventContext(bus);

        Assert.Throws<InvalidOperationException>(() => controller.Initialize(context));
    }

    /// <summary>
    /// Verifies that Initialize sets up the initial state correctly.
    /// </summary>
    [Test]
    public void Initialize_SetsInitialState()
    {
        var (controller, bus) = CreateInitializedController();

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.False, "RAM read should be disabled initially");
            Assert.That(controller.IsRamWriteEnabled, Is.False, "RAM write should be disabled initially");
            Assert.That(controller.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected initially");
        });
    }

    /// <summary>
    /// Verifies that $C080 enables RAM read, disables write, and selects bank 2.
    /// </summary>
    [Test]
    public void Switch_C080_EnablesRamRead_DisablesWrite_SelectsBank2()
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, 0x00);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.True, "RAM read should be enabled");
            Assert.That(controller.IsRamWriteEnabled, Is.False, "RAM write should be disabled");
            Assert.That(controller.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C081 (single read) disables RAM read and prepares for write enable.
    /// </summary>
    [Test]
    public void Switch_C081_SingleRead_DisablesRamRead_DoesNotEnableWrite()
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, 0x01);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.False, "RAM read should be disabled");
            Assert.That(controller.IsRamWriteEnabled, Is.False, "RAM write should be disabled after single read");
            Assert.That(controller.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C081 (double read) enables RAM write.
    /// </summary>
    [Test]
    public void Switch_C081_DoubleRead_EnablesRamWrite()
    {
        var (controller, bus) = CreateInitializedController();

        // First read primes the R×2 protocol
        SimulateRead(controller, 0x01);

        // Second consecutive read enables write
        SimulateRead(controller, 0x01);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.False, "RAM read should be disabled");
            Assert.That(controller.IsRamWriteEnabled, Is.True, "RAM write should be enabled after double read");
            Assert.That(controller.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C082 disables RAM read and write, selects bank 2.
    /// </summary>
    [Test]
    public void Switch_C082_DisablesRamReadWrite_SelectsBank2()
    {
        var (controller, bus) = CreateInitializedController();

        // First enable everything
        SimulateRead(controller, 0x03);
        SimulateRead(controller, 0x03);

        // Now access C082 to disable
        SimulateRead(controller, 0x02);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.False, "RAM read should be disabled");
            Assert.That(controller.IsRamWriteEnabled, Is.False, "RAM write should be disabled");
            Assert.That(controller.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C083 (double read) enables RAM read and write, selects bank 2.
    /// </summary>
    [Test]
    public void Switch_C083_DoubleRead_EnablesRamReadWrite_SelectsBank2()
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, 0x03);
        SimulateRead(controller, 0x03);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.True, "RAM read should be enabled");
            Assert.That(controller.IsRamWriteEnabled, Is.True, "RAM write should be enabled after double read");
            Assert.That(controller.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C088 enables RAM read, disables write, and selects bank 1.
    /// </summary>
    [Test]
    public void Switch_C088_EnablesRamRead_DisablesWrite_SelectsBank1()
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, 0x08);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.True, "RAM read should be enabled");
            Assert.That(controller.IsRamWriteEnabled, Is.False, "RAM write should be disabled");
            Assert.That(controller.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C089 (double read) enables RAM write and selects bank 1.
    /// </summary>
    [Test]
    public void Switch_C089_DoubleRead_EnablesRamWrite_SelectsBank1()
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, 0x09);
        SimulateRead(controller, 0x09);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.False, "RAM read should be disabled");
            Assert.That(controller.IsRamWriteEnabled, Is.True, "RAM write should be enabled after double read");
            Assert.That(controller.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C08A disables RAM read and write, selects bank 1.
    /// </summary>
    [Test]
    public void Switch_C08A_DisablesRamReadWrite_SelectsBank1()
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, 0x0A);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.False, "RAM read should be disabled");
            Assert.That(controller.IsRamWriteEnabled, Is.False, "RAM write should be disabled");
            Assert.That(controller.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected");
        });
    }

    /// <summary>
    /// Verifies that $C08B (double read) enables RAM read and write, selects bank 1.
    /// </summary>
    [Test]
    public void Switch_C08B_DoubleRead_EnablesRamReadWrite_SelectsBank1()
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, 0x0B);
        SimulateRead(controller, 0x0B);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.True, "RAM read should be enabled");
            Assert.That(controller.IsRamWriteEnabled, Is.True, "RAM write should be enabled after double read");
            Assert.That(controller.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected");
        });
    }

    /// <summary>
    /// Verifies that the R×2 protocol requires consecutive reads of the same address.
    /// </summary>
    [Test]
    public void Rx2Protocol_RequiresConsecutiveReadsOfSameAddress()
    {
        var (controller, bus) = CreateInitializedController();

        // Read odd address to prime R×2
        SimulateRead(controller, 0x01);

        // Read a DIFFERENT odd address - should NOT enable write
        SimulateRead(controller, 0x03);

        Assert.That(controller.IsRamWriteEnabled, Is.False, "Write should not be enabled for different addresses");

        // Now do two consecutive reads of same address
        SimulateRead(controller, 0x03);
        SimulateRead(controller, 0x03);

        Assert.That(controller.IsRamWriteEnabled, Is.True, "Write should be enabled after consecutive reads of same address");
    }

    /// <summary>
    /// Verifies that reading an even address clears the R×2 prewrite state.
    /// </summary>
    [Test]
    public void Rx2Protocol_EvenReadClearsPrewriteState()
    {
        var (controller, bus) = CreateInitializedController();

        // Prime R×2 with odd address
        SimulateRead(controller, 0x01);

        // Read even address - should clear prewrite
        SimulateRead(controller, 0x00);

        // Read same odd address again - should NOT enable write (prewrite was cleared)
        SimulateRead(controller, 0x01);

        Assert.That(controller.IsRamWriteEnabled, Is.False, "Write should not be enabled after prewrite was cleared");
    }

    /// <summary>
    /// Verifies that writes clear the R×2 prewrite state.
    /// </summary>
    [Test]
    public void Rx2Protocol_WriteClearsPrewriteState()
    {
        var (controller, bus) = CreateInitializedController();

        // Prime R×2 with odd address read
        SimulateRead(controller, 0x01);

        // Write to same address - should clear prewrite
        SimulateWrite(controller, 0x01);

        // Read same odd address again - should NOT enable write (prewrite was cleared)
        SimulateRead(controller, 0x01);

        Assert.That(controller.IsRamWriteEnabled, Is.False, "Write should not be enabled after prewrite was cleared by write");
    }

    /// <summary>
    /// Verifies that all 16 switch addresses decode correctly.
    /// </summary>
    /// <param name="offset">Switch offset (0x00-0x0F).</param>
    /// <param name="expectedRamRead">Expected RAM read state.</param>
    /// <param name="expectedBank2">Expected bank 2 selection.</param>
    [TestCase(0x00, true, true)]
    [TestCase(0x01, false, true)]
    [TestCase(0x02, false, true)]
    [TestCase(0x03, true, true)]
    [TestCase(0x04, true, true)]
    [TestCase(0x05, false, true)]
    [TestCase(0x06, false, true)]
    [TestCase(0x07, true, true)]
    [TestCase(0x08, true, false)]
    [TestCase(0x09, false, false)]
    [TestCase(0x0A, false, false)]
    [TestCase(0x0B, true, false)]
    [TestCase(0x0C, true, false)]
    [TestCase(0x0D, false, false)]
    [TestCase(0x0E, false, false)]
    [TestCase(0x0F, true, false)]
    public void AllSwitches_DecodeCorrectly(byte offset, bool expectedRamRead, bool expectedBank2)
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, offset);

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.EqualTo(expectedRamRead), $"RAM read for offset 0x{offset:X2}");
            Assert.That(controller.SelectedBank, Is.EqualTo(expectedBank2 ? 2 : 1), $"Bank selection for offset 0x{offset:X2}");
        });
    }

    /// <summary>
    /// Verifies that write-capable addresses enable write after double read.
    /// </summary>
    /// <param name="offset">Switch offset with write-enable capability.</param>
    [TestCase(0x01)] // $C081
    [TestCase(0x03)] // $C083
    [TestCase(0x05)] // $C085
    [TestCase(0x07)] // $C087
    [TestCase(0x09)] // $C089
    [TestCase(0x0B)] // $C08B
    [TestCase(0x0D)] // $C08D
    [TestCase(0x0F)] // $C08F
    public void WriteEnableAddresses_EnableWriteAfterDoubleRead(byte offset)
    {
        var (controller, bus) = CreateInitializedController();

        SimulateRead(controller, offset);
        Assert.That(controller.IsRamWriteEnabled, Is.False, "Should not be enabled after single read");

        SimulateRead(controller, offset);
        Assert.That(controller.IsRamWriteEnabled, Is.True, $"Should be enabled after double read for offset 0x{offset:X2}");
    }

    /// <summary>
    /// Verifies that non-write-enable addresses do not enable write.
    /// </summary>
    /// <param name="offset">Switch offset without write-enable capability.</param>
    [TestCase(0x00)] // $C080
    [TestCase(0x02)] // $C082
    [TestCase(0x04)] // $C084
    [TestCase(0x06)] // $C086
    [TestCase(0x08)] // $C088
    [TestCase(0x0A)] // $C08A
    [TestCase(0x0C)] // $C08C
    [TestCase(0x0E)] // $C08E
    public void NonWriteEnableAddresses_DoNotEnableWrite(byte offset)
    {
        var (controller, bus) = CreateInitializedController();

        // Multiple reads of even address should never enable write
        SimulateRead(controller, offset);
        SimulateRead(controller, offset);
        SimulateRead(controller, offset);

        Assert.That(controller.IsRamWriteEnabled, Is.False, $"Even address 0x{offset:X2} should never enable write");
    }

    /// <summary>
    /// Verifies that layer activation reflects RAM read state.
    /// </summary>
    [Test]
    public void ApplyState_ActivatesLayerWhenRamReadEnabled()
    {
        var (controller, bus) = CreateInitializedController();

        // Enable RAM read
        SimulateRead(controller, 0x00);

        Assert.That(bus.IsLayerActive(LanguageCardController.LayerName), Is.True, "Layer should be active when RAM read enabled");

        // Disable RAM read
        SimulateRead(controller, 0x02);

        Assert.That(bus.IsLayerActive(LanguageCardController.LayerName), Is.False, "Layer should be inactive when RAM read disabled");
    }

    /// <summary>
    /// Verifies that bank selection updates the swap group variant.
    /// </summary>
    [Test]
    public void ApplyState_SelectsCorrectBankVariant()
    {
        var (controller, bus) = CreateInitializedController();

        // Initially, ROM variant is selected (RAM read disabled)
        uint groupId = bus.GetSwapGroupId(LanguageCardController.SwapGroupName);
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo(LanguageCardController.RomVariantName), "ROM variant selected when RAM read disabled");

        // Enable RAM read ($C080) - should select Bank 2
        SimulateRead(controller, 0x00);
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo(LanguageCardController.Bank2VariantName), "Bank 2 should be selected after enabling RAM");

        // Switch to Bank 1 ($C088)
        SimulateRead(controller, 0x08);
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo(LanguageCardController.Bank1VariantName), "Bank 1 should be selected");

        // Switch back to Bank 2 ($C080)
        SimulateRead(controller, 0x00);
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo(LanguageCardController.Bank2VariantName), "Bank 2 should be selected");

        // Disable RAM read ($C082) - should select ROM variant
        SimulateRead(controller, 0x02);
        Assert.That(bus.GetActiveSwapVariant(groupId), Is.EqualTo(LanguageCardController.RomVariantName), "ROM variant selected when RAM read disabled");
    }

    /// <summary>
    /// Verifies that Reset restores the power-on state.
    /// </summary>
    [Test]
    public void Reset_RestoresPowerOnState()
    {
        var (controller, bus) = CreateInitializedController();

        // Enable everything
        SimulateRead(controller, 0x0B); // Bank 1, RAM, prewrite
        SimulateRead(controller, 0x0B); // Enable write

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.True);
            Assert.That(controller.IsRamWriteEnabled, Is.True);
            Assert.That(controller.SelectedBank, Is.EqualTo(1));
        });

        // Reset
        controller.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(controller.IsRamReadEnabled, Is.False, "RAM read should be disabled after reset");
            Assert.That(controller.IsRamWriteEnabled, Is.False, "RAM write should be disabled after reset");
            Assert.That(controller.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected after reset");
        });
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't change state.
    /// </summary>
    [Test]
    public void SideEffectFreeRead_DoesNotChangeState()
    {
        var (controller, bus) = CreateInitializedController();

        // Enable RAM read
        SimulateRead(controller, 0x00);
        Assert.That(controller.IsRamReadEnabled, Is.True);

        // Side-effect-free read of ROM-enabling address should not change state
        SimulateSideEffectFreeRead(controller, 0x02);
        Assert.That(controller.IsRamReadEnabled, Is.True, "Side-effect-free read should not change state");
    }

    /// <summary>
    /// Verifies that write access does not enable write (only R×2 reads do).
    /// </summary>
    [Test]
    public void WriteAccess_DoesNotEnableWrite()
    {
        var (controller, bus) = CreateInitializedController();

        // Write to write-enable address - should not enable write
        SimulateWrite(controller, 0x01);
        SimulateWrite(controller, 0x01);

        Assert.That(controller.IsRamWriteEnabled, Is.False, "Write access should not enable write");
    }

    /// <summary>
    /// Verifies that layer permissions are updated based on write enable state.
    /// </summary>
    /// <remarks>
    /// This test checks the E000-FFFF range which is controlled by the layer.
    /// The D000-DFFF range is controlled by the swap group and has separate
    /// permission handling.
    /// </remarks>
    [Test]
    public void ApplyState_UpdatesLayerPermissions()
    {
        var (controller, bus) = CreateInitializedController();

        // Enable RAM read with write enabled ($C083, double read)
        SimulateRead(controller, 0x03);
        SimulateRead(controller, 0x03);

        Assert.That(controller.IsRamWriteEnabled, Is.True);

        // Verify layer (E000-FFFF) is active and writable
        var entry = bus.GetEffectiveMapping(0xE000);
        Assert.That(entry.CanWrite, Is.True, "Layer should be writable when write is enabled");

        // Disable write by reading an even address ($C080)
        SimulateRead(controller, 0x00);

        entry = bus.GetEffectiveMapping(0xE000);
        Assert.That(entry.CanWrite, Is.False, "Layer should not be writable when write is disabled");
    }

    /// <summary>
    /// Verifies the complete Language Card simulation with actual memory reads.
    /// </summary>
    [Test]
    public void LanguageCardSimulation_EndToEnd()
    {
        var (controller, bus) = CreateInitializedController();

        // Initially ROM should be visible at D000
        var readAccessD000 = CreateTestAccess(0xD000, AccessIntent.DataRead);
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0xFF), "ROM should be visible initially at D000");

        // E000 should also show ROM initially
        var readAccessE000 = CreateTestAccess(0xE000, AccessIntent.DataRead);
        Assert.That(bus.Read8(readAccessE000), Is.EqualTo(0xFF), "ROM should be visible initially at E000");

        // Enable RAM read ($C080) - Bank 2 selected
        SimulateRead(controller, 0x00);

        // Now RAM should be visible (initialized to 0x00)
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0x00), "D000 RAM should be visible after enabling");
        Assert.That(bus.Read8(readAccessE000), Is.EqualTo(0x00), "E000 RAM should be visible after enabling");

        // Write to E000 should fail (write disabled) - layer enforces this
        var writeAccessE000 = CreateTestAccess(0xE000, AccessIntent.DataWrite);
        var writeResult = bus.TryWrite8(writeAccessE000, 0xAA);
        Assert.That(writeResult.Failed, Is.True, "Write to E000 should fail when write is disabled");

        // Enable write ($C083, double read)
        SimulateRead(controller, 0x03);
        SimulateRead(controller, 0x03);

        // Now write to E000 should succeed
        writeResult = bus.TryWrite8(writeAccessE000, 0xAA);
        Assert.That(writeResult.Ok, Is.True, "Write to E000 should succeed when write is enabled");
        Assert.That(bus.Read8(readAccessE000), Is.EqualTo(0xAA), "Written value should be readable at E000");

        // Write to D000 (swap group page - always writable when RAM is selected)
        var writeAccessD000 = CreateTestAccess(0xD000, AccessIntent.DataWrite);
        bus.Write8(writeAccessD000, 0x42);
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0x42), "Written value should be readable at D000");

        // Switch to Bank 1 ($C08B, double read for write)
        SimulateRead(controller, 0x0B);
        SimulateRead(controller, 0x0B);

        // Bank 1 should have different content (initial value 0x00)
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0x00), "Bank 1 should have initial value");

        // Write different value to Bank 1
        bus.Write8(writeAccessD000, 0x99);
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0x99), "Bank 1 write should work");

        // Switch back to Bank 2 - should see our original write
        SimulateRead(controller, 0x03);
        SimulateRead(controller, 0x03);
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0x42), "Bank 2 should retain written value");

        // E000 should still have the value we wrote
        Assert.That(bus.Read8(readAccessE000), Is.EqualTo(0xAA), "E000 should retain written value");

        // Disable RAM ($C082) - ROM should be visible
        SimulateRead(controller, 0x02);
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0xFF), "ROM should be visible at D000 after disabling RAM");
        Assert.That(bus.Read8(readAccessE000), Is.EqualTo(0xFF), "ROM should be visible at E000 after disabling RAM");

        // Re-enable RAM and verify data is preserved
        SimulateRead(controller, 0x03);
        SimulateRead(controller, 0x03);
        Assert.That(bus.Read8(readAccessD000), Is.EqualTo(0x42), "Bank 2 data should be preserved");
        Assert.That(bus.Read8(readAccessE000), Is.EqualTo(0xAA), "E000 data should be preserved");
    }

    // ─── Helper Methods ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a fully initialized Language Card controller with required infrastructure.
    /// </summary>
    private static (LanguageCardController Controller, MainBus Bus) CreateInitializedController()
    {
        var bus = new MainBus();

        // Create ROM for D000-FFFF (3 pages)
        var romMemory = new PhysicalMemory(0x3000, "ROM");
        for (int i = 0; i < 0x3000; i++)
        {
            romMemory.AsSpan()[i] = 0xFF;
        }

        var romTarget = new RomTarget(romMemory.ReadOnlySlice(0, 0x3000));

        // Create Language Card RAM Bank 1 (D000-DFFF, 1 page)
        var lcBank1Memory = new PhysicalMemory(PageSize, "LCBank1");
        var lcBank1Target = new RamTarget(lcBank1Memory.Slice(0, PageSize));

        // Create Language Card RAM Bank 2 (D000-DFFF, 1 page)
        var lcBank2Memory = new PhysicalMemory(PageSize, "LCBank2");
        var lcBank2Target = new RamTarget(lcBank2Memory.Slice(0, PageSize));

        // Create Language Card upper RAM (E000-FFFF, 2 pages)
        var lcUpperMemory = new PhysicalMemory(0x2000, "LCUpper");
        var lcUpperTarget = new RamTarget(lcUpperMemory.Slice(0, 0x2000));

        // Map ROM as base at D000-FFFF
        bus.MapPageRange(0xD, 3, 1, RegionTag.Rom, PagePerms.ReadExecute, TargetCaps.SupportsPeek, romTarget, 0);
        bus.SaveBaseMappingRange(0xD, 3);

        // Create Language Card layer (priority 20)
        // Note: The layer ONLY covers E000-FFFF. The D000-DFFF bank switching is handled
        // separately by the swap group because there are two independent banks there.
        var lcLayer = bus.CreateLayer(LanguageCardController.LayerName, LanguageCardController.LayerPriority);

        // Add E000-FFFF mapping to the layer
        bus.AddLayeredMapping(new LayeredMapping(
            VirtualBase: 0xE000,
            Size: 0x2000,
            Layer: lcLayer,
            DeviceId: 2,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadExecute,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            Target: lcUpperTarget,
            PhysBase: 0));

        // Create swap group for D000 bank switching
        // The swap group has three variants: ROM, Bank1 RAM, and Bank2 RAM
        // When RAM read is disabled, ROM is selected. When enabled, Bank1 or Bank2 is selected.
        uint groupId = bus.CreateSwapGroup(LanguageCardController.SwapGroupName, virtualBase: 0xD000, size: 0x1000);

        // Create ROM target for D000 (first page of ROM)
        var d000RomTarget = new RomTarget(romMemory.ReadOnlySlice(0, PageSize));
        bus.AddSwapVariant(groupId, LanguageCardController.RomVariantName, d000RomTarget, physBase: 0, perms: PagePerms.ReadExecute);

        // Bank variants have full permissions because write protection is enforced
        // at a higher level (though D000 write protection is not fully implemented
        // due to swap group limitations)
        bus.AddSwapVariant(groupId, LanguageCardController.Bank1VariantName, lcBank1Target, physBase: 0, perms: PagePerms.All);
        bus.AddSwapVariant(groupId, LanguageCardController.Bank2VariantName, lcBank2Target, physBase: 0, perms: PagePerms.All);

        // Create and initialize controller
        var controller = new LanguageCardController();
        var context = CreateMockEventContext(bus);
        controller.Initialize(context);

        return (controller, bus);
    }

    /// <summary>
    /// Creates a mock event context with the specified bus.
    /// </summary>
    private static IEventContext CreateMockEventContext(IMemoryBus bus)
    {
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Bus).Returns(bus);
        return mockContext.Object;
    }

    /// <summary>
    /// Simulates a read access to a Language Card soft switch.
    /// </summary>
    private static void SimulateRead(LanguageCardController controller, byte offset)
    {
        var context = CreateTestAccess((uint)(0xC080 + offset), AccessIntent.DataRead);
        controller.IOHandlers.ReadHandlers[offset]?.Invoke((byte)(0x80 + offset), in context);
    }

    /// <summary>
    /// Simulates a side-effect-free read access to a Language Card soft switch.
    /// </summary>
    private static void SimulateSideEffectFreeRead(LanguageCardController controller, byte offset)
    {
        var context = CreateTestAccess((uint)(0xC080 + offset), AccessIntent.DataRead, AccessFlags.NoSideEffects);
        controller.IOHandlers.ReadHandlers[offset]?.Invoke((byte)(0x80 + offset), in context);
    }

    /// <summary>
    /// Simulates a write access to a Language Card soft switch.
    /// </summary>
    private static void SimulateWrite(LanguageCardController controller, byte offset)
    {
        var context = CreateTestAccess((uint)(0xC080 + offset), AccessIntent.DataWrite);
        controller.IOHandlers.WriteHandlers[offset]?.Invoke((byte)(0x80 + offset), 0x00, in context);
    }

    /// <summary>
    /// Helper method to create test bus access structures.
    /// </summary>
    private static BusAccess CreateTestAccess(
        Addr address,
        AccessIntent intent,
        AccessFlags flags = AccessFlags.None)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: intent,
            SourceId: 0,
            Cycle: 0,
            Flags: flags);
    }
}