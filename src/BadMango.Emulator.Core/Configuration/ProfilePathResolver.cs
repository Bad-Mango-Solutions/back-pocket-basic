// <copyright file="ProfilePathResolver.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

/// <summary>
/// Resolves paths specified in machine profiles to absolute file system paths.
/// </summary>
/// <remarks>
/// <para>
/// This resolver supports multiple path schemes commonly used in machine profiles:
/// </para>
/// <list type="bullet">
/// <item><description><c>library://path</c> - Resolves relative to the library root (user's ROM/resource collection)</description></item>
/// <item><description><c>app://path</c> - Resolves relative to the application's base directory</description></item>
/// <item><description>Absolute paths - Used as-is after normalization</description></item>
/// <item><description>Relative paths - Resolved relative to the profile file location, or app directory if unknown</description></item>
/// </list>
/// </remarks>
public sealed class ProfilePathResolver
{
    private const string LibraryScheme = "library://";
    private const string AppScheme = "app://";

    private readonly string? libraryRoot;
    private readonly string appRoot;
    private readonly string? profileDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilePathResolver"/> class.
    /// </summary>
    /// <param name="libraryRoot">
    /// The root directory for library resources. Can be <see langword="null"/> if library paths are not supported.
    /// </param>
    /// <param name="profileFilePath">
    /// The full path to the profile file being processed. Used to resolve relative paths.
    /// Can be <see langword="null"/> if the profile was created in-memory.
    /// </param>
    public ProfilePathResolver(string? libraryRoot, string? profileFilePath = null)
    {
        this.libraryRoot = libraryRoot;
        appRoot = AppContext.BaseDirectory;
        profileDirectory = profileFilePath is not null
            ? Path.GetDirectoryName(Path.GetFullPath(profileFilePath))
            : null;
    }

    /// <summary>
    /// Gets a value indicating whether library path resolution is available.
    /// </summary>
    public bool HasLibraryRoot => libraryRoot is not null;

    /// <summary>
    /// Gets the library root directory, if configured.
    /// </summary>
    public string? LibraryRoot => libraryRoot;

    /// <summary>
    /// Gets the application root directory.
    /// </summary>
    public string AppRoot => appRoot;

    /// <summary>
    /// Gets the profile directory, if a profile file path was provided.
    /// </summary>
    public string? ProfileDirectory => profileDirectory;

    /// <summary>
    /// Resolves a path from a machine profile to an absolute file system path.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The resolved absolute path.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a library path is specified but no library root is configured.
    /// </exception>
    public string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Check for library:// scheme
        if (path.StartsWith(LibraryScheme, StringComparison.OrdinalIgnoreCase))
        {
            if (libraryRoot is null)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve '{path}': Library root not configured.");
            }

            return Path.GetFullPath(Path.Combine(libraryRoot, path[LibraryScheme.Length..]));
        }

        // Check for app:// scheme
        if (path.StartsWith(AppScheme, StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(Path.Combine(appRoot, path[AppScheme.Length..]));
        }

        // Check for absolute path
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        // Relative path: resolve relative to profile directory if available, otherwise app root
        if (profileDirectory is not null)
        {
            return Path.GetFullPath(Path.Combine(profileDirectory, path));
        }

        return Path.GetFullPath(Path.Combine(appRoot, path));
    }

    /// <summary>
    /// Tries to resolve a path from a machine profile to an absolute file system path.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <param name="resolvedPath">When this method returns, contains the resolved path if successful.</param>
    /// <returns><see langword="true"/> if resolution was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryResolve(string? path, out string? resolvedPath)
    {
        resolvedPath = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            resolvedPath = Resolve(path);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}