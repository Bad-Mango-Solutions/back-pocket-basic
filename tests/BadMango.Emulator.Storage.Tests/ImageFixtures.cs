// <copyright file="ImageFixtures.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Tests;

/// <summary>
/// Helpers to author fixture sector / nibble / 2MG / HDV images entirely in memory.
/// </summary>
internal static class ImageFixtures
{
    /// <summary>The total payload byte length of a standard 35-track / 16-sector / 256-byte image.</summary>
    public const int FivePointTwoFiveBytes = 35 * 16 * 256;

    /// <summary>
    /// Returns a deterministic 35-track / 16-sector / 256-byte payload seeded by
    /// <paramref name="seed"/>.
    /// </summary>
    /// <param name="seed">Random seed.</param>
    /// <returns>A buffer of <see cref="FivePointTwoFiveBytes"/> bytes.</returns>
    public static byte[] Random525Payload(int seed)
    {
        var data = new byte[FivePointTwoFiveBytes];
        new Random(seed).NextBytes(data);
        return data;
    }

    /// <summary>
    /// Stamps a valid DOS 3.3 VTOC into the supplied DOS-ordered 35-track image,
    /// at track 17 / sector 0 in DOS-logical terms.
    /// </summary>
    /// <param name="dosImage">DOS-ordered sector image to mutate.</param>
    /// <param name="volume">Volume number.</param>
    public static void WriteDosVtoc(byte[] dosImage, int volume = 254)
    {
        const int track = 17;
        const int sector = 0;
        var offset = ((track * 16) + sector) * 256;
        var span = dosImage.AsSpan(offset, 256);
        span.Clear();
        span[0x00] = 0x04;
        span[0x01] = 0x11;
        span[0x02] = 0x0F;
        span[0x03] = 0x03;
        span[0x06] = (byte)volume;
        span[0x27] = 0x7A;
        span[0x34] = 0x23;
        span[0x35] = 0x10;
        span[0x36] = 0x00;
        span[0x37] = 0x01;
    }

    /// <summary>
    /// Stamps a minimal ProDOS volume-directory key block at file-offset block 2
    /// of the supplied ProDOS-ordered 35-track image.
    /// </summary>
    /// <param name="prodosImage">ProDOS-ordered image to mutate.</param>
    /// <param name="volumeName">Volume name (1..15 ASCII chars).</param>
    public static void WriteProDosRootDirectory(byte[] prodosImage, string volumeName = "BLANK")
    {
        const int blockOffset = 2 * 512;
        var span = prodosImage.AsSpan(blockOffset, 512);
        span.Clear();
        span[0] = 0;
        span[1] = 0; // prev pointer = 0 (key block)
        span[2] = 3;
        span[3] = 0; // next pointer = 3
        span[4] = (byte)(0xF0 | (byte)volumeName.Length);
        for (var i = 0; i < volumeName.Length; i++)
        {
            span[5 + i] = (byte)volumeName[i];
        }
    }

    /// <summary>
    /// Writes <paramref name="bytes"/> to a temp file and returns its path.
    /// </summary>
    /// <param name="bytes">File contents.</param>
    /// <param name="extension">Extension including the dot (e.g. <c>".dsk"</c>).</param>
    /// <returns>The temp file path.</returns>
    public static string WriteTempFile(byte[] bytes, string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"bms-storage-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, bytes);
        return path;
    }
}