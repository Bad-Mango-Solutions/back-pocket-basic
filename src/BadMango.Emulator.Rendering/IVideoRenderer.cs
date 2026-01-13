// <copyright file="IVideoRenderer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using BadMango.Emulator.Devices;

/// <summary>
/// Defines the interface for rendering Pocket2e video modes to a pixel buffer.
/// </summary>
/// <remarks>
/// <para>
/// The video renderer converts emulated video memory into pixels suitable
/// for display. It reads from the emulated system's video memory using a read delegate
/// and produces output in the canonical 560×384 framebuffer format.
/// </para>
/// <para>
/// The renderer is intentionally pure - it contains no UI code, timing logic, or
/// threading assumptions. Frame scheduling is handled by the host/UI layer.
/// </para>
/// </remarks>
public interface IVideoRenderer
{
    /// <summary>
    /// Gets the width of the canonical framebuffer in pixels.
    /// </summary>
    /// <value>The canonical width is 560 pixels for all Pocket2e video modes.</value>
    int CanonicalWidth { get; }

    /// <summary>
    /// Gets the height of the canonical framebuffer in pixels.
    /// </summary>
    /// <value>The canonical height is 384 pixels (2× vertical scaling of 192-line modes).</value>
    int CanonicalHeight { get; }

    /// <summary>
    /// Renders a frame to the specified pixel buffer based on current video mode and memory state.
    /// </summary>
    /// <param name="pixels">
    /// The target pixel buffer in BGRA format. Must be at least
    /// <see cref="CanonicalWidth"/> × <see cref="CanonicalHeight"/> pixels.
    /// </param>
    /// <param name="mode">The current video mode to render.</param>
    /// <param name="readMemory">
    /// A delegate that reads bytes from emulated memory. The delegate should use
    /// side-effect-free access (DMA-style reads) to avoid triggering soft switches.
    /// </param>
    /// <param name="characterRomData">
    /// The character ROM data for text rendering. Must be 4096 bytes (4KB) containing
    /// two 2KB character sets. Pass <see cref="ReadOnlySpan{T}.Empty"/> if character
    /// ROM is not loaded.
    /// </param>
    /// <param name="useAltCharSet">
    /// <see langword="true"/> to use the alternate character set (MouseText);
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="isPage2">
    /// <see langword="true"/> if page 2 is selected; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="flashState">
    /// <see langword="true"/> if flash characters should currently be inverted;
    /// otherwise, <see langword="false"/>. Toggle this at ~1.9 Hz (every 16 frames at 60 Hz).
    /// </param>
    /// <param name="noFlash1Enabled">
    /// <see langword="true"/> if flashing is disabled for character bank 1 (primary set);
    /// otherwise, <see langword="false"/>. When enabled, characters in the $40-$7F range
    /// display normally without flashing.
    /// </param>
    /// <param name="noFlash2Enabled">
    /// <see langword="true"/> if flashing is disabled for character bank 2 (alternate set);
    /// otherwise, <see langword="false"/>. Defaults to enabled (no flashing) for bank 2.
    /// </param>
    /// <param name="colorMode">
    /// The display color mode (green, amber, white, or color). Defaults to green phosphor for classic monochrome display.
    /// </param>
    void RenderFrame(
        Span<uint> pixels,
        VideoMode mode,
        Func<ushort, byte> readMemory,
        ReadOnlySpan<byte> characterRomData,
        bool useAltCharSet,
        bool isPage2,
        bool flashState,
        bool noFlash1Enabled,
        bool noFlash2Enabled,
        DisplayColorMode colorMode = DisplayColorMode.Green);

    /// <summary>
    /// Clears the pixel buffer to black.
    /// </summary>
    /// <param name="pixels">The pixel buffer to clear.</param>
    void Clear(Span<uint> pixels);
}