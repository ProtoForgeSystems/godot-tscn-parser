# Copilot Instructions

## Build, Test, and Lint

```bash
# Restore dependencies
dotnet restore

# Build (warnings are treated as errors)
dotnet build -c Release

# Run all tests
dotnet test -c Release --verbosity normal

# Run a single test by name
dotnet test -c Release --filter "FullyQualifiedName~ParseContent_MinimalScene"

# Run tests in a specific class
dotnet test -c Release --filter "ClassName~SceneParserTests"

# Pack NuGet package
dotnet pack src/ProtoForgeSystems.Godot.TscnParser/ProtoForgeSystems.Godot.TscnParser.csproj -c Release -o artifacts
```

## Architecture

This is a zero-dependency .NET 9 library (NuGet: `ProtoForgeSystems.Godot.TscnParser`) that parses Godot `.tscn` text scene files into a typed model without requiring the Godot engine.

**Pipeline: file/string → Tokenizer → SceneParser + ValueParser → GodotScene**

```
src/
  Tokenization/   Tokenizer (char-by-char lexer, no regex), Token, TokenType
  Parsing/        TscnFileParser (public API), SceneParser (scene structure),
                  ValueParser (recursive descent for property values)
  Extraction/     TscnValueExtractor (convenience: tokenize + parse in one call)
  Models/         GodotScene, GodotValue (IGodotValue hierarchy)
  Exceptions/     TscnParseException (base), SceneParseException,
                  ValueParseException, TokenizationException
```

**Entry points:**
- `TscnFileParser.ParseFile(path)` / `ParseContent(string)` — parse a full scene
- `TscnValueExtractor.ExtractValue(string)` — parse a single property value string

**`GodotScene`** holds: `SceneHeader`, `List<ExternalResource>`, `List<SubResource>`, `List<GodotNode>`, `List<Connection>`, `List<string> EditableInstances`, and `ParsingWarnings`. It also provides `BuildNodeNameIndex()`, `BuildExternalResourceIndex()`, `BuildSubResourceIndex()`, and `BuildParentIndex()` for O(1) lookups when needed.

**`IGodotValue` hierarchy** (all are C# `record` types):
`LiteralValue`, `Vector2Value`, `Vector3Value`, `Vector4Value`, `Transform3DValue`, `ColorValue`, `ExtResourceValue`, `SubResourceValue`, `ArrayValue`, `DictionaryValue`, `NodePathValue`, `BasisValue`, `QuaternionValue`, `AABBValue`, `PlaneValue`, `PackedInt32ArrayValue`, `PackedStringArrayValue`, `PackedVector3ArrayValue`, `UnknownTypedValue`

Unrecognized typed constructors (e.g. `SomeNewType(...)`) are parsed into `UnknownTypedValue(TypeName, Args)` rather than throwing, so the parser stays resilient to unknown Godot types.

**`ValueParser` consumption contract** — `ParseValue` takes a start index and returns the parsed value plus an `out int endIndex` telling the caller where consumption stopped. Callers advance their own position from `endIndex`; the parser never mutates shared state.

## Key Conventions

- **All models are C# `record` types** — `IGodotValue` implementations and all scene model types (`GodotScene`, `GodotNode`, `ExternalResource`, etc.) use `record`.
- **`TreatWarningsAsErrors=true`** — the library project treats all compiler warnings as errors. Fix all warnings before building.
- **`Nullable=enable`** — null-safety is enforced throughout. Use `?` annotations for optional values; don't suppress nullability without cause.
- **Version in `Directory.Build.props`** — the single source of truth for version, authors, and NuGet metadata is `Directory.Build.props` at the repo root. Never set these per-project.
- **Exception hierarchy** — all parse errors derive from `TscnParseException` (abstract), which formats `"ErrorType at line X, column Y: message"`. Use `SceneParseException`, `ValueParseException`, or `TokenizationException` as appropriate — never throw `TscnParseException` directly. Always attach the failing `Token` when available so line/column info is preserved.
- **`ParseContent(string)` for tests** — use `TscnFileParser.ParseContent(...)` or `Tokenizer.TokenizeContent(...)` in tests; never read files from disk in unit tests.
- **Test framework: xunit** with `[Fact]` and `[Theory]`/`[InlineData]`. Test classes instantiate the parser/tokenizer in a field. Tests are organized in `tests/` mirroring `src/` namespaces (e.g., `Tests.Parsing`, `Tests.Extraction`, `Tests.Tokenization`). Test method naming follows `Method_Scenario_ExpectedBehavior` (e.g., `TokenizeContent_Empty_ReturnsOnlyEof`).
- **No external dependencies in the library** — `src/` has zero runtime package references. Only dev-time analyzers (`Microsoft.CodeAnalysis.NetAnalyzers`) are allowed.
- **`AABB` args** — `AABBValue` accepts either 6 flat numeric args or 2 `Vector3` args; other combinations throw `ValueParseException`.
- **Typed dictionary syntax** — `Dictionary[K, V]({...})` is parsed into `DictionaryValue`. Integer/identifier keys are stringified (e.g., key `1` becomes `"1"`). `IsTypedDictionary` is checked before `IsTypedArray` since both start with `Identifier + [`.
