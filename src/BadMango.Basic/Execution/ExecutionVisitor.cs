// <copyright file="ExecutionVisitor.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Execution;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using AST;

using Emulation;

using IO;

using Microsoft.Extensions.Logging;

using Runtime;

using Tokens;

/// <summary>
/// Visitor implementation that executes Applesoft BASIC AST nodes.
/// </summary>
/// <remarks>
/// This class is responsible for evaluating statements and expressions in a BASIC program.
/// It delegates flow control operations (GOTO, GOSUB, RETURN) to the execution context.
/// </remarks>
public class ExecutionVisitor : IAstVisitor<BasicValue>
{
    private readonly IBasicRuntimeContext runtime;
    private readonly ISystemContext system;
    private readonly ExecutionContext context;
    private readonly IAppleSystem appleSystem;
    private readonly ILogger logger;
    private Random random;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionVisitor"/> class.
    /// </summary>
    /// <param name="runtime">The BASIC runtime context containing language state managers.</param>
    /// <param name="system">The system context containing hardware and I/O services.</param>
    /// <param name="context">The execution context for managing program state and flow control.</param>
    /// <param name="appleSystem">The emulated Apple II system.</param>
    /// <param name="logger">The logger for diagnostic and debugging output.</param>
    public ExecutionVisitor(
        IBasicRuntimeContext runtime,
        ISystemContext system,
        ExecutionContext context,
        IAppleSystem appleSystem,
        ILogger logger)
    {
        this.runtime = runtime;
        this.system = system;
        this.context = context;
        this.appleSystem = appleSystem;
        this.logger = logger;
        random = new();
    }

    /// <summary>
    /// Gets the emulated Apple II system.
    /// </summary>
    private IAppleSystem AppleSystem => appleSystem;

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
                    int currentCol = system.IO.GetCursorColumn();
                    if (col > currentCol)
                    {
                        system.IO.Write(new(' ', col - currentCol));
                    }

                    continue;
                }
                else if (func.Function == TokenType.SPC)
                {
                    int spaces = func.Arguments[0].Accept(this).AsInteger();
                    system.IO.Write(new(' ', Math.Max(0, spaces)));
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

            system.IO.Write(output);

            // Handle separators
            if (i < node.Separators.Count)
            {
                switch (node.Separators[i])
                {
                    case PrintSeparator.Comma:
                        // Tab to next 16-column zone
                        int col = system.IO.GetCursorColumn();
                        int nextTab = ((col / 16) + 1) * 16;
                        system.IO.Write(new(' ', nextTab - col));
                        break;
                    case PrintSeparator.Semicolon:
                        // No space
                        break;
                    case PrintSeparator.None:
                        // Space between items
                        if (value.IsNumeric)
                        {
                            system.IO.Write(" ");
                        }

                        break;
                }
            }
        }

        // Print newline unless ends with separator
        if (!node.EndsWithSeparator)
        {
            system.IO.WriteLine();
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
            string input = system.IO.ReadLine(prompt + " ");
            string[] parts = input.Split(',');

            if (parts.Length < node.Variables.Count)
            {
                system.IO.WriteLine("??REDO FROM START");
                continue;
            }

            valid = true;
            for (int i = 0; i < node.Variables.Count; i++)
            {
                var variable = node.Variables[i];
                string value = i < parts.Length ? parts[i].Trim() : string.Empty;

                if (variable.IsString)
                {
                    runtime.Variables.SetVariable(variable.Name, BasicValue.FromString(value));
                }
                else
                {
                    if (double.TryParse(value, out double num))
                    {
                        runtime.Variables.SetVariable(variable.Name, BasicValue.FromNumber(num));
                    }
                    else
                    {
                        system.IO.WriteLine("??REDO FROM START");
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
            runtime.Variables.SetArrayElement(node.Variable.Name, indices, value);
        }
        else
        {
            runtime.Variables.SetVariable(node.Variable.Name, value);
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
        context.GetExecutionPosition(out int lineIndex, out int statementIndex);
        runtime.Gosub.Push(new(lineIndex, statementIndex + 1));
        throw new GotoException(node.LineNumber);
    }

    /// <inheritdoc/>
    public BasicValue VisitReturnStatement(ReturnStatement node)
    {
        var returnAddr = runtime.Gosub.Pop();
        int lineIndex = returnAddr.LineIndex;
        int statementIndex = returnAddr.StatementIndex;

        // Check if we need to advance to next line
        var program = context.GetProgram();
        if (program != null && statementIndex >= program.Lines[lineIndex].Statements.Count)
        {
            lineIndex++;
            statementIndex = 0;
        }

        context.SetExecutionPosition(lineIndex, statementIndex);
        throw new NextIterationException();
    }

    /// <inheritdoc/>
    public BasicValue VisitForStatement(ForStatement node)
    {
        var start = node.Start.Accept(this);
        var end = node.End.Accept(this);
        double step = node.Step?.Accept(this).AsNumber() ?? 1.0;

        // Set loop variable
        runtime.Variables.SetVariable(node.Variable, start);

        // Get current execution position
        context.GetExecutionPosition(out int lineIndex, out int statementIndex);

        // Push loop state
        runtime.Loops.PushFor(new(
            node.Variable,
            end.AsNumber(),
            step,
            lineIndex,
            statementIndex + 1));

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitNextStatement(NextStatement node)
    {
        // Handle multiple variables in NEXT
        var variables = node.Variables.Count > 0 ? node.Variables : [string.Empty];

        foreach (var varName in variables)
        {
            var loopState = runtime.Loops.PopFor(string.IsNullOrEmpty(varName) ? null : varName);
            if (loopState == null)
            {
                throw new BasicRuntimeException("?NEXT WITHOUT FOR ERROR", context.GetCurrentLineNumber());
            }

            string variable = loopState.Variable;
            double currentValue = this.runtime.Variables.GetVariable(variable).AsNumber();
            currentValue += loopState.StepValue;
            this.runtime.Variables.SetVariable(variable, BasicValue.FromNumber(currentValue));

            if (!loopState.IsComplete(currentValue))
            {
                // Continue loop
                runtime.Loops.PushFor(loopState);
                context.SetExecutionPosition(loopState.ReturnLineIndex, loopState.ReturnStatementIndex);
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
            runtime.Variables.DimArray(array.Name, dims);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitReadStatement(ReadStatement node)
    {
        foreach (var variable in node.Variables)
        {
            var value = runtime.Data.Read();
            runtime.Variables.SetVariable(variable.Name, value);
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
        runtime.Data.Restore();
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
        throw new ProgramStopException(context.GetCurrentLineNumber());
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

        appleSystem.Poke(address, (byte)value);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitCallStatement(CallStatement node)
    {
        int address = node.Address.Accept(this).AsInteger();
        appleSystem.Call(address);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitGetStatement(GetStatement node)
    {
        char c = system.IO.ReadChar();
        runtime.Variables.SetVariable(node.Variable.Name, BasicValue.FromString(c.ToString()));
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
            context.GetExecutionPosition(out int lineIndex, out int statementIndex);
            runtime.Gosub.Push(new(lineIndex, statementIndex + 1));
            throw new GotoException(node.LineNumbers[index - 1]);
        }

        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitDefStatement(DefStatement node)
    {
        runtime.Functions.DefineFunction(node.FunctionName, node.Parameter, node.Body);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHomeStatement(HomeStatement node)
    {
        system.IO.ClearScreen();
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHtabStatement(HtabStatement node)
    {
        int col = node.Column.Accept(this).AsInteger();
        system.IO.SetCursorPosition(col, system.IO.GetCursorRow() + 1);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitVtabStatement(VtabStatement node)
    {
        int row = node.Row.Accept(this).AsInteger();
        system.IO.SetCursorPosition(system.IO.GetCursorColumn() + 1, row);
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
        system.IO.SetTextMode(TextMode.Inverse);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitFlashStatement(FlashStatement node)
    {
        system.IO.SetTextMode(TextMode.Flash);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitNormalStatement(NormalStatement node)
    {
        system.IO.SetTextMode(TextMode.Normal);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitClearStatement(ClearStatement node)
    {
        runtime.Clear();
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
        appleSystem.Call(Emulation.AppleSystem.MemoryLocations.AMPERV);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitHimemStatement(HimemStatement node)
    {
        int address = node.Address.Accept(this).AsInteger();
        appleSystem.Memory.WriteWord(0x73, (ushort)address);
        return BasicValue.Zero;
    }

    /// <inheritdoc/>
    public BasicValue VisitLomemStatement(LomemStatement node)
    {
        int address = node.Address.Accept(this).AsInteger();
        appleSystem.Memory.WriteWord(0x69, (ushort)address);
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
        return runtime.Variables.GetVariable(node.Name);
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
            _ => throw new BasicRuntimeException($"Unknown operator: {node.Operator}", context.GetCurrentLineNumber()),
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
            _ => throw new BasicRuntimeException($"Unknown unary operator: {node.Operator}", context.GetCurrentLineNumber()),
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
        return runtime.Variables.GetArrayElement(node.ArrayName, indices);
    }

    /// <inheritdoc/>
    public BasicValue VisitUserFunctionExpression(UserFunctionExpression node)
    {
        var function = runtime.Functions.GetFunction(node.FunctionName);
        if (function == null)
        {
            throw new BasicRuntimeException("?UNDEF'DP FUNCTION ERROR", context.GetCurrentLineNumber());
        }

        // Save current parameter value
        var savedValue = runtime.Variables.VariableExists(function.Parameter)
            ? runtime.Variables.GetVariable(function.Parameter)
            : (BasicValue?)null;

        try
        {
            // Set parameter to argument value
            var argValue = node.Argument.Accept(this);
            runtime.Variables.SetVariable(function.Parameter, argValue);

            // Evaluate function body
            return function.Body.Accept(this);
        }
        finally
        {
            // Restore parameter value
            if (savedValue.HasValue)
            {
                runtime.Variables.SetVariable(function.Parameter, savedValue.Value);
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
            TokenType.PEEK => BasicValue.FromNumber(appleSystem.Peek(args[0].Accept(this).AsInteger())),
            TokenType.FRE => BasicValue.FromNumber(32768),
            TokenType.POS => BasicValue.FromNumber(system.IO.GetCursorColumn()),
            TokenType.SCRN => BasicValue.FromNumber(0),
            TokenType.PDL => BasicValue.FromNumber(128),
            TokenType.USR => EvaluateUsr(args[0]),

            _ => throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber()),
        };
    }

    private BasicValue EvaluateUsr(IExpression arg)
    {
        // Evaluate the parameter expression and get the numeric value
        double value = arg.Accept(this).AsNumber();

        // Store the value in FAC1 at $009D using the FacConverter utility
        FacConverter.WriteToMemory(
            appleSystem.Memory,
            Emulation.AppleSystem.MemoryLocations.FAC1,
            Emulation.AppleSystem.MemoryLocations.FAC1SIGN,
            value);

        // Execute the machine language routine at $000A (USR vector)
        // The user should have placed a JMP instruction there pointing to their ML code
        logger.LogDebug("Executing USR function (JMP to $000A) with value {Value}", value);
        appleSystem.Call(Emulation.AppleSystem.MemoryLocations.USRADR);

        // Read the result from FAC1 after the ML routine returns
        return BasicValue.FromNumber(
            FacConverter.ReadFromMemory(appleSystem.Memory, Emulation.AppleSystem.MemoryLocations.FAC1));
    }

    private BasicValue EvaluateLog(IExpression arg)
    {
        double value = arg.Accept(this).AsNumber();
        if (value <= 0)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber());
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
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber());
        }

        return BasicValue.FromNumber(Math.Sqrt(value));
    }

    private BasicValue EvaluateAsc(IExpression arg)
    {
        string s = arg.Accept(this).AsString();
        if (s.Length == 0)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber());
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
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber());
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
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber());
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
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber());
        }

        length = Math.Min(length, s.Length);
        return BasicValue.FromString(s[^length..]);
    }

    private BasicValue EvaluateChr(IExpression arg)
    {
        int code = arg.Accept(this).AsInteger();
        if (code < 0 || code > 255)
        {
            throw new BasicRuntimeException("?ILLEGAL QUANTITY ERROR", context.GetCurrentLineNumber());
        }

        return BasicValue.FromString(((char)code).ToString());
    }
}