// <copyright file="PopOutComponent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions;

/// <summary>
/// Defines the types of components that can be detached into pop-out windows.
/// </summary>
public enum PopOutComponent
{
    /// <summary>
    /// The video display component showing emulator output.
    /// </summary>
    VideoDisplay,

    /// <summary>
    /// The debug console for debugging running instances.
    /// </summary>
    DebugConsole,

    /// <summary>
    /// The assembly language editor.
    /// </summary>
    AssemblyEditor,

    /// <summary>
    /// The hex editor for memory and disk editing.
    /// </summary>
    HexEditor,
}