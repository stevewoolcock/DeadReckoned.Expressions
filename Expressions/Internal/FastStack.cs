using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions.Internal
{
    internal class FastStack<T>
    {
        private T[] m_Items;
        private int m_Count;
        private readonly int m_MaxCapacity;

        #region Properties

        public int Count => m_Count;

        #endregion

        public FastStack(int initialCapacity, int maxCapacity = 0)
        {
            m_Items = new T[initialCapacity];
            m_MaxCapacity = maxCapacity;
        }

        public void Clear()
        {
            m_Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Discard(int count)
        {
            Debug.Assert(count <= m_Count, "count is out of range");
            m_Count -= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(in T item)
        {
            EnsureCapacity(m_Count + 1);
            m_Items[m_Count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Peek() => ref m_Items[m_Count - 1];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Peek(int count) => ref m_Items[m_Count - count];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> PeekSpan(int count) => new(m_Items, m_Count - count, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> PeekMemory(int count) => new(m_Items, m_Count - count, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Pop()
        {
            Debug.Assert(m_Count > 0, "Stack is empty");
            return ref m_Items[--m_Count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T PopCopy()
        {
            Debug.Assert(m_Count > 0, "Stack is empty");
            return m_Items[--m_Count];
        }

        private void EnsureCapacity(int capacity)
        {
            int size = m_Items.Length;
            if (capacity < size)
                return;

            int max = m_MaxCapacity;
            if (max > 0 && size >= max)
            {
                throw new ExpressionRuntimeException("Stack overflow");
            }

            // Attempt to double in size, enforce a minimum
            int newSize = size > 0 ? size * 2 : 8;
            if (newSize < capacity)
            {
                newSize = capacity;
            }

            // Don't ever exceed the max capacity
            if (max > 0 && newSize > max)
            {
                newSize = max;
            }

            Array.Resize(ref m_Items, newSize);
        }
    }
}
