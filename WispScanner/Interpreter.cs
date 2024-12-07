using static WispScanner.TokenType;

namespace WispScanner;

public class Interpreter
    : Expr.IVisitor<object?>, Stmt.IVisitorVoid
{
    private WispEnvironment environment = new();
    
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
        environment.Assign(expr.Name, value);
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

    public object? VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object? VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
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
        return environment.Get(expr.Name);
    }

    public void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new WispEnvironment(environment));
    }

    public void VisitExprStmtStmt(Stmt.ExprStmt stmt)
    {
        Evaluate(stmt.Expression);
    }

    public void VisitPrintStmt(Stmt.Print stmt)
    {
        object? value = Evaluate(stmt.Expression);
        Console.WriteLine(Stringify(value));
    }

    public void VisitVarStmt(Stmt.Var stmt)
    {
        object? value = null;
        if (stmt.Initializer is not null)
        {
            value = Evaluate(stmt.Initializer);
        }
        
        environment.Define(stmt.Name.Lexeme, value);
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
        if (obj is null) return false;
        if (obj is bool b) return b;
        return true;
    }
    
    private bool IsEqual(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null) return false;
        
        return a.Equals(b);
    }
    
    private string Stringify(object? obj)
    {
        if (obj is null) return "nil";
        
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
    
    private void ExecuteBlock(List<Stmt> statements, WispEnvironment environment)
    {
        WispEnvironment previous = this.environment;
        try
        {
            this.environment = environment;
            
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        finally
        {
            this.environment = previous;
        }
    }
}