namespace WispScanner;

public class WispFunction : Callable
{
    private readonly WispEnvironment _closure;
    private readonly Stmt.Function _declaration;

    public WispFunction(Stmt.Function declaration, WispEnvironment closure)
    {
        _closure = closure;
        _declaration = declaration;
    }
    
    public override int Arity()
    {
        return _declaration.Parameters.Count;
    }

    public override object? Call(Interpreter interpreter, List<object?> arguments)
    {
        WispEnvironment environment = new WispEnvironment(_closure);
        for (int i = 0; i < _declaration.Parameters.Count; i++)
        {
            environment.Define(_declaration.Parameters[i].Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(_declaration.Body, environment);
        }
        catch (Return returnValue)
        {
            return returnValue.Value;
        }
        
        return null;
    }

    public override string ToString()
    {
        return $"<fn {_declaration.Name.Lexeme}>";
    }
}