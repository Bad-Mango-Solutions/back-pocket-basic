// <copyright file="FilePathResolver.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor;

/// <summary>
/// Resolves file paths including support for the library:// scheme.
/// </summary>
/// <remarks>
/// <para>
/// The library:// scheme maps to the user's profile directory (~/.backpocket).
/// For example, library://roms/test.bin resolves to ~/.backpocket/roms/test.bin.
/// </para>
/// <para>
/// The embedded:// scheme is explicitly NOT supported for the text editor.
/// </para>
/// </remarks>
public static class FilePathResolver
{
    /// <summary>
    /// The library path scheme prefix.
    /// </summary>
    public const string LibraryScheme = "library://";

    /// <summary>
    /// The embedded path scheme prefix (not supported).
    /// </summary>
    public const string EmbeddedScheme = "embedded://";

    /// <summary>
    /// Gets the library root directory (user's home directory + .backpocket).
    /// </summary>
    /// <returns>The library root path.</returns>
    public static string GetLibraryRoot()
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".backpocket");
    }

    /// <summary>
    /// Resolves a path that may include the library:// scheme.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>
    /// The resolved absolute file path, or the original path if it's already an absolute path.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the path uses the unsupported embedded:// scheme.
    /// </exception>
    public static string ResolvePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (path.StartsWith(EmbeddedScheme, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The embedded:// scheme is not supported for the text editor.",
                nameof(path));
        }

        if (path.StartsWith(LibraryScheme, StringComparison.OrdinalIgnoreCase))
        {
            string relativePath = path[LibraryScheme.Length..];
            return Path.Combine(GetLibraryRoot(), relativePath);
        }

        return path;
    }

    /// <summary>
    /// Converts an absolute path to a library:// path if it's within the library root.
    /// </summary>
    /// <param name="absolutePath">The absolute file path.</param>
    /// <returns>
    /// A library:// path if the file is within the library root,
    /// otherwise the original absolute path.
    /// </returns>
    public static string ToLibraryPath(string absolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        string libraryRoot = GetLibraryRoot();
        if (absolutePath.StartsWith(libraryRoot, StringComparison.OrdinalIgnoreCase))
        {
            string relativePath = absolutePath[(libraryRoot.Length + 1)..];
            return LibraryScheme + relativePath;
        }

        return absolutePath;
    }

    /// <summary>
    /// Determines whether a path uses the library:// scheme.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path uses the library:// scheme; otherwise, <c>false</c>.</returns>
    public static bool IsLibraryPath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               path.StartsWith(LibraryScheme, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a path uses the unsupported embedded:// scheme.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> if the path uses the embedded:// scheme; otherwise, <c>false</c>.</returns>
    public static bool IsEmbeddedPath(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               path.StartsWith(EmbeddedScheme, StringComparison.OrdinalIgnoreCase);
    }
}