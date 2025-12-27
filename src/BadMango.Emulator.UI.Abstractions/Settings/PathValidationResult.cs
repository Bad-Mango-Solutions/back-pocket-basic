// <copyright file="PathValidationResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// Result of a path validation operation.
/// </summary>
public record PathValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the path is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the normalized path, if validation was successful.
    /// </summary>
    public string? NormalizedPath { get; init; }

    /// <summary>
    /// Gets the error message, if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the warnings generated during validation.
    /// </summary>
    public IReadOnlyList<PathValidationWarning> Warnings { get; init; } = [];
}