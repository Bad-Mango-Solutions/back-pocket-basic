// <copyright file="VideoDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Video device handling video mode soft switches ($C050-$C057), annunciators ($C058-$C05F),
/// and status reads ($C019-$C01F).
/// </summary>
/// <remarks>
/// <para>
/// The Apple IIe video controller supports multiple display modes controlled
/// through soft switches:
/// </para>
/// <list type="bullet">
/// <item><description>$C050: TXTCLR - Graphics mode</description></item>
/// <item><description>$C051: TXTSET - Text mode</description></item>
/// <item><description>$C052: MIXCLR - Full screen</description></item>
/// <item><description>$C053: MIXSET - Mixed mode (4 lines of text)</description></item>
/// <item><description>$C054: LOWSCR - Page 1</description></item>
/// <item><description>$C055: HISCR - Page 2</description></item>
/// <item><description>$C056: LORES - Lo-res mode</description></item>
/// <item><description>$C057: HIRES - Hi-res mode</description></item>
/// <item><description>$C058-$C05F: Annunciator outputs (0-3 off/on)</description></item>
/// </list>
/// <para>
/// Additionally, status read registers are provided at $C019-$C01F:
/// </para>
/// <list type="bullet">
/// <item><description>$C019: RDVBL - Vertical blanking status (bit 7 = 0 during VBL)</description></item>
/// <item><description>$C01A: RDTEXT - Text mode status</description></item>
/// <item><description>$C01B: RDMIXED - Mixed mode status</description></item>
/// <item><description>$C01C: RDPAGE2 - Page 2 status</description></item>
/// <item><description>$C01D: RDHIRES - Hi-res mode status</description></item>
/// <item><description>$C01E: RDALTCHAR - Alternate character set status</description></item>
/// <item><description>$C01F: RD80COL - 80-column mode status</description></item>
/// </list>
/// <para>
/// Note: $C054-$C057 (PAGE2/HIRES switches) are handled by AuxiliaryMemoryController
/// because they affect both video display and memory banking. The
/// AuxiliaryMemoryController calls SetPage2() and SetHiRes() on this controller
/// to keep video state synchronized.
/// </para>
/// </remarks>
[DeviceType("video")]
public sealed class VideoDevice : IVideoModeDevice, ISoftSwitchProvider
{
    private const byte StatusBitSet = 0x80;
    private const byte StatusBitClear = 0x00;

    private readonly bool[] annunciators = new bool[4];
    private bool textMode = true;
    private bool mixedMode;
    private bool page2;
    private bool hiresMode;
    private bool col80Mode;
    private bool doubleHiResMode;
    private bool altCharSet;
    private bool verticalBlanking;

    /// <inheritdoc />
    public event Action<VideoMode>? ModeChanged;

    /// <inheritdoc />
    public string Name => "Video Device";

    /// <inheritdoc />
    public string DeviceType => "Video";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    /// <inheritdoc />
    public VideoMode CurrentMode => ComputeCurrentMode();

    /// <inheritdoc />
    public bool IsTextMode => textMode;

    /// <inheritdoc />
    public bool IsMixedMode => mixedMode;

    /// <inheritdoc />
    public bool IsPage2 => page2;

    /// <inheritdoc />
    public bool IsHiRes => hiresMode;

    /// <inheritdoc />
    public bool Is80Column => col80Mode;

    /// <inheritdoc />
    public bool IsDoubleHiRes => doubleHiResMode;

    /// <inheritdoc />
    public bool IsAltCharSet => altCharSet;

    /// <inheritdoc />
    public IReadOnlyList<bool> Annunciators => annunciators;

    /// <summary>
    /// Gets or sets a value indicating whether vertical blanking is in progress.
    /// </summary>
    /// <remarks>
    /// This is typically updated by the video timing subsystem.
    /// Reading $C019 returns bit 7 = 0 when VBL is active (inverted from other status reads).
    /// </remarks>
    public bool IsVerticalBlanking
    {
        get => verticalBlanking;
        set => verticalBlanking = value;
    }

    /// <inheritdoc />
    public string ProviderName => "Video";

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        // Video mode controller doesn't need scheduler access
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // Status reads ($C019-$C01F) - read-only
        dispatcher.RegisterRead(0x19, ReadVblStatus);       // RDVBL
        dispatcher.RegisterRead(0x1A, ReadTextStatus);      // RDTEXT
        dispatcher.RegisterRead(0x1B, ReadMixedStatus);     // RDMIXED
        dispatcher.RegisterRead(0x1C, ReadPage2Status);     // RDPAGE2
        dispatcher.RegisterRead(0x1D, ReadHiResStatus);     // RDHIRES
        dispatcher.RegisterRead(0x1E, ReadAltCharStatus);   // RDALTCHAR
        dispatcher.RegisterRead(0x1F, Read80ColStatus);     // RD80COL

        // Video mode switches
        dispatcher.Register(0x50, SetGraphicsRead, SetGraphicsWrite);   // TXTCLR
        dispatcher.Register(0x51, SetTextRead, SetTextWrite);           // TXTSET
        dispatcher.Register(0x52, ClearMixedRead, ClearMixedWrite);     // MIXCLR
        dispatcher.Register(0x53, SetMixedRead, SetMixedWrite);         // MIXSET

        // $C054-$C057 (PAGE2/HIRES switches) are handled by AuxiliaryMemoryController
        // because they affect both video display and memory banking. The
        // AuxiliaryMemoryController calls SetPage2() and SetHiRes() on this controller
        // to keep video state synchronized. See Phase 1.4 AuxiliaryMemoryController.

        // Annunciators ($C058-$C05F)
        for (byte i = 0; i < 8; i++)
        {
            byte offset = (byte)(0x58 + i);
            byte annIndex = (byte)(i / 2);
            bool setValue = (i & 1) != 0;
            dispatcher.Register(
                offset,
                (o, in ctx) => HandleAnnunciatorRead(annIndex, setValue, in ctx),
                (o, v, in ctx) => HandleAnnunciatorWrite(annIndex, setValue, in ctx));
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        textMode = true;
        mixedMode = false;
        page2 = false;
        hiresMode = false;
        col80Mode = false;
        doubleHiResMode = false;
        altCharSet = false;
        verticalBlanking = false;
        Array.Clear(annunciators);

        OnModeChanged();
    }

    /// <summary>
    /// Sets the 80-column mode state (typically controlled by 80-column card).
    /// </summary>
    /// <param name="enabled">Whether 80-column mode is enabled.</param>
    public void Set80ColumnMode(bool enabled)
    {
        if (col80Mode != enabled)
        {
            col80Mode = enabled;
            OnModeChanged();
        }
    }

    /// <summary>
    /// Sets the double hi-res mode state.
    /// </summary>
    /// <param name="enabled">Whether double hi-res mode is enabled.</param>
    public void SetDoubleHiResMode(bool enabled)
    {
        if (doubleHiResMode != enabled)
        {
            doubleHiResMode = enabled;
            OnModeChanged();
        }
    }

    /// <summary>
    /// Sets the alternate character set state.
    /// </summary>
    /// <param name="enabled">Whether alternate character set is enabled.</param>
    public void SetAltCharSet(bool enabled)
    {
        if (altCharSet != enabled)
        {
            altCharSet = enabled;
            OnModeChanged();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        return
        [

            // Status reads ($C019-$C01F)
            new("RDVBL", 0xC019, !verticalBlanking, "Vertical blanking (inverted: ON when NOT in VBL)"),
            new("RDTEXT", 0xC01A, textMode, "Text mode enabled"),
            new("RDMIXED", 0xC01B, mixedMode, "Mixed mode enabled"),
            new("RDPAGE2", 0xC01C, page2, "Page 2 selected"),
            new("RDHIRES", 0xC01D, hiresMode, "Hi-res mode enabled"),
            new("RDALTCHAR", 0xC01E, altCharSet, "Alternate character set enabled"),
            new("RD80COL", 0xC01F, col80Mode, "80-column mode enabled"),

            // Mode switches ($C050-$C057)
            new("TXTCLR", 0xC050, !textMode, "Graphics mode (text mode off)"),
            new("TXTSET", 0xC051, textMode, "Text mode"),
            new("MIXCLR", 0xC052, !mixedMode, "Full screen (mixed mode off)"),
            new("MIXSET", 0xC053, mixedMode, "Mixed mode (4 lines text at bottom)"),
            new("LOWSCR", 0xC054, !page2, "Page 1 selected"),
            new("HISCR", 0xC055, page2, "Page 2 selected"),
            new("LORES", 0xC056, !hiresMode, "Lo-res graphics mode"),
            new("HIRES", 0xC057, hiresMode, "Hi-res graphics mode"),

            // Annunciators ($C058-$C05F)
            new("AN0", 0xC058, !annunciators[0], "Annunciator 0 off"),
            new("AN0", 0xC059, annunciators[0], "Annunciator 0 on"),
            new("AN1", 0xC05A, !annunciators[1], "Annunciator 1 off"),
            new("AN1", 0xC05B, annunciators[1], "Annunciator 1 on"),
            new("AN2", 0xC05C, !annunciators[2], "Annunciator 2 off"),
            new("AN2", 0xC05D, annunciators[2], "Annunciator 2 on"),
            new("AN3", 0xC05E, !annunciators[3], "Annunciator 3 off"),
            new("AN3", 0xC05F, annunciators[3], "Annunciator 3 on"),

            // Additional mode controls
            new("80COL", 0xC00D, col80Mode, "80-column display mode"),
            new("ALTCHAR", 0xC00F, altCharSet, "Alternate character set"),
            new("DHIRES", 0xC05E, doubleHiResMode, "Double hi-res mode"),
        ];
    }

    /// <summary>
    /// Sets the page 2 selection state (called by AuxiliaryMemoryController).
    /// </summary>
    /// <param name="selected">Whether page 2 is selected.</param>
    internal void SetPage2(bool selected)
    {
        if (page2 != selected)
        {
            page2 = selected;
            OnModeChanged();
        }
    }

    /// <summary>
    /// Sets the hi-res mode state (called by AuxiliaryMemoryController).
    /// </summary>
    /// <param name="enabled">Whether hi-res mode is enabled.</param>
    internal void SetHiRes(bool enabled)
    {
        if (hiresMode != enabled)
        {
            hiresMode = enabled;
            OnModeChanged();
        }
    }

    private byte SetGraphicsRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetTextModeInternal(false);
        }

        return 0xFF;
    }

    private void SetGraphicsWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetTextModeInternal(false);
        }
    }

    private byte SetTextRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetTextModeInternal(true);
        }

        return 0xFF;
    }

    private void SetTextWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetTextModeInternal(true);
        }
    }

    private byte ClearMixedRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetMixedModeInternal(false);
        }

        return 0xFF;
    }

    private void ClearMixedWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetMixedModeInternal(false);
        }
    }

    private byte SetMixedRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetMixedModeInternal(true);
        }

        return 0xFF;
    }

    private void SetMixedWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            SetMixedModeInternal(true);
        }
    }

    private byte HandleAnnunciatorRead(byte annIndex, bool setValue, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            annunciators[annIndex] = setValue;
        }

        return 0xFF;
    }

    private void HandleAnnunciatorWrite(byte annIndex, bool setValue, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            annunciators[annIndex] = setValue;
        }
    }

    /// <summary>
    /// Reads vertical blanking status ($C019).
    /// </summary>
    /// <remarks>
    /// Unlike other status flags, RDVBL returns bit 7 = 0 during vertical blanking
    /// and bit 7 = 1 when NOT in vertical blanking. This is inverted from the
    /// convention used by other status reads.
    /// </remarks>
    private byte ReadVblStatus(byte offset, in BusAccess context)
    {
        // Note: RDVBL is inverted - bit 7 = 0 during VBL, bit 7 = 1 when NOT in VBL
        return verticalBlanking ? StatusBitClear : StatusBitSet;
    }

    /// <summary>
    /// Reads text mode status ($C01A).
    /// </summary>
    private byte ReadTextStatus(byte offset, in BusAccess context)
    {
        return textMode ? StatusBitSet : StatusBitClear;
    }

    /// <summary>
    /// Reads mixed mode status ($C01B).
    /// </summary>
    private byte ReadMixedStatus(byte offset, in BusAccess context)
    {
        return mixedMode ? StatusBitSet : StatusBitClear;
    }

    /// <summary>
    /// Reads page 2 status ($C01C).
    /// </summary>
    private byte ReadPage2Status(byte offset, in BusAccess context)
    {
        return page2 ? StatusBitSet : StatusBitClear;
    }

    /// <summary>
    /// Reads hi-res mode status ($C01D).
    /// </summary>
    private byte ReadHiResStatus(byte offset, in BusAccess context)
    {
        return hiresMode ? StatusBitSet : StatusBitClear;
    }

    /// <summary>
    /// Reads alternate character set status ($C01E).
    /// </summary>
    private byte ReadAltCharStatus(byte offset, in BusAccess context)
    {
        return altCharSet ? StatusBitSet : StatusBitClear;
    }

    /// <summary>
    /// Reads 80-column mode status ($C01F).
    /// </summary>
    private byte Read80ColStatus(byte offset, in BusAccess context)
    {
        return col80Mode ? StatusBitSet : StatusBitClear;
    }

    private void SetTextModeInternal(bool enabled)
    {
        if (textMode != enabled)
        {
            textMode = enabled;
            OnModeChanged();
        }
    }

    private void SetMixedModeInternal(bool enabled)
    {
        if (mixedMode != enabled)
        {
            mixedMode = enabled;
            OnModeChanged();
        }
    }

    private void OnModeChanged()
    {
        ModeChanged?.Invoke(CurrentMode);
    }

    private VideoMode ComputeCurrentMode()
    {
        if (textMode)
        {
            return col80Mode ? VideoMode.Text80 : VideoMode.Text40;
        }

        if (hiresMode)
        {
            if (doubleHiResMode)
            {
                return mixedMode ? VideoMode.DoubleHiResMixed : VideoMode.DoubleHiRes;
            }

            return mixedMode ? VideoMode.HiResMixed : VideoMode.HiRes;
        }

        // Lo-res
        if (col80Mode)
        {
            return mixedMode ? VideoMode.DoubleLoResMixed : VideoMode.DoubleLoRes;
        }

        return mixedMode ? VideoMode.LoResMixed : VideoMode.LoRes;
    }
}