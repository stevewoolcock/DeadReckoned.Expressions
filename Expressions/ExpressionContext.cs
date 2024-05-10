namespace DeadReckoned.Expressions
{
    public class ExpressionContext
    {
        #region Static

        internal static readonly ExpressionContext Empty = new();

        #endregion

        internal readonly ExpressionParameterCollection m_Params = new();

        /// <summary>
        /// The collection of parameters available to the the expression.
        /// </summary>
        public ExpressionParameterCollection Params => m_Params;

        /// <summary>
        /// Gets or sets an object that will be made available to the expression
        /// engine during evaluation, and when functions are invoked.
        /// </summary>
        public object UserData { get; set; }
    }
}
