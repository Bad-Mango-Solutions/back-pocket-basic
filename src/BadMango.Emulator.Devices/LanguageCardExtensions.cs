// <copyright file="LanguageCardExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Extension methods for <see cref="MachineBuilder"/> providing Language Card support.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide Language Card device registration for profile-based machine loading.
/// </para>
/// </remarks>
public static class LanguageCardExtensions
{
    /// <summary>
    /// Extension block for <see cref="MachineBuilder"/> providing Language Card registration.
    /// </summary>
    extension(MachineBuilder builder)
    {
        /// <summary>
        /// Registers the Language Card device factory for profile-based loading.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the "languagecard" device factory. The factory creates
        /// a <see cref="LanguageCardDevice"/> instance and configures its memory
        /// on the bus (layers and swap groups).
        /// </para>
        /// <para>
        /// The Language Card provides 16KB of bank-switched RAM at $D000-$FFFF:
        /// <list type="bullet">
        /// <item><description>Two 4KB banks for $D000-$DFFF (Bank 1 and Bank 2)</description></item>
        /// <item><description>One 8KB bank for $E000-$FFFF</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// When Language Card RAM is disabled, the layers are deactivated and reads
        /// fall through to the underlying base ROM mapping.
        /// </para>
        /// <para>
        /// Call this method before <see cref="MachineBuilder.FromProfile"/> when loading
        /// profiles that declare a "languagecard" motherboard device.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterLanguageCardFactory()
        {
            return builder.RegisterMotherboardDeviceFactory("languagecard", b =>
            {
                var device = new LanguageCardDevice();

                // Note: Do NOT call b.AddComponent(device) here.
                // ConfigureMotherboardDevice in MachineBuilder.FromProfile.cs will handle
                // adding the device as both a component and a scheduled device.

                // Configure memory during the memory configuration phase.
                // This sets up the required layers and swap groups BEFORE device.Initialize() is called.
                b.ConfigureMemory((bus, registry) =>
                {
                    device.ConfigureMemory(bus, registry);
                });

                return device;
            });
        }

        /// <summary>
        /// Registers the Language Card device factory for profile-based loading.
        /// </summary>
        /// <param name="romTargetFactory">
        /// Legacy parameter for backward compatibility. This parameter is ignored;
        /// the Language Card now uses layer deactivation to show ROM instead of a
        /// passthrough variant.
        /// </param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This overload is provided for backward compatibility with existing code
        /// that passes a ROM target factory. The factory parameter is ignored.
        /// </para>
        /// </remarks>
        [Obsolete("Use RegisterLanguageCardFactory() instead. The romTargetFactory parameter is no longer used.")]
        public MachineBuilder RegisterLanguageCardFactory(Func<MachineBuilder, IBusTarget> romTargetFactory)
        {
            // Ignore romTargetFactory - we now use layer deactivation instead
            return builder.RegisterMotherboardDeviceFactory("languagecard", b =>
            {
                var device = new LanguageCardDevice();

                // Configure memory during the memory configuration phase.
                b.ConfigureMemory((bus, registry) =>
                {
                    device.ConfigureMemory(bus, registry);
                });

                return device;
            });
        }

        /// <summary>
        /// Adds a Language Card device with self-contained ROM passthrough.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method creates and configures a Language Card device with its own 16KB RAM.
        /// When Language Card RAM is disabled, the layers are deactivated and reads
        /// fall through to the underlying base ROM mapping.
        /// </para>
        /// <para>
        /// Use this method when configuring a machine programmatically without a profile.
        /// For profile-based loading, use <see cref="RegisterLanguageCardFactory(MachineBuilder)"/> instead.
        /// </para>
        /// </remarks>
        public MachineBuilder WithLanguageCardDevice()
        {
            var device = new LanguageCardDevice();

            // Add the device as a component
            builder.AddComponent(device);

            // Add as a scheduled device for initialization
            builder.AddDevice(device);

            // Configure memory during the memory configuration phase
            builder.ConfigureMemory((bus, registry) =>
            {
                device.ConfigureMemory(bus, registry);
            });

            return builder;
        }
    }
}