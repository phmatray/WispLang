namespace WispScanner;

public class Resolver(Interpreter interpreter)
    : Expr.IVisitorVoid, Stmt.IVisitorVoid
{
    private readonly Stack<Dictionary<string, bool>> _scopes = [];
    private FunctionType _currentFunction = FunctionType.NONE;

    internal void Resolve(List<Stmt> statements)
    {
        foreach (Stmt statement in statements)
        {
            Resolve(statement);
        }
    }

    internal void Resolve(Stmt statement)
    {
        statement.Accept(this);
    }

    internal void Resolve(Expr expression)
    {
        expression.Accept(this);
    }

    private void ResolveFunction(Stmt.Function function, FunctionType type)
    {
        FunctionType enclosingFunction = _currentFunction;
        _currentFunction = type;

        BeginScope();
        foreach (Token param in function.Parameters)
        {
            Declare(param);
            Define(param);
        }

        Resolve(function.Body);
        EndScope();

        _currentFunction = enclosingFunction;
    }

    private void BeginScope()
    {
        _scopes.Push(new Dictionary<string, bool>());
    }

    private void EndScope()
    {
        _scopes.Pop();
    }

    private void Declare(Token name)
    {
        if (_scopes.Count == 0)
        {
            return;
        }

        Dictionary<string, bool> scope = _scopes.Peek();
        if (!scope.TryAdd(name.Lexeme, false))
        {
            Program.Error(name, $"Variable with name '{name.Lexeme}' already declared in this scope.");
        }
    }

    private void Define(Token name)
    {
        if (_scopes.Count == 0)
        {
            return;
        }
        
        if (_scopes.Peek().ContainsKey(name.Lexeme))
        {
            _scopes.Peek()[name.Lexeme] = true;
        }
        else
        {
            Program.Error(name, "Variable has not been declared.");
        }
    }

    private void ResolveLocal(Expr expr, Token name)
    {
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes.ElementAt(i).ContainsKey(name.Lexeme))
            {
                interpreter.Resolve(expr, _scopes.Count - 1 - i);
                return;
            }
        }
    }

    public void VisitAssignExpr(Expr.Assign expr)
    {
        Resolve(expr.Value);
        ResolveLocal(expr, expr.Name);
    }

    public void VisitBinaryExpr(Expr.Binary expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
    }

    public void VisitCallExpr(Expr.Call expr)
    {
        Resolve(expr.Callee);
        foreach (Expr argument in expr.Arguments)
        {
            Resolve(argument);
        }
    }

    public void VisitGroupingExpr(Expr.Grouping expr)
    {
        Resolve(expr.Expression);
    }

    public void VisitLiteralExpr(Expr.Literal expr)
    {
        // Do nothing.
    }

    public void VisitLogicalExpr(Expr.Logical expr)
    {
        Resolve(expr.Left);
        Resolve(expr.Right);
    }

    public void VisitUnaryExpr(Expr.Unary expr)
    {
        Resolve(expr.Right);
    }

    public void VisitVariableExpr(Expr.Variable expr)
    {
        if (_scopes.Count != 0 && _scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool defined) && !defined)
        {
            throw new RuntimeError(expr.Name, "Cannot read local variable in its own initializer.");
        }

        ResolveLocal(expr, expr.Name);
    }

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        BeginScope();
        Resolve(stmt.Statements);
        EndScope();
    }

    public void VisitExprStmtStmt(Stmt.ExprStmt stmt)
    {
        Resolve(stmt.Expression);
    }

    public void VisitFunctionStmt(Stmt.Function stmt)
    {
        Declare(stmt.Name);
        Define(stmt.Name);

        ResolveFunction(stmt, FunctionType.FUNCTION);
    }

    public void VisitIfStmt(Stmt.If stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.ThenBranch);
        if (stmt.ElseBranch != null)
        {
            Resolve(stmt.ElseBranch);
        }
    }

    public void VisitPrintStmt(Stmt.Print stmt)
    {
        Resolve(stmt.Expression);
    }

    public void VisitReturnStmt(Stmt.Return stmt)
    {
        if (_currentFunction == FunctionType.NONE)
        {
            throw new RuntimeError(stmt.Keyword, "Cannot return from top-level code.");
        }

        if (stmt.Value != null)
        {
            Resolve(stmt.Value);
        }
    }

    public void VisitVarStmt(Stmt.Var stmt)
    {
        Declare(stmt.Name);
        if (stmt.Initializer != null)
        {
            Resolve(stmt.Initializer);
        }

        Define(stmt.Name);
    }

    public void VisitWhileStmt(Stmt.While stmt)
    {
        Resolve(stmt.Condition);
        Resolve(stmt.Body);
    }
}