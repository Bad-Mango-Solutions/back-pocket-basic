// <copyright file="AppleFileSystemFactory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.FileSystems;

using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Factory to create an <see cref="IAppleFileSystem"/> view over a mounted or image disk.
/// Supports auto-detection for DOS 3.3, ProDOS, and basic Pascal.
/// </summary>
public static class AppleFileSystemFactory
{
    /// <summary>
    /// Attempts to open a DOS 3.3 file system view over the supplied sector image.
    /// </summary>
    /// <param name="media">The sector image media to inspect.</param>
    /// <returns>A DOS 3.3 file system view when the image is recognized; otherwise, <see langword="null"/>.</returns>
    public static IAppleFileSystem? TryOpenDos33(SectorImageMedia media)
    {
        if (media == null)
        {
            return null;
        }

        try
        {
            return new Dos33FileSystem(media);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to open a ProDOS file system over the supplied block media.
    /// </summary>
    /// <param name="media">The block-based media to inspect.</param>
    /// <returns>A ProDOS file system view when the image is recognized; otherwise, <see langword="null"/>.</returns>
    public static IAppleFileSystem? TryOpenProDos(IBlockMedia media)
    {
        if (media == null || media.BlockSize != 512)
        {
            return null;
        }

        try
        {
            byte[] ReadBlock(int block)
            {
                var buf = new byte[512];
                media.ReadBlock(block, buf);
                return buf;
            }

            return new ProDosFileSystem(ReadBlock);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to open any supported file system from the supplied disk image result.
    /// </summary>
    /// <param name="open">The disk image to inspect.</param>
    /// <returns>An appropriate file system view when one is recognized; otherwise, <see langword="null"/>.</returns>
    public static IAppleFileSystem? TryOpen(DiskImageOpenResult open)
    {
        if (open is Image525AndBlockResult r)
        {
            if (r.SectorImage != null)
            {
                var dos = TryOpenDos33(r.SectorImage);
                if (dos != null)
                {
                    return dos;
                }
            }

            if (r.BlockMedia != null)
            {
                var prodos = TryOpenProDos(r.BlockMedia);
                if (prodos != null)
                {
                    return prodos;
                }

                var pascal = TryOpenPascal(r.BlockMedia);
                if (pascal != null)
                {
                    return pascal;
                }
            }
        }
        else if (open is ImageBlockResult br && br.Media != null)
        {
            return TryOpenProDos(br.Media);
        }

        return null;
    }

    /// <summary>
    /// Attempts to open a Pascal file system over the supplied block media.
    /// </summary>
    /// <param name="media">The block media to inspect.</param>
    /// <returns>A Pascal file system view when the image is recognized; otherwise, <see langword="null"/>.</returns>
    public static IAppleFileSystem? TryOpenPascal(IBlockMedia media)
    {
        if (media == null || media.BlockSize != 512)
        {
            return null;
        }

        try
        {
            byte[] ReadBlock(int block)
            {
                var buf = new byte[512];
                media.ReadBlock(block, buf);
                return buf;
            }

            return new PascalFileSystem(ReadBlock);
        }
        catch
        {
            return null;
        }
    }
}