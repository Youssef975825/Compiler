Rynix Language Scanner & Lexical Analyzer

A robust, high-performance Lexical Analyzer (Scanner) for the Rynix programming language, written in C#. This project forms the front-end scanning pipeline of the Rynix Compiler, featuring strict Deterministic Finite Automata (DFA) state machines, clean 5-layer architectural separation, error-recovery mechanisms, and full tracking of source positioning.

📸 Overview & Architecture

The scanner tokenizes raw Rynix source code into a structured sequence of Token objects. Built around formal automata theory, the scanner handles complex lexemes (identifiers, keywords, floats, characters, strings with escape sequences, block/line comments, and operators) with deterministic evaluation.

       +-------------------------------------------------------+
       |                  Rynix Source Code                    |
       +-------------------------------------------------------+
                                   |
                                   v
       +-------------------------------------------------------+
       |                  Block 2: Scan Loop                   |
       +-------------------------------------------------------+
           /                       |                       \
          /                        v                        \
+--------------------+   +--------------------+   +--------------------+
| Block 3: Skipping  |   |Block 4: Recognition|   |  Block 5: Support  |
| (Whitespace & Cmts)|   |  (Regex & Matches) |   | (Advance & Pos)    |
+--------------------+   +--------------------+   +--------------------+
                                   |
                                   v
       +-------------------------------------------------------+
       |               Block 1: Data & Lexemes                 |
       |  (Keywords Dict, Token Types, Error Recovery Rules)   |
       +-------------------------------------------------------+
                                   |
                                   v
       +-------------------------------------------------------+
       |          Output Tokens & Error Log Streams            |
       +-------------------------------------------------------+


The 5 Architectural Layers (Blocks)

Block 1: Data Definitions (Data)

Holds token types, Regex patterns, keyword dictionaries, and data structures.

Block 2: Scanner Control (Control)

Drives the core scanning loop (Scan()) that orchestrates skips, recognition, and error logging.

Block 3: Skipping Engine (Skipping)

Bypasses whitespace, single-line comments (//), and multi-line comments (/* ... */).

Block 4: Recognition Engine (Recognition)

Matches standard lexemes, identifies malformed literals (numbers/identifiers/unclosed strings), and categorizes keywords via dictionary lookup.

Block 5: Support & Utilities (Support)

Tracks text position, manages file line/column offsets, decodes escape sequences (\n, \t, \\), and parses typed values (GetValue).

🎨 DFA Design Specifications

The scanner's lexical rules are backed by formal DFAs. Below are highlights of the state machines implemented in Rynix:

1. Identifier DFA (Alpha (Alpha | Digit)*)

q0: Initial state. Accepts [a-zA-Z_].

q1: Final/Accepting state. Accepts self-loops on [a-zA-Z0-9_].

2. Float Literal DFA (Sign? Digit+ Dot Digit+)

Strict decimal validation ensuring trailing digits exist after the dot (e.g., 3.14 is accepted, while 5. is flagged as malformed).

3. Block Comment DFA (/* ... */)

q0 → q1: Matches /*.

q2: Inside comment body; loops on any character except *.

q3: Detects *. If followed by /, transitions to Accepting state q4; otherwise returns to q2.

Detects unclosed comments when reaching EOF while in q2 or q3.

✨ Features

Longest Match Rule: Automatically favors compound operators (e.g., ==, <=, &&) over single-character operators (=, <, &).

Resilient Error Recovery: Catches malformed inputs like 123abc or 1.2.3 and yields descriptive error tokens without crashing the scanner pipeline.

Full Escape Sequence Support: Handles string/character escapes (\n, \r, \t, \\, \', \") seamlessly.

Accurate Position Tracking: Precise Line and Column reporting for error messages and IDE integration.

📁 Repository Structure

├── Scanner.cs             # Complete single-file Scanner implementation
├── Docs/
│   └── Rynix_Scanner_RE_DFA.pdf  # Formal Regex and DFA documentation
└── README.md              # Project documentation


🚀 Getting Started

Prerequisites

.NET 6.0 SDK or higher.

Usage Example

using System;
using RynixCompiler;

class Program
{
    static void Main()
    {
        string sourceCode = @"
            // Sample Rynix Code
            Int x = 42;
            Float y = 3.14;
            if (x > 10) {
                String msg = ""Hello, World!\n"";
            }
        ";

        Scanner scanner = new Scanner();
        var (tokens, errors) = scanner.Scan(sourceCode);

        Console.WriteLine("--- TOKENS ---");
        foreach (var token in tokens)
        {
            Console.WriteLine($"{token.Line}:{token.Column} [{token.Type}] {token.Lexeme} -> Value: {token.Value}");
        }

        if (errors.Count > 0)
        {
            Console.WriteLine("\n--- ERRORS ---");
            foreach (var err in errors)
            {
                Console.WriteLine($"Error at {err.Line}:{err.Column}: {err.Message}");
            }
        }
    }
}


🛠️ Error Handling Behavior

Input Sample

Detected Issue

Scanner Behavior

123abc

Malformed Identifier / Number

Flags error, registers MalformedToken, continues scan.

1.2.3

Multiple decimal points

Emits malformed float error, resumes scanning.

"Hello...

Unclosed String Literal

Catches line break or EOF, logs error, resumes at next line.

/* ...

Unclosed Block Comment

Reaches EOF, registers error, terminates safely.

📜 License

Distributed under the MIT License. Feel free to use and modify for educational or compiler-building purposes.
