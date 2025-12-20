// <copyright file="ExecutionContext.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Execution;

using System.Collections.Generic;

using AST;
using Runtime;

/// <summary>
/// Manages the execution state for a BASIC program, including current position and program reference.
/// </summary>
/// <remarks>
/// This class acts as a shared state object between the interpreter orchestrator and the execution visitor,
/// avoiding circular dependencies while providing controlled access to execution state.
/// </remarks>
public class ExecutionContext
{
    private readonly Dictionary<int, int> lineNumberIndex = [];
    private ProgramNode? program;
    private int currentLineIndex;
    private int currentStatementIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
    /// </summary>
    public ExecutionContext()
    {
    }

    /// <summary>
    /// Gets the currently executing program.
    /// </summary>
    /// <returns>The current program, or null if no program is loaded.</returns>
    public ProgramNode? GetProgram() => program;

    /// <summary>
    /// Loads a program into the execution context and builds the line number index.
    /// </summary>
    /// <param name="programNode">The program to load.</param>
    public void LoadProgram(ProgramNode programNode)
    {
        program = programNode;

        // Build line number index
        lineNumberIndex.Clear();
        for (int i = 0; i < program.Lines.Count; i++)
        {
            lineNumberIndex[program.Lines[i].LineNumber] = i;
        }
    }

    /// <summary>
    /// Resets the execution position to the beginning of the program.
    /// </summary>
    public void ResetPosition()
    {
        currentLineIndex = 0;
        currentStatementIndex = 0;
    }

    /// <summary>
    /// Gets the current execution position within the program.
    /// </summary>
    /// <param name="lineIndex">Outputs the current line index.</param>
    /// <param name="statementIndex">Outputs the current statement index within the line.</param>
    public void GetExecutionPosition(out int lineIndex, out int statementIndex)
    {
        lineIndex = currentLineIndex;
        statementIndex = currentStatementIndex;
    }

    /// <summary>
    /// Sets the current execution position within the program.
    /// </summary>
    /// <param name="lineIndex">The target line index.</param>
    /// <param name="statementIndex">The target statement index within the line.</param>
    public void SetExecutionPosition(int lineIndex, int statementIndex)
    {
        currentLineIndex = lineIndex;
        currentStatementIndex = statementIndex;
    }

    /// <summary>
    /// Transfers control to the specified line number.
    /// </summary>
    /// <param name="lineNumber">The target line number to jump to.</param>
    /// <exception cref="BasicRuntimeException">Thrown if the line number does not exist.</exception>
    public void JumpToLine(int lineNumber)
    {
        if (!lineNumberIndex.TryGetValue(lineNumber, out int index))
        {
            throw new BasicRuntimeException("?UNDEF'DP STATEMENT ERROR", GetCurrentLineNumber());
        }

        currentLineIndex = index;
        currentStatementIndex = 0;
    }

    /// <summary>
    /// Gets the line number of the currently executing statement.
    /// </summary>
    /// <returns>The current line number, or 0 if no program is running.</returns>
    public int GetCurrentLineNumber()
    {
        if (program != null && currentLineIndex < program.Lines.Count)
        {
            return program.Lines[currentLineIndex].LineNumber;
        }

        return 0;
    }

    /// <summary>
    /// Advances to the next statement in the current line.
    /// </summary>
    /// <returns>True if advanced to next statement, false if end of line reached.</returns>
    public bool AdvanceStatement()
    {
        currentStatementIndex++;

        if (program != null && currentLineIndex < program.Lines.Count)
        {
            var line = program.Lines[currentLineIndex];
            if (currentStatementIndex >= line.Statements.Count)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Advances to the next line, resetting the statement index.
    /// </summary>
    /// <returns>True if advanced to next line, false if end of program reached.</returns>
    public bool AdvanceLine()
    {
        currentLineIndex++;
        currentStatementIndex = 0;

        if (program != null && currentLineIndex >= program.Lines.Count)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the current line being executed.
    /// </summary>
    /// <returns>The current line node, or null if no program is loaded or end of program reached.</returns>
    public LineNode? GetCurrentLine()
    {
        if (program != null && currentLineIndex < program.Lines.Count)
        {
            return program.Lines[currentLineIndex];
        }

        return null;
    }

    /// <summary>
    /// Gets the current statement being executed.
    /// </summary>
    /// <returns>The current statement, or null if no statement is available.</returns>
    public IStatement? GetCurrentStatement()
    {
        if (program != null && currentLineIndex < program.Lines.Count)
        {
            var line = program.Lines[currentLineIndex];
            if (currentStatementIndex < line.Statements.Count)
            {
                return line.Statements[currentStatementIndex];
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the execution has reached the end of the program.
    /// </summary>
    /// <returns>True if at end of program, false otherwise.</returns>
    public bool IsAtEnd()
    {
        if (program == null)
        {
            return true;
        }

        return currentLineIndex >= program.Lines.Count;
    }
}