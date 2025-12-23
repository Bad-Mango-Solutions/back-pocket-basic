// <copyright file="ArchitecturalMode.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Defines the architectural modes for the 65xx family processors.
/// </summary>
public enum ArchitecturalMode : byte
{
    /// <summary>6502 compatibility mode (CP=1, E=1).</summary>
    Mode65C02 = 0,

    /// <summary>65816 native mode (CP=1, E=0).</summary>
    Mode65816 = 1,

    /// <summary>65832 native-32 mode (CP=0).</summary>
    Mode65832 = 2,
}