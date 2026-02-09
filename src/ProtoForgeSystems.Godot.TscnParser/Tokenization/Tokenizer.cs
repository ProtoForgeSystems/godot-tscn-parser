using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using ProtoForgeSystems.Godot.TscnParser.Exceptions;

namespace ProtoForgeSystems.Godot.TscnParser.Tokenization;

/// <summary>
/// Tokenizes .tscn files into a stream of Token objects.
/// Implements a character-by-character lexer without regex.
/// </summary>
public class Tokenizer
{
    private string _content = string.Empty;
    private int _position;
    private int _line;
    private int _column;
    
    /// <summary>
    /// Tokenize a .tscn file from disk.
    /// Throws TokenizationException on parse errors.
    /// </summary>
    public List<Token> Tokenize(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return TokenizeContent(content);
    }
    
    /// <summary>
    /// Tokenize content string directly (for testing).
    /// </summary>
    public List<Token> TokenizeContent(string content)
    {
        _content = content;
        _position = 0;
        _line = 1;
        _column = 0;
        
        var tokens = new List<Token>();
        
        while (_position < _content.Length)
        {
            var token = ReadToken();
            if (token.Type == TokenType.Eof)
                break;
            tokens.Add(token);
        }
        
        tokens.Add(new Token(TokenType.Eof, string.Empty, _line, _column));
        return tokens;
    }
    
    /// <summary>
    /// Read one token from the current position.
    /// </summary>
    private Token ReadToken()
    {
        SkipWhitespaceAndComments();

        if (_position >= _content.Length)
            return new Token(TokenType.Eof, string.Empty, _line, _column);

        var startLine = _line;
        var startColumn = _column;

        return ReadTokenByChar(_content[_position], startLine, startColumn);
    }

    /// <summary>
    /// Dispatch token reading based on the current character.
    /// </summary>
    private Token ReadTokenByChar(char ch, int startLine, int startColumn)
    {
        return ch switch
        {
            '[' => ReadSingleCharToken(TokenType.BracketOpen, "[", startLine, startColumn),
            ']' => ReadSingleCharToken(TokenType.BracketClose, "]", startLine, startColumn),
            '(' => ReadSingleCharToken(TokenType.ParenOpen, "(", startLine, startColumn),
            ')' => ReadSingleCharToken(TokenType.ParenClose, ")", startLine, startColumn),
            '{' => ReadSingleCharToken(TokenType.BraceOpen, "{", startLine, startColumn),
            '}' => ReadSingleCharToken(TokenType.BraceClose, "}", startLine, startColumn),
            '=' => ReadSingleCharToken(TokenType.Equal, "=", startLine, startColumn),
            ':' => ReadSingleCharToken(TokenType.Colon, ":", startLine, startColumn),
            ',' => ReadSingleCharToken(TokenType.Comma, ",", startLine, startColumn),
            '.' => ReadNumberOrPeriod(startLine, startColumn),
            '"' => ReadStringToken(startLine, startColumn),
            '-' => ReadNumberOrMinus(startLine, startColumn),
            _ => ReadNumberOrIdentifier(startLine, startColumn)
        };
    }

    /// <summary>
    /// Skip whitespace and comment lines.
    /// </summary>
    private void SkipWhitespaceAndComments()
    {
        while (_position < _content.Length)
        {
            var current = _content[_position];

            if (current == ';')
            {
                SkipCommentLine();
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                AdvanceWhitespace(current);
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Skip to the end of a comment line.
    /// </summary>
    private void SkipCommentLine()
    {
        while (_position < _content.Length && _content[_position] != '\n')
            Advance();
    }

    /// <summary>
    /// Advance past a whitespace character, tracking newlines.
    /// </summary>
    private void AdvanceWhitespace(char current)
    {
        if (current == '\n')
        {
            _line++;
            _column = 0;
        }
        else
        {
            _column++;
        }
        _position++;
    }

    /// <summary>
    /// Read a single-character token and advance.
    /// </summary>
    private Token ReadSingleCharToken(TokenType type, string value, int line, int column)
    {
        Advance();
        return new Token(type, value, line, column);
    }
    
    /// <summary>
    /// Read a number or period token. Periods can start floating-point numbers like ".5".
    /// </summary>
    private Token ReadNumberOrPeriod(int startLine, int startColumn)
    {
        // Check if this is a decimal number like ".5"
        if (_position + 1 < _content.Length && char.IsDigit(_content[_position + 1]))
        {
            return ReadNumber(startLine, startColumn);
        }
        
        // Otherwise it's just a period
        Advance();
        return new Token(TokenType.Period, ".", startLine, startColumn);
    }
    
    /// <summary>
    /// Read a minus sign or negative number.
    /// </summary>
    private Token ReadNumberOrMinus(int startLine, int startColumn)
    {
        // Check if this is a negative number
        if (_position + 1 < _content.Length && 
            (char.IsDigit(_content[_position + 1]) || _content[_position + 1] == '.'))
        {
            return ReadNumber(startLine, startColumn);
        }
        
        // Otherwise it's just a minus operator
        Advance();
        return new Token(TokenType.Minus, "-", startLine, startColumn);
    }
    
    /// <summary>
    /// Read a number, identifier, or color token.
    /// </summary>
    private Token ReadNumberOrIdentifier(int startLine, int startColumn)
    {
        var current = _content[_position];
        
        if (char.IsDigit(current))
        {
            return ReadNumber(startLine, startColumn);
        }
        
        if (char.IsLetter(current) || current == '_')
        {
            return ReadIdentifier(startLine, startColumn);
        }
        
        // Unrecognized character
        var errorChar = current.ToString();
        Advance();
        return new Token(TokenType.Error, $"Unrecognized character: {errorChar}", startLine, startColumn);
    }
    
    /// <summary>
    /// Read a string literal, handling escape sequences.
    /// Assumes opening quote has been seen.
    /// </summary>
    private Token ReadStringToken(int startLine, int startColumn)
    {
        Advance(); // Skip opening quote
        var value = ReadString();
        
        if (_position >= _content.Length)
        {
            throw new TokenizationException("Unterminated string literal", startLine, startColumn);
        }
        
        Advance(); // Skip closing quote
        return new Token(TokenType.String, value, startLine, startColumn);
    }
    
    /// <summary>
    /// Read the content of a string, handling escape sequences.
    /// Stops at closing quote (but does not consume it).
    /// </summary>
    private string ReadString()
    {
        var sb = new StringBuilder();

        while (_position < _content.Length && _content[_position] != '"')
        {
            if (_content[_position] == '\\' && _position + 1 < _content.Length)
                AppendEscapeSequence(sb);
            else
                sb.Append(_content[_position]);

            AdvanceInString();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Process escape sequence at current position and append to StringBuilder.
    /// Assumes position is on backslash character.
    /// </summary>
    private void AppendEscapeSequence(StringBuilder sb)
    {
        _position++;
        _column++;

        switch (_content[_position])
        {
            case '"': sb.Append('"'); break;
            case '\\': sb.Append('\\'); break;
            case 'n': sb.Append('\n'); break;
            case 'r': sb.Append('\r'); break;
            case 't': sb.Append('\t'); break;
            case 'u': AppendUnicodeEscape(sb); break;
            default: sb.Append('\\').Append(_content[_position]); break;
        }
    }

    /// <summary>
    /// Process Unicode escape sequence (\uXXXX) and append to StringBuilder.
    /// Assumes position is on 'u' character.
    /// </summary>
    private void AppendUnicodeEscape(StringBuilder sb)
    {
        if (_position + 4 < _content.Length)
        {
            var hexStr = _content.Substring(_position + 1, 4);
            if (int.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codepoint))
            {
                sb.Append((char)codepoint);
                _position += 4;
                _column += 4;
                return;
            }
        }

        sb.Append('\\').Append(_content[_position]);
    }

    /// <summary>
    /// Advance position within a string, tracking line/column.
    /// </summary>
    private void AdvanceInString()
    {
        if (_content[_position] == '\n')
        {
            _line++;
            _column = 0;
        }
        else
        {
            _column++;
        }

        _position++;
    }
    
    /// <summary>
    /// Read a number token (integer, float, hex, scientific notation).
    /// Handles: 1, 1.5, -5, 0xFF, 1e-5
    /// </summary>
    private Token ReadNumber(int startLine, int startColumn)
    {
        var sb = new StringBuilder();

        AppendLeadingMinus(sb);

        if (IsHexNumber())
            return ReadHexNumber(sb, startLine, startColumn);

        ReadIntegerPart(sb);
        ReadDecimalPart(sb);
        ReadExponentPart(sb);

        return new Token(TokenType.Number, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    /// Append leading minus sign if present.
    /// </summary>
    private void AppendLeadingMinus(StringBuilder sb)
    {
        if (_position < _content.Length && _content[_position] == '-')
        {
            sb.Append(_content[_position]);
            Advance();
        }
    }

    /// <summary>
    /// Check if current position is a hex number (0x prefix).
    /// </summary>
    private bool IsHexNumber()
    {
        return _position + 1 < _content.Length &&
               _content[_position] == '0' &&
               char.ToLowerInvariant(_content[_position + 1]) == 'x';
    }

    /// <summary>
    /// Read a hexadecimal number and return the token.
    /// </summary>
    private Token ReadHexNumber(StringBuilder sb, int startLine, int startColumn)
    {
        sb.Append(_content[_position]);
        Advance();
        sb.Append(_content[_position]);
        Advance();

        while (_position < _content.Length && IsHexDigit(_content[_position]))
        {
            sb.Append(_content[_position]);
            Advance();
        }

        return new Token(TokenType.Number, sb.ToString(), startLine, startColumn);
    }

    /// <summary>
    /// Read the integer part of a number (before decimal point).
    /// </summary>
    private void ReadIntegerPart(StringBuilder sb)
    {
        if (_content[_position] != '.')
        {
            while (_position < _content.Length && char.IsDigit(_content[_position]))
            {
                sb.Append(_content[_position]);
                Advance();
            }
        }
    }

    /// <summary>
    /// Read the decimal part of a number (after decimal point).
    /// </summary>
    private void ReadDecimalPart(StringBuilder sb)
    {
        if (_position < _content.Length && _content[_position] == '.')
        {
            sb.Append(_content[_position]);
            Advance();

            while (_position < _content.Length && char.IsDigit(_content[_position]))
            {
                sb.Append(_content[_position]);
                Advance();
            }
        }
    }

    /// <summary>
    /// Read the exponent part of a number (e.g., e-5).
    /// </summary>
    private void ReadExponentPart(StringBuilder sb)
    {
        if (_position < _content.Length && char.ToLowerInvariant(_content[_position]) == 'e')
        {
            sb.Append(_content[_position]);
            Advance();

            if (_position < _content.Length && (_content[_position] == '+' || _content[_position] == '-'))
            {
                sb.Append(_content[_position]);
                Advance();
            }

            while (_position < _content.Length && char.IsDigit(_content[_position]))
            {
                sb.Append(_content[_position]);
                Advance();
            }
        }
    }
    
    /// <summary>
    /// Read an identifier or keyword.
    /// Identifiers start with letter or underscore, followed by letters, digits, underscores, or slashes (for Godot property names like "tracks/0/type").
    /// </summary>
    private Token ReadIdentifier(int startLine, int startColumn)
    {
        var sb = new System.Text.StringBuilder();
        
        while (_position < _content.Length && 
               (char.IsLetterOrDigit(_content[_position]) || _content[_position] == '_' || _content[_position] == '/'))
        {
            sb.Append(_content[_position]);
            Advance();
        }
        
        var value = sb.ToString();
        
        // Check for Color keyword (followed by parenthesis)
        if (value == "Color" && _position < _content.Length && _content[_position] == '(')
        {
            return new Token(TokenType.Color, value, startLine, startColumn);
        }
        
        return new Token(TokenType.Identifier, value, startLine, startColumn);
    }
    
    /// <summary>
    /// Advance to the next character and update line/column tracking.
    /// </summary>
    private void Advance()
    {
        if (_position < _content.Length)
        {
            if (_content[_position] == '\n')
            {
                _line++;
                _column = 0;
            }
            else
            {
                _column++;
            }
            
            _position++;
        }
    }
    
    /// <summary>
    /// Check if a character is a valid hexadecimal digit.
    /// </summary>
    private static bool IsHexDigit(char c)
    {
        return char.IsDigit(c) || 
               (c >= 'a' && c <= 'f') || 
               (c >= 'A' && c <= 'F');
    }
}
