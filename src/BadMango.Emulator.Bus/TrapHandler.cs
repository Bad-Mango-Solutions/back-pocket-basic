// <copyright file="TrapHandler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core;

/// <summary>
/// Delegate for trap handler implementations.
/// </summary>
/// <param name="cpu">CPU instance for register access.</param>
/// <param name="bus">Memory bus for RAM access.</param>
/// <param name="context">Event context for scheduling and signals.</param>
/// <returns>Result indicating whether the trap was handled.</returns>
/// <remarks>
/// <para>
/// Trap handlers are called when the CPU fetches an instruction from a trapped address.
/// The handler can:
/// </para>
/// <list type="bullet">
/// <item><description>Access and modify CPU registers</description></item>
/// <item><description>Read/write memory through the bus</description></item>
/// <item><description>Schedule events or signal interrupts</description></item>
/// <item><description>Return handled with cycles to simulate ROM timing</description></item>
/// <item><description>Return not-handled to fall through to actual ROM code</description></item>
/// </list>
/// </remarks>
public delegate TrapResult TrapHandler(ICpu cpu, IMemoryBus bus, IEventContext context);