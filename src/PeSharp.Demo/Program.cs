using PeSharp;

if (args.Length == 0)
{
    Console.WriteLine("Usage: PeSharp.Demo <path-to-pe-file>");
    return;
}

using var pe = new PeFile(args[0]);

Console.WriteLine($"File: {args[0]}");
Console.WriteLine($"Machine: {pe.CoffHeader.Machine}");
Console.WriteLine($"Format: {(pe.Is64Bit ? "PE32+ (64-bit)" : "PE32 (32-bit)")}");
Console.WriteLine($"Is DLL: {pe.IsDll}");
Console.WriteLine($"Timestamp: {pe.CoffHeader.Timestamp:u}");
Console.WriteLine($"Entry Point: 0x{pe.OptionalHeader.AddressOfEntryPoint:X8}");
Console.WriteLine($"Image Base: 0x{pe.OptionalHeader.ImageBase:X16}");
Console.WriteLine($"Subsystem: {pe.OptionalHeader.Subsystem}");
Console.WriteLine($"Sections: {pe.SectionHeaders.Count}");
Console.WriteLine();

Console.WriteLine("Sections:");
foreach (var section in pe.SectionHeaders)
{
    Console.WriteLine($"  {section.Name,-8} VA=0x{section.VirtualAddress:X8} " +
                      $"VSize=0x{section.VirtualSize:X8} " +
                      $"Raw=0x{section.SizeOfRawData:X8} " +
                      $"Flags={section.Characteristics}");
}

if (pe.Exports is { } exports)
{
    Console.WriteLine($"\nExports from {exports.Name} ({exports.Functions.Count} functions):");
    foreach (var func in exports.Functions.Take(20))
    {
        string name = func.Name ?? "(by ordinal)";
        string forwarder = func.IsForwarder ? $" -> {func.ForwarderName}" : "";
        Console.WriteLine($"  #{func.Ordinal}: {name} @ 0x{func.Address:X8}{forwarder}");
    }
    if (exports.Functions.Count > 20)
        Console.WriteLine($"  ... and {exports.Functions.Count - 20} more");
}

if (pe.Imports is { } imports)
{
    Console.WriteLine($"\nImports ({imports.Modules.Count} modules):");
    foreach (var mod in imports.Modules)
    {
        Console.WriteLine($"  {mod.Name} ({mod.Functions.Count} functions):");
        foreach (var func in mod.Functions.Take(5))
        {
            Console.WriteLine($"    {(func.IsByOrdinal ? $"#{func.Ordinal}" : func.Name)}");
        }
        if (mod.Functions.Count > 5)
            Console.WriteLine($"    ... and {mod.Functions.Count - 5} more");
    }
}

if (pe.Relocations is { } relocs)
{
    int totalEntries = relocs.Blocks.Sum(b => b.Entries.Count);
    Console.WriteLine($"\nRelocations: {relocs.Blocks.Count} blocks, {totalEntries} entries");
}

if (pe.DebugInfo is { } debug)
{
    Console.WriteLine($"\nDebug entries ({debug.Entries.Count}):");
    foreach (var entry in debug.Entries)
    {
        Console.WriteLine($"  Type={entry.Type} Size=0x{entry.SizeOfData:X} RawAddr=0x{entry.PointerToRawData:X}");
    }
}

if (pe.Resources is { } resources)
{
    Console.WriteLine($"\nResources ({resources.Entries.Count} top-level entries):");
    PrintResources(resources, "  ");
}

static void PrintResources(PeSharp.DataDirectories.ResourceDirectory dir, string indent)
{
    foreach (var entry in dir.Entries)
    {
        string label = entry.IsNamed ? entry.Name! : $"ID={entry.Id}";
        if (entry.IsDirectory)
        {
            Console.WriteLine($"{indent}{label} (directory, {entry.Subdirectory!.Entries.Count} entries)");
            if (indent.Length < 12) // limit depth
                PrintResources(entry.Subdirectory!, indent + "  ");
        }
        else
        {
            Console.WriteLine($"{indent}{label} (data, size=0x{entry.Data!.Size:X})");
        }
    }
}
