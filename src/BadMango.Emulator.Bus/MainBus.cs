// <copyright file="MainBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Interfaces;

using Serilog;
using Serilog.Events;

/// <summary>
/// The main memory bus implementation for routing CPU and DMA memory operations.
/// </summary>
/// <remarks>
/// <para>
/// This is the core implementation of <see cref="IMemoryBus"/> that provides
/// page-based address translation, handles atomic vs decomposed access decisions,
/// and provides the foundation for observability.
/// </para>
/// <para>
/// The bus uses 4KB pages for routing, with each page resolving to a target device
/// and physical base address. Cross-page wide accesses are automatically decomposed
/// into individual byte operations.
/// </para>
/// <para>
/// The CPU does not own memory; all memory interactions flow through the bus.
/// The CPU computes intent; the bus enforces consequences.
/// </para>
/// </remarks>
public sealed partial class MainBus : IMemoryBus
{
    /// <summary>
    /// The default page shift value for 4KB pages.
    /// </summary>
    private const int DefaultPageShift = 12;

    /// <summary>
    /// The default page mask for 4KB pages (0xFFF).
    /// </summary>
    private const Addr DefaultPageMask = 0xFFF;

    /// <summary>
    /// The page size in bytes (4KB).
    /// </summary>
    private const int PageSize = 1 << DefaultPageShift;

    /// <summary>
    /// The page table array for O(1) address-to-page translation.
    /// </summary>
    private readonly PageEntry[] pageTable;

    /// <summary>
    /// The base page table entries before any layers are applied.
    /// Used to restore mappings when layers are deactivated.
    /// </summary>
    private readonly PageEntry[] basePageTable;

    /// <summary>
    /// Dictionary of named layers for layer lookup.
    /// </summary>
    private readonly Dictionary<string, MappingLayer> layers = new(StringComparer.Ordinal);

    /// <summary>
    /// All layered mappings organized by layer name.
    /// </summary>
    private readonly Dictionary<string, List<LayeredMapping>> layeredMappings = new(StringComparer.Ordinal);

    /// <summary>
    /// Dictionary of swap groups by ID for O(1) lookup.
    /// </summary>
    private readonly Dictionary<uint, SwapGroup> swapGroupsById = [];

    /// <summary>
    /// Dictionary of swap group IDs by name for name-based lookup.
    /// </summary>
    private readonly Dictionary<string, uint> swapGroupIdsByName = new(StringComparer.Ordinal);

    /// <summary>
    /// Lock object for thread-safe swap group operations.
    /// </summary>
    private readonly object swapGroupLock = new();

    /// <summary>
    /// Logger for diagnostic messages emitted by the bus, including warnings on
    /// faults observed by the slow-path <c>TryRead8</c>/<c>TryWrite8</c> APIs
    /// and on silent floating-bus / missing sub-target reads observed by the
    /// fast-path <c>Read8</c>/<c>Write8</c> APIs.
    /// </summary>
    private readonly ILogger logger;

    /// <summary>
    /// Optional sink that captures bus faults for later inspection by debug
    /// tooling. When <see langword="null"/>, faults are still returned to
    /// the caller and logged (per <see cref="logger"/>) but are not retained
    /// across calls.
    /// </summary>
    private readonly IBusFaultRecorder? faultRecorder;

    /// <summary>
    /// Backing field for <see cref="FaultRecordingEnabled"/>. Kept as a plain
    /// field so the hot-path check in <see cref="Read8"/> / <see cref="Write8"/>
    /// is a single load.
    /// </summary>
    private bool faultRecordingEnabled;

    /// <summary>
    /// Counter for generating unique swap group IDs.
    /// </summary>
    private uint nextSwapGroupId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainBus"/> class with the specified address space size.
    /// </summary>
    /// <param name="addressSpaceBits">
    /// The number of bits in the address space. Defaults to 16 for a 64KB address space.
    /// For 128KB, use 17. For 16MB (65C816), use 24. For 4GB (65832), use 32.
    /// </param>
    /// <param name="logger">
    /// Optional Serilog logger for diagnostic output. When <see langword="null"/>,
    /// the shared no-op <see cref="Serilog.Core.Logger.None"/> is used so the bus never
    /// touches the global <c>Log.Logger</c> facade. Production callers should pass an
    /// injected logger. Faults are only logged while <see cref="FaultRecordingEnabled"/>
    /// is <see langword="true"/>.
    /// </param>
    /// <param name="faultRecorder">
    /// Optional recorder that captures bus faults into a ring buffer for later
    /// inspection by debug tooling. When non-<see langword="null"/> and the
    /// recorder also implements <see cref="IBusFaultRing"/>, the same instance
    /// is exposed via <see cref="FaultRing"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="addressSpaceBits"/> is less than 12 (minimum for one 4KB page)
    /// or greater than 32.
    /// </exception>
    public MainBus(int addressSpaceBits = 16, ILogger? logger = null, IBusFaultRecorder? faultRecorder = null)
    {
        if (addressSpaceBits < DefaultPageShift)
        {
            throw new ArgumentOutOfRangeException(
                nameof(addressSpaceBits),
                addressSpaceBits,
                $"Address space must be at least {DefaultPageShift} bits to support 4KB pages.");
        }

        if (addressSpaceBits > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(addressSpaceBits),
                addressSpaceBits,
                "Address space cannot exceed 32 bits.");
        }

        int pageCount = 1 << (addressSpaceBits - DefaultPageShift);
        pageTable = new PageEntry[pageCount];
        basePageTable = new PageEntry[pageCount];

        // Use the shared no-op Serilog logger when no logger is injected so we
        // never reach for the global Log.Logger facade from library code and
        // do not allocate a disposable logger per bus.
        this.logger = logger?.ForContext<MainBus>() ?? Serilog.Core.Logger.None;
        this.faultRecorder = faultRecorder;
        FaultRing = faultRecorder as IBusFaultRing;
    }

    /// <inheritdoc />
    public int PageShift => DefaultPageShift;

    /// <inheritdoc />
    public Addr PageMask => DefaultPageMask;

    /// <inheritdoc />
    public int PageCount => pageTable.Length;

    /// <inheritdoc />
    public IBusFaultRing? FaultRing { get; }

    /// <inheritdoc />
    public bool FaultRecordingEnabled
    {
        get => faultRecordingEnabled;
        set => faultRecordingEnabled = value;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(in BusAccess access)
    {
        ref readonly var page = ref pageTable[access.Address >> PageShift];
        Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is not null)
            {
                return subTarget.Read8(physicalAddress, access);
            }

            // No sub-target found, return floating bus value and record a
            // synthetic Unmapped fault so debug tooling can see the silent
            // hole instead of being lied to by the floating-bus value.
            if (faultRecordingEnabled)
            {
                RecordSilentFault(access, BusFault.Unmapped(access), page.DeviceId, page.RegionTag, isComposite: true);
            }

            return 0xFF;
        }

        if (page.Target is null)
        {
            // Page entry exists but no target is mapped. Record an Unmapped
            // fault and return the floating-bus value rather than NRE'ing.
            if (faultRecordingEnabled)
            {
                RecordSilentFault(access, BusFault.Unmapped(access), page.DeviceId, page.RegionTag, isComposite: false);
            }

            return 0xFF;
        }

        return page.Target.Read8(physicalAddress, access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(in BusAccess access, byte value)
    {
        ref readonly var page = ref pageTable[access.Address >> PageShift];
        Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is not null)
            {
                subTarget.Write8(physicalAddress, value, access);
                return;
            }

            // No sub-target found for this offset - record the silent drop.
            if (faultRecordingEnabled)
            {
                RecordSilentFault(access, BusFault.Unmapped(access), page.DeviceId, page.RegionTag, isComposite: true);
            }

            return;
        }

        if (page.Target is null)
        {
            if (faultRecordingEnabled)
            {
                RecordSilentFault(access, BusFault.Unmapped(access), page.DeviceId, page.RegionTag, isComposite: false);
            }

            return;
        }

        page.Target.Write8(physicalAddress, value, access);
    }

    /// <inheritdoc />
    public BusResult<byte> TryRead8(in BusAccess access)
    {
        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return RecordAndReturnFault(access, BusFault.Unmapped(access));
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return RecordAndReturnFault(access, BusFault.Unmapped(access));
        }

        // Check read permission.
        // Debug reads (AccessIntent.DebugRead) bypass this check to allow
        // inspecting memory regardless of page permissions.
        if (!page.CanRead && !access.IsDebugAccess)
        {
            return RecordAndReturnFault(access, BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag));
        }

        // Check NX on instruction fetch (Atomic mode only)
        if (access.Intent == AccessIntent.InstructionFetch &&
            access.Mode == BusAccessMode.Atomic &&
            !page.CanExecute)
        {
            return RecordAndReturnFault(access, BusFault.NoExecute(access, page.DeviceId, page.RegionTag));
        }

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is null)
            {
                // No sub-target found - this is a silent floating-bus read.
                // Record the synthetic Unmapped fault but still return success
                // with $FF so the CPU sees the expected floating-bus value
                // (matches the semantics of the fast-path Read8 above).
                if (faultRecordingEnabled)
                {
                    RecordSilentFault(access, BusFault.Unmapped(access), page.DeviceId, page.RegionTag, isComposite: true);
                }

                return BusResult<byte>.Success(0xFF, access, page.DeviceId, page.RegionTag, cycles: 1);
            }

            Addr physicalAddress = page.PhysicalBase + offset;
            byte value = subTarget.Read8(physicalAddress, access);
            return BusResult<byte>.Success(value, access, page.DeviceId, composite.GetSubRegionTag(offset), cycles: 1);
        }

        // Perform the read
        Addr physAddr = page.PhysicalBase + (access.Address & PageMask);
        byte readValue = page.Target.Read8(physAddr, access);

        return BusResult<byte>.Success(readValue, access, page.DeviceId, page.RegionTag, cycles: 1);
    }

    /// <inheritdoc />
    public BusResult TryWrite8(in BusAccess access, byte value)
    {
        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return RecordAndReturnFaultVoid(access, BusFault.Unmapped(access));
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return RecordAndReturnFaultVoid(access, BusFault.Unmapped(access));
        }

        // Check write permission.
        // Debug writes (AccessIntent.DebugWrite) bypass this check because:
        // 1. They're used for test setup (patching ROM with stubs)
        // 2. The target ultimately decides if the write succeeds
        //    - RomTarget accepts debug writes if constructed with Memory<byte>
        //    - RomTarget ignores debug writes if constructed with ReadOnlyMemory<byte>
        // 3. This enables ICpu.Poke8() to work for debugging and testing scenarios
        if (!page.CanWrite && !access.IsDebugAccess)
        {
            return RecordAndReturnFaultVoid(access, BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag));
        }

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is not null)
            {
                Addr physicalAddress = page.PhysicalBase + offset;
                subTarget.Write8(physicalAddress, value, access);
            }
            else
            {
                // Silent drop - record the synthetic Unmapped fault.
                if (faultRecordingEnabled)
                {
                    RecordSilentFault(access, BusFault.Unmapped(access), page.DeviceId, page.RegionTag, isComposite: true);
                }
            }

            return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        // Perform the write
        Addr physAddr = page.PhysicalBase + (access.Address & PageMask);
        page.Target.Write8(physAddr, value, access);

        return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
    }

    /// <summary>
    /// Records a fault into the recorder (if recording is enabled) and emits
    /// a structured warning log entry, then returns the fault as a
    /// <see cref="BusResult{T}"/> for the caller to propagate.
    /// </summary>
    /// <remarks>
    /// Kept out of line (<see cref="MethodImplOptions.NoInlining"/>) so the
    /// cold recording code does not bloat the <c>TryRead8</c> fast path.
    /// </remarks>
    /// <param name="access">The originating access.</param>
    /// <param name="fault">The fault to record.</param>
    /// <returns>The fault wrapped in a typed result.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BusResult<byte> RecordAndReturnFault(in BusAccess access, in BusFault fault)
    {
        if (faultRecordingEnabled && !access.IsDebugAccess)
        {
            faultRecorder?.Record(in fault);
            LogFault(in fault, isSilent: false, isComposite: false);
        }

        return fault;
    }

    /// <summary>
    /// Records a fault into the recorder (if recording is enabled) and emits
    /// a structured warning log entry, then returns the fault wrapped in a
    /// non-generic <see cref="BusResult"/> for the caller to propagate.
    /// </summary>
    /// <remarks>
    /// Kept out of line (<see cref="MethodImplOptions.NoInlining"/>) so the
    /// cold recording code does not bloat the <c>TryWrite8</c> fast path.
    /// </remarks>
    /// <param name="access">The originating access.</param>
    /// <param name="fault">The fault to record.</param>
    /// <returns>The fault wrapped in a non-generic result.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private BusResult RecordAndReturnFaultVoid(in BusAccess access, in BusFault fault)
    {
        if (faultRecordingEnabled && !access.IsDebugAccess)
        {
            faultRecorder?.Record(in fault);
            LogFault(in fault, isSilent: false, isComposite: false);
        }

        return BusResult.FromFault(fault);
    }

    /// <summary>
    /// Records a synthetic fault for a silent floating-bus or
    /// missing-sub-target case observed by the fast-path read/write methods.
    /// </summary>
    /// <remarks>
    /// Callers must check <see cref="faultRecordingEnabled"/> before calling
    /// so the disabled case costs a single field load on the hot path. The
    /// method is kept out of line (<see cref="MethodImplOptions.NoInlining"/>)
    /// so its struct building, lock, and logging code is not inlined into
    /// <see cref="Read8"/> / <see cref="Write8"/>. Debug accesses (debugger
    /// peeks/pokes) are never recorded.
    /// </remarks>
    /// <param name="access">The originating access.</param>
    /// <param name="fault">The synthetic fault payload.</param>
    /// <param name="deviceId">The device ID from the resolved page entry.</param>
    /// <param name="regionTag">The region tag from the resolved page entry.</param>
    /// <param name="isComposite">
    /// <see langword="true"/> when the silent drop occurred because a
    /// composite target had no sub-target for the requested offset.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void RecordSilentFault(in BusAccess access, in BusFault fault, int deviceId, RegionTag regionTag, bool isComposite)
    {
        if (access.IsDebugAccess)
        {
            return;
        }

        // Rebuild the fault with the resolved device/region so downstream
        // tooling sees "Composite at $C300 / RegionTag.Slot" instead of "-1".
        var enriched = new BusFault(
            fault.Kind,
            access.Address,
            access.WidthBits,
            access.Intent,
            access.Mode,
            access.SourceId,
            deviceId,
            regionTag,
            access.Cycle);

        faultRecorder?.Record(in enriched);
        LogFault(in enriched, isSilent: true, isComposite: isComposite);
    }

    /// <summary>
    /// Emits a structured Serilog warning describing the fault.
    /// </summary>
    /// <param name="fault">The fault to log.</param>
    /// <param name="isSilent">
    /// <see langword="true"/> when the fault originated from a hot-path
    /// silent drop (no result returned to the CPU); <see langword="false"/>
    /// when it was returned as part of a Try-style result.
    /// </param>
    /// <param name="isComposite">
    /// <see langword="true"/> when the fault came from a composite target's
    /// missing sub-target dispatch.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LogFault(in BusFault fault, bool isSilent, bool isComposite)
    {
        // Guard log generation with IsEnabled so message-template assembly is
        // skipped entirely when the sink is silent or warnings are filtered.
        if (!logger.IsEnabled(LogEventLevel.Warning))
        {
            return;
        }

        string source = isComposite
            ? "composite-no-subtarget"
            : isSilent
                ? "hot-path"
                : "try-path";

        logger.Warning(
            "Bus fault {Kind} at ${Address:X4} intent={Intent} width={Width} source={Source} device={DeviceId} region={RegionTag} cycle={Cycle}",
            fault.Kind,
            fault.Address,
            fault.Intent,
            fault.WidthBits,
            source,
            fault.DeviceId,
            fault.RegionTag,
            fault.Cycle);
    }
}