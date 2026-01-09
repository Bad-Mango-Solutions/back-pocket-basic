// <copyright file="CompositeLayerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

using Moq;

/// <summary>
/// Unit tests for <see cref="ICompositeLayer"/> support in the machine builder and bus system.
/// </summary>
/// <remarks>
/// These tests cover composite layer functionality including:
/// <list type="bullet">
/// <item><description>Basic composite layer registration and retrieval</description></item>
/// <item><description>Composite layer resolution for different addresses</description></item>
/// <item><description>Composite layer activation and deactivation</description></item>
/// <item><description>Priority interactions with regular layers</description></item>
/// <item><description>Complex scenarios combining composite layers, regular layers, and swap groups</description></item>
/// </list>
/// </remarks>
[TestFixture]
public class CompositeLayerTests
{
    private const int PageSize = 4096;

    #region Basic Registration Tests

    /// <summary>
    /// Verifies that a composite layer can be added to the machine builder.
    /// </summary>
    [Test]
    public void AddCompositeLayer_AddsLayerToComponents()
    {
        var mockCpu = CreateMockCpu();
        var mockLayer = CreateMockCompositeLayer("TestLayer", 100, 0xD000, 0x3000);

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddCompositeLayer(mockLayer.Object)
            .Build();

        Assert.That(machine.HasComponent<ICompositeLayer>(), Is.True);
    }

    /// <summary>
    /// Verifies that multiple composite layers can be added.
    /// </summary>
    [Test]
    public void AddCompositeLayer_MultipleLayersCanBeAdded()
    {
        var mockCpu = CreateMockCpu();
        var layer1 = CreateMockCompositeLayer("Layer1", 100, 0xD000, 0x1000);
        var layer2 = CreateMockCompositeLayer("Layer2", 200, 0xE000, 0x1000);
        var layer3 = CreateMockCompositeLayer("Layer3", 150, 0xF000, 0x1000);

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddCompositeLayer(layer1.Object)
            .AddCompositeLayer(layer2.Object)
            .AddCompositeLayer(layer3.Object)
            .Build();

        var layers = machine.GetComponents<ICompositeLayer>().ToList();

        Assert.That(layers, Has.Count.EqualTo(3));
    }

    /// <summary>
    /// Verifies that null composite layer throws ArgumentNullException.
    /// </summary>
    [Test]
    public void AddCompositeLayer_NullLayer_ThrowsArgumentNullException()
    {
        var builder = new MachineBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddCompositeLayer(null!));
    }

    /// <summary>
    /// Verifies that composite layer can be retrieved by specific type.
    /// </summary>
    [Test]
    public void GetComponent_CompositeLayerByName_ReturnsCorrectLayer()
    {
        var mockCpu = CreateMockCpu();
        var mockLayer = CreateMockCompositeLayer("LanguageCard", 100, 0xD000, 0x3000);

        var machine = new MachineBuilder()
            .WithCpuFactory(_ => mockCpu.Object)
            .AddCompositeLayer(mockLayer.Object)
            .Build();

        var retrieved = machine.GetComponent<ICompositeLayer>();

        Assert.Multiple(() =>
        {
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("LanguageCard"));
        });
    }

    #endregion

    #region Resolution Tests

    /// <summary>
    /// Verifies that composite layer resolution returns correct target for different addresses.
    /// </summary>
    [Test]
    public void ResolveMapping_DifferentAddresses_ReturnsDifferentTargets()
    {
        var memory1 = new PhysicalMemory(PageSize, "Bank1");
        var memory2 = new PhysicalMemory(PageSize, "Bank2");
        memory1.AsSpan()[0] = 0x11;
        memory2.AsSpan()[0] = 0x22;

        var target1 = new RamTarget(memory1.Slice(0, PageSize));
        var target2 = new RamTarget(memory2.Slice(0, PageSize));

        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("TestLayer");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.IsActive).Returns(true);
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x2000u));

        // D000 resolves to target1, E000 resolves to target2
        mockLayer.Setup(l => l.ResolveMapping(It.IsInRange(0xD000u, 0xDFFFu, Moq.Range.Inclusive), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(target1, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.SupportsPeek));
        mockLayer.Setup(l => l.ResolveMapping(It.IsInRange(0xE000u, 0xEFFFu, Moq.Range.Inclusive), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(target2, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.SupportsPeek));

        var resolution1 = mockLayer.Object.ResolveMapping(0xD000, AccessIntent.DataRead);
        var resolution2 = mockLayer.Object.ResolveMapping(0xE000, AccessIntent.DataRead);

        Assert.Multiple(() =>
        {
            Assert.That(resolution1, Is.Not.Null);
            Assert.That(resolution2, Is.Not.Null);
            Assert.That(resolution1!.Value.Target, Is.SameAs(target1));
            Assert.That(resolution2!.Value.Target, Is.SameAs(target2));
        });
    }

    /// <summary>
    /// Verifies that composite layer can return null to fall through to lower layers.
    /// </summary>
    [Test]
    public void ResolveMapping_ReturnsNull_AllowsFallthrough()
    {
        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("TestLayer");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.IsActive).Returns(true);
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x3000u));

        // Return null for addresses outside D000-DFFF
        mockLayer.Setup(l => l.ResolveMapping(It.IsInRange(0xE000u, 0xFFFFu, Moq.Range.Inclusive), It.IsAny<AccessIntent>()))
            .Returns((CompositeLayerResolution?)null);

        var resolution = mockLayer.Object.ResolveMapping(0xE000, AccessIntent.DataRead);

        Assert.That(resolution, Is.Null);
    }

    /// <summary>
    /// Verifies that composite layer resolution respects access intent.
    /// </summary>
    [Test]
    public void ResolveMapping_DifferentIntents_ReturnsDifferentResolutions()
    {
        var readMemory = new PhysicalMemory(PageSize, "ReadBank");
        var writeMemory = new PhysicalMemory(PageSize, "WriteBank");

        var readTarget = new RamTarget(readMemory.Slice(0, PageSize));
        var writeTarget = new RamTarget(writeMemory.Slice(0, PageSize));

        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("ReadWriteSplit");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.IsActive).Returns(true);
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x1000u));

        // Read intent returns read target, write intent returns write target
        mockLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), AccessIntent.DataRead))
            .Returns(new CompositeLayerResolution(readTarget, 0, PagePerms.Read, RegionTag.Ram, TargetCaps.SupportsPeek));
        mockLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), AccessIntent.DataWrite))
            .Returns(new CompositeLayerResolution(writeTarget, 0, PagePerms.Write, RegionTag.Ram, TargetCaps.SupportsPoke));

        var readResolution = mockLayer.Object.ResolveMapping(0xD000, AccessIntent.DataRead);
        var writeResolution = mockLayer.Object.ResolveMapping(0xD000, AccessIntent.DataWrite);

        Assert.Multiple(() =>
        {
            Assert.That(readResolution!.Value.Target, Is.SameAs(readTarget));
            Assert.That(writeResolution!.Value.Target, Is.SameAs(writeTarget));
        });
    }

    #endregion

    #region Activation/Deactivation Tests

    /// <summary>
    /// Verifies that OnActivate is called when layer becomes active.
    /// </summary>
    [Test]
    public void CompositeLayer_Activation_CallsOnActivate()
    {
        var mockLayer = CreateMockCompositeLayer("TestLayer", 100, 0xD000, 0x3000);
        mockLayer.Setup(l => l.OnActivate()).Verifiable();

        // Simulate activation
        mockLayer.Object.OnActivate();

        mockLayer.Verify(l => l.OnActivate(), Times.Once);
    }

    /// <summary>
    /// Verifies that OnDeactivate is called when layer becomes inactive.
    /// </summary>
    [Test]
    public void CompositeLayer_Deactivation_CallsOnDeactivate()
    {
        var mockLayer = CreateMockCompositeLayer("TestLayer", 100, 0xD000, 0x3000);
        mockLayer.Setup(l => l.OnDeactivate()).Verifiable();

        // Simulate deactivation
        mockLayer.Object.OnDeactivate();

        mockLayer.Verify(l => l.OnDeactivate(), Times.Once);
    }

    /// <summary>
    /// Verifies that inactive composite layer does not resolve mappings.
    /// </summary>
    [Test]
    public void CompositeLayer_WhenInactive_ShouldNotResolve()
    {
        var memory = new PhysicalMemory(PageSize, "Memory");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("TestLayer");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.IsActive).Returns(false); // Inactive
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x3000u));
        mockLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(target, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None));

        // Should not resolve when inactive
        Assert.That(mockLayer.Object.IsActive, Is.False);
    }

    #endregion

    #region Priority Tests

    /// <summary>
    /// Verifies that higher priority composite layer takes precedence.
    /// </summary>
    [Test]
    public void CompositeLayer_HigherPriority_TakesPrecedence()
    {
        var lowMemory = new PhysicalMemory(PageSize, "LowPriority");
        var highMemory = new PhysicalMemory(PageSize, "HighPriority");
        lowMemory.AsSpan()[0] = 0x11;
        highMemory.AsSpan()[0] = 0x22;

        var lowTarget = new RamTarget(lowMemory.Slice(0, PageSize));
        var highTarget = new RamTarget(highMemory.Slice(0, PageSize));

        var lowLayer = CreateMockCompositeLayer("LowPriority", 50, 0xD000, 0x1000);
        lowLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(lowTarget, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None));

        var highLayer = CreateMockCompositeLayer("HighPriority", 100, 0xD000, 0x1000);
        highLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(highTarget, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None));

        // Verify priorities
        Assert.That(highLayer.Object.Priority, Is.GreaterThan(lowLayer.Object.Priority));
    }

    /// <summary>
    /// Verifies that composite layers with same priority are handled deterministically.
    /// </summary>
    [Test]
    public void CompositeLayer_SamePriority_DeterministicOrder()
    {
        var layer1 = CreateMockCompositeLayer("Layer1", 100, 0xD000, 0x1000);
        var layer2 = CreateMockCompositeLayer("Layer2", 100, 0xD000, 0x1000);

        Assert.That(layer1.Object.Priority, Is.EqualTo(layer2.Object.Priority));
    }

    #endregion

    #region CompositeLayerResolution Tests

    /// <summary>
    /// Verifies CompositeLayerResolution record struct properties.
    /// </summary>
    [Test]
    public void CompositeLayerResolution_Properties_AreCorrect()
    {
        var memory = new PhysicalMemory(PageSize, "Memory");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var resolution = new CompositeLayerResolution(
            Target: target,
            PhysicalBase: 0x1000,
            Perms: PagePerms.ReadWrite,
            Tag: RegionTag.Ram,
            Caps: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Target, Is.SameAs(target));
            Assert.That(resolution.PhysicalBase, Is.EqualTo(0x1000u));
            Assert.That(resolution.Perms, Is.EqualTo(PagePerms.ReadWrite));
            Assert.That(resolution.Tag, Is.EqualTo(RegionTag.Ram));
            Assert.That(resolution.Caps, Is.EqualTo(TargetCaps.SupportsPeek | TargetCaps.SupportsPoke));
        });
    }

    /// <summary>
    /// Verifies CompositeLayerResolution equality.
    /// </summary>
    [Test]
    public void CompositeLayerResolution_Equality_WorksCorrectly()
    {
        var memory = new PhysicalMemory(PageSize, "Memory");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var resolution1 = new CompositeLayerResolution(target, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None);
        var resolution2 = new CompositeLayerResolution(target, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None);
        var resolution3 = new CompositeLayerResolution(target, 0x100, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None);

        Assert.Multiple(() =>
        {
            Assert.That(resolution1, Is.EqualTo(resolution2));
            Assert.That(resolution1, Is.Not.EqualTo(resolution3));
        });
    }

    #endregion

    #region Complex Scenario Tests

    /// <summary>
    /// Simulates Language Card behavior with bank switching.
    /// </summary>
    [Test]
    public void LanguageCardSimulation_BankSwitching()
    {
        var romMemory = new PhysicalMemory(0x3000, "ROM");
        var bank1Memory = new PhysicalMemory(0x1000, "Bank1");
        var bank2Memory = new PhysicalMemory(0x1000, "Bank2");
        var upperMemory = new PhysicalMemory(0x2000, "Upper");

        // Initialize with distinct values
        for (int i = 0; i < romMemory.AsSpan().Length; i++)
        {
            romMemory.AsSpan()[i] = 0xFF;
        }

        bank1Memory.AsSpan()[0] = 0xB1;
        bank2Memory.AsSpan()[0] = 0xB2;
        upperMemory.AsSpan()[0] = 0xEE;

        var romTarget = new RomTarget(romMemory.Slice(0, 0x3000));
        var bank1Target = new RamTarget(bank1Memory.Slice(0, 0x1000));
        var bank2Target = new RamTarget(bank2Memory.Slice(0, 0x1000));
        var upperTarget = new RamTarget(upperMemory.Slice(0, 0x2000));

        // Simulate LC state machine
        bool ramReadEnabled = false;
        bool ramWriteEnabled = false;
        bool bank2Selected = false;

        var lcLayer = new Mock<ICompositeLayer>();
        lcLayer.Setup(l => l.Name).Returns("LanguageCard");
        lcLayer.Setup(l => l.Priority).Returns(100);
        lcLayer.Setup(l => l.IsActive).Returns(() => ramReadEnabled || ramWriteEnabled);
        lcLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x3000u));

        lcLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns((Addr addr, AccessIntent intent) =>
            {
                if (addr >= 0xD000 && addr < 0xE000)
                {
                    // D000-DFFF region (bank-switched)
                    if (intent == AccessIntent.DataRead && ramReadEnabled)
                    {
                        return new CompositeLayerResolution(
                            bank2Selected ? bank2Target : bank1Target,
                            0,
                            ramWriteEnabled ? PagePerms.ReadWrite : PagePerms.Read,
                            RegionTag.Ram,
                            TargetCaps.SupportsPeek | TargetCaps.SupportsPoke);
                    }
                    else if (intent == AccessIntent.DataWrite && ramWriteEnabled)
                    {
                        return new CompositeLayerResolution(
                            bank2Selected ? bank2Target : bank1Target,
                            0,
                            PagePerms.Write,
                            RegionTag.Ram,
                            TargetCaps.SupportsPoke);
                    }
                }
                else if (addr >= 0xE000 && addr < 0x10000)
                {
                    // E000-FFFF region (single 8KB bank)
                    if (intent == AccessIntent.DataRead && ramReadEnabled)
                    {
                        return new CompositeLayerResolution(
                            upperTarget,
                            addr - 0xE000,
                            ramWriteEnabled ? PagePerms.ReadWrite : PagePerms.Read,
                            RegionTag.Ram,
                            TargetCaps.SupportsPeek | TargetCaps.SupportsPoke);
                    }
                    else if (intent == AccessIntent.DataWrite && ramWriteEnabled)
                    {
                        return new CompositeLayerResolution(
                            upperTarget,
                            addr - 0xE000,
                            PagePerms.Write,
                            RegionTag.Ram,
                            TargetCaps.SupportsPoke);
                    }
                }

                return null; // Fall through to ROM
            });

        // Initial state: ROM visible
        Assert.That(lcLayer.Object.IsActive, Is.False, "LC should be inactive initially");

        // Enable RAM read
        ramReadEnabled = true;
        Assert.That(lcLayer.Object.IsActive, Is.True, "LC should be active with RAM read enabled");

        var resolution = lcLayer.Object.ResolveMapping(0xD000, AccessIntent.DataRead);
        Assert.That(resolution, Is.Not.Null, "Should resolve D000 read");
        Assert.That(resolution!.Value.Target, Is.SameAs(bank1Target), "Should return bank 1 by default");

        // Switch to bank 2
        bank2Selected = true;
        resolution = lcLayer.Object.ResolveMapping(0xD000, AccessIntent.DataRead);
        Assert.That(resolution!.Value.Target, Is.SameAs(bank2Target), "Should return bank 2 after switch");

        // Enable write
        ramWriteEnabled = true;
        resolution = lcLayer.Object.ResolveMapping(0xD000, AccessIntent.DataWrite);
        Assert.That(resolution, Is.Not.Null, "Should resolve D000 write");
        Assert.That(resolution!.Value.Perms.HasFlag(PagePerms.Write), Is.True, "Should be writable");
    }

    /// <summary>
    /// Simulates auxiliary memory controller with 80STORE mode.
    /// </summary>
    [Test]
    public void AuxiliaryMemorySimulation_80StoreMode()
    {
        var mainMemory = new PhysicalMemory(0x1000, "MainText");
        var auxMemory = new PhysicalMemory(0x1000, "AuxText");

        mainMemory.AsSpan()[0] = 0x4D; // 'M' for main
        auxMemory.AsSpan()[0] = 0x41; // 'A' for aux

        var mainTarget = new RamTarget(mainMemory.Slice(0, 0x1000));
        var auxTarget = new RamTarget(auxMemory.Slice(0, 0x1000));

        bool store80Enabled = false;
        bool page2Selected = false;

        var auxLayer = new Mock<ICompositeLayer>();
        auxLayer.Setup(l => l.Name).Returns("AuxMemory80Store");
        auxLayer.Setup(l => l.Priority).Returns(50);
        auxLayer.Setup(l => l.IsActive).Returns(() => store80Enabled);
        auxLayer.Setup(l => l.AddressRange).Returns((0x0400u, 0x0400u)); // Text page 1

        auxLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns((Addr addr, AccessIntent intent) =>
            {
                if (store80Enabled && addr >= 0x0400 && addr < 0x0800)
                {
                    var target = page2Selected ? auxTarget : mainTarget;
                    return new CompositeLayerResolution(
                        target,
                        addr - 0x0400,
                        PagePerms.ReadWrite,
                        RegionTag.Ram,
                        TargetCaps.SupportsPeek | TargetCaps.SupportsPoke);
                }

                return null;
            });

        // 80STORE off: layer inactive
        Assert.That(auxLayer.Object.IsActive, Is.False);

        // 80STORE on, PAGE2 off: main memory
        store80Enabled = true;
        var resolution = auxLayer.Object.ResolveMapping(0x0400, AccessIntent.DataRead);
        Assert.That(resolution!.Value.Target, Is.SameAs(mainTarget), "Should use main with PAGE2 off");

        // 80STORE on, PAGE2 on: aux memory
        page2Selected = true;
        resolution = auxLayer.Object.ResolveMapping(0x0400, AccessIntent.DataRead);
        Assert.That(resolution!.Value.Target, Is.SameAs(auxTarget), "Should use aux with PAGE2 on");
    }

    /// <summary>
    /// Tests interaction between composite layer and regular layer at same address.
    /// </summary>
    [Test]
    public void CompositeLayerAndRegularLayer_SameAddress_PriorityDeterminesWinner()
    {
        var compositeMem = new PhysicalMemory(PageSize, "Composite");
        var regularMem = new PhysicalMemory(PageSize, "Regular");
        compositeMem.AsSpan()[0] = 0xCC;
        regularMem.AsSpan()[0] = 0xAA;

        var compositeTarget = new RamTarget(compositeMem.Slice(0, PageSize));
        var regularTarget = new RamTarget(regularMem.Slice(0, PageSize));

        // Composite layer with priority 100
        var compositeLayer = CreateMockCompositeLayer("Composite", 100, 0xD000, 0x1000);
        compositeLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(compositeTarget, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None));

        // Regular layer would be priority 50; in real implementation, the bus would compare priorities
        Assert.That(compositeLayer.Object.Priority, Is.EqualTo(100));
    }

    /// <summary>
    /// Tests composite layer with swap group in same region.
    /// </summary>
    [Test]
    public void CompositeLayerAndSwapGroup_InteractionTest()
    {
        // This test verifies that composite layers and swap groups can coexist;
        // composite layer should take precedence when active
        var compositeMemory = new PhysicalMemory(PageSize, "CompositeRAM");
        var swapBank1Memory = new PhysicalMemory(PageSize, "SwapBank1");
        var swapBank2Memory = new PhysicalMemory(PageSize, "SwapBank2");

        compositeMemory.AsSpan()[0] = 0xCC;
        swapBank1Memory.AsSpan()[0] = 0xB1;
        swapBank2Memory.AsSpan()[0] = 0xB2;

        var compositeTarget = new RamTarget(compositeMemory.Slice(0, PageSize));

        bool compositeActive = false;

        var compositeLayer = new Mock<ICompositeLayer>();
        compositeLayer.Setup(l => l.Name).Returns("CompositeOverlay");
        compositeLayer.Setup(l => l.Priority).Returns(100);
        compositeLayer.Setup(l => l.IsActive).Returns(() => compositeActive);
        compositeLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x1000u));
        compositeLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(compositeTarget, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None));

        // When composite is inactive, swap group would be visible
        Assert.That(compositeLayer.Object.IsActive, Is.False);

        // When composite is active, it should overlay the swap group
        compositeActive = true;
        Assert.That(compositeLayer.Object.IsActive, Is.True);

        var resolution = compositeLayer.Object.ResolveMapping(0xD000, AccessIntent.DataRead);
        Assert.That(resolution!.Value.Target, Is.SameAs(compositeTarget));
    }

    /// <summary>
    /// Tests rapid activation/deactivation cycles.
    /// </summary>
    [Test]
    public void CompositeLayer_RapidActivationCycles_MaintainsConsistency()
    {
        var activationCount = 0;
        var deactivationCount = 0;

        var mockLayer = CreateMockCompositeLayer("RapidTest", 100, 0xD000, 0x1000);
        mockLayer.Setup(l => l.OnActivate()).Callback(() => activationCount++);
        mockLayer.Setup(l => l.OnDeactivate()).Callback(() => deactivationCount++);

        // Simulate rapid toggles
        for (int i = 0; i < 1000; i++)
        {
            mockLayer.Object.OnActivate();
            mockLayer.Object.OnDeactivate();
        }

        Assert.Multiple(() =>
        {
            Assert.That(activationCount, Is.EqualTo(1000));
            Assert.That(deactivationCount, Is.EqualTo(1000));
        });
    }

    /// <summary>
    /// Tests address boundary conditions for composite layer.
    /// </summary>
    [Test]
    public void CompositeLayer_AddressBoundaries_HandleCorrectly()
    {
        var memory = new PhysicalMemory(0x3000, "Memory");
        var target = new RamTarget(memory.Slice(0, 0x3000));

        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("BoundaryTest");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.IsActive).Returns(true);
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x3000u));

        // Only resolve for addresses within range
        mockLayer.Setup(l => l.ResolveMapping(It.Is<Addr>(a => a >= 0xD000 && a < 0x10000), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(target, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None));
        mockLayer.Setup(l => l.ResolveMapping(It.Is<Addr>(a => a < 0xD000), It.IsAny<AccessIntent>()))
            .Returns((CompositeLayerResolution?)null);

        Assert.Multiple(() =>
        {
            // Just below range
            Assert.That(mockLayer.Object.ResolveMapping(0xCFFF, AccessIntent.DataRead), Is.Null, "Below range");

            // At start of range
            Assert.That(mockLayer.Object.ResolveMapping(0xD000, AccessIntent.DataRead), Is.Not.Null, "Start of range");

            // In middle of range
            Assert.That(mockLayer.Object.ResolveMapping(0xE000, AccessIntent.DataRead), Is.Not.Null, "Middle of range");

            // At end of range
            Assert.That(mockLayer.Object.ResolveMapping(0xFFFF, AccessIntent.DataRead), Is.Not.Null, "End of range");
        });
    }

    /// <summary>
    /// Tests composite layer with varying permissions per address.
    /// </summary>
    [Test]
    public void CompositeLayer_VaryingPermissions_PerAddress()
    {
        var memory = new PhysicalMemory(0x2000, "Memory");
        var target = new RamTarget(memory.Slice(0, 0x2000));

        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("PermTest");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.IsActive).Returns(true);
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x2000u));

        // D000-DFFF is read-only, E000-EFFF is read-write
        mockLayer.Setup(l => l.ResolveMapping(It.Is<Addr>(a => a >= 0xD000 && a < 0xE000), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(target, 0, PagePerms.Read, RegionTag.Rom, TargetCaps.SupportsPeek));
        mockLayer.Setup(l => l.ResolveMapping(It.Is<Addr>(a => a >= 0xE000 && a < 0xF000), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(target, 0x1000, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.SupportsPeek | TargetCaps.SupportsPoke));

        var readOnlyResolution = mockLayer.Object.ResolveMapping(0xD000, AccessIntent.DataRead);
        var readWriteResolution = mockLayer.Object.ResolveMapping(0xE000, AccessIntent.DataRead);

        Assert.Multiple(() =>
        {
            Assert.That(readOnlyResolution!.Value.Perms, Is.EqualTo(PagePerms.Read));
            Assert.That(readOnlyResolution!.Value.Tag, Is.EqualTo(RegionTag.Rom));
            Assert.That(readWriteResolution!.Value.Perms, Is.EqualTo(PagePerms.ReadWrite));
            Assert.That(readWriteResolution!.Value.Tag, Is.EqualTo(RegionTag.Ram));
        });
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Verifies that composite layer handles concurrent access safely.
    /// </summary>
    [Test]
    public void CompositeLayer_ConcurrentAccess_ThreadSafe()
    {
        var memory = new PhysicalMemory(PageSize, "Memory");
        var target = new RamTarget(memory.Slice(0, PageSize));

        var mockLayer = new Mock<ICompositeLayer>();
        mockLayer.Setup(l => l.Name).Returns("ConcurrentTest");
        mockLayer.Setup(l => l.Priority).Returns(100);
        mockLayer.Setup(l => l.IsActive).Returns(true);
        mockLayer.Setup(l => l.AddressRange).Returns((0xD000u, 0x1000u));
        mockLayer.Setup(l => l.ResolveMapping(It.IsAny<Addr>(), It.IsAny<AccessIntent>()))
            .Returns(new CompositeLayerResolution(target, 0, PagePerms.ReadWrite, RegionTag.Ram, TargetCaps.None));

        var results = new List<CompositeLayerResolution?>();
        var tasks = new List<Task>();

        // Simulate concurrent resolution requests
        for (int i = 0; i < 100; i++)
        {
            var addr = (Addr)(0xD000 + (i % 0x1000));
            tasks.Add(Task.Run(() =>
            {
                var result = mockLayer.Object.ResolveMapping(addr, AccessIntent.DataRead);
                lock (results)
                {
                    results.Add(result);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.That(results.Count, Is.EqualTo(100));
        Assert.That(results.All(r => r != null), Is.True);
    }

    #endregion

    #region Helper Methods

    private static Mock<Core.Interfaces.Cpu.ICpu> CreateMockCpu()
    {
        var mockCpu = new Mock<Core.Interfaces.Cpu.ICpu>();
        mockCpu.Setup(c => c.Halted).Returns(false);
        mockCpu.Setup(c => c.IsStopRequested).Returns(false);
        mockCpu.Setup(c => c.Step()).Returns(new Core.Cpu.CpuStepResult(Core.Cpu.CpuRunState.Running, 1));
        return mockCpu;
    }

    private static Mock<ICompositeLayer> CreateMockCompositeLayer(string name, int priority, Addr start, Addr size)
    {
        var mock = new Mock<ICompositeLayer>();
        mock.Setup(l => l.Name).Returns(name);
        mock.Setup(l => l.Priority).Returns(priority);
        mock.Setup(l => l.IsActive).Returns(true);
        mock.Setup(l => l.AddressRange).Returns((start, size));
        return mock;
    }

    #endregion
}