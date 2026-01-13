// <copyright file="IEmulatorConnection.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Services;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Service interface for connecting the glyph editor to a running emulator instance.
/// </summary>
public interface IEmulatorConnection
{
    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// Gets a value indicating whether a connection to an emulator is active.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Attempts to connect to a character device.
    /// </summary>
    /// <param name="characterDevice">The character device to connect to.</param>
    /// <returns>True if connection succeeded.</returns>
    bool Connect(ICharacterDevice characterDevice);

    /// <summary>
    /// Disconnects from the emulator.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Gets the glyph hot loader from the connected device.
    /// </summary>
    /// <returns>The glyph hot loader, or null if not available.</returns>
    IGlyphHotLoader? GetGlyphHotLoader();

    /// <summary>
    /// Hot-loads glyph data to the connected emulator.
    /// </summary>
    /// <param name="glyphData">4KB glyph data.</param>
    /// <param name="target">Target location.</param>
    /// <returns>True if successful.</returns>
    bool HotLoad(byte[] glyphData, GlyphLoadTarget target);

    /// <summary>
    /// Hot-loads a single character to the connected emulator.
    /// </summary>
    /// <param name="charCode">The character code.</param>
    /// <param name="scanlines">The 8-byte scanline data.</param>
    /// <param name="useAltCharSet">Whether to target the alternate character set.</param>
    /// <param name="target">Target location.</param>
    /// <returns>True if successful.</returns>
    bool HotLoadCharacter(byte charCode, byte[] scanlines, bool useAltCharSet, GlyphLoadTarget target);
}