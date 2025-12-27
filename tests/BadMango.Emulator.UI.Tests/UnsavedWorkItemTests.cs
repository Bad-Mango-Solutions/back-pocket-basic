// <copyright file="UnsavedWorkItemTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.Abstractions;

/// <summary>
/// Tests for <see cref="UnsavedWorkItem"/>.
/// </summary>
[TestFixture]
public class UnsavedWorkItemTests
{
    /// <summary>
    /// Tests that UnsavedWorkItem can be created with valid values.
    /// </summary>
    [Test]
    public void Constructor_WithValidValues_CreatesInstance()
    {
        // Act
        var item = new UnsavedWorkItem
        {
            Name = "Test File",
            Description = "Unsaved changes",
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(item.Name, Is.EqualTo("Test File"));
            Assert.That(item.Description, Is.EqualTo("Unsaved changes"));
        });
    }

    /// <summary>
    /// Tests that Name throws ArgumentNullException when null.
    /// </summary>
    [Test]
    public void Name_WhenNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnsavedWorkItem
        {
            Name = null!,
            Description = "Valid description",
        });
    }

    /// <summary>
    /// Tests that Name throws ArgumentException when empty.
    /// </summary>
    [Test]
    public void Name_WhenEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new UnsavedWorkItem
        {
            Name = string.Empty,
            Description = "Valid description",
        });
    }

    /// <summary>
    /// Tests that Name throws ArgumentException when whitespace.
    /// </summary>
    [Test]
    public void Name_WhenWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new UnsavedWorkItem
        {
            Name = "   ",
            Description = "Valid description",
        });
    }

    /// <summary>
    /// Tests that Description throws ArgumentNullException when null.
    /// </summary>
    [Test]
    public void Description_WhenNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnsavedWorkItem
        {
            Name = "Valid name",
            Description = null!,
        });
    }

    /// <summary>
    /// Tests that Description throws ArgumentException when empty.
    /// </summary>
    [Test]
    public void Description_WhenEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new UnsavedWorkItem
        {
            Name = "Valid name",
            Description = string.Empty,
        });
    }

    /// <summary>
    /// Tests that Description throws ArgumentException when whitespace.
    /// </summary>
    [Test]
    public void Description_WhenWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new UnsavedWorkItem
        {
            Name = "Valid name",
            Description = "   ",
        });
    }

    /// <summary>
    /// Tests that optional properties can be set.
    /// </summary>
    [Test]
    public void OptionalProperties_CanBeSet()
    {
        // Act
        var item = new UnsavedWorkItem
        {
            Name = "Test File",
            Description = "Unsaved changes",
            ComponentType = PopOutComponent.AssemblyEditor,
            WindowId = "window-123",
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(item.ComponentType, Is.EqualTo(PopOutComponent.AssemblyEditor));
            Assert.That(item.WindowId, Is.EqualTo("window-123"));
        });
    }

    /// <summary>
    /// Tests that optional properties default to null.
    /// </summary>
    [Test]
    public void OptionalProperties_DefaultToNull()
    {
        // Act
        var item = new UnsavedWorkItem
        {
            Name = "Test File",
            Description = "Unsaved changes",
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(item.ComponentType, Is.Null);
            Assert.That(item.WindowId, Is.Null);
        });
    }
}