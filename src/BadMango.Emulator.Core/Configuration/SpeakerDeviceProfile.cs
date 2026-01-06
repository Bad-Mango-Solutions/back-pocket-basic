// <copyright file="SpeakerDeviceProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Configuration for the speaker device.
/// </summary>
public sealed class SpeakerDeviceProfile
{
    /// <summary>
    /// Gets or sets a value indicating whether the speaker is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the audio sample rate in Hz.
    /// </summary>
    /// <remarks>
    /// Common values: 44100 (CD quality), 48000 (standard audio), 22050 (low quality).
    /// </remarks>
    [JsonPropertyName("sampleRate")]
    public int SampleRate { get; set; } = 48000;
}