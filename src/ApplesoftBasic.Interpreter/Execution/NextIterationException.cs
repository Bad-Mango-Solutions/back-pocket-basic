// <copyright file="NextIterationException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Execution;

/// <summary>
/// Exception used to signal loop continuation.
/// </summary>
internal class NextIterationException : Exception { }