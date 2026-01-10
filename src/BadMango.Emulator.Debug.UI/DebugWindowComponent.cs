// <copyright file="DebugWindowComponent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI;

/// <summary>
/// Defines the types of debug popup windows that can be launched from the console debugger.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration supports extensibility for future debug windows such as
/// video displays, text editors, and memory inspectors.
/// </para>
/// <para>
/// Unlike <c>PopOutComponent</c> in the main UI, these windows are designed
/// specifically for console debugger workflows and can be opened independently
/// of the main emulator UI application.
/// </para>
/// </remarks>
public enum DebugWindowComponent
{
    /// <summary>
    /// The About window displaying version and copyright information.
    /// </summary>
    About,

    /// <summary>
    /// A video display window for viewing emulator graphics output.
    /// </summary>
    /// <remarks>Reserved for future implementation.</remarks>
    VideoDisplay,

    /// <summary>
    /// A text editor window for editing source code or memory.
    /// </summary>
    /// <remarks>Reserved for future implementation.</remarks>
    TextEditor,

    /// <summary>
    /// A memory viewer/editor window for inspecting and modifying memory.
    /// </summary>
    /// <remarks>Reserved for future implementation.</remarks>
    MemoryViewer,

    /// <summary>
    /// A character ROM preview window displaying the character set bitmap grid.
    /// </summary>
    CharacterPreview,
}