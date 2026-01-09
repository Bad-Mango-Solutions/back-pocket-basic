// <copyright file="DeviceDebugCommandAttribute.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Marks a class as a device-specific debug command for auto-registration.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to command handler classes that provide device-specific
/// debug functionality. The command will be automatically discovered and registered
/// with the debug console when the <c>DeviceDebugCommandsModule</c> is loaded.
/// </para>
/// <para>
/// The command class must implement <c>ICommandHandler</c> from the Debug.Infrastructure
/// assembly and have a public parameterless constructor.
/// </para>
/// <para>
/// Example usage:
/// </para>
/// <code>
/// [DeviceDebugCommand]
/// public sealed class PwTimeCommand : CommandHandlerBase
/// {
///     public PwTimeCommand() : base("pwtime", "Display time from memory location") { }
///     // ...
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DeviceDebugCommandAttribute : Attribute
{
}