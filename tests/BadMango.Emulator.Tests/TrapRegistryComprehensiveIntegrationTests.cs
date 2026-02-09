// <copyright file="TrapRegistryComprehensiveIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Systems;

/// <summary>
/// Comprehensive integration tests for the trap registry and trap handlers
/// using a fully-built Pocket2e machine instance with real CPU execution.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate that trap handlers are correctly invoked during CPU
/// execution by writing machine language test programs, registering traps, and
/// stepping through the code to verify handler invocation and side effects.
/// </para>
/// <para>
/// Test coverage includes:
/// </para>
/// <list type="bullet">
/// <item><description>Basic trap invocation on JSR and JMP targets</description></item>
/// <item><description>Trap handler receiving correct CPU/bus state</description></item>
/// <item><description>Multiple traps at different addresses firing sequentially</description></item>
/// <item><description>RTS, RTI, and None return methods</description></item>
/// <item><description>Language Card memory context switching</description></item>
/// <item><description>Auxiliary memory context switching via RAMRD soft switch</description></item>
/// <item><description>Trap enable/disable toggling</description></item>
/// <item><description>Trap handler modifying CPU register and memory state</description></item>
/// </list>
/// </remarks>
[TestFixture]
public class TrapRegistryComprehensiveIntegrationTests
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
    /// RDKEY routine address in Apple II ROM ($FD0C).
    /// </summary>
    private const ushort RdkeyAddress = 0xFD0C;

    /// <summary>
    /// Language Card soft switch to enable RAM read with Bank 2 ($C083).
    /// </summary>
    private const ushort LcBank2RamReadWrite = 0xC083;

    /// <summary>
    /// Language Card soft switch to disable RAM read (ROM visible) ($C081).
    /// </summary>
    private const ushort LcRomRead = 0xC081;

    /// <summary>
    /// Auxiliary memory RAMRD enable soft switch ($C003) - write-only.
    /// </summary>
    private const ushort RamRdOnAddress = 0xC003;

    /// <summary>
    /// Auxiliary memory RAMRD disable soft switch ($C002) - write-only.
    /// </summary>
    private const ushort RamRdOffAddress = 0xC002;

    /// <summary>
    /// Start address for test ML programs in low memory.
    /// </summary>
    private const ushort TestProgramAddress = 0x0300;

    /// <summary>
    /// Secondary subroutine address for test programs.
    /// </summary>
    private const ushort SubroutineAddress = 0x0400;

    /// <summary>
    /// Stack base address for the 65C02 ($0100).
    /// </summary>
    private const ushort StackBase = 0x0100;

    // ─── Basic Trap Invocation Tests ────────────────────────────────────────────

    /// <summary>
    /// Verifies that a basic trap handler fires when a JSR targets a trapped address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JSR $FDED — Call COUT (trap fires, auto-RTS back)</description></item>
    /// <item><description>$0303: STP — End of test</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_FiredOnJSR_InvokesHandlerAndReturns()
    {
        // Arrange
        var trapInvoked = false;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
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

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: JSR $FDED; STP
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0x20); // JSR
        machine.Cpu.Write8(addr++, 0xED); // low byte $FDED
        machine.Cpu.Write8(addr++, 0xFD); // high byte $FDED
        machine.Cpu.Write8(addr, 0xDB);   // STP

        // Write RTS stub at COUT in case trap doesn't fire
        machine.Cpu.Poke8(CoutAddress, 0x60);

        // Act
        machine.Cpu.SetPC(TestProgramAddress);
        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires at $FDED, auto-RTS

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvoked, Is.True, "Trap handler should have been invoked");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0303), "PC should return to instruction after JSR");
        });

        // Execute STP to confirm clean halt
        machine.Step();
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt after STP");
    }

    /// <summary>
    /// Verifies that the trap handler receives the correct CPU state including
    /// the program counter at the trapped address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: LDA #$42 — Load accumulator with test value</description></item>
    /// <item><description>$0302: JSR $FDED — Call COUT (trap inspects CPU state)</description></item>
    /// <item><description>$0305: STP — End of test</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_ReceivesCorrectCpuState_AccumulatorAndPC()
    {
        // Arrange
        byte capturedAccumulator = 0;
        Addr capturedPC = 0;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(
            CoutAddress,
            "COUT",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                capturedAccumulator = cpu.Registers.A.GetByte();
                capturedPC = cpu.GetPC();
                return TrapResult.Success(new Cycle(6));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: LDA #$42; JSR $FDED; STP
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0xA9); // LDA #imm
        machine.Cpu.Write8(addr++, 0x42); // #$42
        machine.Cpu.Write8(addr++, 0x20); // JSR
        machine.Cpu.Write8(addr++, 0xED); // low byte $FDED
        machine.Cpu.Write8(addr++, 0xFD); // high byte $FDED
        machine.Cpu.Write8(addr, 0xDB);   // STP

        machine.Cpu.Poke8(CoutAddress, 0x60); // RTS stub

        // Act
        machine.Cpu.SetPC(TestProgramAddress);
        machine.Step(); // LDA #$42
        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires at $FDED

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(capturedAccumulator, Is.EqualTo(0x42), "Trap should see accumulator value loaded by LDA");
            Assert.That(capturedPC, Is.EqualTo(CoutAddress), "Trap should see PC at the trapped address");
        });
    }

    /// <summary>
    /// Verifies that multiple traps at different addresses fire correctly
    /// during sequential execution of a single ML program.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JSR $FC58 — Call HOME (trap #1 fires)</description></item>
    /// <item><description>$0303: JSR $FDED — Call COUT (trap #2 fires)</description></item>
    /// <item><description>$0306: JSR $FD0C — Call RDKEY (trap #3 fires)</description></item>
    /// <item><description>$0309: STP — End of test</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void MultipleTraps_AtDifferentAddresses_AllFireInOrder()
    {
        // Arrange
        var invocationOrder = new List<string>();

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(HomeAddress, "HOME", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                invocationOrder.Add("HOME");
                return TrapResult.Success(new Cycle(6));
            });

        trapRegistry.Register(CoutAddress, "COUT", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                invocationOrder.Add("COUT");
                return TrapResult.Success(new Cycle(6));
            });

        trapRegistry.Register(RdkeyAddress, "RDKEY", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                invocationOrder.Add("RDKEY");
                return TrapResult.Success(new Cycle(6));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: JSR $FC58; JSR $FDED; JSR $FD0C; STP
        ushort addr = TestProgramAddress;

        // JSR $FC58 (HOME)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0x58);
        machine.Cpu.Write8(addr++, 0xFC);

        // JSR $FDED (COUT)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0xED);
        machine.Cpu.Write8(addr++, 0xFD);

        // JSR $FD0C (RDKEY)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0x0C);
        machine.Cpu.Write8(addr++, 0xFD);

        // STP
        machine.Cpu.Write8(addr, 0xDB);

        // Write RTS stubs at ROM addresses
        machine.Cpu.Poke8(HomeAddress, 0x60);
        machine.Cpu.Poke8(CoutAddress, 0x60);
        machine.Cpu.Poke8(RdkeyAddress, 0x60);

        // Act
        machine.Cpu.SetPC(TestProgramAddress);

        // Execute: JSR HOME → trap fires → return
        machine.Step(); // JSR $FC58
        machine.Step(); // Trap fires

        // Execute: JSR COUT → trap fires → return
        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires

        // Execute: JSR RDKEY → trap fires → return
        machine.Step(); // JSR $FD0C
        machine.Step(); // Trap fires

        // STP
        machine.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(invocationOrder, Has.Count.EqualTo(3), "All three traps should fire");
            Assert.That(invocationOrder[0], Is.EqualTo("HOME"), "HOME should fire first");
            Assert.That(invocationOrder[1], Is.EqualTo("COUT"), "COUT should fire second");
            Assert.That(invocationOrder[2], Is.EqualTo("RDKEY"), "RDKEY should fire third");
            Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt after STP");
        });
    }

    // ─── Return Method Tests ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a trap returning <see cref="TrapReturnMethod.None"/> with a
    /// specified <see cref="TrapResult.ReturnAddress"/> redirects execution to
    /// the specified address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JMP $FDED — Jump to COUT (trap fires, redirects to $0400)</description></item>
    /// <item><description>$0400: STP — Halt (landed here via redirect)</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_WithReturnMethodNone_RedirectsToSpecifiedAddress()
    {
        // Arrange
        var trapInvoked = false;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(
            CoutAddress,
            "COUT",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                trapInvoked = true;
                return TrapResult.SuccessWithRedirect(new Cycle(10), SubroutineAddress);
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: JMP $FDED (trap redirects to $0400 where STP waits)
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0x4C); // JMP
        machine.Cpu.Write8(addr++, 0xED); // low byte $FDED
        machine.Cpu.Write8(addr, 0xFD);   // high byte $FDED

        // STP at redirect target
        machine.Cpu.Write8(SubroutineAddress, 0xDB);

        // Act
        machine.Cpu.SetPC(TestProgramAddress);
        machine.Step(); // JMP $FDED
        machine.Step(); // Trap fires, redirects to $0400

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvoked, Is.True, "Trap handler should have fired");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(SubroutineAddress),
                "PC should be at redirect target address");
        });

        // Execute STP at redirect target
        machine.Step();
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt at redirect target");
    }

    /// <summary>
    /// Verifies that a trap returning <see cref="TrapReturnMethod.Rti"/> correctly
    /// pulls the processor status and return address from the stack, simulating
    /// an interrupt handler return.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test simulates an interrupt-style trap by manually setting up the
    /// stack as if an interrupt had pushed status and return address, then
    /// returning via RTI.
    /// </para>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JMP $FDED — Jump to trap (handler pushes interrupt frame, returns via RTI)</description></item>
    /// <item><description>$0305: STP — Halt (RTI returns here)</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_WithRtiReturnMethod_RestoresStatusAndReturns()
    {
        // Arrange
        var trapInvoked = false;
        const ushort rtiReturnAddress = 0x0305;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        // The trap handler pushes an interrupt frame onto the stack:
        // status byte, then return address (low, high) - as the CPU would during an interrupt.
        // Then it returns with RTI, which pulls status, then PC from the stack.
        trapRegistry.Register(
            CoutAddress,
            "INT_HANDLER",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                trapInvoked = true;

                // Push return address high byte, low byte, and status onto stack
                // (same order as hardware interrupt push: PCH, PCL, P)
                // RTI pulls in reverse: P, PCL, PCH
                var pushAddr1 = cpu.PushByte(StackBase);
                cpu.Write8(pushAddr1, (byte)(rtiReturnAddress >> 8)); // PCH = $03
                var pushAddr2 = cpu.PushByte(StackBase);
                cpu.Write8(pushAddr2, (byte)(rtiReturnAddress & 0xFF)); // PCL = $05
                var pushAddr3 = cpu.PushByte(StackBase);
                cpu.Write8(pushAddr3, (byte)(ProcessorStatusFlags.R | ProcessorStatusFlags.B));

                return TrapResult.SuccessInterrupt(new Cycle(10));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: JMP $FDED; NOP; NOP; STP (STP at $0305)
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0x4C); // JMP
        machine.Cpu.Write8(addr++, 0xED); // low byte $FDED
        machine.Cpu.Write8(addr++, 0xFD); // high byte $FDED
        machine.Cpu.Write8(addr++, 0xEA); // NOP (filler at $0303)
        machine.Cpu.Write8(addr++, 0xEA); // NOP (filler at $0304)
        machine.Cpu.Write8(addr, 0xDB);   // STP at $0305

        // Act
        machine.Cpu.SetPC(TestProgramAddress);
        machine.Step(); // JMP $FDED
        machine.Step(); // Trap fires, RTI pulls status + PC from stack

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvoked, Is.True, "Trap handler should have fired");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(rtiReturnAddress),
                "PC should be at RTI return address after trap");
        });

        machine.Step(); // STP
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt at RTI return target");
    }

    /// <summary>
    /// Verifies that a trap fires on linear code flow (JMP) rather than JSR,
    /// and that using <see cref="TrapReturnMethod.None"/> without a return address
    /// continues at the current PC.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JMP $FDED — Jump to COUT (trap fires, handler sets PC)</description></item>
    /// <item><description>$0303: STP — Halt (handler redirects here)</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_OnJMP_FiresWithNoneReturnMethod()
    {
        // Arrange
        var trapInvoked = false;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        const ushort continueAddress = 0x0303;

        trapRegistry.Register(
            CoutAddress,
            "COUT",
            TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                trapInvoked = true;
                // Handler explicitly sets PC to where we want to continue
                cpu.SetPC(continueAddress);
                return TrapResult.Success(new Cycle(6), TrapReturnMethod.None);
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: JMP $FDED; STP (at $0303)
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0x4C); // JMP
        machine.Cpu.Write8(addr++, 0xED); // low byte $FDED
        machine.Cpu.Write8(addr++, 0xFD); // high byte $FDED
        machine.Cpu.Write8(addr, 0xDB);   // STP at $0303

        // Act
        machine.Cpu.SetPC(TestProgramAddress);
        machine.Step(); // JMP $FDED
        machine.Step(); // Trap fires, handler sets PC to $0303

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvoked, Is.True, "Trap should fire on JMP target");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(continueAddress),
                "PC should be at continue address set by handler");
        });

        machine.Step(); // STP
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt after STP");
    }

    // ─── Memory Context Tests ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Language Card RAM switching causes the trap registry to select
    /// the correct trap handler based on the active memory context. Tests both
    /// directions: ROM → LC RAM → ROM.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JSR $FDED — COUT (ROM trap fires)</description></item>
    /// <item><description>$0303: LDA $C083 — Enable LC RAM read</description></item>
    /// <item><description>$0306: JSR $FDED — COUT (LC RAM trap fires)</description></item>
    /// <item><description>$0309: LDA $C081 — Disable LC RAM read (back to ROM)</description></item>
    /// <item><description>$030C: JSR $FDED — COUT (ROM trap fires again)</description></item>
    /// <item><description>$030F: STP — End</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void LanguageCard_SwitchRomToLcRamAndBack_SelectsCorrectTrap()
    {
        // Arrange
        var trapInvocationOrder = new List<string>();

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(CoutAddress, "COUT_ROM", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                trapInvocationOrder.Add("ROM");
                return TrapResult.Success(new Cycle(6));
            });

        trapRegistry.RegisterLanguageCardRam(CoutAddress, "COUT_LCRAM", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                trapInvocationOrder.Add("LC_RAM");
                return TrapResult.Success(new Cycle(6));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program
        ushort addr = TestProgramAddress;

        // JSR $FDED (ROM context - first call)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0xED);
        machine.Cpu.Write8(addr++, 0xFD);

        // LDA $C083 (enable LC RAM read)
        machine.Cpu.Write8(addr++, 0xAD);
        machine.Cpu.Write8(addr++, 0x83);
        machine.Cpu.Write8(addr++, 0xC0);

        // JSR $FDED (LC RAM context)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0xED);
        machine.Cpu.Write8(addr++, 0xFD);

        // LDA $C081 (disable LC RAM read, back to ROM)
        machine.Cpu.Write8(addr++, 0xAD);
        machine.Cpu.Write8(addr++, 0x81);
        machine.Cpu.Write8(addr++, 0xC0);

        // JSR $FDED (ROM context - second call)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0xED);
        machine.Cpu.Write8(addr++, 0xFD);

        // STP
        machine.Cpu.Write8(addr, 0xDB);

        // Write RTS stub at COUT
        machine.Cpu.Poke8(CoutAddress, 0x60);

        // Act
        machine.Cpu.SetPC(TestProgramAddress);

        // Phase 1: ROM context
        Assert.That(languageCard.IsRamReadEnabled, Is.False, "Should start with ROM visible");
        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires (ROM)
        Assert.That(trapInvocationOrder, Has.Count.EqualTo(1));
        Assert.That(trapInvocationOrder[0], Is.EqualTo("ROM"), "First call should use ROM trap");

        // Phase 2: Switch to LC RAM
        machine.Step(); // LDA $C083
        Assert.That(languageCard.IsRamReadEnabled, Is.True, "LC RAM should be enabled");

        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires (LC RAM)
        Assert.That(trapInvocationOrder, Has.Count.EqualTo(2));
        Assert.That(trapInvocationOrder[1], Is.EqualTo("LC_RAM"), "Second call should use LC RAM trap");

        // Phase 3: Switch back to ROM
        machine.Step(); // LDA $C081
        Assert.That(languageCard.IsRamReadEnabled, Is.False, "LC RAM should be disabled again");

        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires (ROM again)

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(3), "Three trap invocations total");
            Assert.That(trapInvocationOrder[2], Is.EqualTo("ROM"), "Third call should use ROM trap again");
        });

        machine.Step(); // STP
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt after STP");
    }

    /// <summary>
    /// Verifies that auxiliary memory RAMRD switching interacts with the trap registry
    /// through a custom memory context resolver. When RAMRD is enabled, traps
    /// registered for the auxiliary RAM context fire instead of ROM traps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The auxiliary memory controller switches the $0200-$BFFF region. For trap
    /// testing purposes, we use a custom memory context resolver that checks
    /// the aux controller's RAMRD state.
    /// </para>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JSR $0400 — Call subroutine (main RAM trap fires)</description></item>
    /// <item><description>$0303: STA $C003 — Enable RAMRD (switch to aux RAM)</description></item>
    /// <item><description>$0306: JSR $0400 — Call subroutine (aux RAM trap fires)</description></item>
    /// <item><description>$0309: STA $C002 — Disable RAMRD (back to main RAM)</description></item>
    /// <item><description>$030C: STP — End</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void AuxiliaryMemory_RamRdSwitch_CustomContextSelectsCorrectTrap()
    {
        // Arrange
        var trapInvocationOrder = new List<string>();

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var auxController = machine.GetComponent<AuxiliaryMemoryController>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;

        var auxContext = MemoryContexts.AuxiliaryRam;

        // Create trap registry with custom memory context resolver
        // that uses the aux controller's RAMRD state
        var trapRegistry = new TrapRegistry(
            slotManager,
            languageCard: null,
            memoryContextResolver: address =>
            {
                // For addresses in the $0200-$BFFF range, check RAMRD state
                if (address >= 0x0200 && address < 0xC000 && auxController.IsRamRdEnabled)
                {
                    return auxContext;
                }

                return MemoryContexts.Rom;
            });

        // Register main RAM trap (ROM context - default)
        trapRegistry.Register(SubroutineAddress, "SUB_MAIN", TrapCategory.UserDefined,
            (cpu, bus, ctx) =>
            {
                trapInvocationOrder.Add("MAIN");
                return TrapResult.Success(new Cycle(6));
            });

        // Register aux RAM trap (auxiliary context)
        trapRegistry.RegisterWithContext(
            SubroutineAddress,
            auxContext,
            "SUB_AUX",
            TrapCategory.UserDefined,
            (cpu, bus, ctx) =>
            {
                trapInvocationOrder.Add("AUX");
                return TrapResult.Success(new Cycle(6));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program
        ushort addr = TestProgramAddress;

        // JSR $0400 (main RAM context)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0x00);
        machine.Cpu.Write8(addr++, 0x04);

        // STA $C003 (enable RAMRD - write triggers switch)
        machine.Cpu.Write8(addr++, 0x8D);
        machine.Cpu.Write8(addr++, 0x03);
        machine.Cpu.Write8(addr++, 0xC0);

        // JSR $0400 (aux RAM context)
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0x00);
        machine.Cpu.Write8(addr++, 0x04);

        // STA $C002 (disable RAMRD)
        machine.Cpu.Write8(addr++, 0x8D);
        machine.Cpu.Write8(addr++, 0x02);
        machine.Cpu.Write8(addr++, 0xC0);

        // STP
        machine.Cpu.Write8(addr, 0xDB);

        // Write RTS at subroutine address (in case trap doesn't fire)
        machine.Cpu.Write8(SubroutineAddress, 0x60);

        // Act
        machine.Cpu.SetPC(TestProgramAddress);

        // Phase 1: Main RAM context
        Assert.That(auxController.IsRamRdEnabled, Is.False, "RAMRD should be disabled initially");
        machine.Step(); // JSR $0400
        machine.Step(); // Trap fires (main RAM context)
        Assert.That(trapInvocationOrder[0], Is.EqualTo("MAIN"), "First call should use main trap");

        // Phase 2: Switch to aux RAM
        machine.Step(); // STA $C003 - enable RAMRD
        Assert.That(auxController.IsRamRdEnabled, Is.True, "RAMRD should be enabled after write to $C003");

        machine.Step(); // JSR $0400
        machine.Step(); // Trap fires (aux context)

        // Phase 3: Switch back to main
        machine.Step(); // STA $C002 - disable RAMRD

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(trapInvocationOrder, Has.Count.EqualTo(2), "Two trap invocations total");
            Assert.That(trapInvocationOrder[0], Is.EqualTo("MAIN"), "First call should use main trap");
            Assert.That(trapInvocationOrder[1], Is.EqualTo("AUX"), "Second call should use aux trap");
            Assert.That(auxController.IsRamRdEnabled, Is.False, "RAMRD should be disabled after STA $C002");
        });

        machine.Step(); // STP
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt after STP");
    }

    // ─── Enable/Disable Tests ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that re-enabling a previously disabled trap causes it to fire again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program (executed twice):
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JSR $FDED — Call COUT (first run: trap disabled, second: enabled)</description></item>
    /// <item><description>$0303: STP — End</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_ReenabledAfterDisable_FiresAgain()
    {
        // Arrange
        var trapInvokeCount = 0;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(CoutAddress, "COUT", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                trapInvokeCount++;
                return TrapResult.Success(new Cycle(6));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: JSR $FDED; STP
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0x20);
        machine.Cpu.Write8(addr++, 0xED);
        machine.Cpu.Write8(addr++, 0xFD);
        machine.Cpu.Write8(addr, 0xDB);
        machine.Cpu.Poke8(CoutAddress, 0x60);

        // Run 1: Trap disabled - should NOT fire
        trapRegistry.SetEnabled(CoutAddress, enabled: false);
        machine.Cpu.SetPC(TestProgramAddress);
        cpu.HaltReason = Core.Cpu.HaltState.None;
        machine.Step(); // JSR $FDED
        machine.Step(); // Execute RTS at $FDED (trap disabled)
        Assert.That(trapInvokeCount, Is.EqualTo(0), "Trap should not fire when disabled");

        // Run 2: Re-enable trap - should fire
        trapRegistry.SetEnabled(CoutAddress, enabled: true);
        machine.Cpu.SetPC(TestProgramAddress);
        cpu.HaltReason = Core.Cpu.HaltState.None;
        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires at $FDED

        // Assert
        Assert.That(trapInvokeCount, Is.EqualTo(1), "Trap should fire after re-enabling");
        Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0303), "PC should return after JSR");
    }

    // ─── Register Modification Tests ────────────────────────────────────────────

    /// <summary>
    /// Verifies that a trap handler can modify CPU registers and those
    /// changes persist after the trap completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: LDA #$00 — Clear accumulator</description></item>
    /// <item><description>$0302: JSR $FDED — Call COUT (trap sets A=$42, X=$10)</description></item>
    /// <item><description>$0305: STP — Halt (A and X should have trap-set values)</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_ModifiesRegisters_ChangesPersistAfterTrap()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(CoutAddress, "COUT", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                // Modify registers in the trap handler
                cpu.Registers.A.SetByte(0x42);
                cpu.Registers.X.SetByte(0x10);
                return TrapResult.Success(new Cycle(6));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Write test program: LDA #$00; JSR $FDED; STP
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0xA9); // LDA #imm
        machine.Cpu.Write8(addr++, 0x00); // #$00
        machine.Cpu.Write8(addr++, 0x20); // JSR
        machine.Cpu.Write8(addr++, 0xED); // low byte $FDED
        machine.Cpu.Write8(addr++, 0xFD); // high byte $FDED
        machine.Cpu.Write8(addr, 0xDB);   // STP

        machine.Cpu.Poke8(CoutAddress, 0x60);

        // Act
        machine.Cpu.SetPC(TestProgramAddress);
        machine.Step(); // LDA #$00
        Assert.That(cpu.Registers.A.GetByte(), Is.EqualTo(0x00), "A should be 0 after LDA #$00");

        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires, sets A=$42, X=$10, auto-RTS

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cpu.Registers.A.GetByte(), Is.EqualTo(0x42),
                "Accumulator should have trap-set value $42");
            Assert.That(cpu.Registers.X.GetByte(), Is.EqualTo(0x10),
                "X register should have trap-set value $10");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0x0305),
                "PC should return to instruction after JSR");
        });

        machine.Step(); // STP
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt after STP");
    }

    /// <summary>
    /// Verifies that a trap handler can write to memory through the bus
    /// and the writes are visible to subsequent CPU instructions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Test program:
    /// </para>
    /// <list type="number">
    /// <item><description>$0300: JSR $FDED — Call COUT (trap writes $AA to $0500)</description></item>
    /// <item><description>$0303: LDA $0500 — Load the value written by the trap</description></item>
    /// <item><description>$0306: STP — Halt (A should be $AA)</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void TrapHandler_WritesToMemory_VisibleToSubsequentInstructions()
    {
        // Arrange
        const ushort dataAddress = 0x0500;

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;
        var slotManager = machine.GetComponent<ISlotManager>()!;
        var trapRegistry = new TrapRegistry(slotManager, languageCard);

        trapRegistry.Register(CoutAddress, "COUT", TrapCategory.MonitorRom,
            (cpu, bus, ctx) =>
            {
                // Write $AA to $0500 via the bus
                cpu.Write8(dataAddress, 0xAA);
                return TrapResult.Success(new Cycle(6));
            });

        var cpu = (Cpu65C02)machine.Cpu;
        cpu.TrapRegistry = trapRegistry;
        machine.Reset();

        // Initialize target memory to $00
        machine.Cpu.Write8(dataAddress, 0x00);

        // Write test program: JSR $FDED; LDA $0500; STP
        ushort addr = TestProgramAddress;
        machine.Cpu.Write8(addr++, 0x20); // JSR
        machine.Cpu.Write8(addr++, 0xED); // low byte $FDED
        machine.Cpu.Write8(addr++, 0xFD); // high byte $FDED
        machine.Cpu.Write8(addr++, 0xAD); // LDA absolute
        machine.Cpu.Write8(addr++, 0x00); // low byte $0500
        machine.Cpu.Write8(addr++, 0x05); // high byte $0500
        machine.Cpu.Write8(addr, 0xDB);   // STP

        machine.Cpu.Poke8(CoutAddress, 0x60);

        // Act
        machine.Cpu.SetPC(TestProgramAddress);
        machine.Step(); // JSR $FDED
        machine.Step(); // Trap fires, writes $AA to $0500, auto-RTS
        machine.Step(); // LDA $0500

        // Assert
        Assert.That(cpu.Registers.A.GetByte(), Is.EqualTo(0xAA),
            "Accumulator should contain value written by trap handler");

        machine.Step(); // STP
        Assert.That(machine.Cpu.Halted, Is.True, "CPU should halt after STP");
    }
}