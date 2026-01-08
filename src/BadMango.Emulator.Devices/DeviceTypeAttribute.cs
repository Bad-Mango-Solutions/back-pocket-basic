// <copyright file="DeviceTypeAttribute.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Marks a device class with its type identifier for auto-registration.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to classes implementing <see cref="Bus.Interfaces.IMotherboardDevice"/>
/// or <see cref="Bus.Interfaces.ISlotCard"/> to enable automatic factory registration.
/// </para>
/// <para>
/// The <see cref="TypeId"/> corresponds to the "type" field in machine profile device entries:
/// </para>
/// <code>
/// // Profile JSON
/// "devices": {
///   "motherboard": [
///     { "type": "speaker", "name": "Speaker", "enabled": true }
///   ]
/// }
///
/// // Device class
/// [DeviceType("speaker")]
/// public sealed class SpeakerController : ISpeakerDevice { }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DeviceTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceTypeAttribute"/> class.
    /// </summary>
    /// <param name="typeId">The device type identifier used in profiles (e.g., "speaker", "pocketwatch").</param>
    public DeviceTypeAttribute(string typeId)
    {
        TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
    }

    /// <summary>
    /// Gets the device type identifier.
    /// </summary>
    /// <value>The type identifier used in profile device entries.</value>
    public string TypeId { get; }
}