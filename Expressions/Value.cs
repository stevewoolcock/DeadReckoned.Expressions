using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeadReckoned.Expressions
{
    /// <summary>
    /// Specifies the type of a <see cref="Value"/>.
    /// </summary>
    public enum ValueType
    {
        Null,
        Bool,
        Integer,
        Decimal,
    }

    /// <summary>
    /// Storage for a single value used by the expression engine.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly unsafe struct Value : IEquatable<Value>
    {
        public static Value Null => new(ValueType.Null);

        public static Value True => new(true);

        public static Value False => new(false);

        public static Value NaN => new(double.NaN);

        [FieldOffset(0)]
        internal readonly ValueType m_Type;

        [FieldOffset(4)]
        internal readonly long m_Integer;

        [FieldOffset(4)]
        internal readonly double m_Decimal;

        #region Properties

        public ValueType Type
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type;
        }

        public bool Bool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Bool && m_Integer != 0;
        }

        public long Integer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer ? m_Integer : default;
        }

        public double Decimal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Decimal ? m_Decimal : default;
        }

        public double Number
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer ? m_Integer : (m_Type == ValueType.Decimal ? m_Decimal : default);
        }

        public byte AsI8
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer ? (byte)m_Integer : default;
        }

        public short AsI16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer ? (short)m_Integer : default;
        }

        public int AsI32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer ? (int)m_Integer : default;
        }

        public long AsI64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer ? m_Integer : default;
        }

        public float AsF32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Decimal ? (float)m_Decimal : default;
        }

        public double AsF64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Decimal ? m_Decimal : default;
        }

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Null;
        }

        public bool IsBool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Bool;
        }

        public bool IsNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer || m_Type == ValueType.Decimal;
        }

        public bool IsInteger
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Integer;
        }

        public bool IsDecimal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Type == ValueType.Decimal;
        }

        #endregion

        private Value(ValueType type)
        {
            m_Type = type;
            m_Integer = default;
            m_Decimal = default;
        }

        public Value(long value) : this(ValueType.Integer)
        {
            m_Integer = value;
        }

        public Value(double value) : this(ValueType.Decimal)
        {
            m_Decimal = value;
        }

        public Value(bool boolean) : this(ValueType.Bool)
        {
            m_Integer = boolean ? 1 : 0;
        }


        public bool TryAs(out bool value)
        {
            if (m_Type == ValueType.Bool)
            {
                value = m_Integer != 0;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAs(out byte value)
        {
            if (m_Type == ValueType.Integer)
            {
                value = (byte)m_Integer;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAs(out short value)
        {
            if (m_Type == ValueType.Integer)
            {
                value = (short)m_Integer;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAs(out int value)
        {
            if (m_Type == ValueType.Integer)
            {
                value = (int)m_Integer;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAs(out long value)
        {
            if (m_Type == ValueType.Integer)
            {
                value = (long)m_Integer;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAs(out float value)
        {
            if (m_Type == ValueType.Decimal)
            {
                value = (float)m_Decimal;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAs(out double value)
        {
            if (m_Type == ValueType.Decimal)
            {
                value = m_Decimal;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAsNumber(out double value)
        {
            if (m_Type == ValueType.Integer)
            {
                value = m_Decimal;
                return true;
            }
            
            if (m_Type == ValueType.Decimal)
            {
                value = m_Decimal;
                return true;
            }

            value = default;
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ToBool() => m_Integer != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ToI8() => (byte)ToInteger();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ToI16() => (short)ToInteger();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToI32() => (int)ToInteger();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ToI64() => ToInteger();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToF32() => (float)ToDecimal();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ToF64() => ToDecimal();

        public long ToInteger()
        {
            return m_Type switch
            {
                ValueType.Integer => m_Integer,
                ValueType.Decimal => (long)m_Decimal,
                ValueType.Bool => m_Integer != 0 ? 1 : 0,
                _ => 0,
            };
        }

        public double ToDecimal()
        {
            return m_Type switch
            {
                ValueType.Integer => m_Integer,
                ValueType.Decimal => m_Decimal,
                ValueType.Bool => m_Integer != 0 ? 1.0 : 0.0,
                _ => 0.0,
            };
        }

        #region Object

        public readonly bool Equals(Value other)
        {
            if (other.m_Type == m_Type)
            {
                return other.m_Integer == m_Integer;
            }
            
            if (other.IsNumber == IsNumber)
            {
                return other.Number == Number;
            }

            return false;
        }

        public override bool Equals(object obj) => obj is Value other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(m_Type, m_Integer);

        public override string ToString()
        {
            switch (m_Type)
            {
                case ValueType.Bool: return Bool ? "TRUE" : "FALSE";
                case ValueType.Integer: return m_Integer.ToString();
                case ValueType.Decimal: return m_Decimal.ToString();
                case ValueType.Null:
                default:
                    return "NULL";
            }
        }

        #endregion

        #region Operators

        public static bool operator ==(Value lhs, Value rhs) => lhs.Equals(rhs);
        public static bool operator !=(Value lhs, Value rhs) => !(lhs == rhs);

        public static implicit operator Value(bool value) => new(value);
        public static implicit operator Value(byte value) => new(value);
        public static implicit operator Value(int value) => new(value);
        public static implicit operator Value(long value) => new(value);
        public static implicit operator Value(float value) => new(value);
        public static implicit operator Value(double value) => new(value);

        #endregion
    }
}
