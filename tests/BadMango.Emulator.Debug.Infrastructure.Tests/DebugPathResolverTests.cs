// <copyright file="DebugPathResolverTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Unit tests for <see cref="DebugPathResolver"/>.
/// </summary>
[TestFixture]
public class DebugPathResolverTests
{
    /// <summary>
    /// Verifies that the default constructor sets a library root.
    /// </summary>
    [Test]
    public void Constructor_Default_HasLibraryRoot()
    {
        var resolver = new DebugPathResolver();

        Assert.Multiple(() =>
        {
            Assert.That(resolver.HasLibraryRoot, Is.True);
            Assert.That(resolver.LibraryRoot, Is.Not.Null);
            Assert.That(resolver.LibraryRoot, Does.EndWith(".backpocket"));
        });
    }

    /// <summary>
    /// Verifies that the constructor with null library root has no library root.
    /// </summary>
    [Test]
    public void Constructor_WithNullLibraryRoot_HasNoLibraryRoot()
    {
        var resolver = new DebugPathResolver(null);

        Assert.Multiple(() =>
        {
            Assert.That(resolver.HasLibraryRoot, Is.False);
            Assert.That(resolver.LibraryRoot, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that Resolve correctly resolves library:// paths.
    /// </summary>
    [Test]
    public void Resolve_LibraryPath_ResolvesToLibraryRoot()
    {
        var resolver = new DebugPathResolver("/test/library");

        var result = resolver.Resolve("library://roms/test.bin");

        Assert.That(result, Does.EndWith(Path.Combine("roms", "test.bin")));
        Assert.That(result, Does.StartWith("/test/library"));
    }

    /// <summary>
    /// Verifies that Resolve throws for library:// paths when library root is not configured.
    /// </summary>
    [Test]
    public void Resolve_LibraryPath_WhenLibraryRootNotSet_ThrowsException()
    {
        var resolver = new DebugPathResolver(null);

        Assert.That(
            () => resolver.Resolve("library://roms/test.bin"),
            Throws.TypeOf<InvalidOperationException>());
    }

    /// <summary>
    /// Verifies that TryResolve returns false for library:// paths when library root is not set.
    /// </summary>
    [Test]
    public void TryResolve_LibraryPath_WhenLibraryRootNotSet_ReturnsFalse()
    {
        var resolver = new DebugPathResolver(null);

        bool success = resolver.TryResolve("library://test.bin", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that TryResolve returns true for library:// paths when library root is set.
    /// </summary>
    [Test]
    public void TryResolve_LibraryPath_WhenLibraryRootSet_ReturnsTrue()
    {
        var resolver = new DebugPathResolver("/test/library");

        bool success = resolver.TryResolve("library://test.bin", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith("/test/library"));
        });
    }

    /// <summary>
    /// Verifies that TryResolve returns false for null paths.
    /// </summary>
    [Test]
    public void TryResolve_NullPath_ReturnsFalse()
    {
        var resolver = new DebugPathResolver();

        bool success = resolver.TryResolve(null, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that TryResolve returns false for empty paths.
    /// </summary>
    [Test]
    public void TryResolve_EmptyPath_ReturnsFalse()
    {
        var resolver = new DebugPathResolver();

        bool success = resolver.TryResolve(string.Empty, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that TryResolve returns false for whitespace-only paths.
    /// </summary>
    [Test]
    public void TryResolve_WhitespacePath_ReturnsFalse()
    {
        var resolver = new DebugPathResolver();

        bool success = resolver.TryResolve("   ", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that TryResolve correctly handles absolute paths.
    /// </summary>
    [Test]
    public void TryResolve_AbsolutePath_ReturnsNormalizedPath()
    {
        var resolver = new DebugPathResolver();

        bool success = resolver.TryResolve("/home/user/test.bin", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("test.bin"));
        });
    }

    /// <summary>
    /// Verifies that TryResolve returns false for embedded:// paths.
    /// </summary>
    [Test]
    public void TryResolve_EmbeddedPath_ReturnsFalse()
    {
        var resolver = new DebugPathResolver();

        bool success = resolver.TryResolve("embedded://Assembly/Resource", out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that library:// scheme is case-insensitive.
    /// </summary>
    [Test]
    public void TryResolve_LibraryPathCaseInsensitive_ReturnsTrue()
    {
        var resolver = new DebugPathResolver("/test/library");

        bool success1 = resolver.TryResolve("library://test.bin", out _);
        bool success2 = resolver.TryResolve("LIBRARY://test.bin", out _);
        bool success3 = resolver.TryResolve("Library://test.bin", out _);

        Assert.Multiple(() =>
        {
            Assert.That(success1, Is.True);
            Assert.That(success2, Is.True);
            Assert.That(success3, Is.True);
        });
    }
}