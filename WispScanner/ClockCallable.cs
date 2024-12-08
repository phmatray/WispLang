namespace WispScanner;

public class ClockCallable : Callable
{
    public override int Arity()
    {
        return 0;
    }

    public override object? Call(Interpreter interpreter, List<object?> arguments)
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000.0;
    }

    public override string ToString()
    {
        return "<native fn>";
    }
}