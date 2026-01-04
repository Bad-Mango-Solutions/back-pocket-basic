// <copyright file="IVideoModeDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Video mode controller interface for display rendering.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the host-facing API for the video mode controller,
/// allowing the emulator frontend to query current display mode settings for
/// rendering purposes.
/// </para>
/// <para>
/// The Apple IIe video controller supports multiple display modes controlled
/// through soft switches in the $C050-$C05F range:
/// </para>
/// <list type="bullet">
/// <item><description>$C050/$C051: Graphics/Text mode</description></item>
/// <item><description>$C052/$C053: Full screen/Mixed mode</description></item>
/// <item><description>$C054/$C055: Page 1/Page 2</description></item>
/// <item><description>$C056/$C057: Lo-res/Hi-res mode</description></item>
/// </list>
/// <para>
/// The IIe adds 80-column and double-resolution modes controlled by additional
/// soft switches and the auxiliary memory system.
/// </para>
/// </remarks>
public interface IVideoModeDevice : IMotherboardDevice
{
    /// <summary>
    /// Event raised when the video mode changes.
    /// </summary>
    event Action<VideoMode>? ModeChanged;

    /// <summary>
    /// Gets the current video mode.
    /// </summary>
    /// <value>The currently active video display mode.</value>
    VideoMode CurrentMode { get; }

    /// <summary>
    /// Gets a value indicating whether the display is in text mode.
    /// </summary>
    /// <value><see langword="true"/> if text mode is active; otherwise, <see langword="false"/>.</value>
    bool IsTextMode { get; }

    /// <summary>
    /// Gets a value indicating whether mixed mode is enabled (4 lines of text at bottom).
    /// </summary>
    /// <value><see langword="true"/> if mixed mode is enabled; otherwise, <see langword="false"/>.</value>
    bool IsMixedMode { get; }

    /// <summary>
    /// Gets a value indicating whether page 2 is selected.
    /// </summary>
    /// <value><see langword="true"/> if page 2 is active; otherwise, <see langword="false"/>.</value>
    bool IsPage2 { get; }

    /// <summary>
    /// Gets a value indicating whether hi-res mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if hi-res mode is active; otherwise, <see langword="false"/>.</value>
    bool IsHiRes { get; }

    /// <summary>
    /// Gets a value indicating whether 80-column mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if 80-column mode is active; otherwise, <see langword="false"/>.</value>
    bool Is80Column { get; }

    /// <summary>
    /// Gets a value indicating whether double hi-res mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if double hi-res mode is active; otherwise, <see langword="false"/>.</value>
    bool IsDoubleHiRes { get; }

    /// <summary>
    /// Gets a value indicating whether the alternate character set is active.
    /// </summary>
    /// <value><see langword="true"/> if alternate character set is active; otherwise, <see langword="false"/>.</value>
    bool IsAltCharSet { get; }

    /// <summary>
    /// Gets the annunciator states (0-3).
    /// </summary>
    /// <value>A read-only list of the four annunciator output states.</value>
    IReadOnlyList<bool> Annunciators { get; }
}