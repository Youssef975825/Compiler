using System;
using System.Collections.Generic;
using System.Globalization;
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

    private static readonly HashSet<string> keywords = new()
    {
        "var", "val", "fun", "class", "if", "else", "while", "for",
        "in", "return", "true", "false", "null", "new", "print",
        "input", "break", "continue"
    };

    // Data types + init
    private static readonly HashSet<string> specialWords = new()
    {
        "Int", "Float", "String", "Bool", "Char", "Array", "Void", "init"
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
        new Rule(TokenType.String, @"""(?:\\.|[^""\\\r\n])*"""),
        new Rule(TokenType.Char, @"'(?:\\.|[^'\\\r\n])'"),

        // Identifiers
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

    // true: comments are also returned as TokenType.Comment tokens (for syntax highlighting)
    public bool KeepComments { get; set; }

    
    private int position;
    private int line;
    private int column;
    private bool lastWasCR;

    public List<Token> Scan(string source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // clear the old results
        Errors.Clear();
        Symbols.Clear();

        position = 0;
        line = 1;
        column = 1;
        lastWasCR = false;

        List<Token> tokens = new();

        while (position < source.Length)
        {
            if (TrySkipWhitespace(source))
                continue;

            if (TrySkipComment(source, tokens))
                continue;

            if (TryReadMalformedNumberOrIdentifier(source, tokens))
                continue;

            if (TryReadToken(source, tokens))
                continue;

            char current = source[position];

            if (current == '"' || current == '\'')
            {
                ReadMalformedQuotedLiteral(source, tokens);
                continue;
            }

            ReadUnknownCharacter(source, tokens);
        }

        tokens.Add(CreateToken(
            "",
            TokenType.EOF,
            line,
            column,
            position
        ));

        return tokens;
    }

    private bool TrySkipWhitespace(string source)
    {
        if (position >= source.Length ||
            !char.IsWhiteSpace(source[position]))
        {
            return false;
        }

        Advance(source, 1);
        return true;
    }

    private bool TrySkipComment(string source, List<Token> tokens)
    {
        // Single-line comment -> // ...
        if (StartsWith(source, "//"))
        {
            int commentPosition = position;
            int commentLine = line;
            int commentColumn = column;

            Advance(source, 2);

            while (position < source.Length &&
                   source[position] != '\n' &&
                   source[position] != '\r')
            {
                Advance(source, 1);
            }

            if (KeepComments)
            {
                string comment = source.Substring(commentPosition, position - commentPosition);
                tokens.Add(CreateToken(comment, TokenType.Comment,
                    commentLine, commentColumn, commentPosition, comment));
            }

            return true;
        }

        // Multi-line comment -> /* ... */
        if (!StartsWith(source, "/*"))
            return false;

        int startPosition = position;
        int startLine = line;
        int startColumn = column;

        Advance(source, 2);

        while (position < source.Length &&
               !StartsWith(source, "*/"))
        {
            Advance(source, 1);
        }

        if (position >= source.Length)
        {
            string text = source.Substring(
                startPosition,
                position - startPosition
            );

            AddError(
                "Unclosed multi-line comment",
                startLine,
                startColumn,
                startPosition
            );

            tokens.Add(CreateToken(
                text,
                TokenType.Error,
                startLine,
                startColumn,
                startPosition,
                text
            ));

            return true;
        }

        Advance(source, 2);

        if (KeepComments)
        {
            string comment = source.Substring(startPosition, position - startPosition);
            tokens.Add(CreateToken(comment, TokenType.Comment,
                startLine, startColumn, startPosition, comment));
        }

        return true;
    }

    private bool TryReadMalformedNumberOrIdentifier(string source, List<Token> tokens)
    {
        Match match = malformedNumberOrIdentifier.Match(
            source,
            position
        );

        if (!match.Success)
            return false;

        string text = match.Value;
        int startPosition = position;
        int startLine = line;
        int startColumn = column;

        AddError(
            $"Invalid identifier or numeric literal '{text}'",
            startLine,
            startColumn,
            startPosition
        );

        tokens.Add(CreateToken(
            text,
            TokenType.Error,
            startLine,
            startColumn,
            startPosition,
            text
        ));

        Advance(source, text.Length);
        return true;
    }

    private bool TryReadToken(string source, List<Token> tokens)
    {
        foreach (Rule rule in rules)
        {
            Match match = rule.Pattern.Match(source, position);

            if (!match.Success)
                continue;

            string lexeme = match.Value;
            TokenType type = rule.Type;

            if (type == TokenType.Identifier &&
                (keywords.Contains(lexeme) ||
                 specialWords.Contains(lexeme)))
            {
                type = TokenType.Keyword;
            }

            int tokenLine = line;
            int tokenColumn = column;
            int tokenPosition = position;

            object? value = GetValue(type, lexeme, tokenLine, tokenColumn, tokenPosition);

            // an integer that does not fit in Int32 comes back as a string
            if (type == TokenType.Integer && value is string)
            {
                AddError($"Integer literal '{lexeme}' is too large for Int",
                    tokenLine, tokenColumn, tokenPosition);
            }

            tokens.Add(CreateToken(
                lexeme,
                type,
                tokenLine,
                tokenColumn,
                tokenPosition,
                value
            ));

            if (type == TokenType.Identifier)
                Symbols.Add(lexeme, tokenLine);

            Advance(source, lexeme.Length);
            return true;
        }

        return false;
    }

    private void ReadMalformedQuotedLiteral(
        string source,
        List<Token> tokens)
    {
        int startPosition = position;
        int startLine = line;
        int startColumn = column;
        char quote = source[position];

        // Consume the opening quote.
        Advance(source, 1);

        bool closed = false;

        while (position < source.Length)
        {
            char current = source[position];

            // A quoted literal cannot cross a physical line here.
            if (current == '\r' || current == '\n')
                break;

            // Skip an escaped character so an escaped quote
            // is not mistaken for the closing quote.
            if (current == '\\')
            {
                Advance(source, 1);

                if (position < source.Length &&
                    source[position] != '\r' &&
                    source[position] != '\n')
                {
                    Advance(source, 1);
                }

                continue;
            }

            if (current == quote)
            {
                Advance(source, 1);
                closed = true;
                break;
            }

            Advance(source, 1);
        }

        string text = source.Substring(
            startPosition,
            position - startPosition
        );

        string message;

        if (!closed)
        {
            message = quote == '"'
                ? "Unclosed string literal"
                : "Unclosed character literal";
        }
        else
        {
            message = quote == '"'
                ? "Invalid string literal"
                : "Invalid character literal";
        }

        AddError(message, startLine, startColumn, startPosition);

        tokens.Add(CreateToken(
            text,
            TokenType.Error,
            startLine,
            startColumn,
            startPosition,
            text
        ));
    }

    private void ReadUnknownCharacter(string source, List<Token> tokens)
    {
        char current = source[position];

        int errorLine = line;
        int errorColumn = column;
        int errorPosition = position;

        string text = current.ToString();

        AddError(
            $"Invalid character '{text}'",
            errorLine,
            errorColumn,
            errorPosition
        );

        tokens.Add(CreateToken(
            text,
            TokenType.Error,
            errorLine,
            errorColumn,
            errorPosition,
            text
        ));

        Advance(source, 1);
    }

    private bool StartsWith(string source, string text)
    {
        return source.Length - position >= text.Length &&
               string.Compare(
                   source,
                   position,
                   text,
                   0,
                   text.Length,
                   StringComparison.Ordinal
               ) == 0;
    }

    private void Advance(string source, int count)
    {
        for (int i = 0; i < count && position < source.Length; i++)
        {
            char current = source[position];
            position++;

            if (current == '\r')
            {
                line++;
                column = 1;
                lastWasCR = true;
            }
            else if (current == '\n')
            {
                if (!lastWasCR)
                    line++;

                column = 1;
                lastWasCR = false;
            }
            else
            {
                column++;
                lastWasCR = false;
            }
        }
    }

    private object? GetValue(TokenType type, string lexeme,
        int tokenLine, int tokenColumn, int tokenPosition)
    {
        switch (type)
        {
            case TokenType.Integer:
                if (int.TryParse(lexeme, out int integer))
                    return integer;
                // Keep the lexeme if it is outside Int32's range.
                return lexeme;

            case TokenType.Float:
                if (double.TryParse(
                        lexeme,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number))
                {
                    return number;
                }

                return lexeme;

            case TokenType.String:
            {
                string text = DecodeEscapes(
                    lexeme.Substring(1, lexeme.Length - 2),
                    tokenLine, tokenColumn, tokenPosition);
                return text;
            }

            case TokenType.Char:
            {
                string text = DecodeEscapes(
                    lexeme.Substring(1, lexeme.Length - 2),
                    tokenLine, tokenColumn, tokenPosition);
                return text.Length > 0 ? text[0] : '\0';
            }

            case TokenType.Keyword:
                if (lexeme == "true")
                    return true;

                if (lexeme == "false")
                    return false;

                if (lexeme == "null")
                    return null;

                return lexeme;

            default:
                return lexeme;
        }
    }

    // Turns \n \t \" ... into real characters and reports unknown escapes.
    // raw = the text between the quotes. Literals never span lines, so
    // column = tokenColumn + index of the backslash inside the lexeme.
    private string DecodeEscapes(string raw, int tokenLine, int tokenColumn, int tokenPosition)
    {
        var sb = new System.Text.StringBuilder();

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
                    AddError($"Unknown escape sequence '\\{e}'",
                        tokenLine, tokenColumn + i, tokenPosition + i);
                    sb.Append(e);
                    break;
            }
        }

        return sb.ToString();
    }

    private Token CreateToken(
        string lexeme,
        TokenType type,
        int tokenLine,
        int tokenColumn,
        int tokenPosition,
        object? value = null)
    {
        return new Token(
            lexeme,
            type,
            tokenLine,
            tokenColumn,
            tokenPosition,
            value
        );
    }

    private void AddError(
        string message,
        int errorLine,
        int errorColumn,
        int errorPosition)
    {
        Errors.Add(new Error
        {
            Message = message,
            Line = errorLine,
            Column = errorColumn,
            Position = errorPosition
        });
    }
}