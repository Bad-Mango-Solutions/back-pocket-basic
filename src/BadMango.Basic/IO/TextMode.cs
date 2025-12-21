// <copyright file="TextMode.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.IO;

/// <summary>
/// Specifies the text display modes for the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// These modes determine how text is visually represented during output operations.
/// </remarks>
public enum TextMode
{
    /// <summary>
    /// Represents the normal text output mode.
    /// </summary>
    /// <remarks>
    /// In this mode, text is displayed with the default appearance, without any special effects
    /// such as inversion or flashing. It is the standard mode for text output in the Applesoft BASIC interpreter.
    /// </remarks>
    Normal,

    /// <summary>
    /// Represents the text mode where the colors of the text and background are inverted.
    /// </summary>
    /// <remarks>
    /// When this mode is active, the text is displayed with inverted colors, which can be used
    /// to highlight or emphasize specific content. The exact appearance depends on the
    /// implementation of the output system.
    /// </remarks>
    Inverse,

    /// <summary>
    /// Represents a text output mode where the text flashes on the screen.
    /// </summary>
    /// <remarks>
    /// This mode is typically used to draw attention to specific text by alternating
    /// its visibility or appearance. The exact behavior of the flashing effect may
    /// depend on the implementation of the output system.
    /// </remarks>
    Flash,
}