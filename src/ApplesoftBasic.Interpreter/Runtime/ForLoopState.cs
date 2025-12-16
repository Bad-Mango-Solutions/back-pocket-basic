// <copyright file="ForLoopState.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Represents the state of a FOR loop.
/// </summary>
public class ForLoopState
{
    public string Variable { get; }

    public double EndValue { get; }

    public double StepValue { get; }

    public int ReturnLineIndex { get; }

    public int ReturnStatementIndex { get; }

    public ForLoopState(
        string variable,
        double endValue,
        double stepValue,
        int returnLineIndex,
        int returnStatementIndex)
    {
        Variable = variable;
        EndValue = endValue;
        StepValue = stepValue;
        ReturnLineIndex = returnLineIndex;
        ReturnStatementIndex = returnStatementIndex;
    }

    public bool IsComplete(double currentValue)
    {
        if (StepValue >= 0)
        {
            return currentValue > EndValue;
        }
        else
        {
            return currentValue < EndValue;
        }
    }
}