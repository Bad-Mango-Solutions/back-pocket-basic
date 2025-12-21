// <copyright file="ForLoopState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

/// <summary>
/// Represents the state of a FOR loop.
/// </summary>
public class ForLoopState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForLoopState"/> class.
    /// </summary>
    /// <param name="variable">The name of the loop variable.</param>
    /// <param name="endValue">The end value of the loop.</param>
    /// <param name="stepValue">The step value by which the loop variable is incremented or decremented.</param>
    /// <param name="returnLineIndex">The index of the line to return to after the loop iteration.</param>
    /// <param name="returnStatementIndex">The index of the statement to return to after the loop iteration.</param>
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

    /// <summary>
    /// Gets the name of the loop variable associated with the current FOR loop state.
    /// </summary>
    /// <value>
    /// The name of the loop variable.
    /// </value>
    public string Variable { get; }

    /// <summary>
    /// Gets the end value of the FOR loop, which determines the condition for loop termination.
    /// </summary>
    public double EndValue { get; }

    /// <summary>
    /// Gets the step value by which the loop variable is incremented or decremented
    /// during each iteration of the FOR loop.
    /// </summary>
    public double StepValue { get; }

    /// <summary>
    /// Gets the index of the line to return to after the current iteration of the FOR loop.
    /// </summary>
    /// <remarks>
    /// This property is used to determine the execution flow when the loop continues to the next iteration.
    /// </remarks>
    public int ReturnLineIndex { get; }

    /// <summary>
    /// Gets the index of the statement to return to after the current iteration of the FOR loop.
    /// </summary>
    /// <remarks>
    /// This property is used to track the specific statement within a line of code
    /// that execution should resume at after completing or continuing a loop iteration.
    /// </remarks>
    public int ReturnStatementIndex { get; }

    /// <summary>
    /// Determines whether the FOR loop has completed based on the current value of the loop variable.
    /// </summary>
    /// <param name="currentValue">The current value of the loop variable.</param>
    /// <returns>
    /// <see langword="true"/> if the loop has completed; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The completion of the loop is determined by comparing the <paramref name="currentValue"/>
    /// to the <see cref="EndValue"/>. If the <see cref="StepValue"/> is positive, the loop is
    /// complete when <paramref name="currentValue"/> exceeds <see cref="EndValue"/>. If the
    /// <see cref="StepValue"/> is negative, the loop is complete when <paramref name="currentValue"/>
    /// is less than <see cref="EndValue"/>.
    /// </remarks>
    public bool IsComplete(double currentValue)
    {
        return StepValue >= 0 ? currentValue > EndValue : currentValue < EndValue;
    }
}