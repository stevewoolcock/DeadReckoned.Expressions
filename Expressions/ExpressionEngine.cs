using DeadReckoned.Expressions.Internal;
using DeadReckoned.Expressions.Plugins;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions
{
    public partial class ExpressionEngine
    {
        public delegate Value Function(FunctionCall call);

        internal readonly ExpressionEngineConfig m_Config;
        internal readonly ExpressionParameterCollection m_Params = new();
        private Compiler m_Compiler;
        private VM m_Vm;

        private int m_NextFunctionId = 1;
        private readonly Dictionary<int, FunctionInfo> m_FunctionsById = new();
        private readonly Dictionary<string, FunctionInfo> m_FunctionsByName = new(StringComparer.OrdinalIgnoreCase);

        #region Properties

        /// <summary>
        /// Gets the collection of global parameters available to all expressions evaluated by this engine.
        /// </summary>
        public ExpressionParameterCollection Params => m_Params; 

        #endregion

        public ExpressionEngine(ExpressionEngineConfig config)
        {
            m_Config = config;
            InitializeEngine(m_Config.Plugins);
        }

        public ExpressionEngine(params IExpressionEnginePlugin[] plugins)
        {
            m_Config = ExpressionEngineConfig.CreateDefault();
            InitializeEngine(plugins);
        }

        #region Public API

        public CompileResult Compile(string source, bool throwOnFailure = true) => Compile(source.AsMemory(), throwOnFailure);

        public CompileResult Compile(string source, int start, int length, bool throwOnFailure = true) => Compile(source.AsMemory(start, length), throwOnFailure);

        public CompileResult Compile(ReadOnlyMemory<char> source, bool throwOnFailure = true)
        {
            m_Compiler ??= new Compiler();
            try
            {
                Expression expr = m_Compiler.Compile(this, source);
                return new CompileResult(expr);
            }
            catch (Exception ex)
            {
                if (throwOnFailure)
                    throw;

                return new CompileResult(ex);
            }
        }

        public EvaluateResult Evaluate(ReadOnlyMemory<char> expression, ExpressionContext context = null, bool throwOnFailure = true)
        {
            Expression expr;
            try
            {
                expr = Compile(expression, throwOnFailure);
            }
            catch (Exception ex)
            {
                if (throwOnFailure)
                    throw;

                return new EvaluateResult(ex);
            }

            return Evaluate(expr, context, throwOnFailure);
        }

        public EvaluateResult Evaluate(string expression, ExpressionContext context = null, bool throwOnFailure = true)
        {
            Expression expr;
            try
            {
                expr = Compile(expression, true);
            }
            catch (Exception ex)
            {
                if (throwOnFailure)
                    throw;

                return new EvaluateResult(ex);
            }

            return Evaluate(expr, context, throwOnFailure);
        }

        public EvaluateResult Evaluate(Expression expression, ExpressionContext context = null, bool throwOnFailure = true)
        {
            m_Vm ??= new VM();
            try
            {
                Value value = m_Vm.Evaluate(this, expression, context ?? ExpressionContext.Empty);

                // Ensure the configuration's numeric mode is respected for the output value
                // It could be changed by a function at some point during evaluation
                if (value.IsDecimal && m_Config.NumericMode == NumericMode.Integer)
                {
                    value = Convert.ToInt64(value.Decimal);
                }
                else if (value.IsInteger && m_Config.NumericMode == NumericMode.Decimal)
                {
                    value = Convert.ToDouble(value.Integer);
                }

                return new EvaluateResult(value);
            }
            catch (Exception ex)
            {
                if (throwOnFailure)
                {
                    throw;
                }

                return new EvaluateResult(ex);
            }
        }

        public void SetFunction(string name, Function fn)
        {
            FunctionInfo fnInfo = new()
            {
                Id = m_NextFunctionId++,
                Name = name,
                Function = fn
            };

            m_FunctionsByName[name] = fnInfo;
            m_FunctionsById[fnInfo.Id] = fnInfo;
        }

        #endregion

        private void InitializeEngine(IEnumerable<IExpressionEnginePlugin> plugins)
        {
            SetFunctions(BuiltInFunctions.Functions);

            if (plugins != null)
            {
                foreach (IExpressionEnginePlugin plugin in plugins)
                {
                    SetFunctions(plugin.GetFunctions());
                }
            }

            bool IsDisabled(string name) => m_Config.DisableFunctions.Contains(name);

            void SetFunctions(IEnumerable<FunctionDefinition> functions)
            {
                if (functions == null)
                    return;

                foreach (FunctionDefinition fnDef in functions)
                {
                    if (!IsDisabled(fnDef.Name))
                    {
                        SetFunction(fnDef.Name, fnDef.Function);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetFunction(int id, out FunctionInfo type) => m_FunctionsById.TryGetValue(id, out type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetFunction(string name, out FunctionInfo type) => m_FunctionsByName.TryGetValue(name, out type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetFunction(ReadOnlySpan<char> name, out FunctionInfo type)
        {
            foreach (var pair in m_FunctionsByName)
            {
                if (name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    type = pair.Value;
                    return true;
                }
            }

            type = default;
            return false;
        }
    }
}