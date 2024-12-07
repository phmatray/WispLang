namespace WispScanner;

public class WispEnvironment
{
    private readonly WispEnvironment? _enclosing;
    private readonly Dictionary<string, object?> _values = new();

    public WispEnvironment()
    {
        _enclosing = null;
    }
    
    public WispEnvironment(WispEnvironment enclosing)
    {
        _enclosing = enclosing;
    }
    
    public void Define(string name, object? value)
    {
        _values[name] = value;
    }
    
    public object? Get(Token name)
    {
        if (_values.TryGetValue(name.Lexeme, out object? value))
        {
            return value;
        }
        
        if (_enclosing is not null) return _enclosing.Get(name);
        
        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public void Assign(Token name, object? value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }
        
        if (_enclosing is not null)
        {
            _enclosing.Assign(name, value);
            return;
        }
        
        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }
}