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
/// <item><description>$C01F: RD80COL - 80-column mode status</description></item>
/// </list>
/// <para>
/// Note: Character ROM management and the ALTCHAR switch ($C00E/$C00F/$C01E)
/// are handled by <see cref="ICharacterDevice"/>.
/// </para>
/// </remarks>
public interface IVideoDevice : IMotherboardDevice
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
    /// Sets the hi-res mode state.
    /// </summary>
    /// <param name="enabled">Whether hi-res mode is enabled.</param>
    /// <remarks>
    /// <para>
    /// This method is called by the <c>AuxiliaryMemoryController</c> when the
    /// $C056 (LORES) or $C057 (HIRES) soft switches are triggered.
    /// </para>
    /// <para>
    /// The hi-res switch affects both memory banking (handled by <c>AuxiliaryMemoryController</c>)
    /// and video display mode (handled by this device). This method synchronizes
    /// the video state with the memory controller's state.
    /// </para>
    /// </remarks>
    void SetHiRes(bool enabled);

    /// <summary>
    /// Sets the page 2 selection state.
    /// </summary>
    /// <param name="selected">Whether page 2 is selected.</param>
    /// <remarks>
    /// <para>
    /// This method is called by the <c>AuxiliaryMemoryController</c> when the
    /// $C054 (PAGE1) or $C055 (PAGE2) soft switches are triggered.
    /// </para>
    /// <para>
    /// The page switch affects both memory banking (handled by <c>AuxiliaryMemoryController</c>)
    /// and which video page is displayed (handled by this device). This method synchronizes
    /// the video state with the memory controller's state.
    /// </para>
    /// </remarks>
    void SetPage2(bool selected);

    /// <summary>
    /// Sets the 80-column display mode state.
    /// </summary>
    /// <param name="enabled">Whether 80-column mode is enabled.</param>
    /// <remarks>
    /// <para>
    /// This method is called by the <c>Extended80ColumnDevice</c> when the
    /// $C00C (80COLOFF) or $C00D (80COLON) soft switches are triggered.
    /// </para>
    /// <para>
    /// The 80-column switch enables the double-width text display mode that alternates
    /// characters between main and auxiliary memory for 80 columns of text per line.
    /// </para>
    /// </remarks>
    void Set80ColumnMode(bool enabled);
}