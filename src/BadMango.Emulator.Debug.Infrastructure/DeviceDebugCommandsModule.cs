// <copyright file="DeviceDebugCommandsModule.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using System.Reflection;

using Autofac;

using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Devices;

using AutofacModule = Autofac.Module;

/// <summary>
/// Autofac module that auto-discovers and registers device-specific debug commands.
/// </summary>
/// <remarks>
/// <para>
/// This module scans for classes marked with <see cref="DeviceDebugCommandAttribute"/>
/// that implement <see cref="ICommandHandler"/> and registers them with the container.
/// </para>
/// <para>
/// Device debug commands provide device-specific debugging functionality, such as
/// interpreting memory as device-specific data structures. They are automatically
/// added to the debug console when this module is loaded.
/// </para>
/// </remarks>
public class DeviceDebugCommandsModule : AutofacModule
{
    /// <inheritdoc/>
    protected override void Load(ContainerBuilder builder)
    {
        // Scan the Debug.Infrastructure assembly for device debug commands
        var assembly = typeof(DeviceDebugCommandsModule).Assembly;

        var commandTypes = assembly.GetTypes()
            .Where(IsDeviceDebugCommand)
            .ToList();

        foreach (var commandType in commandTypes)
        {
            builder.RegisterType(commandType)
                .As<ICommandHandler>()
                .SingleInstance();
        }
    }

    /// <summary>
    /// Determines if a type is a device debug command.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type is a device debug command; otherwise, <see langword="false"/>.</returns>
    private static bool IsDeviceDebugCommand(Type type)
    {
        // Must be a concrete class
        if (type.IsAbstract || type.IsInterface)
        {
            return false;
        }

        // Must have the DeviceDebugCommandAttribute
        if (type.GetCustomAttribute<DeviceDebugCommandAttribute>() is null)
        {
            return false;
        }

        // Must implement ICommandHandler
        if (!typeof(ICommandHandler).IsAssignableFrom(type))
        {
            return false;
        }

        // Must have a parameterless constructor
        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            return false;
        }

        return true;
    }
}