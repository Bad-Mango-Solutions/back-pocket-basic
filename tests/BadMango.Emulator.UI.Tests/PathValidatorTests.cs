// <copyright file="PathValidatorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.Abstractions.Settings;
using BadMango.Emulator.UI.Services;

/// <summary>
/// Tests for <see cref="PathValidator"/>.
/// </summary>
[TestFixture]
public class PathValidatorTests
{
    private string tempDirectory = null!;
    private PathValidator validator = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"backpocket_path_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        validator = new PathValidator();
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    /// <summary>
    /// Tests that Normalize expands the home directory shortcut.
    /// </summary>
    [Test]
    public void Normalize_TildePrefix_ExpandsHomeDirectory()
    {
        // Arrange
        var path = "~/test";
        var expectedPrefix = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var normalized = validator.Normalize(path);

        // Assert
        Assert.That(normalized, Does.StartWith(expectedPrefix));
        Assert.That(normalized, Does.EndWith("test"));
    }

    /// <summary>
    /// Tests that Normalize handles tilde alone.
    /// </summary>
    [Test]
    public void Normalize_TildeAlone_ReturnsHomeDirectory()
    {
        // Arrange
        var path = "~";
        var expected = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var normalized = validator.Normalize(path);

        // Assert
        Assert.That(normalized, Is.EqualTo(expected));
    }

    /// <summary>
    /// Tests that Validate returns valid for an existing directory.
    /// </summary>
    [Test]
    public void Validate_ExistingDirectory_ReturnsValid()
    {
        // Arrange & Act
        var result = validator.Validate(tempDirectory, PathPurpose.LibraryRoot);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.NormalizedPath, Is.Not.Null);
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }

    /// <summary>
    /// Tests that Validate returns a warning for non-existent directories.
    /// </summary>
    [Test]
    public void Validate_NonExistentDirectory_ReturnsWarning()
    {
        // Arrange
        var nonExistent = Path.Combine(tempDirectory, "nonexistent");

        // Act
        var result = validator.Validate(nonExistent, PathPurpose.DiskImages);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Does.Contain(PathValidationWarning.DirectoryDoesNotExist));
        });
    }

    /// <summary>
    /// Tests that Validate returns invalid for empty paths.
    /// </summary>
    [Test]
    public void Validate_EmptyPath_ReturnsInvalid()
    {
        // Arrange & Act
        var result = validator.Validate(string.Empty, PathPurpose.LibraryRoot);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        });
    }

    /// <summary>
    /// Tests that Validate normalizes relative paths to absolute paths.
    /// </summary>
    [Test]
    public void Validate_RelativePath_NormalizesToAbsolute()
    {
        // Arrange & Act
        var result = validator.Validate("relative/path", PathPurpose.LibraryRoot);

        // Assert - relative paths are now normalized to absolute paths
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.NormalizedPath, Is.Not.Null);
            Assert.That(Path.IsPathRooted(result.NormalizedPath), Is.True);
            Assert.That(result.Warnings, Does.Contain(PathValidationWarning.DirectoryDoesNotExist));
        });
    }

    /// <summary>
    /// Tests that EnsureDirectoryExistsAsync creates a directory.
    /// </summary>
    [Test]
    public async Task EnsureDirectoryExistsAsync_CreatesDirectory()
    {
        // Arrange
        var newDir = Path.Combine(tempDirectory, "newdir");

        // Act
        var result = await validator.EnsureDirectoryExistsAsync(newDir);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(Directory.Exists(newDir), Is.True);
        });
    }

    /// <summary>
    /// Tests that EnsureDirectoryExistsAsync returns true for existing directories.
    /// </summary>
    [Test]
    public async Task EnsureDirectoryExistsAsync_ExistingDirectory_ReturnsTrue()
    {
        // Arrange & Act
        var result = await validator.EnsureDirectoryExistsAsync(tempDirectory);

        // Assert
        Assert.That(result, Is.True);
    }
}