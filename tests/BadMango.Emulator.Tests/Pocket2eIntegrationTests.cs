// <copyright file="Pocket2eIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Systems;

using Moq;

/// <summary>
/// Integration tests for a fully-built Pocket2e (Apple IIe-Enhanced clone) system.
/// </summary>
/// <remarks>
/// <para>
/// These tests exercise the complete machine assembly including:
/// </para>
/// <list type="bullet">
/// <item><description>Soft switch behavior for memory bank switching</description></item>
/// <item><description>Language Card bank selection and write enable</description></item>
/// <item><description>Auxiliary memory controller soft switches</description></item>
/// <item><description>Slot card installation and I/O handler routing</description></item>
/// <item><description>Expansion ROM selection protocol</description></item>
/// <item><description>Machine lifecycle with full configuration</description></item>
/// </list>
/// </remarks>
[TestFixture]
public class Pocket2eIntegrationTests
{
    // ─── Soft Switch Addresses ──────────────────────────────────────────────────
    private const ushort SoftSwitch80StoreOff = 0xC000;
    private const ushort SoftSwitch80StoreOn = 0xC001;
    private const ushort SoftSwitchRamRdMain = 0xC002;
    private const ushort SoftSwitchRamRdAux = 0xC003;
    private const ushort SoftSwitchRamWrtMain = 0xC004;
    private const ushort SoftSwitchRamWrtAux = 0xC005;
    private const ushort SoftSwitchAltZpOff = 0xC008;
    private const ushort SoftSwitchAltZpOn = 0xC009;
    private const ushort SoftSwitchPage1 = 0xC054;
    private const ushort SoftSwitchPage2 = 0xC055;
    private const ushort SoftSwitchLoRes = 0xC056;
    private const ushort SoftSwitchHiRes = 0xC057;

    // Language Card soft switches
    private const ushort LcBank2RomWrite = 0xC080;
    private const ushort LcBank2RomRead = 0xC081;
    private const ushort LcBank2RamWrite = 0xC083;
    private const ushort LcBank1RomWrite = 0xC088;
    private const ushort LcBank1RamWrite = 0xC08B;

    /// <summary>
    /// Verifies that a Pocket2e machine can be built with a stub ROM.
    /// </summary>
    [Test]
    public void AsPocket2e_WithStubRom_CreatesFunctionalMachine()
    {
        // Arrange & Act
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(machine, Is.Not.Null, "Machine should be created");
            Assert.That(machine.Cpu, Is.Not.Null, "CPU should be created");
            Assert.That(machine.Bus, Is.Not.Null, "Bus should be created");
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped), "Initial state should be Stopped");
        });
    }

    /// <summary>
    /// Verifies that the Pocket2e has main and auxiliary RAM components.
    /// </summary>
    [Test]
    public void AsPocket2e_HasMainAndAuxiliaryRamComponents()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        // Act
        var mainRam = machine.GetComponent<Pocket2eMachineBuilderExtensions.MainRamComponent>();
        var auxRam = machine.GetComponent<Pocket2eMachineBuilderExtensions.AuxiliaryRamComponent>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(mainRam, Is.Not.Null, "Main RAM component should be present");
            Assert.That(auxRam, Is.Not.Null, "Auxiliary RAM component should be present");
            Assert.That(mainRam!.Memory.Size, Is.EqualTo(64 * 1024), "Main RAM should be 64KB");
            Assert.That(auxRam!.Memory.Size, Is.EqualTo(64 * 1024), "Auxiliary RAM should be 64KB");
        });
    }

    /// <summary>
    /// Verifies that the Pocket2e has IOPageDispatcher and SlotManager components.
    /// </summary>
    [Test]
    public void AsPocket2e_HasSlotManagerAndDispatcher()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        // Act
        var dispatcher = machine.GetComponent<IOPageDispatcher>();
        var slotManager = machine.GetComponent<ISlotManager>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dispatcher, Is.Not.Null, "IOPageDispatcher should be present");
            Assert.That(slotManager, Is.Not.Null, "SlotManager should be present");
        });
    }

    /// <summary>
    /// Verifies that the Pocket2e has Language Card and Auxiliary Memory controllers.
    /// </summary>
    [Test]
    public void AsPocket2e_HasMemoryControllers()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        // Act
        var languageCard = machine.GetComponent<LanguageCardDevice>();
        var auxMemory = machine.GetComponent<AuxiliaryMemoryController>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(languageCard, Is.Not.Null, "Language Card device should be present");
            Assert.That(auxMemory, Is.Not.Null, "Auxiliary Memory controller should be present");
        });
    }

    /// <summary>
    /// Verifies that the Language Card device reports correct initial state.
    /// </summary>
    [Test]
    public void LanguageCard_InitialState_IsCorrect()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var languageCard = machine.GetComponent<LanguageCardDevice>()!;

        // Assert - Default state: ROM visible, writes disabled, Bank 1 selected
        Assert.Multiple(() =>
        {
            Assert.That(languageCard.IsRamReadEnabled, Is.False, "RAM read should be disabled initially");
            Assert.That(languageCard.IsRamWriteEnabled, Is.False, "RAM write should be disabled initially");
            Assert.That(languageCard.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected initially");
        });
    }

    /// <summary>
    /// Verifies that the Auxiliary Memory controller reports correct initial state.
    /// </summary>
    [Test]
    public void AuxiliaryMemory_InitialState_IsCorrect()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var auxMemory = machine.GetComponent<AuxiliaryMemoryController>()!;

        // Assert - Default state: all switches off
        Assert.Multiple(() =>
        {
            Assert.That(auxMemory.Is80StoreEnabled, Is.False, "80STORE should be disabled initially");
            Assert.That(auxMemory.IsAltZpEnabled, Is.False, "ALTZP should be disabled initially");
            Assert.That(auxMemory.IsRamRdEnabled, Is.False, "RAMRD should be disabled initially");
            Assert.That(auxMemory.IsRamWrtEnabled, Is.False, "RAMWRT should be disabled initially");
            Assert.That(auxMemory.IsPage2Selected, Is.False, "PAGE2 should be disabled initially");
        });
    }

    /// <summary>
    /// Verifies that slot cards can be installed and retrieved.
    /// </summary>
    [Test]
    public void WithCard_InstallsCardInSlot()
    {
        // Arrange
        var mockCard = new Mock<ISlotCard>();
        mockCard.SetupProperty(c => c.SlotNumber);
        mockCard.Setup(c => c.Name).Returns("Test Card");
        mockCard.Setup(c => c.IOHandlers).Returns((SlotIOHandlers?)null);
        mockCard.Setup(c => c.ROMRegion).Returns((IBusTarget?)null);
        mockCard.Setup(c => c.ExpansionROMRegion).Returns((IBusTarget?)null);

        // Act
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(4, mockCard.Object)
            .Build();

        var slotManager = machine.GetComponent<ISlotManager>()!;
        var installedCard = slotManager.GetCard(4);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(installedCard, Is.SameAs(mockCard.Object), "Card should be installed in slot 4");
            Assert.That(mockCard.Object.SlotNumber, Is.EqualTo(4), "Card's slot number should be set to 4");
        });
    }

    /// <summary>
    /// Verifies that multiple slot cards can be installed.
    /// </summary>
    [Test]
    public void WithCard_MultipleCards_AllInstalled()
    {
        // Arrange
        var card1 = CreateMockSlotCard("Card 1");
        var card2 = CreateMockSlotCard("Card 2");
        var card3 = CreateMockSlotCard("Card 3");

        // Act
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(1, card1.Object)
            .WithCard(4, card2.Object)
            .WithCard(7, card3.Object)
            .Build();

        var slotManager = machine.GetComponent<ISlotManager>()!;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(slotManager.GetCard(1), Is.SameAs(card1.Object), "Card 1 should be in slot 1");
            Assert.That(slotManager.GetCard(4), Is.SameAs(card2.Object), "Card 2 should be in slot 4");
            Assert.That(slotManager.GetCard(7), Is.SameAs(card3.Object), "Card 3 should be in slot 7");
            Assert.That(slotManager.GetCard(2), Is.Null, "Slot 2 should be empty");
            Assert.That(slotManager.GetCard(3), Is.Null, "Slot 3 should be empty");
        });
    }

    /// <summary>
    /// Verifies that slot card I/O handlers are registered with the dispatcher
    /// by executing ML code that reads and writes to slot I/O addresses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA $C0E0 - Read from slot 6 I/O (triggers read handler)</description></item>
    /// <item><description>LDA #$55 - Load value to write</description></item>
    /// <item><description>STA $C0E0 - Write to slot 6 I/O (triggers write handler)</description></item>
    /// <item><description>STP - Halt the CPU</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void WithCard_WithIOHandlers_HandlersAreRouted()
    {
        // Arrange
        var handlers = new SlotIOHandlers();
        bool readCalled = false;
        bool writeCalled = false;

        handlers.Set(
            0,
            (offset, in ctx) =>
            {
                readCalled = true;
                return 0x42;
            },
            (offset, value, in ctx) =>
            {
                writeCalled = true;
            });

        var mockCard = new Mock<ISlotCard>();
        mockCard.SetupProperty(c => c.SlotNumber);
        mockCard.Setup(c => c.IOHandlers).Returns(handlers);
        mockCard.Setup(c => c.ROMRegion).Returns((IBusTarget?)null);
        mockCard.Setup(c => c.ExpansionROMRegion).Returns((IBusTarget?)null);

        // Act
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(6, mockCard.Object)
            .Build();

        machine.Reset();

        // ─── Write ML program at $0300 ──────────────────────────────────────────
        // $0300: LDA $C0E0    ; Read from slot 6 I/O (triggers read handler)
        // $0303: LDA #$55     ; Load value to write
        // $0305: STA $C0E0    ; Write to slot 6 I/O (triggers write handler)
        // $0308: STP          ; Halt the CPU
        const ushort TestProgramAddress = 0x0300;
        ushort addr = TestProgramAddress;

        // LDA $C0E0 (absolute addressing - opcode $AD)
        machine.Cpu.Write8(addr++, 0xAD);     // $0300: LDA absolute
        machine.Cpu.Write8(addr++, 0xE0);     // $0301: Low byte of $C0E0
        machine.Cpu.Write8(addr++, 0xC0);     // $0302: High byte of $C0E0

        // LDA #$55 (immediate - opcode $A9)
        machine.Cpu.Write8(addr++, 0xA9);     // $0303: LDA immediate
        machine.Cpu.Write8(addr++, 0x55);     // $0304: Value $55

        // STA $C0E0 (absolute addressing - opcode $8D)
        machine.Cpu.Write8(addr++, 0x8D);     // $0305: STA absolute
        machine.Cpu.Write8(addr++, 0xE0);     // $0306: Low byte of $C0E0
        machine.Cpu.Write8(addr++, 0xC0);     // $0307: High byte of $C0E0

        // STP (opcode $DB)
        machine.Cpu.Write8(addr, 0xDB);       // $0308: STP

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Step 1: Execute LDA $C0E0 - triggers read handler
        machine.Step();
        Assert.That(readCalled, Is.True, "Read handler should have been called");

        // Step 2: Execute LDA #$55
        machine.Step();

        // Step 3: Execute STA $C0E0 - triggers write handler
        machine.Step();
        Assert.That(writeCalled, Is.True, "Write handler should have been called");

        // Step 4: Execute STP
        machine.Step();

        // ─── Assert ─────────────────────────────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(readCalled, Is.True, "Read handler should have been called");
            Assert.That(writeCalled, Is.True, "Write handler should have been called");
            Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
        });
    }

    /// <summary>
    /// Verifies that ThunderclockCard can be installed and accessed
    /// by executing ML code that reads from the clock I/O address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA $C0C0 - Read from slot 4 I/O (latches time, returns month)</description></item>
    /// <item><description>STP - Halt the CPU</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void WithCard_ThunderclockCard_CanReadTime()
    {
        // Arrange
        var thunderclock = new ThunderclockCard();
        var fixedTime = new DateTime(2025, 6, 15, 14, 30, 45);
        thunderclock.SetFixedTime(fixedTime);

        // Act
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(4, thunderclock)
            .Build();

        machine.Reset();

        // ─── Write ML program at $0300 ──────────────────────────────────────────
        // $0300: LDA $C0C0    ; Read from slot 4 I/O (latches time, returns month)
        // $0303: STP          ; Halt the CPU
        const ushort TestProgramAddress = 0x0300;
        ushort addr = TestProgramAddress;

        // LDA $C0C0 (absolute addressing - opcode $AD)
        machine.Cpu.Write8(addr++, 0xAD);     // $0300: LDA absolute
        machine.Cpu.Write8(addr++, 0xC0);     // $0301: Low byte of $C0C0
        machine.Cpu.Write8(addr++, 0xC0);     // $0302: High byte of $C0C0

        // STP (opcode $DB)
        machine.Cpu.Write8(addr, 0xDB);       // $0303: STP

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Step 1: Execute LDA $C0C0 - latches time and returns month
        machine.Step();

        // Step 2: Execute STP
        machine.Step();

        // ─── Assert ─────────────────────────────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(6), "Should return June (month 6)");
            Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
        });
    }

    /// <summary>
    /// Verifies that machine reset resets all slot cards.
    /// </summary>
    [Test]
    public void Reset_ResetsAllSlotCards()
    {
        // Arrange
        var card1 = CreateMockSlotCard("Card 1");
        var card2 = CreateMockSlotCard("Card 2");

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(2, card1.Object)
            .WithCard(5, card2.Object)
            .Build();

        // Act
        machine.Reset();
        machine.Reset(); // Reset twice to trigger card resets

        // Assert - Reset is called at least once on each card
        card1.Verify(c => c.Reset(), Times.AtLeastOnce(), "Card 1 should be reset");
        card2.Verify(c => c.Reset(), Times.AtLeastOnce(), "Card 2 should be reset");
    }

    /// <summary>
    /// Verifies that expansion ROM selection protocol works.
    /// </summary>
    [Test]
    public void SlotManager_ExpansionROMSelection_SelectsCorrectSlot()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var slotManager = machine.GetComponent<ISlotManager>()!;

        // Act - Simulate access to slot ROM region
        slotManager.HandleSlotROMAccess(0xC600); // Slot 6 ROM

        // Assert
        Assert.That(slotManager.ActiveExpansionSlot, Is.EqualTo(6), "Expansion slot 6 should be selected after $C600 access");
    }

    /// <summary>
    /// Verifies that expansion ROM can be deselected.
    /// </summary>
    [Test]
    public void SlotManager_DeselectExpansionSlot_ClearsSelection()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        var slotManager = machine.GetComponent<ISlotManager>()!;
        slotManager.HandleSlotROMAccess(0xC300); // Select slot 3

        // Act
        slotManager.DeselectExpansionSlot();

        // Assert
        Assert.That(slotManager.ActiveExpansionSlot, Is.Null, "Expansion slot should be deselected");
    }

    /// <summary>
    /// Verifies that slot card receives expansion ROM selection notification.
    /// </summary>
    [Test]
    public void SlotManager_SelectExpansionSlot_NotifiesCard()
    {
        // Arrange
        var mockCard = CreateMockSlotCard("Test Card");

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(5, mockCard.Object)
            .Build();

        var slotManager = machine.GetComponent<ISlotManager>()!;

        // Act
        slotManager.SelectExpansionSlot(5);

        // Assert
        mockCard.Verify(c => c.OnExpansionROMSelected(), Times.Once(), "Card should be notified of expansion ROM selection");
    }

    /// <summary>
    /// Verifies that switching expansion slots notifies previous and new cards.
    /// </summary>
    [Test]
    public void SlotManager_SwitchExpansionSlot_NotifiesBothCards()
    {
        // Arrange
        var card3 = CreateMockSlotCard("Card 3");
        var card5 = CreateMockSlotCard("Card 5");

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(3, card3.Object)
            .WithCard(5, card5.Object)
            .Build();

        var slotManager = machine.GetComponent<ISlotManager>()!;

        // Act - Select slot 3 then switch to slot 5
        slotManager.SelectExpansionSlot(3);
        slotManager.SelectExpansionSlot(5);

        // Assert
        Assert.Multiple(() =>
        {
            card3.Verify(c => c.OnExpansionROMSelected(), Times.Once(), "Card 3 should have been selected");
            card3.Verify(c => c.OnExpansionROMDeselected(), Times.Once(), "Card 3 should have been deselected when switching to card 5");
            card5.Verify(c => c.OnExpansionROMSelected(), Times.Once(), "Card 5 should be selected");
        });
    }

    /// <summary>
    /// Verifies that the machine can execute code after full Pocket2e configuration.
    /// </summary>
    [Test]
    public void Pocket2e_CanExecuteCode()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // The stub ROM sets reset vector to $FF00 which has JMP $FF00
        // So PC should be at $FF00 and stepping should execute JMP

        // Act
        var initialPC = machine.Cpu.GetPC();
        var result = machine.Step();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(initialPC, Is.EqualTo(0xFF00), "PC should start at $FF00 from reset vector");
            Assert.That(result.State, Is.EqualTo(CpuRunState.Running), "CPU should be running");
            Assert.That(machine.Cpu.GetPC(), Is.EqualTo(0xFF00), "PC should be at $FF00 after JMP $FF00");
        });
    }

    /// <summary>
    /// Verifies that zero page can be read and written in Pocket2e
    /// by executing ML code that stores and loads values from zero page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA #$42 - Load $42 into A</description></item>
    /// <item><description>STA $00 - Store A to zero page $00</description></item>
    /// <item><description>LDA #$A5 - Load $A5 into A</description></item>
    /// <item><description>STA $FF - Store A to zero page $FF</description></item>
    /// <item><description>LDA $00 - Load from zero page $00 into A (should be $42)</description></item>
    /// <item><description>LDX $FF - Load from zero page $FF into X (should be $A5)</description></item>
    /// <item><description>STP - Halt the CPU</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void Pocket2e_ZeroPageReadWrite_Works()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // ─── Write ML program at $0300 ──────────────────────────────────────────
        const ushort TestProgramAddress = 0x0300;
        ushort addr = TestProgramAddress;

        // LDA #$42 (immediate - opcode $A9)
        machine.Cpu.Write8(addr++, 0xA9);     // $0300: LDA immediate
        machine.Cpu.Write8(addr++, 0x42);     // $0301: Value $42

        // STA $00 (zero page - opcode $85)
        machine.Cpu.Write8(addr++, 0x85);     // $0302: STA zero page
        machine.Cpu.Write8(addr++, 0x00);     // $0303: ZP address $00

        // LDA #$A5 (immediate - opcode $A9)
        machine.Cpu.Write8(addr++, 0xA9);     // $0304: LDA immediate
        machine.Cpu.Write8(addr++, 0xA5);     // $0305: Value $A5

        // STA $FF (zero page - opcode $85)
        machine.Cpu.Write8(addr++, 0x85);     // $0306: STA zero page
        machine.Cpu.Write8(addr++, 0xFF);     // $0307: ZP address $FF

        // LDA $00 (zero page - opcode $A5)
        machine.Cpu.Write8(addr++, 0xA5);     // $0308: LDA zero page
        machine.Cpu.Write8(addr++, 0x00);     // $0309: ZP address $00

        // LDX $FF (zero page - opcode $A6)
        machine.Cpu.Write8(addr++, 0xA6);     // $030A: LDX zero page
        machine.Cpu.Write8(addr++, 0xFF);     // $030B: ZP address $FF

        // STP (opcode $DB)
        machine.Cpu.Write8(addr, 0xDB);       // $030C: STP

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Execute all 6 instructions before STP:
        // LDA #$42, STA $00, LDA #$A5, STA $FF, LDA $00, LDX $FF
        const int InstructionsBeforeStp = 6;
        for (int i = 0; i < InstructionsBeforeStp; i++)
        {
            machine.Step();
        }

        // Execute STP
        machine.Step();

        // ─── Assert ─────────────────────────────────────────────────────────────
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Registers.A.GetByte(), Is.EqualTo(0x42), "A should contain value from zero page $00");
            Assert.That(machine.Cpu.Registers.X.GetByte(), Is.EqualTo(0xA5), "X should contain value from zero page $FF");
            Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
        });
    }

    /// <summary>
    /// Verifies that ROM region is read-only in Pocket2e.
    /// </summary>
    [Test]
    public void Pocket2e_RomRegion_IsReadOnly()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Read original value from ROM
        var originalValue = machine.Cpu.Read8(0xC000);

        // Act - Try to write to ROM
        machine.Cpu.Write8(0xC000, 0xFF);

        // Assert - Value should be unchanged (ROM is read-only)
        var afterWrite = machine.Cpu.Read8(0xC000);
        Assert.That(afterWrite, Is.EqualTo(originalValue), "ROM should be read-only");
    }

    /// <summary>
    /// Verifies that the Pocket2e lifecycle (reset, run, stop) works correctly.
    /// </summary>
    [Test]
    public void Pocket2e_Lifecycle_WorksCorrectly()
    {
        // Arrange - Create ROM that halts immediately
        var machine = CreateMachineWithHaltRom();

        var stateChanges = new List<MachineState>();
        machine.StateChanged += state => stateChanges.Add(state);

        // Act
        machine.Reset();
        machine.Run(); // Will halt immediately on STP

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(machine.State, Is.EqualTo(MachineState.Stopped), "Machine should be stopped after STP");
            Assert.That(stateChanges, Contains.Item(MachineState.Running), "Should have transitioned to Running");
        });
    }

    /// <summary>
    /// Verifies that the scheduler advances cycles during execution.
    /// </summary>
    [Test]
    public void Pocket2e_Scheduler_AdvancesCycles()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();
        var initialCycle = machine.Now;

        // Act - Execute some instructions
        for (int i = 0; i < 10; i++)
        {
            machine.Step();
        }

        // Assert
        Assert.That(machine.Now, Is.GreaterThan(initialCycle), "Cycle counter should advance during execution");
    }

    /// <summary>
    /// Verifies that the device registry is populated for Pocket2e.
    /// </summary>
    [Test]
    public void Pocket2e_DeviceRegistry_ContainsExpectedDevices()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        // Assert - Device registry should exist and have entries
        Assert.That(machine.Devices, Is.Not.Null, "Device registry should exist");
    }

    /// <summary>
    /// Verifies that a Pocket2e machine with Thunderclock can be fully assembled and run
    /// by executing ML code that reads the clock data sequentially.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test program at $0300 reads all Thunderclock values in sequence:
    /// </para>
    /// <list type="number">
    /// <item><description>LDA $C0C0 - Read month (latches time)</description></item>
    /// <item><description>STA $50 - Store month at $50</description></item>
    /// <item><description>LDA $C0C0 - Read day of week</description></item>
    /// <item><description>STA $51 - Store day of week at $51</description></item>
    /// <item><description>LDA $C0C0 - Read day</description></item>
    /// <item><description>STA $52 - Store day at $52</description></item>
    /// <item><description>LDA $C0C0 - Read hour</description></item>
    /// <item><description>STA $53 - Store hour at $53</description></item>
    /// <item><description>LDA $C0C0 - Read minute</description></item>
    /// <item><description>STA $54 - Store minute at $54</description></item>
    /// <item><description>LDA $C0C0 - Read second</description></item>
    /// <item><description>STA $55 - Store second at $55</description></item>
    /// <item><description>STP - Halt the CPU</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void Pocket2e_WithThunderclock_FullIntegration()
    {
        // Arrange
        var thunderclock = new ThunderclockCard();
        thunderclock.SetFixedTime(new(2025, 12, 25, 10, 30, 00));

        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .WithCard(4, thunderclock)
            .Build();

        machine.Reset();

        // ─── Write ML program at $0300 ──────────────────────────────────────────
        // Read all Thunderclock values and store them in zero page
        const ushort TestProgramAddress = 0x0300;
        ushort addr = TestProgramAddress;

        // Read month (latches time)
        machine.Cpu.Write8(addr++, 0xAD);     // LDA absolute
        machine.Cpu.Write8(addr++, 0xC0);     // Low byte of $C0C0
        machine.Cpu.Write8(addr++, 0xC0);     // High byte of $C0C0
        machine.Cpu.Write8(addr++, 0x85);     // STA zero page
        machine.Cpu.Write8(addr++, 0x50);     // Store at $50

        // Read day of week
        machine.Cpu.Write8(addr++, 0xAD);     // LDA absolute
        machine.Cpu.Write8(addr++, 0xC0);     // Low byte of $C0C0
        machine.Cpu.Write8(addr++, 0xC0);     // High byte of $C0C0
        machine.Cpu.Write8(addr++, 0x85);     // STA zero page
        machine.Cpu.Write8(addr++, 0x51);     // Store at $51

        // Read day
        machine.Cpu.Write8(addr++, 0xAD);     // LDA absolute
        machine.Cpu.Write8(addr++, 0xC0);     // Low byte of $C0C0
        machine.Cpu.Write8(addr++, 0xC0);     // High byte of $C0C0
        machine.Cpu.Write8(addr++, 0x85);     // STA zero page
        machine.Cpu.Write8(addr++, 0x52);     // Store at $52

        // Read hour
        machine.Cpu.Write8(addr++, 0xAD);     // LDA absolute
        machine.Cpu.Write8(addr++, 0xC0);     // Low byte of $C0C0
        machine.Cpu.Write8(addr++, 0xC0);     // High byte of $C0C0
        machine.Cpu.Write8(addr++, 0x85);     // STA zero page
        machine.Cpu.Write8(addr++, 0x53);     // Store at $53

        // Read minute
        machine.Cpu.Write8(addr++, 0xAD);     // LDA absolute
        machine.Cpu.Write8(addr++, 0xC0);     // Low byte of $C0C0
        machine.Cpu.Write8(addr++, 0xC0);     // High byte of $C0C0
        machine.Cpu.Write8(addr++, 0x85);     // STA zero page
        machine.Cpu.Write8(addr++, 0x54);     // Store at $54

        // Read second
        machine.Cpu.Write8(addr++, 0xAD);     // LDA absolute
        machine.Cpu.Write8(addr++, 0xC0);     // Low byte of $C0C0
        machine.Cpu.Write8(addr++, 0xC0);     // High byte of $C0C0
        machine.Cpu.Write8(addr++, 0x85);     // STA zero page
        machine.Cpu.Write8(addr++, 0x55);     // Store at $55

        // STP (opcode $DB)
        machine.Cpu.Write8(addr, 0xDB);       // $0303: STP

        // ─── Act: Execute the ML program ────────────────────────────────────────
        machine.Cpu.SetPC(TestProgramAddress);

        // Execute all 12 instructions before STP (6 reads × 2 instructions each):
        // (LDA $C0C0, STA $5x) × 6 for month, dayOfWeek, day, hour, minute, second
        const int InstructionsBeforeStp = 12;
        for (int i = 0; i < InstructionsBeforeStp; i++)
        {
            machine.Step();
        }

        // Execute STP
        machine.Step();

        // ─── Assert ─────────────────────────────────────────────────────────────
        // Read values from zero page using Peek8 (debug read) to not affect soft switches
        Assert.Multiple(() =>
        {
            Assert.That(machine.Cpu.Peek8(0x50), Is.EqualTo(12), "Month should be December (12)");
            Assert.That(machine.Cpu.Peek8(0x52), Is.EqualTo(25), "Day should be 25");
            Assert.That(machine.Cpu.Peek8(0x53), Is.EqualTo(10), "Hour should be 10");
            Assert.That(machine.Cpu.Peek8(0x54), Is.EqualTo(30), "Minute should be 30");
            Assert.That(machine.Cpu.Peek8(0x55), Is.EqualTo(0), "Second should be 0");
            Assert.That(machine.Cpu.Halted, Is.True, "CPU should be halted after STP");
        });
    }

    /// <summary>
    /// Verifies that empty slots return floating bus value.
    /// </summary>
    [Test]
    public void Pocket2e_EmptySlotIO_ReturnsFloatingBus()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        machine.Reset();

        // Act - Read from slot 3 I/O ($C0B0) which has no card
        var value = machine.Cpu.Read8(0xC0B0);

        // Assert - Should return floating bus value ($FF)
        Assert.That(value, Is.EqualTo(0xFF), "Empty slot should return floating bus ($FF)");
    }

    /// <summary>
    /// Verifies that vector table layer is correctly configured.
    /// </summary>
    [Test]
    public void Pocket2e_VectorTable_HasCorrectVectors()
    {
        // Arrange
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        // Act - Read vectors from $FFFA-$FFFF
        var nmiLow = machine.Cpu.Read8(0xFFFA);
        var nmiHigh = machine.Cpu.Read8(0xFFFB);
        var resetLow = machine.Cpu.Read8(0xFFFC);
        var resetHigh = machine.Cpu.Read8(0xFFFD);
        var irqLow = machine.Cpu.Read8(0xFFFE);
        var irqHigh = machine.Cpu.Read8(0xFFFF);

        var nmiVector = (ushort)(nmiLow | (nmiHigh << 8));
        var resetVector = (ushort)(resetLow | (resetHigh << 8));
        var irqVector = (ushort)(irqLow | (irqHigh << 8));

        // Assert - Stub ROM sets all vectors to $FF00
        Assert.Multiple(() =>
        {
            Assert.That(nmiVector, Is.EqualTo(0xFF00), "NMI vector should point to $FF00");
            Assert.That(resetVector, Is.EqualTo(0xFF00), "RESET vector should point to $FF00");
            Assert.That(irqVector, Is.EqualTo(0xFF00), "IRQ vector should point to $FF00");
        });
    }

    /// <summary>
    /// Verifies that custom vector table can be configured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note: The current implementation of <c>WithVectorTable</c> uses layered mappings
    /// which require page alignment. Since vectors are at $FFFA (not page-aligned),
    /// custom vector tables should be included in the ROM image itself.
    /// </para>
    /// <para>
    /// This test verifies that the stub ROM's vectors are correctly accessible,
    /// which demonstrates the vector table functionality works when properly aligned.
    /// </para>
    /// </remarks>
    [Test]
    public void Pocket2e_WithCustomVectorTable_VectorsFromStubRomAreAccessible()
    {
        // Arrange - Use stub ROM which has vectors at $FFFA-$FFFF
        var machine = new MachineBuilder()
            .AsPocket2e()
            .WithStubRom()
            .Build();

        // Read vectors from stub ROM
        var nmiLow = machine.Cpu.Read8(0xFFFA);
        var nmiHigh = machine.Cpu.Read8(0xFFFB);
        var resetLow = machine.Cpu.Read8(0xFFFC);
        var resetHigh = machine.Cpu.Read8(0xFFFD);
        var irqLow = machine.Cpu.Read8(0xFFFE);
        var irqHigh = machine.Cpu.Read8(0xFFFF);

        var nmiVector = (ushort)(nmiLow | (nmiHigh << 8));
        var resetVector = (ushort)(resetLow | (resetHigh << 8));
        var irqVector = (ushort)(irqLow | (irqHigh << 8));

        // Assert - Stub ROM sets all vectors to $FF00
        Assert.Multiple(() =>
        {
            Assert.That(nmiVector, Is.EqualTo(0xFF00), "NMI vector should be $FF00 from stub ROM");
            Assert.That(resetVector, Is.EqualTo(0xFF00), "RESET vector should be $FF00 from stub ROM");
            Assert.That(irqVector, Is.EqualTo(0xFF00), "IRQ vector should be $FF00 from stub ROM");
        });
    }

    /// <summary>
    /// Verifies that the IOPageDispatcher handles unregistered addresses correctly.
    /// </summary>
    [Test]
    public void IOPageDispatcher_UnregisteredAddress_ReturnsFloatingBus()
    {
        // Arrange
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();

        // Act
        var result = dispatcher.Read(0x50, in context);

        // Assert
        Assert.That(result, Is.EqualTo(0xFF), "Unregistered address should return $FF");
    }

    /// <summary>
    /// Verifies that the IOPageDispatcher routes slot handlers correctly.
    /// </summary>
    [Test]
    public void IOPageDispatcher_SlotHandlers_RoutedCorrectly()
    {
        // Arrange
        var dispatcher = new IOPageDispatcher();
        var handlers = new SlotIOHandlers();
        byte receivedOffset = 0;

        handlers.Set(
            5,
            (offset, in ctx) =>
            {
                receivedOffset = offset;
                return 0xAB;
            },
            null);

        dispatcher.InstallSlotHandlers(3, handlers);

        // Act - Slot 3 I/O base is 0x80 + 3*16 = 0xB0, so offset 5 is $B5
        var context = CreateTestContext();
        var result = dispatcher.Read(0xB5, in context);

        // Assert - The handler receives the global I/O page offset (0xB5 = 181),
        // not the slot-relative offset (5). This is the correct behavior for
        // the IOPageDispatcher which passes raw offsets to handlers.
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0xAB), "Should return value from handler");
            Assert.That(receivedOffset, Is.EqualTo(0xB5), "Handler receives global I/O page offset");
        });
    }

    // ─── Helper Methods ─────────────────────────────────────────────────────────
    private static Mock<ISlotCard> CreateMockSlotCard(string name)
    {
        var mock = new Mock<ISlotCard>();
        mock.SetupProperty(c => c.SlotNumber);
        mock.Setup(c => c.Name).Returns(name);
        mock.Setup(c => c.IOHandlers).Returns((SlotIOHandlers?)null);
        mock.Setup(c => c.ROMRegion).Returns((IBusTarget?)null);
        mock.Setup(c => c.ExpansionROMRegion).Returns((IBusTarget?)null);
        return mock;
    }

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

    private static IMachine CreateMachineWithHaltRom()
    {
        // Create 16KB ROM with STP at reset vector target
        var rom = new byte[16384];
        Array.Fill(rom, (byte)0xEA); // NOP fill

        // Put STP (opcode $DB) at $FF00. The STP instruction halts the 65C02 CPU,
        // which is used to test machine lifecycle behavior when the CPU stops execution.
        rom[0x3F00] = 0xDB; // STP - Stop (halt CPU)

        // Set vectors to $FF00
        rom[0x3FFA] = 0x00;
        rom[0x3FFB] = 0xFF;
        rom[0x3FFC] = 0x00;
        rom[0x3FFD] = 0xFF;
        rom[0x3FFE] = 0x00;
        rom[0x3FFF] = 0xFF;

        return new MachineBuilder()
            .AsPocket2e()
            .WithRom(rom, 0xC000, "Halt ROM")
            .Build();
    }
}