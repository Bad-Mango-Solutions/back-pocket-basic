// <copyright file="TestAppBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(BadMango.Emulator.TextEditor.Tests.TestAppBuilder))]

namespace BadMango.Emulator.TextEditor.Tests;

/// <summary>
/// Configures the Avalonia headless test application for TextEditor tests.
/// </summary>
public class TestAppBuilder
{
    /// <summary>
    /// Builds the Avalonia application for headless testing.
    /// </summary>
    /// <returns>An <see cref="AppBuilder"/> configured for headless testing.</returns>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = true,
        });
}