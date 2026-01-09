// <copyright file="DeviceFactoryExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Devices.Speaker;

/// <summary>
/// Extension methods for <see cref="MachineBuilder"/> providing motherboard device factory registration.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide device factory registration for profile-based machine loading.
/// They allow profiles to declare devices in the <c>devices.motherboard</c> section that
/// are created when the profile is loaded.
/// </para>
/// <para>
/// The preferred approach is to use <see cref="DeviceFactoryRegistry.RegisterAllDeviceFactories"/>
/// which auto-discovers all devices marked with <see cref="DeviceTypeAttribute"/>. The manual
/// registration methods are retained for backward compatibility and special cases.
/// </para>
/// </remarks>
public static class DeviceFactoryExtensions
{
    /// <summary>
    /// Extension block for <see cref="MachineBuilder"/> providing device factory registration.
    /// </summary>
    extension(MachineBuilder builder)
    {
        /// <summary>
        /// Registers the speaker device factory for profile-based loading.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the "speaker" motherboard device factory. When a profile
        /// includes a speaker device in its <c>devices.motherboard</c> section, this factory
        /// creates a <see cref="SpeakerController"/> instance.
        /// </para>
        /// <para>
        /// The speaker controller handles the $C030 soft switch and records toggle events
        /// for audio synthesis.
        /// </para>
        /// <para>
        /// Prefer using <see cref="RegisterStandardDeviceFactories"/> which registers all
        /// known device factories automatically.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterSpeakerDeviceFactory()
        {
            return builder.RegisterMotherboardDeviceFactory("speaker", _ => new SpeakerController());
        }

        /// <summary>
        /// Registers the PocketWatch slot card factory for profile-based loading.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the "pocketwatch" slot card factory. When a profile
        /// includes a PocketWatch card in its <c>devices.slots.cards</c> section, this factory
        /// creates a <see cref="PocketWatchCard"/> instance.
        /// </para>
        /// <para>
        /// Prefer using <see cref="RegisterStandardDeviceFactories"/> which registers all
        /// known device factories automatically.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterPocketWatchCardFactory()
        {
            return builder.RegisterSlotCardFactory("pocketwatch", _ => new PocketWatchCard());
        }

        /// <summary>
        /// Registers all standard device factories for profile-based loading using auto-discovery.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method uses reflection to discover all device types marked with
        /// <see cref="DeviceTypeAttribute"/> and registers their factories automatically.
        /// This includes both motherboard devices and slot cards.
        /// </para>
        /// <para>
        /// Additionally, this method registers device factories that require special
        /// configuration beyond simple instantiation, such as the Language Card which
        /// needs memory layers and swap groups configured.
        /// </para>
        /// <para>
        /// Currently registered devices:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Motherboard: Speaker ($C030)</description></item>
        /// <item><description>Motherboard: Language Card (16KB RAM at $D000-$FFFF)</description></item>
        /// <item><description>Slot Cards: PocketWatch (Thunderclock-compatible RTC)</description></item>
        /// </list>
        /// <para>
        /// Additional devices are automatically registered as they are added with
        /// the <see cref="DeviceTypeAttribute"/>.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterStandardDeviceFactories()
        {
            // Register auto-discovered device factories (simple devices with parameterless constructors)
            builder.RegisterAllDeviceFactories();

            // Register devices that require special memory configuration.
            // The Language Card needs memory layers and swap groups configured, which can't
            // be done through simple auto-discovery. We use a placeholder ROM target that
            // allows the underlying base ROM mapping to show through when LC RAM is disabled.
            builder.RegisterLanguageCardFactory();

            return builder;
        }
    }
}