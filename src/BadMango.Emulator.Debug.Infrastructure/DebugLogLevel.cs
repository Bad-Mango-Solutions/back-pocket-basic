// <copyright file="DebugLogLevel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

/// <summary>
/// Defines severity levels shown in the debug log window.
/// </summary>
public enum DebugLogLevel
{
    /// <summary>
    /// Warning-level event.
    /// </summary>
    Warning,

    /// <summary>
    /// Error-level event.
    /// </summary>
    Error,

    /// <summary>
    /// Fatal-level event.
    /// </summary>
    Fatal,
}