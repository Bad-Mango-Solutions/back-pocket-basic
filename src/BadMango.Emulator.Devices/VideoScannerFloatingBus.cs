// <copyright file="VideoScannerFloatingBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using BadMango.Emulator.Core;

using Interfaces;

/// <summary>
/// Computes Apple II video-scanner addresses for floating-bus reads.
/// </summary>
/// <remarks>
/// Ported from AppleWin <c>NTSC_VideoGetScannerAddress</c>, <c>getVideoScannerAddressTXT</c>,
/// and <c>MemReadFloatingBus</c> (scanner address + physical main-memory read).
/// ProDOS 2.0.3 language-card snapshot/decrypt polls <c>$C08C,X</c> expecting
/// these bytes, not raw Disk II track nibbles.
/// </remarks>
internal static class VideoScannerFloatingBus
{
    private const int HorzClocks = 65;
    private const int NtscScanLines = 262;
    private const int MixedTextScanline = 160;

    // AppleWin kAPPLE_IIE_HORZ_CLOCK_OFFSET (NTSC, Enhanced //e).
    private static readonly ushort[,] AppleIIeHorzClockOffset =
    {
        {
            0x0068, 0x0068, 0x0069, 0x006A, 0x006B, 0x006C, 0x006D, 0x006E, 0x006F,
            0x0070, 0x0071, 0x0072, 0x0073, 0x0074, 0x0075, 0x0076, 0x0077,
            0x0078, 0x0079, 0x007A, 0x007B, 0x007C, 0x007D, 0x007E, 0x007F,
            0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007,
            0x0008, 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x000E, 0x000F,
            0x0010, 0x0011, 0x0012, 0x0013, 0x0014, 0x0015, 0x0016, 0x0017,
            0x0018, 0x0019, 0x001A, 0x001B, 0x001C, 0x001D, 0x001E, 0x001F,
            0x0020, 0x0021, 0x0022, 0x0023, 0x0024, 0x0025, 0x0026, 0x0027,
        },
        {
            0x0010, 0x0010, 0x0011, 0x0012, 0x0013, 0x0014, 0x0015, 0x0016, 0x0017,
            0x0018, 0x0019, 0x001A, 0x001B, 0x001C, 0x001D, 0x001E, 0x001F,
            0x0020, 0x0021, 0x0022, 0x0023, 0x0024, 0x0025, 0x0026, 0x0027,
            0x0028, 0x0029, 0x002A, 0x002B, 0x002C, 0x002D, 0x002E, 0x002F,
            0x0030, 0x0031, 0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037,
            0x0038, 0x0039, 0x003A, 0x003B, 0x003C, 0x003D, 0x003E, 0x003F,
            0x0040, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047,
            0x0048, 0x0049, 0x004A, 0x004B, 0x004C, 0x004D, 0x004E, 0x004F,
        },
        {
            0x0038, 0x0038, 0x0039, 0x003A, 0x003B, 0x003C, 0x003D, 0x003E, 0x003F,
            0x0040, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047,
            0x0048, 0x0049, 0x004A, 0x004B, 0x004C, 0x004D, 0x004E, 0x004F,
            0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056, 0x0057,
            0x0058, 0x0059, 0x005A, 0x005B, 0x005C, 0x005D, 0x005E, 0x005F,
            0x0060, 0x0061, 0x0062, 0x0063, 0x0064, 0x0065, 0x0066, 0x0067,
            0x0068, 0x0069, 0x006A, 0x006B, 0x006C, 0x006D, 0x006E, 0x006F,
            0x0070, 0x0071, 0x0072, 0x0073, 0x0074, 0x0075, 0x0076, 0x0077,
        },
        {
            0x0060, 0x0060, 0x0061, 0x0062, 0x0063, 0x0064, 0x0065, 0x0066, 0x0067,
            0x0068, 0x0069, 0x006A, 0x006B, 0x006C, 0x006D, 0x006E, 0x006F,
            0x0070, 0x0071, 0x0072, 0x0073, 0x0074, 0x0075, 0x0076, 0x0077,
            0x0078, 0x0079, 0x007A, 0x007B, 0x007C, 0x007D, 0x007E, 0x007F,
            0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007,
            0x0008, 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x000E, 0x000F,
            0x0010, 0x0011, 0x0012, 0x0013, 0x0014, 0x0015, 0x0016, 0x0017,
            0x0018, 0x0019, 0x001A, 0x001B, 0x001C, 0x001D, 0x001E, 0x001F,
        },
        {
            0x0060, 0x0060, 0x0061, 0x0062, 0x0063, 0x0064, 0x0065, 0x0066, 0x0067,
            0x0068, 0x0069, 0x006A, 0x006B, 0x006C, 0x006D, 0x006E, 0x006F,
            0x0070, 0x0071, 0x0072, 0x0073, 0x0074, 0x0075, 0x0076, 0x0077,
            0x0078, 0x0079, 0x007A, 0x007B, 0x007C, 0x007D, 0x007E, 0x007F,
            0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007,
            0x0008, 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x000E, 0x000F,
            0x0010, 0x0011, 0x0012, 0x0013, 0x0014, 0x0015, 0x0016, 0x0017,
            0x0018, 0x0019, 0x001A, 0x001B, 0x001C, 0x001D, 0x001E, 0x001F,
        },
    };

    // AppleWin g_kClockVertOffsetsTXT (NTSC).
    private static readonly ushort[] ClockVertOffsetsText =
    [
        0x0000, 0x0080, 0x0100, 0x0180, 0x0200, 0x0280, 0x0300, 0x0380,
        0x0000, 0x0080, 0x0100, 0x0180, 0x0200, 0x0280, 0x0300, 0x0380,
        0x0000, 0x0080, 0x0100, 0x0180, 0x0200, 0x0280, 0x0300, 0x0380,
        0x0000, 0x0080, 0x0100, 0x0180, 0x0200, 0x0280, 0x0300, 0x0380,
        0x0380,
    ];

    /// <summary>
    /// Returns the byte currently on the Apple II floating bus for the given video state.
    /// </summary>
    /// <param name="cyclesThisFrame">CPU cycles elapsed in the current video frame.</param>
    /// <param name="textMode">Whether text mode is active.</param>
    /// <param name="mixedMode">Whether mixed mode is active.</param>
    /// <param name="page2">Whether display page 2 is selected.</param>
    /// <param name="hiresMode">Whether hi-res mode is active.</param>
    /// <param name="page2Via80Store">Whether PAGE2 is routed through 80STORE (IIe only).</param>
    /// <param name="bus">Bus used to read the scanner address when no main-memory provider is available.</param>
    /// <param name="mainMemory">
    /// Optional physical main RAM provider. AppleWin <c>ReadFloatingBus</c> reads
    /// <c>pMain[addr]</c> directly so 80STORE/PAGE2/RAMRD cannot redirect scanner data.
    /// </param>
    /// <returns>The floating-bus byte, or <c>0xFF</c> when neither provider nor bus is available.</returns>
    public static byte Read(
        ulong cyclesThisFrame,
        bool textMode,
        bool mixedMode,
        bool page2,
        bool hiresMode,
        bool page2Via80Store,
        IMemoryBus? bus,
        IMainMemoryProvider? mainMemory = null)
    {
        Addr address = GetScannerAddress(
            cyclesThisFrame,
            textMode,
            mixedMode,
            page2,
            hiresMode,
            page2Via80Store);

        if (mainMemory is not null)
        {
            return mainMemory.ReadMainRam((ushort)address);
        }

        if (bus is null)
        {
            return 0xFF;
        }

        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Atomic,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);
        return bus.Read8(in access);
    }

    private static Addr GetScannerAddress(
        ulong cyclesThisFrame,
        bool textMode,
        bool mixedMode,
        bool page2,
        bool hiresMode,
        bool page2Via80Store)
    {
        int scanCycles = NtscScanLines * HorzClocks;
        int nCycles = (int)(cyclesThisFrame % (ulong)scanCycles);

        int vert = nCycles / HorzClocks;
        int horz = nCycles % HorzClocks;
        ApplyReadLatency(ref vert, ref horz);

        bool useTextAddress = (mixedMode && vert >= MixedTextScanline) || textMode || !hiresMode;
        if (useTextAddress)
        {
            return GetTextScannerAddress(vert, horz, page2, page2Via80Store);
        }

        return GetHiresScannerAddressAlgorithmic(nCycles, mixedMode, page2, hiresMode, page2Via80Store);
    }

    private static void ApplyReadLatency(ref int vert, ref int horz)
    {
        horz--;
        if (horz < 0)
        {
            horz += HorzClocks;
            vert--;
            if (vert < 0)
            {
                vert = NtscScanLines - 1;
            }
        }
    }

    private static Addr GetTextScannerAddress(int vert, int horz, bool page2, bool page2Via80Store)
    {
        // AppleWin NTSC_SetVideoMode: PAGE2 selects text page 2 only when 80STORE is off.
        int textPage = page2 && !page2Via80Store ? 2 : 1;
        ushort vertOffset = ClockVertOffsetsText[vert / 8];
        ushort horzOffset = AppleIIeHorzClockOffset[vert / 64, horz];
        return (Addr)(vertOffset + horzOffset + (textPage * 0x400));
    }

    // Algorithmic hires path retained for non-text modes; text uses AppleWin lookup tables.
    private static Addr GetHiresScannerAddressAlgorithmic(
        int nCycles,
        bool mixedMode,
        bool page2,
        bool hiresMode,
        bool page2Via80Store)
    {
        const int HpeClock = 40;
        const int HClock0State = 0x18;
        const int HPresetClock = 41;
        const int VLine0State = 0x100;
        const int VPresetLine = 256;

        int nHClock = (nCycles + HpeClock) % HorzClocks;
        int nHState = HClock0State + nHClock;
        if (nHClock >= HPresetClock)
        {
            nHState -= 1;
        }

        int h0 = (nHState >> 0) & 1;
        int h1 = (nHState >> 1) & 1;
        int h2 = (nHState >> 2) & 1;
        int h3 = (nHState >> 3) & 1;
        int h4 = (nHState >> 4) & 1;
        int h5 = (nHState >> 5) & 1;

        int nVLine = nCycles / HorzClocks;
        int nVState = VLine0State + nVLine;
        if (nVLine >= VPresetLine)
        {
            nVState -= NtscScanLines;
        }

        int vA = (nVState >> 0) & 1;
        int vB = (nVState >> 1) & 1;
        int vC = (nVState >> 2) & 1;
        int v0 = (nVState >> 3) & 1;
        int v1 = (nVState >> 4) & 1;
        int v2 = (nVState >> 5) & 1;
        int v3 = (nVState >> 6) & 1;
        int v4 = (nVState >> 7) & 1;

        bool hires = hiresMode;
        if (hires && mixedMode && v4 != 0 && v2 != 0)
        {
            hires = false;
        }

        int nAddend0 = 0x0D;
        int nAddend1 = (h5 << 2) | (h4 << 1) | h3;
        int nAddend2 = (v4 << 3) | (v3 << 2) | (v4 << 1) | v3;
        int nSum = (nAddend0 + nAddend1 + nAddend2) & 0x0F;

        Addr nAddressH = 0;
        nAddressH |= (Addr)(h0 << 0);
        nAddressH |= (Addr)(h1 << 1);
        nAddressH |= (Addr)(h2 << 2);
        nAddressH |= (Addr)(nSum << 3);

        Addr nAddressV = 0;
        nAddressV |= (Addr)(v0 << 7);
        nAddressV |= (Addr)(v1 << 8);
        nAddressV |= (Addr)(v2 << 9);

        int p2a = page2 && !page2Via80Store ? 0 : 1;
        int p2b = page2 && !page2Via80Store ? 1 : 0;

        Addr nAddressP = 0;
        if (hires)
        {
            nAddressV |= (Addr)(vA << 10);
            nAddressV |= (Addr)(vB << 11);
            nAddressV |= (Addr)(vC << 12);
            nAddressP |= (Addr)(p2a << 13);
            nAddressP |= (Addr)(p2b << 14);
        }
        else
        {
            nAddressP |= (Addr)(p2a << 10);
            nAddressP |= (Addr)(p2b << 11);
        }

        return nAddressP | nAddressV | nAddressH;
    }
}