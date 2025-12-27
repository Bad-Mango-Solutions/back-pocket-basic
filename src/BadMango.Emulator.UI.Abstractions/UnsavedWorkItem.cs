// <copyright file="UnsavedWorkItem.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions;

/// <summary>
/// Represents an item with unsaved work that should be handled during shutdown.
/// </summary>
public record UnsavedWorkItem
{
    private readonly string name = string.Empty;
    private readonly string description = string.Empty;

    /// <summary>
    /// Gets the display name of the unsaved item.
    /// </summary>
    /// <remarks>
    /// This value is required and must not be empty or consist only of whitespace characters.
    /// </remarks>
    public required string Name
    {
        get => name;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Name));
            name = value;
        }
    }

    /// <summary>
    /// Gets a description of the unsaved changes.
    /// </summary>
    /// <remarks>
    /// This value is required and must not be empty or consist only of whitespace characters.
    /// </remarks>
    public required string Description
    {
        get => description;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Description));
            description = value;
        }
    }

    /// <summary>
    /// Gets the component type containing the unsaved work.
    /// </summary>
    public PopOutComponent? ComponentType { get; init; }

    /// <summary>
    /// Gets the window ID containing the unsaved work.
    /// </summary>
    public string? WindowId { get; init; }
}