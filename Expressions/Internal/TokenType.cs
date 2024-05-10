namespace DeadReckoned.Expressions.Internal
{
    internal enum TokenType
    {
        None,
        EOF,
        Error,

        LeftParen, RightParen,
        Plus, Minus, Star, Slash,
        Percent, Pipe, Ampersand,
        Bang, Dollar,
        Equal, BangEqual,
        Greater, GreaterEqual,
        Less, LessEqual,
        Comma,

        Integer,
        Decimal64,
        Decimal32,
        String,
        Identifier,

        True,
        False,
        NaN,

        MAX,
    }
}
