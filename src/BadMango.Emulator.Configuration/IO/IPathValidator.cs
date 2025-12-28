// <copyright file="IPathValidator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.IO;

/// <summary>
/// Service interface for validating and normalizing path settings.
/// </summary>
public interface IPathValidator
{
    /// <summary>
    /// Validates a path for the specified purpose.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="purpose">The intended purpose of the path.</param>
    /// <returns>The validation result.</returns>
    PathValidationResult Validate(string path, PathPurpose purpose);

    /// <summary>
    /// Normalizes a path by expanding variables and resolving relative paths.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    string Normalize(string path);

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The directory path to ensure exists.</param>
    /// <returns>A task that returns true if the directory exists or was created; otherwise, false.</returns>
    Task<bool> EnsureDirectoryExistsAsync(string path);
}