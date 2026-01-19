// <copyright file="ISchedulerObserver.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

using Core;

/// <summary>
/// Provides observation events for scheduler activity.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables monitoring tools (like ScheduleMonitor) to observe scheduler activity
/// without modifying the core scheduler logic. Implementations can subscribe to events to track
/// when events are scheduled, consumed, and cancelled.
/// </para>
/// <para>
/// <b>Performance Note:</b> The scheduler is on the hot path of the emulation loop.
/// Observers should minimize the work done in event handlers to avoid impacting performance.
/// Consider buffering events for batch processing in the UI thread.
/// </para>
/// </remarks>
public interface ISchedulerObserver
{
    /// <summary>
    /// Occurs when an event is scheduled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event parameters are:
    /// <list type="bullet">
    /// <item><description><see cref="EventHandle"/>: The handle of the scheduled event.</description></item>
    /// <item><description><see cref="Cycle"/>: The target cycle when the event will fire.</description></item>
    /// <item><description><see cref="ScheduledEventKind"/>: The kind of event.</description></item>
    /// <item><description><see cref="int"/>: The priority of the event.</description></item>
    /// <item><description><see cref="object"/>: Optional tag identifying the event source.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    event Action<EventHandle, Cycle, ScheduledEventKind, int, object?>? EventScheduled;

    /// <summary>
    /// Occurs when an event is consumed (dispatched).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event parameters are:
    /// <list type="bullet">
    /// <item><description><see cref="EventHandle"/>: The handle of the consumed event.</description></item>
    /// <item><description><see cref="Cycle"/>: The cycle when the event was consumed.</description></item>
    /// <item><description><see cref="ScheduledEventKind"/>: The kind of event.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    event Action<EventHandle, Cycle, ScheduledEventKind>? EventConsumed;

    /// <summary>
    /// Occurs when an event is cancelled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Event parameters are:
    /// <list type="bullet">
    /// <item><description><see cref="EventHandle"/>: The handle of the cancelled event.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    event Action<EventHandle>? EventCancelled;
}