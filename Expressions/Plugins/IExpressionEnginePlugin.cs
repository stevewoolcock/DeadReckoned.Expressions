using System.Collections.Generic;

namespace DeadReckoned.Expressions.Plugins
{
    public interface IExpressionEnginePlugin
    {
        /// <summary>
        /// Gets an enumeration of <see cref="FunctionDefinition"/> objects the plugin
        /// will register with the engine, when loaded.
        /// </summary>
        /// <returns></returns>
        IEnumerable<FunctionDefinition> GetFunctions();
    }

    public class FunctionDefinition
    {
        /// <summary>
        /// The name of the function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The C# delegate executed when the function is invoked.
        /// </summary>
        public ExpressionEngine.Function Function { get; set; }

        public FunctionDefinition(string name, ExpressionEngine.Function func)
        {
            Name = name;
            Function = func;
        }
    }
}
