// <copyright file="TrapRegistryIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Systems;

/// <summary>
/// Integration tests for the TrapRegistry with a complete Pocket2e system.
/// </summary>
/// <remarks>
/// <para>
/// These tests exercise the trap registry in a fully-built machine context,
/// validating that:
/// </para>
/// <list type="bullet">
/// <item><description>Traps fire correctly during actual CPU execution</description></item>
/// <item><description>Memory context switching (ROM vs LC RAM) selects the correct trap</description></item>
/// <item><description>Language Card state changes are properly detected by the trap registry</description></item>
/// </list>
/// </remarks>
[TestFixture]
public class TrapRegistryIntegrationTests
{
    /// <summary>
    /// COUT routine address in Apple II ROM ($FDED).
    /// </summary>
    private const ushort CoutAddress = 0xFDED;

    /// <summary>
    /// Language Card soft switch to enable RAM read/write with Bank 2 ($C083).
    /// Two consecutive reads enable write.
    /// </summary>
    private const ushort LcBank2RamReadWrite = 0xC083;

    /// <summary>
    /// Language Card soft switch to disable RAM read (ROM visible) ($C081).
    /// </summary>
    private const ushort LcRomRead = 0xC081;

    /// <summary>
    /// Start address for our test ML program in low memory.
    /// </summary>
    private const ushort TestProgramAddress = 0x0300;

    /// <summary>
    /// Integration test that validates memory context-aware trap selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test builds a complete Pocket2e system, then:
    /// </para>
    /// <list type="number">
    /// <item><description>Registers a ROM trap at COUT ($FDED)</description></item>
    /// <item><description>Registers a Language Card RAM trap at the same address</description></item>
    /// <item><description>Executes a JSR to COUT with ROM visible (ROM trap should fire)</description></item>
    /// <item><description>Switches Language Card to RAM mode via soft switches</description></item>
    /// <item><description>Executes another JSR to COUT (LC RAM trap should fire)</description></item>
    /// </list>
    /// <para>
    /// This validates end-to-end functionality including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>TrapRegistry correctly detects Language Card state</description></item>
    /// <item><description>Memory context resolution selects the correct trap</description></item>
    /// <item><description>Traps fire during actual CPU instruction execution</description></item>
    /// <item><description>Soft switches correctly toggle Language Card state</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapRegistry_WithLanguageCardBankSwitch_SelectsCorrectTrap()
    {
        // ─── Arrange: Build machine with Language Card handlers installed ───────
        var romTrapInvoked = false;
        var lcRamTrapInvoked = false;
        var trapInvocationOrder = new List<string>();

        // Build the Pocket2e machine with Language Card soft switch handlers installed
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .AfterDeviceInit(m =>
            {
                // Language Card now implements IMotherboardDevice, so we can call
                // RegisterHandlers directly to install its soft switches at slot 0
                var dispatcher = m.GetComponent<IOPageDispatcher>()!;
                var lc = m.GetComponent<LanguageCardController>()!;
                lc.RegisterHandlers(dispatcher);
            })
            .Build();

        // Get required components
        var languageCard = machine.GetComponent<LanguageCardController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;

        // Create trap registry with Language Card awareness
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        // Register ROM trap at COUT - fires when ROM is visible
        trapRegistry.Register(
            CoutAddress,
            "COUT_ROM",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                romTrapInvoked = true;
                trapInvocationOrder.Add("ROM");
                return TrapResult.Success(new Cycle(6));
            },
            "ROM COUT trap for testing");

        // Register LC RAM trap at the same address - fires when LC RAM is enabled
        trapRegistry.RegisterLanguageCardRam(
            CoutAddress,
            "COUT_LCRAM",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                lcRamTrapInvoked = true;
                trapInvocationOrder.Add("LC_RAM");
                return TrapResult.Success(new Cycle(6));
            },
            "LC RAM COUT trap for testing");

        machine.Reset();

        // ─── Arrange: Write test ML program ─────────────────────────────────────
        // The test program:
        // $0300: JSR $FDED    ; Call COUT (will trigger ROM trap)
        // $0303: RTS          ; Return (we'll check here and continue manually)

        // Write JSR $FDED (opcode $20, low byte $ED, high byte $FD)
        machine.Cpu.Write8(TestProgramAddress, 0x20);     // JSR
        machine.Cpu.Write8(TestProgramAddress + 1, 0xED); // Low byte of $FDED
        machine.Cpu.Write8(TestProgramAddress + 2, 0xFD); // High byte of $FDED
        machine.Cpu.Write8(TestProgramAddress + 3, 0x60); // RTS

        // Write a stub at COUT that just returns (in case trap doesn't fire)
        // This also serves as the ROM content that would execute if no trap
        machine.Cpu.Write8(CoutAddress, 0x60); // RTS at COUT

        // ─── Act 1: Execute with ROM visible (default state) ────────────────────
        // Set PC to our test program
        machine.Cpu.SetPC(TestProgramAddress);

        // Verify Language Card state is ROM visible (default)
        Assert.That(languageCard.IsRamReadEnabled, Is.False, "LC RAM should be disabled initially");

        // Execute trap check at COUT address - this simulates what CPU would do on JSR
        var trapResult = trapRegistry.TryExecute(
            CoutAddress,
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        // ─── Assert 1: ROM trap fired ───────────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(trapResult.Handled, Is.True, "ROM trap should have been handled");
            Assert.That(romTrapInvoked, Is.True, "ROM trap should have been invoked");
            Assert.That(lcRamTrapInvoked, Is.False, "LC RAM trap should NOT have been invoked yet");
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(1));
            Assert.That(trapInvocationOrder[0], Is.EqualTo("ROM"));
        });

        // ─── Act 2: Switch Language Card to RAM mode via soft switches ──────────
        // The soft switches at $C080-$C08F control Language Card state.
        // Reading $C083 twice enables RAM read/write with Bank 2 (R×2 protocol).
        // This triggers the Language Card through the IOPageDispatcher.
        machine.Cpu.Read8(LcBank2RamReadWrite);
        machine.Cpu.Read8(LcBank2RamReadWrite);

        // Verify Language Card is now in RAM mode
        Assert.That(languageCard.IsRamReadEnabled, Is.True, "LC RAM should be enabled after soft switch");

        // ─── Act 3: Execute with LC RAM visible ─────────────────────────────────
        // Now execute another trap check - should select LC RAM trap
        trapResult = trapRegistry.TryExecute(
            CoutAddress,
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        // ─── Assert 2: LC RAM trap fired ────────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(trapResult.Handled, Is.True, "LC RAM trap should have been handled");
            Assert.That(lcRamTrapInvoked, Is.True, "LC RAM trap should have been invoked");
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(2));
            Assert.That(trapInvocationOrder[1], Is.EqualTo("LC_RAM"));
        });

        // ─── Act 4: Switch back to ROM and verify ───────────────────────────────
        // Read $C081 to disable RAM read (ROM visible)
        machine.Cpu.Read8(LcRomRead);

        Assert.That(languageCard.IsRamReadEnabled, Is.False, "LC RAM should be disabled after soft switch");

        // Execute trap check again - should select ROM trap
        trapResult = trapRegistry.TryExecute(
            CoutAddress,
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        // ─── Assert 3: ROM trap fired again ─────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(trapResult.Handled, Is.True, "ROM trap should have been handled again");
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(3));
            Assert.That(trapInvocationOrder[2], Is.EqualTo("ROM"));
        });
    }

    /// <summary>
    /// Verifies that unregistered addresses do not trigger traps.
    /// </summary>
    [Test]
    public void TrapRegistry_UnregisteredAddress_ReturnsNotHandled()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        machine.Reset();

        // Act - Try to execute trap at unregistered address
        var result = trapRegistry.TryExecute(
            0xFC58, // HOME - not registered
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        // Assert
        Assert.That(result.Handled, Is.False, "Unregistered address should not be handled");
    }

    /// <summary>
    /// Verifies that disabled traps do not fire.
    /// </summary>
    [Test]
    public void TrapRegistry_DisabledTrap_DoesNotFire()
    {
        // Arrange
        var trapInvoked = false;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(
            CoutAddress,
            "COUT",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                trapInvoked = true;
                return TrapResult.Success(new Cycle(6));
            });

        // Disable the trap
        trapRegistry.SetEnabled(CoutAddress, enabled: false);

        machine.Reset();

        // Act
        var result = trapRegistry.TryExecute(
            CoutAddress,
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False, "Disabled trap should not be handled");
            Assert.That(trapInvoked, Is.False, "Disabled trap handler should not be invoked");
        });
    }

    /// <summary>
    /// Verifies that disabled categories prevent all traps in that category from firing.
    /// </summary>
    [Test]
    public void TrapRegistry_DisabledCategory_PreventsAllTrapsInCategory()
    {
        // Arrange
        var homeInvoked = false;
        var coutInvoked = false;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(
            0xFC58, // HOME
            "HOME",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                homeInvoked = true;
                return TrapResult.Success(new Cycle(6));
            });

        trapRegistry.Register(
            CoutAddress,
            "COUT",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                coutInvoked = true;
                return TrapResult.Success(new Cycle(6));
            });

        // Disable entire MonitorRom category
        trapRegistry.SetCategoryEnabled(TrapCategory.MonitorRom, enabled: false);

        machine.Reset();

        // Act
        var homeResult = trapRegistry.TryExecute(
            0xFC58,
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        var coutResult = trapRegistry.TryExecute(
            CoutAddress,
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(homeResult.Handled, Is.False, "HOME trap should not be handled");
            Assert.That(coutResult.Handled, Is.False, "COUT trap should not be handled");
            Assert.That(homeInvoked, Is.False, "HOME handler should not be invoked");
            Assert.That(coutInvoked, Is.False, "COUT handler should not be invoked");
        });
    }

    /// <summary>
    /// Verifies that GetTrapsAtAddress returns all traps across memory contexts.
    /// </summary>
    [Test]
    public void TrapRegistry_GetTrapsAtAddress_ReturnsAllContexts()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        trapRegistry.Register(CoutAddress, "COUT_ROM", TrapCategory.MonitorRom, handler);
        trapRegistry.RegisterLanguageCardRam(CoutAddress, "COUT_LC", TrapCategory.MonitorRom, handler);

        // Act
        var traps = trapRegistry.GetTrapsAtAddress(CoutAddress).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(traps, Has.Count.EqualTo(2));
            Assert.That(traps.Any(t => t.Name == "COUT_ROM"), Is.True, "Should contain ROM trap");
            Assert.That(traps.Any(t => t.Name == "COUT_LC"), Is.True, "Should contain LC RAM trap");
        });
    }

    /// <summary>
    /// Verifies that UnregisterContextTraps removes all traps in a specific memory context.
    /// </summary>
    [Test]
    public void TrapRegistry_UnregisterContextTraps_RemovesOnlyThatContext()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        TrapHandler handler = (cpu, bus, ctx) => TrapResult.NotHandled;

        trapRegistry.Register(CoutAddress, "COUT_ROM", TrapCategory.MonitorRom, handler);
        trapRegistry.RegisterLanguageCardRam(CoutAddress, "COUT_LC", TrapCategory.MonitorRom, handler);
        trapRegistry.Register(0xFC58, "HOME_ROM", TrapCategory.MonitorRom, handler);

        Assert.That(trapRegistry.Count, Is.EqualTo(3), "Should have 3 traps initially");

        // Act - Remove all LC RAM traps
        trapRegistry.UnregisterContextTraps(MemoryContexts.LanguageCardRam);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapRegistry.Count, Is.EqualTo(2), "Should have 2 traps after removal");
            Assert.That(trapRegistry.HasTrap(CoutAddress), Is.True, "ROM COUT trap should still exist");
            Assert.That(trapRegistry.HasTrap(0xFC58), Is.True, "ROM HOME trap should still exist");

            var coutTraps = trapRegistry.GetTrapsAtAddress(CoutAddress).ToList();
            Assert.That(coutTraps, Has.Count.EqualTo(1), "Should have 1 trap at COUT");
            Assert.That(coutTraps[0].Name, Is.EqualTo("COUT_ROM"));
        });
    }

    /// <summary>
    /// Verifies that the trap registry correctly handles custom memory contexts.
    /// </summary>
    [Test]
    public void TrapRegistry_CustomMemoryContext_CanBeRegisteredAndQueried()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var slotManager = machine.GetComponent<ISlotManager>()!;
        var customContext = MemoryContexts.Custom("PRODOS_RAM");

        // Create trap registry with custom context resolver
        var trapRegistry = new TrapRegistry(
            slotManager,
            languageCard: null,
            memoryContextResolver: addr =>
            {
                // For testing, always return custom context for addresses >= $BF00
                return addr >= 0xBF00 ? customContext : MemoryContexts.Rom;
            });

        TrapHandler handler = (cpu, bus, ctx) => TrapResult.Success(new Cycle(10));

        // Register trap in custom context
        trapRegistry.RegisterWithContext(
            0xBF00,
            customContext,
            "PRODOS_MLI",
            TrapCategory.OperatingSystem,
            handler);

        machine.Reset();

        // Act
        var result = trapRegistry.TryExecute(
            0xBF00,
            machine.Cpu,
            machine.Bus,
            machine.GetComponent<IEventContext>()!);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True, "Custom context trap should be handled");
            Assert.That(trapRegistry.GetRegisteredContexts(), Does.Contain(customContext));
        });
    }
}