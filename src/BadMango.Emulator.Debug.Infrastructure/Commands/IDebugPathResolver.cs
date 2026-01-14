// <copyright file="IDebugPathResolver.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Provides path resolution services for debug commands.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts path resolution for debugger commands, supporting:
/// </para>
/// <list type="bullet">
/// <item><description><c>library://path</c> - Resolves to the user's library directory (~/.backpocket)</description></item>
/// <item><description><c>app://path</c> - Resolves to the application's base directory</description></item>
/// <item><description>Absolute paths - Used as-is</description></item>
/// <item><description>Relative paths - Resolved relative to the current working directory</description></item>
/// </list>
/// <para>
/// The <c>embedded://</c> scheme is not supported as debugger commands work with file system paths.
/// </para>
/// </remarks>
public interface IDebugPathResolver
{
    /// <summary>
    /// Gets a value indicating whether library path resolution is available.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if a library root directory is configured,
    /// <see langword="false"/> otherwise. When <see langword="false"/>, attempts to
    /// resolve <c>library://</c> paths will fail.
    /// </remarks>
    bool HasLibraryRoot { get; }

    /// <summary>
    /// Gets the library root directory, if configured.
    /// </summary>
    /// <remarks>
    /// Returns the absolute path to the library root directory (e.g., ~/.backpocket),
    /// or <see langword="null"/> if not configured.
    /// </remarks>
    string? LibraryRoot { get; }

    /// <summary>
    /// Attempts to resolve a path to an absolute file system path.
    /// </summary>
    /// <param name="path">The path to resolve. May use <c>library://</c> or <c>app://</c> schemes.</param>
    /// <param name="resolvedPath">
    /// When this method returns <see langword="true"/>, contains the resolved absolute path.
    /// When this method returns <see langword="false"/>, contains <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the path was successfully resolved;
    /// <see langword="false"/> if resolution failed (e.g., library root not configured for library:// paths).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method does not validate that the resolved path exists; it only performs
    /// scheme-based resolution.
    /// </para>
    /// <para>
    /// The <c>embedded://</c> scheme is not supported and will return <see langword="false"/>.
    /// </para>
    /// </remarks>
    bool TryResolve(string? path, out string? resolvedPath);

    /// <summary>
    /// Resolves a path to an absolute file system path.
    /// </summary>
    /// <param name="path">The path to resolve. May use <c>library://</c> or <c>app://</c> schemes.</param>
    /// <returns>The resolved absolute path.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a <c>library://</c> path is specified but no library root is configured,
    /// or when an <c>embedded://</c> path is specified (not supported for debug commands).
    /// </exception>
    string Resolve(string path);
}