using System;

namespace DeadReckoned.Expressions
{
    public readonly struct CompileResult
    {
        /// <summary>
        /// True if expression compilation was successful.
        /// </summary>
        public readonly bool Success;

        /// <summary>
        /// If <see cref="Success"/> is true, contains the <see cref="ExpressionEngine.Expression"/> that was compiled.
        /// </summary>
        public readonly Expression Expression;

        /// <summary>
        /// If <see cref="Success"/> is false, contains any exception thrown by the compiler.
        /// </summary>
        public readonly Exception Exception;

        internal CompileResult(Expression expression)
        {
            Success = true;
            Expression = expression;
            Exception = null;
        }

        internal CompileResult(Exception exception)
        {
            Success = false;
            Expression = default;
            Exception = exception;
        }

        #region Operators

        public static implicit operator Expression(CompileResult result) => result.Expression;

        #endregion
    }
}