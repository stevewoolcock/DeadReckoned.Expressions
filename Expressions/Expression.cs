using System;

namespace DeadReckoned.Expressions
{
    /// <summary>
    /// A compiled expression.
    /// </summary>
    public partial class Expression
    {
        #region Properties

        /// <summary>
        /// The expression's bytecode sequence.
        /// </summary>
        public ReadOnlyMemory<byte> ByteCode { get; internal set; }

        /// <summary>
        /// The string constants used by the expression.
        /// </summary>
        public ReadOnlyMemory<string> Strings { get; internal set; }

        #endregion

        internal Expression() { }

        public Expression(ReadOnlyMemory<byte> byteCode, ReadOnlyMemory<string> strings)
        {
            ByteCode = byteCode;
            Strings = strings;
        }
    }
}
