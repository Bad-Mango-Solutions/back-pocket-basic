// <copyright file="ProfilePathResolverTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core.Configuration;

/// <summary>
/// Unit tests for the <see cref="ProfilePathResolver"/> class.
/// </summary>
[TestFixture]
public class ProfilePathResolverTests
{
    private string testDirectory = null!;

    /// <summary>
    /// Sets up the test directory before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"ProfilePathResolverTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
    }

    /// <summary>
    /// Cleans up the test directory after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that Resolve throws for null path.
    /// </summary>
    [Test]
    public void Resolve_NullPath_ThrowsArgumentException()
    {
        var resolver = new ProfilePathResolver(null);
        Assert.That(() => resolver.Resolve(null!), Throws.InstanceOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that Resolve throws for empty path.
    /// </summary>
    [Test]
    public void Resolve_EmptyPath_ThrowsArgumentException()
    {
        var resolver = new ProfilePathResolver(null);
        Assert.That(() => resolver.Resolve(string.Empty), Throws.InstanceOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that Resolve correctly resolves library:// paths.
    /// </summary>
    [Test]
    public void Resolve_LibraryPath_ResolvesRelativeToLibraryRoot()
    {
        var libraryRoot = Path.Combine(testDirectory, "library");
        var resolver = new ProfilePathResolver(libraryRoot);

        var result = resolver.Resolve("library://roms/test.bin");

        var expected = Path.GetFullPath(Path.Combine(libraryRoot, "roms/test.bin"));
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that Resolve throws for library:// paths when library root is not configured.
    /// </summary>
    [Test]
    public void Resolve_LibraryPathWithNoRoot_ThrowsInvalidOperationException()
    {
        var resolver = new ProfilePathResolver(null);

        Assert.That(
            () => resolver.Resolve("library://roms/test.bin"),
            Throws.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    /// Verifies that Resolve correctly resolves app:// paths.
    /// </summary>
    [Test]
    public void Resolve_AppPath_ResolvesRelativeToAppRoot()
    {
        var resolver = new ProfilePathResolver(null);

        var result = resolver.Resolve("app://data/test.bin");

        var expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "data/test.bin"));
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that Resolve returns absolute paths unchanged.
    /// </summary>
    [Test]
    public void Resolve_AbsolutePath_ReturnsNormalizedPath()
    {
        var resolver = new ProfilePathResolver(null);
        var absolutePath = Path.Combine(testDirectory, "test.bin");

        var result = resolver.Resolve(absolutePath);

        Assert.That(result, Is.EqualTo(Path.GetFullPath(absolutePath)));
    }

    /// <summary>
    /// Verifies that Resolve resolves relative paths relative to profile directory.
    /// </summary>
    [Test]
    public void Resolve_RelativePathWithProfileDirectory_ResolvesRelativeToProfileDir()
    {
        var profilePath = Path.Combine(testDirectory, "profiles", "test.json");
        Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);
        var resolver = new ProfilePathResolver(null, profilePath);

        var result = resolver.Resolve("../roms/test.bin");

        var expected = Path.GetFullPath(Path.Combine(testDirectory, "roms/test.bin"));
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that Resolve resolves relative paths relative to app root when no profile directory.
    /// </summary>
    [Test]
    public void Resolve_RelativePathWithNoProfileDirectory_ResolvesRelativeToAppRoot()
    {
        var resolver = new ProfilePathResolver(null);

        var result = resolver.Resolve("data/test.bin");

        var expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "data/test.bin"));
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that HasLibraryRoot returns correct value.
    /// </summary>
    [Test]
    public void HasLibraryRoot_WithLibraryRoot_ReturnsTrue()
    {
        var resolver = new ProfilePathResolver("/some/path");
        Assert.That(resolver.HasLibraryRoot, Is.True);
    }

    /// <summary>
    /// Verifies that HasLibraryRoot returns correct value when no root.
    /// </summary>
    [Test]
    public void HasLibraryRoot_WithoutLibraryRoot_ReturnsFalse()
    {
        var resolver = new ProfilePathResolver(null);
        Assert.That(resolver.HasLibraryRoot, Is.False);
    }

    /// <summary>
    /// Verifies that TryResolve returns true for valid paths.
    /// </summary>
    [Test]
    public void TryResolve_ValidPath_ReturnsTrueAndResolvedPath()
    {
        var resolver = new ProfilePathResolver(null);

        bool success = resolver.TryResolve("app://test.bin", out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that TryResolve returns false for null path.
    /// </summary>
    [Test]
    public void TryResolve_NullPath_ReturnsFalse()
    {
        var resolver = new ProfilePathResolver(null);

        bool success = resolver.TryResolve(null, out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that TryResolve returns false for library paths without root.
    /// </summary>
    [Test]
    public void TryResolve_LibraryPathWithNoRoot_ReturnsFalse()
    {
        var resolver = new ProfilePathResolver(null);

        bool success = resolver.TryResolve("library://test.bin", out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }
}