// <copyright file="IBasicIO.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.IO;

using Emulation;

/// <summary>
/// Defines an interface for handling input and output operations in the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This interface provides methods for performing various I/O operations such as writing text,
/// reading input, managing cursor position, and controlling text display modes. It also includes
/// support for audio output and screen management.
/// </remarks>
public interface IBasicIO
{
    /// <summary>
    /// Writes the specified text to the output.
    /// </summary>
    /// <param name="text">The text to be written to the output.</param>
    /// <remarks>
    /// This method processes the provided text and outputs it according to the current
    /// text mode and cursor position. Control characters within the text may be handled
    /// differently depending on the implementation.
    /// </remarks>
    void Write(string text);

    /// <summary>
    /// Writes a line of text to the output, followed by a newline character.
    /// </summary>
    /// <param name="text">
    /// The text to be written to the output. If not specified, an empty string is written.
    /// </param>
    /// <remarks>
    /// This method appends a newline character after the specified text. If the current text mode
    /// is set to <see cref="TextMode.Inverse"/>, the text may be displayed with inverted colors.
    /// After writing, the cursor column is typically reset to zero.
    /// </remarks>
    void WriteLine(string text = "");

    /// <summary>
    /// Reads a line of input from the user, optionally displaying a prompt.
    /// </summary>
    /// <param name="prompt">
    /// An optional string to display as a prompt before reading the input.
    /// If <c>null</c> or empty, no prompt is displayed.
    /// </param>
    /// <returns>
    /// The input string entered by the user. If no input is provided, an empty string is returned.
    /// </returns>
    /// <remarks>
    /// This method is typically used to capture user input during program execution.
    /// It supports optional prompts to guide the user in providing the expected input.
    /// </remarks>
    string ReadLine(string? prompt = null);

    /// <summary>
    /// Reads a single character input from the input source.
    /// </summary>
    /// <returns>The character read from the input source.</returns>
    /// <remarks>
    /// This method is typically used to capture a single character input without requiring the user to press Enter.
    /// The behavior of this method may vary depending on the implementation of the <see cref="IBasicIO"/> interface.
    /// </remarks>
    char ReadChar();

    /// <summary>
    /// Clears the screen and resets the cursor position to the top-left corner.
    /// </summary>
    /// <remarks>
    /// This method is typically used to reset the display in the Applesoft BASIC interpreter,
    /// simulating the behavior of the HOME command. Implementations may vary depending on the
    /// underlying output mechanism, but the goal is to provide a clean screen for subsequent output.
    /// </remarks>
    void ClearScreen();

    /// <summary>
    /// Sets the cursor position on the output device.
    /// </summary>
    /// <param name="column">
    /// The 1-based column position to set the cursor to. Values less than 1 may be clamped to the minimum allowed position
    /// depending on the implementation.
    /// </param>
    /// <param name="row">
    /// The 1-based row position to set the cursor to. Values less than 1 may be clamped to the minimum allowed position
    /// depending on the implementation.
    /// </param>
    /// <remarks>
    /// The behavior of this method may vary depending on the implementation of the <see cref="IBasicIO"/> interface.
    /// Implementations may adjust the provided 1-based coordinates to match the internal coordinate system used by the
    /// output device. If the specified position exceeds the dimensions of the output device, it may be clamped to the
    /// maximum allowed position. Errors during cursor positioning may be handled differently depending on the implementation.
    /// </remarks>
    void SetCursorPosition(int column, int row);

    /// <summary>
    /// Retrieves the current cursor column position.
    /// </summary>
    /// <returns>
    /// The zero-based column index of the cursor's current position.
    /// </returns>
    int GetCursorColumn();

    /// <summary>
    /// Retrieves the current row position of the cursor.
    /// </summary>
    /// <returns>
    /// The zero-based row position of the cursor.
    /// </returns>
    /// <remarks>
    /// This method provides the current vertical position of the cursor, which can be used
    /// for operations that depend on the cursor's location, such as text alignment or screen updates.
    /// </remarks>
    int GetCursorRow();

    /// <summary>
    /// Sets the text output mode for the Applesoft BASIC interpreter.
    /// </summary>
    /// <param name="mode">
    /// The <see cref="TextMode"/> to set. This specifies how text is displayed,
    /// such as in normal, inverse, or flashing mode.
    /// </param>
    /// <remarks>
    /// Changing the text mode affects the visual representation of text output.
    /// For example, <see cref="TextMode.Normal"/> restores the default text appearance,
    /// while <see cref="TextMode.Inverse"/> or <see cref="TextMode.Flash"/> alters
    /// the display for emphasis or special effects.
    /// </remarks>
    void SetTextMode(TextMode mode);

    /// <summary>
    /// Emits a beep sound to signal an event or alert the user.
    /// </summary>
    /// <remarks>
    /// The implementation of this method may vary depending on the platform and configuration.
    /// If an <see cref="IAppleSpeaker"/> instance is set, it may use the Apple II speaker emulation
    /// to produce the sound. Otherwise, it may fall back to a system-specific beep mechanism.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown if the beep functionality is not supported in the current environment.
    /// </exception>
    void Beep();

    /// <summary>
    /// Sets the speaker instance for audio output.
    /// </summary>
    /// <param name="speaker">
    /// An instance of <see cref="IAppleSpeaker"/> to be used for audio output, or <c>null</c> to disable speaker functionality.
    /// </param>
    /// <remarks>
    /// This method allows the integration of an <see cref="IAppleSpeaker"/> instance for handling audio output
    /// in the Applesoft BASIC interpreter. Passing <c>null</c> disables audio output.
    /// </remarks>
    void SetSpeaker(IAppleSpeaker? speaker);
}