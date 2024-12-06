using static WispScanner.TokenType;

namespace WispScanner;

public class Interpreter : Expr.IVisitor<object?>
{
    public void Interpret(Expr expression)
    {
        try
        {
            object? value = Evaluate(expression);
            Console.WriteLine(Stringify(value));
        }
        catch (RuntimeError error)
        {
            Program.RuntimeError(error);
        }
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
        if (a == null) return false;
        
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
}