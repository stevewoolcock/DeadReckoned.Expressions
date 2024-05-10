using System;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions.Internal
{
    internal class FunctionInfo
    {
        public int Id;
        public string Name;
        public ExpressionEngine.Function Function;
    }

    internal unsafe class VM
    {
        private readonly struct UnaryArithmeticOps
        {
            public readonly Func<long, long> Integer;
            public readonly Func<double, double> Decimal;

            private UnaryArithmeticOps(Func<long, long> integerOp, Func<double, double> decimalOp)
            {
                Integer = integerOp;
                Decimal = decimalOp;
            }

            public static readonly UnaryArithmeticOps Neg = new(a => -a, a => -a);
        }

        private readonly struct BinaryArithmeticOps
        {
            public readonly Func<long, long, long> Integer;
            public readonly Func<double, double, double> Decimal;

            private BinaryArithmeticOps(Func<long, long, long> integerOp, Func<double, double, double> decimalOp)
            {
                Integer = integerOp;
                Decimal = decimalOp;
            }

            public static readonly BinaryArithmeticOps Add = new((a, b) => a + b, (a, b) => a + b);
            public static readonly BinaryArithmeticOps Sub = new((a, b) => a - b, (a, b) => a - b);
            public static readonly BinaryArithmeticOps Mul = new((a, b) => a * b, (a, b) => a * b);
            public static readonly BinaryArithmeticOps Div = new((a, b) => a / b, (a, b) => a / b);
            public static readonly BinaryArithmeticOps Rem = new((a, b) => a % b, (a, b) => a % b);
        }

        private readonly struct BinaryLogicalOps
        {
            public readonly Func<long, long, bool> Integer;
            public readonly Func<double, double, bool> Decimal;

            private BinaryLogicalOps(Func<long, long, bool> integerOp, Func<double, double, bool> decimalOp)
            {
                Integer = integerOp;
                Decimal = decimalOp;
            }

            public static readonly BinaryLogicalOps Equal = new((a, b) => a == b, (a, b) => a == b);
            public static readonly BinaryLogicalOps Greater = new((a, b) => a > b, (a, b) => a > b);
            public static readonly BinaryLogicalOps GreaterEq = new((a, b) => a >= b, (a, b) => a >= b);
            public static readonly BinaryLogicalOps Less = new((a, b) => a < b, (a, b) => a < b);
            public static readonly BinaryLogicalOps LessEq = new((a, b) => a <= b, (a, b) => a <= b);
        }

        private FastStack<Value> m_Stack;

        private void InitializeStack(ExpressionEngineConfig config)
        {
            if (m_Stack == null)
            {
                int initSize = config.InitialStackSize;
                if (initSize < 0)
                {
                    initSize = 32;
                }

                m_Stack = new FastStack<Value>(initSize, config.MaxStackSize);
            }

            m_Stack.Clear();
        }

        internal Value Evaluate(ExpressionEngine engine, Expression expr, ExpressionContext context)
        {
            if (expr.m_ByteCode.Length == 0)
                return Value.Null;

            InitializeStack(engine.m_Config);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static byte ReadI8(byte* code, ref int ip) => code[ip++];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static short ReadI16(byte* code, ref int ip) => (short)((code[ip++] << 8) | code[ip++]);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int ReadI32(byte* code, ref int ip) => (code[ip++] << 24) | (code[ip++] << 16) | (code[ip++] << 8) | code[ip++];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static long ReadI64(byte* code, ref int ip)
            {
                uint hi = (uint)((code[ip++] << 24) | (code[ip++] << 16) | (code[ip++] << 8) | code[ip++]);
                uint lo = (uint)((code[ip++] << 24) | (code[ip++] << 16) | (code[ip++] << 8) | code[ip++]);
                return (long)((ulong)hi) << 32 | lo;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float ReadF32(byte* code, ref int ip)
            {
                int i = ReadI32(code, ref ip);
                return *(float*)&i;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static double ReadF64(byte* code, ref int ip)
            {
                long tmp = ReadI64(code, ref ip);
                return *((double*)&tmp);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Value UnaryArithmetric(in Value a, in UnaryArithmeticOps ops)
            {
                if (a.IsInteger)
                {
                    return ops.Integer(a.Integer);
                }

                if (a.IsDecimal)
                {
                    return ops.Decimal(a.Decimal);
                }

                throw new ExpressionRuntimeException($"Unary operation is not valid for value of type '{a.m_Type}'");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Value BinaryArithmetic(in Value a, in Value b, in BinaryArithmeticOps ops)
            {
                if (a.IsInteger)
                {
                    if (b.IsInteger)
                    {
                        return ops.Integer(a.Integer, b.Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.Integer, b.Decimal);
                    }
                }
                else if (a.IsDecimal)
                {
                    if (b.IsInteger)
                    {
                        return ops.Decimal(a.Decimal, b.Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.Decimal, b.Decimal);
                    }
                }

                throw new ExpressionRuntimeException($"Binary operation is not valid for values of type '{a.m_Type}' and '{b.m_Type}'");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Value BinaryLogical(in Value a, in Value b, in BinaryLogicalOps ops)
            {
                if (a.IsInteger)
                {
                    if (b.IsInteger)
                    {
                        return ops.Integer(a.Integer, b.Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.Integer, b.Decimal);
                    }
                }
                else if (a.IsDecimal)
                {
                    if (b.IsInteger)
                    {
                        return ops.Decimal(a.Decimal, b.Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.Decimal, b.Decimal);
                    }
                }

                throw new ExpressionRuntimeException($"Binary operation is not valid for values of type '{a.m_Type}' and '{b.m_Type}'");
            }

            int ip = 0;
            fixed (byte* code = expr.m_ByteCode)
            {
                while (true)
                {
                    OpCode opCode = (OpCode)code[ip++];
                    switch (opCode)
                    {
                        case OpCode.Pop:
                            m_Stack.Pop();
                            break;


                        case OpCode.Add:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Add);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Sub:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Sub);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Mul:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Mul);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Div:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Div);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Xor:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = a.ToBool() ^ b.ToBool();
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Rem:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Rem);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Neg:
                            {
                                Value a = m_Stack.Pop();
                                Value v = UnaryArithmetric(in a, in UnaryArithmeticOps.Neg);
                                m_Stack.Push(v);
                            }
                            break;


                        case OpCode.Equal:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                m_Stack.Push(a == b);
                            }
                            break;

                        case OpCode.NotEqual:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                m_Stack.Push(a != b);
                            }
                            break;

                        case OpCode.GreaterEq:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.GreaterEq);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.LessEq:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.LessEq);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Less:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.Less);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Greater:
                            {
                                (Value a, Value b) = m_Stack.Pop2Reverse();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.Greater);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.Not:
                            {
                                Value v = m_Stack.Pop();
                                m_Stack.Push(IsFalsey(v));
                            }
                            break;


                        case OpCode.LdNull:
                            m_Stack.Push(Value.Null);
                            break;

                        case OpCode.LdTrue:
                            m_Stack.Push(true);
                            break;

                        case OpCode.LdFalse:
                            m_Stack.Push(false);
                            break;

                        case OpCode.LdI8:
                            {
                                var v = ReadI8(code, ref ip);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.LdI16:
                            {
                                var v = ReadI16(code, ref ip);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.LdI32:
                            {
                                var v = ReadI32(code, ref ip);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.LdI64:
                            {
                                var v = ReadI64(code, ref ip);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.LdF32:
                            {
                                var v = ReadF32(code, ref ip);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.LdF64:
                            {
                                var v = ReadF64(code, ref ip);
                                m_Stack.Push(v);
                            }
                            break;

                        case OpCode.LdStr:
                            {
                                int index = ReadI32(code, ref ip);
                                m_Stack.Push(index);
                            }
                            break;

                        case OpCode.LdParam:
                            {
                                int index = ReadI32(code, ref ip);
                                string name = expr.m_Strings[index];

                                // Context takes precedence (local)
                                // Followed by engine (global)
                                if (!context.m_Params.TryGet(name, out Value value))
                                {
                                    if (!engine.m_Params.TryGet(name, out value))
                                    {
                                        throw new ExpressionRuntimeException($"Parameter '{name}' is not defined");
                                    }
                                }

                                m_Stack.Push(value);
                            }
                            break;


                        case OpCode.JumpIfFalse:
                            {
                                ushort offset = (ushort)ReadI16(code, ref ip);
                                if (IsFalsey(m_Stack.Peek()))
                                {
                                    ip += offset;
                                }
                            }
                            break;

                        case OpCode.JumpIfTrue:
                            {
                                ushort offset = (ushort)ReadI16(code, ref ip);
                                if (IsTruthy(m_Stack.Peek()))
                                {
                                    ip += offset;
                                }
                            }
                            break;

                        case OpCode.Call:
                            {
                                // Number of arguments passed
                                byte argCount = ReadI8(code, ref ip);
                                
                                int funcId = ReadI32(code, ref ip); 
                                if (!engine.TryGetFunction(funcId, out FunctionInfo fnInfo))
                                {
                                    throw new ExpressionRuntimeException($"Function not defined");
                                }

                                // Arguments are already on the stack, a read-only view is passed to the function
                                // The stack will not be modified while the function is executing
                                ReadOnlyMemory<Value> args = m_Stack.PeekMemory(argCount);
                                FunctionCall call = new(engine, expr, context, args);
                                Value retVal = fnInfo.Function(call);

                                // Arguments are discarded and function return value pushed onto the stack
                                m_Stack.Discard(argCount);
                                m_Stack.Push(retVal);
                            }
                            break;

                        case OpCode.Return:
                            {
                                Value v = m_Stack.Count > 0 ? m_Stack.Pop() : Value.Null;
                                System.Diagnostics.Debug.Assert(m_Stack.Count == 0, "Stack count is > 0");
                                return v;
                            }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTruthy(in Value value)
        {
            return value.Type switch
            {
                ValueType.Bool => value.Bool,
                ValueType.Integer => value.Integer != 0,
                ValueType.Decimal => value.Decimal != 0,
                _ => false,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFalsey(in Value value) => !IsTruthy(in value);
    }
}
