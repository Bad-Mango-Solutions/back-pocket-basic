// <copyright file="GlyphEditHistory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Models;

/// <summary>
/// Manages undo/redo history for a single character glyph.
/// </summary>
/// <remarks>
/// Each character has its own history, allowing undo/redo operations
/// to be preserved when switching between characters.
/// </remarks>
public sealed class GlyphEditHistory
{
    /// <summary>
    /// The maximum number of undo states to retain per character.
    /// </summary>
    public const int MaxHistoryDepth = 50;

    private readonly Stack<byte[]> undoStack = new();
    private readonly Stack<byte[]> redoStack = new();

    /// <summary>
    /// Gets a value indicating whether undo is available.
    /// </summary>
    public bool CanUndo => undoStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether redo is available.
    /// </summary>
    public bool CanRedo => redoStack.Count > 0;

    /// <summary>
    /// Gets the number of states in the undo stack.
    /// </summary>
    public int UndoCount => undoStack.Count;

    /// <summary>
    /// Gets the number of states in the redo stack.
    /// </summary>
    public int RedoCount => redoStack.Count;

    /// <summary>
    /// Records the current state before a modification.
    /// </summary>
    /// <param name="glyph">The glyph about to be modified.</param>
    public void RecordState(CharacterGlyph glyph)
    {
        ArgumentNullException.ThrowIfNull(glyph);

        var state = new byte[CharacterGlyph.Height];
        glyph.CopyTo(state);
        undoStack.Push(state);
        redoStack.Clear();

        // Limit history depth by removing oldest entries
        TrimHistory();
    }

    /// <summary>
    /// Undoes the last modification.
    /// </summary>
    /// <param name="glyph">The glyph to restore.</param>
    /// <returns>True if undo was performed; otherwise, false.</returns>
    public bool Undo(CharacterGlyph glyph)
    {
        ArgumentNullException.ThrowIfNull(glyph);

        if (undoStack.Count == 0)
        {
            return false;
        }

        // Save current state for redo
        var currentState = new byte[CharacterGlyph.Height];
        glyph.CopyTo(currentState);
        redoStack.Push(currentState);

        // Restore previous state
        var previousState = undoStack.Pop();
        glyph.CopyFrom(previousState);

        return true;
    }

    /// <summary>
    /// Redoes the last undone modification.
    /// </summary>
    /// <param name="glyph">The glyph to restore.</param>
    /// <returns>True if redo was performed; otherwise, false.</returns>
    public bool Redo(CharacterGlyph glyph)
    {
        ArgumentNullException.ThrowIfNull(glyph);

        if (redoStack.Count == 0)
        {
            return false;
        }

        // Save current state for undo
        var currentState = new byte[CharacterGlyph.Height];
        glyph.CopyTo(currentState);
        undoStack.Push(currentState);

        // Restore redo state
        var redoState = redoStack.Pop();
        glyph.CopyFrom(redoState);

        return true;
    }

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    private void TrimHistory()
    {
        while (undoStack.Count > MaxHistoryDepth)
        {
            // Convert to array, remove oldest (bottom), and rebuild stack
            var items = undoStack.ToArray();
            undoStack.Clear();

            // Push back in reverse order, skipping the oldest (last in array)
            for (int i = items.Length - 2; i >= 0; i--)
            {
                undoStack.Push(items[i]);
            }
        }
    }
}