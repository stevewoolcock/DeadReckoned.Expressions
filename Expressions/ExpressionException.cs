using System;

namespace DeadReckoned.Expressions
{
    public class ExpressionException : Exception
    {
        public ExpressionException(string message, Exception innerException = default) : base(message, innerException) { }
    }

    public class ExpressionCompileException : ExpressionException
    {
        public ExpressionCompileException(string message, Exception innerException = default) : base(message, innerException) { }
    }

    public class ExpressionRuntimeException : ExpressionException
    {
        public ExpressionRuntimeException(string message, Exception innerException = default) : base(message, innerException) { }
    }
}
