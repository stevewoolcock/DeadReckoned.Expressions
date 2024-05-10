using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions
{
    public class ExpressionParameterCollection
    {
        private readonly Dictionary<string, Value> m_Parameters = new();

        #region Properties

        public int Count => m_Parameters.Count;

        public Value this[string name]
        {
            get => m_Parameters[name];
            set => m_Parameters[name] = value;
        }

        #endregion

        public void Clear() => m_Parameters.Clear();

        public bool Contains(string name) => m_Parameters.ContainsKey(name);

        public bool Remove(string name) => m_Parameters.Remove(name);

        public void Set(string name, Value value) => m_Parameters[name] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(string name, out Value value) => m_Parameters.TryGetValue(name, out value);

        public bool TryGet(string name, out byte value)
        {
            if (TryGet(name, out Value v) && v.IsInteger)
            {
                value = v.AsI8;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet(string name, out short value)
        {
            if (TryGet(name, out Value v) && v.IsInteger)
            {
                value = v.AsI16;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet(string name, out int value)
        {
            if (TryGet(name, out Value v) && v.IsInteger)
            {
                value = v.AsI32;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet(string name, out long value)
        {
            if (TryGet(name, out Value v) && v.IsInteger)
            {
                value = v.AsI64;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet(string name, out float value)
        {
            if (TryGet(name, out Value v) && v.IsDecimal)
            {
                value = v.AsF32;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet(string name, out double value)
        {
            if (TryGet(name, out Value v) && v.IsDecimal)
            {
                value = v.AsF64;
                return true;
            }

            value = default;
            return false;
        }


        public bool TryGet(ReadOnlySpan<char> name, out Value value)
        {
            foreach (var pair in m_Parameters)
            {
                if (name.Equals(pair.Key, StringComparison.Ordinal))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

    }
}
