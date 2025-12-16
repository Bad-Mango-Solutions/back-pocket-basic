// <copyright file="NullAudioOutput.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Null audio output for when audio is unavailable.
/// </summary>
internal class NullAudioOutput : IAudioOutput
{
    public void Play(short[] samples) { }

    public void Dispose() { }
}