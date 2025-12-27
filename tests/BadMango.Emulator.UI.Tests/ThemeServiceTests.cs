// <copyright file="ThemeServiceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using BadMango.Emulator.UI.Services;

/// <summary>
/// Tests for <see cref="ThemeService"/>.
/// </summary>
[TestFixture]
public class ThemeServiceTests
{
    /// <summary>
    /// Tests that the service initializes with dark theme.
    /// </summary>
    [Test]
    public void Constructor_InitializesWithDarkTheme()
    {
        // Arrange & Act
        var service = new ThemeService();

        // Assert
        Assert.That(service.IsDarkTheme, Is.True);
    }

    /// <summary>
    /// Tests that SetTheme changes the theme.
    /// </summary>
    [Test]
    public void SetTheme_ChangesTheme()
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.SetTheme(false);

        // Assert
        Assert.That(service.IsDarkTheme, Is.False);
    }

    /// <summary>
    /// Tests that SetTheme does not raise event when theme is the same.
    /// </summary>
    [Test]
    public void SetTheme_SameValue_DoesNotRaiseEvent()
    {
        // Arrange
        var service = new ThemeService();
        bool eventRaised = false;
        service.ThemeChanged += (_, _) => eventRaised = true;

        // Act - set to the same value (dark)
        service.SetTheme(true);

        // Assert
        Assert.That(eventRaised, Is.False);
    }

    /// <summary>
    /// Tests that SetTheme raises ThemeChanged event.
    /// </summary>
    [Test]
    public void SetTheme_RaisesThemeChangedEvent()
    {
        // Arrange
        var service = new ThemeService();
        bool eventRaised = false;
        bool? eventValue = null;
        service.ThemeChanged += (_, isDark) =>
        {
            eventRaised = true;
            eventValue = isDark;
        };

        // Act
        service.SetTheme(false);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(eventRaised, Is.True);
            Assert.That(eventValue, Is.False);
        });
    }
}