namespace Compiler.Lexing;


public class Error
{
    public string Message { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public int Position { get; set; }

    public override string ToString()
    {
        return $"Lexical error at {Line}:{Column} - {Message}";
    }
}