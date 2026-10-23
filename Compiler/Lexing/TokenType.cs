namespace Compiler.Lexing;

public enum TokenType
{
    // Reserved words (keep them together: IsKeyword() depends on the order)
    Var,
    Val,
    Fun,
    Class,
    If,
    Else,
    While,
    For,
    In,
    Return,
    True,
    False,
    Null,
    New,
    Print,
    Input,
    Break,
    Continue,
    Init,

    // Built-in type names
    TypeInt,
    TypeFloat,
    TypeString,
    TypeBool,
    TypeChar,
    TypeArray,
    TypeVoid,

    Identifier,
    Integer,
    Float,
    String,
    Char,

    // Arithmetic Operators
    Plus,
    Minus,
    Star,
    Slash,
    Percent,

    // Assignment and Operators
    Assign,
    EqualEqual,
    NotEqual,
    Not,
    AndAnd,
    OrOr,
    BitAnd,
    BitOr,
    LessThan,
    LessThanEqual,
    GreaterThan,
    GreaterThanEqual,

    // Delimiters
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Colon,
    Dot,
    DotDot,

    // extra
    Comment,
    EOF,
    Error
}

public static class TokenTypeExtensions
{
    // true for every reserved word and built-in type name
    public static bool IsKeyword(this TokenType type)
    {
        return type >= TokenType.Var && type <= TokenType.TypeVoid;
    }
}