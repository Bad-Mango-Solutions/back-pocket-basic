// <copyright file="GlyphEditHistoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

using BadMango.Emulator.Debug.UI.Editor.Models;

/// <summary>
/// Tests for the <see cref="GlyphEditHistory"/> class.
/// </summary>
[TestFixture]
public class GlyphEditHistoryTests
{
    /// <summary>
    /// Verifies that a new history has no undo/redo available.
    /// </summary>
    [Test]
    public void NewHistoryHasNoUndoRedo()
    {
        var history = new GlyphEditHistory();

        Assert.Multiple(() =>
        {
            Assert.That(history.CanUndo, Is.False);
            Assert.That(history.CanRedo, Is.False);
            Assert.That(history.UndoCount, Is.EqualTo(0));
            Assert.That(history.RedoCount, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that recording state enables undo.
    /// </summary>
    [Test]
    public void RecordStateEnablesUndo()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        history.RecordState(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(history.CanUndo, Is.True);
            Assert.That(history.UndoCount, Is.EqualTo(1));
            Assert.That(history.CanRedo, Is.False);
        });
    }

    /// <summary>
    /// Verifies that undo restores the previous state.
    /// </summary>
    [Test]
    public void UndoRestoresPreviousState()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        // Initial state: pixel off
        history.RecordState(glyph);

        // Change: pixel on
        glyph[3, 4] = true;

        // Undo should restore pixel off
        bool undone = history.Undo(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(undone, Is.True);
            Assert.That(glyph[3, 4], Is.False);
        });
    }

    /// <summary>
    /// Verifies that undo enables redo.
    /// </summary>
    [Test]
    public void UndoEnablesRedo()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        history.RecordState(glyph);
        glyph[3, 4] = true;
        history.Undo(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(history.CanRedo, Is.True);
            Assert.That(history.RedoCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that redo restores the undone state.
    /// </summary>
    [Test]
    public void RedoRestoresUndonState()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        // Initial state: pixel off
        history.RecordState(glyph);

        // Change: pixel on
        glyph[3, 4] = true;

        // Undo, then redo
        history.Undo(glyph);
        bool redone = history.Redo(glyph);

        Assert.Multiple(() =>
        {
            Assert.That(redone, Is.True);
            Assert.That(glyph[3, 4], Is.True);
        });
    }

    /// <summary>
    /// Verifies that recording state clears redo stack.
    /// </summary>
    [Test]
    public void RecordStateClearsRedoStack()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        history.RecordState(glyph);
        glyph[3, 4] = true;
        history.Undo(glyph);

        // Record new state - should clear redo
        history.RecordState(glyph);

        Assert.That(history.CanRedo, Is.False);
    }

    /// <summary>
    /// Verifies that Clear removes all history.
    /// </summary>
    [Test]
    public void ClearRemovesAllHistory()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        history.RecordState(glyph);
        history.RecordState(glyph);
        history.Undo(glyph);

        history.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(history.CanUndo, Is.False);
            Assert.That(history.CanRedo, Is.False);
            Assert.That(history.UndoCount, Is.EqualTo(0));
            Assert.That(history.RedoCount, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that undo returns false when stack is empty.
    /// </summary>
    [Test]
    public void UndoReturnsFalseWhenEmpty()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        bool result = history.Undo(glyph);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that redo returns false when stack is empty.
    /// </summary>
    [Test]
    public void RedoReturnsFalseWhenEmpty()
    {
        var history = new GlyphEditHistory();
        var glyph = new CharacterGlyph();

        bool result = history.Redo(glyph);

        Assert.That(result, Is.False);
    }
}