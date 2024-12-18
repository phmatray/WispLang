using static WispScanner.TokenType;

namespace WispScanner;

public class Interpreter
    : Expr.IVisitor<object?>, Stmt.IVisitorVoid
{
    private readonly WispEnvironment _globals = new();
    private readonly Dictionary<Expr, int> _locals = new();
    private WispEnvironment _environment;

    public Interpreter()
    {
        _environment = _globals;
        _globals.Define("clock", new ClockCallable());
    }
    
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            Program.RuntimeError(error);
        }
    }

    public object? VisitAssignExpr(Expr.Assign expr)
    {
        object? value = Evaluate(expr.Value);
        
        int distance = _locals.GetValueOrDefault(expr, -1);
        if (distance != -1)
        {
            _environment.AssignAt(distance, expr.Name, value);
        }
        else
        {
            _globals.Assign(expr.Name, value);
        }

        return value;
    }

    public object? VisitBinaryExpr(Expr.Binary expr)
    {
        Object? left = Evaluate(expr.Left);
        Object? right = Evaluate(expr.Right);

        switch (expr.Op.Type)
        {
            case BANG_EQUAL:
                return !IsEqual(left, right);
            case EQUAL_EQUAL:
                return IsEqual(left, right);
            case GREATER:
                CheckNumberOperand(expr.Op, left, right);
                return (double)left! > (double)right!;
            case GREATER_EQUAL:
                CheckNumberOperand(expr.Op, left, right);
                return (double)left! >= (double)right!;
            case LESS:
                CheckNumberOperand(expr.Op, left, right);
                return (double)left! < (double)right!;
            case LESS_EQUAL:
                CheckNumberOperand(expr.Op, left, right);
                return (double)left! <= (double)right!;
            case MINUS:
                CheckNumberOperand(expr.Op, left, right);
                return (double)left! - (double)right!;
            case PLUS:
                return left switch
                {
                    double d1 when right is double d2 => d1 + d2,
                    string s1 when right is string s2 => s1 + s2,
                    _ => throw new RuntimeError(expr.Op, "Operands must be two numbers or two strings.")
                };
            case SLASH:
                CheckNumberOperand(expr.Op, left, right);
                return (double)left! / (double)right!;
            case STAR:
                CheckNumberOperand(expr.Op, left, right);
                return (double)left! * (double)right!;
        }
        
        // Unreachable.
        return null;
    }

    public object? VisitCallExpr(Expr.Call expr)
    {
        object? callee = Evaluate(expr.Callee);
        
        List<object?> arguments = [];
        foreach (Expr argument in expr.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }
        
        if (callee is not Callable)
        {
            throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
        }
        
        Callable function = callee as Callable;
        if (arguments.Count != function.Arity())
        {
            throw new RuntimeError(expr.Paren, $"Expected {function.Arity()} arguments but got {arguments.Count}.");
        }
        
        
        return function.Call(this, arguments);
    }

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
    }

    public object? VisitLogicalExpr(Expr.Logical expr)
    {
        object? left = Evaluate(expr.Left);
        
        if (expr.Op.Type == OR)
        {
            if (IsTruthy(left)) return left;
        }
        else
        {
            if (!IsTruthy(left)) return left;
        }
        
        return Evaluate(expr.Right);
    }

    public object? VisitUnaryExpr(Expr.Unary expr)
    {
        Object? right = Evaluate(expr.Right);
        
        switch (expr.Op.Type)
        {
            case BANG:
                return !IsTruthy(right);
            case MINUS:
                CheckNumberOperand(expr.Op, right);
                return -(double)right!;
        }
        
        // Unreachable.
        return null;
    }

    public object? VisitVariableExpr(Expr.Variable expr)
    {
        return LookUpVariable(expr.Name, expr);
    }

    private object? LookUpVariable(Token exprName, Expr expr)
    {
        int distance = _locals.GetValueOrDefault(expr, -1);

        if (distance != -1)
        {
            return _environment.GetAt(distance, exprName.Lexeme);
        }
        else
        {
            return _globals.Get(exprName);
        }
    }

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new WispEnvironment(_environment));
    }

    public void VisitExprStmtStmt(Stmt.ExprStmt stmt)
    {
        Evaluate(stmt.Expression);
    }

    public void VisitFunctionStmt(Stmt.Function stmt)
    {
        WispFunction function = new(stmt, _environment);
        _environment.Define(stmt.Name.Lexeme, function);
    }

    public void VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }
    }

    public void VisitPrintStmt(Stmt.Print stmt)
    {
        object? value = Evaluate(stmt.Expression);
        Console.WriteLine(Stringify(value));
    }

    public void VisitReturnStmt(Stmt.Return stmt)
    {
        object? value = null;
        if (stmt.Value is not null)
        {
            value = Evaluate(stmt.Value);
        }
        
        throw new Return(value);
    }

    public void VisitVarStmt(Stmt.Var stmt)
    {
        object? value = null;
        if (stmt.Initializer is not null)
        {
            value = Evaluate(stmt.Initializer);
        }
        
        _environment.Define(stmt.Name.Lexeme, value);
    }

    public void VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }
    }

    private void CheckNumberOperand(Token op, object? operand)
    {
        if (operand is double) return;
        throw new RuntimeError(op, "Operand must be a number.");
    }
    
    private void CheckNumberOperand(Token op, object? left, object? right)
    {
        if (left is double && right is double) return;
        throw new RuntimeError(op, "Operands must be numbers.");
    }

    private bool IsTruthy(object? obj)
    {
        if (obj == null) return false;
        if (obj is bool b) return b;
        return true;
    }
    
    private bool IsEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a is null) return false;
        
        return a.Equals(b);
    }
    
    private string Stringify(object? obj)
    {
        if (obj == null) return "nil";
        
        if (obj is double d)
        {
            string text = d.ToString();
            if (text.EndsWith(".0"))
            {
                text = text[..^2];
            }
            return text;
        }
        
        return obj.ToString()!;
    }

    private object? Evaluate(Expr exprExpression)
    {
        return exprExpression.Accept(this);
    }
    
    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    internal void ExecuteBlock(List<Stmt> statements, WispEnvironment environment)
    {
        WispEnvironment previous = _environment;
        try
        {
            _environment = environment;
            
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            _environment = previous;
        }
    }
    
    internal void Resolve(Expr expr, int depth)
    {
        _locals[expr] = depth;
    }
}