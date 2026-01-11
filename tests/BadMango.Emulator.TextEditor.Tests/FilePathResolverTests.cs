// <copyright file="FilePathResolverTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor.Tests;

/// <summary>
/// Unit tests for the <see cref="FilePathResolver"/> class.
/// </summary>
[TestFixture]
public class FilePathResolverTests
{
    /// <summary>
    /// Verifies that GetLibraryRoot returns a path ending with .backpocket.
    /// </summary>
    [Test]
    public void GetLibraryRoot_ReturnsPathEndingWithBackpocket()
    {
        var result = FilePathResolver.GetLibraryRoot();

        Assert.That(result, Does.EndWith(".backpocket"));
    }

    /// <summary>
    /// Verifies that ResolvePath throws for null input.
    /// </summary>
    [Test]
    public void ResolvePath_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FilePathResolver.ResolvePath(null!));
    }

    /// <summary>
    /// Verifies that ResolvePath throws for empty string.
    /// </summary>
    [Test]
    public void ResolvePath_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => FilePathResolver.ResolvePath(string.Empty));
    }

    /// <summary>
    /// Verifies that ResolvePath throws for embedded:// scheme.
    /// </summary>
    [Test]
    public void ResolvePath_EmbeddedScheme_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FilePathResolver.ResolvePath("embedded://resource.txt"));

        Assert.That(ex.Message, Does.Contain("embedded://"));
    }

    /// <summary>
    /// Verifies that ResolvePath resolves library:// scheme correctly.
    /// </summary>
    [Test]
    public void ResolvePath_LibraryScheme_ResolvesToLibraryRoot()
    {
        var result = FilePathResolver.ResolvePath("library://roms/test.bin");

        var expected = Path.Combine(FilePathResolver.GetLibraryRoot(), "roms", "test.bin");
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that ResolvePath returns absolute paths unchanged.
    /// </summary>
    [Test]
    public void ResolvePath_AbsolutePath_ReturnsUnchanged()
    {
        var absolutePath = Path.GetTempPath() + "test.txt";

        var result = FilePathResolver.ResolvePath(absolutePath);

        Assert.That(result, Is.EqualTo(absolutePath));
    }

    /// <summary>
    /// Verifies that IsLibraryPath returns true for library:// paths.
    /// </summary>
    [Test]
    public void IsLibraryPath_LibraryPath_ReturnsTrue()
    {
        var result = FilePathResolver.IsLibraryPath("library://test.txt");

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Verifies that IsLibraryPath returns false for regular paths.
    /// </summary>
    [Test]
    public void IsLibraryPath_RegularPath_ReturnsFalse()
    {
        var result = FilePathResolver.IsLibraryPath("/home/user/test.txt");

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that IsLibraryPath returns false for null.
    /// </summary>
    [Test]
    public void IsLibraryPath_Null_ReturnsFalse()
    {
        var result = FilePathResolver.IsLibraryPath(null);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that IsEmbeddedPath returns true for embedded:// paths.
    /// </summary>
    [Test]
    public void IsEmbeddedPath_EmbeddedPath_ReturnsTrue()
    {
        var result = FilePathResolver.IsEmbeddedPath("embedded://resource.txt");

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Verifies that IsEmbeddedPath returns false for regular paths.
    /// </summary>
    [Test]
    public void IsEmbeddedPath_RegularPath_ReturnsFalse()
    {
        var result = FilePathResolver.IsEmbeddedPath("/home/user/test.txt");

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that ToLibraryPath converts paths within library root.
    /// </summary>
    [Test]
    public void ToLibraryPath_PathInLibraryRoot_ReturnsLibraryPath()
    {
        var libraryRoot = FilePathResolver.GetLibraryRoot();
        var absolutePath = Path.Combine(libraryRoot, "roms", "test.bin");

        var result = FilePathResolver.ToLibraryPath(absolutePath);

        Assert.That(result, Does.StartWith("library://"));
    }

    /// <summary>
    /// Verifies that ToLibraryPath returns absolute path for paths outside library root.
    /// </summary>
    [Test]
    public void ToLibraryPath_PathOutsideLibraryRoot_ReturnsAbsolutePath()
    {
        var absolutePath = Path.GetTempPath() + "test.txt";

        var result = FilePathResolver.ToLibraryPath(absolutePath);

        Assert.That(result, Is.EqualTo(absolutePath));
    }

    /// <summary>
    /// Verifies that library:// scheme is case-insensitive.
    /// </summary>
    [Test]
    public void ResolvePath_UppercaseLibraryScheme_ResolvesCorrectly()
    {
        var result = FilePathResolver.ResolvePath("LIBRARY://test.txt");

        Assert.That(result, Does.Contain(".backpocket"));
    }
}