// <copyright file="InfrastructureModule.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Infrastructure.Registration;

using Autofac;

using BadMango.Emulator.Infrastructure.Events;

/// <summary>
/// Autofac module for registering infrastructure services.
/// </summary>
/// <remarks>
/// This module registers the core infrastructure components including:
/// <list type="bullet">
///   <item><description><see cref="IEventAggregator"/> - The pub/sub event system</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// builder.RegisterModule&lt;InfrastructureModule&gt;();
/// </code>
/// </example>
public class InfrastructureModule : Module
{
    /// <inheritdoc/>
    protected override void Load(ContainerBuilder builder)
    {
        // Register EventAggregator as a singleton for application-wide pub/sub
        builder.RegisterType<EventAggregator>()
            .As<IEventAggregator>()
            .SingleInstance();
    }
}