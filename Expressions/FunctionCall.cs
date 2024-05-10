using System;

namespace DeadReckoned.Expressions
{
    /// <summary>
    /// Contains information relating to a function call
    /// </summary>
    public readonly struct FunctionCall
    {
        /// <summary>
        /// The <see cref="ExpressionEngine"/> executing the function.
        /// </summary>
        public readonly ExpressionEngine Engine;

        /// <summary>
        /// The <see cref="ExpressionEngine.Expression"/> executing the function.
        /// </summary>
        public readonly Expression Expression;

        /// <summary>
        /// The <see cref="ExpressionContext"/> passed to the evaluation.
        /// </summary>
        public readonly ExpressionContext Context;

        /// <summary>
        /// The argument collection passed to the function call.
        /// </summary>
        public readonly FunctionArgs Args;

        internal FunctionCall(ExpressionEngine engine, Expression expr, ExpressionContext ctx, in ReadOnlyMemory<Value> args)
        {
            Engine = engine;
            Expression = expr;
            Context = ctx;
            Args = new FunctionArgs(args);
        }
    }
}