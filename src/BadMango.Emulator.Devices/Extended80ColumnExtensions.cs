// <copyright file="Extended80ColumnExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Addr = System.UInt32;

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
        /// Memory configuration includes:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Page 0 composite target for sub-page routing ($0000-$0FFF)</description></item>
        /// <item><description>Auxiliary RAM layer for $1000-$BFFF (RAMRD/RAMWRT)</description></item>
        /// <item><description>Auxiliary hi-res layer for $2000-$3FFF (80STORE+PAGE2+HIRES)</description></item>
        /// </list>
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
                    // Configure page 0 with a composite target for sub-page auxiliary memory routing.
                    // This handles zero page ($0000-$00FF), stack ($0100-$01FF), and text page ($0400-$07FF)
                    // which cannot use layer-based switching due to 4KB page granularity.
                    ConfigurePage0CompositeTarget(b, bus, registry, device);

                    // Configure layers for page-aligned regions ($1000+)
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
                // Configure page 0 composite target
                ConfigurePage0CompositeTarget(builder, bus, registry, device);

                // Configure layers for page-aligned regions ($1000+)
                device.ConfigureMemory(bus, registry);
            });

            return builder;
        }

        /// <summary>
        /// Configures page 0 ($0000-$0FFF) with a composite target for sub-page auxiliary memory routing.
        /// </summary>
        /// <param name="machineBuilder">The machine builder for accessing physical memory.</param>
        /// <param name="bus">The memory bus to configure.</param>
        /// <param name="registry">The device registry.</param>
        /// <param name="device">The Extended 80-Column device for state queries.</param>
        private static void ConfigurePage0CompositeTarget(
            MachineBuilder machineBuilder,
            IMemoryBus bus,
            IDeviceRegistry registry,
            Extended80ColumnDevice device)
        {
            // Get the main RAM physical memory from the builder
            // The main RAM should be configured in the profile as "main-ram-48k" or similar
            var mainRam = machineBuilder.GetPhysicalMemory("main-ram-48k");
            if (mainRam is null)
            {
                // Fall back to trying other common names
                mainRam = machineBuilder.GetPhysicalMemory("main-ram");
            }

            if (mainRam is null)
            {
                // Can't configure page 0 without main RAM - the profile may handle this differently
                return;
            }

            // Create a RAM target for page 0 of main memory
            var page0MainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN_PAGE0");

            // Get the auxiliary page 0 target from the device
            var page0AuxTarget = device.GetAuxPage0Target();

            // Create the composite target that routes based on routing table
            var page0Target = new Extended80ColumnPage0Target(page0MainTarget, page0AuxTarget);

            // Wire up the device to update the routing table when switches change
            device.SetPage0Target(page0Target);

            // Remap page 0 to use the composite target
            int deviceId = registry.GenerateId();
            registry.Register(deviceId, "Extended80Column", "Page0_Composite", "Memory/Page0");
            bus.MapRegion(
                virtualBase: 0x0000,
                size: 0x1000,
                deviceId: deviceId,
                regionTag: RegionTag.Ram,
                perms: PagePerms.All,
                caps: page0Target.Capabilities,
                target: page0Target,
                physicalBase: 0);
        }
    }
}