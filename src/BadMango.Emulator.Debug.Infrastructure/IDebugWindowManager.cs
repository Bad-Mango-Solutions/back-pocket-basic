// <copyright file="IDebugWindowManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

/// <summary>
/// Manages debug popup windows for the console debugger REPL.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides an abstraction for launching and managing Avalonia popup
/// windows from the debug console without creating circular dependencies between
/// the debug infrastructure and UI implementations.
/// </para>
/// <para>
/// Implementations must handle the Avalonia threading model, ensuring that UI
/// operations are dispatched to the appropriate thread while keeping the REPL
/// non-blocking.
/// </para>
/// <para>
/// The interface follows async patterns to allow the REPL to remain responsive
/// while windows are being created or shown.
/// </para>
/// </remarks>
/// <seealso href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/video/Pocket2e%20Debug%20Video%20Window%20(Avalonia)%20%E2%80%94%20Specification.md#8-threading-model">
/// Threading Model Specification
/// </seealso>
public interface IDebugWindowManager
{
    /// <summary>
    /// Gets a value indicating whether the Avalonia application is running.
    /// </summary>
    /// <remarks>
    /// This property can be used to check if windows can be shown before
    /// attempting to call <see cref="ShowWindowAsync"/>.
    /// </remarks>
    bool IsAvaloniaRunning { get; }

    /// <summary>
    /// Shows a debug popup window of the specified type.
    /// </summary>
    /// <param name="windowType">The type of window to show.</param>
    /// <returns>
    /// A task that completes when the window has been shown.
    /// Returns <c>true</c> if the window was successfully shown;
    /// <c>false</c> if the operation failed (e.g., Avalonia not running).
    /// </returns>
    /// <remarks>
    /// <para>
    /// If a window of the specified type is already open, it will be
    /// brought to the front instead of creating a new instance.
    /// </para>
    /// <para>
    /// The implementation must ensure thread safety and dispatch UI
    /// operations to the Avalonia UI thread.
    /// </para>
    /// </remarks>
    Task<bool> ShowWindowAsync(string windowType);

    /// <summary>
    /// Shows a debug popup window of the specified type with context data.
    /// </summary>
    /// <param name="windowType">The type of window to show.</param>
    /// <param name="context">
    /// Optional context data to pass to the window. The window type determines
    /// what data types are accepted and how they are used.
    /// </param>
    /// <returns>
    /// A task that completes when the window has been shown.
    /// Returns <c>true</c> if the window was successfully shown;
    /// <c>false</c> if the operation failed (e.g., Avalonia not running).
    /// </returns>
    /// <remarks>
    /// <para>
    /// If a window of the specified type is already open, it will be
    /// brought to the front and updated with the new context data.
    /// </para>
    /// <para>
    /// The implementation must ensure thread safety and dispatch UI
    /// operations to the Avalonia UI thread.
    /// </para>
    /// </remarks>
    Task<bool> ShowWindowAsync(string windowType, object? context);

    /// <summary>
    /// Closes a debug popup window of the specified type.
    /// </summary>
    /// <param name="windowType">The type of window to close.</param>
    /// <returns>
    /// A task that completes when the window has been closed.
    /// Returns <c>true</c> if the window was closed;
    /// <c>false</c> if no such window was open.
    /// </returns>
    Task<bool> CloseWindowAsync(string windowType);

    /// <summary>
    /// Determines whether a window of the specified type is currently open.
    /// </summary>
    /// <param name="windowType">The type of window to check.</param>
    /// <returns><c>true</c> if the window is open; otherwise, <c>false</c>.</returns>
    bool IsWindowOpen(string windowType);

    /// <summary>
    /// Gets the list of available window types that can be opened.
    /// </summary>
    /// <returns>An enumerable of available window type names.</returns>
    IEnumerable<string> GetAvailableWindowTypes();

    /// <summary>
    /// Closes all open debug windows.
    /// </summary>
    /// <returns>A task that completes when all windows have been closed.</returns>
    Task CloseAllWindowsAsync();
}