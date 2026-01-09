// <copyright file="DebugApp.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI;

using Avalonia;
using Avalonia.Markup.Xaml;

/// <summary>
/// Minimal Avalonia Application for the debug UI popup windows.
/// </summary>
/// <remarks>
/// <para>
/// This application class provides Avalonia infrastructure for popup windows
/// launched from the console debugger. Unlike the main UI application, this
/// does not create a main window automatically - windows are created on demand
/// by the <see cref="Services.DebugWindowManager"/>.
/// </para>
/// <para>
/// The application is designed to run on a background thread while the console
/// REPL runs on the main thread, following the threading model described in
/// the Debug Video Window specification.
/// </para>
/// </remarks>
public partial class DebugApp : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // Do not create a main window - the debug console is the main application
        // Windows will be created on demand by the DebugWindowManager
        base.OnFrameworkInitializationCompleted();
    }
}