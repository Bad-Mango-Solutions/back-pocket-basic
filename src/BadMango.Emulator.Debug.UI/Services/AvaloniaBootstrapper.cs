// <copyright file="AvaloniaBootstrapper.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Services;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

/// <summary>
/// Bootstraps and manages the Avalonia UI thread for the console debugger.
/// </summary>
/// <remarks>
/// <para>
/// This class allows the console REPL to coexist with Avalonia by running
/// the Avalonia UI thread in the background. The REPL continues to run on
/// the main thread while Avalonia handles UI operations on its dedicated thread.
/// </para>
/// <para>
/// The bootstrapper follows the threading model described in the Debug Video
/// Window specification: a dedicated Avalonia UI thread owns all windows and
/// rendering, while the emulator and console run on separate threads.
/// </para>
/// </remarks>
/// <seealso href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/video/Pocket2e%20Debug%20Video%20Window%20(Avalonia)%20%E2%80%94%20Specification.md#8-threading-model">
/// Threading Model Specification
/// </seealso>
public sealed class AvaloniaBootstrapper : IDisposable
{
    /// <summary>
    /// Timeout for waiting for Avalonia to fully initialize.
    /// </summary>
    private static readonly TimeSpan InitializationTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for waiting for the Avalonia thread to complete during shutdown.
    /// </summary>
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private static readonly object SyncLock = new();
    private static AvaloniaBootstrapper? instance;
    private static bool isInitialized;

    private readonly Thread avaloniaThread;
    private readonly ManualResetEventSlim startedEvent = new(false);
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private bool disposed;

    private AvaloniaBootstrapper()
    {
        this.avaloniaThread = new Thread(this.RunAvalonia)
        {
            Name = "Avalonia UI Thread",
            IsBackground = true,
        };
    }

    /// <summary>
    /// Gets a value indicating whether Avalonia has been initialized and is running.
    /// </summary>
    public static bool IsRunning => isInitialized && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;

    /// <summary>
    /// Ensures Avalonia is initialized and running on a background thread.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is thread-safe and can be called multiple times. Subsequent
    /// calls after the first will simply return immediately.
    /// </para>
    /// <para>
    /// The method blocks until Avalonia is fully initialized and ready to
    /// accept UI operations.
    /// </para>
    /// </remarks>
    public static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        lock (SyncLock)
        {
            if (isInitialized)
            {
                return;
            }

            instance = new AvaloniaBootstrapper();
            instance.Start();
            isInitialized = true;
        }
    }

    /// <summary>
    /// Shuts down Avalonia gracefully.
    /// </summary>
    public static void Shutdown()
    {
        lock (SyncLock)
        {
            if (instance is not null)
            {
                instance.Dispose();
                instance = null;
                isInitialized = false;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        // Signal shutdown
        this.cancellationTokenSource.Cancel();

        // Request Avalonia to shutdown from the UI thread
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            Dispatcher.UIThread.InvokeAsync(() => lifetime.Shutdown());
        }

        // Wait for the thread to complete
        this.avaloniaThread.Join(ShutdownTimeout);

        this.startedEvent.Dispose();
        this.cancellationTokenSource.Dispose();
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<DebugApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private void Start()
    {
        this.avaloniaThread.Start();

        // Wait for Avalonia to be fully initialized before returning
        if (!this.startedEvent.Wait(InitializationTimeout))
        {
            throw new TimeoutException("Avalonia failed to initialize within the timeout period.");
        }
    }

    private void RunAvalonia()
    {
        try
        {
            BuildAvaloniaApp()
                .AfterSetup(_ =>
                {
                    // Signal that Avalonia is ready
                    this.startedEvent.Set();
                })
                .StartWithClassicDesktopLifetime([], ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Avalonia initialization failed: {ex.Message}");
            this.startedEvent.Set(); // Unblock caller even on failure
        }
    }
}