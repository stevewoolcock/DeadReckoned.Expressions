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
            static byte ReadI8(ref byte* ip) => *ip++;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static short ReadI16(ref byte* ip) => (short)((*ip++ << 8) | *ip++);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int ReadI32(ref byte* ip) => (*ip++ << 24) | (*ip++ << 16) | (*ip++ << 8) | *ip++;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static long ReadI64(ref byte* ip)
            {
                uint hi = (uint)((*ip++ << 24) | (*ip++ << 16) | (*ip++ << 8) | *ip++);
                uint lo = (uint)((*ip++ << 24) | (*ip++ << 16) | (*ip++ << 8) | *ip++);
                return (long)((ulong)hi) << 32 | lo;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float ReadF32(ref byte* ip)
            {
                int i = ReadI32(ref ip);
                return *(float*)&i;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static double ReadF64(ref byte* ip)
            {
                long tmp = ReadI64(ref ip);
                return *((double*)&tmp);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static Value UnaryArithmetric(in Value a, in UnaryArithmeticOps ops)
            {
                if (a.IsInteger)
                {
                    return ops.Integer(a.m_Integer);
                }

                if (a.IsDecimal)
                {
                    return ops.Decimal(a.m_Decimal);
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
                        return ops.Integer(a.m_Integer, b.m_Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.m_Integer, b.m_Decimal);
                    }
                }
                else if (a.IsDecimal)
                {
                    if (b.IsInteger)
                    {
                        return ops.Decimal(a.m_Decimal, b.m_Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.m_Decimal, b.m_Decimal);
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
                        return ops.Integer(a.m_Integer, b.m_Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.m_Integer, b.m_Decimal);
                    }
                }
                else if (a.IsDecimal)
                {
                    if (b.IsInteger)
                    {
                        return ops.Decimal(a.m_Decimal, b.m_Integer);
                    }

                    if (b.IsDecimal)
                    {
                        return ops.Decimal(a.m_Decimal, b.m_Decimal);
                    }
                }

                throw new ExpressionRuntimeException($"Binary operation is not valid for values of type '{a.m_Type}' and '{b.m_Type}'");
            }

            FastStack<Value> stack = m_Stack;
            fixed (byte* code = expr.m_ByteCode)
            {
                byte* ip = code;
                byte* end = code + expr.m_ByteCode.Length;
                while (ip < end)
                {
                    OpCode opCode = *(OpCode*)ip++;
                    switch (opCode)
                    {
                        case OpCode.Pop:
                            stack.Discard(1);
                            break;


                        case OpCode.Add:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Add);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Sub:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Sub);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.Mul:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Mul);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Div:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Div);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Xor:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = a.ToBool() ^ b.ToBool();
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Rem:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryArithmetic(in a, in b, in BinaryArithmeticOps.Rem);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Neg:
                            {
                                ref readonly Value a = ref stack.Pop();
                                Value v = UnaryArithmetric(in a, in UnaryArithmeticOps.Neg);
                                stack.Push(in v);
                            }
                            break;


                        case OpCode.Equal:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                stack.Push(a == b);
                            }
                            break;

                        case OpCode.NotEqual:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                stack.Push(a != b);
                            }
                            break;

                        case OpCode.GreaterEq:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.GreaterEq);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.LessEq:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.LessEq);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Less:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.Less);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Greater:
                            {
                                ref readonly Value b = ref stack.Pop();
                                ref readonly Value a = ref stack.Pop();
                                Value v = BinaryLogical(in a, in b, in BinaryLogicalOps.Greater);
                                stack.Push(in v);
                            }
                            break;

                        case OpCode.Not:
                            {
                                ref readonly Value v = ref stack.Pop();
                                stack.Push(IsFalsey(in v));
                            }
                            break;


                        case OpCode.LdNull:
                            stack.Push(Value.Null);
                            break;

                        case OpCode.LdTrue:
                            stack.Push(true);
                            break;

                        case OpCode.LdFalse:
                            stack.Push(false);
                            break;

                        case OpCode.LdI8:
                            {
                                var v = ReadI8(ref ip);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.LdI16:
                            {
                                var v = ReadI16(ref ip);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.LdI32:
                            {
                                var v = ReadI32(ref ip);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.LdI64:
                            {
                                var v = ReadI64(ref ip);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.LdF32:
                            {
                                var v = ReadF32(ref ip);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.LdF64:
                            {
                                var v = ReadF64(ref ip);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.LdStr:
                            {
                                var index = ReadI32(ref ip);
                                stack.Push(index);
                            }
                            break;

                        case OpCode.LdParam:
                            {
                                var index = ReadI32(ref ip);
                                var name = expr.m_Strings[index];

                                // Context takes precedence (local)
                                // Followed by engine (global)
                                if (!context.m_Params.TryGet(name, out Value v))
                                {
                                    if (!engine.m_Params.TryGet(name, out v))
                                    {
                                        throw new ExpressionRuntimeException($"Parameter '{name}' is not defined");
                                    }
                                }

                                stack.Push(in v);
                            }
                            break;


                        case OpCode.JumpIfFalse:
                            {
                                var offset = (ushort)ReadI16(ref ip);
                                if (IsFalsey(in stack.Peek()))
                                {
                                    ip += offset;
                                }
                            }
                            break;

                        case OpCode.JumpIfTrue:
                            {
                                var offset = (ushort)ReadI16(ref ip);
                                if (IsTruthy(in stack.Peek()))
                                {
                                    ip += offset;
                                }
                            }
                            break;

                        case OpCode.Call:
                            {
                                var argCount = ReadI8(ref ip);
                                var funcId = ReadI32(ref ip); 
                                if (!engine.TryGetFunction(funcId, out FunctionInfo funcInfo))
                                {
                                    throw new ExpressionRuntimeException($"Function not defined");
                                }

                                // Arguments are already on the stack, a read-only view is passed to the function
                                // The stack will not be modified while the function is executing
                                ReadOnlyMemory<Value> args = stack.PeekMemory(argCount);
                                FunctionCall call = new(engine, expr, context, args);
                                Value v = funcInfo.Function(call);

                                // Arguments are discarded and function return value pushed onto the stack
                                stack.Discard(argCount);
                                stack.Push(v);
                            }
                            break;

                        case OpCode.Return:
                            ip = end;
                            break;
                    }
                }
            }

            // Value remaining on the stack is returned
            Value retval = stack.Count > 0 ? stack.PopCopy() : Value.Null;

            // The stack should now be empty
            // If there's anything left on it, something has gone wrong
            System.Diagnostics.Debug.Assert(stack.Count == 0, "Stack count is > 0");

            return retval;
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
