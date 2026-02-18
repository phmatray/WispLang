# WispLang

> A tree-walk interpreter for the Wisp programming language, implemented in C#.

## Description
WispLang is a C# implementation of a tree-walk interpreter inspired by the Lox language from the book "Crafting Interpreters". It includes a scanner, AST generator, AST printer, and full interpreter — covering lexing, parsing, and evaluation of a custom scripting language.

## Features
- Full scanner/lexer for tokenizing Wisp source code
- Recursive-descent parser with AST generation
- Tree-walk interpreter with variable binding and function calls
- AST printer for debugging parse trees

## Getting Started
```bash
git clone https://github.com/phmatray/WispLang.git
cd WispLang
dotnet run --project WispScanner
```

## License
MIT