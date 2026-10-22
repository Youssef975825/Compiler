namespace Compiler.Lexing;

public enum TokenType
{
    // Reserved KeyWords
    Keyword,
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