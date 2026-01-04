// <copyright file="VideoModeController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Video mode soft switches ($C050-$C057) and annunciators ($C058-$C05F).
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
/// Note: $C054-$C057 overlap with auxiliary memory controller switches.
/// The video mode state is maintained here for display purposes.
/// </para>
/// </remarks>
public sealed class VideoModeController : IVideoModeDevice
{
    private readonly bool[] annunciators = new bool[4];
    private bool textMode = true;
    private bool mixedMode;
    private bool page2;
    private bool hiresMode;
    private bool col80Mode;
    private bool doubleHiResMode;
    private bool altCharSet;

    /// <inheritdoc />
    public event Action<VideoMode>? ModeChanged;

    /// <inheritdoc />
    public string Name => "Video Mode Controller";

    /// <inheritdoc />
    public string DeviceType => "VideoMode";

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

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        // Video mode controller doesn't need scheduler access
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

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