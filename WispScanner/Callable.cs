namespace WispScanner;

public abstract class Callable
{
    public abstract int Arity();
    
    public abstract object? Call(Interpreter interpreter, List<object?> arguments);
}