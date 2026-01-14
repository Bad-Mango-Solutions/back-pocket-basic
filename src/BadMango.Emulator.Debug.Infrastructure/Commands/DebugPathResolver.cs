// <copyright file="DebugPathResolver.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Core.Configuration;

/// <summary>
/// Default implementation of <see cref="IDebugPathResolver"/> that delegates to <see cref="ProfilePathResolver"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses the system/user default library root directory
/// (~/.backpocket or OS equivalent). The library root is determined automatically
/// based on the user's home directory.
/// </para>
/// <para>
/// Path resolution supports:
/// </para>
/// <list type="bullet">
/// <item><description><c>library://path</c> - Resolves to ~/.backpocket/path</description></item>
/// <item><description><c>app://path</c> - Resolves to application base directory</description></item>
/// <item><description>Absolute paths - Used as-is after normalization</description></item>
/// <item><description>Relative paths - Resolved relative to current directory</description></item>
/// </list>
/// </remarks>
public sealed class DebugPathResolver : IDebugPathResolver
{
    private readonly ProfilePathResolver resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugPathResolver"/> class
    /// with the default library root directory.
    /// </summary>
    /// <remarks>
    /// The library root is set to ~/.backpocket (or OS equivalent).
    /// </remarks>
    public DebugPathResolver()
        : this(GetDefaultLibraryRoot())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugPathResolver"/> class
    /// with a specified library root directory.
    /// </summary>
    /// <param name="libraryRoot">
    /// The library root directory. Can be <see langword="null"/> if library paths are not supported.
    /// </param>
    public DebugPathResolver(string? libraryRoot)
    {
        resolver = new ProfilePathResolver(libraryRoot);
    }

    /// <inheritdoc/>
    public bool HasLibraryRoot => resolver.HasLibraryRoot;

    /// <inheritdoc/>
    public string? LibraryRoot => resolver.LibraryRoot;

    /// <inheritdoc/>
    public string Resolve(string path)
    {
        return resolver.Resolve(path);
    }

    /// <inheritdoc/>
    public bool TryResolve(string? path, out string? resolvedPath)
    {
        return resolver.TryResolve(path, out resolvedPath);
    }

    /// <summary>
    /// Gets the default library root directory (~/.backpocket or OS equivalent).
    /// </summary>
    /// <returns>The default library root path.</returns>
    private static string GetDefaultLibraryRoot()
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".backpocket");
    }
}