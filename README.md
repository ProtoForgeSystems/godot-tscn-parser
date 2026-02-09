# ProtoForgeSystems.Godot.TscnParser

A zero-dependency .NET parser for Godot `.tscn` (text scene) files. Produces a typed scene graph and value model without requiring the Godot engine.

## Installation

```bash
dotnet add package ProtoForgeSystems.Godot.TscnParser
```

Or via NuGet Package Manager:

```bash
Install-Package ProtoForgeSystems.Godot.TscnParser
```

## Usage

### Parse a Complete Scene File

```csharp
using ProtoForgeSystems.Godot.TscnParser.Parsing;

var parser = new TscnFileParser();
var scene = parser.ParseFile("path/to/scene.tscn");

// Access scene structure
foreach (var node in scene.Nodes)
{
    Console.WriteLine($"Node: {node.Name}, Type: {node.Type}");
}
```

### Extract Property Values

```csharp
using ProtoForgeSystems.Godot.TscnParser.Extraction;

// Parse a Godot property value from a string
var value = TscnValueExtractor.ExtractValue("Vector3(1.0, 2.0, 3.0)");
```

## Features

- **Zero Dependencies**: Pure .NET implementation with no external packages
- **Typed Scene Graph**: Strongly-typed models for scene structure
- **Value Parsing**: Parse Godot property values (Vector3, Transform3D, etc.)
- **Error Diagnostics**: Detailed error messages with line/column information
- **No Godot Runtime**: Works without Godot engine installed

## Documentation

For more information about Godot's `.tscn` file format, see the [Godot TSCN Format Documentation](https://docs.godotengine.org/en/stable/contributing/development/file_formats/tscn.html).

## License

MIT License - see LICENSE file for details.
