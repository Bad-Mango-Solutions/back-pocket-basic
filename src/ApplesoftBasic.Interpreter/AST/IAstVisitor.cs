// <copyright file="IAstVisitor.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.AST;

/// <summary>
/// Visitor interface for traversing the AST.
/// </summary>
public interface IAstVisitor<T>
{
    // Statements
    T VisitProgram(ProgramNode node);

    T VisitLine(LineNode node);

    T VisitPrintStatement(PrintStatement node);

    T VisitInputStatement(InputStatement node);

    T VisitLetStatement(LetStatement node);

    T VisitIfStatement(IfStatement node);

    T VisitGotoStatement(GotoStatement node);

    T VisitGosubStatement(GosubStatement node);

    T VisitReturnStatement(ReturnStatement node);

    T VisitForStatement(ForStatement node);

    T VisitNextStatement(NextStatement node);

    T VisitDimStatement(DimStatement node);

    T VisitReadStatement(ReadStatement node);

    T VisitDataStatement(DataStatement node);

    T VisitRestoreStatement(RestoreStatement node);

    T VisitEndStatement(EndStatement node);

    T VisitStopStatement(StopStatement node);

    T VisitRemStatement(RemStatement node);

    T VisitPokeStatement(PokeStatement node);

    T VisitCallStatement(CallStatement node);

    T VisitGetStatement(GetStatement node);

    T VisitOnGotoStatement(OnGotoStatement node);

    T VisitOnGosubStatement(OnGosubStatement node);

    T VisitDefStatement(DefStatement node);

    T VisitHomeStatement(HomeStatement node);

    T VisitHtabStatement(HtabStatement node);

    T VisitVtabStatement(VtabStatement node);

    T VisitTextStatement(TextStatement node);

    T VisitGrStatement(GrStatement node);

    T VisitHgrStatement(HgrStatement node);

    T VisitColorStatement(ColorStatement node);

    T VisitHcolorStatement(HcolorStatement node);

    T VisitPlotStatement(PlotStatement node);

    T VisitHplotStatement(HplotStatement node);

    T VisitDrawStatement(DrawStatement node);

    T VisitXdrawStatement(XdrawStatement node);

    T VisitInverseStatement(InverseStatement node);

    T VisitFlashStatement(FlashStatement node);

    T VisitNormalStatement(NormalStatement node);

    T VisitClearStatement(ClearStatement node);

    T VisitSleepStatement(SleepStatement node);

    T VisitHimemStatement(HimemStatement node);

    T VisitLomemStatement(LomemStatement node);

    // Expressions
    T VisitNumberLiteral(NumberLiteral node);

    T VisitStringLiteral(StringLiteral node);

    T VisitVariableExpression(VariableExpression node);

    T VisitBinaryExpression(BinaryExpression node);

    T VisitUnaryExpression(UnaryExpression node);

    T VisitFunctionCallExpression(FunctionCallExpression node);

    T VisitArrayAccessExpression(ArrayAccessExpression node);

    T VisitUserFunctionExpression(UserFunctionExpression node);
}