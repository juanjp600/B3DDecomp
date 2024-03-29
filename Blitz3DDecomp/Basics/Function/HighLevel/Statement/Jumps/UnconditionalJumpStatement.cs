﻿namespace Blitz3DDecomp.HighLevel;

sealed record UnconditionalJumpStatement(string SectionName) : Statement
{
    public override string StringRepresentation
        => $"Goto section{SectionName}";

    public override IEnumerable<Expression> InnerExpressions => Enumerable.Empty<Expression>();

    protected override Statement MapImplementation(
            Func<Statement, Statement> statementMapper,
            Func<Expression, Expression> expressionMapper)
        => statementMapper(this);
}