using System;

namespace DeadReckoned.Expressions
{
    public static class ExpressionExtensions
    {
        public static bool IsType(this ReadOnlySpan<Value> values, int index, ValueType type)
        {
            if (index < 0 || index >= values.Length)
            {
                return false;
            }

            return values[index].m_Type == type;
        }

        public static bool IsBool(this ReadOnlySpan<Value> values, int index)
        {
            if (index < 0 || index >= values.Length)
            {
                return false;
            }

            return values[index].IsBool;
        }

        public static bool IsNumber(this ReadOnlySpan<Value> values, int index)
        {
            if (index < 0 || index >= values.Length)
            {
                return false;
            }

            return values[index].IsNumber;
        }

        public static bool IsInteger(this ReadOnlySpan<Value> values, int index)
        {
            if (index < 0 || index >= values.Length)
            {
                return false;
            }

            return values[index].IsInteger;
        }

        public static bool IsDecimal(this ReadOnlySpan<Value> values, int index)
        {
            if (index < 0 || index >= values.Length)
            {
                return false;
            }

            return values[index].IsDecimal;
        }


        public static bool GetBool(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out bool value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Bool}'");
        }

        public static byte GetI8(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out byte value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Integer}'");
        }

        public static short GetI16(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out short value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Integer}'");
        }

        public static int GetI32(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out int value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Integer}'");
        }

        public static long GetI64(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out long value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Integer}'");
        }

        public static float GetF32(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out float value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Decimal}'");
        }

        public static double GetF64(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out double value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Decimal}'");
        }

        public static double GetNumber(this ReadOnlySpan<Value> values, int index)
        {
            return values[index].TryAs(out double value)
                ? value
                : throw new ExpressionRuntimeException($"Value {index} is not of type '{ValueType.Integer}' or '{ValueType.Decimal}'");
        }


        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out Value value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            value = values[index];
            return true;
        }

        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out bool value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAs(out value);
        }

        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out byte value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAs(out value);
        }

        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out short value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAs(out value);
        }

        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out int value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAs(out value);
        }

        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out long value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAs(out value);
        }

        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out float value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAs(out value);
        }

        public static bool TryGet(this ReadOnlySpan<Value> values, int index, out double value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAs(out value);
        }

        public static bool TryGetNumber(this ReadOnlySpan<Value> values, int index, out double value)
        {
            if (index < 0 || index >= values.Length)
            {
                value = default;
                return false;
            }

            return values[index].TryAsNumber(out value);
        }


        public static double Sum(this ReadOnlySpan<Value> values)
        {
            double sum = 0;
            foreach (Value v in values)
            {
                sum += v.Number;
            }
            return sum;
        }

        public static double SumDecimal(this ReadOnlySpan<Value> values)
        {
            double sum = 0;
            foreach (var v in values)
            {
                sum += v.AsF64;
            }
            return sum;
        }

        public static long SumInteger(this ReadOnlySpan<Value> values)
        {
            long sum = 0;
            foreach (var v in values)
            {
                sum += v.AsI64;
            }
            return sum;
        }
    }
}
