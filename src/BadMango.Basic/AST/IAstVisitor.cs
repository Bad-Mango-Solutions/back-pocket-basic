// <copyright file="IAstVisitor.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.AST;

/// <summary>
/// Defines a visitor interface for traversing and processing nodes in the Abstract Syntax Tree (AST)
/// of an Applesoft BASIC program. This interface follows the Visitor design pattern, allowing
/// operations to be performed on various types of AST nodes without modifying their structure.
/// </summary>
/// <typeparam name="T">
/// The type of the result produced by the visitor methods when processing AST nodes.
/// </typeparam>
public interface IAstVisitor<out T>
{
    // Statements

    /// <summary>
    /// Visits the root node of the Abstract Syntax Tree (AST) for an Applesoft BASIC program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="ProgramNode"/> representing the entire Applesoft BASIC program,
    /// including its lines and associated data values.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="ProgramNode"/>.
    /// </returns>
    /// <remarks>
    /// This method is part of the Visitor design pattern, enabling operations to be performed
    /// on the <see cref="ProgramNode"/> without modifying its structure.
    /// </remarks>
    T VisitProgram(ProgramNode node);

    /// <summary>
    /// Visits a <see cref="LineNode"/>, representing a single numbered line in the Applesoft BASIC program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="LineNode"/> instance to be visited, containing the line number and associated statements.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="LineNode"/>.
    /// </returns>
    T VisitLine(LineNode node);

    /// <summary>
    /// Visits a <see cref="PrintStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="PrintStatement"/> node to process, which represents a PRINT statement
    /// in an Applesoft BASIC program. This node contains the expressions to be printed,
    /// separators between them, and a flag indicating whether the statement ends with a separator.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="PrintStatement"/> node.
    /// </returns>
    T VisitPrintStatement(PrintStatement node);

    /// <summary>
    /// Visits an <see cref="InputStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="InputStatement"/> node to be processed. This node represents
    /// an Applesoft BASIC INPUT statement, which prompts the user for input and assigns
    /// the entered values to specified variables.
    /// </param>
    /// <returns>
    /// A value of type <typeparamref name="T"/> that represents the result of processing
    /// the <see cref="InputStatement"/> node.
    /// </returns>
    T VisitInputStatement(InputStatement node);

    /// <summary>
    /// Visits a <see cref="LetStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the LET assignment statement, which assigns a value to a variable
    /// or an array element. The LET keyword is optional in Applesoft BASIC.
    /// </summary>
    /// <param name="node">
    /// The <see cref="LetStatement"/> node representing the assignment statement to be processed.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="LetStatement"/> node.
    /// </returns>
    T VisitLetStatement(LetStatement node);

    /// <summary>
    /// Visits an <see cref="IfStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="IfStatement"/> node to visit, representing an IF-THEN statement in the Applesoft BASIC program.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="IfStatement"/> node.
    /// </returns>
    T VisitIfStatement(IfStatement node);

    /// <summary>
    /// Visits a <see cref="GotoStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="GotoStatement"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="GotoStatement"/> node.
    /// </returns>
    T VisitGotoStatement(GotoStatement node);

    /// <summary>
    /// Visits a <see cref="GosubStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the GOSUB statement, which transfers control to a specified
    /// line number in the Applesoft BASIC program and saves the return address for later use.
    /// </summary>
    /// <param name="node">
    /// The <see cref="GosubStatement"/> node representing the GOSUB statement in the AST.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/>, representing the outcome of processing
    /// the GOSUB statement.
    /// </returns>
    T VisitGosubStatement(GosubStatement node);

    /// <summary>
    /// Visits a <see cref="ReturnStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="ReturnStatement"/> node to be processed by the visitor.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="ReturnStatement"/> node.
    /// </returns>
    T VisitReturnStatement(ReturnStatement node);

    /// <summary>
    /// Visits a <see cref="ForStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="ForStatement"/> node to be visited.</param>
    /// <returns>
    /// The result of processing the <see cref="ForStatement"/> node, as determined by the implementation
    /// of the visitor.
    /// </returns>
    T VisitForStatement(ForStatement node);

    /// <summary>
    /// Visits a <see cref="NextStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="NextStatement"/> node to be processed. This node represents
    /// a NEXT statement in an Applesoft BASIC program, which is used to advance
    /// the execution of a FOR loop.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the NEXT statement.
    /// </returns>
    T VisitNextStatement(NextStatement node);

    /// <summary>
    /// Visits a <see cref="DimStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method is responsible for processing DIM statements, which declare arrays
    /// in an Applesoft BASIC program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="DimStatement"/> node representing the DIM statement to be processed.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the DIM statement.
    /// </returns>
    T VisitDimStatement(DimStatement node);

    /// <summary>
    /// Visits a <see cref="ReadStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the READ statement, which assigns values from the DATA statements
    /// to the specified variables in the program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="ReadStatement"/> node representing the READ statement in the AST.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the READ statement.
    /// </returns>
    T VisitReadStatement(ReadStatement node);

    /// <summary>
    /// Visits a <see cref="DataStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="DataStatement"/> node to be visited, representing a DATA statement
    /// in an Applesoft BASIC program.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="DataStatement"/> node.
    /// </returns>
    /// <remarks>
    /// The DATA statement defines a list of values that can be read by subsequent READ statements.
    /// These values are typically processed during the parsing phase.
    /// </remarks>
    T VisitDataStatement(DataStatement node);

    /// <summary>
    /// Visits a <see cref="RestoreStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the RESTORE statement, which resets the data pointer
    /// to the beginning of the DATA statements in the program or to a specified line number.
    /// </summary>
    /// <param name="node">
    /// The <see cref="RestoreStatement"/> node to be visited and processed.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the RESTORE statement.
    /// </returns>
    T VisitRestoreStatement(RestoreStatement node);

    /// <summary>
    /// Visits an <see cref="EndStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method is invoked to process the END statement, which signifies the termination
    /// of program execution in Applesoft BASIC.
    /// </summary>
    /// <param name="node">The <see cref="EndStatement"/> node to be visited.</param>
    /// <returns>The result of processing the END statement, as defined by the visitor's implementation.</returns>
    T VisitEndStatement(EndStatement node);

    /// <summary>
    /// Visits a <see cref="StopStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="StopStatement"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="StopStatement"/> node.
    /// </returns>
    T VisitStopStatement(StopStatement node);

    /// <summary>
    /// Visits a <see cref="RemStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="RemStatement"/> node representing a REM (comment) statement.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the REM statement.
    /// Typically, this result is implementation-specific and may represent a no-op or a default value
    /// since REM statements do not affect program execution.
    /// </returns>
    T VisitRemStatement(RemStatement node);

    /// <summary>
    /// Visits a <see cref="PokeStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="PokeStatement"/> node to be processed, representing a POKE statement in the Applesoft BASIC program.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="PokeStatement"/> node.
    /// </returns>
    T VisitPokeStatement(PokeStatement node);

    /// <summary>
    /// Visits a <see cref="CallStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="CallStatement"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="CallStatement"/> node.
    /// </returns>
    /// <remarks>
    /// The <see cref="CallStatement"/> represents a CALL statement in Applesoft BASIC,
    /// which is used to invoke a machine language subroutine at a specified memory address.
    /// </remarks>
    T VisitCallStatement(CallStatement node);

    /// <summary>
    /// Visits a <see cref="GetStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the GET statement, which is used for single-character input
    /// in Applesoft BASIC programs.
    /// </summary>
    /// <param name="node">
    /// The <see cref="GetStatement"/> node to be visited. This node represents the GET statement
    /// and contains the variable where the input character will be stored.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/>, representing the outcome of processing the GET statement.
    /// </returns>
    T VisitGetStatement(GetStatement node);

    /// <summary>
    /// Visits an <see cref="OnGotoStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="OnGotoStatement"/> node to be processed. This node represents an ON ... GOTO statement
    /// in Applesoft BASIC, which conditionally transfers control to one of several line numbers based on
    /// the evaluated value of an expression.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="OnGotoStatement"/> node.
    /// </returns>
    T VisitOnGotoStatement(OnGotoStatement node);

    /// <summary>
    /// Visits an <see cref="OnGosubStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the ON ... GOSUB statement, which evaluates an expression
    /// and performs a subroutine call to one of the specified line numbers based on the result.
    /// </summary>
    /// <param name="node">
    /// The <see cref="OnGosubStatement"/> node representing the ON ... GOSUB statement to be visited.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="OnGosubStatement"/> node.
    /// </returns>
    T VisitOnGosubStatement(OnGosubStatement node);

    /// <summary>
    /// Visits a <see cref="DefStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes a DEF FN statement, which defines a user-defined function
    /// in an Applesoft BASIC program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="DefStatement"/> node representing the user-defined function,
    /// including its name, parameter, and body expression.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="DefStatement"/> node.
    /// </returns>
    T VisitDefStatement(DefStatement node);

    /// <summary>
    /// Visits a <see cref="HomeStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="HomeStatement"/> node to be visited.</param>
    /// <returns>
    /// The result of processing the <see cref="HomeStatement"/> node, as defined by the implementation
    /// of the visitor.
    /// </returns>
    T VisitHomeStatement(HomeStatement node);

    /// <summary>
    /// Visits an <see cref="HtabStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the HTAB statement, which is used to set the horizontal
    /// tab position in the Applesoft BASIC program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="HtabStatement"/> node representing the HTAB statement in the AST.
    /// </param>
    /// <returns>
    /// A value of type <typeparamref name="T"/> that represents the result of processing
    /// the HTAB statement.
    /// </returns>
    T VisitHtabStatement(HtabStatement node);

    /// <summary>
    /// Visits a <see cref="VtabStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="VtabStatement"/> node to visit, representing a vertical tab (VTAB) statement.</param>
    /// <returns>
    /// The result of processing the <see cref="VtabStatement"/> node, as determined by the implementation of the visitor.
    /// </returns>
    T VisitVtabStatement(VtabStatement node);

    /// <summary>
    /// Processes a <see cref="TextStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="TextStatement"/> node representing a TEXT statement, which switches the display to text mode.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="TextStatement"/> node.
    /// </returns>
    T VisitTextStatement(TextStatement node);

    /// <summary>
    /// Visits a <see cref="GrStatement"/> node in the Abstract Syntax Tree (AST),
    /// representing the GR statement in Applesoft BASIC, which activates the low-resolution graphics mode.
    /// </summary>
    /// <param name="node">The <see cref="GrStatement"/> node to process.</param>
    /// <returns>The result of the visitor's operation, as defined by the implementation.</returns>
    T VisitGrStatement(GrStatement node);

    /// <summary>
    /// Visits an <see cref="HgrStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the HGR statement, which is used to activate high-resolution
    /// graphics mode in Applesoft BASIC. The mode can be either standard high-resolution
    /// graphics (HGR) or extended high-resolution graphics (HGR2), depending on the
    /// <see cref="HgrStatement.IsHgr2"/> property.
    /// </summary>
    /// <param name="node">
    /// The <see cref="HgrStatement"/> node representing the HGR statement in the AST.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the HGR statement.
    /// </returns>
    T VisitHgrStatement(HgrStatement node);

    /// <summary>
    /// Visits a <see cref="ColorStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="ColorStatement"/> node to be visited.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="ColorStatement"/> node.
    /// </returns>
    T VisitColorStatement(ColorStatement node);

    /// <summary>
    /// Visits an <see cref="HcolorStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the HCOLOR statement, which is used to set the high-resolution color
    /// in an Applesoft BASIC program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="HcolorStatement"/> node representing the HCOLOR statement in the AST.
    /// </param>
    /// <returns>
    /// A value of type <typeparamref name="T"/> that represents the result of processing the HCOLOR statement.
    /// </returns>
    T VisitHcolorStatement(HcolorStatement node);

    /// <summary>
    /// Visits a <see cref="PlotStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="PlotStatement"/> node to be processed, which represents a PLOT statement
    /// that plots a point in low-resolution graphics.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="PlotStatement"/> node.
    /// </returns>
    T VisitPlotStatement(PlotStatement node);

    /// <summary>
    /// Visits an <see cref="HplotStatement"/> node in the Abstract Syntax Tree (AST) of an Applesoft BASIC program.
    /// </summary>
    /// <param name="node">
    /// The <see cref="HplotStatement"/> node to be processed, representing the HPLOT statement used for plotting points
    /// or drawing lines in high-resolution graphics mode.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="HplotStatement"/> node.
    /// </returns>
    /// <remarks>
    /// The HPLOT statement allows specifying a series of points to plot or draw lines between them.
    /// It supports the use of the TO keyword to connect multiple points in a sequence.
    /// </remarks>
    T VisitHplotStatement(HplotStatement node);

    /// <summary>
    /// Visits a <see cref="DrawStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="DrawStatement"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="DrawStatement"/> node.
    /// </returns>
    T VisitDrawStatement(DrawStatement node);

    /// <summary>
    /// Visits an <see cref="XdrawStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the XDRAW statement, which performs an XOR drawing operation
    /// for a specified shape, optionally at a given position.
    /// </summary>
    /// <param name="node">
    /// The <see cref="XdrawStatement"/> node representing the XDRAW statement to be processed.
    /// </param>
    /// <returns>
    /// The result of processing the XDRAW statement, as defined by the implementation of the visitor.
    /// </returns>
    T VisitXdrawStatement(XdrawStatement node);

    /// <summary>
    /// Visits an <see cref="InverseStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="InverseStatement"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="InverseStatement"/> node.
    /// </returns>
    T VisitInverseStatement(InverseStatement node);

    /// <summary>
    /// Processes a <see cref="FlashStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="FlashStatement"/> node to be processed.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="FlashStatement"/> node.
    /// </returns>
    /// <remarks>
    /// This method is part of the Visitor design pattern implementation, enabling operations
    /// to be performed on the <see cref="FlashStatement"/> node without modifying its structure.
    /// </remarks>
    T VisitFlashStatement(FlashStatement node);

    /// <summary>
    /// Visits a <see cref="NormalStatement"/> node in the Abstract Syntax Tree (AST).
    /// This method processes the NORMAL statement, which sets the text mode to normal.
    /// </summary>
    /// <param name="node">
    /// The <see cref="NormalStatement"/> node to be visited and processed.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the NORMAL statement.
    /// </returns>
    T VisitNormalStatement(NormalStatement node);

    /// <summary>
    /// Visits a <see cref="ClearStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="ClearStatement"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="ClearStatement"/> node.
    /// </returns>
    T VisitClearStatement(ClearStatement node);

    /// <summary>
    /// Visits a <see cref="SleepStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="SleepStatement"/> node to visit.</param>
    /// <returns>The result of processing the <see cref="SleepStatement"/> node.</returns>
    T VisitSleepStatement(SleepStatement node);

    /// <summary>
    /// Visits an <see cref="AmpersandStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="AmpersandStatement"/> node representing the '&amp;' statement,
    /// which calls a machine language routine at memory location $03F5.
    /// </param>
    /// <returns>
    /// The result of processing the <see cref="AmpersandStatement"/> node,
    /// typically a value of type <typeparamref name="T"/>.
    /// </returns>
    T VisitAmpersandStatement(AmpersandStatement node);

    /// <summary>
    /// Visits a <see cref="HimemStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="HimemStatement"/> node representing the HIMEM: statement,
    /// which sets the top of memory in the Applesoft BASIC program.
    /// </param>
    /// <returns>
    /// The result of processing the <see cref="HimemStatement"/> node,
    /// typically a value of type <typeparamref name="T"/>.
    /// </returns>
    T VisitHimemStatement(HimemStatement node);

    /// <summary>
    /// Visits a <see cref="LomemStatement"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="LomemStatement"/> node to be visited, which represents the LOMEM statement
    /// used to set the bottom of variable memory in an Applesoft BASIC program.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="LomemStatement"/> node.
    /// </returns>
    T VisitLomemStatement(LomemStatement node);

    // Expressions

    /// <summary>
    /// Visits a numeric literal node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="NumberLiteral"/> node representing a numeric literal in the AST.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the numeric literal node.
    /// </returns>
    T VisitNumberLiteral(NumberLiteral node);

    /// <summary>
    /// Visits a <see cref="StringLiteral"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="StringLiteral"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="StringLiteral"/> node.
    /// </returns>
    T VisitStringLiteral(StringLiteral node);

    /// <summary>
    /// Visits a <see cref="VariableExpression"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="VariableExpression"/> node representing a reference to a variable in the Applesoft BASIC program.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="VariableExpression"/> node.
    /// </returns>
    T VisitVariableExpression(VariableExpression node);

    /// <summary>
    /// Visits a <see cref="BinaryExpression"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="BinaryExpression"/> node representing a binary operation, such as addition,
    /// subtraction, multiplication, or division, with its left and right operands and the operator.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="BinaryExpression"/> node.
    /// </returns>
    T VisitBinaryExpression(BinaryExpression node);

    /// <summary>
    /// Visits a <see cref="UnaryExpression"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">The <see cref="UnaryExpression"/> node to process.</param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the <see cref="UnaryExpression"/> node.
    /// </returns>
    T VisitUnaryExpression(UnaryExpression node);

    /// <summary>
    /// Visits a <see cref="FunctionCallExpression"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="FunctionCallExpression"/> node representing a built-in function call
    /// (e.g., SIN(X), LEN(A$)) in the Applesoft BASIC program.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the function call expression.
    /// </returns>
    T VisitFunctionCallExpression(FunctionCallExpression node);

    /// <summary>
    /// Visits an <see cref="ArrayAccessExpression"/> node in the Abstract Syntax Tree (AST).
    /// This method processes an array access expression, which represents access to an element
    /// of an array using one or more indices (e.g., <c>A(1)</c>, <c>B$(I, J)</c>).
    /// </summary>
    /// <param name="node">
    /// The <see cref="ArrayAccessExpression"/> node to visit, containing the array name
    /// and the list of index expressions used to access the array element.
    /// </param>
    /// <returns>
    /// A result of type <typeparamref name="T"/> produced by processing the array access expression.
    /// </returns>
    T VisitArrayAccessExpression(ArrayAccessExpression node);

    /// <summary>
    /// Visits a <see cref="UserFunctionExpression"/> node in the Abstract Syntax Tree (AST).
    /// </summary>
    /// <param name="node">
    /// The <see cref="UserFunctionExpression"/> node representing a user-defined function call
    /// (e.g., FN name) to be processed.
    /// </param>
    /// <returns>
    /// The result of processing the <see cref="UserFunctionExpression"/> node, as determined
    /// by the implementation of the visitor.
    /// </returns>
    T VisitUserFunctionExpression(UserFunctionExpression node);
}