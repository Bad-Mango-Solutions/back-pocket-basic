// <copyright file="UserFunction.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

using AST;

/// <summary>
/// Represents a user-defined function within the Applesoft BASIC interpreter runtime.
/// </summary>
/// <remarks>
/// A user-defined function consists of a name, a parameter, and a body represented as an <see cref="IExpression"/>.
/// This class is used to encapsulate the logic and structure of such functions, enabling their definition and execution
/// within the interpreter.
/// </remarks>
public class UserFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserFunction"/> class with the specified name, parameter, and body.
    /// </summary>
    /// <param name="name">The name of the user-defined function.</param>
    /// <param name="parameter">The name of the parameter for the function.</param>
    /// <param name="body">The body of the function, represented as an <see cref="IExpression"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name"/>, <paramref name="parameter"/>, or <paramref name="body"/> is <c>null</c>.
    /// </exception>
    public UserFunction(string name, string parameter, IExpression body)
    {
        Name = name;
        Parameter = parameter;
        Body = body;
    }

    /// <summary>
    /// Gets the name of the user-defined function.
    /// </summary>
    /// <value>
    /// The name of the function as a <see cref="string"/>.
    /// </value>
    /// <remarks>
    /// The name uniquely identifies the function within the Applesoft BASIC interpreter runtime.
    /// It is specified during the creation of the <see cref="UserFunction"/> instance and cannot be modified.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets the name of the parameter for the user-defined function.
    /// </summary>
    /// <remarks>
    /// The parameter represents the variable name used within the function body to refer to the argument passed during the function's invocation.
    /// </remarks>
    public string Parameter { get; }

    /// <summary>
    /// Gets the body of the user-defined function.
    /// </summary>
    /// <remarks>
    /// The body of the function is represented as an <see cref="IExpression"/> and defines the logic
    /// or computation performed by the function. It is evaluated when the function is invoked.
    /// </remarks>
    public IExpression Body { get; }
}