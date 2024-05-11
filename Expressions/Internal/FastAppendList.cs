using System;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions.Internal
{
    internal class FastAppendList<T>
    {
        private T[] m_Items;
        private int m_Count;

        #region Properties

        public int Count => m_Count;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Items[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_Items[index] = value;
        }

        #endregion

        public FastAppendList(int initialCapacity)
        {
            m_Items = new T[initialCapacity];
            m_Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> AsMemory() => new(m_Items, 0, m_Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            EnsureCapacity(m_Count + 1);
            m_Items[m_Count++] = item;
        }

        public void Clear(bool clearBuffer = true)
        {
            if (clearBuffer)
            {
                Array.Clear(m_Items, 0, m_Count);
            }

            m_Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray() => new ReadOnlyMemory<T>(m_Items, 0, m_Count).ToArray();

        private void EnsureCapacity(int capacity)
        {
            int length = m_Items.Length;
            if (capacity < length)
                return;

            int newLength = m_Items.Length > 0 ? m_Items.Length * 2 : 8;
            if (newLength < capacity)
            {
                newLength = capacity;
            }

            Array.Resize(ref m_Items, newLength);
        }
    }
}
