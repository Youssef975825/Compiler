using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Compiler.Lexing;

public class Scanner
{
    private sealed class Rule
    {
        public TokenType Type { get; }
        public Regex Pattern { get; }

        public Rule(TokenType type, string pattern)
        {
            Type = type;
            Pattern = new Regex(@"\G" + pattern, RegexOptions.CultureInvariant);
        }
    }

    private static readonly Dictionary<string, TokenType> keywords = new()
    {
        // Reserved words
        { "var", TokenType.Var },
        { "val", TokenType.Val },
        { "fun", TokenType.Fun },
        { "class", TokenType.Class },
        { "if", TokenType.If },
        { "else", TokenType.Else },
        { "while", TokenType.While },
        { "for", TokenType.For },
        { "in", TokenType.In },
        { "return", TokenType.Return },
        { "true", TokenType.True },
        { "false", TokenType.False },
        { "null", TokenType.Null },
        { "new", TokenType.New },
        { "print", TokenType.Print },
        { "input", TokenType.Input },
        { "break", TokenType.Break },
        { "continue", TokenType.Continue },
        { "init", TokenType.Init },

        // Built-in type names
        { "Int", TokenType.TypeInt },
        { "Float", TokenType.TypeFloat },
        { "String", TokenType.TypeString },
        { "Bool", TokenType.TypeBool },
        { "Char", TokenType.TypeChar },
        { "Array", TokenType.TypeArray },
        { "Void", TokenType.TypeVoid }
    };

    // such as 123abc, 1.2abc and also 1.2.3
    private static readonly Regex malformedNumberOrIdentifier = new(
        @"\G(?:[0-9]+(?:\.[0-9]+)*[A-Za-z_][A-Za-z0-9_]*|[0-9]+(?:\.[0-9]+){2,})",
        RegexOptions.CultureInvariant
    );

    private static readonly List<Rule> rules = new()
    {
        // Literals
        new Rule(TokenType.Float, @"[0-9]+\.[0-9]+"),
        new Rule(TokenType.Integer, @"[0-9]+"),
        new Rule(TokenType.String, @"""(?:\\[^\r\n]|[^""\\\r\n])*"""),
        new Rule(TokenType.Char, @"'(?:\\[^\r\n]|[^'\\\r\n])'"),

        // Identifiers (a keyword is an identifier that is found in the dictionary)
        new Rule(TokenType.Identifier, @"[A-Za-z_][A-Za-z0-9_]*"),

        // Two-character operators
        // Longer operators must appear before the shorter ones:
        // if "=" came first, "x == y" would be scanned as AssignAssign
        // instead of EqualEqual.
        new Rule(TokenType.EqualEqual, @"=="),
        new Rule(TokenType.NotEqual, @"!="),
        new Rule(TokenType.AndAnd, @"&&"),
        new Rule(TokenType.OrOr, @"\|\|"),
        new Rule(TokenType.LessThanEqual, @"<="),
        new Rule(TokenType.GreaterThanEqual, @">="),
        new Rule(TokenType.DotDot, @"\.\."),

        // One-character operators
        new Rule(TokenType.Assign, @"="),
        new Rule(TokenType.Not, @"!"),
        new Rule(TokenType.BitAnd, @"&"),
        new Rule(TokenType.BitOr, @"\|"),
        new Rule(TokenType.LessThan, @"<"),
        new Rule(TokenType.GreaterThan, @">"),
        new Rule(TokenType.Plus, @"\+"),
        new Rule(TokenType.Minus, @"-"),
        new Rule(TokenType.Star, @"\*"),
        new Rule(TokenType.Slash, @"/"),
        new Rule(TokenType.Percent, @"%"),

        // Delimiters
        new Rule(TokenType.LeftParen, @"\("),
        new Rule(TokenType.RightParen, @"\)"),
        new Rule(TokenType.LeftBrace, @"\{"),
        new Rule(TokenType.RightBrace, @"\}"),
        new Rule(TokenType.LeftBracket, @"\["),
        new Rule(TokenType.RightBracket, @"\]"),
        new Rule(TokenType.Comma, @","),
        new Rule(TokenType.Colon, @":"),
        new Rule(TokenType.Dot, @"\.")
    };

    public List<Error> Errors { get; } = new();
    public SymbolTable Symbols { get; } = new();

    // true: comments are also returned as TokenType.Comment Tokens (for syntax highlighting)
    public bool KeepComments { get; set; }

    // State of the current Scan() call.
    private string source = "";
    private List<Token> tokens = new();
    private int position;
    private int line;
    private int column;
    private bool lastWasCR;

    public List<Token> Scan(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        source = input;
        tokens = new List<Token>();
        Errors.Clear();
        Symbols.Clear();

        position = 0;
        line = 1;
        column = 1;
        lastWasCR = false;

        while (position < source.Length)
        {
            if (TrySkipWhitespace() ||
                TrySkipComment() ||
                TryReadMalformedNumberOrIdentifier() ||
                TryReadToken())
            {
                continue;
            }

            if (source[position] is '"' or '\'')
                ReadMalformedQuotedLiteral();
            else
                ReadUnknownCharacter();
        }

        AddToken("", TokenType.EOF, line, column, position);
        return tokens;
    }

    private bool TrySkipWhitespace()
    {
        if (!char.IsWhiteSpace(source[position]))
            return false;

        Advance(1);
        return true;
    }

    // skip any comment
    private bool TrySkipComment()
    {
        int startPosition = position;
        int startLine = line;
        int startColumn = column;

        // Single-line comment -> // ...
        if (StartsWith("//"))
        {
            Advance(2);

            while (position < source.Length &&
                   source[position] != '\n' &&
                   source[position] != '\r')
            {
                Advance(1);
            }

            AddCommentToken(startPosition, startLine, startColumn);
            return true;
        }

        // Multi-line comment -> /* ... */
        if (!StartsWith("/*"))
            return false;

        Advance(2);

        while (position < source.Length && !StartsWith("*/"))
            Advance(1);

        if (position >= source.Length)
        {
            AddErrorToken(
                "Unclosed multi-line comment",
                source.Substring(startPosition),
                startLine, startColumn, startPosition);
            return true;
        }

        Advance(2);
        AddCommentToken(startPosition, startLine, startColumn);
        return true;
    }


    // Read any token
    private bool TryReadMalformedNumberOrIdentifier()
    {
        Match match = malformedNumberOrIdentifier.Match(source, position);

        if (!match.Success)
            return false;

        AddErrorToken(
            $"Invalid identifier or numeric literal '{match.Value}'",
            match.Value,
            line, column, position);

        Advance(match.Value.Length);
        return true;
    }

    private bool TryReadToken()
    {
        foreach (Rule rule in rules)
        {
            Match match = rule.Pattern.Match(source, position);

            if (!match.Success)
                continue;

            string lexeme = match.Value;
            TokenType type = rule.Type;

            // A reserved word is an identifier that is found in the dictionary.
            if (type == TokenType.Identifier &&
                keywords.TryGetValue(lexeme, out TokenType keywordType))
            {
                type = keywordType;
            }

            // The cursor has not moved yet, so line-column-Position
            // are still the start of this token.
            object? value = GetValue(type, lexeme);

            // a number that does not fit comes back as a string:
            // report it and emit an Error token instead of a number token
            if ((type == TokenType.Integer || type == TokenType.Float) && value is string)
            {
                string message = type == TokenType.Integer
                    ? $"Integer literal '{lexeme}' is too large for Int"
                    : $"Float literal '{lexeme}' is too large";

                AddErrorToken(message, lexeme, line, column, position);
                Advance(lexeme.Length);
                return true;
            }

            AddToken(lexeme, type, line, column, position, value);

            if (type == TokenType.Identifier)
                Symbols.Add(lexeme, line);

            Advance(lexeme.Length);
            return true;
        }

        return false;
    }

    // A string or char literal that the normal rules rejected
    // (unclosed, or a char with more than one character).
    private void ReadMalformedQuotedLiteral()
    {
        int startPosition = position;
        int startLine = line;
        int startColumn = column;
        char quote = source[position];

        Advance(1); // the opening quote

        bool closed = false;

        while (position < source.Length)
        {
            char current = source[position];

            // A quoted literal cannot cross a physical line.
            if (current == '\r' || current == '\n')
                break;

            // Skip the backslash and the escaped character,
            // so an escaped quote is not mistaken for the closing quote.
            if (current == '\\')
            {
                Advance(1);

                if (position < source.Length &&
                    source[position] != '\r' &&
                    source[position] != '\n')
                {
                    Advance(1);
                }

                continue;
            }

            Advance(1);

            if (current == quote)
            {
                closed = true;
                break;
            }
        }

        string kind = quote == '"' ? "string" : "character";
        string message = closed ? $"Invalid {kind} literal" : $"Unclosed {kind} literal";
        string text = source.Substring(startPosition, position - startPosition);

        AddErrorToken(message, text, startLine, startColumn, startPosition);
    }

    private void ReadUnknownCharacter()
    {
        string text = source[position].ToString();

        AddErrorToken($"Invalid character '{text}'", text, line, column, position);
        Advance(1);
    }

    // cursor

    private bool StartsWith(string text)
    {
        return source.AsSpan(position).StartsWith(text, StringComparison.Ordinal);
    }

    // The ONLY place where the cursor moves: keeps line/column correct.
    // "\r\n" counts as one line break, a lone "\r" or "\n" as one too.
    private void Advance(int count)
    {
        for (int i = 0; i < count && position < source.Length; i++)
        {
            char current = source[position++];

            if (current == '\r')
            {
                line++;
                column = 1;
            }
            else if (current == '\n')
            {
                if (!lastWasCR)
                    line++;

                column = 1;
            }
            else
            {
                column++;
            }

            lastWasCR = current == '\r';
        }
    }

    // values

    private object? GetValue(TokenType type, string lexeme)
    {
        switch (type)
        {
            case TokenType.Integer:
                // outside Int32's range: keep the lexeme (the caller reports it)
                return int.TryParse(lexeme, NumberStyles.None,
                    CultureInfo.InvariantCulture, out int integer)
                    ? integer : lexeme;

            case TokenType.Float:
                if (double.TryParse(lexeme, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double number) &&
                    !double.IsInfinity(number))
                {
                    return number;
                }

                return lexeme;

            case TokenType.String:
                return DecodeEscapes(lexeme[1..^1]);

            case TokenType.Char:
            {
                string text = DecodeEscapes(lexeme[1..^1]);
                return text.Length > 0 ? text[0] : '\0';
            }

            case TokenType.True:
                return true;

            case TokenType.False:
                return false;

            case TokenType.Null:
                return null;

            default:
                return lexeme;
        }
    }

    // Turns \n \t \" ... into real characters and reports unknown escapes.
    // raw = the text between the quotes. Literals never span lines, so the
    // column of a backslash = token column + its index inside the lexeme.
    private string DecodeEscapes(string raw)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < raw.Length; i++)
        {
            if (raw[i] != '\\')
            {
                sb.Append(raw[i]);
                continue;
            }

            i++; // the character after the backslash
            char e = raw[i]; // safe: the regex guarantees one

            switch (e)
            {
                case 'n': sb.Append('\n'); break;
                case 't': sb.Append('\t'); break;
                case 'r': sb.Append('\r'); break;
                case '0': sb.Append('\0'); break;
                case '\\':
                case '"':
                case '\'':
                    sb.Append(e);
                    break;
                default:
                    AddError($"Unknown escape sequence '\\{e}'", line, column + i, position + i);
                    sb.Append(e);
                    break;
            }
        }

        return sb.ToString();
    }

    // output

    private void AddToken(
        string lexeme,
        TokenType type,
        int tokenLine,
        int tokenColumn,
        int tokenPosition,
        object? value = null)
    {
        tokens.Add(new Token(lexeme, type, tokenLine, tokenColumn, tokenPosition, value));
    }

    private void AddCommentToken(int startPosition, int startLine, int startColumn)
    {
        if (!KeepComments)
            return;

        string comment = source.Substring(startPosition, position - startPosition);
        AddToken(comment, TokenType.Comment, startLine, startColumn, startPosition, comment);
    }

    private void AddError(string message, int errorLine, int errorColumn, int errorPosition)
    {
        Errors.Add(new Error
        {
            Message = message,
            Line = errorLine,
            Column = errorColumn,
            Position = errorPosition
        });
    }

    // Reports an error AND adds an Error token that holds the bad text.
    private void AddErrorToken(string message, string text, int startLine, int startColumn, int startPosition)
    {
        AddError(message, startLine, startColumn, startPosition);
        AddToken(text, TokenType.Error, startLine, startColumn, startPosition, text);
    }
}