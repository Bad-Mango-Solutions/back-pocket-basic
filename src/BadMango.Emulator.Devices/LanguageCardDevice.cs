// <copyright file="LanguageCardDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Language Card device providing 16KB of bank-switched RAM at $D000-$FFFF.
/// </summary>
/// <remarks>
/// <para>
/// The Language Card is a motherboard device that provides 16KB of RAM that can
/// overlay the ROM at $D000-$FFFF. It consists of:
/// </para>
/// <list type="bullet">
/// <item><description>Two 4KB banks for $D000-$DFFF (Bank 1 and Bank 2)</description></item>
/// <item><description>One 8KB bank for $E000-$FFFF</description></item>
/// </list>
/// <para>
/// Bank selection and RAM read/write enable are controlled through 16 soft switches
/// at $C080-$C08F (slot 0's I/O space). The R�2 (double-read) protocol provides
/// write-enable protection: write enable requires two consecutive reads of the
/// same odd address.
/// </para>
/// <para>
/// This device owns its physical memory and configures the bus layers and swap
/// groups during initialization.
/// </para>
/// </remarks>
[DeviceType("languagecard")]
public sealed class LanguageCardDevice : IMotherboardDevice, ISoftSwitchProvider, ILanguageCardState
{
    /// <summary>
    /// The name of the Language Card RAM layer for $E000-$FFFF.
    /// </summary>
    public const string HighLayerName = "LC_E000";

    /// <summary>
    /// The name of the Language Card RAM layer for $D000-$DFFF.
    /// </summary>
    public const string LowLayerName = "LC_D000";

    /// <summary>
    /// The name of the swap group for the $D000-$DFFF bank switching.
    /// </summary>
    public const string SwapGroupName = "LC_D000_BANK";

    /// <summary>
    /// The name of the Bank 1 variant.
    /// </summary>
    public const string Bank1VariantName = "BANK1";

    /// <summary>
    /// The name of the Bank 2 variant.
    /// </summary>
    public const string Bank2VariantName = "BANK2";

    /// <summary>
    /// The layer priority for the Language Card layers.
    /// </summary>
    public const int LayerPriority = 20;

    /// <summary>
    /// Size of each D000 bank (4KB).
    /// </summary>
    private const uint D000BankSize = 0x1000;

    /// <summary>
    /// Size of the E000-FFFF region (8KB).
    /// </summary>
    private const uint E000Size = 0x2000;

    /// <summary>
    /// The number of soft switches controlled by the Language Card.
    /// </summary>
    private const int SwitchCount = 16;

    private readonly SlotIOHandlers handlers;
    private readonly PhysicalMemory bank1Memory;
    private readonly PhysicalMemory bank2Memory;
    private readonly PhysicalMemory highMemory;
    private readonly RamTarget bank1Target;
    private readonly RamTarget bank2Target;
    private readonly RamTarget highTarget;

    private IMemoryBus? bus;
    private uint bankSwapGroupId;
    private int deviceId;

    private bool readRam;           // True = read from LC RAM
    private bool writeEnabled;      // True = writes go to LC RAM
    private bool bank2Selected;     // True = $D000 bank 2 (Bank 1 selected on power-on)
    private bool preWrite;          // R�2 protocol state
    private byte lastReadOffset;    // Last read offset for R�2

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageCardDevice"/> class.
    /// </summary>
    public LanguageCardDevice()
    {
        handlers = new();
        InitializeHandlers();

        // Create the 16KB of Language Card RAM
        // Two 4KB banks for D000-DFFF, one 8KB bank for E000-FFFF
        bank1Memory = new(D000BankSize, "LC_Bank1");
        bank2Memory = new(D000BankSize, "LC_Bank2");
        highMemory = new(E000Size, "LC_E000");

        bank1Target = new(bank1Memory.Slice(0, D000BankSize), "LC_Bank1");
        bank2Target = new(bank2Memory.Slice(0, D000BankSize), "LC_Bank2");
        highTarget = new(highMemory.Slice(0, E000Size), "LC_E000");
    }

    // ??? IPeripheral ????????????????????????????????????????????????????????????

    /// <inheritdoc />
    public string Name => "Language Card";

    /// <inheritdoc />
    public string DeviceType => "LanguageCard";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    // ??? Public Properties ??????????????????????????????????????????????????????

    /// <summary>
    /// Gets a value indicating whether RAM read is enabled.
    /// </summary>
    /// <value><see langword="true"/> if RAM read is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When RAM read is enabled, reads from $D000-$FFFF return Language Card RAM.
    /// When disabled, reads return ROM.
    /// </remarks>
    public bool IsRamReadEnabled => readRam;

    /// <summary>
    /// Gets a value indicating whether RAM write is enabled.
    /// </summary>
    /// <value><see langword="true"/> if RAM write is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When RAM write is enabled, writes to $D000-$FFFF go to Language Card RAM.
    /// When disabled, writes are ignored.
    /// </remarks>
    public bool IsRamWriteEnabled => writeEnabled;

    /// <summary>
    /// Gets the currently selected bank (1 or 2) for the $D000-$DFFF region.
    /// </summary>
    /// <value>1 for Bank 1, 2 for Bank 2.</value>
    public int SelectedBank => bank2Selected ? 2 : 1;

    /// <summary>
    /// Gets the I/O handlers for the Language Card soft switches.
    /// </summary>
    /// <value>The slot I/O handlers for $C080-$C08F.</value>
    public SlotIOHandlers IOHandlers => handlers;

    /// <summary>
    /// Gets the Bank 1 RAM target.
    /// </summary>
    /// <value>The RAM target for Bank 1 ($D000-$DFFF when Bank 1 selected).</value>
    public RamTarget Bank1Target => bank1Target;

    /// <summary>
    /// Gets the Bank 2 RAM target.
    /// </summary>
    /// <value>The RAM target for Bank 2 ($D000-$DFFF when Bank 2 selected).</value>
    public RamTarget Bank2Target => bank2Target;

    /// <summary>
    /// Gets the high RAM target.
    /// </summary>
    /// <value>The RAM target for $E000-$FFFF.</value>
    public RamTarget HighTarget => highTarget;

    /// <summary>
    /// Gets the total size of Language Card RAM (16KB).
    /// </summary>
    /// <value>The total RAM size in bytes.</value>
    public uint TotalRamSize => D000BankSize + D000BankSize + E000Size;

    // ??? ISoftSwitchProvider ????????????????????????????????????????????????????

    /// <inheritdoc />
    public string ProviderName => "Language Card";

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        // Display all 8 Language Card soft switches from the truth table.
        // The "value" indicates whether that switch's configuration is currently active.
        //
        // Truth Table:
        // | Softswitch | Bank | RAM Read | RAM Write | $D000�$DFFF Mapping            |
        // | $C080      | 2    | Yes      | No        | Bank 2 RAM (read)              |
        // | $C081      | 2    | No       | Yes*      | ROM (read), Bank 2 RAM (write) |
        // | $C082      | 2    | No       | No        | ROM (read-only)                |
        // | $C083      | 2    | Yes      | Yes*      | Bank 2 RAM (read/write)        |
        // | $C088      | 1    | Yes      | No        | Bank 1 RAM (read)              |
        // | $C089      | 1    | No       | Yes*      | ROM (read), Bank 1 RAM (write) |
        // | $C08A      | 1    | No       | No        | ROM (read-only)                |
        // | $C08B      | 1    | Yes      | Yes*      | Bank 1 RAM (read/write)        |

        // Determine which switch configuration is currently active
        bool isC080 = bank2Selected && readRam && !writeEnabled;
        bool isC081 = bank2Selected && !readRam && writeEnabled;
        bool isC082 = bank2Selected && !readRam && !writeEnabled;
        bool isC083 = bank2Selected && readRam && writeEnabled;
        bool isC088 = !bank2Selected && readRam && !writeEnabled;
        bool isC089 = !bank2Selected && !readRam && writeEnabled;
        bool isC08A = !bank2Selected && !readRam && !writeEnabled;
        bool isC08B = !bank2Selected && readRam && writeEnabled;

#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
        return
        [
            // Bank 2 switches ($C080-$C083)
            new(
                "LCBANK2RD",
                0xC080,
                isC080,
                "Bank 2 RAM read, write-protected"),
            new(
                "LCBANK2WR",
                0xC081,
                isC081,
                "ROM read, Bank 2 RAM write (R�2)"),
            new(
                "LCBANK2ROM",
                0xC082,
                isC082,
                "ROM read, RAM write-protected"),
            new(
                "LCBANK2RW",
                0xC083,
                isC083,
                "Bank 2 RAM read/write (R�2)"),

            // Bank 1 switches ($C088-$C08B)
            new(
                "LCBANK1RD",
                0xC088,
                isC088,
                "Bank 1 RAM read, write-protected"),
            new(
                "LCBANK1WR",
                0xC089,
                isC089,
                "ROM read, Bank 1 RAM write (R�2)"),
            new(
                "LCBANK1ROM",
                0xC08A,
                isC08A,
                "ROM read, RAM write-protected"),
            new(
                "LCBANK1RW",
                0xC08B,
                isC08B,
                "Bank 1 RAM read/write (R�2)"),
        ];
#pragma warning restore SA1515 // Single-line comment should be preceded by blank line
    }

    // ??? IMotherboardDevice ?????????????????????????????????????????????????????

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // Language Card soft switches are at slot 0's I/O space ($C080-$C08F)
        dispatcher.InstallSlotHandlers(0, handlers);
    }

    // ??? IScheduledDevice ???????????????????????????????????????????????????????

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bus = context.Bus;

        // Save the current page table entries for $D000-$FFFF as base mappings.
        // This must be done during Initialize (not ConfigureMemory) because ROMs
        // are mapped AFTER memory configurations in MachineBuilder.Build().
        // At this point, the ROM is properly mapped and we can save it as the base.
        if (bus is MainBus mainBus)
        {
            // Pages $D through $F (3 pages covering $D000-$FFFF)
            mainBus.SaveBaseMappingRange(0xD, 3);
        }

        // Get the swap group ID (created during configuration)
        try
        {
            bankSwapGroupId = bus.GetSwapGroupId(SwapGroupName);
        }
        catch (KeyNotFoundException)
        {
            throw new InvalidOperationException(
                $"Language Card swap group '{SwapGroupName}' not found. " +
                $"Ensure ConfigureMemory was called during machine configuration.");
        }

        // Set initial state: RAM disabled, Bank 1 selected (default power-on state)
        readRam = false;
        writeEnabled = false;
        bank2Selected = false;
        preWrite = false;
        lastReadOffset = 0;

        ApplyState();
    }

    /// <inheritdoc />
    public void Reset()
    {
        readRam = false;
        writeEnabled = false;
        bank2Selected = false;
        preWrite = false;
        lastReadOffset = 0;

        ApplyState();
    }

    // ??? Configuration Methods ??????????????????????????????????????????????????

    /// <summary>
    /// Configures the Language Card memory layers and swap groups on the bus.
    /// </summary>
    /// <param name="bus">The memory bus to configure.</param>
    /// <param name="registry">The device registry for ID generation.</param>
    /// <remarks>
    /// <para>
    /// This method must be called during machine configuration, before the device
    /// is initialized. It sets up:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The E000-FFFF layer for RAM/ROM switching</description></item>
    /// <item><description>The D000-DFFF layer for RAM/ROM switching</description></item>
    /// <item><description>The D000-DFFF swap group for Bank1/Bank2 selection within the layer</description></item>
    /// </list>
    /// <para>
    /// When RAM read is disabled, both layers are deactivated, allowing reads to
    /// fall through to the underlying base ROM mapping. This ensures the <c>pages</c>
    /// command shows the actual ROM source rather than a placeholder.
    /// </para>
    /// <para>
    /// Note: This method does NOT register the device in the registry. Device registration
    /// is handled by the profile loader's ConfigureMotherboardDevice method to avoid
    /// duplicate registry entries.
    /// </para>
    /// <para>
    /// <b>Important:</b> The base ROM mappings for $D000-$FFFF are saved during
    /// <see cref="Initialize"/> (not here) because ROMs are mapped after memory
    /// configurations in MachineBuilder.Build().
    /// </para>
    /// </remarks>
    public void ConfigureMemory(IMemoryBus bus, IDeviceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(registry);

        // Generate a device ID for internal use in layered mappings.
        // Note: Do NOT call registry.Register() here - device registration is handled
        // by ConfigureMotherboardDevice in MachineBuilder.FromProfile.cs to avoid
        // duplicate registry entries when loading from profiles.
        deviceId = registry.GenerateId();

        // Note: SaveBaseMappingRange is called in Initialize(), not here.
        // This is because ROMs are mapped AFTER memory configurations in
        // MachineBuilder.Build(), so the ROM won't be visible yet at this point.

        // Create the layer for E000-FFFF RAM overlay
        var highLayer = bus.CreateLayer(HighLayerName, LayerPriority);

        // Add the E000-FFFF mapping to the layer
        bus.AddLayeredMapping(new(
            VirtualBase: 0xE000,
            Size: E000Size,
            Layer: highLayer,
            DeviceId: deviceId,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadExecute, // Initial: read-only until write enabled
            Caps: highTarget.Capabilities,
            Target: highTarget,
            PhysBase: 0));

        // Create the layer for D000-DFFF RAM overlay
        // When this layer is inactive, reads fall through to the base ROM mapping
        var lowLayer = bus.CreateLayer(LowLayerName, LayerPriority);

        // Add Bank1 mapping to the D000 layer (Bank1 is default)
        bus.AddLayeredMapping(new(
            VirtualBase: 0xD000,
            Size: D000BankSize,
            Layer: lowLayer,
            DeviceId: deviceId,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.ReadExecute,
            Caps: bank1Target.Capabilities,
            Target: bank1Target,
            PhysBase: 0));

        // Create swap group for D000-DFFF bank switching (Bank1/Bank2 within the layer)
        // Note: We don't select a variant here because the layer is inactive.
        // The swap group variants will be selected in ApplyState() when the layer becomes active.
        uint groupId = bus.CreateSwapGroup(SwapGroupName, virtualBase: 0xD000, size: D000BankSize);

        // Add Bank1 variant
        bus.AddSwapVariant(groupId, Bank1VariantName, bank1Target, physBase: 0, perms: PagePerms.ReadExecute);

        // Add Bank2 variant
        bus.AddSwapVariant(groupId, Bank2VariantName, bank2Target, physBase: 0, perms: PagePerms.ReadExecute);

        // DO NOT call SelectSwapVariant here - the layer is inactive and we don't want
        // to overwrite the base ROM mapping in the page table. The variant will be
        // selected in ApplyState() when the layer becomes active.

        // Both layers start deactivated (LC RAM disabled at power-on, ROM visible)
        // Note: Layers are inactive by default, so we don't need to explicitly deactivate
    }

    /// <summary>
    /// Configures the Language Card memory layers and swap groups on the bus.
    /// </summary>
    /// <param name="bus">The memory bus to configure.</param>
    /// <param name="registry">The device registry for ID generation.</param>
    /// <param name="romTarget">
    /// Legacy parameter for backward compatibility. This parameter is ignored;
    /// the Language Card now uses layer deactivation to show ROM instead of a
    /// passthrough variant.
    /// </param>
    /// <remarks>
    /// <para>
    /// This overload is provided for backward compatibility with existing code
    /// that passes a ROM target. The ROM target parameter is ignored because
    /// the Language Card now deactivates its layers to let the underlying base
    /// ROM mapping show through.
    /// </para>
    /// </remarks>
    [Obsolete("Use ConfigureMemory(IMemoryBus, IDeviceRegistry) instead. The romTarget parameter is no longer used.")]
    public void ConfigureMemory(IMemoryBus bus, IDeviceRegistry registry, IBusTarget romTarget)
    {
        // Ignore romTarget - we now use layer deactivation instead
        ConfigureMemory(bus, registry);
    }

    //// ??? Private Methods ????????????????????????????????????????????????????????

    private void InitializeHandlers()
    {
        for (byte i = 0; i < SwitchCount; i++)
        {
            byte offset = i;
            handlers.Set(i, (o, in ctx) => HandleRead(offset, in ctx), (o, v, in ctx) => HandleWrite(offset, in ctx));
        }
    }

    private byte HandleRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            ProcessSwitch(offset);
        }

        // Language Card soft switches return floating bus value
        return 0xFF;
    }

    private void HandleWrite(byte offset, in BusAccess context)
    {
        // Writes to Language Card soft switches have NO effect.
        // Only reads trigger switch state changes. This is a common emulator misconception.
        // The write is simply ignored.
    }

    /// <summary>
    /// Processes a soft switch read access and updates internal state.
    /// </summary>
    /// <param name="offset">The offset within the I/O page (0x80-0x8F for slot 0).</param>
    /// <remarks>
    /// <para>
    /// The Language Card soft switch encoding uses the low 4 bits of the address:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Bit 0: Write enable address (1 = write enable possible with R�2)</description></item>
    /// <item><description>Bits 0 and 1: Read source - RAM when bits are equal (00 or 11), ROM when different (01 or 10)</description></item>
    /// <item><description>Bit 3: Bank select (0 = Bank 2, 1 = Bank 1)</description></item>
    /// </list>
    /// <para>
    /// The R�2 protocol requires two consecutive reads of the same odd address
    /// to enable writes. This prevents accidental write enabling.
    /// </para>
    /// <para>
    /// Note: Only reads trigger switch effects. Writes to these addresses are ignored.
    /// </para>
    /// </remarks>
    private void ProcessSwitch(byte offset)
    {
        // The offset from the dispatcher is 0x80-0x8F for slot 0.
        // We need to mask to get the low 4 bits (0x0-0xF) for switch decoding.
        byte switchIndex = (byte)(offset & 0x0F);

        bool isWriteEnableAddr = (switchIndex & 0x01) != 0;

        if (isWriteEnableAddr)
        {
            // R�2 protocol: requires two consecutive reads of the same odd address
            if (preWrite && switchIndex == lastReadOffset)
            {
                // Second consecutive read of same odd address - enable write
                writeEnabled = true;
                preWrite = false;
            }
            else if (!writeEnabled)
            {
                // First read of odd address (or different odd address) - prime the R�2 protocol
                preWrite = true;
                lastReadOffset = switchIndex;
            }
            else
            {
                // Write already enabled, reading odd address keeps it enabled but clears preWrite
                preWrite = false;
            }
        }
        else
        {
            // Reading an even address disables write and clears R�2 state
            preWrite = false;
            writeEnabled = false;
        }

        // Decode state from offset bits
        // RAM read is enabled when bits 0 and 1 are the same (both 0 or both 1)
        bool bit0 = (switchIndex & 0x01) != 0;
        bool bit1 = (switchIndex & 0x02) != 0;
        readRam = bit0 == bit1;

        // Bank 2 is selected when bit 3 is 0 (addresses $C080-$C087)
        // Bank 1 is selected when bit 3 is 1 (addresses $C088-$C08F)
        bank2Selected = (switchIndex & 0x08) == 0;

        ApplyState();
    }

    private void ApplyState()
    {
        if (bus is null)
        {
            return;
        }

        //// The Language Card has complex state depending on readRam and writeEnabled:
        ////
        //// Truth Table (from spec):
        //// | Softswitch | RAM Read | RAM Write | $D000�$DFFF                    | $E000�$FFFF |
        //// | $C080      | Yes      | No        | Bank RAM (read)                | RAM         |
        //// | $C081      | No       | Yes*      | ROM (read), Bank RAM (write)   | RAM         |
        //// | $C082      | No       | No        | ROM                            | ROM         |
        //// | $C083      | Yes      | Yes*      | Bank RAM (read/write)          | RAM         |
        ////
        //// Key insight from the spec:
        //// - "RAM Read" column specifically refers to the $D000-$DFFF bank-switched region
        //// - $E000-$FFFF is LC RAM whenever readRam OR writeEnabled is true (i.e., not $C082/$C08A)
        //// - Only $C082/$C08A (both false) maps $E000-$FFFF to ROM
        ////
        //// For $D000-$DFFF:
        //// - readRam=true: LC RAM is visible for reads (bank selected)
        //// - readRam=false, writeEnabled=true ($C081/$C089): ROM reads, but writes go to LC RAM
        ////   NOTE: This "split routing" mode is a hardware quirk that our layer system cannot
        ////   fully emulate. When in this state, we keep the D000 layer active with write-only
        ////   permission, which means reads will fail rather than falling through to ROM.
        ////   This is a known limitation - most software uses $C083/$C08B (full RAM) or
        ////   $C082/$C08A (full ROM) rather than the split modes.

        bool lcRamActive = readRam || writeEnabled;

        // Determine permissions for $E000-$FFFF layer
        // E000-FFFF is always readable when LC RAM is active, writable when writeEnabled
        PagePerms highPerms = PagePerms.ReadExecute;
        if (writeEnabled)
        {
            highPerms |= PagePerms.Write;
        }

        if (lcRamActive)
        {
            // Activate E000-FFFF layer (always RAM when lcRamActive)
            if (!bus.IsLayerActive(HighLayerName))
            {
                bus.ActivateLayer(HighLayerName);
            }

            bus.SetLayerPermissions(HighLayerName, highPerms);

            // For D000-DFFF: determine the correct state
            if (readRam)
            {
                // Full RAM read mode: activate layer with read permission
                PagePerms lowPerms = writeEnabled ? PagePerms.All : PagePerms.ReadExecute;

                if (!bus.IsLayerActive(LowLayerName))
                {
                    bus.ActivateLayer(LowLayerName);
                }

                bus.SetLayerPermissions(LowLayerName, lowPerms);
            }
            else
            {
                // Split mode ($C081/$C089): ROM reads, RAM writes for $D000-$DFFF
                // LIMITATION: Our layer system can't do split read/write routing.
                // We keep the layer active for writes but reads from D000-DFFF
                // will come from LC RAM instead of ROM. This is a known limitation.
                // For accurate split-mode emulation, a composite target would be needed.
                if (!bus.IsLayerActive(LowLayerName))
                {
                    bus.ActivateLayer(LowLayerName);
                }

                // Set write permission; reads will come from LC RAM (not ROM as spec requires)
                bus.SetLayerPermissions(LowLayerName, PagePerms.All);
            }

            // Select the appropriate bank variant for D000-DFFF
            string variantName = bank2Selected ? Bank2VariantName : Bank1VariantName;
            if (bus.GetActiveSwapVariant(bankSwapGroupId) != variantName)
            {
                bus.SelectSwapVariant(bankSwapGroupId, variantName);
            }
        }
        else
        {
            // Full ROM mode ($C082/$C08A): deactivate both layers
            if (bus.IsLayerActive(HighLayerName))
            {
                bus.DeactivateLayer(HighLayerName);
            }

            if (bus.IsLayerActive(LowLayerName))
            {
                bus.DeactivateLayer(LowLayerName);
            }
        }
    }
}