// <copyright file="IVideoFrameSource.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using BadMango.Emulator.Devices;

/// <summary>
/// Read-only source of latest rendered video frames.
/// </summary>
public interface IVideoFrameSource
{
    /// <summary>
    /// Event raised when a new frame is produced.
    /// </summary>
    event Action? FrameReady;

    /// <summary>
    /// Gets the frame width in pixels.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the frame height in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets or sets the color mode used when producing frames.
    /// </summary>
    DisplayColorMode ColorMode { get; set; }

    /// <summary>
    /// Attempts to copy the latest produced frame into the destination span.
    /// </summary>
    /// <param name="destination">Destination pixel buffer in BGRA format.</param>
    /// <param name="metadata">Output frame metadata captured with the frame.</param>
    /// <returns>
    /// <see langword="true"/> when a frame has been produced and copied; otherwise, <see langword="false"/>.
    /// </returns>
    bool TryCopyLatestFrame(Span<uint> destination, out VideoFrameMetadata metadata);

    /// <summary>
    /// Forces rendering of a fresh frame from current machine state.
    /// </summary>
    /// <remarks>
    /// This does not raise <see cref="FrameReady"/> because it is used for synchronous refresh paths.
    /// </remarks>
    void ForceRender();
}