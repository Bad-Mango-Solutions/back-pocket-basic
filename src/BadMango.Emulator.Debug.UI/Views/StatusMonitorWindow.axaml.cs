// <copyright file="StatusMonitorWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Debug.UI.StatusMonitor;

/// <summary>
/// Status monitor window displaying active machine metrics and state.
/// </summary>
/// <remarks>
/// <para>
/// This window provides real-time monitoring of machine performance including
/// CPU registers, machine state, performance metrics, and annunciator visualization.
/// </para>
/// <para>
/// The window uses a <see cref="DispatcherTimer"/> to poll the machine state
/// at approximately 60Hz for smooth UI updates.
/// </para>
/// </remarks>
public partial class StatusMonitorWindow : Window
{
    /// <summary>
    /// History length for annunciator scrolling visualization.
    /// </summary>
    private const int AnnunciatorHistoryLength = 60;

    /// <summary>
    /// Number of frames for fade effect when annunciator turns off.
    /// </summary>
    private const int FadeFrames = 20;

    /// <summary>
    /// Refresh interval in milliseconds, targeting approximately 60Hz refresh rate.
    /// </summary>
    private const double RefreshIntervalMs = 16.67;

    /// <summary>
    /// Annunciator colors for each track.
    /// </summary>
    private static readonly IBrush[] AnnunciatorColors =
    [
        new SolidColorBrush(Color.FromRgb(255, 100, 100)), // Red
        new SolidColorBrush(Color.FromRgb(100, 255, 100)), // Green
        new SolidColorBrush(Color.FromRgb(100, 100, 255)), // Blue
        new SolidColorBrush(Color.FromRgb(255, 200, 100)), // Orange/Yellow
    ];

    private readonly DispatcherTimer updateTimer;
    private readonly List<List<float>> annunciatorHistory = [];
    private readonly Canvas[] annunciatorCanvases;
    private IMachineStatsProvider? statsProvider;
    private bool extensionsInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusMonitorWindow"/> class.
    /// </summary>
    public StatusMonitorWindow()
    {
        InitializeComponent();

        // Initialize annunciator history
        for (int i = 0; i < 4; i++)
        {
            annunciatorHistory.Add([]);
        }

        annunciatorCanvases = [Ann0Canvas, Ann1Canvas, Ann2Canvas, Ann3Canvas];

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
    /// Sets the machine statistics provider.
    /// </summary>
    /// <param name="provider">The statistics provider for the machine to monitor.</param>
    public void SetStatsProvider(IMachineStatsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        statsProvider = provider;
    }

    private static string FormatFlags(ProcessorStatusFlags flags)
    {
        var flagChars = new char[8];
        flagChars[0] = (flags & ProcessorStatusFlags.N) != 0 ? 'N' : 'n';
        flagChars[1] = (flags & ProcessorStatusFlags.V) != 0 ? 'V' : 'v';
        flagChars[2] = '-'; // Reserved bit
        flagChars[3] = (flags & ProcessorStatusFlags.B) != 0 ? 'B' : 'b';
        flagChars[4] = (flags & ProcessorStatusFlags.D) != 0 ? 'D' : 'd';
        flagChars[5] = (flags & ProcessorStatusFlags.I) != 0 ? 'I' : 'i';
        flagChars[6] = (flags & ProcessorStatusFlags.Z) != 0 ? 'Z' : 'z';
        flagChars[7] = (flags & ProcessorStatusFlags.C) != 0 ? 'C' : 'c';
        return new string(flagChars);
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

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        updateTimer.Start();
        InitializeExtensions();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        updateTimer.Stop();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (statsProvider == null)
        {
            return;
        }

        // Sample statistics
        statsProvider.Sample();

        // Update machine state
        UpdateMachineState();

        // Update CPU registers
        UpdateCpuRegisters();

        // Update performance metrics
        UpdatePerformanceMetrics();

        // Update annunciators
        UpdateAnnunciators();

        // Update extensions
        UpdateExtensions();
    }

    private void UpdateMachineState()
    {
        if (statsProvider == null)
        {
            return;
        }

        // Machine state with color coding
        var state = statsProvider.State;
        MachineStateText.Text = state.ToString();
        MachineStateText.Foreground = state switch
        {
            MachineState.Running => Brushes.LimeGreen,
            MachineState.Paused => Brushes.Yellow,
            MachineState.Stopped => Brushes.Orange,
            _ => Brushes.Red,
        };

        // WAI state
        var isWai = statsProvider.IsWaitingForInterrupt;
        WaiStateText.Text = isWai ? "Yes" : "No";
        WaiStateText.Foreground = isWai ? Brushes.Yellow : Brushes.White;

        // WAI duration
        var waiDuration = statsProvider.WaiDuration;
        if (waiDuration > TimeSpan.Zero)
        {
            WaiDurationText.Text = waiDuration.TotalSeconds >= 1
                ? $"{waiDuration.TotalSeconds:F1}s"
                : $"{waiDuration.TotalMilliseconds:F0}ms";
        }
        else
        {
            WaiDurationText.Text = "--";
        }
    }

    private void UpdateCpuRegisters()
    {
        if (statsProvider == null)
        {
            return;
        }

        var regs = statsProvider.Registers;

        RegAText.Text = $"${regs.A.GetByte():X2}";
        RegXText.Text = $"${regs.X.GetByte():X2}";
        RegYText.Text = $"${regs.Y.GetByte():X2}";
        RegSPText.Text = $"${regs.SP.GetByte():X2}";
        RegPCText.Text = $"${regs.PC.GetAddr():X4}";
        RegPText.Text = FormatFlags(regs.P);
    }

    private void UpdatePerformanceMetrics()
    {
        if (statsProvider == null)
        {
            return;
        }

        CyclesText.Text = FormatNumber(statsProvider.TotalCycles);
        ActualMHzText.Text = $"{statsProvider.ActualMHz:F3}";
        IpsText.Text = FormatNumber((ulong)statsProvider.InstructionsPerSecond);
        TargetMHzText.Text = $"{statsProvider.TargetMHz:F3}";
        CpiText.Text = $"{statsProvider.AverageCyclesPerInstruction:F1}";
        CpuLoadText.Text = $"{statsProvider.CpuUtilization:F0}%";

        // Scheduler info
        var queueDepth = statsProvider.SchedulerQueueDepth;
        SchedulerText.Text = queueDepth == 1 ? "1 event" : $"{queueDepth} events";

        var nextEvent = statsProvider.NextEventCycles;
        NextEventText.Text = nextEvent.HasValue ? $"+{FormatNumber(nextEvent.Value)}" : "--";
    }

    private void UpdateAnnunciators()
    {
        if (statsProvider == null)
        {
            return;
        }

        var states = statsProvider.AnnunciatorStates;

        for (int i = 0; i < 4; i++)
        {
            var history = annunciatorHistory[i];

            // Add new state to history (1.0 = on, decaying value = fading)
            if (states[i])
            {
                history.Add(1.0f);
            }
            else
            {
                // If turning off, start fade from last value
                float lastValue = history.Count > 0 ? history[^1] : 0f;
                if (lastValue > 0)
                {
                    // Fade down
                    float fadeAmount = 1.0f / FadeFrames;
                    history.Add(Math.Max(0, lastValue - fadeAmount));
                }
                else
                {
                    history.Add(0f);
                }
            }

            // Trim history to fixed length
            while (history.Count > AnnunciatorHistoryLength)
            {
                history.RemoveAt(0);
            }

            // Render the history
            RenderAnnunciatorTrack(i);
        }
    }

    private void RenderAnnunciatorTrack(int annIndex)
    {
        var canvas = annunciatorCanvases[annIndex];
        var history = annunciatorHistory[annIndex];
        var baseBrush = AnnunciatorColors[annIndex] as SolidColorBrush;

        canvas.Children.Clear();

        if (history.Count == 0 || baseBrush == null)
        {
            return;
        }

        var canvasWidth = canvas.Bounds.Width;
        var canvasHeight = canvas.Bounds.Height;

        if (canvasWidth <= 0 || canvasHeight <= 0)
        {
            return;
        }

        // Calculate bar width
        var barWidth = canvasWidth / AnnunciatorHistoryLength;

        for (int i = 0; i < history.Count; i++)
        {
            var value = history[i];
            if (value <= 0)
            {
                continue;
            }

            // Calculate color with alpha for fade effect
            var baseColor = baseBrush.Color;
            var alpha = (byte)(value * 255);
            var color = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

            var rect = new Rectangle
            {
                Width = Math.Max(1, barWidth - 1),
                Height = canvasHeight * value,
                Fill = new SolidColorBrush(color),
            };

            Canvas.SetLeft(rect, i * barWidth);
            Canvas.SetBottom(rect, 0);

            canvas.Children.Add(rect);
        }
    }

    private void InitializeExtensions()
    {
        if (statsProvider == null || extensionsInitialized)
        {
            return;
        }

        var extensions = statsProvider.Extensions;
        if (extensions.Count == 0)
        {
            return;
        }

        // Hide the "no extensions" message
        NoExtensionsText.IsVisible = false;

        // Add each extension's control
        foreach (var ext in extensions)
        {
            var control = ext.CreateControl();
            ExtensionsPanel.Children.Add(control);
        }

        extensionsInitialized = true;
    }

    private void UpdateExtensions()
    {
        if (statsProvider == null)
        {
            return;
        }

        foreach (var ext in statsProvider.Extensions)
        {
            ext.UpdateDisplay();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
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
}