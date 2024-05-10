using System;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions
{
    public readonly struct FunctionArgs
    {
        private readonly ReadOnlyMemory<Value> m_Args;

        #region Properties

        /// <summary>
        /// Gets the number of arguments.
        /// </summary>
        public int Count => m_Args.Length;

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> of the argument <see cref="Value"/>s.
        /// </summary>
        public ReadOnlySpan<Value> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Args.Span;
        }

        /// <summary>
        /// Returns a readonly reference to the <see cref="Value"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref readonly Value this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Args.Span[index];
        }

        #endregion

        internal FunctionArgs(ReadOnlyMemory<Value> args)
        {
            m_Args = args;
        }

        /// <summary>
        /// Throws <see cref="ExpressionRuntimeException"/> if <see cref="Count"/> is not equal to <paramref name="expectedCount"/>.
        /// </summary>
        /// <param name="expectedCount">The expected argument count.</param>
        /// <exception cref="ExpressionRuntimeException">Thrown if the <see cref="Count"/> is not equal to <paramref name="expectedCount"/>.</exception>
        /// <summary>
        public void EnsureCount(int expectedCount)
        {
            if (m_Args.Length != expectedCount)
            {
                throw new ExpressionRuntimeException($"Expected {expectedCount} arguments, but received {m_Args.Length}");
            }
        }

        /// <summary>
        /// Throws <see cref="ExpressionRuntimeException"/> if <see cref="Count"/> is not greater or equal to <paramref name="minCount"/>.
        /// </summary>
        /// <param name="minCount">The minimum argument count.</param>
        /// <exception cref="ExpressionRuntimeException">Thrown if the <see cref="Count"/> is not greater or equal to <paramref name="minCount"/>.</exception>
        public void EnsureMinCount(int minCount)
        {
            if (m_Args.Length < minCount)
            {
                throw new ExpressionRuntimeException($"Expected at least {minCount} arguments, but received {m_Args.Length}");
            }
        }

        /// <summary>
        /// Throws <see cref="ExpressionRuntimeException"/> if <see cref="Count"/> is not greater or equal to <paramref name="minCount"/> and less or equal to <paramref name="maxCount"/>.
        /// </summary>
        /// <param name="minCount">The minimum argument count.</param>
        /// <param name="maxCount">The maximum argument count.</param>
        /// <exception cref="ExpressionRuntimeException">Thrown if the <see cref="Count"/> is not greater or equal to <paramref name="minCount"/> and less or equal to <paramref name="maxCount"/>.</exception>
        public void EnsureMinMaxCount(int minCount, int maxCount)
        {
            if (m_Args.Length < minCount)
            {
                throw new ExpressionRuntimeException($"Expected at least {minCount} arguments, but received {m_Args.Length}");
            }

            if (m_Args.Length > maxCount)
            {
                throw new ExpressionRuntimeException($"Expected nore more than {maxCount} arguments, but received {m_Args.Length}");
            }
        }
    }
}