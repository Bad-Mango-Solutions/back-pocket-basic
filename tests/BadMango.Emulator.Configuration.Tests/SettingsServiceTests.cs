// <copyright file="SettingsServiceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Tests;

using System.Text.Json;

using BadMango.Emulator.Configuration.Models;
using BadMango.Emulator.Configuration.Services;

/// <summary>
/// Tests for <see cref="SettingsService"/>.
/// </summary>
[TestFixture]
public class SettingsServiceTests
{
    private string tempDirectory = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"backpocket_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
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
    /// Tests that the service initializes with default settings when no file exists.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadAsync_NoFile_ReturnsDefaultSettings()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);

        // Act
        var settings = await service.LoadAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Version, Is.EqualTo(1));
            Assert.That(settings.General.Theme, Is.EqualTo("Dark"));
            Assert.That(settings.General.LoadLastProfile, Is.True);
        });
    }

    /// <summary>
    /// Tests that settings can be saved and loaded.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SaveAndLoad_RoundTrip_PreservesSettings()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);
        var settings = new AppSettings
        {
            General = new()
            {
                Theme = "Light",
                Language = "de-DE",
                LoadLastProfile = false,
            },
            Display = new()
            {
                ScaleFactor = 3,
                ScanlineEffect = true,
            },
        };

        // Act
        await service.SaveAsync(settings);
        var service2 = new SettingsService(settingsDirectory: tempDirectory);
        var loaded = await service2.LoadAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(loaded.General.Theme, Is.EqualTo("Light"));
            Assert.That(loaded.General.Language, Is.EqualTo("de-DE"));
            Assert.That(loaded.General.LoadLastProfile, Is.False);
            Assert.That(loaded.Display.ScaleFactor, Is.EqualTo(3));
            Assert.That(loaded.Display.ScanlineEffect, Is.True);
        });
    }

    /// <summary>
    /// Tests that Current property returns the loaded settings.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task Current_AfterLoad_ReturnsSameSettings()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);

        // Act
        var loaded = await service.LoadAsync();
        var current = service.Current;

        // Assert
        Assert.That(current, Is.EqualTo(loaded));
    }

    /// <summary>
    /// Tests that ResetToDefaultsAsync returns default settings.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ResetToDefaultsAsync_ReturnsDefaultSettings()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);
        await service.SaveAsync(new()
        {
            General = new() { Theme = "Light" },
        });

        // Act
        var defaults = await service.ResetToDefaultsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(defaults.General.Theme, Is.EqualTo("Dark"));
            Assert.That(defaults.General.LoadLastProfile, Is.True);
        });
    }

    /// <summary>
    /// Tests that ExportAsync creates a valid JSON file.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ExportAsync_CreatesValidJsonFile()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);
        await service.LoadAsync();
        var exportPath = Path.Combine(tempDirectory, "exported.json");

        // Act
        await service.ExportAsync(exportPath);

        // Assert
        Assert.That(File.Exists(exportPath), Is.True);
        var json = await File.ReadAllTextAsync(exportPath);
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.That(parsed.TryGetProperty("version", out _), Is.True);
    }

    /// <summary>
    /// Tests that ImportAsync loads settings from a file.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task ImportAsync_LoadsSettingsFromFile()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);
        var importPath = Path.Combine(tempDirectory, "import.json");
        var settingsJson = """
        {
            "version": 1,
            "general": {
                "theme": "Light",
                "language": "fr-FR"
            }
        }
        """;
        await File.WriteAllTextAsync(importPath, settingsJson);

        // Act
        var imported = await service.ImportAsync(importPath);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(imported.General.Theme, Is.EqualTo("Light"));
            Assert.That(imported.General.Language, Is.EqualTo("fr-FR"));
        });
    }

    /// <summary>
    /// Tests that SettingsChanged event is raised on load when file exists.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task LoadAsync_WithExistingFile_RaisesSettingsChangedEvent()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);

        // Create a settings file first
        await service.SaveAsync(new());

        // Create a new service and subscribe to events
        var service2 = new SettingsService(settingsDirectory: tempDirectory);
        bool eventRaised = false;
        service2.SettingsChanged += (_, _) => eventRaised = true;

        // Act
        await service2.LoadAsync();

        // Assert
        Assert.That(eventRaised, Is.True);
    }

    /// <summary>
    /// Tests that SettingsChanged event is raised on save.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SaveAsync_RaisesSettingsChangedEvent()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);
        bool eventRaised = false;
        service.SettingsChanged += (_, _) => eventRaised = true;

        // Act
        await service.SaveAsync(new());

        // Assert
        Assert.That(eventRaised, Is.True);
    }

    /// <summary>
    /// Tests that GetValue returns the correct value.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task GetValue_ReturnsCorrectValue()
    {
        // Arrange
        var service = new SettingsService(settingsDirectory: tempDirectory);
        await service.LoadAsync();

        // Act
        var theme = service.GetValue<string>("General.Theme");

        // Assert
        Assert.That(theme, Is.EqualTo("Dark"));
    }
}