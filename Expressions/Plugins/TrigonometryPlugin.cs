using System;
using System.Collections.Generic;

namespace DeadReckoned.Expressions.Plugins
{
    /// <summary>
    /// Adds support for trigonmetric functions to an <see cref="ExpressionEngine"/>.
    /// </summary>
    public class TrigonometryPlugin : IExpressionEnginePlugin
    {
        private readonly FunctionDefinition[] m_Functions = new[]
        {
            new FunctionDefinition("ACOS", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Acos(call.Args[0].Number);
            }),

            new FunctionDefinition("ACOSH", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Acosh(call.Args[0].Number);
            }),

            new FunctionDefinition("ASIN", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Asin(call.Args[0].Number);
            }),

            new FunctionDefinition("ASINH", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Asinh(call.Args[0].Number);
            }),

            new FunctionDefinition("ATAN", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Atan(call.Args[0].Number);
            }),

            new FunctionDefinition("ATAN2", call =>
            {
                call.Args.EnsureCount(2);
                return Math.Atan2(call.Args[0].Number, call.Args[1].Number);
            }),

            new FunctionDefinition("ATANH", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Atanh(call.Args[0].Number);
            }),

            new FunctionDefinition("COS", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Cos(call.Args[0].Number);
            }),

            new FunctionDefinition("COSH", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Cosh(call.Args[0].Number);
            }),

            new FunctionDefinition("DEGREES", call =>
            {
                call.Args.EnsureCount(1);
                return call.Args[0].Number * 180.0 / Math.PI;
            }),

            new FunctionDefinition("PI", call => Math.PI),

            new FunctionDefinition("PI2", call => Math.PI * 2),

            new FunctionDefinition("RADIANS", call =>
            {
                call.Args.EnsureCount(1);
                return call.Args[0].Number * Math.PI / 180.0;
            }),

            new FunctionDefinition("SIN", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Sin(call.Args[0].Number);
            }),

            new FunctionDefinition("SINH", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Sinh(call.Args[0].Number);
            }),

            new FunctionDefinition("TAN", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Tan(call.Args[0].Number);
            }),

            new FunctionDefinition("TANH", call =>
            {
                call.Args.EnsureCount(1);
                return Math.Tanh(call.Args[0].Number);
            }),
        };

        /// <inheritdoc/>
        public IEnumerable<FunctionDefinition> GetFunctions() => m_Functions;
    }
}
