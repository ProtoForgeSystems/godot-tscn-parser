using System;
using System.Collections.Generic;
using ProtoForgeSystems.Godot.TscnParser.Models;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;
using ProtoForgeSystems.Godot.TscnParser.Exceptions;

namespace ProtoForgeSystems.Godot.TscnParser.Parsing;

/// <summary>
/// Parses complete .tscn scene structure from a token stream.
/// Implements recursive descent parsing for scene headers, resources, nodes, and connections.
/// </summary>
public class SceneParser
{
    private readonly ValueParser _valueParser = new();
    private List<Token> _tokens = new();
    private int _position;
    private List<string> _warnings = new();
    
    /// <summary>
    /// Parse a complete .tscn file from token stream.
    /// </summary>
    public GodotScene Parse(List<Token> tokens)
    {
        InitializeParserState(tokens);

        try
        {
            var header = ParseSceneHeader();
            var externalResources = ParseAllExternalResources();
            var subResources = ParseAllSubResources();
            var nodes = ParseAllNodes();
            var connections = ParseAllConnections();
            var editables = ParseAllEditables();

            return BuildScene(header, externalResources, subResources, nodes, connections, editables);
        }
        catch (Exception ex) when (ex is not SceneParseException)
        {
            throw new SceneParseException($"Unexpected error during parsing: {ex.Message}", ex);
        }
    }

    private void InitializeParserState(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
        _warnings = new();
    }

    private List<ExternalResource> ParseAllExternalResources()
    {
        var externalResources = new List<ExternalResource>();
        while (HasMoreSectionsOfType("ext_resource"))
        {
            externalResources.Add(ParseExternalResource());
        }
        return externalResources;
    }

    private List<SubResource> ParseAllSubResources()
    {
        var subResources = new List<SubResource>();
        while (HasMoreSectionsOfType("sub_resource"))
        {
            subResources.Add(ParseSubResource());
        }
        return subResources;
    }

    private List<GodotNode> ParseAllNodes()
    {
        var nodes = new List<GodotNode>();
        while (HasMoreSectionsOfType("node"))
        {
            nodes.Add(ParseNode());
        }
        return nodes;
    }

    private List<Connection> ParseAllConnections()
    {
        var connections = new List<Connection>();
        while (HasMoreSectionsOfType("connection"))
        {
            connections.Add(ParseConnection());
        }
        return connections;
    }

    private List<string> ParseAllEditables()
    {
        var editables = new List<string>();
        while (HasMoreSectionsOfType("editable"))
        {
            editables.Add(ParseEditable());
        }
        return editables;
    }

    private bool HasMoreSectionsOfType(string sectionType)
    {
        return _position < _tokens.Count &&
               PeekToken().Type == TokenType.BracketOpen &&
               PeekIdentifierAt(1) == sectionType;
    }

    private GodotScene BuildScene(
        SceneHeader header,
        List<ExternalResource> externalResources,
        List<SubResource> subResources,
        List<GodotNode> nodes,
        List<Connection> connections,
        List<string> editables)
    {
        return new GodotScene(header, externalResources, subResources, nodes, connections, editables, _warnings);
    }
    
    /// <summary>
    /// Parse scene header: [gd_scene load_steps=X format=Y uid="..."]
    /// Note: load_steps is deprecated in Godot 4+ and is optional
    /// </summary>
    private SceneHeader ParseSceneHeader()
    {
        var token = ExpectSectionHeader("gd_scene");
        var attributes = ParseSectionAttributes();
        ValidateSceneFormat(attributes, token);

        var loadSteps = ExtractOptionalInt(attributes, "load_steps", 0);
        var format = ExtractRequiredInt(attributes, "format", token);
        var uid = ExtractOptionalString(attributes, "uid");

        return new SceneHeader(loadSteps, format, uid);
    }

    private Token ExpectSectionHeader(string sectionName)
    {
        var token = ExpectToken(TokenType.BracketOpen);
        ExpectIdentifier(sectionName);
        return token;
    }

    private Dictionary<string, IGodotValue> ParseSectionAttributes()
    {
        var attributes = ParseTagAttributes();
        ExpectToken(TokenType.BracketClose);
        return attributes;
    }

    private static void ValidateSceneFormat(Dictionary<string, IGodotValue> attributes, Token token)
    {
        if (!attributes.ContainsKey("format"))
            throw new SceneParseException("Missing 'format' in gd_scene header", token);
    }
    
    /// <summary>
    /// Parse external resource: [ext_resource type="..." path="..." uid="..." id="..."]
    /// </summary>
    private ExternalResource ParseExternalResource()
    {
        var token = ExpectSectionHeader("ext_resource");
        var attributes = ParseSectionAttributes();
        return BuildExternalResource(attributes, token);
    }

    private static ExternalResource BuildExternalResource(Dictionary<string, IGodotValue> attributes, Token token)
    {
        var type = ExtractRequiredString(attributes, "type", token);
        var path = ExtractRequiredString(attributes, "path", token);
        var id = ExtractRequiredString(attributes, "id", token);
        var uid = ExtractOptionalString(attributes, "uid");

        return new ExternalResource(type, path, uid, id);
    }
    
    /// <summary>
    /// Parse sub-resource: [sub_resource type="..." id="..."]
    /// Followed by property assignments.
    /// </summary>
    private SubResource ParseSubResource()
    {
        var token = ExpectSectionHeader("sub_resource");
        var attributes = ParseSectionAttributes();
        return BuildSubResource(attributes, token);
    }

    private SubResource BuildSubResource(Dictionary<string, IGodotValue> attributes, Token token)
    {
        var type = ExtractRequiredString(attributes, "type", token);
        var id = ExtractRequiredString(attributes, "id", token);
        var properties = ParseNodeProperties();

        return new SubResource(type, id, properties);
    }
    
    /// <summary>
    /// Parse node: [node name="..." type="..." parent="..." instance=...]
    /// Followed by property assignments.
    /// </summary>
    private GodotNode ParseNode()
    {
        var token = ExpectSectionHeader("node");
        var attributes = ParseSectionAttributes();
        return BuildNode(attributes, token);
    }

    private GodotNode BuildNode(Dictionary<string, IGodotValue> attributes, Token token)
    {
        var name = ExtractRequiredString(attributes, "name", token);
        var type = ExtractOptionalString(attributes, "type");
        var parent = ExtractOptionalString(attributes, "parent");
        var instance = ExtractOptionalInstance(attributes);
        var instancePlaceholder = ExtractOptionalString(attributes, "instance_placeholder");
        var uniqueId = ExtractOptionalIntNullable(attributes, "unique_id");
        var groups = ExtractOptionalGroups(attributes);
        var properties = ParseNodeProperties();

        return new GodotNode(name, type, parent, instance, instancePlaceholder, groups, properties, uniqueId);
    }

    private static string? ExtractOptionalInstance(Dictionary<string, IGodotValue> attributes)
    {
        if (!attributes.TryGetValue("instance", out var instanceValue))
            return null;
        return ExtractReferenceFromValue(instanceValue);
    }

    private List<string> ExtractOptionalGroups(Dictionary<string, IGodotValue> attributes)
    {
        if (!attributes.TryGetValue("groups", out var groupsValue))
            return new List<string>();
        return ExtractGroupsList(groupsValue);
    }
    
    /// <summary>
    /// Parse connection: [connection signal="..." from="..." to="..." method="..."]
    /// </summary>
    private Connection ParseConnection()
    {
        var token = ExpectSectionHeader("connection");
        var attributes = ParseSectionAttributes();
        return BuildConnection(attributes, token);
    }

    private static Connection BuildConnection(Dictionary<string, IGodotValue> attributes, Token token)
    {
        var signal = ExtractRequiredString(attributes, "signal", token);
        var from = ExtractRequiredString(attributes, "from", token);
        var to = ExtractRequiredString(attributes, "to", token);
        var method = ExtractRequiredString(attributes, "method", token);
        var flags = ExtractConnectionFlags(attributes);
        var binds = ExtractConnectionBinds(attributes);
        var unbinds = ExtractOptionalIntNullable(attributes, "unbinds");

        return new Connection(signal, from, to, method, binds, flags, unbinds);
    }

    private static int ExtractConnectionFlags(Dictionary<string, IGodotValue> attributes)
    {
        const int DefaultConnectionFlags = 1; // Object.CONNECT_PERSIST

        if (!attributes.TryGetValue("flags", out var flagsValue))
            return DefaultConnectionFlags;

        return ExtractIntFromLiteral(flagsValue, "flags");
    }

    private static List<IGodotValue>? ExtractConnectionBinds(Dictionary<string, IGodotValue> attributes)
    {
        if (!attributes.TryGetValue("binds", out var bindsValue))
            return null;

        if (bindsValue is ArrayValue arrayValue)
            return arrayValue.Items;

        return null;
    }
    
    /// <summary>
    /// Parse editable: [editable path="..."]
    /// </summary>
    private string ParseEditable()
    {
        var token = ExpectToken(TokenType.BracketOpen);
        ExpectIdentifier("editable");
        
        var attributes = ParseTagAttributes();
        
        ExpectToken(TokenType.BracketClose);
        
        if (!attributes.TryGetValue("path", out var pathValue))
            throw new SceneParseException("Missing 'path' in editable", token);
        
        string path = ExtractStringFromLiteral(pathValue, "path");
        
        return path;
    }
    
    /// <summary>
    /// Helper: parse tag attributes (e.g., name="...", type="...")
    /// Returns a dictionary of attribute name -> value
    /// </summary>
    private Dictionary<string, IGodotValue> ParseTagAttributes()
    {
        var attributes = new Dictionary<string, IGodotValue>();

        while (HasMoreAttributes())
        {
            var attrName = ConsumeAttributeName();
            ExpectAttributeEquals(attrName);
            var value = ParseAttributeValue(attrName);
            attributes[attrName] = value;
        }

        return attributes;
    }

    private bool HasMoreAttributes()
    {
        return _position < _tokens.Count &&
               PeekToken().Type != TokenType.BracketClose &&
               PeekToken().Type == TokenType.Identifier;
    }

    private string ConsumeAttributeName()
    {
        return ConsumeToken().Value;
    }

    private void ExpectAttributeEquals(string attrName)
    {
        if (_position >= _tokens.Count || PeekToken().Type != TokenType.Equal)
            throw new SceneParseException($"Expected '=' after attribute name '{attrName}'", PeekToken());
        ConsumeToken();
    }

    private IGodotValue ParseAttributeValue(string attrName)
    {
        if (_position >= _tokens.Count)
            throw new SceneParseException($"Expected value for attribute '{attrName}'");

        var value = _valueParser.ParseValue(_tokens, _position, out int endIndex);
        _position = endIndex;
        return value;
    }
    
    /// <summary>
    /// Helper: parse property assignments until next tag
    /// </summary>
    private Dictionary<string, IGodotValue> ParseNodeProperties()
    {
        var properties = new Dictionary<string, IGodotValue>();

        while (HasMoreProperties())
        {
            if (!TryParseNextProperty(properties))
                break;
        }

        return properties;
    }

    private bool HasMoreProperties()
    {
        return _position < _tokens.Count &&
               PeekToken().Type != TokenType.BracketOpen &&
               PeekToken().Type == TokenType.Identifier;
    }

    private bool TryParseNextProperty(Dictionary<string, IGodotValue> properties)
    {
        string propName = ConsumeToken().Value;

        if (!IsValidPropertyAssignment())
            return false;

        ConsumeToken(); // consume '='

        if (_position >= _tokens.Count)
            return false;

        return TryParsePropertyValue(properties, propName);
    }

    private bool IsValidPropertyAssignment()
    {
        return _position < _tokens.Count && PeekToken().Type == TokenType.Equal;
    }

    private bool TryParsePropertyValue(Dictionary<string, IGodotValue> properties, string propName)
    {
        try
        {
            var value = _valueParser.ParseValue(_tokens, _position, out int endIndex);
            _position = endIndex;
            properties[propName] = value;
            return true;
        }
        catch (Exception ex)
        {
            HandlePropertyParseError(propName, ex);
            return true;
        }
    }

    private void HandlePropertyParseError(string propName, Exception ex)
    {
        _warnings.Add($"Failed to parse property '{propName}': {ex.Message}");
        SkipToNextPropertyOrTag();
    }

    private void SkipToNextPropertyOrTag()
    {
        while (_position < _tokens.Count &&
               PeekToken().Type != TokenType.BracketOpen &&
               PeekToken().Type != TokenType.Identifier)
        {
            _position++;
        }
    }
    
    #region Helper Methods

    private static string ExtractRequiredString(Dictionary<string, IGodotValue> attributes, string key, Token token)
    {
        if (!attributes.TryGetValue(key, out var value))
            throw new SceneParseException($"Missing '{key}' in {token.Value}", token);
        return ExtractStringFromLiteral(value, key);
    }

    private static int ExtractRequiredInt(Dictionary<string, IGodotValue> attributes, string key, Token token)
    {
        if (!attributes.TryGetValue(key, out var value))
            throw new SceneParseException($"Missing '{key}' in {token.Value}", token);
        return ExtractIntFromLiteral(value, key);
    }

    private static string? ExtractOptionalString(Dictionary<string, IGodotValue> attributes, string key)
    {
        if (!attributes.TryGetValue(key, out var value))
            return null;
        return ExtractStringFromLiteral(value, key);
    }

    private static int ExtractOptionalInt(Dictionary<string, IGodotValue> attributes, string key, int defaultValue)
    {
        if (!attributes.TryGetValue(key, out var value))
            return defaultValue;
        return ExtractIntFromLiteral(value, key);
    }

    private static int? ExtractOptionalIntNullable(Dictionary<string, IGodotValue> attributes, string key)
    {
        if (!attributes.TryGetValue(key, out var value))
            return null;
        return ExtractIntFromLiteral(value, key);
    }

    private Token PeekToken()
    {
        if (_position >= _tokens.Count)
            return new Token(TokenType.Eof, string.Empty, 0, 0);
        return _tokens[_position];
    }
    
    private string? PeekIdentifierAt(int offset)
    {
        if (_position + offset >= _tokens.Count)
            return null;
        var token = _tokens[_position + offset];
        if (token.Type == TokenType.Identifier)
            return token.Value;
        return null;
    }
    
    private Token ConsumeToken()
    {
        if (_position >= _tokens.Count)
            throw new SceneParseException("Unexpected end of input");
        return _tokens[_position++];
    }
    
    private Token ExpectToken(TokenType type)
    {
        var token = PeekToken();
        if (token.Type != type)
            throw new SceneParseException($"Expected {type}, got {token.Type}", token);
        return ConsumeToken();
    }
    
    private Token ExpectIdentifier(string expected)
    {
        var token = PeekToken();
        if (token.Type != TokenType.Identifier || token.Value != expected)
            throw new SceneParseException($"Expected identifier '{expected}', got '{token.Value}'", token);
        return ConsumeToken();
    }
    
    private static int ExtractIntFromLiteral(IGodotValue value, string fieldName)
    {
        if (value is not LiteralValue lit)
            throw new SceneParseException($"Expected literal value for {fieldName}");

        if (lit.Value is int intVal)
            return intVal;

        if (lit.Value is double doubleVal)
            return (int)doubleVal;

        throw new SceneParseException($"Expected numeric value for {fieldName}");
    }

    private static string ExtractStringFromLiteral(IGodotValue value, string fieldName)
    {
        if (value is not LiteralValue lit)
            throw new SceneParseException($"Expected literal value for {fieldName}");

        if (lit.Value is string strVal)
            return strVal;

        throw new SceneParseException($"Expected string value for {fieldName}");
    }

    private static string ExtractReferenceFromValue(IGodotValue value)
    {
        if (value is ExtResourceValue ext)
            return $"ExtResource(\"{ext.Id}\")";
        
        if (value is SubResourceValue sub)
            return $"SubResource(\"{sub.Id}\")";
        
        if (value is LiteralValue lit && lit.Value is string str)
            return str;
        
        throw new SceneParseException("Expected ExtResource, SubResource, or string reference");
    }
    
    private List<string> ExtractGroupsList(IGodotValue value)
    {
        var groups = new List<string>();
        
        if (value is ArrayValue arrayValue)
        {
            foreach (var item in arrayValue.Items)
            {
                if (item is LiteralValue lit && lit.Value is string str)
                {
                    groups.Add(str);
                }
                else
                {
                    _warnings.Add("Invalid value in groups array, expected string");
                }
            }
        }
        else
        {
            _warnings.Add("Expected array value for groups");
        }
        
        return groups;
    }
    
    #endregion
}
