// <copyright file="Pocket2eMachineBuilderExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Systems;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Emulation.Cpu;

/// <summary>
/// Extension methods for configuring <see cref="MachineBuilder"/> as a Pocket2e system.
/// </summary>
/// <remarks>
/// <para>
/// Pocket2e is an Apple IIe-Enhanced clone configuration using a 65C02 CPU with
/// a 128KB address space (64KB main + 64KB auxiliary) and standard IIe memory layout.
/// </para>
/// <para>
/// The standard configuration includes:
/// <list type="bullet">
/// <item><description>65C02 CPU at 1.023 MHz (emulated)</description></item>
/// <item><description>128KB RAM (64KB main + 64KB auxiliary)</description></item>
/// <item><description>Language Card (16KB RAM bank at $D000-$FFFF)</description></item>
/// <item><description>Auxiliary memory controller for 80-column and double hi-res</description></item>
/// <item><description>Standard IIe ROM vectors at $FFFA-$FFFF</description></item>
/// <item><description>High-priority vector table layer for NMI, RESET, and IRQ/BRK</description></item>
/// <item><description>Slot manager for expansion cards (slots 1-7)</description></item>
/// <item><description>Built-in 80-column support</description></item>
/// </list>
/// </para>
/// </remarks>
public static class Pocket2eMachineBuilderExtensions
{
    /// <summary>
    /// The standard IIe base RAM size (48KB).
    /// </summary>
    public const uint BaseRamSize = 48 * 1024;

    /// <summary>
    /// The standard IIe main RAM size (64KB).
    /// </summary>
    public const uint MainRamSize = 64 * 1024;

    /// <summary>
    /// The standard IIe auxiliary RAM size (64KB).
    /// </summary>
    public const uint AuxRamSize = 64 * 1024;

    /// <summary>
    /// The standard IIe full RAM size including auxiliary memory (128KB).
    /// </summary>
    public const uint FullRamSize = 128 * 1024;

    /// <summary>
    /// The vector table layer name.
    /// </summary>
    public const string VectorTableLayerName = "VectorTable";

    /// <summary>
    /// The vector table layer priority (high to override ROM).
    /// </summary>
    public const int VectorTableLayerPriority = 1000;

    /// <summary>
    /// Address of the NMI vector ($FFFA).
    /// </summary>
    public const Addr NmiVectorAddress = 0xFFFA;

    /// <summary>
    /// Address of the RESET vector ($FFFC).
    /// </summary>
    public const Addr ResetVectorAddress = 0xFFFC;

    /// <summary>
    /// Address of the IRQ/BRK vector ($FFFE).
    /// </summary>
    public const Addr IrqVectorAddress = 0xFFFE;

    /// <summary>
    /// The size of the 6502 vector table (6 bytes).
    /// </summary>
    private const uint VectorTableSize = 6;

    /// <summary>
    /// Sets up a known good basic Pocket2e (Apple IIe-Enhanced clone) configuration.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This configures a complete Apple IIe-Enhanced compatible system with:
    /// <list type="bullet">
    /// <item><description>16-bit address space (64KB visible, 128KB total)</description></item>
    /// <item><description>65C02 CPU</description></item>
    /// <item><description>128KB RAM (64KB main + 64KB auxiliary)</description></item>
    /// <item><description>Language Card (16KB RAM bank at $D000-$FFFF)</description></item>
    /// <item><description>Auxiliary memory controller</description></item>
    /// <item><description>I/O page with soft switches</description></item>
    /// <item><description>Slot manager for expansion cards</description></item>
    /// <item><description>High-priority vector table layer for the 6502 vectors</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling this method, you should add ROM images using
    /// <see cref="MachineBuilder.WithRom(RomDescriptor)"/> or
    /// <see cref="MachineBuilder.WithRom(byte[], uint, string?)"/>.
    /// </para>
    /// </remarks>
    public static MachineBuilder AsPocket2e(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        return builder
            .WithAddressSpace(16)
            .WithCpu(CpuFamily.Cpu65C02)
            .WithCpuFactory(CreatePocket2eCpu)
            .WithPocket2eMemoryLayout()
            .WithLanguageCard()
            .WithAuxiliaryMemory()
            .WithSlotManager()
            .WithPocket2eIOPage()
            .CreateLayer(VectorTableLayerName, VectorTableLayerPriority);
    }

    /// <summary>
    /// Configures the base Pocket2e memory layout with 128KB RAM.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This configures 128KB total RAM:
    /// <list type="bullet">
    /// <item><description>64KB main RAM mapped at $0000-$FFFF</description></item>
    /// <item><description>64KB auxiliary RAM for bank switching</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The auxiliary RAM is not directly visible but is accessed through the
    /// auxiliary memory controller soft switches. Both RAM banks are added as
    /// components (<see cref="MainRamComponent"/> and <see cref="AuxiliaryRamComponent"/>)
    /// for retrieval by controllers.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithPocket2eMemoryLayout(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Create 64KB main RAM and add as component
        var mainRam = new PhysicalMemory(MainRamSize, "MainRAM");
        var mainRamTarget = new RamTarget(mainRam.Slice(0, MainRamSize));
        builder.AddComponent(new MainRamComponent(mainRam, mainRamTarget));

        // Create 64KB auxiliary RAM and add as component
        var auxRam = new PhysicalMemory(AuxRamSize, "AuxRAM");
        var auxRamTarget = new RamTarget(auxRam.Slice(0, AuxRamSize));
        builder.AddComponent(new AuxiliaryRamComponent(auxRam, auxRamTarget));

        return builder.ConfigureMemory((bus, devices) =>
        {
            // Register the main RAM device
            int mainRamDeviceId = devices.GenerateId();
            devices.Register(mainRamDeviceId, "RAM", "Main RAM", "Memory/MainRAM");

            // Map full 64KB as RAM (will be overlaid by ROM and I/O later)
            bus.MapRegion(
                0x0000,
                MainRamSize,
                mainRamDeviceId,
                RegionTag.Ram,
                PagePerms.All,
                mainRamTarget.Capabilities,
                mainRamTarget,
                0);

            // Register the auxiliary RAM device (not mapped directly, accessed via controller)
            int auxRamDeviceId = devices.GenerateId();
            devices.Register(auxRamDeviceId, "RAM", "Auxiliary RAM", "Memory/AuxRAM");
        });
    }

    /// <summary>
    /// Adds Language Card (16KB RAM bank) support.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The Language Card provides 16KB of RAM at $D000-$FFFF that can overlay ROM.
    /// It consists of:
    /// <list type="bullet">
    /// <item><description>Two 4KB banks for $D000-$DFFF (Bank 1 and Bank 2)</description></item>
    /// <item><description>One 8KB bank for $E000-$FFFF</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Bank selection and RAM read/write enable are controlled through 16 soft switches
    /// at $C080-$C08F. The controller is added as both a device and a component.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithLanguageCard(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var languageCard = new LanguageCardController();
        return builder
            .AddComponent(languageCard)
            .AddDevice(languageCard);
    }

    /// <summary>
    /// Adds auxiliary memory controller for 128KB mode.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The auxiliary memory controller manages bank switching between main and
    /// auxiliary RAM through soft switches:
    /// <list type="bullet">
    /// <item><description>ALTZP ($C008/$C009): Switches zero page and stack</description></item>
    /// <item><description>80STORE ($C000/$C001): Enables PAGE2-based switching for display memory</description></item>
    /// <item><description>PAGE2 ($C054/$C055): Selects which page for 80STORE</description></item>
    /// <item><description>HIRES ($C056/$C057): Extends 80STORE to hi-res pages</description></item>
    /// <item><description>RAMRD ($C002/$C003): Reads from aux for $0200-$BFFF</description></item>
    /// <item><description>RAMWRT ($C004/$C005): Writes to aux for $0200-$BFFF</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The controller is added as both a device and a component for retrieval.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithAuxiliaryMemory(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var auxController = new AuxiliaryMemoryController();
        return builder
            .AddComponent(auxController)
            .AddDevice(auxController);
    }

    /// <summary>
    /// Adds the Pocket2e I/O page handler ($C000-$CFFF).
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The I/O page handles three distinct regions:
    /// <list type="bullet">
    /// <item><description>$C000-$C0FF: Soft switches (keyboard, video, memory control)</description></item>
    /// <item><description>$C100-$C7FF: Slot ROM (256 bytes per slot)</description></item>
    /// <item><description>$C800-$CFFF: Expansion ROM (2KB, banked from selected slot)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method retrieves the <see cref="IOPageDispatcher"/> and <see cref="ISlotManager"/>
    /// from the component bag (creating them via <see cref="WithSlotManager"/> if needed).
    /// </para>
    /// </remarks>
    public static MachineBuilder WithPocket2eIOPage(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Use AfterBuild to access components after they've been added to the machine
        return builder.AfterBuild(machine =>
        {
            // Get dispatcher and slot manager from the component bag
            // These should have been added by WithSlotManager (which AsPocket2e calls first)
            var dispatcher = machine.GetComponent<IOPageDispatcher>();
            var slotManager = machine.GetComponent<ISlotManager>();

            if (dispatcher == null || slotManager == null)
            {
                throw new InvalidOperationException(
                    "WithPocket2eIOPage requires WithSlotManager to be called first to create " +
                    "the IOPageDispatcher and SlotManager components.");
            }

            // Create the I/O page composite target
            var ioPage = new Pocket2eIOPage(dispatcher, slotManager);

            // Register the I/O page device
            int ioPageDeviceId = machine.Devices.GenerateId();
            machine.Devices.Register(ioPageDeviceId, "IO", "I/O Page", "Memory/IOPage");

            // Map the I/O page at $C000-$CFFF
            machine.Bus.MapRegion(
                0xC000,
                0x1000,
                ioPageDeviceId,
                RegionTag.Io,
                PagePerms.ReadWrite,
                ioPage.Capabilities,
                ioPage,
                0);
        });
    }

    /// <summary>
    /// Adds the slot manager for expansion cards (slots 1-7).
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// The slot manager handles:
    /// <list type="bullet">
    /// <item><description>Card installation in slots 1-7</description></item>
    /// <item><description>I/O handler registration ($C0n0-$C0nF per slot)</description></item>
    /// <item><description>ROM region access ($Cn00-$CnFF per slot)</description></item>
    /// <item><description>Expansion ROM bank selection ($C800-$CFFF)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Creates an <see cref="IOPageDispatcher"/> and <see cref="SlotManager"/> and adds them
    /// as components for later retrieval by <see cref="WithPocket2eIOPage"/> and
    /// <see cref="WithCard"/>.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithSlotManager(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Create the I/O page dispatcher and slot manager
        var dispatcher = new IOPageDispatcher();
        var slotManager = new SlotManager(dispatcher);

        // Add both as components for retrieval
        return builder
            .AddComponent(dispatcher)
            .AddComponent<ISlotManager>(slotManager);
    }

    /// <summary>
    /// Installs an expansion card in a slot.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <param name="slot">The slot number (1-7).</param>
    /// <param name="card">The slot card to install.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not in the range 1-7.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="card"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cards are installed during machine build and initialized with the event context.
    /// Each card can provide:
    /// <list type="bullet">
    /// <item><description>I/O handlers at $C0n0-$C0nF</description></item>
    /// <item><description>Slot ROM at $Cn00-$CnFF</description></item>
    /// <item><description>Expansion ROM at $C800-$CFFF (bank-selected)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The card is stored as a <see cref="PendingSlotCard"/> component and installed
    /// during the build process when the <see cref="ISlotManager"/> is available.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithCard(this MachineBuilder builder, int slot, ISlotCard card)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(card, nameof(card));

        if (slot < 1 || slot > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "Slot must be between 1 and 7.");
        }

        // Store the card for later installation during build
        return builder.AddComponent(new PendingSlotCard(slot, card));
    }

    /// <summary>
    /// Adds a stub ROM for testing without a real ROM image.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Creates a minimal ROM with:
    /// <list type="bullet">
    /// <item><description>NOP ($EA) fill pattern</description></item>
    /// <item><description>Reset vector pointing to $FF00 (ROM start)</description></item>
    /// <item><description>JMP to itself at $FF00 for testing</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This is useful for unit testing and bring-up before real ROM images are available.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithStubRom(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Create a 16KB stub ROM
        var romData = new byte[16384];

        // Fill with NOP ($EA)
        Array.Fill(romData, (byte)0xEA);

        // Put JMP $FF00 at $FF00 (offset $3F00 in the ROM)
        // JMP is 4C lo hi
        romData[0x3F00] = 0x4C; // JMP
        romData[0x3F01] = 0x00; // low byte of $FF00
        romData[0x3F02] = 0xFF; // high byte of $FF00

        // Set up vectors at the end of ROM ($FFFA-$FFFF = offsets $3FFA-$3FFF)
        // NMI vector -> $FF00
        romData[0x3FFA] = 0x00;
        romData[0x3FFB] = 0xFF;

        // RESET vector -> $FF00
        romData[0x3FFC] = 0x00;
        romData[0x3FFD] = 0xFF;

        // IRQ/BRK vector -> $FF00
        romData[0x3FFE] = 0x00;
        romData[0x3FFF] = 0xFF;

        return builder.WithRom(romData, 0xC000, "Stub ROM");
    }

    /// <summary>
    /// Configures a complete Pocket2e motherboard with all standard components.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method that configures:
    /// <list type="bullet">
    /// <item><description>Base memory layout (128KB)</description></item>
    /// <item><description>Language Card</description></item>
    /// <item><description>Auxiliary memory controller</description></item>
    /// <item><description>Slot manager</description></item>
    /// <item><description>I/O page handler</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Use <see cref="AddMotherboardDevice{T}"/> to add additional motherboard devices.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithPocket2eMotherboard(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        return builder
            .WithPocket2eMemoryLayout()
            .WithLanguageCard()
            .WithAuxiliaryMemory()
            .WithSlotManager()
            .WithPocket2eIOPage();
    }

    /// <summary>
    /// Adds a motherboard device that registers with the I/O page dispatcher.
    /// </summary>
    /// <typeparam name="T">The type of motherboard device.</typeparam>
    /// <param name="builder">The machine builder to configure.</param>
    /// <param name="device">The motherboard device to add.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="device"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Motherboard devices are built into the Apple II and have no slot number.
    /// They register their soft switch handlers directly with the
    /// <see cref="IOPageDispatcher"/> during initialization.
    /// </para>
    /// <para>
    /// Examples of motherboard devices:
    /// <list type="bullet">
    /// <item><description>Keyboard controller</description></item>
    /// <item><description>Speaker device</description></item>
    /// <item><description>Video mode controller</description></item>
    /// <item><description>Game port (paddles/joystick)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static MachineBuilder AddMotherboardDevice<T>(this MachineBuilder builder, T device)
        where T : class, IMotherboardDevice
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(device, nameof(device));

        // Add as both a component and a scheduled device
        return builder
            .AddComponent(device)
            .AddDevice(device);
    }

    /// <summary>
    /// Adds standard IIe vector table ROM with specified vectors.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <param name="nmiHandler">The address of the NMI handler routine.</param>
    /// <param name="resetHandler">The address of the RESET handler routine.</param>
    /// <param name="irqHandler">The address of the IRQ/BRK handler routine.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This creates a small 6-byte ROM containing the vector table and maps it
    /// as a high-priority layer at $FFFA-$FFFF. The vectors are stored in
    /// little-endian format.
    /// </para>
    /// <para>
    /// The vector table layer has higher priority than the base ROM, ensuring
    /// these vectors are always used regardless of other ROM mappings.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithVectorTable(
        this MachineBuilder builder,
        Addr nmiHandler,
        Addr resetHandler,
        Addr irqHandler)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Create vector table data (6 bytes: NMI, RESET, IRQ)
        byte[] vectorData =
        [
            (byte)(nmiHandler & 0xFF), (byte)((nmiHandler >> 8) & 0xFF),       // NMI vector at $FFFA
            (byte)(resetHandler & 0xFF), (byte)((resetHandler >> 8) & 0xFF),   // RESET vector at $FFFC
            (byte)(irqHandler & 0xFF), (byte)((irqHandler >> 8) & 0xFF),       // IRQ/BRK vector at $FFFE
        ];

        var physical = new PhysicalMemory(vectorData, "VectorTable");
        var target = new RomTarget(physical.Slice(0, VectorTableSize));

        // Map the 6-byte vector table at $FFFA-$FFFF
        // Note: The size is intentionally small (6 bytes) to only cover the vector addresses.
        // The layered mapping system handles the offset from the layer's base address.
        return builder.AddLayeredMapping(
            VectorTableLayerName,
            NmiVectorAddress,
            VectorTableSize,
            target,
            RegionTag.Rom,
            PagePerms.ReadExecute);
    }

    /// <summary>
    /// Adds standard IIe vector table with default monitor entry points.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Uses common IIe-compatible vector addresses:
    /// <list type="bullet">
    /// <item><description>NMI: $FA40 (standard NMI handler)</description></item>
    /// <item><description>RESET: $FA62 (standard RESET handler)</description></item>
    /// <item><description>IRQ/BRK: $FA40 (standard IRQ/BRK handler)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// These are placeholder values; actual values depend on the ROM image being used.
    /// For accurate emulation, use <see cref="WithVectorTable(MachineBuilder, Addr, Addr, Addr)"/>
    /// with vectors from your specific ROM.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithStandardVectorTable(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Standard placeholder vectors pointing to common entry points
        // These are typical IIe-compatible addresses
        return builder.WithVectorTable(
            nmiHandler: 0xFA40,   // Typical NMI handler
            resetHandler: 0xFA62, // Typical RESET handler
            irqHandler: 0xFA40);  // Typical IRQ/BRK handler
    }

    private static ICpu CreatePocket2eCpu(IEventContext context)
    {
        return new Cpu65C02(context);
    }

    /// <summary>
    /// Component wrapper for main RAM, allowing retrieval from the component bag.
    /// </summary>
    /// <param name="Memory">The physical memory backing store.</param>
    /// <param name="Target">The RAM bus target for read/write operations.</param>
    public sealed record MainRamComponent(PhysicalMemory Memory, RamTarget Target);

    /// <summary>
    /// Component wrapper for auxiliary RAM, allowing retrieval from the component bag.
    /// </summary>
    /// <param name="Memory">The physical memory backing store.</param>
    /// <param name="Target">The RAM bus target for read/write operations.</param>
    public sealed record AuxiliaryRamComponent(PhysicalMemory Memory, RamTarget Target);

    /// <summary>
    /// Represents a slot card pending installation during machine build.
    /// </summary>
    /// <param name="Slot">The slot number (1-7).</param>
    /// <param name="Card">The slot card to install.</param>
    public sealed record PendingSlotCard(int Slot, ISlotCard Card) : IPendingSlotCard;
}