using DeadReckoned.Expressions.Plugins;
using System;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions
{
    public partial class ExpressionEngine
    {
        private static class BuiltInFunctions
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Value UnaryArithmetricOp(in Value value, Func<long, Value> integerOp, Func<double, Value> decimalOp)
            {
                return value.m_Type switch
                {
                    ValueType.Integer => integerOp(value.Integer),
                    ValueType.Decimal => decimalOp(value.Decimal),
                    _ => throw new ExpressionRuntimeException($"Argument type '{value.m_Type}' is not valid"),
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Value BinaryArithmetricOp(in Value a, in Value b, Func<long, long, Value> integerOp, Func<double, double, Value> decimalOp)
            {
                if (a.IsInteger)
                {
                    if (b.IsInteger)
                    {
                        return integerOp(a.Integer, b.Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return decimalOp(a.Integer, b.Decimal);
                    }

                    throw new ExpressionRuntimeException($"Argument '{nameof(b)}' type '{a.m_Type}' is not valid");
                }

                if (a.IsDecimal)
                {
                    if (b.IsInteger)
                    {
                        return decimalOp(a.Decimal, b.Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return decimalOp(a.Decimal, b.Decimal);
                    }

                    throw new ExpressionRuntimeException($"Argument '{nameof(b)}' type '{a.m_Type}' is not valid");
                }

                throw new ExpressionRuntimeException($"Argument '{nameof(a)}' type '{a.m_Type}' is not valid");
            }

            public static readonly FunctionDefinition[] Functions = new FunctionDefinition[]
            {
                // Type Coercion
                new("BOOL", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].ToBool();
                }),

                new("INTEGER", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].ToInteger();
                }),

                new("DECIMAL", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].ToDecimal();
                }),


                new("IS_BOOL", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].IsBool;
                }),

                new("IS_INTEGER", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].IsInteger;
                }),

                new("IS_DECIMAL", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].IsDecimal;
                }),

                new("IS_NUMBER", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].IsNumber;
                }),

                new("IS_NULL", call =>
                {
                    call.Args.EnsureCount(1);
                    return call.Args[0].IsNumber;
                }),


                // Logic
                new("AND", call =>
                {
                    call.Args.EnsureMinCount(1);
                    for (int i = 0; i < call.Args.Count; i++)
                    {
                        if (!call.Args[i].ToBool())
                            return false;
                    }
                    return true;
                }),

                new("IF", call =>
                {
                    call.Args.EnsureCount(3);
                    return call.Args[0].ToBool() ? call.Args[1] : call.Args[2];
                }),

                new("OR", call =>
                {
                    call.Args.EnsureMinCount(1);
                    for (int i = 0; i < call.Args.Count; i++)
                    {
                        if (call.Args[i].ToBool())
                            return true;
                    }
                    return false;
                }),

                new("XOR", call =>
                {
                    call.Args.EnsureCount(2);
                    return call.Args[0] == call.Args[1] ? 1 : 0;
                }),

                new("ISNAN", call =>
                {
                    call.Args.EnsureCount(1);
                    return double.IsNaN(call.Args[0].Decimal);
                }),


                // Math
                new("E", call => Math.E),

                new("ABS", call =>
                {
                    call.Args.EnsureCount(1);
                    return UnaryArithmetricOp(in call.Args[0], v => v < 0 ? -v : v, v => v < 0 ? -v : v);
                }),

                new("CEIL", call =>
                {
                    call.Args.EnsureCount(1);
                    return UnaryArithmetricOp(in call.Args[0], v => v, v => Math.Ceiling(v));
                }),

                new("CLAMP", call =>
                {
                    call.Args.EnsureCount(3);
                    ref readonly Value arg0 = ref call.Args[0];
                    double clamped = Math.Clamp(arg0.Number, call.Args[1].Number, call.Args[2].Number);
                    return arg0.m_Type == ValueType.Integer ? (long)clamped : clamped;
                }),

                new("EXP", call =>
                {
                    call.Args.EnsureCount(1);
                    return Math.Exp(call.Args[0].Number);
                }),

                new("FLOOR", call =>
                {
                    call.Args.EnsureCount(1);
                    return UnaryArithmetricOp(in call.Args[0], v => v, v => Math.Floor(v));
                }),

                new("LOG", call =>
                {
                    call.Args.EnsureMinMaxCount(1, 2);
                    return call.Args.Count == 1
                        ? Math.Log(call.Args[0].Number)
                        : Math.Log(call.Args[0].Number, call.Args[1].Number);
                }),

                new("LOG10", call =>
                {
                    call.Args.EnsureMinCount(1);
                    return Math.Log10(call.Args[0].Number);
                }),

                new("LOG2", call =>
                {
                    call.Args.EnsureMinCount(1);
                    return Math.Log(call.Args[0].Number, 2);
                }),

                new("MAX", call =>
                {
                    call.Args.EnsureMinCount(2);

                    if (call.Args.Count == 2)
                    {
                        return BinaryArithmetricOp(in call.Args[0], in call.Args[1], (a, b) => Math.Max(a, b), (a, b) => Math.Max(a, b));
                    }

                    Value arg = call.Args[0];
                    double result = arg.Number;
                    bool isDecimal = arg.IsDecimal;
                    for (int i = 1; i < call.Args.Count; i++)
                    {
                        arg = call.Args[i];
                        result = Math.Max(result, arg.Number);
                        isDecimal |= arg.IsDecimal;
                    }

                    return isDecimal ? result : (long)result;
                }),

                new("MIN", call =>
                {
                    call.Args.EnsureMinCount(2);

                    if (call.Args.Count == 2)
                    {
                        return BinaryArithmetricOp(in call.Args[0], in call.Args[1], (a, b) => Math.Min(a, b), (a, b) => Math.Min(a, b));
                    }

                    Value arg = call.Args[0];
                    double result = arg.Number;
                    bool isDecimal = arg.IsDecimal;
                    for (int i = 1; i < call.Args.Count; i++)
                    {
                        arg = call.Args[i];
                        result = Math.Min(result, arg.Number);
                        isDecimal |= arg.IsDecimal;
                    }

                    return isDecimal ? result : (long)result;
                }),

                new("POW", call =>
                {
                    call.Args.EnsureCount(2);
                    return Math.Pow(call.Args[0].Number, call.Args[1].Number);
                }),

                new("ROUND", call =>
                {
                    call.Args.EnsureCount(1);
                    return UnaryArithmetricOp(in call.Args[0], v => v, v => Math.Round(v));
                }),

                new("SIGN", call =>
                {
                    call.Args.EnsureCount(1);
                    return UnaryArithmetricOp(in call.Args[0], v => Math.Sign(v), v => Math.Sign(v));
                }),

                new("SQRT", call =>
                {
                    call.Args.EnsureMinCount(1);
                    return UnaryArithmetricOp(in call.Args[0], v => Math.Sqrt(v), v => Math.Sqrt(v));
                }),

                new("SUM", call =>
                {
                    call.Args.EnsureMinCount(2);
                    return call.Args.Span.Sum();
                }),

                new("TRUNC", call =>
                {
                    call.Args.EnsureMinCount(1);
                    return UnaryArithmetricOp(in call.Args[0], v => v, v => Math.Truncate(v));
                }),
            };
        }
    }
}
