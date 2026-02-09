// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// CA1720: TokenType.String is a TSCN file format token type, not a C# type reference
[assembly: SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "String is a valid TSCN token type name from Godot file format", Scope = "member", Target = "~F:ProtoForgeSystems.Godot.TscnParser.Tokenization.TokenType.String")]
