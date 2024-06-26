﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace DeadReckoned.Expressions.Internal
{
    internal class Compiler
    {
        private delegate void ParseFunction();

        private enum Precedence
        {
            None,
            Or,         // |
            And,        // &
            Equality,   // = !=
            Comparison, // < > <= >=
            Term,       // + -
            Factor,     // * /
            Unary,      // -
            Call,       // ()
            Primary
        }

        private readonly struct ParseRule
        {
            public readonly ParseFunction Prefix;
            public readonly ParseFunction Infix;
            public readonly Precedence Precedence;

            public ParseRule(ParseFunction prefix, ParseFunction infix, Precedence precedence)
            {
                Prefix = prefix;
                Infix = infix;
                Precedence = precedence;
            }
        }

        [ThreadStatic]
        private static Expression _transientExpression;

        private readonly ParseRule[] _parseRules = new ParseRule[(int)TokenType.MAX];

        private readonly FastAppendList<byte> m_ByteCode = new(32);
        private readonly FastAppendList<string> m_Strings = new(8);
        private readonly Dictionary<string, int> m_StringsLookup = new();
        private readonly StringBuilder m_StringBuilder = new(0);
        private readonly Parser m_Parser = new();
        private int m_Depth;
        private Token m_Current;
        private Token m_Previous;
        private Token m_Previous2;
        private ReadOnlyMemory<char> m_Source;
        private ExpressionEngine m_Engine;

        public Compiler()
        {
            _parseRules[(int)TokenType.LeftParen]    /**/ = new ParseRule(ParseGrouping,    /**/ ParseCall,    /**/ Precedence.Call);
            _parseRules[(int)TokenType.RightParen]   /**/ = new ParseRule(null,             /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.Minus]        /**/ = new ParseRule(ParseUnary,       /**/ ParseBinary,  /**/ Precedence.Term);
            _parseRules[(int)TokenType.Plus]         /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Term);
            _parseRules[(int)TokenType.Slash]        /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Factor);
            _parseRules[(int)TokenType.Percent]      /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Factor);
            _parseRules[(int)TokenType.Star]         /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Factor);
            _parseRules[(int)TokenType.Caret]        /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Factor);
            _parseRules[(int)TokenType.Dollar]       /**/ = new ParseRule(ParseParameter,   /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.Bang]         /**/ = new ParseRule(ParseUnary,       /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.BangEqual]    /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Equality);
            _parseRules[(int)TokenType.Equal]        /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Equality);
            _parseRules[(int)TokenType.Greater]      /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Comparison);
            _parseRules[(int)TokenType.GreaterEqual] /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Comparison);
            _parseRules[(int)TokenType.Less]         /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Comparison);
            _parseRules[(int)TokenType.LessEqual]    /**/ = new ParseRule(null,             /**/ ParseBinary,  /**/ Precedence.Comparison);
            _parseRules[(int)TokenType.Identifier]   /**/ = new ParseRule(ParseIdentifier,  /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.Integer]      /**/ = new ParseRule(ParseInteger,     /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.Decimal64]    /**/ = new ParseRule(ParseDecimal64,   /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.Decimal32]    /**/ = new ParseRule(ParseDecimal32,   /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.String]       /**/ = new ParseRule(ParseString,      /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.Ampersand]    /**/ = new ParseRule(null,             /**/ ParseAnd,     /**/ Precedence.And);
            _parseRules[(int)TokenType.Pipe]         /**/ = new ParseRule(null,             /**/ ParseOr,      /**/ Precedence.Or);
            _parseRules[(int)TokenType.True]         /**/ = new ParseRule(ParseLiteral,     /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.False]        /**/ = new ParseRule(ParseLiteral,     /**/ null,         /**/ Precedence.None);
            _parseRules[(int)TokenType.NaN]          /**/ = new ParseRule(ParseLiteral,     /**/ null,         /**/ Precedence.None);
        }

        public Expression Compile(ExpressionEngine engine, ReadOnlyMemory<char> source, bool transient)
        {
            m_Engine = engine;
            m_Source = source;

            // Reset state
            m_Depth = 0;
            m_Current = default;
            m_Previous = default;
            m_Previous2 = default;
            m_ByteCode.Clear(clearBuffer: false);
            m_Strings.Clear(clearBuffer: true);
            m_StringsLookup.Clear();
            m_StringBuilder.Clear();

            m_Parser.Init(source);

            Advance();

            while (!Match(TokenType.EOF))
            {
                ParseExpression();
            }

            Expression expr;
            if (transient)
            {
                // In this case, the expression is expected to be evaluated immediately and discarded,
                // so a direct view of the compiler's buffers are supplied to a thread static cached expression.
                // This avoids a lot of allocations and copy overhead for throw-away expressions.
                expr = _transientExpression ??= new Expression();
                expr.ByteCode = m_ByteCode.AsMemory();
                expr.Strings = m_Strings.AsMemory();
            }
            else
            {
                expr = new Expression(m_ByteCode.ToArray(), m_Strings.ToArray());
            }

            return expr;
        }

        #region Errors

        private void Error(string message, int errorCursorOffset = 0) => ErrorAt(m_Previous, message, errorCursorOffset);

        private void ErrorAt(in Token token, string message, int errorCursorOffset = 0)
        {
            // Portion of expression to display in error message
            // Guaranteed to include the contents of the token
            const int MaxCharsLeft = 64;  // Num chars left of the token
            const int MaxCharsRight = 16; // Num chars right of the token 
            const int Indent = 4;

            int tokenLength, tokenStart;
            if (token.Type == TokenType.EOF)
            {
                tokenStart = m_Source.Length;
                tokenLength = 1;
            }
            else
            {
                tokenStart = token.Start;
                tokenLength = Math.Max(token.Length, 1);
            }

            int startOffset = Math.Min(tokenStart, MaxCharsLeft);
            int segmentStart = Math.Max(tokenStart - startOffset, 0);
            int segmentLength = Math.Min(startOffset + tokenLength + MaxCharsRight, m_Source.Length);

            m_StringBuilder.Clear();
            m_StringBuilder
                .Append("Error: ").Append(message).AppendLine()
                .Append("Column: ").Append(tokenStart).AppendLine()
                .Append(' ', Indent).Append(m_Source.Slice(segmentStart, segmentLength)).AppendLine()
                .Append(' ', Indent + Math.Max(startOffset + errorCursorOffset, 0))
                .Append('^', tokenLength);

            throw new ExpressionCompileException(m_StringBuilder.ToString());
        }

        private void ErrorAtCurrent(string message, int errorCursorOffset = 0) => ErrorAt(m_Current, message, errorCursorOffset);

        #endregion

        private void Advance()
        {
            m_Previous2 = m_Previous;
            m_Previous = m_Current;

            while (true)
            {
                m_Current = m_Parser.Next();
                if (m_Current.Type != TokenType.Error)
                {
                    break;
                }

                ErrorAtCurrent(m_Parser.Error);
            }
        }

        private void Consume(TokenType expectedToken, string errorMessage)
        {
            if (m_Current.Type == expectedToken)
            {
                Advance();
                return;
            }

            ErrorAtCurrent(errorMessage);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Check(TokenType tokenType) => m_Current.Type == tokenType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Match(TokenType tokenType)
        {
            if (Check(tokenType))
            {
                Advance();
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementDepth()
        {
            m_Depth++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementDepth()
        {
            m_Depth--;
            System.Diagnostics.Debug.Assert(m_Depth >= 0);
        }

        private void ErrorIfOrphaned()
        {
            // The previous expression is considered "orphaned" if it is at the root of the
            // expression, is not followed by an EOF token or a binary operator token.
            // Orphaned expressions are invalid syntax, and would occur in an expression such as:
            //
            //  "TRUE 1234 + 5678"
            //
            // In the above, 'TRUE' would be considered orphaned and a syntax error.

            if (m_Depth > 0)
                return;

            if (Check(TokenType.EOF) || Check(TokenType.Error))
                return;

            switch (m_Current.Type)
            {
                // Binary operators are OK, something is likely to follow...
                case TokenType.Ampersand:
                case TokenType.Pipe:
                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Star:
                case TokenType.Slash:
                case TokenType.Percent:
                case TokenType.Equal:
                case TokenType.BangEqual:
                case TokenType.LessEqual:
                case TokenType.GreaterEqual:
                case TokenType.Less:
                case TokenType.Greater:
                case TokenType.Caret:
                    return;

                default:
                    ErrorAtCurrent("Expected operator");
                    return;
            }
        }

        private ref ParseRule GetParseRule(TokenType tokenType) => ref _parseRules[(int)tokenType];

        #region Emitters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Emit(OpCode code) => Emit((byte)code);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Emit(byte value) => m_ByteCode.Add(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Emit(short value)
        {
            Emit((byte)(value >> 8));
            Emit((byte)(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Emit(int value)
        {
            Emit((byte)(value >> 24));
            Emit((byte)(value >> 16));
            Emit((byte)(value >> 8));
            Emit((byte)(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Emit(long value)
        {
            Emit((byte)(value >> 56));
            Emit((byte)(value >> 48));
            Emit((byte)(value >> 40));
            Emit((byte)(value >> 32));
            Emit((byte)(value >> 24));
            Emit((byte)(value >> 16));
            Emit((byte)(value >> 8));
            Emit((byte)(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Emit(float value) => Emit(*(int*)&value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Emit(double value) => Emit(*(long*)&value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitConst(byte value)
        {
            Emit(OpCode.LdI8);
            Emit(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitConst(short value)
        {
            Emit(OpCode.LdI16);
            Emit(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitConst(int value)
        {
            Emit(OpCode.LdI32);
            Emit(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitConst(long value)
        {
            Emit(OpCode.LdI64);
            Emit(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitConst(float value)
        {
            Emit(OpCode.LdF32);
            Emit(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitConst(double value)
        {
            Emit(OpCode.LdF64);
            Emit(value);
        }

        private void EmitConstSmallest(long integer)
        {
            if (integer <= byte.MaxValue)
            {
                EmitConst((byte)integer);
            }
            else if (integer <= short.MaxValue)
            {
                EmitConst((short)integer);
            }
            else if (integer <= int.MaxValue)
            {
                EmitConst((int)integer);
            }
            else
            {
                EmitConst(integer);
            }
        }

        private void EmitString(string str)
        {
            int addr = GetOrAddString(str);
            Emit(OpCode.LdStr);
            Emit(addr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EmitJump(OpCode jumpCode)
        {
            Emit(jumpCode);
            Emit((short)0); // Reserve 16 bits for operand
            return m_ByteCode.Count - sizeof(short);
        }

        private void PatchJump(int offset)
        {
            int jump = m_ByteCode.Count - offset - sizeof(ushort);
            if (jump > ushort.MaxValue)
            {
                ErrorAtCurrent($"Exceeded maximum jump size: {jump}, max={ushort.MaxValue}");
                return;
            }

            // Patch in 16 bit operand for jump
            m_ByteCode[offset + 0] = (byte)(jump >> 8);
            m_ByteCode[offset + 1] = (byte)(jump);
        }

        #endregion

        private void ParsePrecedence(Precedence precedence)
        {
            Advance();
            ParseFunction prefixRule = GetParseRule(m_Previous.Type).Prefix;
            if (prefixRule == null)
            {
                Error("Expected expression");
                return;
            }

            prefixRule();

            while (precedence <= GetParseRule(m_Current.Type).Precedence)
            {
                Advance();
                ParseFunction infixRule = GetParseRule(m_Previous.Type).Infix;
                infixRule();
            }
        }

        private void ParseExpression()
        {
            ParsePrecedence(Precedence.Or);
            ErrorIfOrphaned();
        }

        private void ParseGrouping()
        {
            IncrementDepth();

            ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");

            DecrementDepth();
        }

        private void ParseCall()
        {
            // Currently, only functions can be called, and a function name is always
            // an identifier. So ensure that an identifier preceded.
            if (m_Previous2.Type != TokenType.Identifier)
            {
                ErrorAt(in m_Previous2, "Expected function name");
                return;
            }

            int argCount = 0;
            if (!Check(TokenType.RightParen))
            {
                IncrementDepth();
                do
                {
                    ParseExpression();
                    argCount++;

                    if (argCount > byte.MaxValue)
                    {
                        Error("Too many arguments supplied to function");
                        return;
                    }
                }
                while (Match(TokenType.Comma));

                DecrementDepth();
            }

            Consume(TokenType.RightParen, "Expected ')' after argument list");
            
            Emit(OpCode.Call);
            Emit((byte)argCount);
        }
        
        private void ParseUnary()
        {
            TokenType operatorType = m_Previous.Type;
            IncrementDepth();
            ParsePrecedence(Precedence.Unary);

            switch (operatorType)
            {
                case TokenType.Bang:  /**/  Emit(OpCode.Not); break;
                case TokenType.Minus: /**/  Emit(OpCode.Neg); break;
                default:
                    ErrorAtCurrent("Unreachable");
                    return;
            }
            DecrementDepth();
        }

        private void ParseBinary()
        {
            TokenType operatorType = m_Previous.Type;
            ref ParseRule rule = ref GetParseRule(operatorType);

            IncrementDepth();
            ParsePrecedence(rule.Precedence + 1);

            switch (operatorType)
            {
                case TokenType.Equal:        /**/  Emit(OpCode.Equal); break;
                case TokenType.BangEqual:    /**/  Emit(OpCode.NotEqual); break;
                case TokenType.Greater:      /**/  Emit(OpCode.Greater); break;
                case TokenType.GreaterEqual: /**/  Emit(OpCode.GreaterEq); break;
                case TokenType.Less:         /**/  Emit(OpCode.Less); break;
                case TokenType.LessEqual:    /**/  Emit(OpCode.LessEq); break;
                case TokenType.Plus:         /**/  Emit(OpCode.Add); break;
                case TokenType.Minus:        /**/  Emit(OpCode.Sub); break;
                case TokenType.Star:         /**/  Emit(OpCode.Mul); break;
                case TokenType.Slash:        /**/  Emit(OpCode.Div); break;
                case TokenType.Caret:        /**/  Emit(OpCode.Xor); break;
                case TokenType.Percent:      /**/  Emit(OpCode.Rem); break;
                default:
                    ErrorAtCurrent("Unreachable");
                    return;
            }
            DecrementDepth();
        }

        private void ParseParameter()
        {
            Consume(TokenType.Identifier, "Expected identifier after '$'");

            int index = GetOrAddString(in m_Previous, out string name);
            Emit(OpCode.LdParam);
            Emit(index);

            if (Check(TokenType.LeftParen))
            {
                ErrorAtCurrent($"Parameter '{name}' is not callable");
                return;
            }
        }

        private void ParseIdentifier()
        {
            Token nameToken = m_Previous;
            string name = nameToken.ToString(m_Source);

            // If followed by a '(', this is a function call
            // Functions must be known at compile time, so it is an error if it doesn't exist
            if (Check(TokenType.LeftParen))
            {
                if (!m_Engine.TryGetFunction(name, out FunctionInfo fn))
                {
                    ErrorAt(nameToken, $"'{name}' is not a function");
                    return;
                }

                EmitConst(fn.Id);
                return;
            }

            // Currently, lone identifiers do nothing, so this is a syntax error
            Error($"Invalid identifier '{name}'");
        }

        private void ParseInteger()
        {
            // Integers are always parsed as decimal types in decimal numeric mode
            if (m_Engine.m_Config.NumericMode == NumericMode.Decimal)
            {
                ParseDecimal64();
                return;
            }

            ReadOnlySpan<char> span = m_Previous.Slice(m_Source);
            if (long.TryParse(span, out long integer))
            {
                EmitConstSmallest(integer);
                return;
            }

            ErrorAtCurrent($"'{span.ToString()}' is not a valid integer value");
        }
        
        private void ParseDecimal32()
        {
            ReadOnlySpan<char> span = m_Previous.Slice(m_Source);
            if (float.TryParse(span, out float value))
            {
                if (m_Engine.m_Config.NumericMode == NumericMode.Integer)
                {
                    EmitConstSmallest(Convert.ToInt64(value));
                }
                else
                {
                    EmitConst(value);
                }

                return;
            }

            ErrorAtCurrent($"'{span.ToString()}' is not a valid 32bit floating point value");
        }

        private void ParseDecimal64()
        {
            ReadOnlySpan<char> span = m_Previous.Slice(m_Source);
            if (double.TryParse(span, out double value))
            {
                if (m_Engine.m_Config.NumericMode == NumericMode.Integer)
                {
                    EmitConstSmallest(Convert.ToInt64(value));
                }
                else
                {
                    EmitConst(value);
                }

                return;
            }

            ErrorAtCurrent($"'{span.ToString()}' is not a valid 64bit floating point value");
        }

        private void ParseString()
        {
            ref Token token = ref m_Previous;
            ReadOnlySpan<char> span = m_Source.Slice(token.Start + 1, token.Length - 2).Span;

            m_StringBuilder.Clear();
            m_StringBuilder.EnsureCapacity(span.Length);

            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];
                if (c == '\\' && i < span.Length - 1)
                {
                    switch (span[i + 1])
                    {
                        case '\'': m_StringBuilder.Append('\''); i++; continue;
                        case '\"': m_StringBuilder.Append('\"'); i++; continue;
                        case '\\': m_StringBuilder.Append('\\'); i++; continue;
                        case '0': m_StringBuilder.Append('\0'); i++; continue;
                        case 'a': m_StringBuilder.Append('\a'); i++; continue;
                        case 'b': m_StringBuilder.Append('\b'); i++; continue;
                        case 'f': m_StringBuilder.Append('\f'); i++; continue;
                        case 'n': m_StringBuilder.Append('\n'); i++; continue;
                        case 'r': m_StringBuilder.Append('\r'); i++; continue;
                        case 't': m_StringBuilder.Append('\t'); i++; continue;
                        case 'v': m_StringBuilder.Append('\v'); i++; continue;
                        case 'u':
                            {
                                const int Count = 4;
                                if ((i += 2 + Count) <= span.Length)
                                {
                                    uint unicode = uint.Parse(span[(i - Count)..i], NumberStyles.HexNumber);
                                    char hi = (char)(unicode >> 8);
                                    char lo = (char)(unicode);

                                    if (hi != '\0')
                                    {
                                        m_StringBuilder.Append(hi);
                                    }

                                    m_StringBuilder.Append(lo);
                                    continue;
                                }
                            }
                            break;
                    }
                }

                m_StringBuilder.Append(c);
            }

            EmitString(m_StringBuilder.ToString());
        }

        private void ParseLiteral()
        {
            switch (m_Previous.Type)
            {
                case TokenType.False:   /**/ Emit(OpCode.LdFalse); break;
                case TokenType.True:    /**/ Emit(OpCode.LdTrue); break;
                case TokenType.NaN:     /**/ EmitConst(double.NaN); break;
                //case TokenType.Infinity: EmitConst(double.PositiveInfinity); break;
                default:
                    Error("Unreachable");
                    return;
            }
        }

        private void ParseAnd()
        {
            int endJump = EmitJump(OpCode.JumpIfFalse);

            Emit(OpCode.Pop);
            ParsePrecedence(Precedence.And);
            PatchJump(endJump);
        }

        private void ParseOr()
        {
            int endJump = EmitJump(OpCode.JumpIfTrue);

            Emit(OpCode.Pop);
            ParsePrecedence(Precedence.Or);
            PatchJump(endJump);
        }

        private int GetOrAddString(in Token token, out string value)
        {
            // TODO: The allocations for strings here bothers me. There should be a way around it using
            //       ReadOnlyMemory<char> as views into the source, but requires a specialized dictionary
            //       that can handle string views as keys and compare them for content equality, becuse
            //       they are used lookups for parameters at evaluation time.

            value = token.ToString(m_Source.Span);
            return GetOrAddString(value);
        }

        private int GetOrAddString(string value)
        {
            if (m_StringsLookup.TryGetValue(value, out int index))
            {
                return index;
            }

            index = m_Strings.Count;
            m_StringsLookup[value] = index;
            m_Strings.Add(value);
            return index;
        }
    }
}
