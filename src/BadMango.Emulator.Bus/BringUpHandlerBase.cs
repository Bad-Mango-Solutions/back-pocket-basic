// <copyright file="BringUpHandlerBase.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Abstract base class for machine bring-up handlers providing common functionality.
/// </summary>
/// <remarks>
/// <para>
/// This base class provides common bring-up logic that can be reused across different
/// machine types, including:
/// <list type="bullet">
/// <item><description>Standard RAM/ROM validation against machine constants</description></item>
/// <item><description>Physical memory pool allocation</description></item>
/// <item><description>Region creation helpers</description></item>
/// <item><description>ROM loading into physical memory</description></item>
/// </list>
/// </para>
/// <para>
/// Derived classes implement machine-specific logic such as memory layout, bank
/// switching configuration, and device initialization.
/// </para>
/// </remarks>
public abstract class BringUpHandlerBase : IBringUpHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BringUpHandlerBase"/> class.
    /// </summary>
    /// <param name="constants">The machine constants for this handler.</param>
    protected BringUpHandlerBase(IMachineConstants constants)
    {
        ArgumentNullException.ThrowIfNull(constants);
        Constants = constants;
    }

    /// <inheritdoc />
    public IMachineConstants Constants { get; }

    /// <inheritdoc />
    public virtual BringUpValidationResult Validate(IProvisioningBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate RAM size
        if (bundle.RequestedRamSize < Constants.MinRamSize)
        {
            errors.Add($"Requested RAM size {bundle.RequestedRamSize} bytes is below minimum {Constants.MinRamSize} bytes for {Constants.MachineName}.");
        }

        if (bundle.RequestedRamSize > Constants.MaxRamSize)
        {
            errors.Add($"Requested RAM size {bundle.RequestedRamSize} bytes exceeds maximum {Constants.MaxRamSize} bytes for {Constants.MachineName}.");
        }

        // Validate boot ROM
        if (bundle.RomImages.TryGetValue("boot", out var bootRom))
        {
            if ((uint)bootRom.Length != Constants.BootRomSize)
            {
                errors.Add($"Boot ROM size {bootRom.Length} bytes does not match expected size {Constants.BootRomSize} bytes for {Constants.MachineName}.");
            }
        }
        else
        {
            warnings.Add("No boot ROM provided. Machine may not boot correctly.");
        }

        // Run machine-specific validation
        ValidateMachineSpecific(bundle, errors, warnings);

        if (errors.Count > 0)
        {
            return BringUpValidationResult.Invalid(errors);
        }

        if (warnings.Count > 0)
        {
            return BringUpValidationResult.ValidWithWarnings(warnings);
        }

        return BringUpValidationResult.Valid();
    }

    /// <inheritdoc />
    public virtual IBringUpResult BringUp(IProvisioningBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        try
        {
            // Create the components
            var regionManager = new RegionManager();
            var deviceRegistry = new DeviceRegistry();
            var physicalMemoryPools = new Dictionary<string, IPhysicalMemory>();

            // Allocate main RAM
            var mainRam = AllocateMainRam(bundle);
            physicalMemoryPools["main_ram"] = mainRam;

            // Allocate and load boot ROM
            if (bundle.RomImages.TryGetValue("boot", out var bootRomData))
            {
                var bootRom = AllocateAndLoadRom("boot_rom", bootRomData);
                physicalMemoryPools["boot_rom"] = bootRom;
            }

            // Allow derived classes to allocate additional memory pools
            AllocateAdditionalPools(bundle, physicalMemoryPools);

            // Create and map regions
            CreateAndMapRegions(bundle, regionManager, physicalMemoryPools);

            // Initialize devices
            InitializeDevices(bundle, deviceRegistry, regionManager);

            // Build the page table
            // Note: This requires an IMemoryBus which would be created by the caller
            // The region manager holds all the mapping information needed
            return BringUpResult.Succeeded(
                regionManager,
                physicalMemoryPools,
                deviceRegistry,
                Constants.BootRomBase,
                Constants);
        }
        catch (Exception ex)
        {
            return BringUpResult.Failed($"Bring-up failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a RAM region from a physical memory pool.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <param name="name">The region name.</param>
    /// <param name="baseAddress">The base address for mapping.</param>
    /// <param name="physicalMemory">The physical memory backing the region.</param>
    /// <param name="priority">The mapping priority.</param>
    /// <returns>A configured RAM region.</returns>
    protected static IMemoryRegion CreateRamRegion(
        string id,
        string name,
        Addr baseAddress,
        IPhysicalMemory physicalMemory,
        int priority = 100)
    {
        return MemoryRegion.CreateRam(id, name, baseAddress, physicalMemory, priority);
    }

    /// <summary>
    /// Creates a ROM region from a physical memory pool.
    /// </summary>
    /// <param name="id">The region identifier.</param>
    /// <param name="name">The region name.</param>
    /// <param name="baseAddress">The base address for mapping.</param>
    /// <param name="physicalMemory">The physical memory backing the region.</param>
    /// <param name="priority">The mapping priority.</param>
    /// <returns>A configured ROM region.</returns>
    protected static IMemoryRegion CreateRomRegion(
        string id,
        string name,
        Addr baseAddress,
        IPhysicalMemory physicalMemory,
        int priority = 0)
    {
        return MemoryRegion.CreateRom(id, name, baseAddress, physicalMemory, priority);
    }

    /// <summary>
    /// Performs machine-specific validation on the provisioning bundle.
    /// </summary>
    /// <param name="bundle">The provisioning bundle to validate.</param>
    /// <param name="errors">Collection to add error messages to.</param>
    /// <param name="warnings">Collection to add warning messages to.</param>
    protected virtual void ValidateMachineSpecific(
        IProvisioningBundle bundle,
        List<string> errors,
        List<string> warnings)
    {
        // Override in derived classes for machine-specific validation
    }

    /// <summary>
    /// Allocates the main RAM pool for the machine.
    /// </summary>
    /// <param name="bundle">The provisioning bundle.</param>
    /// <returns>The allocated physical memory for main RAM.</returns>
    protected virtual IPhysicalMemory AllocateMainRam(IProvisioningBundle bundle)
    {
        var ramSize = bundle.RequestedRamSize > 0
            ? bundle.RequestedRamSize
            : Constants.DefaultRamSize;

        return new PhysicalMemory(ramSize, "Main RAM");
    }

    /// <summary>
    /// Allocates physical memory and loads ROM data into it.
    /// </summary>
    /// <param name="name">The name for the ROM pool.</param>
    /// <param name="romData">The ROM binary data.</param>
    /// <returns>The physical memory containing the ROM data.</returns>
    protected virtual IPhysicalMemory AllocateAndLoadRom(string name, ReadOnlyMemory<byte> romData)
    {
        var rom = new PhysicalMemory((uint)romData.Length, name);

        // Load ROM data using debug privilege
        var debugPrivilege = new DebugPrivilege();
        rom.WritePhysical(debugPrivilege, 0, romData.Span);

        return rom;
    }

    /// <summary>
    /// Allocates additional memory pools specific to the machine type.
    /// </summary>
    /// <param name="bundle">The provisioning bundle.</param>
    /// <param name="pools">Dictionary to add pools to.</param>
    protected virtual void AllocateAdditionalPools(
        IProvisioningBundle bundle,
        Dictionary<string, IPhysicalMemory> pools)
    {
        // Override in derived classes to allocate auxiliary RAM, video RAM, etc.
    }

    /// <summary>
    /// Creates memory regions and maps them into the address space.
    /// </summary>
    /// <param name="bundle">The provisioning bundle.</param>
    /// <param name="regionManager">The region manager to configure.</param>
    /// <param name="pools">The allocated physical memory pools.</param>
    protected abstract void CreateAndMapRegions(
        IProvisioningBundle bundle,
        IRegionManager regionManager,
        IReadOnlyDictionary<string, IPhysicalMemory> pools);

    /// <summary>
    /// Initializes devices and their memory-mapped regions.
    /// </summary>
    /// <param name="bundle">The provisioning bundle.</param>
    /// <param name="deviceRegistry">The device registry to configure.</param>
    /// <param name="regionManager">The region manager for memory-mapped I/O.</param>
    protected virtual void InitializeDevices(
        IProvisioningBundle bundle,
        IDeviceRegistry deviceRegistry,
        IRegionManager regionManager)
    {
        // Override in derived classes to initialize machine-specific devices
    }
}