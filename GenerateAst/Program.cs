﻿using System.Text;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("GenerateAst");

        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: GenerateAst <output directory>");
            Environment.Exit(64);
        }

        string outputDir = args[0];
        DefineAst(outputDir, "Expr", [
            "Assign   : Token name, Expr value",
            "Binary   : Expr left, Token op, Expr right",
            "Call     : Expr callee, Token paren, List<Expr> arguments",
            "Grouping : Expr expression",
            "Literal  : object? value",
            "Logical  : Expr left, Token op, Expr right",
            "Unary    : Token op, Expr right",
            "Variable : Token name"
        ]);
        
        DefineAst(outputDir, "Stmt", [
            "Block    : List<Stmt> statements",
            "ExprStmt : Expr expression",
            "Function : Token name, List<Token> parameters, List<Stmt> body",
            "If       : Expr condition, Stmt thenBranch, Stmt? elseBranch",
            "Print    : Expr expression",
            "Return   : Token keyword, Expr? value",
            "Var      : Token name, Expr? initializer",
            "While    : Expr condition, Stmt body"
        ]);
    }

    private static void DefineAst(string outputDir, string baseName, List<string> types)
    {
        string path = $"{outputDir}/{baseName}.cs";
        using StreamWriter writer = new(path, false, Encoding.UTF8);
        
        writer.WriteLine("// This file has been generated by GenerateAst.");
        writer.WriteLine();
        
        writer.WriteLine("namespace WispScanner;");
        writer.WriteLine();
        writer.WriteLine("public abstract class " + baseName);
        writer.WriteLine("{");
        
        DefineVisitor(writer, baseName, types);
        
        // The AST classes.
        foreach (string type in types)
        {
            string className = type.Split(":")[0].Trim();
            string fields = type.Split(":")[1].Trim();
            DefineType(writer, baseName, className, fields);
        }
        
        // The base accept() method.
        switch (baseName)
        {
            case "Expr":
            {
                writer.WriteLine();
                writer.WriteLine("    public abstract T Accept<T>(IVisitor<T> visitor);");
                break;
            }
            case "Stmt":
            {
                writer.WriteLine();
                writer.WriteLine("    public abstract void Accept(IVisitorVoid visitor);");
                break;
            }
        }
        
        writer.WriteLine("}");
        writer.Close();
    }
    
    private static void DefineVisitor(StreamWriter writer, string baseName, List<string> types)
    {
        switch (baseName)
        {
            case "Expr":
            {
                writer.WriteLine("    public interface IVisitor<out T>");
                writer.WriteLine("    {");
        
                foreach (string type in types)
                {
                    string typeName = type.Split(":")[0].Trim();
                    writer.WriteLine($"        T Visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
                }
        
                writer.WriteLine("    }");
                writer.WriteLine();
                break;
            }
            case "Stmt":
            {
                writer.WriteLine("    public interface IVisitorVoid");
                writer.WriteLine("    {");
        
                foreach (string type in types)
                {
                    string typeName = type.Split(":")[0].Trim();
                    writer.WriteLine($"        void Visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
                }
        
                writer.WriteLine("    }");
                writer.WriteLine();
                break;
            }
        }
    }
    
    private static void DefineType(StreamWriter writer, string baseName, string className, string fieldList)
    {
        writer.WriteLine($"    public class {className} : {baseName}");
        writer.WriteLine("    {");
        
        // Constructor.
        writer.WriteLine($"        public {className}({fieldList})");
        writer.WriteLine("        {");
        
        // Store parameters in fields.
        string[] fields = fieldList.Split(", ");
        foreach (string field in fields)
        {
            string name = field.Split(" ")[1];
            writer.WriteLine($"            {name.First().ToString().ToUpper() + name[1..]} = {name};");
        }
        
        writer.WriteLine("        }");
        
        // Fields.
        writer.WriteLine();
        foreach (string field in fields)
        {
            var type = field.Split(" ")[0];
            var name = field.Split(" ")[1];
            var property = name.First().ToString().ToUpper() + name[1..];
            writer.WriteLine($"        public {type} {property} {{ get; }}");
        }
        
        // Visitor pattern.
        switch (baseName)
        {
            case "Expr":
            {
                writer.WriteLine();
                writer.WriteLine("        public override T Accept<T>(IVisitor<T> visitor)");
                writer.WriteLine("        {");
                writer.WriteLine($"            return visitor.Visit{className}{baseName}(this);");
                writer.WriteLine("        }");
                break;
            }
            case "Stmt":
            {
                writer.WriteLine();
                writer.WriteLine("        public override void Accept(IVisitorVoid visitor)");
                writer.WriteLine("        {");
                writer.WriteLine($"            visitor.Visit{className}{baseName}(this);");
                writer.WriteLine("        }");
                break;
            }
        }
        
        writer.WriteLine("    }");
        writer.WriteLine();
    }
}