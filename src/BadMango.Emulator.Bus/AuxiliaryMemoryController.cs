// <copyright file="AuxiliaryMemoryController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Controls auxiliary memory bank switching for Pocket2e 128KB mode.
/// </summary>
/// <remarks>
/// <para>
/// The Apple IIe Enhanced has 128KB RAM (64KB main + 64KB auxiliary). Various soft switches
/// control which RAM is visible at different address ranges:
/// </para>
/// <list type="bullet">
/// <item><description>ALTZP ($C008/$C009): Switches zero page and stack</description></item>
/// <item><description>80STORE ($C000/$C001): Enables PAGE2-based switching for display memory</description></item>
/// <item><description>PAGE2 ($C054/$C055): Selects which page for 80STORE</description></item>
/// <item><description>HIRES ($C056/$C057): Extends 80STORE to hi-res pages</description></item>
/// <item><description>RAMRD ($C002/$C003): Reads from aux for $0200-$BFFF</description></item>
/// <item><description>RAMWRT ($C004/$C005): Writes to aux for $0200-$BFFF</description></item>
/// </list>
/// <para>
/// This controller manages soft switch state. Sub-page regions (zero page, stack, text page)
/// are handled by <see cref="AuxiliaryMemoryPage0Target"/> which reads the controller state
/// directly. Hi-res pages use the layered mapping API since they are page-aligned.
/// </para>
/// </remarks>
public sealed class AuxiliaryMemoryController : IMotherboardDevice, ISoftSwitchProvider
{
    /// <summary>
    /// The name of the auxiliary hi-res page 1 layer ($2000-$3FFF).
    /// </summary>
    public const string LayerNameHiResPage1 = "AUX_HIRES1";

    /// <summary>
    /// The name of the auxiliary hi-res page 2 layer ($4000-$5FFF).
    /// </summary>
    public const string LayerNameHiResPage2 = "AUX_HIRES2";

    /// <summary>
    /// The layer priority for auxiliary memory layers.
    /// </summary>
    /// <remarks>
    /// Auxiliary memory layers have priority 10, below the Language Card layer (priority 20).
    /// </remarks>
    public const int LayerPriority = 10;

    private IMemoryBus? bus;
    private IOPageDispatcher? ioDispatcher;

    // ─── Soft Switch State ──────────────────────────────────────────────
    private bool store80;
    private bool ramrd;
    private bool ramwrt;
    private bool altzp;
    private bool page2;
    private bool hires;

    /// <inheritdoc />
    public string Name => "Auxiliary Memory Controller";

    /// <inheritdoc />
    public string DeviceType => "AuxiliaryMemory";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    /// <inheritdoc />
    public string ProviderName => "Auxiliary Memory";

    // ─── Properties ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets a value indicating whether 80STORE mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if 80STORE mode is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When 80STORE is enabled, PAGE2 ($C054/$C055) controls switching between main and auxiliary
    /// memory for display regions (text page and optionally hi-res pages).
    /// </remarks>
    public bool Is80StoreEnabled => store80;

    /// <summary>
    /// Gets a value indicating whether the alternate zero page/stack is enabled.
    /// </summary>
    /// <value><see langword="true"/> if auxiliary zero page/stack is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When ALTZP is enabled, zero page ($0000-$00FF) and stack ($0100-$01FF) access
    /// auxiliary memory instead of main memory.
    /// </remarks>
    public bool IsAltZpEnabled => altzp;

    /// <summary>
    /// Gets a value indicating whether reads from $0200-$BFFF come from auxiliary memory.
    /// </summary>
    /// <value><see langword="true"/> if RAMRD is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// This affects the general RAM region $0200-$BFFF (excluding regions controlled by 80STORE).
    /// </remarks>
    public bool IsRamRdEnabled => ramrd;

    /// <summary>
    /// Gets a value indicating whether writes to $0200-$BFFF go to auxiliary memory.
    /// </summary>
    /// <value><see langword="true"/> if RAMWRT is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// This affects the general RAM region $0200-$BFFF (excluding regions controlled by 80STORE).
    /// </remarks>
    public bool IsRamWrtEnabled => ramwrt;

    /// <summary>
    /// Gets a value indicating whether PAGE2 is selected.
    /// </summary>
    /// <value><see langword="true"/> if PAGE2 is selected; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When 80STORE is enabled, PAGE2 controls whether display memory accesses go to
    /// auxiliary memory (PAGE2=on) or main memory (PAGE2=off).
    /// </remarks>
    public bool IsPage2Selected => page2;

    /// <summary>
    /// Gets a value indicating whether HIRES mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if HIRES mode is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When both 80STORE and HIRES are enabled, PAGE2 controls switching for both
    /// text pages and hi-res pages.
    /// </remarks>
    public bool IsHiResEnabled => hires;

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        return
        [
            new SoftSwitchState("80STORE", 0xC000, store80, "PAGE2 controls display memory switching"),
            new SoftSwitchState("RAMRD", 0xC002, ramrd, "Reads from auxiliary RAM ($0200-$BFFF)"),
            new SoftSwitchState("RAMWRT", 0xC004, ramwrt, "Writes to auxiliary RAM ($0200-$BFFF)"),
            new SoftSwitchState("ALTZP", 0xC008, altzp, "Alternate zero page/stack enabled"),
            new SoftSwitchState("PAGE2", 0xC054, page2, "Page 2 selected for 80STORE"),
            new SoftSwitchState("HIRES", 0xC056, hires, "Hi-res mode (extends 80STORE)"),
        ];
    }

    // ─── IScheduledDevice ───────────────────────────────────────────────

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bus = context.Bus;

        // Set initial state: all switches disabled (main RAM visible)
        store80 = false;
        ramrd = false;
        ramwrt = false;
        altzp = false;
        page2 = false;
        hires = false;

        ApplyState();
    }

    /// <summary>
    /// Registers the auxiliary memory soft switch handlers with the I/O page dispatcher.
    /// </summary>
    /// <param name="dispatcher">The I/O page dispatcher to register handlers with.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dispatcher"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers handlers for the following soft switches:
    /// </para>
    /// <list type="bullet">
    /// <item><description>$C000-$C001: 80STORE off/on</description></item>
    /// <item><description>$C002-$C003: RAMRD off/on (main/auxiliary read)</description></item>
    /// <item><description>$C004-$C005: RAMWRT off/on (main/auxiliary write)</description></item>
    /// <item><description>$C008-$C009: ALTZP off/on (standard/alternate zero page)</description></item>
    /// <item><description>$C054-$C055: PAGE2 off/on (page 1/page 2 selection)</description></item>
    /// <item><description>$C056-$C057: HIRES off/on</description></item>
    /// </list>
    /// <para>
    /// Note: Some offsets overlap with keyboard ($C000) — write-only switches don't conflict
    /// with keyboard read. The keyboard handler should remain registered for reads at $C000.
    /// </para>
    /// </remarks>
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        ioDispatcher = dispatcher;

        // $C000: 80STOREOFF - Write-only (read is keyboard)
        dispatcher.RegisterWrite(0x00, Handle80StoreOff);

        // $C001: 80STOREON - Write-only
        dispatcher.RegisterWrite(0x01, Handle80StoreOn);

        // $C002: RDMAINRAM - Write-only
        dispatcher.RegisterWrite(0x02, HandleRdMainRam);

        // $C003: RDCARDRAM - Write-only
        dispatcher.RegisterWrite(0x03, HandleRdCardRam);

        // $C004: WRMAINRAM - Write-only
        dispatcher.RegisterWrite(0x04, HandleWrMainRam);

        // $C005: WRCARDRAM - Write-only
        dispatcher.RegisterWrite(0x05, HandleWrCardRam);

        // $C008: SETSTDZP - Write-only
        dispatcher.RegisterWrite(0x08, HandleSetStdZp);

        // $C009: SETALTZP - Write-only
        dispatcher.RegisterWrite(0x09, HandleSetAltZp);

        // $C054: PAGE1 / TXTPAGE1 - Both read and write
        dispatcher.Register(0x54, HandlePage1Read, HandlePage1Write);

        // $C055: PAGE2 / TXTPAGE2 - Both read and write
        dispatcher.Register(0x55, HandlePage2Read, HandlePage2Write);

        // $C056: TXTCLR / LORES - Both read and write
        dispatcher.Register(0x56, HandleLoResRead, HandleLoResWrite);

        // $C057: TXTSET / HIRES - Both read and write
        dispatcher.Register(0x57, HandleHiResRead, HandleHiResWrite);
    }

    /// <summary>
    /// Resets the auxiliary memory controller to its power-on state.
    /// </summary>
    /// <remarks>
    /// The power-on state is:
    /// <list type="bullet">
    /// <item><description>80STORE disabled</description></item>
    /// <item><description>RAMRD disabled (reading from main RAM)</description></item>
    /// <item><description>RAMWRT disabled (writing to main RAM)</description></item>
    /// <item><description>ALTZP disabled (main zero page/stack)</description></item>
    /// <item><description>PAGE2 disabled (page 1 selected)</description></item>
    /// <item><description>HIRES disabled</description></item>
    /// </list>
    /// </remarks>
    public void Reset()
    {
        store80 = false;
        ramrd = false;
        ramwrt = false;
        altzp = false;
        page2 = false;
        hires = false;

        ApplyState();
    }

    // ─── Soft Switch Handlers ───────────────────────────────────────────
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

    private byte HandlePage1Read(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            page2 = false;
            ApplyState();
        }

        return 0xFF; // Floating bus
    }

    private void HandlePage1Write(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        page2 = false;
        ApplyState();
    }

    private byte HandlePage2Read(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            page2 = true;
            ApplyState();
        }

        return 0xFF; // Floating bus
    }

    private void HandlePage2Write(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        page2 = true;
        ApplyState();
    }

    private byte HandleLoResRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            hires = false;
            ApplyState();
        }

        return 0xFF; // Floating bus
    }

    private void HandleLoResWrite(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        hires = false;
        ApplyState();
    }

    private byte HandleHiResRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            hires = true;
            ApplyState();
        }

        return 0xFF; // Floating bus
    }

    private void HandleHiResWrite(byte offset, byte value, in BusAccess context)
    {
        if (context.IsSideEffectFree)
        {
            return;
        }

        hires = true;
        ApplyState();
    }

    // ─── State Management ───────────────────────────────────────────────

    /// <summary>
    /// Applies the current soft switch state to the memory bus layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Layer activation rules for hi-res pages (which are page-aligned and use layers):
    /// </para>
    /// <list type="bullet">
    /// <item><description>Hi-res pages are controlled by 80STORE AND HIRES AND PAGE2</description></item>
    /// </list>
    /// <para>
    /// Sub-page regions (zero page, stack, text page) are handled by
    /// <see cref="AuxiliaryMemoryPage0Target"/> which reads the controller state directly.
    /// </para>
    /// </remarks>
    private void ApplyState()
    {
        if (bus is null)
        {
            return;
        }

        // Hi-res pages controlled by 80STORE + HIRES + PAGE2
        // These are page-aligned (8KB each) so they use layers
        bool auxHires = store80 && hires && page2;
        SetLayerActive(LayerNameHiResPage1, auxHires);
        SetLayerActive(LayerNameHiResPage2, auxHires);
    }

    /// <summary>
    /// Sets the active state of a layer, handling the case where the layer may not exist.
    /// </summary>
    /// <param name="layerName">The name of the layer to activate or deactivate.</param>
    /// <param name="active">Whether the layer should be active.</param>
    private void SetLayerActive(string layerName, bool active)
    {
        if (bus is null)
        {
            return;
        }

        try
        {
            bool currentActive = bus.IsLayerActive(layerName);
            if (currentActive != active)
            {
                if (active)
                {
                    bus.ActivateLayer(layerName);
                }
                else
                {
                    bus.DeactivateLayer(layerName);
                }
            }
        }
        catch (KeyNotFoundException)
        {
            // Layer not found - this is expected in configurations without auxiliary memory
            // (e.g., basic Apple II without 80-column card) or during early initialization
            // when layers haven't been created yet. The controller gracefully handles this
            // by treating the layer as effectively deactivated.
        }
    }
}