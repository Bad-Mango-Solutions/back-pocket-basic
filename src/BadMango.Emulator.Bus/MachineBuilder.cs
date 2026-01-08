// <copyright file="MachineBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core.Configuration;
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
/// <item><description>Create physical memory blocks and load ROM images</description></item>
/// <item><description>Wire memory regions to bus (map pages)</description></item>
/// <item><description>Create CPU</description></item>
/// <item><description>Create machine</description></item>
/// <item><description>Initialize devices with event context</description></item>
/// </list>
/// <para>
/// Use <see cref="Create(ProfilePathResolver?, Func{IEventContext, ICpu}?)"/> to create
/// a pre-configured builder instance with system services, or use the default constructor
/// for manual configuration.
/// </para>
/// </remarks>
public sealed partial class MachineBuilder
{
    private readonly List<object> components = [];
    private readonly List<IScheduledDevice> scheduledDevices = [];
    private readonly List<RomDescriptor> romDescriptors = [];
    private readonly List<Action<IMemoryBus, IDeviceRegistry>> memoryConfigurations = [];
    private readonly Dictionary<string, int> layerPriorities = new(StringComparer.Ordinal);
    private readonly List<(string LayerName, Addr VirtualBase, Addr Size, IBusTarget Target, RegionTag Tag, PagePerms Perms)> layeredMappings = [];
    private readonly List<ICompositeLayer> compositeLayers = [];
    private readonly List<Action<IMachine>> postBuildCallbacks = [];
    private readonly List<Action<IMachine>> beforeDeviceInitCallbacks = [];
    private readonly List<Action<IMachine>> afterDeviceInitCallbacks = [];
    private readonly List<Action<IMachine>> beforeSlotCardInstallCallbacks = [];
    private readonly List<Action<IMachine>> afterSlotCardInstallCallbacks = [];
    private readonly List<Action<IMachine>> beforeSoftSwitchHandlerRegistrationCallbacks = [];
    private readonly List<Action<IMachine>> afterSoftSwitchHandlerRegistrationCallbacks = [];
    private readonly Dictionary<string, Func<MachineBuilder, IBusTarget>> compositeHandlerFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<MachineBuilder, IMotherboardDevice>> motherboardDeviceFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<MachineBuilder, ISlotCard>> slotCardFactories = new(StringComparer.OrdinalIgnoreCase);

    // Track physical memory instances created from profile
    private readonly Dictionary<string, PhysicalMemory> physicalMemoryBlocks = new(StringComparer.OrdinalIgnoreCase);

    private int addressSpaceBits = 16;
    private CpuFamily cpuFamily = CpuFamily.Cpu65C02;
    private Func<IEventContext, ICpu>? cpuFactory;
    private ProfilePathResolver? profilePathResolver;

    /// <summary>
    /// Creates a new <see cref="MachineBuilder"/> instance with pre-configured system services.
    /// </summary>
    /// <param name="pathResolver">
    /// The path resolver for loading ROM files and other resources. If <see langword="null"/>,
    /// a default resolver with no library root is used.
    /// </param>
    /// <param name="defaultCpuFactory">
    /// The default CPU factory to use when no specific factory is configured. If <see langword="null"/>,
    /// a CPU factory must be provided via <see cref="WithCpuFactory"/> or through system-specific
    /// extension methods like <c>AsPocket2e()</c>.
    /// </param>
    /// <returns>A pre-configured <see cref="MachineBuilder"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// This factory method is the preferred way to create a <see cref="MachineBuilder"/> when
    /// integrating with a dependency injection container. It allows system services to be
    /// injected once at startup rather than passed to each method call.
    /// </para>
    /// <para>
    /// Example usage with Autofac:
    /// </para>
    /// <code>
    /// // In DI registration
    /// builder.Register(ctx =&gt;
    /// {
    ///     var pathResolver = ctx.Resolve&lt;ProfilePathResolver&gt;();
    ///     var cpuFactory = ctx.Resolve&lt;Func&lt;IEventContext, ICpu&gt;&gt;();
    ///     return MachineBuilder.Create(pathResolver, cpuFactory);
    /// }).AsSelf();
    ///
    /// // Usage
    /// var machine = machineBuilder
    ///     .FromProfile(profile)
    ///     .Build();
    /// </code>
    /// </remarks>
    public static MachineBuilder Create(
        ProfilePathResolver? pathResolver = null,
        Func<IEventContext, ICpu>? defaultCpuFactory = null)
    {
        var builder = new MachineBuilder
        {
            profilePathResolver = pathResolver ?? new ProfilePathResolver(null),
            cpuFactory = defaultCpuFactory,
        };

        return builder;
    }

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
    /// Registers a factory function for creating composite region handlers.
    /// </summary>
    /// <param name="handlerName">The name of the handler (e.g., "pocket2e-io").</param>
    /// <param name="factory">A factory function that creates the <see cref="IBusTarget"/> for the handler.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handlerName"/> or <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Composite handlers are used by profiles that specify a "composite" region type with a "handler"
    /// property. When <see cref="FromProfile"/> encounters such a region, it looks up the handler
    /// by name and invokes the factory to create the <see cref="IBusTarget"/>.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// builder.RegisterCompositeHandler("pocket2e-io", b => new Pocket2eIOPage(
    ///     b.GetComponent&lt;IOPageDispatcher&gt;(),
    ///     b.GetComponent&lt;ISlotManager&gt;()));
    /// </code>
    /// </remarks>
    public MachineBuilder RegisterCompositeHandler(string handlerName, Func<MachineBuilder, IBusTarget> factory)
    {
        ArgumentNullException.ThrowIfNull(handlerName, nameof(handlerName));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        compositeHandlerFactories[handlerName] = factory;
        return this;
    }

    /// <summary>
    /// Registers a factory function for creating motherboard devices from profiles.
    /// </summary>
    /// <param name="deviceType">The device type identifier (e.g., "speaker", "keyboard").</param>
    /// <param name="factory">A factory function that creates the <see cref="IMotherboardDevice"/>.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="deviceType"/> or <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Motherboard device factories are used by <see cref="FromProfile"/> when processing the
    /// <c>devices.motherboard</c> section. Each device entry in the profile specifies a type
    /// that is matched against registered factories.
    /// </para>
    /// <para>
    /// If no factory is registered for a device type declared in a profile, the device is
    /// silently skipped. This allows profiles to declare devices that may not be supported
    /// by all hosts.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// builder.RegisterMotherboardDeviceFactory("speaker", _ => new SpeakerController());
    /// </code>
    /// </remarks>
    public MachineBuilder RegisterMotherboardDeviceFactory(string deviceType, Func<MachineBuilder, IMotherboardDevice> factory)
    {
        ArgumentNullException.ThrowIfNull(deviceType, nameof(deviceType));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        motherboardDeviceFactories[deviceType] = factory;
        return this;
    }

    /// <summary>
    /// Registers a factory function for creating slot cards from profiles.
    /// </summary>
    /// <param name="cardType">The card type identifier (e.g., "pocketwatch", "disk-ii-compatible").</param>
    /// <param name="factory">A factory function that creates the <see cref="ISlotCard"/>.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cardType"/> or <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Slot card factories are used by <see cref="FromProfile"/> when processing the
    /// <c>devices.slots.cards</c> section. Each card entry in the profile specifies a type
    /// that is matched against registered factories.
    /// </para>
    /// <para>
    /// If no factory is registered for a card type declared in a profile, the card is
    /// silently skipped. This allows profiles to declare cards that may not be supported
    /// by all hosts.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// builder.RegisterSlotCardFactory("pocketwatch", _ => new PocketWatchCard());
    /// </code>
    /// </remarks>
    public MachineBuilder RegisterSlotCardFactory(string cardType, Func<MachineBuilder, ISlotCard> factory)
    {
        ArgumentNullException.ThrowIfNull(cardType, nameof(cardType));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        slotCardFactories[cardType] = factory;
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
    /// Registers a callback to be invoked after the machine is assembled but before devices are initialized.
    /// </summary>
    /// <param name="callback">The callback to invoke with the built machine.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Before-device-init callbacks are invoked in the order they were registered, after:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Memory bus is created and configured</description></item>
    /// <item><description>ROMs are mapped</description></item>
    /// <item><description>Layers are created and layered mappings added</description></item>
    /// <item><description>CPU is created</description></item>
    /// <item><description>Machine is assembled with all components</description></item>
    /// </list>
    /// <para>
    /// But before:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Devices are initialized</description></item>
    /// <item><description>Slot cards are installed</description></item>
    /// <item><description>Layers are activated</description></item>
    /// </list>
    /// <para>
    /// This is useful for mapping I/O regions that need to be in place before slot cards
    /// are installed and their handlers registered.
    /// </para>
    /// </remarks>
    public MachineBuilder BeforeDeviceInit(Action<IMachine> callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        beforeDeviceInitCallbacks.Add(callback);
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked after scheduled devices are initialized.
    /// </summary>
    /// <param name="callback">The callback to invoke with the built machine.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// After-device-init callbacks are invoked in the order they were registered, after:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All scheduled devices have been initialized</description></item>
    /// </list>
    /// <para>
    /// But before:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Slot cards are installed</description></item>
    /// <item><description>AfterBuild callbacks are invoked</description></item>
    /// </list>
    /// <para>
    /// This is useful for configuration that depends on device initialization being complete,
    /// such as verifying device state or setting up inter-device communication.
    /// </para>
    /// </remarks>
    public MachineBuilder AfterDeviceInit(Action<IMachine> callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        afterDeviceInitCallbacks.Add(callback);
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked before slot cards are installed.
    /// </summary>
    /// <param name="callback">The callback to invoke with the built machine.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Before-slot-card-install callbacks are invoked in the order they were registered, after:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All scheduled devices have been initialized</description></item>
    /// <item><description>AfterDeviceInit callbacks have been invoked</description></item>
    /// </list>
    /// <para>
    /// But before:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Slot cards are installed</description></item>
    /// </list>
    /// <para>
    /// This is useful for verifying slot manager state before cards are installed,
    /// or for setting up preconditions required by slot cards.
    /// </para>
    /// </remarks>
    public MachineBuilder BeforeSlotCardInstall(Action<IMachine> callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        beforeSlotCardInstallCallbacks.Add(callback);
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked after slot cards are installed.
    /// </summary>
    /// <param name="callback">The callback to invoke with the built machine.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// After-slot-card-install callbacks are invoked in the order they were registered, after:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All pending slot cards have been installed</description></item>
    /// </list>
    /// <para>
    /// But before:
    /// </para>
    /// <list type="bullet">
    /// <item><description>AfterBuild callbacks are invoked</description></item>
    /// </list>
    /// <para>
    /// This is useful for verifying slot card installation or performing setup that
    /// depends on all cards being installed, such as card-to-card communication setup.
    /// </para>
    /// </remarks>
    public MachineBuilder AfterSlotCardInstall(Action<IMachine> callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        afterSlotCardInstallCallbacks.Add(callback);
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked before motherboard device soft switch handlers are registered.
    /// </summary>
    /// <param name="callback">The callback to invoke with the built machine.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Before-soft-switch-handler-registration callbacks are invoked in the order they were registered, after:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All scheduled devices have been initialized</description></item>
    /// <item><description>AfterDeviceInit callbacks have been invoked</description></item>
    /// </list>
    /// <para>
    /// But before:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Motherboard device soft switch handlers are registered</description></item>
    /// <item><description>Slot cards are installed</description></item>
    /// </list>
    /// <para>
    /// This is useful for setting up preconditions required by soft switch handlers,
    /// or for registering custom handlers before the automatic registration.
    /// </para>
    /// </remarks>
    public MachineBuilder BeforeSoftSwitchHandlerRegistration(Action<IMachine> callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        beforeSoftSwitchHandlerRegistrationCallbacks.Add(callback);
        return this;
    }

    /// <summary>
    /// Registers a callback to be invoked after motherboard device soft switch handlers are registered.
    /// </summary>
    /// <param name="callback">The callback to invoke with the built machine.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// After-soft-switch-handler-registration callbacks are invoked in the order they were registered, after:
    /// </para>
    /// <list type="bullet">
    /// <item><description>All motherboard device soft switch handlers have been registered</description></item>
    /// </list>
    /// <para>
    /// But before:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Slot cards are installed</description></item>
    /// <item><description>AfterBuild callbacks are invoked</description></item>
    /// </list>
    /// <para>
    /// This is useful for verifying handler registration or performing setup that
    /// depends on all motherboard soft switches being available.
    /// </para>
    /// </remarks>
    public MachineBuilder AfterSoftSwitchHandlerRegistration(Action<IMachine> callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));
        afterSoftSwitchHandlerRegistrationCallbacks.Add(callback);
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
    /// The build process and callback order:
    /// </para>
    /// <list type="number">
    /// <item><description>Creates infrastructure (scheduler, signal bus, device registry)</description></item>
    /// <item><description>Creates the memory bus with configured address space</description></item>
    /// <item><description>Applies memory configurations and ROM mappings</description></item>
    /// <item><description>Creates mapping layers and layered mappings</description></item>
    /// <item><description>Creates the CPU</description></item>
    /// <item><description>Assembles the machine with all components</description></item>
    /// <item><description>Runs <see cref="BeforeDeviceInit"/> callbacks</description></item>
    /// <item><description>Initializes all scheduled devices</description></item>
    /// <item><description>Runs <see cref="AfterDeviceInit"/> callbacks</description></item>
    /// <item><description>Runs <see cref="BeforeSoftSwitchHandlerRegistration"/> callbacks</description></item>
    /// <item><description>Registers motherboard device soft switch handlers</description></item>
    /// <item><description>Runs <see cref="AfterSoftSwitchHandlerRegistration"/> callbacks</description></item>
    /// <item><description>Runs <see cref="BeforeSlotCardInstall"/> callbacks</description></item>
    /// <item><description>Installs pending slot cards</description></item>
    /// <item><description>Runs <see cref="AfterSlotCardInstall"/> callbacks</description></item>
    /// <item><description>Runs <see cref="AfterBuild"/> callbacks</description></item>
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

        // Create and map ROMs (for programmatic usage, not profile-based)
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

        // Run before-device-init callbacks (e.g., I/O page mapping)
        foreach (var callback in beforeDeviceInitCallbacks)
        {
            callback(machine);
        }

        // Initialize devices
        machine.InitializeDevices();

        // Run after-device-init callbacks
        foreach (var callback in afterDeviceInitCallbacks)
        {
            callback(machine);
        }

        // Run before-soft-switch-handler-registration callbacks
        foreach (var callback in beforeSoftSwitchHandlerRegistrationCallbacks)
        {
            callback(machine);
        }

        // Register soft switch handlers for all motherboard devices
        RegisterMotherboardDeviceHandlers(machine);

        // Run after-soft-switch-handler-registration callbacks
        foreach (var callback in afterSoftSwitchHandlerRegistrationCallbacks)
        {
            callback(machine);
        }

        // Run before-slot-card-install callbacks
        foreach (var callback in beforeSlotCardInstallCallbacks)
        {
            callback(machine);
        }

        // Install pending slot cards now that SlotManager is available
        InstallPendingSlotCards(machine);

        // Run after-slot-card-install callbacks
        foreach (var callback in afterSlotCardInstallCallbacks)
        {
            callback(machine);
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

    private static void RegisterMotherboardDeviceHandlers(Machine machine)
    {
        // Get the I/O page dispatcher from the component bag
        var dispatcher = machine.GetComponent<IOPageDispatcher>();
        if (dispatcher == null)
        {
            return;
        }

        // Find and register handlers for all motherboard devices
        foreach (var device in machine.GetComponents<IMotherboardDevice>())
        {
            device.RegisterHandlers(dispatcher);
        }
    }

    private static void MapRom(MainBus bus, DeviceRegistry devices, RomDescriptor rom)
    {
        var physical = new PhysicalMemory(rom.Data, rom.Name ?? $"ROM@{rom.LoadAddress:X4}");

        // Use Slice (writable) instead of ReadOnlySlice to allow debug writes (Poke8) to ROM
        var target = new RomTarget(physical.Slice(0, (uint)rom.Data.Length), physical.Name);

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