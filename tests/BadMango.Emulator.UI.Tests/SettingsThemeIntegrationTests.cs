// <copyright file="SettingsThemeIntegrationTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Styling;
using Avalonia.Threading;

using BadMango.Emulator.Configuration.Models;
using BadMango.Emulator.Configuration.Services;
using BadMango.Emulator.Infrastructure.Events;
using BadMango.Emulator.UI.Abstractions.Events;
using BadMango.Emulator.UI.Services;
using BadMango.Emulator.UI.ViewModels;
using BadMango.Emulator.UI.ViewModels.Settings;
using BadMango.Emulator.UI.Views;

/// <summary>
/// Integration tests for settings and theme interaction.
/// Validates the full flow from settings change to UI theme update.
/// </summary>
[TestFixture]
public class SettingsThemeIntegrationTests
{
    private string tempDirectory = null!;

    /// <summary>
    /// Sets up the test environment.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"backpocket_theme_tests_{Guid.NewGuid():N}");
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
    /// Integration test: User changes theme from Dark to Light via Settings panel.
    /// Validates the complete flow: Settings panel -> Apply -> SettingsChangedEvent via EventAggregator -> ThemeService -> ThemeChangedEvent -> MainWindowViewModel -> UI.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [AvaloniaTest]
    public async Task SettingsThemeChange_DarkToLight_UpdatesUITheme()
    {
        // Arrange - Setup with dark theme (default)
        var settingsService = new SettingsService(settingsDirectory: tempDirectory);
        await settingsService.LoadAsync();

        // Confirm initial state is dark
        Assert.That(settingsService.Current.General.Theme, Is.EqualTo("Dark"));

        // Create event aggregator for pub/sub messaging
        var eventAggregator = new EventAggregator();

        // Create bridge to connect SettingsService events to EventAggregator
        using var settingsEventBridge = new SettingsEventBridge(settingsService, eventAggregator);

        // Create ThemeService with event aggregator and settings service
        // ThemeService subscribes to SettingsChangedEvent and publishes ThemeChangedEvent
        using var themeService = new ThemeService(eventAggregator: eventAggregator, settingsService: settingsService);
        Assert.That(themeService.IsDarkTheme, Is.True);

        // Track event firing - verify full event aggregator flow
        bool settingsChangedEventFired = false;
        bool settingsChangedViaAggregator = false;
        bool themeChangedEventFired = false;
        bool themeChangedViaAggregator = false;
        bool? newThemeIsDark = null;

        settingsService.SettingsChanged += (_, args) =>
        {
            settingsChangedEventFired = true;
        };

        // Subscribe to SettingsChangedEvent via EventAggregator to verify pub/sub flow
        using var settingsAggregatorSubscription = eventAggregator.Subscribe<Configuration.Events.SettingsChangedEvent>(evt =>
        {
            settingsChangedViaAggregator = true;
        });

        themeService.ThemeChanged += (_, isDark) =>
        {
            themeChangedEventFired = true;
            newThemeIsDark = isDark;
        };

        // Subscribe to ThemeChangedEvent via EventAggregator to verify pub/sub flow
        using var themeAggregatorSubscription = eventAggregator.Subscribe<ThemeChangedEvent>(evt =>
        {
            themeChangedViaAggregator = true;
        });

        // Create main window with services - uses event aggregator for loose coupling
        var navigationService = new NavigationService();
        using var mainViewModel = new MainWindowViewModel(themeService, navigationService, settingsService, eventAggregator);

        _ = new MainWindow
        {
            DataContext = mainViewModel,
        };

        // Act - Step 1: Navigate to Settings panel
        mainViewModel.NavigateCommand.Execute("Settings");
        Assert.That(mainViewModel.CurrentView, Is.InstanceOf<SettingsWindowViewModel>());

        var settingsViewModel = (SettingsWindowViewModel)mainViewModel.CurrentView!;

        // Step 2: Select General Settings page (should be selected by default)
        var generalSettingsPage = settingsViewModel.SettingsPages.FirstOrDefault(p => p.DisplayName == "General");
        Assert.That(generalSettingsPage, Is.Not.Null);
        Assert.That(generalSettingsPage, Is.InstanceOf<GeneralSettingsViewModel>());

        var generalViewModel = (GeneralSettingsViewModel)generalSettingsPage!;

        // Step 3: Load the settings into the view model
        await generalViewModel.LoadAsync();

        // Verify current theme is "Dark"
        Assert.That(generalViewModel.Theme, Is.EqualTo("Dark"));

        // Step 4: Change theme from "Dark" to "Light"
        generalViewModel.Theme = "Light";

        // Verify HasChanges flag is set
        Assert.That(generalViewModel.HasChanges, Is.True);

        // Step 5: Press Apply (execute the apply command)
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await settingsViewModel.ApplyCommand.ExecuteAsync(null);
        });

        // Wait for the dispatcher to process the posted theme change
        await Dispatcher.UIThread.InvokeAsync(() => { });

        // Assert - Verify the complete event chain
        Assert.Multiple(() =>
        {
            // Settings service direct event was fired
            Assert.That(settingsChangedEventFired, Is.True, "SettingsChanged direct event should fire");

            // SettingsChangedEvent was published via EventAggregator
            Assert.That(settingsChangedViaAggregator, Is.True, "SettingsChangedEvent should be published via EventAggregator");

            // Theme service direct event was fired
            Assert.That(themeChangedEventFired, Is.True, "ThemeChanged direct event should fire");

            // ThemeChangedEvent was published via EventAggregator
            Assert.That(themeChangedViaAggregator, Is.True, "ThemeChangedEvent should be published via EventAggregator");

            // New theme is Light (isDark = false)
            Assert.That(newThemeIsDark, Is.False, "Theme should be Light (isDark = false)");

            // Theme service state is now Light
            Assert.That(themeService.IsDarkTheme, Is.False, "ThemeService.IsDarkTheme should be false");

            // MainViewModel's IsDarkTheme property is synchronized via ThemeChangedEvent subscription
            Assert.That(mainViewModel.IsDarkTheme, Is.False, "MainWindowViewModel.IsDarkTheme should be false");

            // Verify the persisted settings have Light theme
            Assert.That(settingsService.Current.General.Theme, Is.EqualTo("Light"));
        });

        // Step 6: Validate UI theme is now light by checking the Application's RequestedThemeVariant
        if (Application.Current is not null)
        {
            Assert.That(
                Application.Current.RequestedThemeVariant,
                Is.EqualTo(ThemeVariant.Light),
                "Application should be in Light theme");
        }
    }

    /// <summary>
    /// Integration test: Validates round-trip theme change from Light back to Dark.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [AvaloniaTest]
    public async Task SettingsThemeChange_LightToDark_UpdatesUITheme()
    {
        // Arrange - Start with Light theme
        var settingsService = new SettingsService(settingsDirectory: tempDirectory);
        var initialSettings = new AppSettings
        {
            General = new GeneralSettings { Theme = "Light" },
        };
        await settingsService.SaveAsync(initialSettings);
        await settingsService.LoadAsync();

        Assert.That(settingsService.Current.General.Theme, Is.EqualTo("Light"));

        // Create event aggregator and bridge
        var eventAggregator = new EventAggregator();
        using var settingsEventBridge = new SettingsEventBridge(settingsService, eventAggregator);

        // Create ThemeService with aggregator and settings service for pub/sub flow
        using var themeService = new ThemeService(eventAggregator: eventAggregator, settingsService: settingsService);

        // Set theme to Light initially via Dispatcher
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            themeService.SetTheme(false);
        });

        Assert.That(themeService.IsDarkTheme, Is.False);

        bool themeChangedEventFired = false;
        bool themeChangedViaAggregator = false;

        themeService.ThemeChanged += (_, _) =>
        {
            themeChangedEventFired = true;
        };

        // Subscribe to ThemeChangedEvent via EventAggregator
        using var themeAggregatorSubscription = eventAggregator.Subscribe<ThemeChangedEvent>(evt =>
        {
            themeChangedViaAggregator = true;
        });

        var navigationService = new NavigationService();
        using var mainViewModel = new MainWindowViewModel(themeService, navigationService, settingsService, eventAggregator);

        // Sync the ViewModel's IsDarkTheme with the initial theme state
        mainViewModel.IsDarkTheme = false;

        _ = new MainWindow
        {
            DataContext = mainViewModel,
        };

        // Act - Navigate to Settings and change theme to Dark
        mainViewModel.NavigateCommand.Execute("Settings");
        var settingsViewModel = (SettingsWindowViewModel)mainViewModel.CurrentView!;
        var generalViewModel = (GeneralSettingsViewModel)settingsViewModel.SettingsPages.First(p => p.DisplayName == "General");

        await generalViewModel.LoadAsync();
        Assert.That(generalViewModel.Theme, Is.EqualTo("Light"));

        generalViewModel.Theme = "Dark";

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await settingsViewModel.ApplyCommand.ExecuteAsync(null);
        });

        // Wait for the dispatcher to process the posted theme change
        await Dispatcher.UIThread.InvokeAsync(() => { });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(themeChangedEventFired, Is.True, "ThemeChanged direct event should fire");
            Assert.That(themeChangedViaAggregator, Is.True, "ThemeChangedEvent should be published via EventAggregator");
            Assert.That(themeService.IsDarkTheme, Is.True);
            Assert.That(mainViewModel.IsDarkTheme, Is.True);
            Assert.That(settingsService.Current.General.Theme, Is.EqualTo("Dark"));
        });

        if (Application.Current is not null)
        {
            Assert.That(
                Application.Current.RequestedThemeVariant,
                Is.EqualTo(ThemeVariant.Dark));
        }
    }

    /// <summary>
    /// Integration test: Validates that settings are persisted correctly after theme change.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [AvaloniaTest]
    public async Task SettingsThemeChange_PersistsToFile()
    {
        // Arrange
        var settingsService = new SettingsService(settingsDirectory: tempDirectory);
        await settingsService.LoadAsync();

        var eventAggregator = new EventAggregator();
        using var settingsEventBridge = new SettingsEventBridge(settingsService, eventAggregator);
        using var themeService = new ThemeService(eventAggregator: eventAggregator, settingsService: settingsService);
        var navigationService = new NavigationService();
        using var mainViewModel = new MainWindowViewModel(themeService, navigationService, settingsService, eventAggregator);

        _ = new MainWindow
        {
            DataContext = mainViewModel,
        };

        // Act - Change theme to Light and apply
        mainViewModel.NavigateCommand.Execute("Settings");
        var settingsViewModel = (SettingsWindowViewModel)mainViewModel.CurrentView!;
        var generalViewModel = (GeneralSettingsViewModel)settingsViewModel.SettingsPages.First(p => p.DisplayName == "General");

        await generalViewModel.LoadAsync();
        generalViewModel.Theme = "Light";

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await settingsViewModel.ApplyCommand.ExecuteAsync(null);
        });

        // Create a new service to verify persistence
        var newSettingsService = new SettingsService(settingsDirectory: tempDirectory);
        await newSettingsService.LoadAsync();

        // Assert
        Assert.That(newSettingsService.Current.General.Theme, Is.EqualTo("Light"));
    }

    /// <summary>
    /// Integration test: Validates that no theme change event fires when setting same value.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [AvaloniaTest]
    public async Task SettingsThemeChange_SameValue_NoThemeEventFired()
    {
        // Arrange
        var settingsService = new SettingsService(settingsDirectory: tempDirectory);
        await settingsService.LoadAsync();

        var eventAggregator = new EventAggregator();
        using var settingsEventBridge = new SettingsEventBridge(settingsService, eventAggregator);
        using var themeService = new ThemeService(eventAggregator: eventAggregator, settingsService: settingsService);
        bool themeEventFired = false;
        bool themeEventViaAggregator = false;

        themeService.ThemeChanged += (_, _) => themeEventFired = true;
        using var themeAggregatorSubscription = eventAggregator.Subscribe<ThemeChangedEvent>(_ => themeEventViaAggregator = true);

        var navigationService = new NavigationService();
        using var mainViewModel = new MainWindowViewModel(themeService, navigationService, settingsService, eventAggregator);

        _ = new MainWindow
        {
            DataContext = mainViewModel,
        };

        // Act - Navigate to Settings but keep theme as Dark (same as default)
        mainViewModel.NavigateCommand.Execute("Settings");
        var settingsViewModel = (SettingsWindowViewModel)mainViewModel.CurrentView!;
        var generalViewModel = (GeneralSettingsViewModel)settingsViewModel.SettingsPages.First(p => p.DisplayName == "General");

        await generalViewModel.LoadAsync();

        // Confirm theme is already Dark
        Assert.That(generalViewModel.Theme, Is.EqualTo("Dark"));

        // Don't change the theme, just apply
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await settingsViewModel.ApplyCommand.ExecuteAsync(null);
        });

        // Assert - Theme service should not fire event since theme didn't change value
        Assert.Multiple(() =>
        {
            Assert.That(themeEventFired, Is.False, "ThemeChanged direct event should not fire for same value");
            Assert.That(themeEventViaAggregator, Is.False, "ThemeChangedEvent via aggregator should not fire for same value");
            Assert.That(themeService.IsDarkTheme, Is.True);
        });
    }
}