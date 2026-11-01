// <copyright file="AboutViewModelTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.ViewModels.Settings;

/// <summary>
/// Tests for <see cref="AboutViewModel"/>.
/// </summary>
[TestFixture]
public class AboutViewModelTests
{
    /// <summary>
    /// Tests that the ViewModel can be constructed without throwing an exception.
    /// This verifies the fix for AmbiguousMatchException when reading assembly metadata.
    /// </summary>
    [Test]
    public void Constructor_DoesNotThrowAmbiguousMatchException()
    {
        // Arrange & Act - Construction should not throw
        var viewModel = new AboutViewModel();

        // Assert
        Assert.That(viewModel, Is.Not.Null);
    }

    /// <summary>
    /// Tests that the ViewModel initializes with valid property values.
    /// </summary>
    [Test]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var viewModel = new AboutViewModel();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ApplicationName, Is.EqualTo("BackPocket Emulator"));
            Assert.That(viewModel.Description, Is.EqualTo("An Apple II family emulator with modern enhancements"));
            Assert.That(viewModel.Version, Is.Not.Null.And.Not.Empty);
            Assert.That(viewModel.BuildDate, Is.Not.Null.And.Not.Empty);
            Assert.That(viewModel.DotNetVersion, Is.Not.Null.And.Not.Empty);
            Assert.That(viewModel.OperatingSystem, Is.Not.Null.And.Not.Empty);
            Assert.That(viewModel.License, Is.EqualTo("MIT License"));
            Assert.That(viewModel.GitHubUrl, Is.EqualTo("https://github.com/Bad-Mango-Solutions/back-pocket-emulator"));
        });
    }

    /// <summary>
    /// Tests that ISettingsPage properties are correctly implemented.
    /// </summary>
    [Test]
    public void SettingsPageProperties_AreCorrectlyImplemented()
    {
        // Arrange & Act
        var viewModel = new AboutViewModel();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.DisplayName, Is.EqualTo("About"));
            Assert.That(viewModel.IconKey, Is.EqualTo("InfoIcon"));
            Assert.That(viewModel.ParentCategory, Is.Null);
            Assert.That(viewModel.SortOrder, Is.EqualTo(100));
            Assert.That(viewModel.HasChanges, Is.False);
        });
    }

    /// <summary>
    /// Tests that the BuildDate property returns a valid date format.
    /// </summary>
    [Test]
    public void BuildDate_ReturnsValidDateFormat()
    {
        // Arrange & Act
        var viewModel = new AboutViewModel();

        // Assert - BuildDate should be in yyyy-MM-dd format or similar
        Assert.That(viewModel.BuildDate, Does.Match(@"\d{4}-\d{2}-\d{2}"));
    }
}