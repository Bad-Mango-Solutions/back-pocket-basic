// <copyright file="VideoFrameMetadata.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using BadMango.Emulator.Devices;

/// <summary>
/// Metadata captured together with a produced video frame.
/// </summary>
public readonly struct VideoFrameMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFrameMetadata"/> struct.
    /// </summary>
    /// <param name="sequenceNumber">Monotonic sequence number of the produced frame.</param>
    /// <param name="mode">Video mode used to render the frame.</param>
    /// <param name="isPage2">PAGE2 state used to render the frame.</param>
    /// <param name="useAltCharSet">ALTCHAR state used to render the frame.</param>
    /// <param name="flashState">Flash inversion state used to render the frame.</param>
    /// <param name="noFlash1Enabled">NOFLASH1 state used to render the frame.</param>
    /// <param name="noFlash2Enabled">NOFLASH2 state used to render the frame.</param>
    public VideoFrameMetadata(
        long sequenceNumber,
        VideoMode mode,
        bool isPage2,
        bool useAltCharSet,
        bool flashState,
        bool noFlash1Enabled,
        bool noFlash2Enabled)
    {
        SequenceNumber = sequenceNumber;
        Mode = mode;
        IsPage2 = isPage2;
        UseAltCharSet = useAltCharSet;
        FlashState = flashState;
        NoFlash1Enabled = noFlash1Enabled;
        NoFlash2Enabled = noFlash2Enabled;
    }

    /// <summary>
    /// Gets the monotonic sequence number of the produced frame.
    /// </summary>
    public long SequenceNumber { get; }

    /// <summary>
    /// Gets the video mode used to render the frame.
    /// </summary>
    public VideoMode Mode { get; }

    /// <summary>
    /// Gets a value indicating whether PAGE2 was selected when the frame was rendered.
    /// </summary>
    public bool IsPage2 { get; }

    /// <summary>
    /// Gets a value indicating whether ALTCHAR was active when the frame was rendered.
    /// </summary>
    public bool UseAltCharSet { get; }

    /// <summary>
    /// Gets a value indicating whether flash inversion was active for the frame.
    /// </summary>
    public bool FlashState { get; }

    /// <summary>
    /// Gets a value indicating whether NOFLASH1 was enabled for the frame.
    /// </summary>
    public bool NoFlash1Enabled { get; }

    /// <summary>
    /// Gets a value indicating whether NOFLASH2 was enabled for the frame.
    /// </summary>
    public bool NoFlash2Enabled { get; }
}