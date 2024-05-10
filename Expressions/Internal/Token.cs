using System;

namespace DeadReckoned.Expressions.Internal
{
    internal struct Token
    {
        public TokenType Type;
        public int Start;
        public int Length;

        public readonly ReadOnlySpan<char> Slice(ReadOnlySpan<char> span) => span.Slice(Start, Length);

        public readonly ReadOnlySpan<char> Slice(ReadOnlyMemory<char> memory) => memory.Span.Slice(Start, Length);

        public readonly ReadOnlySpan<char> ToSpan(string source) => source.AsSpan(Start, Length);

        public readonly string ToString(string source) => source.Substring(Start, Length);

        public readonly string ToString(ReadOnlySpan<char> span) => span.Slice(Start, Length).ToString();

        public readonly string ToString(ReadOnlyMemory<char> memory) => memory.Slice(Start, Length).ToString();
    }
}
