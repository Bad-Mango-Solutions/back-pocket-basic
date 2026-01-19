// <copyright file="ScheduleMonitorWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;

/// <summary>
/// Schedule monitor window displaying scheduler events in real-time.
/// </summary>
/// <remarks>
/// <para>
/// This window provides real-time monitoring of the scheduler's event queue,
/// showing pending events and logging when events are scheduled and consumed.
/// </para>
/// <para>
/// The window uses a <see cref="DispatcherTimer"/> to poll the scheduler state
/// at approximately 60Hz for smooth UI updates.
/// </para>
/// </remarks>
public partial class ScheduleMonitorWindow : Window
{
    /// <summary>
    /// Maximum number of log entries to keep in the event log.
    /// </summary>
    private const int MaxLogEntries = 500;

    /// <summary>
    /// Refresh interval in milliseconds, targeting approximately 60Hz refresh rate.
    /// </summary>
    private const double RefreshIntervalMs = 16.67;

    private readonly DispatcherTimer updateTimer;
    private readonly ObservableCollection<PendingEventInfo> pendingEvents = [];
    private readonly ObservableCollection<string> eventLog = [];

    // UI Control references
    private readonly ComboBox deviceFilter;
    private readonly ComboBox kindFilter;
    private readonly CheckBox pauseUpdatesCheckbox;
    private readonly TextBlock pendingEventsHeader;
    private readonly DataGrid pendingEventsGrid;
    private readonly ItemsControl eventLogList;
    private readonly TextBlock currentCycleText;
    private readonly TextBlock pendingCountText;
    private readonly TextBlock totalScheduledText;
    private readonly TextBlock consumedCountText;

    private IMachine? machine;
    private ISchedulerObserver? schedulerObserver;
    private ulong totalScheduledCount;
    private ulong totalConsumedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleMonitorWindow"/> class.
    /// </summary>
    public ScheduleMonitorWindow()
    {
        InitializeComponent();

        // Get references to UI controls
        deviceFilter = this.FindControl<ComboBox>("DeviceFilter")!;
        kindFilter = this.FindControl<ComboBox>("KindFilter")!;
        pauseUpdatesCheckbox = this.FindControl<CheckBox>("PauseUpdatesCheckbox")!;
        pendingEventsHeader = this.FindControl<TextBlock>("PendingEventsHeader")!;
        pendingEventsGrid = this.FindControl<DataGrid>("PendingEventsGrid")!;
        eventLogList = this.FindControl<ItemsControl>("EventLogList")!;
        currentCycleText = this.FindControl<TextBlock>("CurrentCycleText")!;
        pendingCountText = this.FindControl<TextBlock>("PendingCountText")!;
        totalScheduledText = this.FindControl<TextBlock>("TotalScheduledText")!;
        consumedCountText = this.FindControl<TextBlock>("ConsumedCountText")!;

        // Bind collections to UI
        pendingEventsGrid.ItemsSource = pendingEvents;
        eventLogList.ItemsSource = eventLog;

        // Populate the Kind filter with ScheduledEventKind values
        PopulateKindFilter();

        // Set up update timer at ~60Hz
        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(RefreshIntervalMs),
        };
        updateTimer.Tick += OnTimerTick;

        // Allow closing with Escape key
        KeyDown += OnKeyDown;
        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// Sets the machine to monitor.
    /// </summary>
    /// <param name="machineToMonitor">The machine containing the scheduler to monitor.</param>
    public void SetMachine(IMachine machineToMonitor)
    {
        ArgumentNullException.ThrowIfNull(machineToMonitor);
        machine = machineToMonitor;

        // Subscribe to scheduler observer events if the scheduler supports observation
        if (machine.Scheduler is ISchedulerObserver observer)
        {
            schedulerObserver = observer;
            schedulerObserver.EventScheduled += OnEventScheduled;
            schedulerObserver.EventConsumed += OnEventConsumed;
            schedulerObserver.EventCancelled += OnEventCancelled;
        }

        // Populate device filter with devices from the machine
        PopulateDeviceFilter();
    }

    private static string GetDeviceName(object? tag)
    {
        return tag switch
        {
            IPeripheral peripheral => peripheral.Name,
            string name => name,
            _ => "Unknown",
        };
    }

    private static string FormatNumber(ulong value)
    {
        if (value >= 1_000_000_000)
        {
            return $"{value / 1_000_000_000.0:F2}G";
        }

        if (value >= 1_000_000)
        {
            return $"{value / 1_000_000.0:F2}M";
        }

        if (value >= 1_000)
        {
            return $"{value / 1_000.0:F1}K";
        }

        return value.ToString();
    }

    private void OnEventScheduled(EventHandle handle, Cycle cycle, ScheduledEventKind kind, int priority, object? tag)
    {
        totalScheduledCount++;

        // Get device name from tag if available
        var deviceName = GetDeviceName(tag);

        // Add log entry (use Dispatcher.Post for thread safety since scheduler runs on emulator thread)
        Dispatcher.UIThread.Post(() =>
        {
            AddLogEntry($"[{cycle}] {deviceName}: Scheduled {kind} @ cycle {cycle} (pri {priority})");

            // Add to pending events grid
            pendingEvents.Add(new PendingEventInfo(
                $"0x{handle.Id:X8}",
                deviceName,
                kind.ToString(),
                FormatNumber(cycle),
                priority));
        });
    }

    private void OnEventConsumed(EventHandle handle, Cycle cycle, ScheduledEventKind kind)
    {
        totalConsumedCount++;

        // Add log entry (use Dispatcher.Post for thread safety since scheduler runs on emulator thread)
        Dispatcher.UIThread.Post(() =>
        {
            AddLogEntry($"[{cycle}] Consumed {kind} @ cycle {cycle}");

            // Remove from pending events grid
            var handleStr = $"0x{handle.Id:X8}";
            var toRemove = pendingEvents.FirstOrDefault(e => e.Handle == handleStr);
            if (toRemove != null)
            {
                pendingEvents.Remove(toRemove);
            }
        });
    }

    private void OnEventCancelled(EventHandle handle)
    {
        // Remove from pending events grid
        Dispatcher.UIThread.Post(() =>
        {
            var handleStr = $"0x{handle.Id:X8}";
            var toRemove = pendingEvents.FirstOrDefault(e => e.Handle == handleStr);
            if (toRemove != null)
            {
                pendingEvents.Remove(toRemove);
                AddLogEntry($"Cancelled event {handleStr}");
            }
        });
    }

    private void PopulateKindFilter()
    {
        // Add all ScheduledEventKind enum values except None
        foreach (var kind in Enum.GetValues<ScheduledEventKind>().Where(k => k != ScheduledEventKind.None))
        {
            kindFilter.Items.Add(new ComboBoxItem { Content = kind.ToString() });
        }
    }

    private void PopulateDeviceFilter()
    {
        // Clear existing items except "All"
        while (deviceFilter.Items.Count > 1)
        {
            deviceFilter.Items.RemoveAt(1);
        }

        if (machine is null)
        {
            return;
        }

        // Get devices from machine (if available through a component interface)
        var scheduledDevices = machine.GetComponents<IScheduledDevice>();
        foreach (var device in scheduledDevices)
        {
            deviceFilter.Items.Add(new ComboBoxItem { Content = device.Name });
        }
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        updateTimer.Start();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        updateTimer.Stop();

        // Unsubscribe from scheduler observer events
        if (schedulerObserver != null)
        {
            schedulerObserver.EventScheduled -= OnEventScheduled;
            schedulerObserver.EventConsumed -= OnEventConsumed;
            schedulerObserver.EventCancelled -= OnEventCancelled;
            schedulerObserver = null;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (pauseUpdatesCheckbox.IsChecked == true)
        {
            return;
        }

        UpdateSchedulerState();
    }

    /// <summary>
    /// Updates the scheduler state display with current values from the scheduler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The window subscribes to <see cref="ISchedulerObserver"/> events to track
    /// when events are scheduled, consumed, and cancelled. The pending events grid
    /// is updated in real-time via these event handlers.
    /// </para>
    /// </remarks>
    private void UpdateSchedulerState()
    {
        if (machine is null)
        {
            return;
        }

        var scheduler = machine.Scheduler;
        if (scheduler is null)
        {
            return;
        }

        // Update current cycle
        currentCycleText.Text = FormatNumber(scheduler.Now);

        // Update pending count from scheduler
        var pendingCount = scheduler.PendingEventCount;
        pendingCountText.Text = pendingCount.ToString();
        pendingEventsHeader.Text = $"Pending Events ({pendingCount})";

        // Update statistics from our counters
        totalScheduledText.Text = FormatNumber(totalScheduledCount);
        consumedCountText.Text = FormatNumber(totalConsumedCount);
    }

    private void AddLogEntry(string entry)
    {
        eventLog.Add(entry);

        // Trim log if it exceeds maximum
        while (eventLog.Count > MaxLogEntries)
        {
            eventLog.RemoveAt(0);
        }
    }

    private void OnClearLogClick(object? sender, RoutedEventArgs e)
    {
        eventLog.Clear();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Close();
        e.Handled = true;
    }

    /// <summary>
    /// Represents information about a pending scheduled event for display.
    /// </summary>
    /// <param name="Handle">The event handle as a hex string.</param>
    /// <param name="DeviceName">The name of the device that scheduled the event.</param>
    /// <param name="Kind">The kind of scheduled event.</param>
    /// <param name="DueCycle">The cycle at which the event is due.</param>
    /// <param name="Priority">The event priority.</param>
    public record PendingEventInfo(
        string Handle,
        string DeviceName,
        string Kind,
        string DueCycle,
        int Priority);
}