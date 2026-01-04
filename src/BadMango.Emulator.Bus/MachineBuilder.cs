// <copyright file="MachineBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

using Interfaces;

/// <summary>
/// Builder for constructing machine configurations with a fluent API.
/// </summary>
/// <remarks>
/// <para>
/// The machine builder provides a clean API to construct any machine configuration.
/// The machine abstraction is a component bucket, not a typed hierarchy.
/// Machine-specific configuration lives in extension methods.
/// </para>
/// <para>
/// The builder handles the complex initialization order:
/// </para>
/// <list type="number">
/// <item><description>Create infrastructure (registry, scheduler, signals)</description></item>
/// <item><description>Create memory bus with specified address space</description></item>
/// <item><description>Create devices (RAM, ROM, I/O controllers)</description></item>
/// <item><description>Wire devices to bus (map pages)</description></item>
/// <item><description>Create CPU</description></item>
/// <item><description>Create machine</description></item>
/// <item><description>Initialize devices with event context</description></item>
/// </list>
/// </remarks>
public sealed class MachineBuilder
{
    private readonly List<object> components = [];
    private readonly List<IScheduledDevice> scheduledDevices = [];
    private readonly List<RomDescriptor> romDescriptors = [];
    private readonly List<Action<IMemoryBus, IDeviceRegistry>> memoryConfigurations = [];
    private readonly Dictionary<string, int> layerPriorities = new(StringComparer.Ordinal);
    private readonly List<(string LayerName, Addr VirtualBase, Addr Size, IBusTarget Target, RegionTag Tag, PagePerms Perms)> layeredMappings = [];
    private readonly List<ICompositeLayer> compositeLayers = [];
    private readonly List<Action<IMachine>> postBuildCallbacks = [];

    private int addressSpaceBits = 16;
    private CpuFamily cpuFamily = CpuFamily.Cpu65C02;
    private Func<IEventContext, ICpu>? cpuFactory;

    /// <summary>
    /// Sets the address space size for the machine.
    /// </summary>
    /// <param name="bits">The number of bits in the address space (12-32).</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bits"/> is less than 12 or greater than 32.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Common values:
    /// </para>
    /// <list type="bullet">
    /// <item><description>16 bits = 64KB (6502, 65C02)</description></item>
    /// <item><description>24 bits = 16MB (65C816)</description></item>
    /// <item><description>32 bits = 4GB (65832)</description></item>
    /// </list>
    /// </remarks>
    public MachineBuilder WithAddressSpace(int bits)
    {
        if (bits < 12 || bits > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(bits), bits, "Address space bits must be between 12 and 32.");
        }

        addressSpaceBits = bits;
        return this;
    }

    /// <summary>
    /// Sets the CPU family for the machine.
    /// </summary>
    /// <param name="family">The CPU family to use.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// The CPU factory is selected based on the CPU family. Use <see cref="WithCpuFactory"/>
    /// to provide a custom CPU implementation.
    /// </remarks>
    public MachineBuilder WithCpu(CpuFamily family)
    {
        cpuFamily = family;
        return this;
    }

    /// <summary>
    /// Sets a custom CPU factory for the machine.
    /// </summary>
    /// <param name="factory">A function that creates an <see cref="ICpu"/> given an event context.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Use this method to provide a custom CPU implementation or to configure
    /// CPU-specific options not exposed through <see cref="WithCpu"/>.
    /// </remarks>
    public MachineBuilder WithCpuFactory(Func<IEventContext, ICpu> factory)
    {
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));
        cpuFactory = factory;
        return this;
    }

    /// <summary>
    /// Adds an arbitrary component to the machine's component bucket.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="component">The component instance to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="component"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Components can be retrieved later via <see cref="IEventContext.GetComponent{T}"/>.
    /// </remarks>
    public MachineBuilder AddComponent<T>(T component)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(component, nameof(component));
        components.Add(component);
        return this;
    }

    /// <summary>
    /// Adds a scheduled device to the machine.
    /// </summary>
    /// <param name="device">The device to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="device"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Scheduled devices are initialized with the event context after the machine is built.
    /// They can then schedule events and respond to signals.
    /// </remarks>
    public MachineBuilder AddDevice(IScheduledDevice device)
    {
        ArgumentNullException.ThrowIfNull(device, nameof(device));
        scheduledDevices.Add(device);
        return this;
    }

    /// <summary>
    /// Configures memory mappings through a callback.
    /// </summary>
    /// <param name="configure">A callback that receives the memory bus and device registry for configuration.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method allows complex memory configurations that require access to both
    /// the memory bus and device registry. Multiple configuration callbacks can be added;
    /// they are invoked in order during <see cref="Build"/>.
    /// </remarks>
    public MachineBuilder ConfigureMemory(Action<IMemoryBus, IDeviceRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure, nameof(configure));
        memoryConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// Maps a memory region to a bus target.
    /// </summary>
    /// <param name="virtualBase">The starting virtual address (must be page-aligned).</param>
    /// <param name="size">The size of the region in bytes (must be page-aligned).</param>
    /// <param name="target">The bus target to handle accesses.</param>
    /// <param name="tag">The region tag for categorization.</param>
    /// <param name="perms">The permissions for the region.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This is a convenience method that adds a memory configuration callback.
    /// For complex mappings or mappings that need device IDs, use <see cref="ConfigureMemory"/>.
    /// </remarks>
    public MachineBuilder MapRegion(
        Addr virtualBase,
        Addr size,
        IBusTarget target,
        RegionTag tag = default,
        PagePerms perms = PagePerms.All)
    {
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        memoryConfigurations.Add((bus, registry) =>
        {
            int deviceId = registry.GenerateId();
            registry.Register(deviceId, "Region", $"Region@{virtualBase:X4}", $"Memory/{virtualBase:X4}");
            bus.MapRegion(virtualBase, size, deviceId, tag, perms, target.Capabilities, target, 0);
        });

        return this;
    }

    /// <summary>
    /// Creates a new mapping layer with the specified name and priority.
    /// </summary>
    /// <param name="name">A unique name for the layer.</param>
    /// <param name="priority">The priority of the layer (higher values override lower).</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when a layer with the same name already exists.</exception>
    /// <remarks>
    /// Layers are used to implement overlays such as vector tables that need to
    /// appear at specific addresses regardless of underlying memory configuration.
    /// </remarks>
    public MachineBuilder CreateLayer(string name, int priority)
    {
        if (layerPriorities.ContainsKey(name))
        {
            throw new ArgumentException($"Layer '{name}' already exists.", nameof(name));
        }

        layerPriorities[name] = priority;
        return this;
    }

    /// <summary>
    /// Adds a mapping to an existing layer.
    /// </summary>
    /// <param name="layerName">The name of the layer to add the mapping to.</param>
    /// <param name="virtualBase">The starting virtual address (must be page-aligned).</param>
    /// <param name="size">The size of the region in bytes (must be page-aligned).</param>
    /// <param name="target">The bus target to handle accesses.</param>
    /// <param name="tag">The region tag for categorization.</param>
    /// <param name="perms">The permissions for the region.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="layerName"/> does not exist.</exception>
    public MachineBuilder AddLayeredMapping(
        string layerName,
        Addr virtualBase,
        Addr size,
        IBusTarget target,
        RegionTag tag,
        PagePerms perms)
    {
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        if (!layerPriorities.ContainsKey(layerName))
        {
            throw new ArgumentException($"Layer '{layerName}' does not exist. Create it first with CreateLayer.", nameof(layerName));
        }

        layeredMappings.Add((layerName, virtualBase, size, target, tag, perms));
        return this;
    }

    /// <summary>
    /// Adds a composite layer to the machine.
    /// </summary>
    /// <param name="layer">The composite layer to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="layer"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Composite layers provide dynamic target resolution based on address and access type.
    /// They are useful for complex memory overlays like the Language Card which has:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Multiple switchable RAM banks</description></item>
    /// <item><description>Dynamic read/write enable states</description></item>
    /// <item><description>Pre-write state machine for write protection</description></item>
    /// </list>
    /// <para>
    /// The composite layer is also added as a component for retrieval via
    /// <see cref="IEventContext.GetComponent{T}"/>.
    /// </para>
    /// </remarks>
    public MachineBuilder AddCompositeLayer(ICompositeLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer, nameof(layer));
        compositeLayers.Add(layer);
        components.Add(layer);
        return this;
    }

    /// <summary>
    /// Adds a ROM image to the machine at the specified address.
    /// </summary>
    /// <param name="data">The ROM data bytes.</param>
    /// <param name="loadAddress">The address at which to load the ROM.</param>
    /// <param name="name">An optional name for the ROM.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
    public MachineBuilder WithRom(byte[] data, Addr loadAddress, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        romDescriptors.Add(new RomDescriptor(data, loadAddress, name));
        return this;
    }

    /// <summary>
    /// Adds a ROM image to the machine using a descriptor.
    /// </summary>
    /// <param name="descriptor">The ROM descriptor containing data and load address.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MachineBuilder WithRom(RomDescriptor descriptor)
    {
        romDescriptors.Add(descriptor);
        return this;
    }

    /// <summary>
    /// Adds multiple ROM images to the machine.
    /// </summary>
    /// <param name="descriptors">The ROM descriptors to add.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <remarks>
    /// ROMs are loaded in order; later ROMs overlay earlier ones at overlapping addresses.
    /// </remarks>
    public MachineBuilder WithRoms(params RomDescriptor[] descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            romDescriptors.Add(descriptor);
        }

        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked after the machine is built but before it is returned.
    /// </summary>
    /// <param name="callback">The callback to invoke with the built machine.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Post-build callbacks are invoked in the order they were registered, after:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All components have been added to the machine</description></item>
    /// <item><description>All scheduled devices have been initialized</description></item>
    /// <item><description>All pending slot cards have been installed</description></item>
    /// <item><description>All layers have been activated</description></item>
    /// </list>
    /// <para>
    /// This is useful for extension methods that need to perform additional setup
    /// after the machine is fully assembled, such as registering motherboard devices
    /// with the I/O dispatcher.
    /// </para>
    /// </remarks>
    public MachineBuilder AfterBuild(Action<IMachine> callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        postBuildCallbacks.Add(callback);
        return this;
    }

    /// <summary>
    /// Builds the machine with all configured components.
    /// </summary>
    /// <returns>A fully assembled and initialized <see cref="IMachine"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the machine cannot be built due to missing or invalid configuration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The build process:
    /// </para>
    /// <list type="number">
    /// <item><description>Creates infrastructure (scheduler, signal bus, device registry)</description></item>
    /// <item><description>Creates the memory bus with configured address space</description></item>
    /// <item><description>Applies memory configurations and ROM mappings</description></item>
    /// <item><description>Creates mapping layers and layered mappings</description></item>
    /// <item><description>Creates the CPU</description></item>
    /// <item><description>Assembles the machine</description></item>
    /// <item><description>Initializes all scheduled devices</description></item>
    /// <item><description>Runs post-build callbacks</description></item>
    /// </list>
    /// </remarks>
    public IMachine Build()
    {
        // Create infrastructure
        var scheduler = new Scheduler();
        var signals = new SignalBus();
        var devices = new DeviceRegistry();
        var bus = new MainBus(addressSpaceBits);

        // Apply memory configurations
        foreach (var configure in memoryConfigurations)
        {
            configure(bus, devices);
        }

        // Create and map ROMs
        foreach (var rom in romDescriptors)
        {
            MapRom(bus, devices, rom);
        }

        // Create layers
        foreach (var (name, priority) in layerPriorities)
        {
            bus.CreateLayer(name, priority);
        }

        // Add layered mappings
        foreach (var (layerName, virtualBase, size, target, tag, perms) in layeredMappings)
        {
            var layer = bus.GetLayer(layerName) ?? throw new InvalidOperationException($"Layer '{layerName}' not found.");
            int deviceId = devices.GenerateId();
            devices.Register(deviceId, "LayeredRegion", $"LayeredRegion@{virtualBase:X4}", $"Layer/{layerName}/{virtualBase:X4}");

            var mapping = new LayeredMapping(
                virtualBase,
                size,
                layer,
                deviceId,
                tag,
                perms,
                target.Capabilities,
                target,
                0); // physicalBase

            bus.AddLayeredMapping(mapping);
        }

        // Create event context for CPU
        var tempContext = new EventContext(scheduler, signals, bus);

        // Create CPU
        ICpu cpu = cpuFactory != null
            ? cpuFactory(tempContext)
            : CreateDefaultCpu(cpuFamily, tempContext);

        // Create machine
        var machine = new Machine(cpu, bus, scheduler, signals, devices);

        // Add components
        foreach (var component in components)
        {
            machine.AddComponent(component);
        }

        // Add scheduled devices
        foreach (var device in scheduledDevices)
        {
            machine.AddScheduledDevice(device);
        }

        // Set event context for scheduler
        scheduler.SetEventContext(machine);

        // Initialize devices
        machine.InitializeDevices();

        // Install pending slot cards now that SlotManager is available
        InstallPendingSlotCards(machine);

        // Activate all created layers
        foreach (var (name, _) in layerPriorities)
        {
            bus.ActivateLayer(name);
        }

        // Run post-build callbacks
        foreach (var callback in postBuildCallbacks)
        {
            callback(machine);
        }

        return machine;
    }

    private static void InstallPendingSlotCards(Machine machine)
    {
        // Get the slot manager from the component bag
        var slotManager = machine.GetComponent<ISlotManager>();
        if (slotManager == null)
        {
            return;
        }

        // Find and install all pending slot cards using the interface (no reflection needed)
        foreach (var pendingCard in machine.GetComponents<IPendingSlotCard>())
        {
            slotManager.Install(pendingCard.Slot, pendingCard.Card);
        }
    }

    private static void MapRom(MainBus bus, DeviceRegistry devices, RomDescriptor rom)
    {
        var physical = new PhysicalMemory(rom.Data, rom.Name ?? $"ROM@{rom.LoadAddress:X4}");
        var target = new RomTarget(physical.Slice(0, (uint)rom.Data.Length));

        int deviceId = devices.GenerateId();
        devices.Register(deviceId, "ROM", rom.Name ?? $"ROM@{rom.LoadAddress:X4}", $"ROM/{rom.LoadAddress:X4}");

        bus.MapRegion(
            rom.LoadAddress,
            (uint)rom.Data.Length,
            deviceId,
            RegionTag.Rom,
            PagePerms.ReadExecute,
            target.Capabilities,
            target,
            0);
    }

    private static ICpu CreateDefaultCpu(CpuFamily family, IEventContext context)
    {
        // CPU creation requires a CPU factory to avoid reflection-based coupling.
        // Use WithCpuFactory() to provide a CPU implementation, or use a system-specific
        // extension method like AsPocket2e() which provides its own CPU factory.
        return family switch
        {
            CpuFamily.Cpu65C02 => throw new InvalidOperationException(
                "No CPU factory configured. Use WithCpuFactory() to provide a 65C02 implementation, " +
                "or use AsPocket2e() which includes a CPU factory."),
            CpuFamily.Cpu65C816 => throw new NotSupportedException("65C816 CPU is not yet implemented."),
            CpuFamily.Cpu65832 => throw new NotSupportedException("65832 CPU is not yet implemented."),
            _ => throw new NotSupportedException($"CPU family '{family}' is not supported."),
        };
    }
}