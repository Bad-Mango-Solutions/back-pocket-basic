// <copyright file="OpenAlNativeLibrary.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

using System.Runtime.InteropServices;

using OpenTK.Audio.OpenAL;

using Serilog;

/// <summary>
/// Points OpenTK's OpenAL loader at the OpenAL Soft native library bundled with the application.
/// </summary>
/// <remarks>
/// <para>
/// OpenTK resolves OpenAL by fixed system names (<c>openal32.dll</c> on Windows,
/// <c>libopenal.so.1</c> on Linux, and the <c>OpenAL.framework</c> on macOS), none of which
/// match the files shipped by the <c>Silk.NET.OpenAL.Soft.Native</c> package
/// (<c>soft_oal.dll</c>, <c>libopenal.so</c>, and <c>libopenal.dylib</c> under
/// <c>runtimes/&lt;rid&gt;/native</c>). Windows has no system OpenAL, so without this bridge
/// audio would silently fall back to the null output there.
/// </para>
/// <para>
/// OpenTK already registers its own <see cref="NativeLibrary.SetDllImportResolver"/> for its
/// assembly, so the supported hook is <see cref="OpenALLibraryNameContainer.OverridePath"/>.
/// The bundled library is used only when it exists and loads; otherwise the override is left
/// unset and OpenTK falls back to the system library. A host-supplied override is never replaced.
/// </para>
/// </remarks>
internal static class OpenAlNativeLibrary
{
    /// <summary>
    /// The bundled OpenAL Soft file name on Windows.
    /// </summary>
    internal const string WindowsFileName = "soft_oal.dll";

    /// <summary>
    /// The bundled OpenAL Soft file name on Linux.
    /// </summary>
    internal const string LinuxFileName = "libopenal.so";

    /// <summary>
    /// The bundled OpenAL Soft file name on macOS.
    /// </summary>
    internal const string MacOSFileName = "libopenal.dylib";

    private static readonly Lock ConfigureLock = new();

    private static bool configured;

    /// <summary>
    /// Configures OpenTK to load the bundled OpenAL Soft library, once per process.
    /// </summary>
    /// <remarks>
    /// Must run before the first OpenAL call; OpenTK resolves the library on first use.
    /// </remarks>
    internal static void EnsureConfigured()
    {
        lock (ConfigureLock)
        {
            if (configured)
            {
                return;
            }

            configured = true;

            if (OpenALLibraryNameContainer.OverridePath is not null)
            {
                Log.ForContext(typeof(OpenAlNativeLibrary)).Debug(
                    "OpenAL library override already set to {Path}; leaving it unchanged",
                    OpenALLibraryNameContainer.OverridePath);
                return;
            }

            string? fileName = GetBundledFileName(RuntimeInformation.IsOSPlatform);
            string? path = fileName is null ? null : FindBundledLibrary(GetSearchDirectories(), fileName, File.Exists);

            if (path is not null && NativeLibrary.TryLoad(path, out _))
            {
                OpenALLibraryNameContainer.OverridePath = path;
                Log.ForContext(typeof(OpenAlNativeLibrary)).Debug("Using bundled OpenAL library {Path}", path);
            }
            else
            {
                Log.ForContext(typeof(OpenAlNativeLibrary)).Debug(
                    "Bundled OpenAL library {FileName} not found or not loadable (candidate {Path}); using the system OpenAL library",
                    fileName,
                    path);
            }
        }
    }

    /// <summary>
    /// Gets the file name of the bundled OpenAL Soft library for the current platform.
    /// </summary>
    /// <param name="isOSPlatform">A predicate reporting whether the process runs on a given platform.</param>
    /// <returns>The bundled file name, or <see langword="null"/> if no library is bundled for the platform.</returns>
    internal static string? GetBundledFileName(Func<OSPlatform, bool> isOSPlatform)
    {
        ArgumentNullException.ThrowIfNull(isOSPlatform);

        if (isOSPlatform(OSPlatform.Windows))
        {
            return WindowsFileName;
        }

        if (isOSPlatform(OSPlatform.Linux))
        {
            return LinuxFileName;
        }

        if (isOSPlatform(OSPlatform.OSX))
        {
            return MacOSFileName;
        }

        return null;
    }

    /// <summary>
    /// Finds the first directory containing the bundled library.
    /// </summary>
    /// <param name="directories">The directories to probe, in priority order.</param>
    /// <param name="fileName">The library file name.</param>
    /// <param name="fileExists">A predicate reporting whether a file exists.</param>
    /// <returns>The full path of the library, or <see langword="null"/> if it was not found.</returns>
    internal static string? FindBundledLibrary(IEnumerable<string> directories, string fileName, Func<string, bool> fileExists)
    {
        ArgumentNullException.ThrowIfNull(directories);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(fileExists);

        foreach (string directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string candidate = Path.GetFullPath(Path.Combine(directory, fileName));
            if (fileExists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the directories that may contain the bundled library, in priority order.
    /// </summary>
    /// <param name="nativeSearchDirectories">
    /// The host's native search directories (the <c>NATIVE_DLL_SEARCH_DIRECTORIES</c> app-context value),
    /// which include the <c>runtimes/&lt;rid&gt;/native</c> folders resolved from the deps file.
    /// </param>
    /// <param name="baseDirectory">The application base directory.</param>
    /// <param name="runtimeIdentifier">The runtime identifier of the current process.</param>
    /// <returns>The directories to probe.</returns>
    internal static IEnumerable<string> GetSearchDirectories(string? nativeSearchDirectories, string baseDirectory, string runtimeIdentifier)
    {
        if (!string.IsNullOrEmpty(nativeSearchDirectories))
        {
            foreach (string directory in nativeSearchDirectories.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return directory;
            }
        }

        yield return baseDirectory;
        yield return Path.Combine(baseDirectory, "runtimes", runtimeIdentifier, "native");
    }

    private static IEnumerable<string> GetSearchDirectories() =>
        GetSearchDirectories(
            AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string,
            AppContext.BaseDirectory,
            RuntimeInformation.RuntimeIdentifier);
}