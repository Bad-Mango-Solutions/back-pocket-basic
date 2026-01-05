// <copyright file="TrapRegistryIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Emulation.Cpu;
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
/// <para>
/// Unlike unit tests that call TryExecute directly, these integration tests
/// execute real 65C02 machine code using the CPU emulator, verifying that
/// traps are triggered during actual instruction execution.
/// </para>
/// </remarks>
[TestFixture]
public class TrapRegistryIntegrationTests
{
    /// <summary>
    /// COUT routine address in Apple II ROM ($FDED).
    /// </summary>
    private const ushort CoutAddress = 0xFDED;

    /// <summary>
    /// HOME routine address in Apple II ROM ($FC58).
    /// </summary>
    private const ushort HomeAddress = 0xFC58;

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
    /// Integration test that validates memory context-aware trap selection
    /// by executing actual JSR instructions through the CPU emulator.
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
    /// <item><description>CPU performs automatic RTS after trap handler returns</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapRegistry_WithLanguageCardBankSwitch_SelectsCorrectTrap()
    {
        // ─── Arrange: Build machine with Language Card handlers installed ───────
        var romTrapInvoked = false;
        var lcRamTrapInvoked = false;
        var trapInvocationOrder = new List<string>();

        // Build the Pocket2e machine - Language Card soft switch handlers
        // are automatically registered during Build() via IMotherboardDevice
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
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

        // Attach trap registry to CPU
        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;

        machine.Reset();

        // ─── Arrange: Write test ML program ─────────────────────────────────────
        // The test program at $0300:
        // $0300: JSR $FDED    ; Call COUT (will trigger ROM trap)
        // $0303: DB           ; STP - halt after first JSR

        // Write JSR $FDED (opcode $20, low byte $ED, high byte $FD)
        machine.Cpu.Write8(TestProgramAddress, 0x20);     // JSR
        machine.Cpu.Write8(TestProgramAddress + 1, 0xED); // Low byte of $FDED
        machine.Cpu.Write8(TestProgramAddress + 2, 0xFD); // High byte of $FDED
        machine.Cpu.Write8(TestProgramAddress + 3, 0xDB); // STP - halt

        // Write a stub at COUT that just returns (in case trap doesn't fire)
        // This also serves as the ROM content that would execute if no trap
        machine.Cpu.Write8(CoutAddress, 0x60); // RTS at COUT

        // ─── Act 1: Execute with ROM visible (default state) ────────────────────
        // Set PC to our test program
        machine.Cpu.SetPC(TestProgramAddress);

        // Verify Language Card state is ROM visible (default)
        Assert.That(languageCard.IsRamReadEnabled, Is.False, "LC RAM should be disabled initially");

        // Execute the test program:
        // First Step(): PC=$0300, executes JSR $FDED. JSR pushes return address ($0302) and sets PC=$FDED
        // Second Step(): PC=$FDED, trap check fires before instruction execution, auto-RTS back to $0303
        machine.Step(); // Execute JSR - PC now at $FDED
        machine.Step(); // Trap fires at $FDED, auto-RTS

        // ─── Assert 1: ROM trap fired ───────────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(romTrapInvoked, Is.True, "ROM trap should have been invoked");
            Assert.That(lcRamTrapInvoked, Is.False, "LC RAM trap should NOT have been invoked yet");
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(1));
            Assert.That(trapInvocationOrder[0], Is.EqualTo("ROM"));

            // After trap + auto-RTS, PC should be at $0303 (next instruction after JSR)
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(TestProgramAddress + 3), "PC should be at instruction after JSR");
        });

        // ─── Act 2: Switch Language Card to RAM mode via soft switches ──────────
        // The soft switches at $C080-$C08F control Language Card state.
        // Reading $C083 twice enables RAM read/write with Bank 2 (R×2 protocol).
        // This triggers the Language Card through the IOPageDispatcher.
        machine.Cpu.Read8(LcBank2RamReadWrite);
        machine.Cpu.Read8(LcBank2RamReadWrite);

        // Verify Language Card is now in RAM mode
        Assert.That(languageCard.IsRamReadEnabled, Is.True, "LC RAM should be enabled after soft switch");

        // Reset flags for next test
        romTrapInvoked = false;
        lcRamTrapInvoked = false;

        // Set up another JSR $FDED at current PC location
        ushort program2Address = TestProgramAddress + 0x10; // $0310
        machine.Cpu.Write8(program2Address, 0x20);     // JSR
        machine.Cpu.Write8((ushort)(program2Address + 1), 0xED); // Low byte of $FDED
        machine.Cpu.Write8((ushort)(program2Address + 2), 0xFD); // High byte of $FDED
        machine.Cpu.Write8((ushort)(program2Address + 3), 0xDB); // STP - halt

        // ─── Act 3: Execute with LC RAM visible ─────────────────────────────────
        machine.Cpu.SetPC(program2Address);
        machine.Step(); // Execute JSR - PC now at $FDED
        machine.Step(); // LC RAM trap should fire, auto-RTS

        // ─── Assert 2: LC RAM trap fired ────────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(lcRamTrapInvoked, Is.True, "LC RAM trap should have been invoked");
            Assert.That(romTrapInvoked, Is.False, "ROM trap should NOT have been invoked");
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(2));
            Assert.That(trapInvocationOrder[1], Is.EqualTo("LC_RAM"));
        });

        // ─── Act 4: Switch back to ROM and verify ───────────────────────────────
        // Read $C081 to disable RAM read (ROM visible)
        machine.Cpu.Read8(LcRomRead);

        Assert.That(languageCard.IsRamReadEnabled, Is.False, "LC RAM should be disabled after soft switch");

        // Reset flags
        romTrapInvoked = false;
        lcRamTrapInvoked = false;

        // Set up third JSR
        ushort program3Address = TestProgramAddress + 0x20; // $0320
        machine.Cpu.Write8(program3Address, 0x20);     // JSR
        machine.Cpu.Write8((ushort)(program3Address + 1), 0xED); // Low byte of $FDED
        machine.Cpu.Write8((ushort)(program3Address + 2), 0xFD); // High byte of $FDED
        machine.Cpu.Write8((ushort)(program3Address + 3), 0xDB); // STP - halt

        machine.Cpu.SetPC(program3Address);
        machine.Step(); // Execute JSR - PC now at $FDED
        machine.Step(); // ROM trap should fire again, auto-RTS

        // ─── Assert 3: ROM trap fired again ─────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(romTrapInvoked, Is.True, "ROM trap should have been invoked again");
            Assert.That(lcRamTrapInvoked, Is.False, "LC RAM trap should NOT have been invoked");
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(3));
            Assert.That(trapInvocationOrder[2], Is.EqualTo("ROM"));
        });
    }

    /// <summary>
    /// Verifies that unregistered addresses execute normally without trap interception.
    /// </summary>
    [Test]
    public void TrapRegistry_UnregisteredAddress_ExecutesNormally()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        // Attach trap registry (empty - no traps registered)
        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;

        machine.Reset();

        // Write a JSR to HOME (not registered as a trap)
        machine.Cpu.Write8(TestProgramAddress, 0x20);     // JSR
        machine.Cpu.Write8(TestProgramAddress + 1, 0x58); // Low byte of $FC58 (HOME)
        machine.Cpu.Write8(TestProgramAddress + 2, 0xFC); // High byte of $FC58
        machine.Cpu.Write8(TestProgramAddress + 3, 0xDB); // STP - halt

        // Write RTS at HOME
        machine.Cpu.Write8(HomeAddress, 0x60); // RTS

        machine.Cpu.SetPC(TestProgramAddress);

        // Act - Execute JSR. Since no trap is registered, it should actually JSR to HOME
        machine.Step(); // JSR - jumps to HOME

        // Assert - PC should now be at HOME (no trap intercept)
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(HomeAddress), "PC should be at HOME - trap didn't intercept");
    }

    /// <summary>
    /// Verifies that disabled traps do not fire during CPU execution.
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

        // Attach trap registry to CPU
        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;

        machine.Reset();

        // Write JSR to COUT
        machine.Cpu.Write8(TestProgramAddress, 0x20);     // JSR
        machine.Cpu.Write8(TestProgramAddress + 1, 0xED); // Low byte of $FDED
        machine.Cpu.Write8(TestProgramAddress + 2, 0xFD); // High byte of $FDED
        machine.Cpu.Write8(TestProgramAddress + 3, 0xDB); // STP

        // Write RTS at COUT
        machine.Cpu.Write8(CoutAddress, 0x60); // RTS

        machine.Cpu.SetPC(TestProgramAddress);

        // Act - Execute JSR. Trap is disabled, so normal execution should occur
        machine.Step(); // JSR - should jump to COUT (trap disabled)

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvoked, Is.False, "Disabled trap handler should not be invoked");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(CoutAddress), "PC should be at COUT - disabled trap didn't intercept");
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
            HomeAddress,
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

        // Attach trap registry to CPU
        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;

        machine.Reset();

        // Write JSR to HOME
        machine.Cpu.Write8(TestProgramAddress, 0x20);     // JSR
        machine.Cpu.Write8(TestProgramAddress + 1, 0x58); // Low byte of HOME
        machine.Cpu.Write8(TestProgramAddress + 2, 0xFC); // High byte of HOME
        machine.Cpu.Write8(TestProgramAddress + 3, 0xDB); // STP

        // Write RTS at HOME and COUT
        machine.Cpu.Write8(HomeAddress, 0x60); // RTS
        machine.Cpu.Write8(CoutAddress, 0x60); // RTS

        machine.Cpu.SetPC(TestProgramAddress);

        // Act - Execute JSR to HOME (category disabled)
        machine.Step();

        // Assert - HOME trap should not have fired
        Assert.Multiple(() =>
        {
            Assert.That(homeInvoked, Is.False, "HOME handler should not be invoked (category disabled)");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(HomeAddress), "PC should be at HOME - category disabled");
        });

        // Now test COUT
        ushort program2Address = TestProgramAddress + 0x10;
        machine.Cpu.Write8(program2Address, 0x20);     // JSR
        machine.Cpu.Write8((ushort)(program2Address + 1), 0xED); // Low byte of COUT
        machine.Cpu.Write8((ushort)(program2Address + 2), 0xFD); // High byte of COUT
        machine.Cpu.Write8((ushort)(program2Address + 3), 0xDB); // STP

        machine.Cpu.SetPC(program2Address);
        machine.Step();

        Assert.Multiple(() =>
        {
            Assert.That(coutInvoked, Is.False, "COUT handler should not be invoked (category disabled)");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(CoutAddress), "PC should be at COUT - category disabled");
        });
    }

    /// <summary>
    /// Verifies that GetTrapsAtAddress returns all traps across memory contexts.
    /// </summary>
    /// <remarks>
    /// This is a query-only test that doesn't require CPU execution.
    /// </remarks>
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
    /// <remarks>
    /// This is a registration test that doesn't require CPU execution.
    /// </remarks>
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
        trapRegistry.Register(HomeAddress, "HOME_ROM", TrapCategory.MonitorRom, handler);

        Assert.That(trapRegistry.Count, Is.EqualTo(3), "Should have 3 traps initially");

        // Act - Remove all LC RAM traps
        trapRegistry.UnregisterContextTraps(MemoryContexts.LanguageCardRam);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapRegistry.Count, Is.EqualTo(2), "Should have 2 traps after removal");
            Assert.That(trapRegistry.HasTrap(CoutAddress), Is.True, "ROM COUT trap should still exist");
            Assert.That(trapRegistry.HasTrap(HomeAddress), Is.True, "ROM HOME trap should still exist");

            var coutTraps = trapRegistry.GetTrapsAtAddress(CoutAddress).ToList();
            Assert.That(coutTraps, Has.Count.EqualTo(1), "Should have 1 trap at COUT");
            Assert.That(coutTraps[0].Name, Is.EqualTo("COUT_ROM"));
        });
    }

    /// <summary>
    /// Verifies that the trap registry correctly handles custom memory contexts
    /// during actual CPU execution.
    /// </summary>
    [Test]
    public void TrapRegistry_CustomMemoryContext_FiresDuringExecution()
    {
        // Arrange
        var trapInvoked = false;
        const ushort ProdosMLIAddress = 0xBF00;

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

        // Register trap in custom context
        trapRegistry.RegisterWithContext(
            ProdosMLIAddress,
            customContext,
            "PRODOS_MLI",
            TrapCategory.OperatingSystem,
            (cpu, bus, ctx) =>
            {
                trapInvoked = true;
                return TrapResult.Success(new Cycle(10));
            });

        // Attach trap registry to CPU
        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;

        machine.Reset();

        // Write JSR to $BF00 (ProDOS MLI)
        machine.Cpu.Write8(TestProgramAddress, 0x20);     // JSR
        machine.Cpu.Write8(TestProgramAddress + 1, 0x00); // Low byte of $BF00
        machine.Cpu.Write8(TestProgramAddress + 2, 0xBF); // High byte of $BF00
        machine.Cpu.Write8(TestProgramAddress + 3, 0xDB); // STP

        // Write RTS at $BF00 (in case trap doesn't fire)
        machine.Cpu.Write8(ProdosMLIAddress, 0x60); // RTS

        machine.Cpu.SetPC(TestProgramAddress);

        // Act
        machine.Step(); // Execute JSR - PC now at $BF00
        machine.Step(); // Custom context trap should fire, auto-RTS

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvoked, Is.True, "Custom context trap should have been invoked");
            Assert.That(trapRegistry.GetRegisteredContexts(), Does.Contain(customContext));

            // After trap + auto-RTS, PC should be at the instruction after JSR
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(TestProgramAddress + 3), "PC should be at instruction after JSR");
        });
    }
}