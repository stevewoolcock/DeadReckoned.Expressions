using System;
using System.Runtime.CompilerServices;

namespace DeadReckoned.Expressions.Internal
{
    internal class Parser
    {
        private static readonly (string keyword, TokenType type, bool ignoreCase)[] Identifiers = new(string, TokenType, bool)[]
        {
            ("TRUE",  TokenType.True,  true),
            ("FALSE", TokenType.False, true),
            ("NaN",   TokenType.NaN,   true),
        };

        private ReadOnlyMemory<char> m_Source;
        private int m_Start;
        private int m_Current;
        private string m_Error;

        public string Error => m_Error;
        public int Column => m_Current;

        public void Init(ReadOnlyMemory<char> source)
        {
            m_Source = source;
            m_Start = 0;
            m_Current = m_Start;
            m_Error = default;
        }

        public Token Next()
        {
            if (IsEOF())
            {
                return MakeEmptyToken(TokenType.EOF);
            }

            SkipWhitespace();

            m_Start = m_Current;

            char c = Advance();
            switch (c)
            {
                case '(': return MakeToken(TokenType.LeftParen);
                case ')': return MakeToken(TokenType.RightParen);
                case ',': return MakeToken(TokenType.Comma);
                case '+': return MakeToken(TokenType.Plus);
                case '-': return MakeToken(TokenType.Minus);
                case '*': return MakeToken(TokenType.Star);
                case '/': return MakeToken(TokenType.Slash);
                case '%': return MakeToken(TokenType.Percent);
                case '$': return MakeToken(TokenType.Dollar);
                case '|': return MakeToken(TokenType.Pipe);
                case '&': return MakeToken(TokenType.Ampersand);
                case '=': return MakeToken(TokenType.Equal);
                case '!': return MakeToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                case '<': return MakeToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                case '>': return MakeToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                //case '"': return MakeString();
                //case '\'': return MakeString();
                default:
                    if (char.IsDigit(c))
                    {
                        return MakeNumber();
                    }

                    if (char.IsLetter(c))
                    {
                        return MakeIdentifier();
                    }

                    return MakeError($"Unexpected character '{(c == '\0' ? "\\0" : c)}'");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char Peek() => Peek(0);

        private char Peek(int count)
        {
            int i = m_Current + count;
            return i < m_Source.Length ? m_Source.Span[i] : '\0';
        }

        private char Advance()
        {
            m_Current++;
            return Peek(-1);
        }

        private bool IsEOF() => m_Current >= m_Source.Length;

        private bool Match(char expected)
        {
            if (Peek() != expected)
            {
                return false;
            }

            Advance();
            return true;
        }

        private void SkipWhitespace()
        {
            while (!IsEOF())
            {
                char c = Peek();
                switch (c)
                {
                    case ' ':
                    case '\t':
                        Advance();
                        continue;

                    default:
                        return;
                }
            }
        }


        private Token MakeToken(TokenType type)
        {
            return new Token()
            {
                Type = type,
                Start = m_Start,
                Length = m_Current - m_Start,
            };
        }

        private static Token MakeEmptyToken(TokenType type)
        {
            return new Token()
            {
                Type = type,
                Start = 0,
                Length = 0,
            };
        }

        private Token MakeError(string error)
        {
            m_Error = error;
            return MakeToken(TokenType.Error);
        }

        private Token MakeString()
        {
            char openChar = Peek(-1);
            while (!IsEOF())
            {
                char c = Peek();
                if (c == openChar)
                {
                    break;
                }

                Advance();
            }

            if (IsEOF())
            {
                return MakeError("Unterminated string");
            }

            Advance();
            return MakeToken(TokenType.String);
        }

        private Token MakeNumber()
        {
            /*if (Peek() == '0' && Peek(1) == 'x')
            {
                // Possible hexadecimal value
                Advance(); // Consume 'x'
            }*/

            while (char.IsDigit(Peek()))
            {
                Advance();
            }

            if (Peek() == '.')
            {
                // Possibly a decimal
                // Next char must be a digit, or a valid letter that can be used
                // to represent a number type, scientific notiation, etc
                if (char.IsDigit(Peek(1)) || Peek(1) == 'f')
                {
                    Advance();

                    while (!IsEOF())
                    {
                        char c = Peek();
                        if (c == 'f')
                        {
                            Token token = MakeToken(TokenType.Decimal32);
                            Advance(); // Consume 'f'
                            return token;
                        }

                        if (!char.IsDigit(c))
                        {
                            break;
                        }

                        Advance();
                    }

                    return MakeToken(TokenType.Decimal64);
                }
            }

            return MakeToken(TokenType.Integer);
        }

        private Token MakeIdentifier()
        {
            static bool IsIdentifierChar(char c) => c == '_' || char.IsLetterOrDigit(c);

            while (IsIdentifierChar(Peek()))
            {
                Advance();
            }

            return MakeToken(ParseIdentifierType());
        }

        private TokenType ParseIdentifierType()
        {
            for (int i = 0; i < Identifiers.Length; i++)
            {
                ref var tuple = ref Identifiers[i];

                string keyword = tuple.keyword;
                if (m_Current - m_Start != keyword.Length)
                {
                    continue;
                }

                ReadOnlySpan<char> span = m_Source.Slice(m_Start, keyword.Length).Span;
                StringComparison cmp = tuple.ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (span.Equals(keyword, cmp))
                {
                    return tuple.type;
                }
            }

            return TokenType.Identifier;
        }
    }
}
