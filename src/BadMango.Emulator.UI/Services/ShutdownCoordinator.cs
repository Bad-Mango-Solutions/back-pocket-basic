// <copyright file="ShutdownCoordinator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

using Avalonia.Controls.ApplicationLifetimes;

using Microsoft.Extensions.Logging;

/// <summary>
/// Default implementation of <see cref="IShutdownCoordinator"/> for handling application shutdown.
/// </summary>
public class ShutdownCoordinator : IShutdownCoordinator
{
    private readonly IWindowManager windowManager;
    private readonly ILogger<ShutdownCoordinator>? logger;
    private readonly List<Func<Task<IReadOnlyList<UnsavedWorkItem>>>> unsavedWorkProviders = [];
    private readonly object providersLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ShutdownCoordinator"/> class.
    /// </summary>
    /// <param name="windowManager">The window manager for closing pop-out windows.</param>
    /// <param name="logger">Optional logger for shutdown operations.</param>
    public ShutdownCoordinator(IWindowManager windowManager, ILogger<ShutdownCoordinator>? logger = null)
    {
        this.windowManager = windowManager;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> RequestShutdownAsync()
    {
        logger?.LogInformation("Shutdown requested");

        var unsavedWork = await GetUnsavedWorkAsync().ConfigureAwait(false);
        if (unsavedWork.Count > 0)
        {
            logger?.LogInformation("Found {Count} items with unsaved work", unsavedWork.Count);

            // In a real implementation, this would show a dialog to the user
            // For now, we allow shutdown but log the unsaved items
            foreach (var item in unsavedWork)
            {
                logger?.LogWarning("Unsaved work: {Name} - {Description}", item.Name, item.Description);
            }
        }

        // Close all pop-out windows
        await windowManager.CloseAllWindowsAsync().ConfigureAwait(false);

        logger?.LogInformation("Shutdown proceeding");
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnsavedWorkItem>> GetUnsavedWorkAsync()
    {
        var allUnsavedWork = new List<UnsavedWorkItem>();

        List<Func<Task<IReadOnlyList<UnsavedWorkItem>>>> providersCopy;
        lock (providersLock)
        {
            providersCopy = [.. unsavedWorkProviders];
        }

        foreach (var provider in providersCopy)
        {
            try
            {
                var items = await provider().ConfigureAwait(false);
                allUnsavedWork.AddRange(items);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting unsaved work from provider");
            }
        }

        return allUnsavedWork;
    }

    /// <inheritdoc />
    public void ForceShutdown()
    {
        logger?.LogWarning("Force shutdown requested");

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    /// <inheritdoc />
    public IDisposable RegisterUnsavedWorkProvider(Func<Task<IReadOnlyList<UnsavedWorkItem>>> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (providersLock)
        {
            unsavedWorkProviders.Add(provider);
        }

        logger?.LogDebug("Registered unsaved work provider");

        return new ProviderRegistration(this, provider);
    }

    private void UnregisterProvider(Func<Task<IReadOnlyList<UnsavedWorkItem>>> provider)
    {
        lock (providersLock)
        {
            unsavedWorkProviders.Remove(provider);
        }

        logger?.LogDebug("Unregistered unsaved work provider");
    }

    private sealed class ProviderRegistration : IDisposable
    {
        private readonly ShutdownCoordinator coordinator;
        private readonly Func<Task<IReadOnlyList<UnsavedWorkItem>>> provider;
        private bool disposed;

        public ProviderRegistration(ShutdownCoordinator coordinator, Func<Task<IReadOnlyList<UnsavedWorkItem>>> provider)
        {
            this.coordinator = coordinator;
            this.provider = provider;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                coordinator.UnregisterProvider(provider);
                disposed = true;
            }
        }
    }
}