// <copyright file="IAudioOutput.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

/// <summary>
/// Defines the contract for platform-specific audio output functionality.
/// </summary>
/// <remarks>
/// This interface provides methods for playing audio samples and managing
/// the lifecycle of audio output resources. Implementations of this interface
/// are responsible for handling platform-specific details of audio playback.
/// </remarks>
internal interface IAudioOutput : IDisposable
{
    /// <summary>
    /// Plays the specified audio samples through the audio output.
    /// </summary>
    /// <param name="samples">
    /// An array of audio samples to be played. Each sample is represented as a 16-bit signed integer.
    /// </param>
    /// <remarks>
    /// Implementations of this method are responsible for handling the playback of the provided audio samples.
    /// The behavior of this method may vary depending on the specific implementation of the <see cref="IAudioOutput"/> interface.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called after the audio output has been disposed.
    /// </exception>
    void Play(short[] samples);
}