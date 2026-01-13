// <copyright file="DefaultCharacterRom.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Reflection;

/// <summary>
/// Provides access to the default Pocket2e character ROM embedded in the assembly.
/// </summary>
/// <remarks>
/// <para>
/// The default character ROM is a 4KB binary containing the Pocket2e character set
/// with proper inverse characters, uppercase, lowercase, and symbols.
/// </para>
/// <para>
/// The ROM is organized as:
/// </para>
/// <list type="bullet">
/// <item><description>$00-$3F: Inverse characters (bit-flipped @, A-Z, symbols)</description></item>
/// <item><description>$40-$7F: Flashing characters (@, A-Z, symbols)</description></item>
/// <item><description>$80-$BF: Normal characters (@, A-Z, symbols)</description></item>
/// <item><description>$C0-$FF: Lowercase characters (a-z, extra symbols)</description></item>
/// </list>
/// <para>
/// The iconic inverse @ character at $00 displays as white-on-black, famously
/// causing screens full of inverse at-signs when memory was zeroed.
/// </para>
/// </remarks>
public static class DefaultCharacterRom
{
    private const string ResourceName = "BadMango.Emulator.Devices.Resources.pocket2-charset.rom";
    private const int ExpectedSize = 4096;

    private static byte[]? cachedRom;

    /// <summary>
    /// Gets the default character ROM data.
    /// </summary>
    /// <returns>
    /// A byte array containing the 4KB character ROM data.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the embedded resource cannot be found or is invalid.
    /// </exception>
    /// <remarks>
    /// The ROM data is cached after the first load for performance.
    /// </remarks>
    public static byte[] GetRomData()
    {
        if (cachedRom != null)
        {
            return cachedRom;
        }

        var assembly = typeof(DefaultCharacterRom).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Default character ROM resource '{ResourceName}' not found in assembly.");

        if (stream.Length != ExpectedSize)
        {
            throw new InvalidOperationException(
                $"Default character ROM has invalid size {stream.Length}. Expected {ExpectedSize} bytes.");
        }

        cachedRom = new byte[ExpectedSize];
        int bytesRead = stream.Read(cachedRom, 0, ExpectedSize);

        if (bytesRead != ExpectedSize)
        {
            throw new InvalidOperationException(
                $"Failed to read complete character ROM. Read {bytesRead} of {ExpectedSize} bytes.");
        }

        return cachedRom;
    }

    /// <summary>
    /// Tries to get the default character ROM data.
    /// </summary>
    /// <param name="romData">
    /// When this method returns, contains the ROM data if available,
    /// or <see langword="null"/> if the resource could not be loaded.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the ROM was successfully loaded;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryGetRomData(out byte[]? romData)
    {
        try
        {
            romData = GetRomData();
            return true;
        }
        catch (InvalidOperationException)
        {
            romData = null;
            return false;
        }
    }

    /// <summary>
    /// Loads the default character ROM into a video device.
    /// </summary>
    /// <param name="videoDevice">The video device to load the ROM into.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="videoDevice"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the embedded resource cannot be found or is invalid.
    /// </exception>
    public static void LoadIntoVideoDevice(Interfaces.IVideoDevice videoDevice)
    {
        ArgumentNullException.ThrowIfNull(videoDevice);
        videoDevice.LoadCharacterRom(GetRomData());
    }

    /// <summary>
    /// Loads the default character ROM into a character device.
    /// </summary>
    /// <param name="characterDevice">The character device to load the ROM into.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="characterDevice"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the embedded resource cannot be found or is invalid.
    /// </exception>
    public static void LoadIntoCharacterDevice(Interfaces.ICharacterDevice characterDevice)
    {
        ArgumentNullException.ThrowIfNull(characterDevice);
        characterDevice.LoadCharacterRom(GetRomData());
    }
}