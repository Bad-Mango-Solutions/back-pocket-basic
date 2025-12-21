// <copyright file="IAppleSpeaker.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

/// <summary>
/// Interface for Apple II speaker emulation.
/// </summary>
public interface IAppleSpeaker : IDisposable
{
    /// <summary>
    /// Toggles the speaker cone (click), emulating access to $C030.
    /// </summary>
    void Click();

    /// <summary>
    /// Plays the authentic Apple II beep tone (~1000Hz for ~0.1 seconds)
    /// Used when CHR$(7) is printed.
    /// </summary>
    void Beep();

    /// <summary>
    /// Flushes any buffered audio to the output device.
    /// </summary>
    void Flush();
}