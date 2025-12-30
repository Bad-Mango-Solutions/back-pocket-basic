// <copyright file="ScheduledEventKind.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>Represents the types of scheduled events that can occur in the emulator.</summary>
public enum ScheduledEventKind
{
    /// <summary>Represents an invalid scheduled event type.</summary>
    None = 0,

    /// <summary>Represents a scheduled event triggered by a device timer.</summary>
    DeviceTimer,

    /// <summary>Represents a scheduled event triggered by a change in an interrupt line.</summary>
    InterruptLineChange,

    /// <summary>Represents a scheduled event triggered by a DMA phase change.</summary>
    DmaPhase,

    /// <summary>Represents a scheduled event triggered by an audio tick.</summary>
    AudioTick,

    /// <summary>Represents a scheduled event triggered by a video scanline.</summary>
    VideoScanline,

    /// <summary>Represents a scheduled event triggered by a video blanking interval.</summary>
    VideoBlank,

    /// <summary>Represents a scheduled event for deferred work.</summary>
    DeferredWork,

    /// <summary>Represents a custom scheduled event.</summary>
    Custom,
}