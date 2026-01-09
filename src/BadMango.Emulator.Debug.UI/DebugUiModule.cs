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
/// When running in headless mode (no Avalonia), this module should not be registered,
/// and commands will fall back to console-based output.
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