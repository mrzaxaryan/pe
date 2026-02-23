# PeSharp

A managed .NET library for parsing Windows Portable Executable (PE) files. Supports PE32 and PE32+ formats with lazy evaluation and memory-efficient file access.

## Features

- **Headers** — DOS, COFF, and Optional headers (PE32/PE32+)
- **Sections** — Section headers with characteristics, sizes, and addresses
- **Exports** — Exported functions with names, ordinals, and forwarding info
- **Imports** — Imported modules and functions (by name or ordinal)
- **Resources** — Hierarchical resource directory tree
- **Debug info** — Debug directory entries (CodeView, PDB, etc.)
- **Base relocations** — Relocation blocks and entries
- **RVA resolution** — Convert relative virtual addresses to file offsets
- **Flexible input** — Read from file path, `byte[]`, or `Stream`
- **Memory-efficient** — Uses `MemoryMappedFile` for large files

## Installation

```
dotnet add package PeSharp
```

## Quick start

```csharp
using PeSharp;

using var pe = new PeFile("path/to/file.exe");

Console.WriteLine($"Machine: {pe.CoffHeader.Machine}");
Console.WriteLine($"64-bit: {pe.Is64Bit}");
Console.WriteLine($"DLL: {pe.IsDll}");
Console.WriteLine($"Entry point: 0x{pe.OptionalHeader.AddressOfEntryPoint:X8}");
Console.WriteLine($"Sections: {pe.SectionHeaders.Count}");

// Inspect sections
foreach (var section in pe.SectionHeaders)
{
    Console.WriteLine($"  {section.Name,-8} VA=0x{section.VirtualAddress:X8} Size=0x{section.VirtualSize:X8}");
}

// Inspect imports (lazy-loaded)
if (pe.Imports is { } imports)
{
    foreach (var module in imports.Modules)
    {
        Console.WriteLine($"{module.Name} ({module.Functions.Count} functions)");
    }
}

// Inspect exports (lazy-loaded)
if (pe.Exports is { } exports)
{
    foreach (var func in exports.Functions)
    {
        Console.WriteLine($"  #{func.Ordinal}: {func.Name ?? "(by ordinal)"} @ 0x{func.Address:X8}");
    }
}
```

You can also load from a byte array or stream:

```csharp
byte[] data = File.ReadAllBytes("file.dll");
using var pe = new PeFile(data);
```

```csharp
using var stream = File.OpenRead("file.dll");
using var pe = new PeFile(stream);
```

## API overview

### `PeFile`

| Member | Type | Description |
|---|---|---|
| `DosHeader` | `DosHeader` | Legacy DOS header |
| `CoffHeader` | `CoffHeader` | COFF file header (machine type, characteristics, timestamp) |
| `OptionalHeader` | `OptionalHeader` | PE optional header (image base, entry point, subsystem) |
| `SectionHeaders` | `IReadOnlyList<SectionHeader>` | Section table |
| `Is64Bit` | `bool` | `true` if PE32+ |
| `IsDll` | `bool` | `true` if DLL |
| `Exports` | `ExportDirectory?` | Export directory (lazy) |
| `Imports` | `ImportDirectory?` | Import directory (lazy) |
| `Resources` | `ResourceDirectory?` | Resource directory (lazy) |
| `Relocations` | `BaseRelocation?` | Base relocation table (lazy) |
| `DebugInfo` | `DebugDirectory?` | Debug directory (lazy) |
| `ResolveRva(uint)` | `uint?` | Convert RVA to file offset |
| `GetSectionData(string)` | `ReadOnlySpan<byte>` | Raw section data by name |
| `GetSectionData(int)` | `ReadOnlySpan<byte>` | Raw section data by index |

### Exceptions

| Exception | Description |
|---|---|
| `PeFormatException` | Base exception for PE format errors |
| `InvalidPeSignatureException` | Invalid MZ or PE signature |
| `PeTruncatedException` | Unexpected end of file |

## Requirements

- .NET 10.0 or later

## License

[MIT](LICENSE)
