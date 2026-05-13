// <copyright file="DiskImageFactory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Formats;

using BadMango.Emulator.Storage.Backends;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Opens a disk image file and returns a strongly-typed pattern-matchable
/// <see cref="DiskImageOpenResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// Per PRD §6.1 FR-S4 and FR-S5, format detection is by extension first and then by
/// magic-byte sniffing for headered formats:
/// </para>
/// <list type="bullet">
/// <item><description><c>.dsk</c> — sector image; ordering sniffed per §10 decision 5.</description></item>
/// <item><description><c>.do</c> — DOS 3.3 sector image.</description></item>
/// <item><description><c>.po</c> — ProDOS sector image.</description></item>
/// <item><description><c>.2mg</c>/<c>.2img</c> — 2MG-wrapped DOS / ProDOS / nibble payload.</description></item>
/// <item><description><c>.nib</c> — raw nibble image.</description></item>
/// <item><description><c>.hdv</c> — raw 512-byte block image (hard disk).</description></item>
/// <item><description><c>.d13</c> — recognised and refused with a clear error.</description></item>
/// <item><description><c>.woz</c> — out of scope here (PRD row 14).</description></item>
/// </list>
/// </remarks>
public class DiskImageFactory
{
    private const int FivePointTwoFiveStandardBytes = 35 * SectorSkew.SectorsPerTrack * GcrEncoder.BytesPerSector; // 143360
    private const int D13StandardBytes = 35 * 13 * GcrEncoder.BytesPerSector; // 116480
    private const int NibStandardBytes = 35 * GcrEncoder.StandardTrackLength; // 232960

    /// <summary>
    /// Opens the supplied image file.
    /// </summary>
    /// <param name="path">Path to the disk image; must exist.</param>
    /// <param name="forceReadOnly">If <see langword="true"/>, the image is opened read-only regardless of file permissions.</param>
    /// <returns>A pattern-matchable result describing the chosen format and exposing the appropriate media views.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="NotSupportedException">If the format is recognised as unsupported (e.g. <c>.d13</c>) or is not implemented (e.g. <c>.woz</c>).</exception>
    /// <exception cref="InvalidDataException">If the file's contents do not match a supported format.</exception>
    public virtual DiskImageOpenResult Open(string path, bool forceReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Disk image not found.", path);
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".d13" => throw new NotSupportedException("13-sector .d13 images are recognised but not supported."),
            ".woz" => throw new NotSupportedException(".woz images are out of scope for this assembly (PRD row 14)."),
            ".nib" => this.OpenNib(path, forceReadOnly),
            ".hdv" => this.OpenHdv(path, forceReadOnly),
            ".do" => this.OpenSectorImage(path, SectorOrder.Dos33, sniffed: false, DiskImageFormat.Dos33SectorImage, forceReadOnly),
            ".po" => this.OpenSectorImage(path, SectorOrder.ProDos, sniffed: false, DiskImageFormat.ProDosSectorImage, forceReadOnly),
            ".dsk" => this.OpenDsk(path, forceReadOnly),
            ".2mg" or ".2img" => this.OpenTwoImg(path, forceReadOnly),
            _ => this.OpenByMagic(path, forceReadOnly),
        };
    }

    /// <summary>
    /// Creates a writable file-backed storage backend for the given image path.
    /// </summary>
    /// <remarks>
    /// Exposed as a virtual seam so that tests can substitute a RAM-backed store.
    /// </remarks>
    /// <param name="path">Image file path.</param>
    /// <param name="readOnly">Whether to open read-only.</param>
    /// <returns>A new <see cref="IStorageBackend"/> over the file.</returns>
    protected virtual IStorageBackend OpenBackend(string path, bool readOnly)
        => new FileStorageBackend(path, readOnly);

    private static byte[] PeekFirstBytes(string path, int count)
    {
        using var fs = File.OpenRead(path);
        var len = (int)Math.Min(count, fs.Length);
        var buf = new byte[len];
        var read = 0;
        while (read < len)
        {
            var n = fs.Read(buf.AsSpan(read));
            if (n <= 0)
            {
                break;
            }

            read += n;
        }

        return buf;
    }

    private DiskImageOpenResult OpenByMagic(string path, bool forceReadOnly)
    {
        var head = PeekFirstBytes(path, 4);
        if (head.Length >= 4 && head.AsSpan(0, 4).SequenceEqual(TwoImgHeader.Magic))
        {
            return this.OpenTwoImg(path, forceReadOnly);
        }

        // Sniff by length as a last resort.
        var length = new FileInfo(path).Length;
        if (length == FivePointTwoFiveStandardBytes)
        {
            return this.OpenDsk(path, forceReadOnly);
        }

        if (length == NibStandardBytes)
        {
            return this.OpenNib(path, forceReadOnly);
        }

        if (length == D13StandardBytes)
        {
            throw new NotSupportedException("13-sector .d13 images are recognised but not supported.");
        }

        if (length > 0 && length % 512 == 0)
        {
            return this.OpenHdv(path, forceReadOnly);
        }

        throw new InvalidDataException($"Could not identify disk image format for '{path}'.");
    }

    private DiskImageOpenResult OpenSectorImage(string path, SectorOrder order, bool sniffed, DiskImageFormat format, bool forceReadOnly)
    {
        var backend = this.OpenBackend(path, forceReadOnly);
        var length = backend.Length;
        if (length != FivePointTwoFiveStandardBytes)
        {
            backend.Dispose();
            throw new InvalidDataException($"5.25\" sector image must be {FivePointTwoFiveStandardBytes} bytes; '{path}' is {length}.");
        }

        var geometry = new DiskGeometry(35, SectorSkew.SectorsPerTrack, GcrEncoder.BytesPerSector, order);
        var media = new SectorImageMedia(backend, geometry, backingOffset: 0, writeProtected: forceReadOnly);
        return new Image525AndBlockResult(media.As525Media(), media.AsBlockMedia(), order, sniffed, format, path, media.IsReadOnly);
    }

    private DiskImageOpenResult OpenDsk(string path, bool forceReadOnly)
    {
        // .dsk: ambiguous order; sniff first.
        var preview = PeekFirstBytes(path, FivePointTwoFiveStandardBytes);
        if (preview.Length != FivePointTwoFiveStandardBytes)
        {
            throw new InvalidDataException($"5.25\" .dsk must be {FivePointTwoFiveStandardBytes} bytes; '{path}' is {preview.Length}.");
        }

        var order = DskOrderSniffer.Sniff(preview, out var sniffed);
        var format = order == SectorOrder.ProDos ? DiskImageFormat.ProDosSectorImage : DiskImageFormat.Dos33SectorImage;
        return this.OpenSectorImage(path, order, sniffed, format, forceReadOnly);
    }

    private DiskImageOpenResult OpenNib(string path, bool forceReadOnly)
    {
        var backend = this.OpenBackend(path, forceReadOnly);
        if (backend.Length % GcrEncoder.StandardTrackLength != 0 || backend.Length == 0)
        {
            backend.Dispose();
            throw new InvalidDataException($".nib image length must be a positive multiple of {GcrEncoder.StandardTrackLength}; '{path}' is {backend.Length}.");
        }

        var trackCount = (int)(backend.Length / GcrEncoder.StandardTrackLength);
        var media = new NibbleImageMedia(backend, trackCount, backingOffset: 0, writeProtected: forceReadOnly);
        return new Image525Result(media, DiskImageFormat.NibbleImage, path, media.IsReadOnly);
    }

    private DiskImageOpenResult OpenHdv(string path, bool forceReadOnly)
    {
        var backend = this.OpenBackend(path, forceReadOnly);
        if (backend.Length == 0 || backend.Length % 512 != 0)
        {
            backend.Dispose();
            throw new InvalidDataException($".hdv image length must be a positive multiple of 512; '{path}' is {backend.Length}.");
        }

        var blockCount = (int)(backend.Length / 512);
        var media = new BlockImageMedia(backend, blockCount, blockSize: 512, backingOffset: 0, writeProtected: forceReadOnly);
        return new ImageBlockResult(media, DiskImageFormat.HdvBlockImage, path, media.IsReadOnly);
    }

    private DiskImageOpenResult OpenTwoImg(string path, bool forceReadOnly)
    {
        var headBytes = PeekFirstBytes(path, 64);
        if (headBytes.Length < 64 || !headBytes.AsSpan(0, 4).SequenceEqual(TwoImgHeader.Magic))
        {
            throw new InvalidDataException($"'{path}' is not a 2MG image (missing 2IMG magic).");
        }

        var header = TwoImgHeader.Parse(headBytes);
        if (header.HeaderLength < 64)
        {
            throw new InvalidDataException("2MG header length is invalid.");
        }

        if (header.DataOffset < 0)
        {
            throw new InvalidDataException("2MG data offset is invalid.");
        }

        if (header.DataLength < 0)
        {
            throw new InvalidDataException("2MG data length is invalid.");
        }

        if (header.DataOffset < header.HeaderLength)
        {
            throw new InvalidDataException("2MG data offset precedes the end of the header.");
        }

        var readOnly = forceReadOnly || header.IsWriteProtected;
        var backend = this.OpenBackend(path, readOnly);

        try
        {
            if (header.DataOffset + (long)header.DataLength > backend.Length)
            {
                throw new InvalidDataException("2MG header points past end of file.");
            }

            switch (header.Format)
            {
                case 0: // DOS 3.3 sector order
                case 1: // ProDOS sector order
                    {
                        var order = header.Format == 0 ? SectorOrder.Dos33 : SectorOrder.ProDos;
                        var fmt = header.Format == 0 ? DiskImageFormat.TwoImgDos : DiskImageFormat.TwoImgProDos;
                        if (header.DataLength % GcrEncoder.BytesPerSector != 0)
                        {
                            throw new InvalidDataException("2MG sector payload length is not a multiple of 256.");
                        }

                        int trackCount;
                        int sectorsPerTrack = SectorSkew.SectorsPerTrack;
                        if (header.DataLength == FivePointTwoFiveStandardBytes)
                        {
                            trackCount = 35;
                        }
                        else if (header.DataLength % (sectorsPerTrack * GcrEncoder.BytesPerSector) == 0)
                        {
                            trackCount = header.DataLength / (sectorsPerTrack * GcrEncoder.BytesPerSector);
                        }
                        else
                        {
                            throw new InvalidDataException("2MG sector payload does not represent whole 16-sector tracks.");
                        }

                        var geometry = new DiskGeometry(trackCount, sectorsPerTrack, GcrEncoder.BytesPerSector, order);
                        var media = new SectorImageMedia(backend, geometry, backingOffset: header.DataOffset, writeProtected: readOnly, volume: header.DosVolumeNumber);
                        return new Image525AndBlockResult(media.As525Media(), media.AsBlockMedia(), order, false, fmt, path, media.IsReadOnly);
                    }

                case 2: // nibble
                    {
                        if (header.DataLength % GcrEncoder.StandardTrackLength != 0)
                        {
                            throw new InvalidDataException("2MG nibble payload is not a multiple of the standard track length.");
                        }

                        var trackCount = header.DataLength / GcrEncoder.StandardTrackLength;
                        var media = new NibbleImageMedia(backend, trackCount, backingOffset: header.DataOffset, writeProtected: readOnly);
                        return new Image525Result(media, DiskImageFormat.TwoImgNibble, path, media.IsReadOnly);
                    }

                default:
                    throw new InvalidDataException($"Unknown 2MG format code {header.Format}.");
            }
        }
        catch
        {
            backend.Dispose();
            throw;
        }
    }
}