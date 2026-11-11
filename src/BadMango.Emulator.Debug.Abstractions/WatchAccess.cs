// <copyright file="WatchAccess.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

using System.Text.Json.Serialization;

/// <summary>
/// The access mode for a memory watchpoint.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WatchAccess>))]
public enum WatchAccess
{
    /// <summary>
    /// Watch reads only.
    /// </summary>
    Read,

    /// <summary>
    /// Watch writes only.
    /// </summary>
    Write,

    /// <summary>
    /// Watch both reads and writes.
    /// </summary>
    ReadWrite,
}