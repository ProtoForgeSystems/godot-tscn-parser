using System;
using System.Collections.Generic;
using System.Linq;
using ProtoForgeSystems.Godot.TscnParser.Models;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;
using ProtoForgeSystems.Godot.TscnParser.Exceptions;

namespace ProtoForgeSystems.Godot.TscnParser.Parsing;

/// <summary>
/// Parses token streams into strongly-typed Godot value objects.
/// Implements a recursive descent parser for Godot's property value syntax.
/// </summary>
public class ValueParser
{
    // ===== Token Validation Helpers =====

    /// <summary>Ensure token is available at position, throw if EOF</summary>
    private static void ValidateTokenAvailable(List<Token> tokens, int pos)
    {
        if (pos >= tokens.Count)
            throw new ValueParseException("Unexpected end of input");
    }

    /// <summary>Get token at position or throw descriptive EOF error</summary>
    private static Token GetTokenOrThrow(List<Token> tokens, int pos, string expected)
    {
        if (pos >= tokens.Count)
            throw new ValueParseException($"Unexpected end of input, expected {expected}");
        return tokens[pos];
    }

    /// <summary>Expect specific token type at position, throw if mismatch or EOF</summary>
    private static void ExpectToken(List<Token> tokens, int pos, TokenType expectedType, string description)
    {
        var token = GetTokenOrThrow(tokens, pos, description);
        if (token.Type != expectedType)
            throw new ValueParseException($"Expected {description}", token);
    }

    /// <summary>Check if position is at end of array (BracketClose or EOF)</summary>
    private static bool IsAtArrayEnd(List<Token> tokens, int pos) =>
        pos >= tokens.Count || tokens[pos].Type == TokenType.BracketClose;

    /// <summary>Check if position is at end of dictionary (BraceClose or EOF)</summary>
    private static bool IsAtDictionaryEnd(List<Token> tokens, int pos) =>
        pos >= tokens.Count || tokens[pos].Type == TokenType.BraceClose;

    /// <summary>Skip comma if present, return next position</summary>
    private static int SkipOptionalComma(List<Token> tokens, int pos)
    {
        if (pos < tokens.Count && tokens[pos].Type == TokenType.Comma)
            return pos + 1;
        return pos;
    }

    // ===== Value Parsing =====

    /// <summary>
    /// Parse a value from a token stream starting at the given index.
    /// Returns the parsed value and updates endIndex to point after the consumed tokens.
    /// </summary>
    public IGodotValue ParseValue(List<Token> tokens, int startIndex, out int endIndex)
    {
        ValidateTokenAvailable(tokens, startIndex);
        var token = tokens[startIndex];

        return token.Type switch
        {
            TokenType.Number => ParseLiteralNumber(token, startIndex, out endIndex),
            TokenType.String => ParseLiteralString(token, startIndex, out endIndex),
            TokenType.BracketOpen => ParseArray(tokens, startIndex, out endIndex),
            TokenType.BraceOpen => ParseDictionary(tokens, startIndex, out endIndex),
            TokenType.Identifier or TokenType.Color => ParseIdentifierOrTyped(tokens, startIndex, out endIndex),
            _ => throw new ValueParseException($"Unexpected token type: {token.Type}", token)
        };
    }

    /// <summary>Dispatch identifier or typed value parsing (boolean, null, typed array, typed constructor)</summary>
    private IGodotValue ParseIdentifierOrTyped(List<Token> tokens, int startIndex, out int endIndex)
    {
        var token = tokens[startIndex];

        if (TryParseLiteral(token, out var literal, out endIndex))
        {
            endIndex = startIndex + 1;
            return literal!; // TryParseLiteral guarantees non-null when returning true
        }

        if (IsTypedArray(tokens, startIndex))
            return ParseTypedArray(tokens, startIndex, out endIndex);

        if (IsTypedConstructor(tokens, startIndex))
            return ParseTypedValue(tokens, startIndex, out endIndex);

        throw new ValueParseException($"Unrecognized identifier: {token.Value}", token);
    }

    /// <summary>Check if token is a boolean or null literal (Identifier only)</summary>
    private static bool TryParseLiteral(Token token, out IGodotValue? literal, out int endIndex)
    {
        literal = null;
        endIndex = 0;

        if (token.Type != TokenType.Identifier)
            return false;

        literal = token.Value switch
        {
            "true" => new LiteralValue(true),
            "false" => new LiteralValue(false),
            "null" => new LiteralValue(null),
            _ => null
        };

        return literal != null;
    }

    /// <summary>Check if token sequence represents a typed array (Array[type](...))</summary>
    private static bool IsTypedArray(List<Token> tokens, int pos) =>
        pos + 1 < tokens.Count && tokens[pos + 1].Type == TokenType.BracketOpen;

    /// <summary>Check if token sequence represents a typed constructor (Type(...))</summary>
    private static bool IsTypedConstructor(List<Token> tokens, int pos) =>
        pos + 1 < tokens.Count && tokens[pos + 1].Type == TokenType.ParenOpen;

    /// <summary>Parse numeric literal token</summary>
    private static LiteralValue ParseLiteralNumber(Token token, int startIndex, out int endIndex)
    {
        endIndex = startIndex + 1;
        return new LiteralValue(token.AsNumber());
    }

    /// <summary>Parse string literal token</summary>
    private static LiteralValue ParseLiteralString(Token token, int startIndex, out int endIndex)
    {
        endIndex = startIndex + 1;
        return new LiteralValue(token.AsStringValue());
    }
    
    /// <summary>Parse an array: [item1, item2, ...]</summary>
    private ArrayValue ParseArray(List<Token> tokens, int startIndex, out int endIndex)
    {
        ExpectToken(tokens, startIndex, TokenType.BracketOpen, "[");
        int pos = startIndex + 1;

        var elements = ParseArrayElements(tokens, ref pos);

        ExpectToken(tokens, pos, TokenType.BracketClose, "]");
        endIndex = pos + 1;
        return new ArrayValue(elements);
    }

    /// <summary>Parse array elements until closing bracket</summary>
    private List<IGodotValue> ParseArrayElements(List<Token> tokens, ref int pos)
    {
        var elements = new List<IGodotValue>();

        while (!IsAtArrayEnd(tokens, pos))
        {
            var element = ParseValue(tokens, pos, out pos);
            elements.Add(element);

            pos = HandleArraySeparator(tokens, pos);
        }

        return elements;
    }

    /// <summary>Handle comma or closing bracket in array, advance position</summary>
    private static int HandleArraySeparator(List<Token> tokens, int pos)
    {
        var token = GetTokenOrThrow(tokens, pos, ", or ]");

        if (token.Type == TokenType.Comma)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == TokenType.BracketClose)
                return pos; // Trailing comma
            return pos;
        }

        if (token.Type == TokenType.BracketClose)
            return pos;

        throw new ValueParseException($"Expected , or ] in array, got {token.Type}", token);
    }
    
    /// <summary>Parse a dictionary: {key: value, ...}</summary>
    private DictionaryValue ParseDictionary(List<Token> tokens, int startIndex, out int endIndex)
    {
        ExpectToken(tokens, startIndex, TokenType.BraceOpen, "{");
        int pos = startIndex + 1;

        var pairs = ParseDictionaryPairs(tokens, ref pos);

        ExpectToken(tokens, pos, TokenType.BraceClose, "}");
        endIndex = pos + 1;
        return new DictionaryValue(pairs);
    }

    /// <summary>Parse dictionary key-value pairs until closing brace</summary>
    private Dictionary<string, IGodotValue> ParseDictionaryPairs(List<Token> tokens, ref int pos)
    {
        var pairs = new Dictionary<string, IGodotValue>();

        while (!IsAtDictionaryEnd(tokens, pos))
        {
            var (key, value) = ParseDictionaryPair(tokens, ref pos);
            pairs[key] = value;

            pos = HandleDictionarySeparator(tokens, pos);
        }

        return pairs;
    }

    /// <summary>Parse single key: value pair</summary>
    private (string key, IGodotValue value) ParseDictionaryPair(List<Token> tokens, ref int pos)
    {
        var keyToken = GetTokenOrThrow(tokens, pos, "dictionary key");
        if (keyToken.Type != TokenType.String)
            throw new ValueParseException("Dictionary key must be a string", keyToken);

        var key = keyToken.AsStringValue();
        pos++;

        ExpectToken(tokens, pos, TokenType.Colon, ":");
        pos++;

        var value = ParseValue(tokens, pos, out pos);
        return (key, value);
    }

    /// <summary>Handle comma or closing brace in dictionary, advance position</summary>
    private static int HandleDictionarySeparator(List<Token> tokens, int pos)
    {
        var token = GetTokenOrThrow(tokens, pos, ", or }");

        if (token.Type == TokenType.Comma)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == TokenType.BraceClose)
                return pos; // Trailing comma
            return pos;
        }

        if (token.Type == TokenType.BraceClose)
            return pos;

        throw new ValueParseException($"Expected , or }} in dictionary, got {token.Type}", token);
    }
    
    /// <summary>
    /// Parse a typed array: Array[type]([item1, item2, ...])
    /// Godot 4.x syntax for typed collections like Array[int]([1, 2, 3])
    /// </summary>
    private ArrayValue ParseTypedArray(List<Token> tokens, int startIndex, out int endIndex)
    {
        ExpectToken(tokens, startIndex, TokenType.Identifier, "Array identifier");
        var arrayTypeName = tokens[startIndex].Value;
        int pos = startIndex + 1;

        ParseTypedArrayHeader(tokens, ref pos, out _); // Discard elementType
        var items = ParseTypedArrayContents(tokens, ref pos);

        ExpectToken(tokens, pos, TokenType.ParenClose, ")");
        endIndex = pos + 1;
        return new ArrayValue(items);
    }

    /// <summary>Parse typed array header: [type]( portion</summary>
    private static void ParseTypedArrayHeader(List<Token> tokens, ref int pos, out string elementType)
    {
        ExpectToken(tokens, pos, TokenType.BracketOpen, "[");
        pos++;

        var elementTypeToken = GetTokenOrThrow(tokens, pos, "type identifier");
        if (elementTypeToken.Type != TokenType.Identifier)
            throw new ValueParseException("Expected type identifier in typed array", elementTypeToken);

        elementType = elementTypeToken.Value;
        pos++;

        ExpectToken(tokens, pos, TokenType.BracketClose, "]");
        pos++;

        ExpectToken(tokens, pos, TokenType.ParenOpen, "(");
        pos++;
    }

    /// <summary>Parse typed array contents: either nested array or comma-separated values</summary>
    private List<IGodotValue> ParseTypedArrayContents(List<Token> tokens, ref int pos)
    {
        if (pos < tokens.Count && tokens[pos].Type == TokenType.BracketOpen)
            return ParseNestedArrayContents(tokens, ref pos);

        if (pos < tokens.Count && tokens[pos].Type != TokenType.ParenClose)
            return ParseDirectArrayContents(tokens, ref pos);

        return new List<IGodotValue>();
    }

    /// <summary>Parse nested array syntax: Array[type]([item1, item2])</summary>
    private List<IGodotValue> ParseNestedArrayContents(List<Token> tokens, ref int pos)
    {
        var nestedArray = ParseArray(tokens, pos, out pos);
        return nestedArray is ArrayValue arrayVal ? arrayVal.Items : new List<IGodotValue>();
    }

    /// <summary>Parse direct syntax: Array[type](item1, item2, item3)</summary>
    private List<IGodotValue> ParseDirectArrayContents(List<Token> tokens, ref int pos)
    {
        var items = new List<IGodotValue>();

        while (pos < tokens.Count && tokens[pos].Type != TokenType.ParenClose)
        {
            var item = ParseValue(tokens, pos, out pos);
            items.Add(item);

            pos = HandleTypedArraySeparator(tokens, pos);
        }

        return items;
    }

    /// <summary>Handle comma or closing paren in typed array</summary>
    private static int HandleTypedArraySeparator(List<Token> tokens, int pos)
    {
        var token = GetTokenOrThrow(tokens, pos, ", or )");

        if (token.Type == TokenType.Comma)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == TokenType.ParenClose)
                return pos; // Trailing comma
            return pos;
        }

        if (token.Type == TokenType.ParenClose)
            return pos;

        throw new ValueParseException($"Expected , or ) in typed array, got {token.Type}", token);
    }

    /// <summary>Parse a typed value: TypeName(arg1, arg2, ...)</summary>
    private IGodotValue ParseTypedValue(List<Token> tokens, int startIndex, out int endIndex)
    {
        var typeToken = tokens[startIndex];
        if (typeToken.Type != TokenType.Identifier && typeToken.Type != TokenType.Color)
            throw new ValueParseException("Expected identifier or Color", typeToken);

        var typeName = typeToken.Value;
        int pos = startIndex + 1;

        ExpectToken(tokens, pos, TokenType.ParenOpen, "(");
        pos++;

        var args = ParseTypedValueArguments(tokens, ref pos);

        ExpectToken(tokens, pos, TokenType.ParenClose, ")");
        endIndex = pos + 1;

        return ConstructTypedValue(typeName, args, typeToken);
    }

    /// <summary>Parse arguments for typed value constructor</summary>
    private List<IGodotValue> ParseTypedValueArguments(List<Token> tokens, ref int pos)
    {
        var args = new List<IGodotValue>();

        if (pos < tokens.Count && tokens[pos].Type == TokenType.ParenClose)
            return args;

        while (pos < tokens.Count && tokens[pos].Type != TokenType.ParenClose)
        {
            var arg = ParseValue(tokens, pos, out pos);
            args.Add(arg);

            pos = HandleTypedValueSeparator(tokens, pos);
        }

        return args;
    }

    /// <summary>Handle comma or closing paren in typed value arguments</summary>
    private static int HandleTypedValueSeparator(List<Token> tokens, int pos)
    {
        var token = GetTokenOrThrow(tokens, pos, ", or )");

        if (token.Type == TokenType.Comma)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == TokenType.ParenClose)
                return pos; // Trailing comma
            return pos;
        }

        if (token.Type == TokenType.ParenClose)
            return pos;

        throw new ValueParseException($"Expected , or ) in typed value, got {token.Type}", token);
    }
    
    /// <summary>
    /// Construct a typed value object based on type name and arguments.
    /// </summary>
    private static IGodotValue ConstructTypedValue(string typeName, List<IGodotValue> args, Token typeToken)
    {
        return typeName switch
        {
            "Vector2" => ParseVector2(args, typeToken),
            "Vector3" => ParseVector3(args, typeToken),
            "Vector4" => ParseVector4(args, typeToken),
            "Transform3D" => ParseTransform3D(args, typeToken),
            "Color" => ParseColor(args, typeToken),
            "ExtResource" => ParseExtResource(args, typeToken),
            "SubResource" => ParseSubResource(args, typeToken),
            "NodePath" => ParseNodePath(args, typeToken),
            "Basis" => ParseBasis(args, typeToken),
            "Quaternion" => ParseQuaternion(args, typeToken),
            "AABB" => ParseAABB(args, typeToken),
            "Plane" => ParsePlane(args, typeToken),
            "PackedInt32Array" => ParsePackedInt32Array(args, typeToken),
            _ => throw new ValueParseException($"Unrecognized type: {typeName}", typeToken)
        };
    }
    
    /// <summary>Extract numeric value from argument, must be LiteralValue</summary>
    private static double ExtractNumber(IGodotValue arg, Token context)
    {
        if (arg is not LiteralValue lit || lit.Value is not double num)
            throw new ValueParseException("Expected numeric argument", context);
        return num;
    }

    /// <summary>Extract string value from argument, must be LiteralValue</summary>
    private static string ExtractString(IGodotValue arg, Token context)
    {
        if (arg is not LiteralValue lit || lit.Value is not string str)
            throw new ValueParseException("Expected string argument", context);
        return str;
    }

    private static Vector2Value ParseVector2(List<IGodotValue> args, Token context)
    {
        if (args.Count != 2)
            throw new ValueParseException($"Vector2 expects 2 arguments, got {args.Count}", context);

        var x = ExtractNumber(args[0], context);
        var y = ExtractNumber(args[1], context);
        return new Vector2Value(x, y);
    }

    private static Vector3Value ParseVector3(List<IGodotValue> args, Token context)
    {
        if (args.Count != 3)
            throw new ValueParseException($"Vector3 expects 3 arguments, got {args.Count}", context);

        var x = ExtractNumber(args[0], context);
        var y = ExtractNumber(args[1], context);
        var z = ExtractNumber(args[2], context);
        return new Vector3Value(x, y, z);
    }

    private static Vector4Value ParseVector4(List<IGodotValue> args, Token context)
    {
        if (args.Count != 4)
            throw new ValueParseException($"Vector4 expects 4 arguments, got {args.Count}", context);

        var x = ExtractNumber(args[0], context);
        var y = ExtractNumber(args[1], context);
        var z = ExtractNumber(args[2], context);
        var w = ExtractNumber(args[3], context);
        return new Vector4Value(x, y, z, w);
    }

    private static Transform3DValue ParseTransform3D(List<IGodotValue> args, Token context)
    {
        if (args.Count != 12)
            throw new ValueParseException($"Transform3D expects 12 arguments, got {args.Count}", context);

        var basis = new double[9];
        for (int i = 0; i < 9; i++)
            basis[i] = ExtractNumber(args[i], context);

        var originX = ExtractNumber(args[9], context);
        var originY = ExtractNumber(args[10], context);
        var originZ = ExtractNumber(args[11], context);

        return new Transform3DValue(basis, originX, originY, originZ);
    }

    private static ColorValue ParseColor(List<IGodotValue> args, Token context)
    {
        if (args.Count != 4)
            throw new ValueParseException($"Color expects 4 arguments, got {args.Count}", context);

        var r = ExtractNumber(args[0], context);
        var g = ExtractNumber(args[1], context);
        var b = ExtractNumber(args[2], context);
        var a = ExtractNumber(args[3], context);
        return new ColorValue(r, g, b, a);
    }

    private static ExtResourceValue ParseExtResource(List<IGodotValue> args, Token context)
    {
        if (args.Count != 1)
            throw new ValueParseException($"ExtResource expects 1 argument, got {args.Count}", context);

        var id = ExtractString(args[0], context);
        return new ExtResourceValue(id);
    }

    private static SubResourceValue ParseSubResource(List<IGodotValue> args, Token context)
    {
        if (args.Count != 1)
            throw new ValueParseException($"SubResource expects 1 argument, got {args.Count}", context);

        var id = ExtractString(args[0], context);
        return new SubResourceValue(id);
    }

    private static NodePathValue ParseNodePath(List<IGodotValue> args, Token context)
    {
        if (args.Count != 1)
            throw new ValueParseException($"NodePath expects 1 argument, got {args.Count}", context);

        var path = ExtractString(args[0], context);
        return new NodePathValue(path);
    }

    private static BasisValue ParseBasis(List<IGodotValue> args, Token context)
    {
        if (args.Count != 9)
            throw new ValueParseException($"Basis expects 9 arguments, got {args.Count}", context);

        var values = new double[9];
        for (int i = 0; i < 9; i++)
            values[i] = ExtractNumber(args[i], context);

        return new BasisValue(values);
    }

    private static QuaternionValue ParseQuaternion(List<IGodotValue> args, Token context)
    {
        if (args.Count != 4)
            throw new ValueParseException($"Quaternion expects 4 arguments, got {args.Count}", context);

        var x = ExtractNumber(args[0], context);
        var y = ExtractNumber(args[1], context);
        var z = ExtractNumber(args[2], context);
        var w = ExtractNumber(args[3], context);
        return new QuaternionValue(x, y, z, w);
    }

    private static AABBValue ParseAABB(List<IGodotValue> args, Token context)
    {
        if (args.Count != 2)
            throw new ValueParseException($"AABB expects 2 arguments (position and size), got {args.Count}", context);

        if (args[0] is not Vector3Value position)
            throw new ValueParseException("AABB position must be Vector3", context);
        if (args[1] is not Vector3Value size)
            throw new ValueParseException("AABB size must be Vector3", context);

        return new AABBValue(position, size);
    }

    private static PlaneValue ParsePlane(List<IGodotValue> args, Token context)
    {
        if (args.Count != 2)
            throw new ValueParseException($"Plane expects 2 arguments (normal and distance), got {args.Count}", context);

        if (args[0] is not Vector3Value normal)
            throw new ValueParseException("Plane normal must be Vector3", context);

        var distance = ExtractNumber(args[1], context);
        return new PlaneValue(normal, distance);
    }

    private static PackedInt32ArrayValue ParsePackedInt32Array(List<IGodotValue> args, Token context)
    {
        var values = args
            .Select(arg => (int)ExtractNumber(arg, context))
            .ToList();
        return new PackedInt32ArrayValue(values);
    }
}
