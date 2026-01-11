// <copyright file="IVideoSession.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

/// <summary>
/// Manages the lifetime of a video display session, connecting the emulator to a host display.
/// </summary>
/// <remarks>
/// <para>
/// The video session owns the relationship between the emulated machine and the host display
/// window. It coordinates frame rendering, VBlank callbacks, and window lifecycle.
/// </para>
/// <para>
/// This interface follows the architecture where the emulator core does not know about
/// the UI framework (Avalonia). The session bridges the emulator's video device to the
/// host presentation layer.
/// </para>
/// </remarks>
public interface IVideoSession : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the video display is currently open.
    /// </summary>
    /// <value><see langword="true"/> if the display window is open; otherwise, <see langword="false"/>.</value>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the current display scale factor.
    /// </summary>
    /// <value>The integer scale factor (e.g., 1 for native, 2 for 2× scaling).</value>
    int Scale { get; }

    /// <summary>
    /// Gets a value indicating whether FPS display is enabled.
    /// </summary>
    /// <value><see langword="true"/> if FPS display is enabled; otherwise, <see langword="false"/>.</value>
    bool ShowFps { get; }

    /// <summary>
    /// Opens the video display window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Opens the video display on the UI thread. The window is sized based on the
    /// current <see cref="Scale"/> value and positioned on screen.
    /// </para>
    /// <para>
    /// If the window is already open, this method has no effect.
    /// </para>
    /// </remarks>
    void Open();

    /// <summary>
    /// Closes the video display window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Closes the video display window and releases associated resources.
    /// </para>
    /// <para>
    /// If the window is not open, this method has no effect.
    /// </para>
    /// </remarks>
    void Close();

    /// <summary>
    /// Sets the display scale factor.
    /// </summary>
    /// <param name="scale">The scale factor (1-4).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="scale"/> is less than 1 or greater than 4.
    /// </exception>
    void SetScale(int scale);

    /// <summary>
    /// Enables or disables the FPS display overlay.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to show FPS; otherwise, <see langword="false"/>.</param>
    void SetShowFps(bool enabled);

    /// <summary>
    /// Forces a redraw of the video display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this method when screen memory is modified by debug commands while
    /// the machine is paused. This ensures the display reflects the current
    /// state of video memory without waiting for a VBlank event.
    /// </para>
    /// </remarks>
    void ForceRedraw();
}