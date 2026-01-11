// <copyright file="UnsavedChangesResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor;

/// <summary>
/// Result of the unsaved changes dialog.
/// </summary>
internal enum UnsavedChangesResult
{
    /// <summary>
    /// Save changes before closing.
    /// </summary>
    Save,

    /// <summary>
    /// Discard changes and close.
    /// </summary>
    Discard,

    /// <summary>
    /// Cancel the close operation.
    /// </summary>
    Cancel,
}