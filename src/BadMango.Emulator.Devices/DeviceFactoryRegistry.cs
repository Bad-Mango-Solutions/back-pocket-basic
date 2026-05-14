// <copyright file="DeviceFactoryRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Reflection;
using System.Text.Json;

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
/// <para>
/// Types annotated with <see cref="DeviceTypeAttribute"/> that cannot be auto-instantiated
/// (for example, controllers that require an injected logger or other dependencies) are
/// recorded in <see cref="SkippedDeviceTypes"/> so that callers can log a clear diagnostic
/// rather than silently dropping the device. The Disk II controller is one such case:
/// <see cref="DiskIIController"/> is reported as skipped, while the parameterless
/// <see cref="DiskIIControllerStub"/> sharing the same <c>disk-ii-compatible</c> type id
/// is registered as the auto-discovered factory. A configured factory that builds the
/// real controller (with logger, boot ROM, and disk images) can replace the stub
/// entry on the <see cref="MachineBuilder"/> via
/// <see cref="MachineBuilder.RegisterSlotCardFactory(string, Func{MachineBuilder, JsonElement?, ISlotCard})"/>.
/// </para>
/// </remarks>
public static class DeviceFactoryRegistry
{
    private static readonly Lock SyncLock = new();

#pragma warning disable SA1311
    private static readonly Dictionary<string, Func<MachineBuilder, IMotherboardDevice>> motherboardFactories
        = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Func<MachineBuilder, JsonElement?, ISlotCard>> slotCardFactories
        = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> skippedDeviceTypes
        = new(StringComparer.Ordinal);
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
    /// <value>
    /// A dictionary mapping type IDs to slot card factory delegates of shape
    /// <c>Func&lt;MachineBuilder, JsonElement?, ISlotCard&gt;</c>. The second argument is the
    /// optional per-card <see cref="JsonElement"/> taken from
    /// <see cref="Core.Configuration.SlotCardProfile.Config"/>; auto-discovered factories that
    /// do not require configuration ignore it.
    /// </value>
    public static IReadOnlyDictionary<string, Func<MachineBuilder, JsonElement?, ISlotCard>> SlotCardFactories
        => slotCardFactories;

    /// <summary>
    /// Gets the device types that were discovered (annotated with <see cref="DeviceTypeAttribute"/>)
    /// but excluded from auto-registration, together with a human-readable reason.
    /// </summary>
    /// <value>
    /// A dictionary keyed by the assembly-qualified-ish display name of the skipped device
    /// type (<see cref="Type.FullName"/>), mapping to the diagnostic reason — for example
    /// <c>"requires constructor dependencies (no public parameterless constructor); register
    /// a configured factory on MachineBuilder instead"</c>. Callers that have an
    /// <see cref="Serilog.ILogger"/> available (for example, the application bootstrap or an
    /// Autofac module) should iterate this collection after calling
    /// <see cref="EnsureInitialized"/> and emit a warning per entry so that misconfigured
    /// devices fail loudly rather than silently.
    /// </value>
    public static IReadOnlyDictionary<string, string> SkippedDeviceTypes
        => skippedDeviceTypes;

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
        // Find parameterless constructor first so that "needs custom factory" is reported
        // even when another type with the same typeId is already registered.
        var constructor = deviceType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            RecordSkip(deviceType, "requires constructor dependencies (no public parameterless constructor); register a configured factory on MachineBuilder instead");
            return;
        }

        // If another type already supplied a working factory for this typeId, keep it
        // and record this one as skipped so the duplicate doesn't silently win.
        if (motherboardFactories.ContainsKey(typeId))
        {
            RecordSkip(deviceType, $"duplicate motherboard device type id '{typeId}'; another type is already registered");
            return;
        }

        motherboardFactories[typeId] = _ => (IMotherboardDevice)constructor.Invoke(null);
    }

    private static void RegisterSlotCardFactory(string typeId, Type deviceType)
    {
        // Find parameterless constructor first so that "needs custom factory" is reported
        // even when another type with the same typeId is already registered.
        var constructor = deviceType.GetConstructor(Type.EmptyTypes);
        if (constructor is null)
        {
            RecordSkip(deviceType, "requires constructor dependencies (no public parameterless constructor); register a configured factory on MachineBuilder instead");
            return;
        }

        // If another type already supplied a working factory for this typeId, keep it
        // and record this one as skipped so the duplicate doesn't silently win.
        if (slotCardFactories.ContainsKey(typeId))
        {
            RecordSkip(deviceType, $"duplicate slot card type id '{typeId}'; another type is already registered");
            return;
        }

        slotCardFactories[typeId] = (_, _) => (ISlotCard)constructor.Invoke(null);
    }

    private static void RecordSkip(Type deviceType, string reason)
    {
        // Prefer FullName (namespace + nested-type chain). Fall back to
        // AssemblyQualifiedName to avoid collisions for distinct types that happen to
        // share a simple name; only fall back to Name as a last resort.
        var key = deviceType.FullName
                  ?? deviceType.AssemblyQualifiedName
                  ?? deviceType.Name;
        skippedDeviceTypes[key] = reason;
    }
}