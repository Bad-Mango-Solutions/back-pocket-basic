// <copyright file="BreakpointManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Cpu;

/// <summary>
/// Manages execution breakpoints by registering <see cref="TrapOperation.Call"/>
/// handlers with the active <see cref="ITrapRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each breakpoint is implemented as a non-handling call trap: when the CPU fetches
/// an instruction at the breakpoint address, the trap fires, the breakpoint's hit
/// counter is incremented, <see cref="ICpu.RequestStop"/> is invoked, and the trap
/// returns <see cref="TrapResult.NotHandled"/> so the original instruction still
/// executes. The run loop then exits on the next iteration. This gives clean
/// "execute-and-break" semantics and avoids the need for a skip-once mechanism on
/// resume.
/// </para>
/// <para>
/// Breakpoints can be temporarily disabled without being removed. Disabled
/// breakpoints are still registered with the trap registry but skip the stop
/// request.
/// </para>
/// </remarks>
public sealed class BreakpointManager
{
    private readonly Lock syncLock = new();
    private readonly Dictionary<uint, BreakpointEntry> entries = [];
    private ITrapRegistry? registry;
    private ICpu? cpu;

    /// <summary>
    /// Gets the number of registered breakpoints.
    /// </summary>
    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return entries.Count;
            }
        }
    }

    /// <summary>
    /// Gets the address of the breakpoint that most recently caused a stop,
    /// or <see langword="null"/> if no breakpoint has fired.
    /// </summary>
    public uint? LastHitAddress { get; private set; }

    /// <summary>
    /// Attaches the manager to a CPU and trap registry. Any breakpoints added
    /// before attachment are re-registered with the new registry.
    /// </summary>
    /// <param name="cpu">The CPU whose execution will be stopped on hits.</param>
    /// <param name="registry">The trap registry to register breakpoint traps with.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cpu"/> or <paramref name="registry"/> is <see langword="null"/>.
    /// </exception>
    public void Attach(ICpu cpu, ITrapRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        ArgumentNullException.ThrowIfNull(registry);

        lock (syncLock)
        {
            this.cpu = cpu;
            this.registry = registry;

            // Re-register any pre-existing breakpoints with the new registry.
            foreach (var entry in entries.Values)
            {
                RegisterWithRegistry(entry);
            }
        }
    }

    /// <summary>
    /// Detaches the manager, unregistering all live trap entries from the
    /// previously attached registry. Breakpoint definitions are retained.
    /// </summary>
    public void Detach()
    {
        lock (syncLock)
        {
            if (registry is not null)
            {
                foreach (var addr in entries.Keys)
                {
                    registry.Unregister(addr, TrapOperation.Call);
                }
            }

            registry = null;
            cpu = null;
        }
    }

    /// <summary>
    /// Adds a breakpoint at the specified address.
    /// </summary>
    /// <param name="address">The address to break on.</param>
    /// <param name="label">Optional human-readable label for the breakpoint.</param>
    /// <returns>
    /// <see langword="true"/> if a new breakpoint was added;
    /// <see langword="false"/> if one already existed at the address.
    /// </returns>
    public bool Add(uint address, string? label = null)
    {
        lock (syncLock)
        {
            if (entries.ContainsKey(address))
            {
                return false;
            }

            var entry = new BreakpointEntry(address, label, true);
            entries[address] = entry;
            RegisterWithRegistry(entry);
            return true;
        }
    }

    /// <summary>
    /// Removes the breakpoint at the specified address.
    /// </summary>
    /// <param name="address">The address of the breakpoint to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the breakpoint existed and was removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Remove(uint address)
    {
        lock (syncLock)
        {
            if (!entries.Remove(address))
            {
                return false;
            }

            registry?.Unregister(address, TrapOperation.Call);
            return true;
        }
    }

    /// <summary>
    /// Removes every breakpoint.
    /// </summary>
    public void Clear()
    {
        lock (syncLock)
        {
            if (registry is not null)
            {
                foreach (var addr in entries.Keys)
                {
                    registry.Unregister(addr, TrapOperation.Call);
                }
            }

            entries.Clear();
        }
    }

    /// <summary>
    /// Enables or disables the breakpoint at the specified address.
    /// </summary>
    /// <param name="address">The address of the breakpoint.</param>
    /// <param name="enabled">Whether the breakpoint should be enabled.</param>
    /// <returns>
    /// <see langword="true"/> if the breakpoint exists and its state was updated;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool SetEnabled(uint address, bool enabled)
    {
        lock (syncLock)
        {
            if (!entries.TryGetValue(address, out var entry))
            {
                return false;
            }

            entry.Enabled = enabled;
            return true;
        }
    }

    /// <summary>
    /// Gets a snapshot of every registered breakpoint.
    /// </summary>
    /// <returns>A copy of the current breakpoint entries.</returns>
    public IReadOnlyList<BreakpointEntry> GetAll()
    {
        lock (syncLock)
        {
            return [.. entries.Values];
        }
    }

    /// <summary>
    /// Clears the recorded last-hit address.
    /// </summary>
    public void ResetLastHit()
    {
        LastHitAddress = null;
    }

    private void RegisterWithRegistry(BreakpointEntry entry)
    {
        if (registry is null)
        {
            return;
        }

        TrapResult Handler(ICpu trapCpu, IMemoryBus trapBus, IEventContext trapContext)
        {
            BreakpointEntry? snapshot;
            lock (syncLock)
            {
                entries.TryGetValue(entry.Address, out snapshot);
            }

            if (snapshot is null || !snapshot.Enabled)
            {
                return TrapResult.NotHandled;
            }

            snapshot.IncrementHits();
            LastHitAddress = entry.Address;
            cpu?.RequestStop();
            return TrapResult.NotHandled;
        }

        registry.Register(
            entry.Address,
            TrapOperation.Call,
            $"BP_{entry.Address:X4}",
            TrapCategory.UserDefined,
            Handler,
            entry.Label);
    }

    /// <summary>
    /// Represents a single breakpoint entry.
    /// </summary>
    public sealed class BreakpointEntry
    {
        private long hits;

        /// <summary>
        /// Initializes a new instance of the <see cref="BreakpointEntry"/> class.
        /// </summary>
        /// <param name="address">The breakpoint address.</param>
        /// <param name="label">Optional label.</param>
        /// <param name="enabled">Whether the breakpoint is enabled.</param>
        public BreakpointEntry(uint address, string? label, bool enabled)
        {
            Address = address;
            Label = label;
            Enabled = enabled;
        }

        /// <summary>
        /// Gets the address of the breakpoint.
        /// </summary>
        public uint Address { get; }

        /// <summary>
        /// Gets the optional human-readable label for the breakpoint.
        /// </summary>
        public string? Label { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the breakpoint is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets the number of times this breakpoint has been hit.
        /// </summary>
        public long Hits => Interlocked.Read(ref hits);

        /// <summary>
        /// Increments the hit counter for this breakpoint by one.
        /// </summary>
        internal void IncrementHits() => Interlocked.Increment(ref hits);
    }
}