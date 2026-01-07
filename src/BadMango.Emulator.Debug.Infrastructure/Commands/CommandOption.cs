// <copyright file="CommandOption.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Describes a command-line option for a debug command.
/// </summary>
/// <param name="Name">The full name of the option (e.g., "--trace").</param>
/// <param name="ShortName">The short alias for the option (e.g., "-t"), or null if none.</param>
/// <param name="Type">The type of the option value (e.g., "flag", "int", "address").</param>
/// <param name="Description">A description of what the option does.</param>
/// <param name="DefaultValue">The default value if not specified, or null for required options.</param>
public sealed record CommandOption(
    string Name,
    string? ShortName,
    string Type,
    string Description,
    string? DefaultValue);