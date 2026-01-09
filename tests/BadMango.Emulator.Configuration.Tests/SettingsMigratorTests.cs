// <copyright file="SettingsMigratorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Tests;

using System.Text.Json;

using BadMango.Emulator.Configuration.Services;

/// <summary>
/// Tests for <see cref="SettingsMigrator"/>.
/// </summary>
[TestFixture]
public class SettingsMigratorTests
{
    private SettingsMigrator migrator = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        migrator = new();
    }

    /// <summary>
    /// Tests that CurrentVersion returns 1.
    /// </summary>
    [Test]
    public void CurrentVersion_ReturnsOne()
    {
        // Assert
        Assert.That(migrator.CurrentVersion, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that NeedsMigration returns false for current version.
    /// </summary>
    [Test]
    public void NeedsMigration_CurrentVersion_ReturnsFalse()
    {
        // Arrange
        var currentVersion = migrator.CurrentVersion;

        // Act
        var result = migrator.NeedsMigration(currentVersion);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Tests that NeedsMigration returns true for older versions.
    /// </summary>
    [Test]
    public void NeedsMigration_OlderVersion_ReturnsTrue()
    {
        // Arrange
        var olderVersion = migrator.CurrentVersion - 1;

        // Act
        var result = migrator.NeedsMigration(olderVersion);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Tests that NeedsMigration returns false for future versions.
    /// </summary>
    [Test]
    public void NeedsMigration_FutureVersion_ReturnsFalse()
    {
        // Arrange
        var futureVersion = migrator.CurrentVersion + 1;

        // Act
        var result = migrator.NeedsMigration(futureVersion);

        // Assert
        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Tests that Migrate returns valid AppSettings for valid JSON.
    /// </summary>
    [Test]
    public void Migrate_ValidJson_ReturnsAppSettings()
    {
        // Arrange
        var json = """
        {
            "version": 1,
            "general": {
                "theme": "Light",
                "language": "en-US"
            }
        }
        """;
        using var document = JsonDocument.Parse(json);

        // Act
        var result = migrator.Migrate(document.RootElement, 0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.General.Theme, Is.EqualTo("Light"));
            Assert.That(result.General.Language, Is.EqualTo("en-US"));
        });
    }

    /// <summary>
    /// Tests that Migrate returns default AppSettings for empty JSON.
    /// </summary>
    [Test]
    public void Migrate_EmptyJson_ReturnsDefaultAppSettings()
    {
        // Arrange
        var json = "{}";
        using var document = JsonDocument.Parse(json);

        // Act
        var result = migrator.Migrate(document.RootElement, 0);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Version, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that Migrate preserves settings during migration.
    /// </summary>
    [Test]
    public void Migrate_WithSettings_PreservesValues()
    {
        // Arrange
        var json = """
        {
            "version": 1,
            "general": {
                "loadLastProfile": false,
                "startPaused": true,
                "restoreWindowLayout": false,
                "language": "de-DE",
                "theme": "Dark",
                "checkForUpdates": false,
                "enableTelemetry": true
            },
            "display": {
                "scaleFactor": 3,
                "scanlineEffect": true
            }
        }
        """;
        using var document = JsonDocument.Parse(json);

        // Act
        var result = migrator.Migrate(document.RootElement, 0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.General.LoadLastProfile, Is.False);
            Assert.That(result.General.StartPaused, Is.True);
            Assert.That(result.General.RestoreWindowLayout, Is.False);
            Assert.That(result.General.Language, Is.EqualTo("de-DE"));
            Assert.That(result.General.Theme, Is.EqualTo("Dark"));
            Assert.That(result.General.CheckForUpdates, Is.False);
            Assert.That(result.General.EnableTelemetry, Is.True);
            Assert.That(result.Display.ScaleFactor, Is.EqualTo(3));
            Assert.That(result.Display.ScanlineEffect, Is.True);
        });
    }

    /// <summary>
    /// Tests that Migrate handles partial settings correctly.
    /// </summary>
    [Test]
    public void Migrate_PartialSettings_UsesDefaults()
    {
        // Arrange - only provide general settings, not display
        var json = """
        {
            "version": 1,
            "general": {
                "theme": "Light"
            }
        }
        """;
        using var document = JsonDocument.Parse(json);

        // Act
        var result = migrator.Migrate(document.RootElement, 0);

        // Assert - display should use defaults
        Assert.Multiple(() =>
        {
            Assert.That(result.General.Theme, Is.EqualTo("Light"));
            Assert.That(result.Display, Is.Not.Null);
            Assert.That(result.Display.ScaleFactor, Is.EqualTo(2)); // Default value
        });
    }
}