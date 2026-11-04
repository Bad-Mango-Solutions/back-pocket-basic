// <copyright file="DskOrderSniffer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Sniffs the sector ordering of an ambiguous <c>.dsk</c> image per PRD §10 decision 5.
/// </summary>
/// <remarks>
/// <para>
/// Checks the DOS 3.3 VTOC location at track 17, sector 0 and also checks the ProDOS
/// root-directory key block at block 2 (file offset 1024 in a ProDOS-ordered image),
/// inspecting those inputs as appropriate for each ordering:
/// </para>
/// <list type="number">
/// <item><description>If interpreting track 17, sector 0 as DOS 3.3 yields a valid VTOC signature, choose <see cref="SectorOrder.Dos33"/>.</description></item>
/// <item><description>Otherwise, if interpreting the image as ProDOS yields a root-directory signature at the ProDOS sniffing locations, choose <see cref="SectorOrder.ProDos"/>.</description></item>
/// <item><description>Otherwise fall back to <see cref="SectorOrder.Dos33"/>.</description></item>
/// </list>
/// </remarks>
public static class DskOrderSniffer
{
    private const int VtocTrack = 17;
    private const int VtocSector = 0;

    /// <summary>
    /// Sniffs the sector order from a 35-track / 16-sector / 256-byte raw payload.
    /// </summary>
    /// <param name="payload">A buffer of at least 143360 bytes containing the raw sector image.</param>
    /// <param name="sniffed">Receives <see langword="true"/> if a positive identification was made; <see langword="false"/> if the fallback was used.</param>
    /// <returns>The chosen sector order.</returns>
    /// <exception cref="ArgumentException">If <paramref name="payload"/> is too short.</exception>
    public static SectorOrder Sniff(ReadOnlySpan<byte> payload, out bool sniffed)
    {
        const int payloadBytes = 35 * SectorSkew.SectorsPerTrack * GcrEncoder.BytesPerSector;
        if (payload.Length < payloadBytes)
        {
            throw new ArgumentException($"Payload must be at least {payloadBytes} bytes.", nameof(payload));
        }

        // Read the same physical (track 17, physical sector 0) as if the image were
        // each order in turn.
        var dosLogical = SectorSkew.PhysicalToLogical(SectorOrder.Dos33, VtocSector);
        var prodosLogical = SectorSkew.PhysicalToLogical(SectorOrder.ProDos, VtocSector);

        var dosOffset = ((VtocTrack * SectorSkew.SectorsPerTrack) + dosLogical) * GcrEncoder.BytesPerSector;
        var prodosOffset = ((VtocTrack * SectorSkew.SectorsPerTrack) + prodosLogical) * GcrEncoder.BytesPerSector;

        if (LooksLikeDosVtoc(payload.Slice(dosOffset, GcrEncoder.BytesPerSector)))
        {
            sniffed = true;
            return SectorOrder.Dos33;
        }

        // ProDOS root directory lives in block 2 = track 0 / sector 4+5 in physical
        // terms, but the standard sniff is to read the same fixed offset in the
        // backing image and look for the directory header.
        if (LooksLikeProDosRootDirectory(payload.Slice(prodosOffset, GcrEncoder.BytesPerSector))
            || LooksLikeProDosRootDirectoryAtBlock2(payload))
        {
            sniffed = true;
            return SectorOrder.ProDos;
        }

        sniffed = false;
        return SectorOrder.Dos33;
    }

    private static bool LooksLikeDosVtoc(ReadOnlySpan<byte> sector)
    {
        // DOS 3.3 VTOC, per "DOS 3.3 Reference":
        //   $00 unused (typically $04)
        //   $01 track of first catalog sector (typically 17 = $11)
        //   $02 sector of first catalog sector (typically 15 = $0F)
        //   $03 release number of DOS used to INIT (always 3)
        //   $06 disk volume number (1..254)
        //   $27 maximum number of track/sector pairs (typically $7A = 122)
        //   $34 number of tracks per disk (typically 35 = $23)
        //   $35 number of sectors per track (typically 16 = $10)
        //   $36-$37 number of bytes per sector ($00 $01 = 256, little-endian)
        if (sector[1] != 0x11)
        {
            return false;
        }

        if (sector[3] != 0x03)
        {
            return false;
        }

        if (sector[0x35] != 0x10)
        {
            return false;
        }

        if (sector[0x36] != 0x00 || sector[0x37] != 0x01)
        {
            return false;
        }

        return true;
    }

    private static bool LooksLikeProDosRootDirectoryAtBlock2(ReadOnlySpan<byte> proDosOrderedPayload)
    {
        // For a ProDOS-ordered .dsk, block 2 is the first half of track 0, sector 4
        // (ProDOS-logical), so file offset block 2 == 2 * 512 == 1024.
        const int rootBlockOffset = 2 * 512;
        if (proDosOrderedPayload.Length < rootBlockOffset + 512)
        {
            return false;
        }

        return LooksLikeProDosRootDirectory(proDosOrderedPayload.Slice(rootBlockOffset, 512));
    }

    private static bool LooksLikeProDosRootDirectory(ReadOnlySpan<byte> firstBytes)
    {
        // ProDOS volume directory key block layout (from ProDOS 8 Technical Reference):
        //   bytes 0..3   - prev / next pointers (prev is 0 for key block)
        //   byte  4      - storage_type (high nibble) | name_length (low nibble);
        //                  storage_type for a volume directory header is $F.
        //   bytes 5..19  - volume name (1..15 ASCII chars, name_length valid).
        if (firstBytes.Length < 20)
        {
            return false;
        }

        // prev pointer must be 0 for the volume directory key block.
        if (firstBytes[0] != 0 || firstBytes[1] != 0)
        {
            return false;
        }

        var typeAndLen = firstBytes[4];
        if ((typeAndLen & 0xF0) != 0xF0)
        {
            return false;
        }

        var nameLen = typeAndLen & 0x0F;
        if (nameLen == 0 || nameLen > 15)
        {
            return false;
        }

        for (var i = 0; i < nameLen; i++)
        {
            var c = firstBytes[5 + i];
            if (c is < (byte)'A' or > (byte)'Z')
            {
                if (!(c == (byte)'.' || (c >= (byte)'0' && c <= (byte)'9')))
                {
                    return false;
                }
            }
        }

        return true;
    }
}