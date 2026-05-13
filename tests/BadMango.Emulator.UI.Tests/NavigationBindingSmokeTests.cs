// <copyright file="NavigationBindingSmokeTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Tests;

using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using BadMango.Emulator.Configuration.Services;
using BadMango.Emulator.UI.Services;
using BadMango.Emulator.UI.ViewModels;
using BadMango.Emulator.UI.ViewModels.Settings;
using BadMango.Emulator.UI.Views;
using BadMango.Emulator.UI.Views.Settings;

/// <summary>
/// Smoke tests verifying that navigation-item button bindings using
/// <c>{Binding ((vm:Type)DataContext).SomeCommand, RelativeSource={RelativeSource AncestorType=...}}</c>
/// resolve correctly under Avalonia 12 compiled bindings.
/// </summary>
/// <remarks>
/// Avalonia 12 made <c>BindingPlugins</c> internal and made compiled bindings strict by default.
/// Bindings that walked through a <see cref="RelativeSource"/> ancestor's <c>DataContext</c>
/// (typed as <see cref="object"/>) no longer compile without an explicit type cast in the
/// binding path. These tests guard against regression of that fix by constructing each
/// navigation-bearing view, realizing its templates, and asserting that the resulting
/// item buttons resolve their <see cref="ICommand"/> to the parent view-model's command.
/// </remarks>
[TestFixture]
public class NavigationBindingSmokeTests
{
    private string tempDirectory = null!;

    /// <summary>
    /// Creates a temporary settings directory for tests that need a real
    /// <see cref="SettingsService"/>.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"backpocket_navbinding_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
    }

    /// <summary>
    /// Cleans up the temporary settings directory.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the navigation buttons inside <see cref="MainWindow"/>'s
    /// items control resolve their <c>Command</c> to
    /// <c>MainWindowViewModel.NavigateCommand</c> via the
    /// <c>((vm:MainWindowViewModel)DataContext).NavigateCommand</c> compiled binding.
    /// </summary>
    [AvaloniaTest]
    public void MainWindow_NavigationItemButtons_ResolveNavigateCommandFromAncestorWindow()
    {
        // Arrange
        var viewModel = new MainWindowViewModel(new ThemeService(), new NavigationService());
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        // Act - show the window so templates are realized in the headless surface.
        window.Show();
        try
        {
            RunFrame();

            var navigationButtons = FindNavigationItemButtons(
                window,
                viewModel.NavigationItems.Select(i => i.Name));

            // Assert
            Assert.That(
                navigationButtons,
                Is.Not.Empty,
                "Expected the navigation ItemsControl to realize a Button per NavigationItem.");

            Assert.Multiple(() =>
            {
                foreach (var button in navigationButtons)
                {
                    Assert.That(
                        button.Command,
                        Is.SameAs(viewModel.NavigateCommand),
                        $"Navigation button for '{button.CommandParameter}' did not bind to MainWindowViewModel.NavigateCommand.");
                }
            });
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Verifies that the page buttons inside <see cref="SettingsWindow"/>'s
    /// items control resolve their <c>Command</c> to
    /// <c>SettingsWindowViewModel.NavigateToPageCommand</c> via the
    /// <c>((vm:SettingsWindowViewModel)DataContext).NavigateToPageCommand</c>
    /// compiled binding using <c>AncestorType=Window</c>.
    /// </summary>
    [AvaloniaTest]
    public void SettingsWindow_PageButtons_ResolveNavigateToPageCommandFromAncestorWindow()
    {
        // Arrange
        var settingsService = new SettingsService(settingsDirectory: tempDirectory);
        var viewModel = new SettingsWindowViewModel(settingsService);
        var window = new SettingsWindow
        {
            DataContext = viewModel,
        };

        // Act
        window.Show();
        try
        {
            RunFrame();

            var pageButtons = FindNavigationItemButtons(
                window,
                viewModel.SettingsPages.Select(p => p.DisplayName));

            // Assert
            Assert.That(
                pageButtons,
                Is.Not.Empty,
                "Expected the settings ItemsControl to realize a Button per settings page.");

            Assert.Multiple(() =>
            {
                foreach (var button in pageButtons)
                {
                    Assert.That(
                        button.Command,
                        Is.SameAs(viewModel.NavigateToPageCommand),
                        $"Settings page button for '{button.CommandParameter}' did not bind to SettingsWindowViewModel.NavigateToPageCommand.");
                }
            });
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Verifies that the page buttons inside <see cref="SettingsEmbeddedView"/>'s
    /// items control resolve their <c>Command</c> to
    /// <c>SettingsWindowViewModel.NavigateToPageCommand</c> via the
    /// <c>((vm:SettingsWindowViewModel)DataContext).NavigateToPageCommand</c>
    /// compiled binding using <c>AncestorType=UserControl</c>.
    /// </summary>
    [AvaloniaTest]
    public void SettingsEmbeddedView_PageButtons_ResolveNavigateToPageCommandFromAncestorUserControl()
    {
        // Arrange
        var settingsService = new SettingsService(settingsDirectory: tempDirectory);
        var viewModel = new SettingsWindowViewModel(settingsService);
        var view = new SettingsEmbeddedView
        {
            DataContext = viewModel,
        };

        // Host the UserControl in a Window so the headless surface lays it out.
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = view,
        };

        // Act
        window.Show();
        try
        {
            RunFrame();

            var pageButtons = FindNavigationItemButtons(
                view,
                viewModel.SettingsPages.Select(p => p.DisplayName));

            // Assert
            Assert.That(
                pageButtons,
                Is.Not.Empty,
                "Expected the settings ItemsControl to realize a Button per settings page.");

            Assert.Multiple(() =>
            {
                foreach (var button in pageButtons)
                {
                    Assert.That(
                        button.Command,
                        Is.SameAs(viewModel.NavigateToPageCommand),
                        $"Settings page button for '{button.CommandParameter}' did not bind to SettingsWindowViewModel.NavigateToPageCommand.");
                }
            });
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// Pumps a single headless render frame so item containers and templates
    /// are fully realized before the visual tree is queried.
    /// </summary>
    private static void RunFrame()
    {
        // Flush queued dispatcher jobs so item containers materialize and bindings apply.
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Finds buttons in the logical tree whose <c>CommandParameter</c> matches one
    /// of the expected navigation-item names.
    /// </summary>
    /// <param name="root">The root logical element to search beneath.</param>
    /// <param name="expectedParameters">The set of expected command-parameter values.</param>
    /// <returns>The matching realized <see cref="Button"/> instances.</returns>
    private static IReadOnlyList<Button> FindNavigationItemButtons(
        ILogical root,
        IEnumerable<string> expectedParameters)
    {
        var expected = new HashSet<string>(expectedParameters, StringComparer.Ordinal);
        return root.GetLogicalDescendants()
            .OfType<Button>()
            .Where(b => b.CommandParameter is string name && expected.Contains(name))
            .ToList();
    }
}