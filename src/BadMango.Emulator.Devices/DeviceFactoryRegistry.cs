// <copyright file="DeviceFactoryRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Reflection;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Auto-discovers and registers device factories based on <see cref="DeviceTypeAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// This registry scans assemblies for device types marked with <see cref="DeviceTypeAttribute"/>
/// and automatically creates factory registrations. This eliminates the need to manually
/// register each device factory.
/// </para>
/// <para>
/// Device classes must:
/// <list type="bullet">
/// <item><description>Have the <see cref="DeviceTypeAttribute"/> applied</description></item>
/// <item><description>Implement <see cref="IMotherboardDevice"/> or <see cref="ISlotCard"/></description></item>
/// <item><description>Have a public parameterless constructor</description></item>
/// </list>
/// </para>
/// </remarks>
public static class DeviceFactoryRegistry
{
    private static readonly Lock SyncLock = new();

#pragma warning disable SA1311
    private static readonly Dictionary<string, Func<MachineBuilder, IMotherboardDevice>> motherboardFactories
        = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Func<MachineBuilder, ISlotCard>> slotCardFactories
        = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore SA1311

    private static bool isInitialized;

    /// <summary>
    /// Gets the discovered motherboard device factories.
    /// </summary>
    /// <value>A dictionary mapping type IDs to device factory functions.</value>
    public static IReadOnlyDictionary<string, Func<MachineBuilder, IMotherboardDevice>> MotherboardDeviceFactories
        => motherboardFactories;

    /// <summary>
    /// Gets the discovered slot card factories.
    /// </summary>
    /// <value>A dictionary mapping type IDs to slot card factory functions.</value>
    public static IReadOnlyDictionary<string, Func<MachineBuilder, ISlotCard>> SlotCardFactories
        => slotCardFactories;

    /// <summary>
    /// Ensures device factories are discovered and registered.
    /// </summary>
    /// <remarks>
    /// This method is idempotent - it only performs discovery once.
    /// Call this before using <see cref="MotherboardDeviceFactories"/> or <see cref="SlotCardFactories"/>.
    /// </remarks>
    public static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        lock (SyncLock)
        {
            if (isInitialized)
            {
                return;
            }

            DiscoverDevices();
            isInitialized = true;
        }
    }

    /// <summary>
    /// Registers all discovered device factories with the machine builder.
    /// </summary>
    /// <param name="builder">The machine builder to register factories with.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// This method discovers all device types in loaded assemblies and registers
    /// their factories with the builder. It handles both motherboard devices and
    /// slot cards.
    /// </remarks>
    public static MachineBuilder RegisterAllDeviceFactories(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        EnsureInitialized();

        // Register motherboard device factories
        foreach (var (typeId, factory) in motherboardFactories)
        {
            builder.RegisterMotherboardDeviceFactory(typeId, factory);
        }

        // Register slot card factories
        foreach (var (typeId, factory) in slotCardFactories)
        {
            builder.RegisterSlotCardFactory(typeId, factory);
        }

        return builder;
    }

    /// <summary>
    /// Scans for device types and creates factory registrations.
    /// </summary>
    private static void DiscoverDevices()
    {
        // Get the Devices assembly
        var devicesAssembly = typeof(DeviceFactoryRegistry).Assembly;

        // Scan for types with DeviceTypeAttribute
        foreach (var type in devicesAssembly.GetTypes())
        {
            var attribute = type.GetCustomAttribute<DeviceTypeAttribute>();
            if (attribute is null)
            {
                continue;
            }

            // Check if it's a motherboard device
            if (typeof(IMotherboardDevice).IsAssignableFrom(type))
            {
                RegisterMotherboardFactory(attribute.DeviceTypeId, type);
            }

            // Check if it's a slot card
            if (typeof(ISlotCard).IsAssignableFrom(type))
            {
                RegisterSlotCardFactory(attribute.DeviceTypeId, type);
            }
        }
    }

    private static void RegisterMotherboardFactory(string typeId, Type deviceType)
    {
        // Find parameterless constructor
        var constructor = deviceType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            // Device type doesn't have a parameterless constructor - skip
            return;
        }

        motherboardFactories[typeId] = _ => (IMotherboardDevice)constructor.Invoke(null);
    }

    private static void RegisterSlotCardFactory(string typeId, Type deviceType)
    {
        // Find parameterless constructor
        var constructor = deviceType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            // Device type doesn't have a parameterless constructor - skip
            return;
        }

        slotCardFactories[typeId] = _ => (ISlotCard)constructor.Invoke(null);
    }
}