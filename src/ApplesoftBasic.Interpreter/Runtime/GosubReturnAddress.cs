// <copyright file="GosubReturnAddress.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents a return address for a GOSUB statement in the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// This class encapsulates the line and statement indices that define the return point for a GOSUB statement.
/// It is used in conjunction with the <see cref="GosubManager"/> to manage the GOSUB/RETURN stack.
/// </remarks>
public class GosubReturnAddress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GosubReturnAddress"/> class with the specified line and statement indices.
    /// </summary>
    /// <param name="lineIndex">The index of the line where the GOSUB statement is located.</param>
    /// <param name="statementIndex">The index of the statement within the line where the GOSUB statement is located.</param>
    public GosubReturnAddress(int lineIndex, int statementIndex)
    {
        LineIndex = lineIndex;
        StatementIndex = statementIndex;
    }

    /// <summary>
    /// Gets the index of the line where the GOSUB statement is located.
    /// </summary>
    /// <value>
    /// The zero-based index of the line in the program.
    /// </value>
    /// <remarks>
    /// This property is used to identify the return point for a GOSUB statement
    /// in the Applesoft BASIC interpreter.
    /// </remarks>
    public int LineIndex { get; }

    /// <summary>
    /// Gets the index of the statement within the line where the GOSUB statement is located.
    /// </summary>
    /// <remarks>
    /// This property is used to identify the specific statement in a line to which control should return
    /// after a RETURN statement is executed in the Applesoft BASIC interpreter.
    /// </remarks>
    public int StatementIndex { get; }
}