// <copyright file="PathValidator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.IO;

using Microsoft.Extensions.Logging;

/// <summary>
/// Service for validating and normalizing path settings.
/// </summary>
public class PathValidator : IPathValidator
{
    private readonly ILogger<PathValidator>? logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathValidator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for path validation operations.</param>
    public PathValidator(ILogger<PathValidator>? logger = null)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public PathValidationResult Validate(string path, PathPurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(path);

        var warnings = new List<PathValidationWarning>();
        var normalizedPath = Normalize(path);

        // Check for empty path
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return new()
            {
                IsValid = false,
                ErrorMessage = "Path cannot be empty.",
            };
        }

        // Check for invalid path characters
        try
        {
            _ = Path.GetFullPath(normalizedPath);
        }
        catch (ArgumentException ex)
        {
            return new()
            {
                IsValid = false,
                ErrorMessage = $"Path contains invalid characters: {ex.Message}",
            };
        }
        catch (NotSupportedException ex)
        {
            return new()
            {
                IsValid = false,
                ErrorMessage = $"Path format is not supported: {ex.Message}",
            };
        }

        // Check if directory exists
        if (!Directory.Exists(normalizedPath))
        {
            warnings.Add(PathValidationWarning.DirectoryDoesNotExist);
        }

        // Check for network path (UNC)
        if (normalizedPath.StartsWith(@"\\", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("//", StringComparison.Ordinal))
        {
            warnings.Add(PathValidationWarning.NetworkPath);
        }

        // Try to check permissions if directory exists
        // Note: This creates a temporary file to test write permissions.
        // For performance-sensitive scenarios, consider caching results or
        // using platform-specific APIs to check permissions without file I/O.
        if (Directory.Exists(normalizedPath))
        {
            try
            {
                // Attempt to get directory info as a basic permission check
                var testFile = Path.Combine(normalizedPath, $".backpocket_test_{Guid.NewGuid():N}");
                using (File.Create(testFile, 1, FileOptions.DeleteOnClose))
                {
                    // File created and will be deleted on close
                }
            }
            catch (UnauthorizedAccessException)
            {
                warnings.Add(PathValidationWarning.InsufficientPermissions);
            }
            catch (IOException)
            {
                // May indicate disk issues or other I/O problems
                warnings.Add(PathValidationWarning.InsufficientPermissions);
            }
        }

        logger?.LogDebug("Path validation completed for {Path} ({Purpose}): valid with {WarningCount} warnings", path, purpose, warnings.Count);

        return new()
        {
            IsValid = true,
            NormalizedPath = normalizedPath,
            Warnings = warnings,
        };
    }

    /// <inheritdoc/>
    public string Normalize(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var normalized = path;

        // Expand ~ to home directory
        if (normalized == "~")
        {
            normalized = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else if (normalized.StartsWith("~/", StringComparison.Ordinal))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Extract the part after "~/" and combine with home directory
            // Using substring starting at index 2 to skip "~/"
            var relativePart = normalized.Substring(2);
            normalized = Path.Combine(homeDir, relativePart);
        }

        // Expand environment variables
        normalized = Environment.ExpandEnvironmentVariables(normalized);

        // Use Path.GetFullPath to normalize path separators and resolve relative paths
        // This handles both forward and backward slashes and removes trailing separators
        try
        {
            normalized = Path.GetFullPath(normalized);

            // Use Path.TrimEndingDirectorySeparator to remove trailing separator
            normalized = Path.TrimEndingDirectorySeparator(normalized);
        }
        catch (ArgumentException)
        {
            // If the path contains invalid characters, return as-is
            // Validation will catch this later
        }

        return normalized;
    }

    /// <inheritdoc/>
    public async Task<bool> EnsureDirectoryExistsAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var normalizedPath = Normalize(path);

        try
        {
            if (!Directory.Exists(normalizedPath))
            {
                await Task.Run(() => Directory.CreateDirectory(normalizedPath)).ConfigureAwait(false);
                logger?.LogInformation("Created directory: {Path}", normalizedPath);
            }

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger?.LogError(ex, "Insufficient permissions to create directory: {Path}", normalizedPath);
            return false;
        }
        catch (IOException ex)
        {
            logger?.LogError(ex, "Failed to create directory: {Path}", normalizedPath);
            return false;
        }
    }
}