using System;

namespace DeadReckoned.Expressions
{
    /// <summary>
    /// A compiled expression.
    /// </summary>
    public unsafe partial class Expression
    {
        internal readonly byte[] m_ByteCode;
        internal readonly string[] m_Strings;

        #region Properties

        /// <summary>
        /// The expression's bytecode sequence.
        /// </summary>
        public ReadOnlyMemory<byte> ByteCode => m_ByteCode;

        /// <summary>
        /// The string constants used by the expression.
        /// </summary>
        public ReadOnlyMemory<string> Strings => m_Strings;

        #endregion

        internal Expression(byte[] byteCode, string[] strings)
        {
            m_ByteCode = byteCode;
            m_Strings = strings;
        }
    }
}
