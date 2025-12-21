// <copyright file="MemorySizes.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Provides commonly used memory size constants for emulated systems.
/// </summary>
/// <remarks>
/// This static class defines standard memory sizes used in various 6502-family systems,
/// making it easier to configure memory without using magic numbers.
/// Uses uint to support memory sizes up to 4GB for future 32-bit processors.
/// Minimum size is 64KB as Apple IIe-enhanced and later devices don't go lower.
/// </remarks>
public static class MemorySizes
{
    /// <summary>
    /// 64 KB of memory (65,536 bytes) - Standard Apple II and 6502 address space.
    /// </summary>
    public const uint Size64KB = 65536;

    /// <summary>
    /// 128 KB of memory (131,072 bytes) - Apple IIe enhanced with auxiliary memory.
    /// </summary>
    public const uint Size128KB = 131072;

    /// <summary>
    /// 256 KB of memory (262,144 bytes).
    /// </summary>
    public const uint Size256KB = 262144;

    /// <summary>
    /// 512 KB of memory (524,288 bytes).
    /// </summary>
    public const uint Size512KB = 524288;

    /// <summary>
    /// 1 MB of memory (1,048,576 bytes).
    /// </summary>
    public const uint Size1MB = 1048576;

    /// <summary>
    /// 8 MB of memory (8,388,608 bytes) - Apple IIgs maximum.
    /// </summary>
    public const uint Size8MB = 8388608;

    /// <summary>
    /// 16 MB of memory (16,777,216 bytes) - 65816 maximum addressable.
    /// </summary>
    public const uint Size16MB = 16777216;
}