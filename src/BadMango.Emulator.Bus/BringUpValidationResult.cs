// <copyright file="BringUpValidationResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents the result of validating a provisioning bundle against machine constraints.
/// </summary>
/// <param name="IsValid">Whether the bundle passes validation.</param>
/// <param name="Errors">Collection of validation error messages, if any.</param>
/// <param name="Warnings">Collection of validation warning messages, if any.</param>
public readonly record struct BringUpValidationResult(
    bool IsValid,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Warnings)
{
    /// <summary>
    /// Creates a successful validation result with no errors or warnings.
    /// </summary>
    /// <returns>A valid result.</returns>
    public static BringUpValidationResult Valid() =>
        new(true, Array.Empty<string>(), Array.Empty<string>());

    /// <summary>
    /// Creates a successful validation result with warnings.
    /// </summary>
    /// <param name="warnings">The warning messages.</param>
    /// <returns>A valid result with warnings.</returns>
    public static BringUpValidationResult ValidWithWarnings(IReadOnlyCollection<string> warnings) =>
        new(true, Array.Empty<string>(), warnings);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    /// <returns>An invalid result.</returns>
    public static BringUpValidationResult Invalid(IReadOnlyCollection<string> errors) =>
        new(false, errors, Array.Empty<string>());

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>An invalid result.</returns>
    public static BringUpValidationResult Invalid(string error) =>
        new(false, [error], Array.Empty<string>());
}