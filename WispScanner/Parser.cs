using static WispScanner.TokenType;

namespace WispScanner;

public class Parser
{
    private class ParseError : Exception;
    
    private readonly List<Token> _tokens;
    private int _current = 0;
    
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }
    
    public List<Stmt> Parse()
    {
        List<Stmt> statements = [];
        
        while (!IsAtEnd())
        {
            statements.Add(Declaration());
        }
        
        return statements;
    }
    
    private Expr Expression()
    {
        return Assignment();
    }
    
    private Stmt Statement()
    {
        if (Match(FOR)) return ForStatement();
        if (Match(IF)) return IfStatement();
        if (Match(PRINT)) return PrintStatement();
        if (Match(WHILE)) return WhileStatement();
        if (Match(LEFT_BRACE)) return new Stmt.Block(Block());
        
        return ExpressionStatement();
    }

    private Stmt ForStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'for'.");
        
        Stmt? initializer;
        if (Match(SEMICOLON))
        {
            initializer = null;
        }
        else if (Match(VAR))
        {
            initializer = VarDeclaration();
        }
        else
        {
            initializer = ExpressionStatement();
        }
        
        Expr? condition = null;
        if (!Check(SEMICOLON))
        {
            condition = Expression();
        }
        Consume(SEMICOLON, "Expect ';' after loop condition.");
        
        Expr? increment = null;
        if (!Check(RIGHT_PAREN))
        {
            increment = Expression();
        }
        Consume(RIGHT_PAREN, "Expect ')' after for clauses.");
        Stmt body = Statement();
        
        if (increment != null)
        {
            body = new Stmt.Block([body, new Stmt.ExprStmt(increment)]);
        }
        
        condition ??= new Expr.Literal(true);
        body = new Stmt.While(condition, body);
        
        if (initializer != null)
        {
            body = new Stmt.Block([initializer, body]);
        }

        return body;
    }

    private Stmt IfStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after if condition.");
        
        Stmt thenBranch = Statement();
        Stmt? elseBranch = null;
        if (Match(ELSE))
        {
            elseBranch = Statement();
        }
        
        return new Stmt.If(condition, thenBranch, elseBranch);
    }
    
    private Stmt PrintStatement()
    {
        Expr value = Expression();
        Consume(SEMICOLON, "Expect ';' after value.");
        return new Stmt.Print(value);
    }
    
    private Stmt WhileStatement()
    {
        Consume(LEFT_PAREN, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(RIGHT_PAREN, "Expect ')' after condition.");
        Stmt body = Statement();
        
        return new Stmt.While(condition, body);
    }
    
    private Stmt VarDeclaration()
    {
        Token name = Consume(IDENTIFIER, "Expect variable name.");
        
        Expr? initializer = null;
        if (Match(EQUAL))
        {
            initializer = Expression();
        }
        
        Consume(SEMICOLON, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }
    
    private Stmt ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(SEMICOLON, "Expect ';' after expression.");
        return new Stmt.ExprStmt(expr);
    }

    private List<Stmt> Block()
    {
        List<Stmt> statements = [];
        
        while (!Check(RIGHT_BRACE) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }
        
        Consume(RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }

    private Expr Assignment()
    {
        Expr expr = Or();
        
        if (Match(EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment();
            
            if (expr is Expr.Variable variable)
            {
                Token name = variable.Name;
                return new Expr.Assign(name, value);
            }
            
            Error(equals, "Invalid assignment target.");
        }
        
        return expr;
    }

    private Expr Or()
    {
        Expr expr = And();

        while (Match(OR))
        {
            Token op = Previous();
            Expr right = And();
            expr = new Expr.Logical(expr, op, right);
        }
        
        return expr;
    }

    private Expr And()
    {
        Expr expr = Equality();
        
        while (Match(AND))
        {
            Token op = Previous();
            Expr right = Equality();
            expr = new Expr.Logical(expr, op, right);
        }
        
        return expr;
    }
    
    private Stmt? Declaration()
    {
        try
        {
            if (Match(VAR)) return VarDeclaration();
            
            return Statement();
        }
        catch (ParseError error)
        {
            Synchronize();
            return null;
        }
    }
    
    private Expr Equality()
    {
        Expr expr = Comparison();
        
        while (Match(BANG_EQUAL, EQUAL_EQUAL))
        {
            Token op = Previous();
            Expr right = Comparison();
            expr = new Expr.Binary(expr, op, right);
        }
        
        return expr;
    }
    
    private Expr Comparison()
    {
        Expr expr = Term();
        
        while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL))
        {
            Token op = Previous();
            Expr right = Term();
            expr = new Expr.Binary(expr, op, right);
        }
        
        return expr;
    }
    
    private Expr Term()
    {
        Expr expr = Factor();
        
        while (Match(MINUS, PLUS))
        {
            Token op = Previous();
            Expr right = Factor();
            expr = new Expr.Binary(expr, op, right);
        }
        
        return expr;
    }
    
    private Expr Factor()
    {
        Expr expr = Unary();
        
        while (Match(SLASH, STAR))
        {
            Token op = Previous();
            Expr right = Unary();
            expr = new Expr.Binary(expr, op, right);
        }
        
        return expr;
    }
    
    private Expr Unary()
    {
        if (Match(BANG, MINUS))
        {
            Token op = Previous();
            Expr right = Unary();
            return new Expr.Unary(op, right);
        }
        
        return Primary();
    }
    
    private Expr Primary()
    {
        if (Match(FALSE)) return new Expr.Literal(false);
        if (Match(TRUE)) return new Expr.Literal(true);
        if (Match(NIL)) return new Expr.Literal(null);
        
        if (Match(NUMBER, STRING))
        {
            return new Expr.Literal(Previous().Literal);
        }
        
        if (Match(IDENTIFIER))
        {
            return new Expr.Variable(Previous());
        }
        
        if (Match(LEFT_PAREN))
        {
            Expr expr = Expression();
            Consume(RIGHT_PAREN, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }
        
        throw Error(Peek(), "Expect expression.");
    }
    
    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        
        throw Error(Peek(), message);
    }
    
    private ParseError Error(Token token, string message)
    {
        Program.Error(token, message);
        return new ParseError();
    }
    
    private void Synchronize()
    {
        Advance();
        
        while (!IsAtEnd())
        {
            if (Previous().Type == SEMICOLON) return;
            
            switch (Peek().Type)
            {
                case CLASS:
                case FOR:
                case FUN:
                case IF:
                case PRINT:
                case RETURN:
                case VAR:
                case WHILE:
                    return;
            }
            
            Advance();
        }
    }
    
    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        
        return false;
    }
    
    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }
    
    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }
    
    private bool IsAtEnd()
    {
        return Peek().Type == EOF;
    }
    
    private Token Peek()
    {
        return _tokens[_current];
    }
    
    private Token Previous()
    {
        return _tokens[_current - 1];
    }
}