// <copyright file="IBasicIO.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.IO;

using Emulation;

/// <summary>
/// Interface for BASIC I/O operations.
/// </summary>
public interface IBasicIO
{
    /// <summary>
    /// Writes output (PRINT).
    /// </summary>
    void Write(string text);

    /// <summary>
    /// Writes a line (PRINT with newline).
    /// </summary>
    void WriteLine(string text = "");

    /// <summary>
    /// Reads a line of input (INPUT).
    /// </summary>
    /// <returns></returns>
    string ReadLine(string? prompt = null);

    /// <summary>
    /// Reads a single character (GET).
    /// </summary>
    /// <returns></returns>
    char ReadChar();

    /// <summary>
    /// Clears the screen (HOME).
    /// </summary>
    void ClearScreen();

    /// <summary>
    /// Sets cursor position.
    /// </summary>
    void SetCursorPosition(int column, int row);

    /// <summary>
    /// Gets current cursor column (for POS function).
    /// </summary>
    /// <returns></returns>
    int GetCursorColumn();

    /// <summary>
    /// Gets current cursor row.
    /// </summary>
    /// <returns></returns>
    int GetCursorRow();

    /// <summary>
    /// Sets text output mode (NORMAL, INVERSE, FLASH).
    /// </summary>
    void SetTextMode(TextMode mode);

    /// <summary>
    /// Produces a beep.
    /// </summary>
    void Beep();

    /// <summary>
    /// Sets the speaker instance for audio output.
    /// </summary>
    void SetSpeaker(IAppleSpeaker? speaker);
}