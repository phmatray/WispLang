using static WispScanner.TokenType;

namespace WispScanner;

public class Scanner
{
    private static readonly Dictionary<string, TokenType> keywords = new()
    {
        { "and", AND },
        { "class", CLASS },
        { "else", ELSE },
        { "false", FALSE },
        { "for", FOR },
        { "fun", FUN },
        { "if", IF },
        { "nil", NIL },
        { "or", OR },
        { "print", PRINT },
        { "return", RETURN },
        { "super", SUPER },
        { "this", THIS },
        { "true", TRUE },
        { "var", VAR },
        { "while", WHILE }
    };
    
    private readonly string source;
    private readonly List<Token> tokens = [];
    private int start = 0;
    private int current = 0;
    private int line = 1;

    public Scanner(string source)
    {
        this.source = source;
    }
    
    public List<Token> ScanTokens()
    {
        while(!IsAtEnd())
        {
            // We are at the beginning of the next lexeme.
            start = current;
            ScanToken();
        }
        
        tokens.Add(new Token(EOF, "", null, line));
        return tokens;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(': AddToken(LEFT_PAREN); break;
            case ')': AddToken(RIGHT_PAREN); break;
            case '{': AddToken(LEFT_BRACE); break;
            case '}': AddToken(RIGHT_BRACE); break;
            case ',': AddToken(COMMA); break;
            case '.': AddToken(DOT); break;
            case '-': AddToken(MINUS); break;
            case '+': AddToken(PLUS); break;
            case ';': AddToken(SEMICOLON); break;
            case '*': AddToken(STAR); break;

            case '!': AddToken(Match('=') ? BANG_EQUAL : BANG); break;
            case '=': AddToken(Match('=') ? EQUAL_EQUAL : EQUAL); break;
            case '<': AddToken(Match('=') ? LESS_EQUAL : LESS); break;
            case '>': AddToken(Match('=') ? GREATER_EQUAL : GREATER); break;
            
            case '/':
                if (Match('/'))
                {
                    // A comment goes until the end of the line.
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else
                {
                    AddToken(SLASH);
                }
                break;
            
            case ' ':
            case '\r':
            case '\t':
                // Ignore whitespace.
                break;
            
            case '\n':
                line++;
                break;
            
            case '"': WispString(); break;
            
            default:
                if (IsDigit(c))
                {
                    WispNumber();
                }
                else if (IsAlpha(c))
                {
                    WispIdentifier();
                }
                else
                {
                    Program.Error(line, "Unexpected character.");
                }
                break;
        }
    }

    private void WispString()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n') line++;
            Advance();
        }
        
        // Unterminated string.
        if (IsAtEnd())
        {
            Program.Error(line, "Unterminated string.");
            return;
        }
        
        // The closing ".
        Advance();
        
        // Trim the surrounding quotes.
        string value = source.Substring(start + 1, current - start - 2);
        AddToken(STRING, value);
    }
    
    private void WispNumber()
    {
        while (IsDigit(Peek())) Advance();
        
        // Look for a fractional part.
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            // Consume the "."
            Advance();
            
            while (IsDigit(Peek())) Advance();
        }
        
        AddToken(NUMBER, double.Parse(source.Substring(start, current - start)));
    }

    private void WispIdentifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();
        
        string text = source.Substring(start, current - start);
        TokenType type = keywords.GetValueOrDefault(text, IDENTIFIER);
        AddToken(type);
    }
    
    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (source.ElementAt(current) != expected) return false;
        
        current++;
        return true;
    }
    
    private char Peek()
    {
        if (IsAtEnd()) return '\0';
        return source.ElementAt(current);
    }
    
    private char PeekNext()
    {
        if (current + 1 >= source.Length) return '\0';
        return source.ElementAt(current + 1);
    }
    
    private bool IsDigit(char c)
    {
        return c is >= '0' and <= '9';
    }
    
    private bool IsAlpha(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
    }
    
    private bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }
    
    private bool IsAtEnd()
    {
        return current >= source.Length;
    }
    
    private char Advance()
    {
        return source.ElementAt(current++);
    }
    
    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }
    
    private void AddToken(TokenType type, object? literal)
    {
        string text = source.Substring(start, current - start);
        tokens.Add(new Token(type, text, literal, line));
    }
}