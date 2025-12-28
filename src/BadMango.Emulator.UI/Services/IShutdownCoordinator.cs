// <copyright file="IShutdownCoordinator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Services;

/// <summary>
/// Handles application shutdown sequence, coordinating between windows and services.
/// </summary>
public interface IShutdownCoordinator
{
    /// <summary>
    /// Initiates a graceful shutdown sequence.
    /// </summary>
    /// <returns>True if shutdown should proceed, false if cancelled by user.</returns>
    Task<bool> RequestShutdownAsync();

    /// <summary>
    /// Gets all items with unsaved work across all windows.
    /// </summary>
    /// <returns>A list of unsaved work items.</returns>
    Task<IReadOnlyList<UnsavedWorkItem>> GetUnsavedWorkAsync();

    /// <summary>
    /// Forces immediate shutdown without prompts or saving.
    /// </summary>
    void ForceShutdown();

    /// <summary>
    /// Registers a callback to check for unsaved work.
    /// </summary>
    /// <param name="provider">The provider function that returns unsaved work items.</param>
    /// <returns>A disposable that unregisters the provider when disposed.</returns>
    IDisposable RegisterUnsavedWorkProvider(Func<Task<IReadOnlyList<UnsavedWorkItem>>> provider);
}