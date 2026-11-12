// <copyright file="DebugUiModule.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI;

using Autofac;

using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Debug.UI.Services;

/// <summary>
/// Autofac module for registering Debug UI services.
/// </summary>
/// <remarks>
/// <para>
/// This module registers the debug window management infrastructure that allows
/// the console REPL to open Avalonia popup windows. It should be registered
/// with the Autofac container when UI support is desired.
/// </para>
/// <para>
/// The module registers:
/// <list type="bullet">
/// <item><description><see cref="DebugWindowManager"/> as <see cref="IDebugWindowManager"/></description></item>
/// </list>
/// </para>
/// <para>
/// The module can be registered even for headless runs. When AvaloniaBootstrapper.UseHeadless is true,
/// Avalonia will be initialized with its headless platform, enabling video rendering and diagnostics
/// without a physical display (useful for AI agents performing video diagnostics).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.RegisterModule&lt;DebugUiModule&gt;();
/// </code>
/// </example>
public class DebugUiModule : Module
{
    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DebugWindowManager>()
            .As<IDebugWindowManager>()
            .SingleInstance();
    }
}