using DeadReckoned.Expressions.Plugins;
using System;
using System.Collections.Generic;

namespace DeadReckoned.Expressions
{
    public enum NumericMode
    {
        /// <summary>
        /// Allow for strongly typed integer and decimal numbers.
        /// </summary>
        All,

        /// <summary>
        /// Compile all numbers in an expression as integers. This may result in data
        /// loss if decimal numbers are used an expression.
        /// Ensures the return value of an expression is also a integer.
        /// </summary>
        Integer,

        /// <summary>
        /// Compile all numbers in an expression as decimals.
        /// Ensures the return value of an expression is also a decimal.
        /// </summary>
        Decimal,
    }

    public class ExpressionEngineConfig
    {
        public static ExpressionEngineConfig CreateDefault()
        {
            return new ExpressionEngineConfig();
        }

        /// <summary>
        /// The numeric mode supported by the engine.
        /// </summary>
        public NumericMode NumericMode { get; set; } = NumericMode.All;

        /// <summary>
        /// The list of plugins to load when the engine is initialized.
        /// </summary>
        public IEnumerable<IExpressionEnginePlugin> Plugins { get; set; }

        /// <summary>
        /// A set of function names disable. Functions names in this set will not be initialised or available
        /// to the engine at runtime. Functions created using <see cref="ExpressionEngine.SetFunction(string, ExpressionEngine.Function)"/>
        /// are not affected.
        /// </summary>
        public HashSet<string> DisableFunctions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
