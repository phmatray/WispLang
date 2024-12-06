using System.Text;
using WispScanner;

internal class Program
{
    private static readonly Interpreter interpreter = new();
    static bool hadError = false;
    static bool hadRuntimeError = false;

    public static void Main(string[] args)
    {
        Console.WriteLine("WispScanner");

        if (args.Length > 1)
        {
            Console.WriteLine("Usage: WispScanner [script]");
            Environment.Exit(64);
        }
        else if (args.Length == 1)
        {
            RunFile(args[0]);
        }
        else
        {
            RunPrompt();
        }
    }

    static void RunFile(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Run(new string(Encoding.UTF8.GetChars(bytes)));
    
        // Indicate an error in the exit code.
        if (hadError) Environment.Exit(65);
        if (hadRuntimeError) Environment.Exit(70);
    }

    static void RunPrompt()
    {
        var input = Console.OpenStandardInput();
        var reader = new StreamReader(input);
    
        while (true)
        {
            Console.Write("> ");
            var line = reader.ReadLine();
            if (line == null) break;
            Run(line);
        }
    }

    // static void Run(string source)
    // {
    //     Scanner scanner = new(source);
    //     List<Token> tokens = scanner.ScanTokens();
    //
    //     foreach (Token token in tokens)
    //     {
    //         Console.WriteLine(token);
    //     }
    // }
    
    static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();
        Parser parser = new(tokens);
        Expr? expression = parser.Parse();
        
        // Stop if there was a syntax error.
        if (hadError) return;
    
        // Console.WriteLine(new AstPrinter().Print(expression!));
        interpreter.Interpret(expression!);
    }

    public static void Error(int line, string message)
    {
        Report(line, "", message); 
    }

    static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
        hadError = true;
    }
    
    public static void Error(Token token, string message)
    {
        if (token.Type == TokenType.EOF)
        {
            Report(token.Line, " at end", message);
        }
        else
        {
            Report(token.Line, $" at '{token.Lexeme}'", message);
        }
    }
    
    public static void RuntimeError(RuntimeError error)
    {
        Console.Error.WriteLine($"{error.Message}\n[line {error.Token.Line}]");
        hadRuntimeError = true;
    }
}