// <copyright file="ConfigurationModule.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Registration;

using Autofac;

using BadMango.Emulator.Configuration.IO;
using BadMango.Emulator.Configuration.Services;

/// <summary>
/// Autofac module for registering configuration services.
/// </summary>
/// <remarks>
/// This module registers the configuration components including:
/// <list type="bullet">
///   <item><description><see cref="ISettingsService"/> - Settings persistence and change notifications</description></item>
///   <item><description><see cref="ISettingsMigrator"/> - Settings schema migration</description></item>
///   <item><description><see cref="IPathValidator"/> - Path validation and normalization</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// builder.RegisterModule&lt;ConfigurationModule&gt;();
/// </code>
/// </example>
public class ConfigurationModule : Module
{
    /// <inheritdoc/>
    protected override void Load(ContainerBuilder builder)
    {
        // Register SettingsService as a singleton for application-wide settings
        builder.RegisterType<SettingsService>()
            .As<ISettingsService>()
            .SingleInstance();

        // Register SettingsMigrator as a singleton
        builder.RegisterType<SettingsMigrator>()
            .As<ISettingsMigrator>()
            .SingleInstance();

        // Register PathValidator as a singleton
        builder.RegisterType<PathValidator>()
            .As<IPathValidator>()
            .SingleInstance();
    }
}