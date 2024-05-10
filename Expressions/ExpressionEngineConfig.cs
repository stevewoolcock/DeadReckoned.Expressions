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

        /// <summary>
        /// The initial size of the evaluation stack. This is the number of <see cref="Value"/> instances that can be placed onto the evaluation stack,
        /// before it must be grown. The stack will double in size each time it grows.<para/>
        /// The default value is <c>32</c>.
        /// </summary>
        public int InitialStackSize { get; set; } = 32;

        /// <summary>
        /// The maximum size of the evaluation stack. A <see cref="ExpressionRuntimeException"/> is thrown if the stack is exhausted.
        /// A value of zero places no explicit upper-limit on the stack size.<para/>
        /// The default value is <c>0</c>.
        /// </summary>
        public int MaxStackSize { get; set; } = 0;
    }
}
