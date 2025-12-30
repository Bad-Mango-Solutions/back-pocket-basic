// <copyright file="EventHandle.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents a handle for an event within the emulator bus system.
/// </summary>
/// <remarks>
/// This structure is used to uniquely identify an event by its <see cref="Id"/>.
/// </remarks>
/// <param name="Id">The unique identifier for the event.</param>
public readonly record struct EventHandle(ulong Id);