// <copyright file="VideoMode.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Video display modes for the Apple II video controller.
/// </summary>
public enum VideoMode
{
    /// <summary>
    /// 40-column text mode.
    /// </summary>
    Text40,

    /// <summary>
    /// 80-column text mode.
    /// </summary>
    Text80,

    /// <summary>
    /// Low-resolution graphics mode (40x48, 16 colors).
    /// </summary>
    LoRes,

    /// <summary>
    /// Double low-resolution graphics mode (80x48, 16 colors).
    /// </summary>
    DoubleLoRes,

    /// <summary>
    /// High-resolution graphics mode (280x192, 6 colors).
    /// </summary>
    HiRes,

    /// <summary>
    /// Double high-resolution graphics mode (560x192, 16 colors).
    /// </summary>
    DoubleHiRes,

    /// <summary>
    /// Low-resolution graphics with 4 lines of text at bottom.
    /// </summary>
    LoResMixed,

    /// <summary>
    /// Double low-resolution graphics with 4 lines of text at bottom.
    /// </summary>
    DoubleLoResMixed,

    /// <summary>
    /// High-resolution graphics with 4 lines of text at bottom.
    /// </summary>
    HiResMixed,

    /// <summary>
    /// Double high-resolution graphics with 4 lines of text at bottom.
    /// </summary>
    DoubleHiResMixed,
}