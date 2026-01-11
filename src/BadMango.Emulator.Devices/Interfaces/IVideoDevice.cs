// <copyright file="IVideoDevice.cs" company="Bad Mango Solutions">
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
/// <para>
/// Status read registers at $C019-$C01F provide read-back of video state:
/// </para>
/// <list type="bullet">
/// <item><description>$C019: RDVBL - Vertical blanking (inverted: bit 7 = 0 during VBL)</description></item>
/// <item><description>$C01A: RDTEXT - Text mode status</description></item>
/// <item><description>$C01B: RDMIXED - Mixed mode status</description></item>
/// <item><description>$C01C: RDPAGE2 - Page 2 status</description></item>
/// <item><description>$C01D: RDHIRES - Hi-res mode status</description></item>
/// <item><description>$C01E: RDALTCHAR - Alternate character set status</description></item>
/// <item><description>$C01F: RD80COL - 80-column mode status</description></item>
/// </list>
/// <para>
/// The video device also provides access to character ROM data for text rendering
/// through the <see cref="ICharacterRomProvider"/> interface.
/// </para>
/// </remarks>
public interface IVideoDevice : IMotherboardDevice, ICharacterRomProvider
{
    /// <summary>
    /// Event raised when the video mode changes.
    /// </summary>
    event Action<VideoMode>? ModeChanged;

    /// <summary>
    /// Event raised when vertical blanking begins, signaling the video window to refresh.
    /// </summary>
    /// <remarks>
    /// This event fires at approximately 60 Hz (every 17,030 cycles at 1.023 MHz).
    /// Video renderers should subscribe to this event to trigger frame redraws.
    /// </remarks>
    event Action? VBlankOccurred;

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
    /// Gets or sets a value indicating whether vertical blanking is in progress.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is typically updated by the video timing subsystem.
    /// </para>
    /// <para>
    /// When read via the $C019 soft switch, the status is inverted from other
    /// status reads: bit 7 = 0 during vertical blanking, bit 7 = 1 when NOT
    /// in vertical blanking. This convention allows efficient BMI/BPL testing
    /// for waiting for VBL.
    /// </para>
    /// </remarks>
    /// <value><see langword="true"/> if vertical blanking is in progress; otherwise, <see langword="false"/>.</value>
    bool IsVerticalBlanking { get; set; }

    /// <summary>
    /// Gets the annunciator states (0-3).
    /// </summary>
    /// <value>A read-only list of the four annunciator output states.</value>
    IReadOnlyList<bool> Annunciators { get; }

    /// <summary>
    /// Loads character ROM data into the video device.
    /// </summary>
    /// <param name="romData">
    /// The character ROM data to load. Must be exactly 4096 bytes (4KB)
    /// containing two 2KB character sets (primary and alternate).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="romData"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="romData"/> is not exactly 4096 bytes.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The character ROM is organized as two 2KB segments:
    /// </para>
    /// <list type="bullet">
    /// <item><description>$0000-$07FF: Primary character set (256 × 8 bytes)</description></item>
    /// <item><description>$0800-$0FFF: Alternate character set with MouseText (256 × 8 bytes)</description></item>
    /// </list>
    /// <para>
    /// Each character occupies 8 consecutive bytes, one per scanline from top to bottom.
    /// Each byte contains 7 pixel bits (bits 0-6), with bit 7 unused.
    /// </para>
    /// </remarks>
    void LoadCharacterRom(byte[] romData);
}