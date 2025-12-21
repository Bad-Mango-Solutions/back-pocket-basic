// <copyright file="NullAudioOutput.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a null implementation of the <see cref="IAudioOutput"/> interface,
/// intended for scenarios where audio output is unavailable or not required.
/// </summary>
/// <remarks>
/// This class provides a no-op implementation of audio output functionality.
/// It is used as a fallback to ensure that audio-related operations can be
/// safely invoked without producing any sound or consuming resources.
/// </remarks>
[ExcludeFromCodeCoverage]
internal class NullAudioOutput : IAudioOutput
{
    /// <summary>
    /// Plays the provided audio samples.
    /// </summary>
    /// <param name="samples">An array of audio samples to play. Each sample is represented as a 16-bit signed integer.</param>
    /// <remarks>
    /// This method is a no-op in the <see cref="NullAudioOutput"/> implementation, as it represents a null audio output
    /// that does not produce any sound.
    /// </remarks>
    public void Play(short[] samples)
    {
        // This implementation intentionally does nothing, as this is a null audio output.
    }

    /// <summary>
    /// Releases all resources used by the <see cref="NullAudioOutput"/> instance.
    /// </summary>
    /// <remarks>
    /// This implementation does not release any resources, as the <see cref="NullAudioOutput"/>
    /// represents a null audio output that does not allocate any resources.
    /// </remarks>
    public void Dispose()
    {
        // No resources to release, as this is a null audio output.
    }
}