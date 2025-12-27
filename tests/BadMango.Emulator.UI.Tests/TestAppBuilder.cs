// <copyright file="TestAppBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(BadMango.Emulator.UI.Tests.TestAppBuilder))]

namespace BadMango.Emulator.UI.Tests;

/// <summary>
/// Configures the Avalonia headless test application.
/// </summary>
public class TestAppBuilder
{
    /// <summary>
    /// Builds the Avalonia application for headless testing.
    /// </summary>
    /// <returns>An <see cref="AppBuilder"/> configured for headless testing.</returns>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = true,
        });
}