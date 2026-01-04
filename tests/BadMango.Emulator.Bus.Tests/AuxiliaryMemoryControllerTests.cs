// <copyright file="AuxiliaryMemoryControllerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using BadMango.Emulator.Bus.Interfaces;
using Moq;

/// <summary>
/// Unit tests for the <see cref="AuxiliaryMemoryController"/> class.
/// </summary>
[TestFixture]
public class AuxiliaryMemoryControllerTests
{
    /// <summary>
    /// Verifies that the controller is created with the correct name.
    /// </summary>
    [Test]
    public void Constructor_CreatesWithCorrectName()
    {
        var controller = new AuxiliaryMemoryController();

        Assert.That(controller.Name, Is.EqualTo("Auxiliary Memory Controller"));
    }

    /// <summary>
    /// Verifies that Initialize throws when context is null.
    /// </summary>
    [Test]
    public void Initialize_NullContext_ThrowsArgumentNullException()
    {
        var controller = new AuxiliaryMemoryController();

        Assert.Throws<ArgumentNullException>(() => controller.Initialize(null!));
    }

    /// <summary>
    /// Verifies that Initialize sets all switches to their disabled state.
    /// </summary>
    [Test]
    public void Initialize_SetsAllSwitchesToDisabled()
    {
        var (controller, _, _) = CreateInitializedController();

        Assert.Multiple(() =>
        {
            Assert.That(controller.Is80StoreEnabled, Is.False, "80STORE should be disabled initially");
            Assert.That(controller.IsRamRdEnabled, Is.False, "RAMRD should be disabled initially");
            Assert.That(controller.IsRamWrtEnabled, Is.False, "RAMWRT should be disabled initially");
            Assert.That(controller.IsAltZpEnabled, Is.False, "ALTZP should be disabled initially");
            Assert.That(controller.IsPage2Selected, Is.False, "PAGE2 should be disabled initially");
            Assert.That(controller.IsHiResEnabled, Is.False, "HIRES should be disabled initially");
        });
    }

    /// <summary>
    /// Verifies that hi-res layers start inactive.
    /// </summary>
    [Test]
    public void Initialize_HiResLayersStartInactive()
    {
        var (_, bus, _) = CreateInitializedController();

        Assert.Multiple(() =>
        {
            Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.False, "AUX_HIRES1 should be inactive");
            Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.False, "AUX_HIRES2 should be inactive");
        });
    }

    /// <summary>
    /// Verifies that RegisterHandlers throws when dispatcher is null.
    /// </summary>
    [Test]
    public void RegisterHandlers_NullDispatcher_ThrowsArgumentNullException()
    {
        var controller = new AuxiliaryMemoryController();

        Assert.Throws<ArgumentNullException>(() => controller.RegisterHandlers(null!));
    }

    // ─── ALTZP Tests ($C008/$C009) ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that SETSTDZP ($C008) disables ALTZP.
    /// </summary>
    [Test]
    public void SetStdZp_DisablesAltZp()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // First enable ALTZP
        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP
        Assert.That(controller.IsAltZpEnabled, Is.True, "ALTZP should be enabled");

        // Now disable via SETSTDZP
        SimulateWrite(dispatcher, 0x08, 0x00); // SETSTDZP
        Assert.That(controller.IsAltZpEnabled, Is.False, "ALTZP should be disabled");
    }

    /// <summary>
    /// Verifies that SETALTZP ($C009) enables ALTZP.
    /// </summary>
    [Test]
    public void SetAltZp_EnablesAltZp()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP

        Assert.That(controller.IsAltZpEnabled, Is.True, "ALTZP should be enabled");
    }

    // ─── 80STORE + PAGE2 Tests ($C000/$C001, $C054/$C055) ───────────────────────

    /// <summary>
    /// Verifies that 80STOREOFF ($C000) disables 80STORE mode.
    /// </summary>
    [Test]
    public void Store80Off_Disables80StoreMode()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // Enable first
        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON
        Assert.That(controller.Is80StoreEnabled, Is.True);

        // Now disable
        SimulateWrite(dispatcher, 0x00, 0x00); // 80STOREOFF
        Assert.That(controller.Is80StoreEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that 80STOREON ($C001) enables 80STORE mode.
    /// </summary>
    [Test]
    public void Store80On_Enables80StoreMode()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON

        Assert.That(controller.Is80StoreEnabled, Is.True);
    }

    /// <summary>
    /// Verifies that PAGE1 ($C054) clears PAGE2 selection.
    /// </summary>
    [Test]
    public void Page1_ClearsPage2Selection()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // Enable PAGE2
        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2
        Assert.That(controller.IsPage2Selected, Is.True);

        // Select PAGE1
        SimulateWrite(dispatcher, 0x54, 0x00); // PAGE1
        Assert.That(controller.IsPage2Selected, Is.False);
    }

    /// <summary>
    /// Verifies that PAGE2 ($C055) sets PAGE2 selection.
    /// </summary>
    [Test]
    public void Page2_SetsPage2Selection()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2

        Assert.That(controller.IsPage2Selected, Is.True);
    }

    /// <summary>
    /// Verifies that PAGE1/PAGE2 reads also affect the switch state.
    /// </summary>
    [Test]
    public void PageSwitches_ReadAccessAlsoAffectsState()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // Read PAGE2 should enable it
        SimulateRead(dispatcher, 0x55); // PAGE2
        Assert.That(controller.IsPage2Selected, Is.True, "PAGE2 read should enable it");

        // Read PAGE1 should disable it
        SimulateRead(dispatcher, 0x54); // PAGE1
        Assert.That(controller.IsPage2Selected, Is.False, "PAGE1 read should disable PAGE2");
    }

    // ─── 80STORE + HIRES + PAGE2 Tests ($C056/$C057) ────────────────────────────

    /// <summary>
    /// Verifies that LORES ($C056) disables HIRES mode.
    /// </summary>
    [Test]
    public void LoRes_DisablesHiResMode()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // Enable HIRES first
        SimulateWrite(dispatcher, 0x57, 0x00); // HIRES
        Assert.That(controller.IsHiResEnabled, Is.True);

        // Now disable
        SimulateWrite(dispatcher, 0x56, 0x00); // LORES
        Assert.That(controller.IsHiResEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that HIRES ($C057) enables HIRES mode.
    /// </summary>
    [Test]
    public void HiRes_EnablesHiResMode()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        SimulateWrite(dispatcher, 0x57, 0x00); // HIRES

        Assert.That(controller.IsHiResEnabled, Is.True);
    }

    /// <summary>
    /// Verifies that hi-res page layers activate only when 80STORE + HIRES + PAGE2 are all enabled.
    /// </summary>
    [Test]
    public void HiResPageLayers_ActivateOnlyWhen80StoreHiResAndPage2Enabled()
    {
        var (_, bus, dispatcher) = CreateInitializedController();

        // Initially all off - hi-res layers should be inactive
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.False);
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.False);

        // Enable only 80STORE + HIRES (no PAGE2) - should still be inactive
        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON
        SimulateWrite(dispatcher, 0x57, 0x00); // HIRES
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.False);
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.False);

        // Enable PAGE2 - now should be active
        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.True);
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.True);

        // Disable HIRES - should be inactive
        SimulateWrite(dispatcher, 0x56, 0x00); // LORES
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.False);
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.False);
    }

    /// <summary>
    /// Verifies that HIRES/LORES reads also affect the switch state.
    /// </summary>
    [Test]
    public void HiresSwitches_ReadAccessAlsoAffectsState()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // Read HIRES should enable it
        SimulateRead(dispatcher, 0x57); // HIRES
        Assert.That(controller.IsHiResEnabled, Is.True, "HIRES read should enable it");

        // Read LORES should disable it
        SimulateRead(dispatcher, 0x56); // LORES
        Assert.That(controller.IsHiResEnabled, Is.False, "LORES read should disable HIRES");
    }

    // ─── RAMRD/RAMWRT Tests ($C002-$C005) ───────────────────────────────────────

    /// <summary>
    /// Verifies that RDMAINRAM ($C002) disables RAMRD.
    /// </summary>
    [Test]
    public void RdMainRam_DisablesRamRd()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // Enable first
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        Assert.That(controller.IsRamRdEnabled, Is.True);

        // Now disable
        SimulateWrite(dispatcher, 0x02, 0x00); // RDMAINRAM
        Assert.That(controller.IsRamRdEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that RDCARDRAM ($C003) enables RAMRD.
    /// </summary>
    [Test]
    public void RdCardRam_EnablesRamRd()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM

        Assert.That(controller.IsRamRdEnabled, Is.True);
    }

    /// <summary>
    /// Verifies that WRMAINRAM ($C004) disables RAMWRT.
    /// </summary>
    [Test]
    public void WrMainRam_DisablesRamWrt()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        // Enable first
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM
        Assert.That(controller.IsRamWrtEnabled, Is.True);

        // Now disable
        SimulateWrite(dispatcher, 0x04, 0x00); // WRMAINRAM
        Assert.That(controller.IsRamWrtEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that WRCARDRAM ($C005) enables RAMWRT.
    /// </summary>
    [Test]
    public void WrCardRam_EnablesRamWrt()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM

        Assert.That(controller.IsRamWrtEnabled, Is.True);
    }

    // ─── Reset Tests ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Reset restores all switches to their disabled state.
    /// </summary>
    [Test]
    public void Reset_RestoresPowerOnState()
    {
        var (controller, bus, dispatcher) = CreateInitializedController();

        // Enable everything
        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM
        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP
        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2
        SimulateWrite(dispatcher, 0x57, 0x00); // HIRES

        // Reset
        controller.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(controller.Is80StoreEnabled, Is.False, "80STORE should be disabled after reset");
            Assert.That(controller.IsRamRdEnabled, Is.False, "RAMRD should be disabled after reset");
            Assert.That(controller.IsRamWrtEnabled, Is.False, "RAMWRT should be disabled after reset");
            Assert.That(controller.IsAltZpEnabled, Is.False, "ALTZP should be disabled after reset");
            Assert.That(controller.IsPage2Selected, Is.False, "PAGE2 should be disabled after reset");
            Assert.That(controller.IsHiResEnabled, Is.False, "HIRES should be disabled after reset");
            Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.False);
            Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.False);
        });
    }

    // ─── Side-Effect-Free Access Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that side-effect-free writes do not change state.
    /// </summary>
    [Test]
    public void SideEffectFreeWrite_DoesNotChangeState()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        Assert.That(controller.Is80StoreEnabled, Is.False);

        SimulateSideEffectFreeWrite(dispatcher, 0x01, 0x00); // 80STOREON
        Assert.That(controller.Is80StoreEnabled, Is.False, "Side-effect-free write should not change state");
    }

    /// <summary>
    /// Verifies that side-effect-free reads do not change state.
    /// </summary>
    [Test]
    public void SideEffectFreeRead_DoesNotChangeState()
    {
        var (controller, _, dispatcher) = CreateInitializedController();

        Assert.That(controller.IsPage2Selected, Is.False);

        SimulateSideEffectFreeRead(dispatcher, 0x55); // PAGE2
        Assert.That(controller.IsPage2Selected, Is.False, "Side-effect-free read should not change state");
    }

    // ─── Layer Constant Tests ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that layer name constants have expected values.
    /// </summary>
    [Test]
    public void LayerNameConstants_HaveExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AuxiliaryMemoryController.LayerNameHiResPage1, Is.EqualTo("AUX_HIRES1"));
            Assert.That(AuxiliaryMemoryController.LayerNameHiResPage2, Is.EqualTo("AUX_HIRES2"));
        });
    }

    /// <summary>
    /// Verifies that layer priority is set correctly.
    /// </summary>
    [Test]
    public void LayerPriority_IsSetCorrectly()
    {
        Assert.That(AuxiliaryMemoryController.LayerPriority, Is.EqualTo(10));
    }

    // ─── Integration Tests with AuxiliaryMemoryPage0Target ──────────────────────

    /// <summary>
    /// Verifies zero page switching with ALTZP using the composite target.
    /// </summary>
    [Test]
    public void ZeroPageSwitching_WithCompositeTarget_EndToEnd()
    {
        var (_, bus, dispatcher) = CreateInitializedController();

        var readAccessZp = CreateTestAccess(0x0042, AccessIntent.DataRead);
        var writeAccessZp = CreateTestAccess(0x0042, AccessIntent.DataWrite);

        // Write to main zero page
        bus.Write8(writeAccessZp, 0xAA);
        Assert.That(bus.Read8(readAccessZp), Is.EqualTo(0xAA), "Write to main zero page should work");

        // Enable ALTZP - auxiliary zero page should now be visible
        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP
        Assert.That(bus.Read8(readAccessZp), Is.EqualTo(0x00), "Auxiliary zero page should be visible");

        // Write to auxiliary zero page
        bus.Write8(writeAccessZp, 0xBB);
        Assert.That(bus.Read8(readAccessZp), Is.EqualTo(0xBB), "Write to auxiliary zero page should work");

        // Switch back to main
        SimulateWrite(dispatcher, 0x08, 0x00); // SETSTDZP
        Assert.That(bus.Read8(readAccessZp), Is.EqualTo(0xAA), "Main zero page should retain value");
    }

    /// <summary>
    /// Verifies stack switching with ALTZP using the composite target.
    /// </summary>
    [Test]
    public void StackSwitching_WithCompositeTarget_EndToEnd()
    {
        var (_, bus, dispatcher) = CreateInitializedController();

        var readAccessStack = CreateTestAccess(0x0142, AccessIntent.DataRead);
        var writeAccessStack = CreateTestAccess(0x0142, AccessIntent.DataWrite);

        // Write to main stack
        bus.Write8(writeAccessStack, 0x11);
        Assert.That(bus.Read8(readAccessStack), Is.EqualTo(0x11), "Write to main stack should work");

        // Enable ALTZP
        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP
        Assert.That(bus.Read8(readAccessStack), Is.EqualTo(0x00), "Auxiliary stack should be visible");

        // Write to auxiliary stack
        bus.Write8(writeAccessStack, 0x22);
        Assert.That(bus.Read8(readAccessStack), Is.EqualTo(0x22), "Write to auxiliary stack should work");

        // Switch back to main
        SimulateWrite(dispatcher, 0x08, 0x00); // SETSTDZP
        Assert.That(bus.Read8(readAccessStack), Is.EqualTo(0x11), "Main stack should retain value");
    }

    /// <summary>
    /// Verifies text page switching with 80STORE and PAGE2 using the composite target.
    /// </summary>
    [Test]
    public void TextPageSwitching_WithCompositeTarget_EndToEnd()
    {
        var (_, bus, dispatcher) = CreateInitializedController();

        var readAccessText = CreateTestAccess(0x0500, AccessIntent.DataRead);
        var writeAccessText = CreateTestAccess(0x0500, AccessIntent.DataWrite);

        // Write to main text page
        bus.Write8(writeAccessText, 0x41);
        Assert.That(bus.Read8(readAccessText), Is.EqualTo(0x41), "Main text page write should work");

        // Enable 80STORE + PAGE2
        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON
        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2
        Assert.That(bus.Read8(readAccessText), Is.EqualTo(0x00), "Auxiliary text page should be visible");

        // Write to auxiliary text page
        bus.Write8(writeAccessText, 0x42);
        Assert.That(bus.Read8(readAccessText), Is.EqualTo(0x42), "Auxiliary text page write should work");

        // Switch to PAGE1
        SimulateWrite(dispatcher, 0x54, 0x00); // PAGE1
        Assert.That(bus.Read8(readAccessText), Is.EqualTo(0x41), "Main text page should be visible");
    }

    /// <summary>
    /// Verifies general memory switching with RAMRD/RAMWRT in page 0.
    /// </summary>
    [Test]
    public void GeneralMemoryPage0Switching_WithRAMRDRAMWRT_EndToEnd()
    {
        var (_, bus, dispatcher) = CreateInitializedControllerWithGeneralAux();

        // Address in general region within page 0 ($0200-$03FF)
        var readAccess = CreateTestAccess(0x0300, AccessIntent.DataRead);
        var writeAccess = CreateTestAccess(0x0300, AccessIntent.DataWrite);

        // Write to main memory
        bus.Write8(writeAccess, 0x55);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x55), "Write to main memory should work");

        // Enable RAMRD - auxiliary should now be visible for reads
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x00), "Auxiliary memory should be visible for reads");

        // Write still goes to main (RAMWRT not enabled)
        bus.Write8(writeAccess, 0x66);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x00), "Write should still go to main memory");

        // Enable RAMWRT - writes now go to auxiliary
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM
        bus.Write8(writeAccess, 0x77);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x77), "Write to auxiliary memory should work");

        // Disable RAMRD - should see main memory for reads
        SimulateWrite(dispatcher, 0x02, 0x00); // RDMAINRAM
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x66), "Main memory should be visible for reads");

        // Disable RAMWRT - writes now go to main
        SimulateWrite(dispatcher, 0x04, 0x00); // WRMAINRAM
        bus.Write8(writeAccess, 0x88);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x88), "Write to main memory should work");
    }

    /// <summary>
    /// Verifies general memory switching with RAMRD/RAMWRT for pages 1-11.
    /// </summary>
    [Test]
    public void GeneralMemoryPages1To11Switching_WithRAMRDRAMWRT_EndToEnd()
    {
        var (_, bus, dispatcher) = CreateInitializedControllerWithGeneralAux();

        // Address in general region in page 1 ($1000-$1FFF)
        var readAccess = CreateTestAccess(0x1500, AccessIntent.DataRead);
        var writeAccess = CreateTestAccess(0x1500, AccessIntent.DataWrite);

        // Write to main memory
        bus.Write8(writeAccess, 0x33);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x33), "Write to main memory should work");

        // Enable RAMRD - auxiliary should now be visible for reads
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x00), "Auxiliary memory should be visible for reads");

        // Enable RAMWRT - writes now go to auxiliary
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM
        bus.Write8(writeAccess, 0x44);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x44), "Write to auxiliary memory should work");

        // Disable both - back to main memory
        SimulateWrite(dispatcher, 0x02, 0x00); // RDMAINRAM
        SimulateWrite(dispatcher, 0x04, 0x00); // WRMAINRAM
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x33), "Main memory should retain original value");
    }

    // ─── Permutation Tests: Aux Memory + LC + Various Switch Combinations ───────

    /// <summary>
    /// Verifies that ALTZP works independently of RAMRD/RAMWRT state.
    /// </summary>
    [Test]
    public void Permutation_ALTZP_IndependentOfRAMRDRAMWRT()
    {
        var (_, bus, dispatcher) = CreateInitializedControllerWithGeneralAux();

        // Zero page address
        var zpReadAccess = CreateTestAccess(0x0042, AccessIntent.DataRead);
        var zpWriteAccess = CreateTestAccess(0x0042, AccessIntent.DataWrite);

        // General address (page 0, $0300 region)
        var generalReadAccess = CreateTestAccess(0x0300, AccessIntent.DataRead);
        var generalWriteAccess = CreateTestAccess(0x0300, AccessIntent.DataWrite);

        // Write initial values
        bus.Write8(zpWriteAccess, 0xAA);
        bus.Write8(generalWriteAccess, 0xBB);

        // Enable RAMRD/RAMWRT - should affect general region but not ZP
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM

        Assert.That(bus.Read8(zpReadAccess), Is.EqualTo(0xAA), "ZP should still be main (ALTZP off)");
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0x00), "General should be aux (RAMRD on)");

        // Now enable ALTZP - should affect ZP independently
        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP
        Assert.That(bus.Read8(zpReadAccess), Is.EqualTo(0x00), "ZP should be aux (ALTZP on)");
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0x00), "General should still be aux (RAMRD on)");

        // Disable RAMRD/RAMWRT - ZP should still be aux
        SimulateWrite(dispatcher, 0x02, 0x00); // RDMAINRAM
        SimulateWrite(dispatcher, 0x04, 0x00); // WRMAINRAM
        Assert.That(bus.Read8(zpReadAccess), Is.EqualTo(0x00), "ZP should still be aux (ALTZP still on)");
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0xBB), "General should be main (RAMRD off)");
    }

    /// <summary>
    /// Verifies that 80STORE+PAGE2 overrides RAMRD/RAMWRT for text page.
    /// </summary>
    [Test]
    public void Permutation_80STORE_PAGE2_OverridesRAMRDRAMWRT_ForTextPage()
    {
        var (_, bus, dispatcher) = CreateInitializedControllerWithGeneralAux();

        // Text page address
        var textReadAccess = CreateTestAccess(0x0500, AccessIntent.DataRead);
        var textWriteAccess = CreateTestAccess(0x0500, AccessIntent.DataWrite);

        // General address outside text page (page 0, $0300 region)
        var generalReadAccess = CreateTestAccess(0x0300, AccessIntent.DataRead);
        var generalWriteAccess = CreateTestAccess(0x0300, AccessIntent.DataWrite);

        // Write initial values
        bus.Write8(textWriteAccess, 0x41); // 'A' to main text
        bus.Write8(generalWriteAccess, 0x42); // 'B' to main general

        // Enable RAMRD/RAMWRT for general regions
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM

        // Text page should also be affected by RAMRD when 80STORE is off
        Assert.That(bus.Read8(textReadAccess), Is.EqualTo(0x00), "Text should be aux (RAMRD, 80STORE off)");
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0x00), "General should be aux (RAMRD on)");

        // Now enable 80STORE - text page should switch to PAGE2 control
        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON
        Assert.That(bus.Read8(textReadAccess), Is.EqualTo(0x41), "Text should be main (80STORE on, PAGE2 off)");
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0x00), "General should still be aux (RAMRD on)");

        // Enable PAGE2 - text page should go to aux
        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2
        Assert.That(bus.Read8(textReadAccess), Is.EqualTo(0x00), "Text should be aux (80STORE+PAGE2)");
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0x00), "General should still be aux (RAMRD on)");
    }

    /// <summary>
    /// Verifies that all aux memory switches can be toggled independently.
    /// </summary>
    [Test]
    public void Permutation_AllSwitchesIndependent()
    {
        var (controller, _, dispatcher) = CreateInitializedControllerWithGeneralAux();

        // Test all combinations of switches being on/off
        bool[] states = [false, true];

        foreach (bool altzp in states)
        {
            foreach (bool store80 in states)
            {
                foreach (bool ramrd in states)
                {
                    foreach (bool ramwrt in states)
                    {
                        // Set the state
                        SimulateWrite(dispatcher, (byte)(altzp ? 0x09 : 0x08), 0x00);
                        SimulateWrite(dispatcher, (byte)(store80 ? 0x01 : 0x00), 0x00);
                        SimulateWrite(dispatcher, (byte)(ramrd ? 0x03 : 0x02), 0x00);
                        SimulateWrite(dispatcher, (byte)(ramwrt ? 0x05 : 0x04), 0x00);

                        // Verify the state
                        Assert.Multiple(() =>
                        {
                            Assert.That(controller.IsAltZpEnabled, Is.EqualTo(altzp), $"ALTZP mismatch at {altzp},{store80},{ramrd},{ramwrt}");
                            Assert.That(controller.Is80StoreEnabled, Is.EqualTo(store80), $"80STORE mismatch at {altzp},{store80},{ramrd},{ramwrt}");
                            Assert.That(controller.IsRamRdEnabled, Is.EqualTo(ramrd), $"RAMRD mismatch at {altzp},{store80},{ramrd},{ramwrt}");
                            Assert.That(controller.IsRamWrtEnabled, Is.EqualTo(ramwrt), $"RAMWRT mismatch at {altzp},{store80},{ramrd},{ramwrt}");
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifies that hi-res layer activation respects the three-switch combination.
    /// </summary>
    [Test]
    public void Permutation_HiResLayerActivation_RequiresAllThreeSwitches()
    {
        var (_, bus, dispatcher) = CreateInitializedController();

        // Test all permutations of 80STORE, HIRES, PAGE2
        // Layer should only activate when ALL THREE are enabled
        var testCases = new[]
        {
            (store80: false, hires: false, page2: false, expected: false),
            (store80: true,  hires: false, page2: false, expected: false),
            (store80: false, hires: true,  page2: false, expected: false),
            (store80: false, hires: false, page2: true,  expected: false),
            (store80: true,  hires: true,  page2: false, expected: false),
            (store80: true,  hires: false, page2: true,  expected: false),
            (store80: false, hires: true,  page2: true,  expected: false),
            (store80: true,  hires: true,  page2: true,  expected: true),
        };

        foreach (var (store80, hires, page2, expected) in testCases)
        {
            // Set the switches
            SimulateWrite(dispatcher, (byte)(store80 ? 0x01 : 0x00), 0x00);
            SimulateWrite(dispatcher, (byte)(hires ? 0x57 : 0x56), 0x00);
            SimulateWrite(dispatcher, (byte)(page2 ? 0x55 : 0x54), 0x00);

            Assert.That(
                bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1),
                Is.EqualTo(expected),
                $"HiRes1 layer mismatch at 80STORE={store80}, HIRES={hires}, PAGE2={page2}");
            Assert.That(
                bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2),
                Is.EqualTo(expected),
                $"HiRes2 layer mismatch at 80STORE={store80}, HIRES={hires}, PAGE2={page2}");
        }
    }

    /// <summary>
    /// Verifies correct memory access patterns with asymmetric RAMRD/RAMWRT settings.
    /// </summary>
    [Test]
    public void Permutation_AsymmetricRAMRDRAMWRT_CorrectRouting()
    {
        var (_, bus, dispatcher) = CreateInitializedControllerWithGeneralAux();

        var readAccess = CreateTestAccess(0x0300, AccessIntent.DataRead);
        var writeAccess = CreateTestAccess(0x0300, AccessIntent.DataWrite);

        // Write initial value to main
        bus.Write8(writeAccess, 0x11);

        // Case 1: RAMRD off, RAMWRT on - reads from main, writes to aux
        SimulateWrite(dispatcher, 0x02, 0x00); // RDMAINRAM
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x11), "Should read from main");
        bus.Write8(writeAccess, 0x22);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x11), "Main should be unchanged, write went to aux");

        // Case 2: RAMRD on, RAMWRT off - reads from aux, writes to main
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        SimulateWrite(dispatcher, 0x04, 0x00); // WRMAINRAM
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x22), "Should read 0x22 from aux");
        bus.Write8(writeAccess, 0x33);
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x22), "Aux should be unchanged, write went to main");

        // Verify main has the new value
        SimulateWrite(dispatcher, 0x02, 0x00); // RDMAINRAM
        Assert.That(bus.Read8(readAccess), Is.EqualTo(0x33), "Main should have 0x33");
    }

    /// <summary>
    /// Verifies stack switching is independent of text page switching.
    /// </summary>
    [Test]
    public void Permutation_StackIndependentOfTextPage()
    {
        var (_, bus, dispatcher) = CreateInitializedController();

        var stackReadAccess = CreateTestAccess(0x0142, AccessIntent.DataRead);
        var stackWriteAccess = CreateTestAccess(0x0142, AccessIntent.DataWrite);
        var textReadAccess = CreateTestAccess(0x0500, AccessIntent.DataRead);
        var textWriteAccess = CreateTestAccess(0x0500, AccessIntent.DataWrite);

        // Write initial values
        bus.Write8(stackWriteAccess, 0xAA);
        bus.Write8(textWriteAccess, 0xBB);

        // Enable ALTZP - stack should switch, text should not
        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP
        Assert.That(bus.Read8(stackReadAccess), Is.EqualTo(0x00), "Stack should be aux (ALTZP on)");
        Assert.That(bus.Read8(textReadAccess), Is.EqualTo(0xBB), "Text should be main (80STORE off)");

        // Enable 80STORE + PAGE2 - text should switch, stack unchanged
        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON
        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2
        Assert.That(bus.Read8(stackReadAccess), Is.EqualTo(0x00), "Stack should still be aux (ALTZP on)");
        Assert.That(bus.Read8(textReadAccess), Is.EqualTo(0x00), "Text should be aux (80STORE+PAGE2)");

        // Disable ALTZP - stack back to main, text stays aux
        SimulateWrite(dispatcher, 0x08, 0x00); // SETSTDZP
        Assert.That(bus.Read8(stackReadAccess), Is.EqualTo(0xAA), "Stack should be main (ALTZP off)");
        Assert.That(bus.Read8(textReadAccess), Is.EqualTo(0x00), "Text should still be aux (80STORE+PAGE2)");
    }

    /// <summary>
    /// Verifies that reset restores all switches and layers to initial state.
    /// </summary>
    [Test]
    public void Permutation_Reset_RestoresAllSwitchesAndLayers()
    {
        var (controller, bus, dispatcher) = CreateInitializedControllerWithGeneralAux();

        // Enable everything
        SimulateWrite(dispatcher, 0x01, 0x00); // 80STOREON
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        SimulateWrite(dispatcher, 0x05, 0x00); // WRCARDRAM
        SimulateWrite(dispatcher, 0x09, 0x00); // SETALTZP
        SimulateWrite(dispatcher, 0x55, 0x00); // PAGE2
        SimulateWrite(dispatcher, 0x57, 0x00); // HIRES

        // Verify everything is enabled
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.True);
        Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.True);
        Assert.That(controller.IsAltZpEnabled, Is.True);
        Assert.That(controller.Is80StoreEnabled, Is.True);
        Assert.That(controller.IsRamRdEnabled, Is.True);
        Assert.That(controller.IsRamWrtEnabled, Is.True);
        Assert.That(controller.IsPage2Selected, Is.True);
        Assert.That(controller.IsHiResEnabled, Is.True);

        // Reset
        controller.Reset();

        // Verify everything is back to initial state
        Assert.Multiple(() =>
        {
            Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage1), Is.False, "HiRes1 should be inactive");
            Assert.That(bus.IsLayerActive(AuxiliaryMemoryController.LayerNameHiResPage2), Is.False, "HiRes2 should be inactive");
            Assert.That(controller.IsAltZpEnabled, Is.False, "ALTZP should be disabled");
            Assert.That(controller.Is80StoreEnabled, Is.False, "80STORE should be disabled");
            Assert.That(controller.IsRamRdEnabled, Is.False, "RAMRD should be disabled");
            Assert.That(controller.IsRamWrtEnabled, Is.False, "RAMWRT should be disabled");
            Assert.That(controller.IsPage2Selected, Is.False, "PAGE2 should be disabled");
            Assert.That(controller.IsHiResEnabled, Is.False, "HIRES should be disabled");
        });
    }

    /// <summary>
    /// Verifies rapid switch toggling maintains correct state.
    /// </summary>
    [Test]
    public void Permutation_RapidSwitchToggling_MaintainsCorrectState()
    {
        var (_, bus, dispatcher) = CreateInitializedControllerWithGeneralAux();

        var generalReadAccess = CreateTestAccess(0x0300, AccessIntent.DataRead);
        var generalWriteAccess = CreateTestAccess(0x0300, AccessIntent.DataWrite);

        // Write to main
        bus.Write8(generalWriteAccess, 0x11);

        // Rapid toggling
        for (int i = 0; i < 100; i++)
        {
            SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
            SimulateWrite(dispatcher, 0x02, 0x00); // RDMAINRAM
        }

        // Should end at main
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0x11), "Should read from main after toggling");

        // End with RAMRD on
        SimulateWrite(dispatcher, 0x03, 0x00); // RDCARDRAM
        Assert.That(bus.Read8(generalReadAccess), Is.EqualTo(0x00), "Should read from aux after final toggle");
    }

    // ─── Helper Methods ─────────────────────────────────────────────────────────

    private static (AuxiliaryMemoryController Controller, MainBus Bus, IOPageDispatcher Dispatcher) CreateInitializedController()
    {
        var bus = new MainBus();
        var dispatcher = new IOPageDispatcher();

        // Create main RAM for page 0 (4KB)
        var mainPage0Memory = new PhysicalMemory(0x1000, "MainPage0");
        var mainPage0Target = new RamTarget(mainPage0Memory.Slice(0, 0x1000));

        // Create auxiliary memory with exact sizes
        var auxZpMemory = new PhysicalMemory(0x0100, "AuxZP");
        var auxZpTarget = new RamTarget(auxZpMemory.Slice(0, 0x0100));

        var auxStackMemory = new PhysicalMemory(0x0100, "AuxStack");
        var auxStackTarget = new RamTarget(auxStackMemory.Slice(0, 0x0100));

        var auxTextMemory = new PhysicalMemory(0x0400, "AuxText");
        var auxTextTarget = new RamTarget(auxTextMemory.Slice(0, 0x0400));

        // Create controller first
        var controller = new AuxiliaryMemoryController();

        // Create composite target for page 0
        var page0Target = new AuxiliaryMemoryPage0Target(
            mainPage0Target,
            auxZpTarget,
            auxStackTarget,
            auxTextTarget,
            controller);

        // Map page 0 to composite target
        bus.MapPage(0, new PageEntry(
            DeviceId: 1,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.All,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            Target: page0Target,
            PhysicalBase: 0));

        // Create main RAM for pages 1-11
        var mainRam = new PhysicalMemory(0xB000, "MainRAM");
        var mainRamTarget = new RamTarget(mainRam.Slice(0, 0xB000));
        bus.MapPageRange(1, 11, 1, RegionTag.Ram, PagePerms.All, TargetCaps.SupportsPeek | TargetCaps.SupportsPoke, mainRamTarget, 0);
        bus.SaveBaseMappingRange(1, 11);

        // Create auxiliary hi-res memory (8KB each)
        var auxHires1Memory = new PhysicalMemory(0x2000, "AuxHiRes1");
        var auxHires1Target = new RamTarget(auxHires1Memory.Slice(0, 0x2000));

        var auxHires2Memory = new PhysicalMemory(0x2000, "AuxHiRes2");
        var auxHires2Target = new RamTarget(auxHires2Memory.Slice(0, 0x2000));

        // Create hi-res layers
        var auxHires1Layer = bus.CreateLayer(AuxiliaryMemoryController.LayerNameHiResPage1, AuxiliaryMemoryController.LayerPriority);
        var auxHires2Layer = bus.CreateLayer(AuxiliaryMemoryController.LayerNameHiResPage2, AuxiliaryMemoryController.LayerPriority);

        bus.AddLayeredMapping(new LayeredMapping(
            VirtualBase: 0x2000, Size: 0x2000, Layer: auxHires1Layer,
            DeviceId: 2, RegionTag: RegionTag.Ram, Perms: PagePerms.All,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            Target: auxHires1Target, PhysBase: 0));

        bus.AddLayeredMapping(new LayeredMapping(
            VirtualBase: 0x4000, Size: 0x2000, Layer: auxHires2Layer,
            DeviceId: 2, RegionTag: RegionTag.Ram, Perms: PagePerms.All,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            Target: auxHires2Target, PhysBase: 0));

        controller.RegisterHandlers(dispatcher);
        var context = CreateMockEventContext(bus);
        controller.Initialize(context);

        return (controller, bus, dispatcher);
    }

    /// <summary>
    /// Creates a fully initialized auxiliary memory controller with general auxiliary memory support.
    /// </summary>
    /// <returns>A tuple containing the initialized controller, the configured memory bus, and the I/O dispatcher.</returns>
    private static (AuxiliaryMemoryController Controller, MainBus Bus, IOPageDispatcher Dispatcher) CreateInitializedControllerWithGeneralAux()
    {
        var bus = new MainBus();
        var dispatcher = new IOPageDispatcher();

        // Create main RAM for page 0 (4KB)
        var mainPage0Memory = new PhysicalMemory(0x1000, "MainPage0");
        var mainPage0Target = new RamTarget(mainPage0Memory.Slice(0, 0x1000));

        // Create auxiliary memory with exact sizes
        var auxZpMemory = new PhysicalMemory(0x0100, "AuxZP");
        var auxZpTarget = new RamTarget(auxZpMemory.Slice(0, 0x0100));

        var auxStackMemory = new PhysicalMemory(0x0100, "AuxStack");
        var auxStackTarget = new RamTarget(auxStackMemory.Slice(0, 0x0100));

        var auxTextMemory = new PhysicalMemory(0x0400, "AuxText");
        var auxTextTarget = new RamTarget(auxTextMemory.Slice(0, 0x0400));

        // Create auxiliary general memory for page 0 (4KB for general regions)
        var auxGeneralPage0Memory = new PhysicalMemory(0x1000, "AuxGeneralPage0");
        var auxGeneralPage0Target = new RamTarget(auxGeneralPage0Memory.Slice(0, 0x1000));

        // Create controller first
        var controller = new AuxiliaryMemoryController();

        // Create composite target for page 0 with general aux support
        var page0Target = new AuxiliaryMemoryPage0Target(
            mainPage0Target,
            auxZpTarget,
            auxStackTarget,
            auxTextTarget,
            controller,
            auxGeneralPage0Target);

        // Map page 0 to composite target
        bus.MapPage(0, new PageEntry(
            DeviceId: 1,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.All,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            Target: page0Target,
            PhysicalBase: 0));

        // Create main RAM for pages 1-11 ($1000-$BFFF = 44KB)
        var mainRamPages1to11 = new PhysicalMemory(0xB000, "MainRAMPages1to11");
        var mainRamPages1to11Target = new RamTarget(mainRamPages1to11.Slice(0, 0xB000));

        // Create auxiliary RAM for pages 1-11 ($1000-$BFFF = 44KB)
        var auxRamPages1to11 = new PhysicalMemory(0xB000, "AuxRAMPages1to11");
        var auxRamPages1to11Target = new RamTarget(auxRamPages1to11.Slice(0, 0xB000));

        // Create a general target that handles RAMRD/RAMWRT switching for pages 1-11
        var generalTarget = new AuxiliaryMemoryGeneralTarget(
            mainRamPages1to11Target,
            auxRamPages1to11Target,
            controller,
            baseOffset: 0x1000);

        // Map pages 1-11 to the general switching target
        bus.MapPageRange(1, 11, 1, RegionTag.Ram, PagePerms.All,
            TargetCaps.SupportsPeek | TargetCaps.SupportsPoke, generalTarget, 0x1000);
        bus.SaveBaseMappingRange(1, 11);

        // Create auxiliary hi-res memory (8KB each)
        var auxHires1Memory = new PhysicalMemory(0x2000, "AuxHiRes1");
        var auxHires1Target = new RamTarget(auxHires1Memory.Slice(0, 0x2000));

        var auxHires2Memory = new PhysicalMemory(0x2000, "AuxHiRes2");
        var auxHires2Target = new RamTarget(auxHires2Memory.Slice(0, 0x2000));

        // Create hi-res layers
        var auxHires1Layer = bus.CreateLayer(AuxiliaryMemoryController.LayerNameHiResPage1, AuxiliaryMemoryController.LayerPriority);
        var auxHires2Layer = bus.CreateLayer(AuxiliaryMemoryController.LayerNameHiResPage2, AuxiliaryMemoryController.LayerPriority);

        bus.AddLayeredMapping(new LayeredMapping(
            VirtualBase: 0x2000, Size: 0x2000, Layer: auxHires1Layer,
            DeviceId: 2, RegionTag: RegionTag.Ram, Perms: PagePerms.All,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            Target: auxHires1Target, PhysBase: 0));

        bus.AddLayeredMapping(new LayeredMapping(
            VirtualBase: 0x4000, Size: 0x2000, Layer: auxHires2Layer,
            DeviceId: 2, RegionTag: RegionTag.Ram, Perms: PagePerms.All,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke,
            Target: auxHires2Target, PhysBase: 0));

        controller.RegisterHandlers(dispatcher);
        var context = CreateMockEventContext(bus);
        controller.Initialize(context);

        return (controller, bus, dispatcher);
    }

    private static IEventContext CreateMockEventContext(IMemoryBus bus)
    {
        var mockContext = new Mock<IEventContext>();
        mockContext.Setup(c => c.Bus).Returns(bus);
        return mockContext.Object;
    }

    private static byte SimulateRead(IOPageDispatcher dispatcher, byte offset)
    {
        var context = CreateTestAccess((uint)(0xC000 + offset), AccessIntent.DataRead);
        return dispatcher.Read(offset, in context);
    }

    private static byte SimulateSideEffectFreeRead(IOPageDispatcher dispatcher, byte offset)
    {
        var context = CreateTestAccess((uint)(0xC000 + offset), AccessIntent.DataRead, AccessFlags.NoSideEffects);
        return dispatcher.Read(offset, in context);
    }

    private static void SimulateWrite(IOPageDispatcher dispatcher, byte offset, byte value)
    {
        var context = CreateTestAccess((uint)(0xC000 + offset), AccessIntent.DataWrite);
        dispatcher.Write(offset, value, in context);
    }

    private static void SimulateSideEffectFreeWrite(IOPageDispatcher dispatcher, byte offset, byte value)
    {
        var context = CreateTestAccess((uint)(0xC000 + offset), AccessIntent.DataWrite, AccessFlags.NoSideEffects);
        dispatcher.Write(offset, value, in context);
    }

    private static BusAccess CreateTestAccess(Addr address, AccessIntent intent, AccessFlags flags = AccessFlags.None)
    {
        return new BusAccess(
            Address: address, Value: 0, WidthBits: 8, Mode: BusAccessMode.Decomposed,
            EmulationFlag: true, Intent: intent, SourceId: 0, Cycle: 0, Flags: flags);
    }
}