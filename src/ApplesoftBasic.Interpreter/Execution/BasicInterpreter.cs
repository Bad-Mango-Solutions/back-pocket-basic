// <copyright file="BasicInterpreter.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Execution;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AST;
using Emulation;
using IO;
using Microsoft.Extensions.Logging;
using Parser;
using Runtime;
using Tokens;

/// <summary>
/// Applesoft BASIC interpreter that executes parsed BASIC programs.
/// </summary>
public class BasicInterpreter : IBasicInterpreter, IAstVisitor<BasicValue>
{
    private readonly IParser parser;
    private readonly IBasicIO io;
    private readonly IVariableManager variables;
    private readonly IFunctionManager functions;
    private readonly IDataManager data;
    private readonly ILoopManager loops;
    private readonly IGosubManager gosub;
    private readonly ILogger<BasicInterpreter> logger;
    private readonly Dictionary<int, int> lineNumberIndex = [];

    private Random random;
    private ProgramNode? program;
    private int currentLineIndex;
    private int currentStatementIndex;
    private bool running;
    private bool shouldStop;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicInterpreter"/> class.
    /// </summary>
    /// <param name="parser">The parser for BASIC source code.</param>
    /// <param name="io">The I/O handler for console operations.</param>
    /// <param name="variables">The variable manager.</param>
    /// <param name="functions">The function manager.</param>
    /// <param name="data">The DATA/READ manager.</param>
    /// <param name="loops">The loop manager.</param>
    /// <param name="gosub">The GOSUB/RETURN manager.</param>
    /// <param name="appleSystem">The Apple II system emulator.</param>
    /// <param name="logger">The logger instance.</param>
    public BasicInterpreter(
        IParser parser,
        IBasicIO io,
        IVariableManager variables,
        IFunctionManager functions,
        IDataManager data,
        ILoopManager loops,
        IGosubManager gosub,
        IAppleSystem appleSystem,
        ILogger<BasicInterpreter> logger)
    {
        this.parser = parser;
        this.io = io;
        this.variables = variables;
        this.functions = functions;
        this.data = data;
        this.loops = loops;
        this.gosub = gosub;
        AppleSystem = appleSystem;
        this.logger = logger;
        random = new();
    }

    /// <inheritdoc/>
    public IAppleSystem AppleSystem { get; }

    /// <inheritdoc/>
    public void Run(string source)
    {
        logger.LogInformation("Starting BASIC program execution");

        try
        {
            // Parse the program
            program = parser.Parse(source);

            // Build line number index
            lineNumberIndex.Clear();
            for (int i = 0; i < program.Lines.Count; i++)
            {
                lineNumberIndex[program.Lines[i].LineNumber] = i;
            }

            // Initialize runtime
            variables.Clear();
            functions.Clear();
            loops.Clear();
            gosub.Clear();
            data.Initialize(program.DataValues);

            // Start execution
            currentLineIndex = 0;
            currentStatementIndex = 0;
            running = true;
            shouldStop = false;

            Execute();
        }
        catch (ProgramEndException)
        {
            logger.LogInformation("Program ended normally");
        }
        catch (ProgramStopException ex)
        {
            io.WriteLine();
            io.WriteLine(ex.Message);
        }
        catch (BasicRuntimeException ex)
        {
            io.WriteLine();
            io.WriteLine(ex.Message);
            logger.LogError(ex, "Runtime error");
        }
        catch (ParseException ex)
        {
            io.WriteLine();
            io.WriteLine("?SYNTAX ERROR");
            logger.LogError(ex, "Parse error");
        }
        finally
        {
            running = false;
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        shouldStop = true;
    }

    /// <inheritdoc/>
    public BasicValue VisitProgram(ProgramNode node)
    {
        // Not used directly - execution happens in Run()
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitLine(LineNode node)
    {
        foreach (var statement in node.Statements)
        {
            statement.Accept(this);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitPrintStatement(PrintStatement node)
    {
        for (int i = 0; i < node.Expressions.Count; i++)
        {
            var expr = node.Expressions[i];

            // Handle TAB and SPC functions
            if (expr is FunctionCallExpression func)
            {
                if (func.Function == TokenType.TAB)
                {
                    int col = func.Arguments[0].Accept(this).AsInteger();
                    int currentCol = io.GetCursorColumn();
                    if (col > currentCol)
                    {
                        io.Write(new(' ', col - currentCol));
                    }

                    continue;
                }
                else if (func.Function == TokenType.SPC)
                {
                    int spaces = func.Arguments[0].Accept(this).AsInteger();
                    io.Write(new(' ', Math.Max(0, spaces)));
                    continue;
                }
            }

            var value = expr.Accept(this);

            // Add leading space for positive numbers
            string output = value.AsString();
            if (value.IsNumeric && value.AsNumber() >= 0)
            {
                output = " " + output;
            }

            io.Write(output);

            // Handle separators
            if (i < node.Separators.Count)
            {
                switch (node.Separators[i])
                {
                    case PrintSeparator.Comma:
                        // Tab to next 16-column zone
                        int col = io.GetCursorColumn();
                        int nextTab = ((col / 16) + 1) * 16;
                        io.Write(new(' ', nextTab - col));
                        break;
                    case PrintSeparator.Semicolon:
                        // No space
                        break;
                    case PrintSeparator.None:
                        // Space between items
                        if (value.IsNumeric)
                        {
                            io.Write(" ");
                        }

                        break;
                }
            }
        }

        // Print newline unless ends with separator
        if (!node.EndsWithSeparator)
        {
            io.WriteLine();
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitInputStatement(InputStatement node)
    {
        string prompt = node.Prompt ?? "?";
        if (!prompt.EndsWith('?'))
        {
            prompt += "?";
        }

        bool valid = false;
        while (!valid)
        {
            string input = io.ReadLine(prompt + " ");
            string[] parts = input.Split(',');

            if (parts.Length < node.Variables.Count)
            {
                io.WriteLine("??REDO FROM START");
                continue;
            }

            valid = true;
            for (int i = 0; i < node.Variables.Count; i++)
            {
                var variable = node.Variables[i];
                string value = i < parts.Length ? parts[i].Trim() : string.Empty;

                if (variable.IsString)
                {
                    variables.SetVariable(variable.Name, BasicValue.FromString(value));
                }
                else
                {
                    if (double.TryParse(value, out double num))
                    {
                        variables.SetVariable(variable.Name, BasicValue.FromNumber(num));
                    }
                    else
                    {
                        io.WriteLine("??REDO FROM START");
                        valid = false;
                        break;
                    }
                }
            }
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitLetStatement(LetStatement node)
    {
        var value = node.Value.Accept(this);

        if (node.ArrayIndices != null && node.ArrayIndices.Count > 0)
        {
            int[] indices = node.ArrayIndices.Select(e => e.Accept(this).AsInteger()).ToArray();
            variables.SetArrayElement(node.Variable.Name, indices, value);
        }
        else
        {
            variables.SetVariable(node.Variable.Name, value);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitIfStatement(IfStatement node)
    {
        var condition = node.Condition.Accept(this);

        if (condition.IsTrue())
        {
            if (node.GotoLineNumber.HasValue)
            {
                throw new GotoException(node.GotoLineNumber.Value);
            }

            foreach (var statement in node.ThenBranch)
            {
                statement.Accept(this);
            }
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitGotoStatement(GotoStatement node)
    {
        throw new GotoException(node.LineNumber);
    }

    /// <inheritdoc/>
    public BasicValue VisitGosubStatement(GosubStatement node)
    {
        // Save return address
        gosub.Push(new(currentLineIndex, currentStatementIndex + 1));
        throw new GotoException(node.LineNumber);
    }

    /// <inheritdoc/>
    public BasicValue VisitReturnStatement(ReturnStatement node)
    {
        var returnAddr = gosub.Pop();
        currentLineIndex = returnAddr.LineIndex;
        currentStatementIndex = returnAddr.StatementIndex;

        // Check if we need to advance to next line
        if (program != null && currentStatementIndex >= program.Lines[currentLineIndex].Statements.Count)
        {
            currentLineIndex++;
            currentStatementIndex = 0;
        }

        throw new NextIterationException();
    }

    /// <inheritdoc/>
    public BasicValue VisitForStatement(ForStatement node)
    {
        var start = node.Start.Accept(this);
        var end = node.End.Accept(this);
        double step = node.Step?.Accept(this).AsNumber() ?? 1.0;

        // Set loop variable
        variables.SetVariable(node.Variable, start);

        // Push loop state
        loops.PushFor(new(
            node.Variable,
            end.AsNumber(),
            step,
            currentLineIndex,
            currentStatementIndex + 1));

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitNextStatement(NextStatement node)
    {
        // Handle multiple variables in NEXT
        var variables = node.Variables.Count > 0 ? node.Variables : [string.Empty];

        foreach (var varName in variables)
        {
            var loopState = loops.PopFor(string.IsNullOrEmpty(varName) ? null : varName);
            if (loopState == null)
            {
                throw new BasicRuntimeException("?NEXT WITHOUT FOR ERROR", GetCurrentLineNumber());
            }

            string variable = loopState.Variable;
            double currentValue = this.variables.GetVariable(variable).AsNumber();
            currentValue += loopState.StepValue;
            this.variables.SetVariable(variable, BasicValue.FromNumber(currentValue));

            if (!loopState.IsComplete(currentValue))
            {
                // Continue loop
                loops.PushFor(loopState);
                currentLineIndex = loopState.ReturnLineIndex;
                currentStatementIndex = loopState.ReturnStatementIndex;
                throw new NextIterationException();
            }
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitDimStatement(DimStatement node)
    {
        foreach (var array in node.Arrays)
        {
            int[] dims = array.Dimensions.Select(e => e.Accept(this).AsInteger()).ToArray();
            variables.DimArray(array.Name, dims);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitReadStatement(ReadStatement node)
    {
        foreach (var variable in node.Variables)
        {
            var value = data.Read();
            variables.SetVariable(variable.Name, value);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitDataStatement(DataStatement node)
    {
        // DATA statements are processed during parsing
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitRestoreStatement(RestoreStatement node)
    {
        data.Restore();
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitEndStatement(EndStatement node)
    {
        throw new ProgramEndException();
    }

    /// <inheritdoc/>
    public BasicValue VisitStopStatement(StopStatement node)
    {
        throw new ProgramStopException(GetCurrentLineNumber());
    }

    /// <inheritdoc/>
    public BasicValue VisitRemStatement(RemStatement node)
    {
        // Comments do nothing
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitPokeStatement(PokeStatement node)
    {
        int address = node.Address.Accept(this).AsInteger();
        int value = node.Value.Accept(this).AsInteger() & 0xFF;

        AppleSystem.Poke(address, (byte)value);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitCallStatement(CallStatement node)
    {
        int address = node.Address.Accept(this).AsInteger();
        AppleSystem.Call(address);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitGetStatement(GetStatement node)
    {
        char c = io.ReadChar();
        variables.SetVariable(node.Variable.Name, BasicValue.FromString(c.ToString()));
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitOnGotoStatement(OnGotoStatement node)
    {
        int index = node.Expression.Accept(this).AsInteger();

        if (index >= 1 && index <= node.LineNumbers.Count)
        {
            throw new GotoException(node.LineNumbers[index - 1]);
        }

        // If index is out of range, continue to next statement
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitOnGosubStatement(OnGosubStatement node)
    {
        int index = node.Expression.Accept(this).AsInteger();

        if (index >= 1 && index <= node.LineNumbers.Count)
        {
            gosub.Push(new(currentLineIndex, currentStatementIndex + 1));
            throw new GotoException(node.LineNumbers[index - 1]);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitDefStatement(DefStatement node)
    {
        functions.DefineFunction(node.FunctionName, node.Parameter, node.Body);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHomeStatement(HomeStatement node)
    {
        io.ClearScreen();
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHtabStatement(HtabStatement node)
    {
        int col = node.Column.Accept(this).AsInteger();
        io.SetCursorPosition(col, io.GetCursorRow() + 1);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitVtabStatement(VtabStatement node)
    {
        int row = node.Row.Accept(this).AsInteger();
        io.SetCursorPosition(io.GetCursorColumn() + 1, row);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitTextStatement(TextStatement node)
    {
        logger.LogDebug("TEXT mode activated (stubbed)");
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitGrStatement(GrStatement node)
    {
        logger.LogDebug("GR mode activated (stubbed)");
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHgrStatement(HgrStatement node)
    {
        logger.LogDebug("HGR{Mode} mode activated (stubbed)", node.IsHgr2 ? "2" : string.Empty);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitColorStatement(ColorStatement node)
    {
        int color = node.Color.Accept(this).AsInteger();
        logger.LogDebug("COLOR set to {Color} (stubbed)", color);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHcolorStatement(HcolorStatement node)
    {
        int color = node.Color.Accept(this).AsInteger();
        logger.LogDebug("HCOLOR set to {Color} (stubbed)", color);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitPlotStatement(PlotStatement node)
    {
        int x = node.X.Accept(this).AsInteger();
        int y = node.Y.Accept(this).AsInteger();
        logger.LogDebug("PLOT {X},{Y} (stubbed)", x, y);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHplotStatement(HplotStatement node)
    {
        foreach (var point in node.Points)
        {
            int x = point.X.Accept(this).AsInteger();
            int y = point.Y.Accept(this).AsInteger();
            logger.LogDebug("HPLOT {X},{Y} (stubbed)", x, y);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitDrawStatement(DrawStatement node)
    {
        int shape = node.ShapeNumber.Accept(this).AsInteger();
        logger.LogDebug("DRAW {Shape} (stubbed)", shape);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitXdrawStatement(XdrawStatement node)
    {
        int shape = node.ShapeNumber.Accept(this).AsInteger();
        logger.LogDebug("XDRAW {Shape} (stubbed)", shape);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitInverseStatement(InverseStatement node)
    {
        io.SetTextMode(TextMode.Inverse);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitFlashStatement(FlashStatement node)
    {
        io.SetTextMode(TextMode.Flash);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitNormalStatement(NormalStatement node)
    {
        io.SetTextMode(TextMode.Normal);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitClearStatement(ClearStatement node)
    {
        variables.Clear();
        functions.Clear();
        loops.Clear();
        gosub.Clear();
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitSleepStatement(SleepStatement node)
    {
        int ms = node.Milliseconds.Accept(this).AsInteger();
        Thread.Sleep(Math.Max(0, ms));
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitAmpersandStatement(AmpersandStatement node)
    {
        // The ampersand operator performs a JSR to $03F5
        // This allows user-provided machine language routines to be called
        logger.LogDebug("Executing & operator (JSR to $03F5)");
        AppleSystem.Call(Emulation.AppleSystem.MemoryLocations.AMPERV);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHimemStatement(HimemStatement node)
    {
        int address = node.Address.Accept(this).AsInteger();
        AppleSystem.Memory.WriteWord(0x73, (ushort)address);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitLomemStatement(LomemStatement node)
    {
        int address = node.Address.Accept(this).AsInteger();
        AppleSystem.Memory.WriteWord(0x69, (ushort)address);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitNumberLiteral(NumberLiteral node)
    {
        return BasicValue.FromNumber(node.Value);
    }

    /// <inheritdoc/>
    public BasicValue VisitStringLiteral(StringLiteral node)
    {
        return BasicValue.FromString(node.Value);
    }

    /// <inheritdoc/>
    public BasicValue VisitVariableExpression(VariableExpression node)
    {
        return variables.GetVariable(node.Name);
    }

    /// <inheritdoc/>
    public BasicValue VisitBinaryExpression(BinaryExpression node)
    {
        var left = node.Left.Accept(this);

        // Short-circuit evaluation for AND/OR
        if (node.Operator == TokenType.AND)
        {
            if (!left.IsTrue())
            {
                return BasicValue.FromNumber(0);
            }

            var right = node.Right.Accept(this);
            return BasicValue.FromNumber(right.IsTrue() ? 1 : 0);
        }

        if (node.Operator == TokenType.OR)
        {
            if (left.IsTrue())
            {
                return BasicValue.FromNumber(1);
            }

            var right = node.Right.Accept(this);
            return BasicValue.FromNumber(right.IsTrue() ? 1 : 0);
        }

        var rightVal = node.Right.Accept(this);

        return node.Operator switch
        {
            TokenType.Plus => left + rightVal,
            TokenType.Minus => left - rightVal,
            TokenType.Multiply => left * rightVal,
            TokenType.Divide => left / rightVal,
            TokenType.Power => left ^ rightVal,
            TokenType.Equal => BasicValue.FromNumber(left.ApproximatelyEquals(rightVal) ? 1 : 0),
            TokenType.NotEqual => BasicValue.FromNumber(!left.ApproximatelyEquals(rightVal) ? 1 : 0),
            TokenType.LessThan => BasicValue.FromNumber(left < rightVal ? 1 : 0),
            TokenType.GreaterThan => BasicValue.FromNumber(left > rightVal ? 1 : 0),
            TokenType.LessOrEqual => BasicValue.FromNumber(left <= rightVal ? 1 : 0),
            TokenType.GreaterOrEqual => BasicValue.FromNumber(left >= rightVal ? 1 : 0),
            _ => throw new BasicRuntimeException($"Unknown operator: {node.Operator}", GetCurrentLineNumber()),
        };
    }

    /// <inheritdoc/>
    public BasicValue VisitUnaryExpression(UnaryExpression node)
    {
        var operand = node.Operand.Accept(this);

        return node.Operator switch
        {
            TokenType.Minus => -operand,
            TokenType.NOT => BasicValue.FromNumber(operand.IsTrue() ? 0 : 1),
            _ => throw new BasicRuntimeException($"Unknown unary operator: {node.Operator}", GetCurrentLineNumber()),
        };
    }

    /// <inheritdoc/>
    public BasicValue VisitFunctionCallExpression(FunctionCallExpression node)
    {
        return EvaluateBuiltInFunction(node.Function, node.Arguments);
    }

    /// <inheritdoc/>
    public BasicValue VisitArrayAccessExpression(ArrayAccessExpression node)
    {
        int[] indices = node.Indices.Select(e => e.Accept(this).AsInteger()).ToArray();
        return variables.GetArrayElement(node.ArrayName, indices);
    }

    /// <inheritdoc/>
    public BasicValue VisitUserFunctionExpression(UserFunctionExpression node)
    {
        var function = functions.GetFunction(node.FunctionName);
        if (function == null)
        {
            throw new BasicRuntimeException("?UNDEF'DP FUNCTION ERROR", GetCurrentLineNumber());
        }

        // Save current parameter value
        var savedValue = variables.VariableExists(function.Parameter)
            ? variables.GetVariable(function.Parameter)
            : (BasicValue?)null;

        try
        {
            // Set parameter to argument value
            var argValue = node.Argument.Accept(this);
            variables.SetVariable(function.Parameter, argValue);

            // Evaluate function body
            return function.Body.Accept(this);
        }
        finally
        {
            // Restore parameter value
            if (savedValue.HasValue)
            {
                variables.SetVariable(function.Parameter, savedValue.Value);
            }
        }
    }

    private static double ParseVal(string s)
    {
        s = s.Trim();
        if (string.IsNullOrEmpty(s))
        {
            return 0;
        }

        // Parse as much of the string as possible as a number
        int i = 0;
        if (i < s.Length && (s[i] == '+' || s[i] == '-'))
        {
            i++;
        }

        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'))
        {
            i++;
        }

        if (i < s.Length && (s[i] == 'E' || s[i] == 'e'))
        {
            i++;
            if (i < s.Length && (s[i] == '+' || s[i] == '-'))
            {
                i++;
            }

            while (i < s.Length && char.IsDigit(s[i]))
            {
                i++;
            }
        }

        if (i == 0)
        {
            return 0;
        }

        return double.TryParse(s[..i], out double result) ? result : 0;
    }

    private void Execute()
    {
        while (running && !shouldStop && program != null)
        {
            if (currentLineIndex >= program.Lines.Count)
            {
                break;
            }

            var line = program.Lines[currentLineIndex];

            while (currentStatementIndex < line.Statements.Count)
            {
                if (shouldStop)
                {
                    break;
                }

                var statement = line.Statements[currentStatementIndex];

                try
                {
                    statement.Accept(this);
                }
                catch (GotoException ex)
                {
                    JumpToLine(ex.LineNumber);
                    break;
                }
                catch (NextIterationException)
                {
                    // Continue with next iteration of FOR loop
                    break;
                }

                currentStatementIndex++;
            }

            // Move to next line
            if (currentStatementIndex >= line.Statements.Count)
            {
                currentLineIndex++;
                currentStatementIndex = 0;
            }
        }
    }

    private void JumpToLine(int lineNumber)
    {
        if (!lineNumberIndex.TryGetValue(lineNumber, out int index))
        {
            throw new BasicRuntimeException("?UNDEF'DP STATEMENT ERROR", GetCurrentLineNumber());
        }

        currentLineIndex = index;
        currentStatementIndex = 0;
    }

    private int GetCurrentLineNumber()
    {
        if (program != null && currentLineIndex < program.Lines.Count)
        {
            return program.Lines[currentLineIndex].LineNumber;
        }

        return 0;
    }

    private BasicValue EvaluateBuiltInFunction(TokenType function, List<IExpression> args)
    {
        return function switch
        {
            // Math functions
            TokenType.ABS => BasicValue.FromNumber(Math.Abs(args[0].Accept(this).AsNumber())),
            TokenType.ATN => BasicValue.FromNumber(Math.Atan(args[0].Accept(this).AsNumber())),
            TokenType.COS => BasicValue.FromNumber(Math.Cos(args[0].Accept(this).AsNumber())),
            TokenType.EXP => BasicValue.FromNumber(Math.Exp(args[0].Accept(this).AsNumber())),
            TokenType.INT => BasicValue.FromNumber(Math.Floor(args[0].Accept(this).AsNumber())),
            TokenType.LOG => EvaluateLog(args[0]),
            TokenType.RND => EvaluateRnd(args[0]),
            TokenType.SGN => BasicValue.FromNumber(Math.Sign(args[0].Accept(this).AsNumber())),
            TokenType.SIN => BasicValue.FromNumber(Math.Sin(args[0].Accept(this).AsNumber())),
            TokenType.SQR => EvaluateSqr(args[0]),
            TokenType.TAN => BasicValue.FromNumber(Math.Tan(args[0].Accept(this).AsNumber())),

            // String functions
            TokenType.LEN => BasicValue.FromNumber(args[0].Accept(this).AsString().Length),
            TokenType.VAL => BasicValue.FromNumber(ParseVal(args[0].Accept(this).AsString())),
            TokenType.ASC => EvaluateAsc(args[0]),
            TokenType.MID_S => EvaluateMid(args),
            TokenType.LEFT_S => EvaluateLeft(args),
            TokenType.RIGHT_S => EvaluateRight(args),
            TokenType.STR_S => BasicValue.FromString(args[0].Accept(this).AsNumber().ToString()),
            TokenType.CHR_S => EvaluateChr(args[0]),

            // Utility functions
            TokenType.PEEK => BasicValue.FromNumber(AppleSystem.Peek(args[0].Accept(this).AsInteger())),
            TokenType.FRE => BasicValue.FromNumber(32768),
            TokenType.POS => BasicValue.FromNumber(io.GetCursorColumn()),
            TokenType.SCRN => BasicValue.FromNumber(0),
            TokenType.PDL => BasicValue.FromNumber(128),
            TokenType.USR => EvaluateUsr(args[0]),

            _ => throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber()),
        };
    }

    private BasicValue EvaluateUsr(IExpression arg)
    {
        // Evaluate the parameter expression and get the numeric value
        double value = arg.Accept(this).AsNumber();

        // Store the value in FAC1 at $009D using the FacConverter utility
        Emulation.FacConverter.WriteToMemory(
            AppleSystem.Memory,
            Emulation.AppleSystem.MemoryLocations.FAC1,
            Emulation.AppleSystem.MemoryLocations.FAC1SIGN,
            value);

        // Execute the machine language routine at $000A (USR vector)
        // The user should have placed a JMP instruction there pointing to their ML code
        logger.LogDebug("Executing USR function (JMP to $000A) with value {Value}", value);
        AppleSystem.Call(Emulation.AppleSystem.MemoryLocations.USRADR);

        // Read the result from FAC1 after the ML routine returns
        return BasicValue.FromNumber(
            Emulation.FacConverter.ReadFromMemory(AppleSystem.Memory, Emulation.AppleSystem.MemoryLocations.FAC1));
    }

    private BasicValue EvaluateLog(IExpression arg)
    {
        double value = arg.Accept(this).AsNumber();
        if (value <= 0)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber());
        }

        return BasicValue.FromNumber(Math.Log(value));
    }

    private BasicValue EvaluateRnd(IExpression arg)
    {
        double n = arg.Accept(this).AsNumber();

        if (n < 0)
        {
            // Negative: seed the generator and return consistent value
            random = new((int)(n * 1000));
        }

        return BasicValue.FromNumber(random.NextDouble());
    }

    private BasicValue EvaluateSqr(IExpression arg)
    {
        double value = arg.Accept(this).AsNumber();
        if (value < 0)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber());
        }

        return BasicValue.FromNumber(Math.Sqrt(value));
    }

    private BasicValue EvaluateAsc(IExpression arg)
    {
        string s = arg.Accept(this).AsString();
        if (s.Length == 0)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber());
        }

        return BasicValue.FromNumber(s[0]);
    }

    private BasicValue EvaluateMid(List<IExpression> args)
    {
        string s = args[0].Accept(this).AsString();
        int start = args[1].Accept(this).AsInteger();
        int length = args.Count > 2 ? args[2].Accept(this).AsInteger() : s.Length;

        if (start < 1)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber());
        }

        start--; // Convert to 0-based
        if (start >= s.Length)
        {
            return BasicValue.FromString(string.Empty);
        }

        length = Math.Min(length, s.Length - start);
        return BasicValue.FromString(s.Substring(start, length));
    }

    private BasicValue EvaluateLeft(List<IExpression> args)
    {
        string s = args[0].Accept(this).AsString();
        int length = args[1].Accept(this).AsInteger();

        if (length < 0)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber());
        }

        length = Math.Min(length, s.Length);
        return BasicValue.FromString(s[..length]);
    }

    private BasicValue EvaluateRight(List<IExpression> args)
    {
        string s = args[0].Accept(this).AsString();
        int length = args[1].Accept(this).AsInteger();

        if (length < 0)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber());
        }

        length = Math.Min(length, s.Length);
        return BasicValue.FromString(s[^length..]);
    }

    private BasicValue EvaluateChr(IExpression arg)
    {
        int code = arg.Accept(this).AsInteger();
        if (code < 0 || code > 255)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", GetCurrentLineNumber());
        }

        return BasicValue.FromString(((char)code).ToString());
    }
}