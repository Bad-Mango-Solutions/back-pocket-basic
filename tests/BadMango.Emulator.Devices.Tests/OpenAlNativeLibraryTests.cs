// <copyright file="OpenAlNativeLibraryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using System.Runtime.InteropServices;

using BadMango.Emulator.Devices.Speaker;

/// <summary>
/// Unit tests for <see cref="OpenAlNativeLibrary"/>.
/// </summary>
[TestFixture]
public class OpenAlNativeLibraryTests
{
    /// <summary>
    /// Verifies that each supported platform maps to the file shipped by Silk.NET.OpenAL.Soft.Native.
    /// </summary>
    /// <param name="platform">The platform name.</param>
    /// <param name="expected">The expected bundled file name.</param>
    [TestCase("WINDOWS", "soft_oal.dll")]
    [TestCase("LINUX", "libopenal.so")]
    [TestCase("OSX", "libopenal.dylib")]
    public void GetBundledFileName_MapsPlatformToBundledFile(string platform, string expected)
    {
        var target = OSPlatform.Create(platform);

        string? fileName = OpenAlNativeLibrary.GetBundledFileName(p => p == target);

        Assert.That(fileName, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that platforms without a bundled library defer to the system library.
    /// </summary>
    [Test]
    public void GetBundledFileName_UnknownPlatform_ReturnsNull()
    {
        string? fileName = OpenAlNativeLibrary.GetBundledFileName(_ => false);

        Assert.That(fileName, Is.Null);
    }

    /// <summary>
    /// Verifies that the first directory containing the library wins.
    /// </summary>
    [Test]
    public void FindBundledLibrary_ReturnsFirstMatchInPriorityOrder()
    {
        string first = Path.Combine(Path.GetTempPath(), "first");
        string second = Path.Combine(Path.GetTempPath(), "second");
        string third = Path.Combine(Path.GetTempPath(), "third");
        var existing = new HashSet<string>
        {
            Path.GetFullPath(Path.Combine(second, "libopenal.so")),
            Path.GetFullPath(Path.Combine(third, "libopenal.so")),
        };

        string? path = OpenAlNativeLibrary.FindBundledLibrary([first, string.Empty, second, third], "libopenal.so", existing.Contains);

        Assert.That(path, Is.EqualTo(Path.GetFullPath(Path.Combine(second, "libopenal.so"))));
    }

    /// <summary>
    /// Verifies that a missing library yields <see langword="null"/> so the system library is used.
    /// </summary>
    [Test]
    public void FindBundledLibrary_NotFound_ReturnsNull()
    {
        string? path = OpenAlNativeLibrary.FindBundledLibrary([Path.GetTempPath()], "soft_oal.dll", _ => false);

        Assert.That(path, Is.Null);
    }

    /// <summary>
    /// Verifies that host search directories are probed before the base and RID-specific folders.
    /// </summary>
    [Test]
    public void GetSearchDirectories_OrdersHostDirectoriesFirst()
    {
        string hostA = Path.Combine("host", "a");
        string hostB = Path.Combine("host", "b");
        string baseDirectory = Path.Combine("app");
        string native = $"{hostA}{Path.PathSeparator}{hostB}{Path.PathSeparator}";

        var directories = OpenAlNativeLibrary.GetSearchDirectories(native, baseDirectory, "linux-x64").ToList();

        Assert.That(
            directories,
            Is.EqualTo(new[] { hostA, hostB, baseDirectory, Path.Combine(baseDirectory, "runtimes", "linux-x64", "native") }));
    }

    /// <summary>
    /// Verifies that a missing host search path still probes the base and RID-specific folders.
    /// </summary>
    [Test]
    public void GetSearchDirectories_NoHostDirectories_ProbesBaseAndRidFolders()
    {
        var directories = OpenAlNativeLibrary.GetSearchDirectories(null, "app", "win-x64").ToList();

        Assert.That(directories, Is.EqualTo(new[] { "app", Path.Combine("app", "runtimes", "win-x64", "native") }));
    }

    /// <summary>
    /// Verifies that the bundled OpenAL Soft library for the current platform is copied to the build output.
    /// </summary>
    [Test]
    public void BundledLibrary_IsPresentInBuildOutput()
    {
        string? fileName = OpenAlNativeLibrary.GetBundledFileName(RuntimeInformation.IsOSPlatform);
        Assume.That(fileName, Is.Not.Null, "No OpenAL Soft library is bundled for this platform.");

        string runtimes = Path.Combine(AppContext.BaseDirectory, "runtimes");
        bool found = Directory.Exists(runtimes)
            && Directory.EnumerateFiles(runtimes, fileName!, SearchOption.AllDirectories).Any();

        Assert.That(found, Is.True, $"Expected {fileName} under {runtimes}.");
    }
}