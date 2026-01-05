// <copyright file="LanguageCardController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Controls Language Card bank switching using layer and swap group APIs.
/// </summary>
/// <remarks>
/// <para>
/// The Language Card provides 16KB of RAM ($D000-$FFFF) that can overlay ROM.
/// It consists of:
/// </para>
/// <list type="bullet">
/// <item><description>Two 4KB banks for $D000-$DFFF (Bank 1 and Bank 2)</description></item>
/// <item><description>One 8KB bank for $E000-$FFFF</description></item>
/// </list>
/// <para>
/// The controller uses the layered mapping API to overlay RAM over ROM, and the
/// swap group API to switch between the two banks at $D000-$DFFF.
/// </para>
/// <para>
/// Bank selection and RAM read/write enable are controlled through 16 soft switches
/// at $C080-$C08F. The R×2 (double-read) protocol provides write-enable protection:
/// write enable requires two consecutive reads of the same odd address.
/// </para>
/// </remarks>
public sealed class LanguageCardController : IScheduledDevice
{
    /// <summary>
    /// The name of the Language Card RAM layer.
    /// </summary>
    public const string LayerName = "LC_RAM";

    /// <summary>
    /// The name of the swap group for the $D000-$DFFF bank switching.
    /// </summary>
    public const string SwapGroupName = "LC_D000_BANK";

    /// <summary>
    /// The name of the ROM variant (used when RAM read is disabled).
    /// </summary>
    public const string RomVariantName = "ROM";

    /// <summary>
    /// The name of the Bank 1 variant.
    /// </summary>
    public const string Bank1VariantName = "BANK1";

    /// <summary>
    /// The name of the Bank 2 variant.
    /// </summary>
    public const string Bank2VariantName = "BANK2";

    /// <summary>
    /// The layer priority for the Language Card layer.
    /// </summary>
    public const int LayerPriority = 20;

    /// <summary>
    /// The base address for Language Card soft switches.
    /// </summary>
    private const byte SwitchBaseOffset = 0x80;

    /// <summary>
    /// The number of soft switches controlled by the Language Card.
    /// </summary>
    private const int SwitchCount = 16;

    private readonly SlotIOHandlers handlers;
    private IMemoryBus? bus;
    private uint bankSwapGroupId;

    private bool readRam;           // True = read from LC RAM
    private bool writeEnabled;      // True = writes go to LC RAM
    private bool bank2Selected;     // True = $D000 bank 2
    private bool preWrite;          // R×2 protocol state
    private byte lastReadOffset;    // Last read offset for R×2

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageCardController"/> class.
    /// </summary>
    public LanguageCardController()
    {
        handlers = new SlotIOHandlers();
        InitializeHandlers();
    }

    // ─── Properties ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public string Name => "Language Card Controller";

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

    // ─── IScheduledDevice ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        bus = context.Bus;

        // Get the swap group ID (created during machine build)
        try
        {
            bankSwapGroupId = bus.GetSwapGroupId(SwapGroupName);
        }
        catch (KeyNotFoundException)
        {
            // Swap group not created - this is a configuration error
            throw new InvalidOperationException(
                $"Language Card swap group '{SwapGroupName}' not found. " +
                $"Ensure the swap group is created during machine initialization.");
        }

        // Set initial state: RAM disabled, Bank 2 selected (default power-on state)
        readRam = false;
        writeEnabled = false;
        bank2Selected = true;
        preWrite = false;
        lastReadOffset = 0;

        ApplyState();
    }

    /// <summary>
    /// Resets the Language Card controller to its power-on state.
    /// </summary>
    /// <remarks>
    /// The power-on state is:
    /// <list type="bullet">
    /// <item><description>RAM read disabled (ROM visible)</description></item>
    /// <item><description>RAM write disabled</description></item>
    /// <item><description>Bank 2 selected</description></item>
    /// </list>
    /// </remarks>
    public void Reset()
    {
        readRam = false;
        writeEnabled = false;
        bank2Selected = true;
        preWrite = false;
        lastReadOffset = 0;

        ApplyState();
    }

    // ─── Private Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the soft switch handlers for $C080-$C08F.
    /// </summary>
    private void InitializeHandlers()
    {
        for (byte i = 0; i < SwitchCount; i++)
        {
            byte offset = i;
            handlers.Set(i, (o, in ctx) => HandleRead(offset, in ctx), (o, v, in ctx) => HandleWrite(offset, in ctx));
        }
    }

    /// <summary>
    /// Handles a read access to a Language Card soft switch.
    /// </summary>
    /// <param name="offset">The offset within the soft switch range (0-15).</param>
    /// <param name="context">The bus access context.</param>
    /// <returns>The floating bus value (soft switches don't return meaningful data).</returns>
    private byte HandleRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            ProcessSwitch(offset, isRead: true);
        }

        // Language Card soft switches return floating bus value
        return 0xFF;
    }

    /// <summary>
    /// Handles a write access to a Language Card soft switch.
    /// </summary>
    /// <param name="offset">The offset within the soft switch range (0-15).</param>
    /// <param name="context">The bus access context.</param>
    private void HandleWrite(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            ProcessSwitch(offset, isRead: false);
        }
    }

    /// <summary>
    /// Processes a soft switch access and updates internal state.
    /// </summary>
    /// <param name="offset">The offset within the soft switch range (0-15).</param>
    /// <param name="isRead">True if this is a read access, false for write.</param>
    /// <remarks>
    /// <para>
    /// The Language Card soft switch encoding:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Bit 0: Write enable address (1 = write enable possible with R×2)</description></item>
    /// <item><description>Bits 0 and 1: Read source - RAM when bits are equal (00 or 11), ROM when different (01 or 10)</description></item>
    /// <item><description>Bit 3: Bank select (0 = Bank 2, 1 = Bank 1)</description></item>
    /// </list>
    /// <para>
    /// The R×2 protocol requires two consecutive reads of the same odd address
    /// to enable writes. This prevents accidental write enabling.
    /// </para>
    /// </remarks>
    private void ProcessSwitch(byte offset, bool isRead)
    {
        bool isWriteEnableAddr = (offset & 0x01) != 0;

        if (isRead && isWriteEnableAddr)
        {
            // R×2 protocol: requires two consecutive reads of the same odd address
            if (preWrite && offset == lastReadOffset)
            {
                // Second consecutive read of same odd address - enable write
                writeEnabled = true;
                preWrite = false;
            }
            else if (!writeEnabled)
            {
                // First read of odd address (or different odd address) - prime the R×2 protocol
                // When write is already enabled, reading a different odd address maintains
                // the write-enabled state rather than resetting it. This matches the Apple II
                // behavior where once write is enabled, only an even address read disables it.
                preWrite = true;
                lastReadOffset = offset;
            }
            else
            {
                // Write already enabled, reading odd address keeps it enabled but clears preWrite
                // (reading different odd address after write is enabled should not disable write)
                preWrite = false;
            }
        }
        else if (isRead)
        {
            // Reading an even address disables write and clears R×2 state
            preWrite = false;
            writeEnabled = false;
        }

        // Writes (regardless of address) do not affect write enable, but they
        // do clear the R×2 state
        if (!isRead)
        {
            preWrite = false;
        }

        // Decode state from offset bits
        // RAM read is enabled when bits 0 and 1 are the same (both 0 or both 1)
        // This gives: $C080=RAM, $C081=ROM, $C082=ROM, $C083=RAM (and mirrors)
        bool bit0 = (offset & 0x01) != 0;
        bool bit1 = (offset & 0x02) != 0;
        readRam = bit0 == bit1;

        // Bank 2 is selected when bit 3 is 0
        bank2Selected = (offset & 0x08) == 0;

        ApplyState();
    }

    /// <summary>
    /// Applies the current state to the memory bus layers and swap groups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Language Card RAM has two regions with different switching mechanisms:
    /// </para>
    /// <list type="bullet">
    /// <item><description>D000-DFFF: Handled by swap group (Bank1/Bank2 switching)</description></item>
    /// <item><description>E000-FFFF: Handled by layer (ROM/RAM switching)</description></item>
    /// </list>
    /// <para>
    /// When RAM read is enabled, the layer is activated for E000-FFFF and the
    /// swap group selects the appropriate bank for D000-DFFF. When RAM read is
    /// disabled, the layer is deactivated and the ROM variant is selected for D000.
    /// </para>
    /// </remarks>
    private void ApplyState()
    {
        if (bus is null)
        {
            return;
        }

        // Handle the E000-FFFF layer and D000 swap group together
        if (readRam)
        {
            // Activate the layer for E000-FFFF
            if (!bus.IsLayerActive(LayerName))
            {
                bus.ActivateLayer(LayerName);
            }

            // Select the appropriate bank variant for D000-DFFF
            string variantName = bank2Selected ? Bank2VariantName : Bank1VariantName;
            if (bus.GetActiveSwapVariant(bankSwapGroupId) != variantName)
            {
                bus.SelectSwapVariant(bankSwapGroupId, variantName);
            }

            // Update layer permissions based on write enable state
            // Note: Swap group variant permissions are set when adding variants
            PagePerms perms = writeEnabled ? PagePerms.All : PagePerms.ReadExecute;
            bus.SetLayerPermissions(LayerName, perms);
        }
        else
        {
            // Deactivate the layer for E000-FFFF
            if (bus.IsLayerActive(LayerName))
            {
                bus.DeactivateLayer(LayerName);
            }

            // Select ROM variant for D000-DFFF
            if (bus.GetActiveSwapVariant(bankSwapGroupId) != RomVariantName)
            {
                bus.SelectSwapVariant(bankSwapGroupId, RomVariantName);
            }
        }
    }
}