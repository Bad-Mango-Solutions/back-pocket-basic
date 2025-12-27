// <copyright file="AboutViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using System.Reflection;
using System.Runtime.InteropServices;

using BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// ViewModel for the About panel.
/// </summary>
public class AboutViewModel : ViewModelBase, ISettingsPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
    /// </summary>
    public AboutViewModel()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();

        Version = assemblyName.Version?.ToString() ?? "1.0.0";
        BuildDate = GetBuildDate(assembly);
        DotNetVersion = RuntimeInformation.FrameworkDescription;
        OperatingSystem = RuntimeInformation.OSDescription;
    }

    /// <inheritdoc/>
    public string DisplayName => "About";

    /// <inheritdoc/>
    public string IconKey => "InfoIcon";

    /// <inheritdoc/>
    public string? ParentCategory => null;

    /// <inheritdoc/>
    public int SortOrder => 100;

    /// <inheritdoc/>
    public bool HasChanges => false;

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName => "BackPocket Emulator";

    /// <summary>
    /// Gets the application description.
    /// </summary>
    public string Description => "An Apple II family emulator with modern enhancements";

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the build date.
    /// </summary>
    public string BuildDate { get; }

    /// <summary>
    /// Gets the .NET version.
    /// </summary>
    public string DotNetVersion { get; }

    /// <summary>
    /// Gets the operating system description.
    /// </summary>
    public string OperatingSystem { get; }

    /// <summary>
    /// Gets the license text.
    /// </summary>
    public string License => "MIT License";

    /// <summary>
    /// Gets the GitHub repository URL.
    /// </summary>
    public string GitHubUrl => "https://github.com/Bad-Mango-Solutions/back-pocket-basic";

    /// <inheritdoc/>
    public Task LoadAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task SaveAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ResetToDefaultsAsync() => Task.CompletedTask;

    private static string GetBuildDate(Assembly assembly)
    {
        // Try to get build date from assembly metadata
        var buildDateAttr = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
        if (buildDateAttr?.Key == "BuildDate" && buildDateAttr.Value is not null)
        {
            return buildDateAttr.Value;
        }

        // Fall back to file date
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
        {
            return File.GetLastWriteTime(location).ToString("yyyy-MM-dd");
        }

        return DateTime.Now.ToString("yyyy-MM-dd");
    }
}