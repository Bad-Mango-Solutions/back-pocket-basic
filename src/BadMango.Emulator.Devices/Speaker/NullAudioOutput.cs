// <copyright file="NullAudioOutput.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

using System.Diagnostics.CodeAnalysis;

using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Represents a null implementation of the <see cref="IAudioOutput"/> interface,
/// intended for scenarios where audio output is unavailable or not required.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a no-op implementation of audio output functionality.
/// It is used as a fallback to ensure that audio-related operations can be
/// safely invoked without producing any sound or consuming resources.
/// </para>
/// <para>
/// The <see cref="SpeakerController"/> uses this implementation when platform
/// audio output initialization fails.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class NullAudioOutput : IAudioOutput
{
    /// <inheritdoc />
    public void Play(short[] samples)
    {
        // This implementation intentionally does nothing, as this is a null audio output.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to release, as this is a null audio output.
    }
}