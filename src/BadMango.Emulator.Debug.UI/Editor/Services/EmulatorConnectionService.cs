// <copyright file="EmulatorConnectionService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Services;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Service implementation for connecting the glyph editor to a running emulator instance.
/// </summary>
public sealed class EmulatorConnectionService : IEmulatorConnection
{
    private ICharacterDevice? characterDevice;

    /// <inheritdoc />
    public event EventHandler<bool>? ConnectionStateChanged;

    /// <inheritdoc />
    public bool IsConnected => characterDevice != null;

    /// <inheritdoc />
    public bool Connect(ICharacterDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        characterDevice = device;
        ConnectionStateChanged?.Invoke(this, true);
        return true;
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        characterDevice = null;
        ConnectionStateChanged?.Invoke(this, false);
    }

    /// <inheritdoc />
    public IGlyphHotLoader? GetGlyphHotLoader()
    {
        return characterDevice as IGlyphHotLoader;
    }

    /// <inheritdoc />
    public bool HotLoad(byte[] glyphData, GlyphLoadTarget target)
    {
        var hotLoader = GetGlyphHotLoader();
        if (hotLoader == null)
        {
            return false;
        }

        try
        {
            hotLoader.HotLoadGlyphData(glyphData, target);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool HotLoadCharacter(byte charCode, byte[] scanlines, bool useAltCharSet, GlyphLoadTarget target)
    {
        var hotLoader = GetGlyphHotLoader();
        if (hotLoader == null)
        {
            return false;
        }

        try
        {
            hotLoader.HotLoadCharacter(charCode, scanlines, useAltCharSet, target);
            return true;
        }
        catch
        {
            return false;
        }
    }
}