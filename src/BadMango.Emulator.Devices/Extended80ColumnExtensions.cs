// <copyright file="Extended80ColumnExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;

/// <summary>
/// Extension methods for <see cref="MachineBuilder"/> providing Extended 80-Column Card support.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide Extended 80-Column Card device registration for profile-based machine loading.
/// </para>
/// </remarks>
public static class Extended80ColumnExtensions
{
    /// <summary>
    /// Extension block for <see cref="MachineBuilder"/> providing Extended 80-Column Card registration.
    /// </summary>
    extension(MachineBuilder builder)
    {
        /// <summary>
        /// Registers the Extended 80-Column Card device factory for profile-based loading.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers the "extended80column" device factory. The factory creates
        /// an <see cref="Extended80ColumnDevice"/> instance and configures its memory
        /// on the bus (auxiliary RAM layers for 80-column text and graphics).
        /// </para>
        /// <para>
        /// The Extended 80-Column Card provides 64KB of auxiliary RAM enabling:
        /// <list type="bullet">
        /// <item><description>80-column text display (interleaved main/aux memory)</description></item>
        /// <item><description>Double hi-res graphics</description></item>
        /// <item><description>Expanded memory for applications</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Call this method before <see cref="MachineBuilder.FromProfile"/> when loading
        /// profiles that declare an "extended80column" motherboard device.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterExtended80ColumnFactory()
        {
            return builder.RegisterMotherboardDeviceFactory("extended80column", b =>
            {
                var device = new Extended80ColumnDevice();

                // Configure memory during the memory configuration phase.
                // This sets up the required auxiliary RAM layers BEFORE device.Initialize() is called.
                b.ConfigureMemory((bus, registry) =>
                {
                    device.ConfigureMemory(bus, registry);
                });

                return device;
            });
        }

        /// <summary>
        /// Adds an Extended 80-Column Card device with full auxiliary RAM support.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method creates and configures an Extended 80-Column Card device with 64KB auxiliary RAM.
        /// </para>
        /// <para>
        /// Use this method when configuring a machine programmatically without a profile.
        /// For profile-based loading, use <see cref="RegisterExtended80ColumnFactory(MachineBuilder)"/> instead.
        /// </para>
        /// </remarks>
        public MachineBuilder WithExtended80ColumnDevice()
        {
            var device = new Extended80ColumnDevice();

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