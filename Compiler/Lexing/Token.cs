namespace Compiler.Lexing;

public class Token
{
    public string Lexeme { get; set; } = "";
    public TokenType Type { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public object? Value { get; set; }
    public int Position { get; set; }

    public Token(
        string lexeme,
        TokenType type,
        int line,
        int column,
        int position,
        object? value = null
        )
    {
        Lexeme = lexeme;
        Type = type;
        Line = line;
        Column = column;
        Position = position;
        Value = value;
    }
    
    public Token(){}

    public override string ToString()
    {
        return $"[{Line} : {Column}] {Type,-12} {Lexeme}";
    }
}