// <copyright file="IAudioOutput.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Interface for platform-specific audio output.
/// </summary>
internal interface IAudioOutput : IDisposable
{
    void Play(short[] samples);
}