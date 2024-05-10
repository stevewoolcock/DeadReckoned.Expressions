using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions.Internal
{
    internal class FastStack<T>
    {
        private T[] m_Items;
        private int m_Count;
        private int m_MaxCapacity;

        public int Count => m_Count;

        public FastStack(int initialCapacity, int maxCapacity = 0)
        {
            m_Items = new T[initialCapacity];
            m_MaxCapacity = maxCapacity;
        }

        public ReadOnlySpan<T> AsSpan() => new(m_Items, 0, m_Count);

        public void Clear()
        {
            m_Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item)
        {
            int size = m_Items.Length;
            if (m_Count >= size)
            {
                if (m_MaxCapacity > 0 && size >= m_MaxCapacity)
                {
                    throw new ExpressionRuntimeException("Stack overflow");
                }

                int newSize = size > 0 ? size * 2 : 8;
                if (newSize > m_MaxCapacity)
                {
                    newSize = m_MaxCapacity;
                }

                Array.Resize(ref m_Items, newSize);
            }

            m_Items[m_Count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek() => m_Items[m_Count - 1];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek(int count) => m_Items[m_Count - count];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> PeekSpan(int count) => new(m_Items, m_Count - count, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> PeekMemory(int count) => new(m_Items, m_Count - count, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            Debug.Assert(m_Count >= 1, "Out of range");
            m_Count--;
            return m_Items[m_Count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Discard(int count)
        {
            Debug.Assert(count <= m_Count, "count is out of range");
            m_Count -= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (T, T) Pop2()
        {
            Debug.Assert(m_Count >= 2, "Out of range");
            m_Count -= 2;
            return (m_Items[m_Count + 1], m_Items[m_Count]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (T, T) Pop2Reverse()
        {
            Debug.Assert(m_Count >= 2, "Out of range");
            m_Count -= 2;
            return (m_Items[m_Count], m_Items[m_Count + 1]);
        }
    }
}
