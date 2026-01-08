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
        /// Call this method before <see cref="MachineBuilder.FromProfile"/> when loading
        /// profiles that include a speaker device.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterSpeakerDeviceFactory()
        {
            return builder.RegisterMotherboardDeviceFactory("speaker", _ => new SpeakerController());
        }

        /// <summary>
        /// Registers all standard motherboard device factories for profile-based loading.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This convenience method registers factories for all standard motherboard devices:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Speaker ($C030)</description></item>
        /// </list>
        /// <para>
        /// Additional device factories (keyboard, video, game I/O) will be added as they
        /// are implemented.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterStandardDeviceFactories()
        {
            return builder.RegisterSpeakerDeviceFactory();
        }
    }
}
