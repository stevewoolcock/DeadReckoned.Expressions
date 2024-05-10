using System;

namespace DeadReckoned.Expressions
{
    public readonly struct EvaluateResult
    {
        /// <summary>
        /// True if expression compilation was successful.
        /// </summary>
        public readonly bool Success;

        /// <summary>
        /// Contains the <see cref="DeadReckoned.Expressions.Value"/> that was returned by the evaluation.
        /// </summary>
        public readonly Value Value;

        /// <summary>
        /// If <see cref="Success"/> is false, contains any exception thrown by the compiler.
        /// </summary>
        public readonly Exception Exception;

        internal EvaluateResult(in Value value)
        {
            Success = true;
            Value = value;
            Exception = null;
        }

        internal EvaluateResult(Exception exception)
        {
            Success = false;
            Value = default;
            Exception = exception;
        }

        #region Operators

        public static implicit operator Value(EvaluateResult result) => result.Value;

        #endregion
    }
}