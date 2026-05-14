// <copyright file="DeviceFactoryRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Reflection;
using System.Text.Json;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Serilog;

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
/// </para>
/// <list type="bullet">
/// <item><description>Have the <see cref="DeviceTypeAttribute"/> applied.</description></item>
/// <item><description>Implement <see cref="IMotherboardDevice"/> or <see cref="ISlotCard"/>.</description></item>
/// <item><description>
/// Expose a public constructor whose parameters are all either resolvable by the registry
/// (currently <see cref="ILogger"/> via <see cref="LoggerFactory"/>) or have default values.
/// A public parameterless constructor is the simplest such case.
/// </description></item>
/// </list>
/// <para>
/// To accommodate device controllers that take constructor dependencies (the canonical
/// example is <see cref="DiskIIController"/>, which requires an injected
/// <see cref="ILogger"/>), the registry resolves each constructor argument at factory
/// invocation time:
/// </para>
/// <list type="bullet">
/// <item><description>
/// Parameters of type <see cref="ILogger"/> are resolved via the configurable
/// <see cref="LoggerFactory"/> hook. Application bootstrap code (or an Autofac
/// module) should set <see cref="LoggerFactory"/> so loggers are scoped to the
/// device's type. When unset, the registry falls back to a sink-less Serilog
/// logger so unit tests and bootstrap-light callers still get a working factory.
/// </description></item>
/// <item><description>
/// Parameters with default values fall back to those defaults.
/// </description></item>
/// <item><description>
/// Types annotated with <see cref="DeviceTypeAttribute"/> whose constructors cannot be
/// satisfied by either of the rules above are recorded in <see cref="SkippedDeviceTypes"/>
/// so that callers can log a clear diagnostic instead of silently dropping the device.
/// </description></item>
/// </list>
/// <para>
/// Two classes must not declare the same <see cref="DeviceTypeAttribute.DeviceTypeId"/>;
/// the registry refuses to overwrite an existing registration and records the duplicate
/// in <see cref="SkippedDeviceTypes"/>.
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

    /// <summary>
    /// Lazily-constructed sink-less Serilog logger used as a fallback when
    /// <see cref="LoggerFactory"/> has not been configured. It silently discards all events,
    /// which keeps device construction working in tests and bootstrap-light callers
    /// without the registry ever touching the global <c>Log.Logger</c> facade.
    /// </summary>
    private static readonly Lazy<ILogger> SilentLoggerInstance = new(
        () => new LoggerConfiguration().CreateLogger(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static bool isInitialized;

    /// <summary>
    /// Gets or sets the factory used to obtain a <see cref="ILogger"/> when constructing
    /// a discovered device that takes one as a constructor parameter.
    /// </summary>
    /// <value>
    /// A delegate that returns an <see cref="ILogger"/> for the given device <see cref="Type"/>.
    /// Application bootstrap code (or an Autofac module) should set this once at startup,
    /// for example <c>t =&gt; container.Resolve&lt;ILogger&gt;().ForContext(t)</c>. When
    /// <see langword="null"/>, the registry falls back to a sink-less Serilog logger so
    /// device construction never throws solely because no logger has been configured —
    /// useful for unit tests and command-line tools that don't wire up Serilog.
    /// </value>
    public static Func<Type, ILogger>? LoggerFactory { get; set; }

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
    /// <c>"no public constructor whose parameters can be resolved"</c> or
    /// <c>"duplicate slot card type id 'foo'; another type is already registered"</c>.
    /// Callers that have an <see cref="ILogger"/> available (for example, the application
    /// bootstrap or an Autofac module) should iterate this collection after calling
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
        var constructor = FindResolvableConstructor(deviceType);
        if (constructor is null)
        {
            RecordSkip(deviceType, "no public constructor whose parameters can all be resolved (expected a parameterless ctor or one whose non-default parameters are all of type Serilog.ILogger)");
            return;
        }

        if (motherboardFactories.ContainsKey(typeId))
        {
            RecordSkip(deviceType, $"duplicate motherboard device type id '{typeId}'; another type is already registered");
            return;
        }

        motherboardFactories[typeId] = _ => (IMotherboardDevice)InvokeConstructor(constructor, deviceType);
    }

    private static void RegisterSlotCardFactory(string typeId, Type deviceType)
    {
        var constructor = FindResolvableConstructor(deviceType);
        if (constructor is null)
        {
            RecordSkip(deviceType, "no public constructor whose parameters can all be resolved (expected a parameterless ctor or one whose non-default parameters are all of type Serilog.ILogger)");
            return;
        }

        if (slotCardFactories.ContainsKey(typeId))
        {
            RecordSkip(deviceType, $"duplicate slot card type id '{typeId}'; another type is already registered");
            return;
        }

        slotCardFactories[typeId] = (_, _) => (ISlotCard)InvokeConstructor(constructor, deviceType);
    }

    /// <summary>
    /// Finds the public constructor with the most parameters whose arguments the registry
    /// can satisfy (parameterless wins over multi-arg only when no resolvable multi-arg
    /// ctor exists).
    /// </summary>
    private static ConstructorInfo? FindResolvableConstructor(Type deviceType)
    {
        // Prefer parameterless when present — it's unambiguous and matches the historical contract.
        var parameterless = deviceType.GetConstructor(Type.EmptyTypes);
        if (parameterless is not null)
        {
            return parameterless;
        }

        // Otherwise pick the public constructor with the most parameters that we can fully resolve.
        ConstructorInfo? best = null;
        var bestParamCount = -1;
        foreach (var candidate in deviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = candidate.GetParameters();
            if (!parameters.All(CanResolveParameter))
            {
                continue;
            }

            if (parameters.Length > bestParamCount)
            {
                best = candidate;
                bestParamCount = parameters.Length;
            }
        }

        return best;
    }

    private static bool CanResolveParameter(ParameterInfo parameter)
        => parameter.ParameterType == typeof(ILogger) || parameter.HasDefaultValue;

    private static object InvokeConstructor(ConstructorInfo constructor, Type deviceType)
    {
        var parameters = constructor.GetParameters();
        if (parameters.Length == 0)
        {
            return constructor.Invoke(null);
        }

        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            args[i] = ResolveParameter(parameters[i], deviceType);
        }

        return constructor.Invoke(args);
    }

    private static object? ResolveParameter(ParameterInfo parameter, Type deviceType)
    {
        if (parameter.ParameterType == typeof(ILogger))
        {
            return LoggerFactory?.Invoke(deviceType) ?? SilentLoggerInstance.Value;
        }

        return parameter.DefaultValue;
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