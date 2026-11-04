// <copyright file="ConfigurationData.cs" company="Bad Mango Solutions">Copyright (c) Bad Mango Solutions. All rights reserved.</copyright>

namespace BadMango.Emulator.Core;

/// <summary>Provides access to runtime configuration settings.</summary>
public static class ConfigurationData
{
    /// <summary>The current executing assembly.</summary>
    public static string Assembly => System.Reflection.Assembly.GetExecutingAssembly().ToString();

    /// <summary>The configured .Net Core environment name.</summary>
    public static string Environment => ShellEnvironment ?? "Production";

    /// <summary>Indicates whether the application is running in a CI environment.</summary>
    public static bool IsCI { get; } =
        string.Equals(
            System.Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static string? ShellEnvironment => System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                                               System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
}