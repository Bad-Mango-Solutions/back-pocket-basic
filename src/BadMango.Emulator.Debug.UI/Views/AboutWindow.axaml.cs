// <copyright file="AboutWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

/// <summary>
/// About window displaying version and copyright information for the debug console.
/// </summary>
/// <remarks>
/// <para>
/// This window serves as a proof-of-concept for the debug popup window infrastructure,
/// demonstrating that Avalonia windows can be launched from the console REPL.
/// </para>
/// <para>
/// The window is designed to be modal and can be closed with the Close button,
/// by pressing Escape, or by clicking the window's close button.
/// </para>
/// </remarks>
public partial class AboutWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutWindow"/> class.
    /// </summary>
    public AboutWindow()
    {
        this.InitializeComponent();
        this.SetupVersionInfo();
        this.SetupGitHubLink();

        // Allow closing with Escape key
        this.KeyDown += this.OnKeyDown;
    }

    private static void OpenUrl(string url)
    {
        try
        {
            // Cross-platform URL opening
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch
        {
            // Silently ignore if we can't open the URL
        }
    }

    private void SetupVersionInfo()
    {
        var assembly = typeof(AboutWindow).Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? version;

        // Extract the main version part (before any + or - for commit info)
        var displayVersion = informationalVersion.Split(['+', '-'])[0];

        this.VersionText.Text = $"Version {displayVersion}";
    }

    private void SetupGitHubLink()
    {
        this.GitHubLink.PointerPressed += this.OnGitHubLinkClick;
    }

    private void OnGitHubLinkClick(object? sender, PointerPressedEventArgs e)
    {
        var url = "https://github.com/Bad-Mango-Solutions/back-pocket-basic";
        OpenUrl(url);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Close();
            e.Handled = true;
        }
    }
}