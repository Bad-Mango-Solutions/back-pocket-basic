// <copyright file="Extended80ColumnDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Extended 80-Column Card device providing 64KB auxiliary RAM, 80-column text display,
/// double hi-res graphics, and expansion ROM support for the Apple IIe.
/// </summary>
/// <remarks>
/// <para>
/// The Extended 80-Column Card (also known as the Apple IIe Memory Expansion Card) provides:
/// </para>
/// <list type="bullet">
/// <item><description>64KB of auxiliary RAM for a total of 128KB system memory</description></item>
/// <item><description>80-column text display capability (alternating main/aux memory)</description></item>
/// <item><description>Double hi-res graphics mode (560×192 pixels)</description></item>
/// <item><description>Expansion ROM at $C100-$CFFF when INTCXROM is enabled</description></item>
/// </list>
/// <para>
/// Memory bank switching is controlled through soft switches at $C000-$C00F:
/// </para>
/// <list type="bullet">
/// <item><description>$C000/$C001: 80STORE off/on - PAGE2 controls display memory switching</description></item>
/// <item><description>$C002/$C003: RAMRD off/on - Read from auxiliary RAM ($0200-$BFFF)</description></item>
/// <item><description>$C004/$C005: RAMWRT off/on - Write to auxiliary RAM ($0200-$BFFF)</description></item>
/// <item><description>$C006/$C007: SETSLOTCX/SETINTCX - Peripheral/internal ROM at $C100-$CFFF</description></item>
/// <item><description>$C008/$C009: SETSTDZP/SETALTZP - Standard/alternate zero page and stack</description></item>
/// <item><description>$C00A/$C00B: SETINTC3/SETSLOTC3 - Internal/slot ROM at $C300</description></item>
/// <item><description>$C00C/$C00D: 80COLOFF/80COLON - 40/80-column display mode</description></item>
/// </list>
/// <para>
/// Note: ALTCHAR ($C00E/$C00F) is managed by <see cref="CharacterDevice"/>, not this device.
/// </para>
/// </remarks>
[DeviceType("extended80column")]
public sealed class Extended80ColumnDevice : IMotherboardDevice, ISoftSwitchProvider, IExtended80ColumnDevice
{
    /// <summary>
    /// The name of the auxiliary RAM layer for $0200-$BFFF.
    /// </summary>
    public const string LayerNameAuxRam = "AUX_RAM";

    /// <summary>
    /// The name of the auxiliary zero page layer for $0000-$01FF.
    /// </summary>
    public const string LayerNameAuxZeroPage = "AUX_ZP";

    /// <summary>
    /// The name of the auxiliary text page layer for $0400-$07FF (80-column text).
    /// </summary>
    public const string LayerNameAuxText = "AUX_TEXT";

    /// <summary>
    /// The name of the auxiliary hi-res page 1 layer ($2000-$3FFF).
    /// </summary>
    public const string LayerNameAuxHiRes1 = "AUX_HIRES1";

    /// <summary>
    /// The name of the internal ROM layer for $C100-$CFFF.
    /// </summary>
    public const string LayerNameInternalRom = "INT_CXROM";

    /// <summary>
    /// The layer priority for auxiliary memory layers.
    /// </summary>
    public const int LayerPriority = 10;

    /// <summary>
    /// Size of auxiliary RAM (64KB).
    /// </summary>
    private const uint AuxRamSize = 0x10000;

    /// <summary>
    /// Size of the expansion ROM (4KB to cover full $C000-$CFFF I/O page).
    /// The CompositeIOTarget passes I/O page offsets directly, so we need
    /// the full 4KB even though only $C100-$CFFF contains ROM data.
    /// </summary>
    private const uint ExpansionRomSize = 0x1000;

    private readonly PhysicalMemory auxiliaryRam;
    private readonly PhysicalMemory expansionRom;
    private readonly RamTarget auxRamTarget;
    private readonly RamTarget auxZpTarget;
    private readonly RamTarget auxTextTarget;
    private readonly RamTarget auxHiRes1Target;
    private readonly RomTarget expansionRomTarget;
    private readonly RomTarget expansionRomC8Target; // Target for $C800-$CFFF region only

    private IMemoryBus? bus;
    private IVideoDevice? videoDevice;
    private IInternalRomHandler? internalRomHandler;
    private Extended80ColumnPage0Target? page0Target; // Reference to the page 0 composite target
    private int deviceId;
    private bool layersConfigured; // Tracks whether ConfigureMemory has been called

    // ─── Soft Switch State ──────────────────────────────────────────────
    private bool store80;       // 80STORE: PAGE2 controls display memory
    private bool ramrd;         // RAMRD: Read from auxiliary RAM
    private bool ramwrt;        // RAMWRT: Write to auxiliary RAM
    private bool intcxrom;      // INTCXROM: Internal ROM at $C100-$CFFF
    private bool altzp;         // ALTZP: Alternate zero page/stack
    private bool slotc3rom;     // SLOTC3ROM: Slot 3 ROM (false = internal ROM at $C300)
    private bool col80;         // 80COL: 80-column display mode
    private bool page2;         // PAGE2: Page 2 selected (affects 80STORE)
    private bool hires;         // HIRES: Hi-res mode (affects 80STORE)

    /// <summary>
    /// Initializes a new instance of the <see cref="Extended80ColumnDevice"/> class.
    /// </summary>
    public Extended80ColumnDevice()
    {
        // Create 64KB of auxiliary RAM
        auxiliaryRam = new PhysicalMemory(AuxRamSize, "AUX_RAM");

        // Create expansion ROM (can be loaded from profile)
        // Uses full 4KB so I/O page offsets map directly
        expansionRom = new PhysicalMemory(ExpansionRomSize, "EXP_ROM");

        // Create targets for bus mapping
        // Aux RAM target for $0200-$BFFF: offset=0x0200, size=0xBE00 (47.5KB)
        auxRamTarget = new RamTarget(auxiliaryRam.Slice(0x0200, 0xBE00), "AUX_RAM");

        // Zero page/stack target for $0000-$01FF: offset=0x0000, size=0x0200 (512 bytes)
        auxZpTarget = new RamTarget(auxiliaryRam.Slice(0x0000, 0x0200), "AUX_ZP");

        // Text page target for $0400-$07FF: offset=0x0400, size=0x0400 (1KB)
        auxTextTarget = new RamTarget(auxiliaryRam.Slice(0x0400, 0x0400), "AUX_TEXT");

        // Hi-res page 1 target for $2000-$3FFF: offset=0x2000, size=0x2000 (8KB)
        auxHiRes1Target = new RamTarget(auxiliaryRam.Slice(0x2000, 0x2000), "AUX_HIRES1");

        // Expansion ROM target - full 4KB for direct I/O page offset mapping
        // Used for INTCXROM ($C100-$CFFF)
        expansionRomTarget = new RomTarget(expansionRom.Slice(0, ExpansionRomSize), "EXP_ROM");

        // Expansion ROM target for $C800-$CFFF region only (2KB)
        // Used as the default expansion ROM and for slot 3 internal expansion ROM
        expansionRomC8Target = new RomTarget(expansionRom.Slice(0x800, 0x800), "INT_EXP_ROM");
    }

    // ─── IPeripheral ────────────────────────────────────────────────────

    /// <inheritdoc />
    public string Name => "Extended 80-Column Card";

    /// <inheritdoc />
    public string DeviceType => "Extended80Column";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    // ─── ISoftSwitchProvider ────────────────────────────────────────────

    /// <inheritdoc />
    public string ProviderName => "Extended 80-Column Card";

    // ─── Public Properties ──────────────────────────────────────────────

    /// <summary>
    /// Gets a value indicating whether 80STORE mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if 80STORE mode is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When 80STORE is enabled, the PAGE2 soft switch ($C054/$C055) controls whether
    /// display memory accesses go to auxiliary or main memory, enabling 80-column text.
    /// </remarks>
    public bool Is80StoreEnabled => store80;

    /// <summary>
    /// Gets a value indicating whether reads from $0200-$BFFF come from auxiliary RAM.
    /// </summary>
    /// <value><see langword="true"/> if RAMRD is enabled; otherwise, <see langword="false"/>.</value>
    public bool IsRamRdEnabled => ramrd;

    /// <summary>
    /// Gets a value indicating whether writes to $0200-$BFFF go to auxiliary RAM.
    /// </summary>
    /// <value><see langword="true"/> if RAMWRT is enabled; otherwise, <see langword="false"/>.</value>
    public bool IsRamWrtEnabled => ramwrt;

    /// <summary>
    /// Gets a value indicating whether internal ROM is selected at $C100-$CFFF.
    /// </summary>
    /// <value><see langword="true"/> if internal ROM is selected; otherwise, <see langword="false"/>.</value>
    public bool IsIntCXRomEnabled => intcxrom;

    /// <summary>
    /// Gets a value indicating whether alternate zero page/stack is enabled.
    /// </summary>
    /// <value><see langword="true"/> if auxiliary zero page/stack is active; otherwise, <see langword="false"/>.</value>
    public bool IsAltZpEnabled => altzp;

    /// <summary>
    /// Gets a value indicating whether slot 3 ROM is selected at $C300.
    /// </summary>
    /// <value><see langword="true"/> if slot 3 ROM is selected; otherwise, <see langword="false"/> for internal ROM.</value>
    public bool IsSlotC3RomEnabled => slotc3rom;

    /// <summary>
    /// Gets a value indicating whether 80-column display mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if 80-column mode is enabled; otherwise, <see langword="false"/>.</value>
    public bool Is80ColumnEnabled => col80;

    /// <summary>
    /// Gets a value indicating whether PAGE2 is selected.
    /// </summary>
    /// <value><see langword="true"/> if page 2 is selected; otherwise, <see langword="false"/>.</value>
    public bool IsPage2Selected => page2;

    /// <summary>
    /// Gets a value indicating whether hi-res mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if hi-res mode is enabled; otherwise, <see langword="false"/>.</value>
    public bool IsHiResEnabled => hires;

    /// <summary>
    /// Gets the auxiliary RAM target for direct access.
    /// </summary>
    /// <value>The RAM target for auxiliary memory.</value>
    public RamTarget AuxRamTarget => auxRamTarget;

    /// <summary>
    /// Gets the expansion ROM target for direct access.
    /// </summary>
    /// <value>The ROM target for expansion ROM.</value>
    public RomTarget ExpansionRomTarget => expansionRomTarget;

    /// <summary>
    /// Gets the total size of auxiliary RAM (64KB).
    /// </summary>
    /// <value>The total auxiliary RAM size in bytes.</value>
    public uint TotalAuxRamSize => AuxRamSize;

    /// <summary>
    /// Gets the auxiliary RAM as a span for direct memory access.
    /// </summary>
    /// <value>A span covering the 64KB auxiliary RAM.</value>
    public Span<byte> AuxiliaryRam => auxiliaryRam.AsSpan();

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        return
        [
            new("80STORE", 0xC000, store80, "PAGE2 controls display memory switching"),
            new("RAMRD", 0xC002, ramrd, "Reads from auxiliary RAM ($0200-$BFFF)"),
            new("RAMWRT", 0xC004, ramwrt, "Writes to auxiliary RAM ($0200-$BFFF)"),
            new("INTCXROM", 0xC006, intcxrom, "Internal ROM at $C100-$CFFF"),
            new("ALTZP", 0xC008, altzp, "Alternate zero page/stack enabled"),
            new("SLOTC3ROM", 0xC00A, slotc3rom, "Slot 3 ROM at $C300 (vs internal)"),
            new("80COL", 0xC00C, col80, "80-column display mode enabled"),
            new("PAGE2", 0xC054, page2, "Page 2 selected for 80STORE"),
            new("HIRES", 0xC056, hires, "Hi-res mode for 80STORE"),
        ];
    }

    // ─── IScheduledDevice ───────────────────────────────────────────────

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bus = context.Bus;

        // Get the video device from the machine context
        videoDevice = context.GetComponent<IVideoDevice>();

        // Get the internal ROM handler (CompositeIOTarget) to register our expansion ROM
        internalRomHandler = context.GetComponent<IInternalRomHandler>();
        if (internalRomHandler is not null)
        {
            // Register our expansion ROM as the internal ROM for INTCXROM switching
            internalRomHandler.SetInternalRom(expansionRomTarget);
        }

        // Get the slot manager to register our expansion ROM
        var slotManager = context.GetComponent<ISlotManager>();
        if (slotManager is not null)
        {
            // Register our expansion ROM as the DEFAULT expansion ROM at $C800-$CFFF.
            // This makes our ROM visible at power-on and whenever $CFFF is accessed
            // (which deselects any slot's layered expansion ROM).
            slotManager.SetDefaultExpansionRom(GetExpansionRomTarget());

            // Also register as the internal expansion ROM for slot 3.
            // When INTC3ROM is enabled and $C300-$C3FF is accessed, the internal ROM
            // provides data but expansion ROM selection for slot 3 still occurs.
            // This sets up the slot 3 expansion ROM to be our ROM.
            slotManager.RegisterInternalExpansionRom(3, GetExpansionRomTarget());
        }

        // Set initial state: all switches disabled (main RAM visible)
        store80 = false;
        ramrd = false;
        ramwrt = false;
        intcxrom = false;
        altzp = false;
        slotc3rom = false;
        col80 = false;
        page2 = false;
        hires = false;

        ApplyState();
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // ─── Memory Bank Switches ($C000-$C00F) ─────────────────────────
        // Note: $C000 read is keyboard, we only handle write
        dispatcher.RegisterWrite(0x00, Handle80StoreOff);   // $C000: 80STOREOFF
        dispatcher.RegisterWrite(0x01, Handle80StoreOn);    // $C001: 80STOREON
        dispatcher.RegisterWrite(0x02, HandleRdMainRam);    // $C002: RDMAINRAM
        dispatcher.RegisterWrite(0x03, HandleRdCardRam);    // $C003: RDCARDRAM
        dispatcher.RegisterWrite(0x04, HandleWrMainRam);    // $C004: WRMAINRAM
        dispatcher.RegisterWrite(0x05, HandleWrCardRam);    // $C005: WRCARDRAM
        dispatcher.RegisterWrite(0x06, HandleSetSlotCX);    // $C006: SETSLOTCX
        dispatcher.RegisterWrite(0x07, HandleSetIntCX);     // $C007: SETINTCX
        dispatcher.RegisterWrite(0x08, HandleSetStdZp);     // $C008: SETSTDZP
        dispatcher.RegisterWrite(0x09, HandleSetAltZp);     // $C009: SETALTZP
        dispatcher.RegisterWrite(0x0A, HandleSetIntC3);     // $C00A: SETINTC3
        dispatcher.RegisterWrite(0x0B, HandleSetSlotC3);    // $C00B: SETSLOTC3
        dispatcher.RegisterWrite(0x0C, Handle80ColOff);     // $C00C: 80COLOFF
        dispatcher.RegisterWrite(0x0D, Handle80ColOn);      // $C00D: 80COLON
        dispatcher.RegisterRead(0x13, ReadRamRdStatus);     // $C013: RDRAMRD
        dispatcher.RegisterRead(0x14, ReadRamWrtStatus);    // $C014: RDRAMWRT
        dispatcher.RegisterRead(0x15, ReadIntCXRomStatus);  // $C015: RDCXROM
        dispatcher.RegisterRead(0x16, ReadAltZpStatus);     // $C016: RDALTZP
        dispatcher.RegisterRead(0x17, ReadC3RomStatus);     // $C017: RDC3ROM
        dispatcher.RegisterRead(0x18, Read80StoreStatus);   // $C018: RD80STORE

        // $C019 (RDVBL), $C01A-$C01D, $C01F (RD80COL) are handled by VideoDevice
        // $C01E (RDALTCHAR) is handled by CharacterDevice
        // PAGE2 and HIRES switches are registered by VideoDevice
    }

    /// <inheritdoc />
    public void Reset()
    {
        store80 = false;
        ramrd = false;
        ramwrt = false;
        intcxrom = false;
        altzp = false;
        slotc3rom = false;
        col80 = false;
        page2 = false;
        hires = false;

        ApplyState();
    }

    /// <summary>
    /// Configures the auxiliary memory layers and swap groups on the bus.
    /// </summary>
    /// <param name="memoryBus">The memory bus to configure.</param>
    /// <param name="registry">The device registry for ID generation.</param>
    /// <remarks>
    /// <para>
    /// This method must be called during machine configuration, before the device
    /// is initialized. It sets up:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The auxiliary RAM layer for $1000-$BFFF (RAMRD/RAMWRT switching)</description></item>
    /// </list>
    /// <para>
    /// Note: Sub-page regions (zero page, stack, text page, and the first part of general RAM)
    /// cannot use layer-based switching because the bus uses 4KB page granularity.
    /// These regions require a composite target approach (like AuxiliaryMemoryPage0Target)
    /// that routes each access based on soft switch state at access time.
    /// </para>
    /// <para>
    /// When a layer is inactive, accesses fall through to the underlying base memory
    /// mapping (main RAM). When active, the layer redirects accesses to auxiliary RAM.
    /// </para>
    /// </remarks>
    public void ConfigureMemory(IMemoryBus memoryBus, IDeviceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(memoryBus);
        ArgumentNullException.ThrowIfNull(registry);

        // Generate a device ID for internal use in layered mappings
        deviceId = registry.GenerateId();

        // NOTE: Sub-page regions ($0000-$0FFF) cannot be handled by layers because the bus
        // uses 4KB page granularity. The text page ($0400-$07FF), zero page ($0000-$00FF),
        // and stack ($0100-$01FF) are all within the first 4KB page.
        //
        // These regions require a composite target approach where each memory access
        // checks the soft switch state at access time. This is handled by mapping page 0
        // with a composite target (like AuxiliaryMemoryPage0Target) that has a reference
        // to this device and checks Is80StoreEnabled/IsPage2Selected for routing.
        //
        // For now, we only set up layers for regions that are page-aligned (>= $1000).

        // Create the layer for auxiliary RAM ($1000-$BFFF)
        // This handles RAMRD/RAMWRT switching for general memory above page 0.
        var ramLayer = memoryBus.CreateLayer(LayerNameAuxRam, LayerPriority);
        memoryBus.AddLayeredMapping(new(
            VirtualBase: 0x1000,
            Size: 0xB000,  // $1000-$BFFF = 44KB
            Layer: ramLayer,
            DeviceId: deviceId,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.All,
            Caps: auxRamTarget.Capabilities,
            Target: new RamTarget(auxiliaryRam.Slice(0x1000, 0xB000), "AUX_RAM_1000"),
            PhysBase: 0));

        // Create the layer for auxiliary hi-res page 1 ($2000-$3FFF) - 80STORE + HIRES mode
        // This has higher priority than the general AUX_RAM layer and handles 80STORE+PAGE2+HIRES
        var hiresLayer = memoryBus.CreateLayer(LayerNameAuxHiRes1, LayerPriority + 1);
        memoryBus.AddLayeredMapping(new(
            VirtualBase: 0x2000,
            Size: 0x2000,
            Layer: hiresLayer,
            DeviceId: deviceId,
            RegionTag: RegionTag.Ram,
            Perms: PagePerms.All,
            Caps: auxHiRes1Target.Capabilities,
            Target: auxHiRes1Target,
            PhysBase: 0));

        // Note: The following regions CANNOT be handled by layers (sub-page):
        // - AUX_ZP ($0000-$01FF): Zero page and stack, controlled by ALTZP
        // - AUX_TEXT ($0400-$07FF): Text page, controlled by 80STORE+PAGE2
        // - General RAM in page 0 ($0200-$0FFF except text): controlled by RAMRD/RAMWRT
        //
        // These require a composite target configured separately. See AuxiliaryMemoryPage0Target.
        //
        // For full 80-column support, the machine configuration should set up a composite
        // target for page 0 that references this device for state queries.

        // Note: The expansion ROM layer for $C100-$CFFF is NOT created here.
        // The CompositeIOTarget owns that region and handles INTCXROM switching
        // via the IInternalRomHandler interface. We register our expansion ROM
        // with it during Initialize().

        // All layers start deactivated (main RAM visible at power-on)
        layersConfigured = true;
    }

    /// <summary>
    /// Called when PAGE2 state changes (from VideoDevice or internal).
    /// </summary>
    /// <param name="selected">Whether PAGE2 is selected.</param>
    public void SetPage2(bool selected)
    {
        if (page2 != selected)
        {
            page2 = selected;
            ApplyState();
        }
    }

    /// <summary>
    /// Called when HIRES state changes (from VideoDevice or internal).
    /// </summary>
    /// <param name="enabled">Whether HIRES mode is enabled.</param>
    public void SetHiRes(bool enabled)
    {
        if (hires != enabled)
        {
            hires = enabled;
            ApplyState();
        }
    }

    /// <summary>
    /// Loads expansion ROM data into the device.
    /// </summary>
    /// <param name="romData">The ROM data to load (up to 3840 bytes for $C100-$CFFF).</param>
    /// <remarks>
    /// <para>
    /// This method loads the 80-column card's expansion ROM, which is mapped at
    /// $C100-$CFFF when the INTCXROM soft switch is enabled.
    /// </para>
    /// <para>
    /// The ROM data should be 3840 bytes (0x0F00) to cover the entire $C100-$CFFF range.
    /// If the provided data is smaller, only that portion is loaded. The data is loaded
    /// at offset 0x100 in the 4KB ROM buffer to match I/O page addressing.
    /// </para>
    /// </remarks>
    public void LoadExpansionRom(ReadOnlySpan<byte> romData)
    {
        // Load ROM data at offset 0x100 to match I/O page addressing ($C100-$CFFF)
        const int romOffset = 0x100;
        int maxCopyLength = (int)ExpansionRomSize - romOffset;
        int copyLength = Math.Min(romData.Length, maxCopyLength);
        romData[..copyLength].CopyTo(expansionRom.AsSpan().Slice(romOffset));
    }

    /// <summary>
    /// Loads expansion ROM data from a byte array.
    /// </summary>
    /// <param name="romData">The ROM data to load.</param>
    public void LoadExpansionRom(byte[] romData)
    {
        ArgumentNullException.ThrowIfNull(romData);
        LoadExpansionRom(romData.AsSpan());
    }

    /// <summary>
    /// Reads a byte from auxiliary RAM.
    /// </summary>
    /// <param name="address">The address within auxiliary RAM (0-65535).</param>
    /// <returns>The byte value at the specified address.</returns>
    public byte ReadAuxRam(ushort address)
    {
        return auxiliaryRam.AsSpan()[address];
    }

    /// <summary>
    /// Writes a byte to auxiliary RAM.
    /// </summary>
    /// <param name="address">The address within auxiliary RAM (0-65535).</param>
    /// <param name="value">The byte value to write.</param>
    public void WriteAuxRam(ushort address, byte value)
    {
        auxiliaryRam.AsSpan()[address] = value;
    }

    /// <summary>
    /// Gets the auxiliary RAM target for page 0 (used by composite target configuration).
    /// </summary>
    /// <returns>A RAM target covering page 0 of auxiliary memory.</returns>
    public RamTarget GetAuxPage0Target()
    {
        return new RamTarget(auxiliaryRam.Slice(0, 0x1000), "AUX_PAGE0");
    }

    /// <summary>
    /// Sets the page 0 composite target reference for routing updates.
    /// </summary>
    /// <param name="target">The page 0 composite target.</param>
    /// <remarks>
    /// This must be called after the page 0 target is created and configured.
    /// The device will call <see cref="Extended80ColumnPage0Target.UpdateRouting"/>
    /// whenever soft switches change to update the routing table.
    /// </remarks>
    public void SetPage0Target(Extended80ColumnPage0Target target)
    {
        page0Target = target;

        // Initialize routing with current state
        page0Target?.UpdateRouting(altzp, store80, page2, ramrd, ramwrt);
    }

    /// <summary>
    /// Gets the expansion ROM target for the $C800-$CFFF region.
    /// </summary>
    /// <returns>A bus target that provides the expansion ROM content.</returns>
    /// <remarks>
    /// <para>
    /// This returns a view into the expansion ROM that maps to the $C800-$CFFF range.
    /// The full expansion ROM is stored in a 4KB buffer with $C000-$C0FF being unused,
    /// $C100-$C7FF being the slot ROM portion, and $C800-$CFFF being the expansion ROM.
    /// </para>
    /// <para>
    /// This is used to register as:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The default expansion ROM (visible at power-on and after $CFFF access)</description></item>
    /// <item><description>The internal expansion ROM for slot 3 (selected when $C300 is accessed with INTC3ROM)</description></item>
    /// </list>
    /// </remarks>
    private IBusTarget GetExpansionRomTarget()
    {
        return expansionRomC8Target;
    }

    private void Handle80StoreOff(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        store80 = false;
        ApplyState();
    }

    private void Handle80StoreOn(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        store80 = true;
        ApplyState();
    }

    private void HandleRdMainRam(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        ramrd = false;
        ApplyState();
    }

    private void HandleRdCardRam(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        ramrd = true;
        ApplyState();
    }

    private void HandleWrMainRam(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        ramwrt = false;
        ApplyState();
    }

    private void HandleWrCardRam(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        ramwrt = true;
        ApplyState();
    }

    private void HandleSetSlotCX(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        intcxrom = false;
        ApplyState();
    }

    private void HandleSetIntCX(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        intcxrom = true;
        ApplyState();
    }

    private void HandleSetStdZp(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        altzp = false;
        ApplyState();
    }

    private void HandleSetAltZp(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        altzp = true;
        ApplyState();
    }

    private void HandleSetIntC3(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        slotc3rom = false;
        ApplyState();
    }

    private void HandleSetSlotC3(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        slotc3rom = true;
        ApplyState();
    }

    private void Handle80ColOff(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        col80 = false;

        // Notify video device of mode change
        videoDevice?.Set80ColumnMode(false);

        ApplyState();
    }

    private void Handle80ColOn(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        col80 = true;

        // Notify video device of mode change
        videoDevice?.Set80ColumnMode(true);

        ApplyState();
    }

    private byte ReadRamRdStatus(byte offset, in BusAccess context)
    {
        return ramrd ? (byte)0x80 : (byte)0x00;
    }

    private byte ReadRamWrtStatus(byte offset, in BusAccess context)
    {
        return ramwrt ? (byte)0x80 : (byte)0x00;
    }

    private byte ReadIntCXRomStatus(byte offset, in BusAccess context)
    {
        return intcxrom ? (byte)0x80 : (byte)0x00;
    }

    private byte ReadAltZpStatus(byte offset, in BusAccess context)
    {
        return altzp ? (byte)0x80 : (byte)0x00;
    }

    private byte ReadC3RomStatus(byte offset, in BusAccess context)
    {
        // RDC3ROM returns 1 if internal ROM at $C300 (i.e., NOT slot C3 ROM)
        return slotc3rom ? (byte)0x00 : (byte)0x80;
    }

    private byte Read80StoreStatus(byte offset, in BusAccess context)
    {
        return store80 ? (byte)0x80 : (byte)0x00;
    }

    /// <summary>
    /// Applies the current soft switch state to the memory bus layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method updates the memory mapping based on the current soft switch state:
    /// </para>
    /// <list type="bullet">
    /// <item><description>RAMRD/RAMWRT: Activates the auxiliary RAM layer ($1000-$BFFF)</description></item>
    /// <item><description>80STORE + PAGE2 + HIRES: Activates the auxiliary hi-res layer ($2000-$3FFF)</description></item>
    /// <item><description>INTCXROM: Controls expansion ROM layer ($C100-$CFFF) via CompositeIOTarget</description></item>
    /// <item><description>Updates page 0 routing table for sub-page regions</description></item>
    /// </list>
    /// <para>
    /// When a layer is deactivated, accesses fall through to the base memory mapping (main RAM/ROM).
    /// </para>
    /// </remarks>
    private void ApplyState()
    {
        // INTCXROM and SLOTC3ROM: Control internal ROM overlay via CompositeIOTarget
        // These always work, even when layers aren't configured
        if (internalRomHandler is not null)
        {
            internalRomHandler.SetIntCxRom(intcxrom);
            internalRomHandler.SetIntC3Rom(!slotc3rom); // INTC3ROM is inverted from SLOTC3ROM
        }

        // Update page 0 routing table for sub-page regions
        // This is efficient - just updates a few array entries, no per-access checks
        page0Target?.UpdateRouting(altzp, store80, page2, ramrd, ramwrt);

        if (bus is null)
        {
            return;
        }

        // If layers haven't been configured, skip layer-related operations
        // This happens when the device is used without ConfigureMemory being called
        if (!layersConfigured)
        {
            return;
        }

        // RAMRD/RAMWRT: Auxiliary RAM ($1000-$BFFF)
        // Note: This layer uses read/write permissions based on RAMRD and RAMWRT
        bool auxRamActive = ramrd || ramwrt;
        SetLayerActive(LayerNameAuxRam, auxRamActive);
        if (auxRamActive)
        {
            PagePerms ramPerms = PagePerms.None;
            if (ramrd)
            {
                ramPerms |= PagePerms.ReadExecute;
            }

            if (ramwrt)
            {
                ramPerms |= PagePerms.Write;
            }

            SetLayerPermissions(LayerNameAuxRam, ramPerms);
        }

        // 80STORE mode: Hi-res page 1 ($2000-$3FFF) controlled by PAGE2 + HIRES
        // When 80STORE is on, PAGE2 is set, and HIRES is on, use auxiliary hi-res
        bool auxHiResActive = store80 && page2 && hires;
        SetLayerActive(LayerNameAuxHiRes1, auxHiResActive);
    }

    /// <summary>
    /// Activates or deactivates a memory layer.
    /// </summary>
    /// <param name="layerName">The name of the layer.</param>
    /// <param name="active">Whether the layer should be active.</param>
    private void SetLayerActive(string layerName, bool active)
    {
        if (bus is null)
        {
            return;
        }

        try
        {
            bool isActive = bus.IsLayerActive(layerName);
            if (active && !isActive)
            {
                bus.ActivateLayer(layerName);
            }
            else if (!active && isActive)
            {
                bus.DeactivateLayer(layerName);
            }
        }
        catch (KeyNotFoundException)
        {
            // Layer not configured yet - this is expected during initialization
            // before ConfigureMemory is called
        }
    }

    /// <summary>
    /// Sets permissions on a memory layer.
    /// </summary>
    /// <param name="layerName">The name of the layer.</param>
    /// <param name="perms">The permissions to set.</param>
    private void SetLayerPermissions(string layerName, PagePerms perms)
    {
        if (bus is null)
        {
            return;
        }

        try
        {
            bus.SetLayerPermissions(layerName, perms);
        }
        catch (KeyNotFoundException)
        {
            // Layer not configured yet - this is expected during initialization
            // before ConfigureMemory is called
        }
    }
}