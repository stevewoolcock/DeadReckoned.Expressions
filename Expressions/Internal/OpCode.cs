namespace DeadReckoned.Expressions.Internal
{
    internal enum OpCode : byte
    {
        Pop,

        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Neg,

        Equal,
        NotEqual,
        Greater,
        GreaterEq,
        Less,
        LessEq,
        Not,

        LdParam,
        LdStr,
        LdNull,
        LdTrue,
        LdFalse,
        LdI8,
        LdI16,
        LdI32,
        LdI64,
        LdF32,
        LdF64,

        JumpIfFalse,
        JumpIfTrue,
        Call,
        Return,
    }
}
