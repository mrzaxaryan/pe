using System.Text;

namespace PeSharp.Tests.Helpers;

/// <summary>
/// Builds minimal valid PE byte arrays for unit testing.
/// </summary>
internal sealed class PeBuilder
{
    private bool _is64Bit = true;
    private MachineType _machine = MachineType.Amd64;
    private FileCharacteristics _characteristics = FileCharacteristics.ExecutableImage;
    private Subsystem _subsystem = Subsystem.WindowsCui;
    private readonly List<SectionInfo> _sections = [];
    private int _numDataDirectories = 16;

    private const int FileAlignment = 0x200;
    private const int SectionAlignment = 0x1000;

    public PeBuilder SetIs64Bit(bool is64Bit)
    {
        _is64Bit = is64Bit;
        _machine = is64Bit ? MachineType.Amd64 : MachineType.I386;
        return this;
    }

    public PeBuilder SetMachine(MachineType machine) { _machine = machine; return this; }
    public PeBuilder SetCharacteristics(FileCharacteristics characteristics) { _characteristics = characteristics; return this; }
    public PeBuilder SetSubsystem(Subsystem subsystem) { _subsystem = subsystem; return this; }
    public PeBuilder SetNumDataDirectories(int count) { _numDataDirectories = count; return this; }

    public PeBuilder AddSection(string name, byte[] data, SectionCharacteristics characteristics = SectionCharacteristics.MemRead)
    {
        _sections.Add(new SectionInfo(name, data, characteristics));
        return this;
    }

    public byte[] Build()
    {
        // Layout:
        // 0x00: DOS header (64 bytes)
        // 0x40: PE signature (4 bytes)
        // 0x44: COFF header (20 bytes)
        // 0x58: Optional header (variable)
        // After optional header: section headers (40 bytes each)
        // Aligned to FileAlignment: section data

        int optionalHeaderSize = _is64Bit
            ? 112 + _numDataDirectories * 8  // PE32+
            : 96 + _numDataDirectories * 8;  // PE32

        int headersEnd = 64 + 4 + 20 + optionalHeaderSize + _sections.Count * 40;
        int firstSectionOffset = AlignUp(headersEnd, FileAlignment);

        // Calculate section offsets
        var sectionLayouts = new List<(int fileOffset, uint virtualAddress, int rawSize)>();
        int currentFileOffset = firstSectionOffset;
        uint currentVa = (uint)SectionAlignment; // First section VA

        foreach (var section in _sections)
        {
            int rawSize = AlignUp(section.Data.Length, FileAlignment);
            sectionLayouts.Add((currentFileOffset, currentVa, rawSize));
            currentFileOffset += rawSize;
            currentVa += (uint)AlignUp(section.Data.Length, SectionAlignment);
        }

        int totalSize = currentFileOffset;
        var buffer = new byte[totalSize];
        var writer = new BinaryWriter(new MemoryStream(buffer));

        // DOS header
        writer.Write((ushort)0x5A4D); // e_magic
        writer.BaseStream.Position = 0x3C;
        writer.Write((uint)64);       // e_lfanew -> PE signature at offset 64

        // PE signature
        writer.BaseStream.Position = 64;
        writer.Write((uint)0x00004550); // "PE\0\0"

        // COFF header (20 bytes at offset 68)
        writer.Write((ushort)_machine);
        writer.Write((ushort)_sections.Count);
        writer.Write((uint)0x60000000);  // TimeDateStamp (arbitrary)
        writer.Write((uint)0);           // PointerToSymbolTable
        writer.Write((uint)0);           // NumberOfSymbols
        writer.Write((ushort)optionalHeaderSize);
        writer.Write((ushort)_characteristics);

        // Optional header
        uint sizeOfImage = currentVa;
        uint sizeOfHeaders = (uint)firstSectionOffset;

        if (_is64Bit)
        {
            // PE32+ (magic 0x020B)
            writer.Write((ushort)0x020B);      // Magic
            writer.Write((byte)14);             // MajorLinkerVersion
            writer.Write((byte)0);              // MinorLinkerVersion
            writer.Write((uint)0);              // SizeOfCode
            writer.Write((uint)0);              // SizeOfInitializedData
            writer.Write((uint)0);              // SizeOfUninitializedData
            writer.Write((uint)0x1000);         // AddressOfEntryPoint
            writer.Write((uint)0x1000);         // BaseOfCode
            // No BaseOfData in PE32+
            writer.Write((ulong)0x140000000);   // ImageBase (64-bit)
            writer.Write((uint)SectionAlignment);
            writer.Write((uint)FileAlignment);
            writer.Write((ushort)6);            // MajorOperatingSystemVersion
            writer.Write((ushort)0);            // MinorOperatingSystemVersion
            writer.Write((ushort)0);            // MajorImageVersion
            writer.Write((ushort)0);            // MinorImageVersion
            writer.Write((ushort)6);            // MajorSubsystemVersion
            writer.Write((ushort)0);            // MinorSubsystemVersion
            writer.Write((uint)0);              // Win32VersionValue
            writer.Write(sizeOfImage);
            writer.Write(sizeOfHeaders);
            writer.Write((uint)0);              // CheckSum
            writer.Write((ushort)_subsystem);
            writer.Write((ushort)0x8160);       // DllCharacteristics (ASLR, DEP, etc.)
            writer.Write((ulong)0x100000);      // SizeOfStackReserve
            writer.Write((ulong)0x1000);        // SizeOfStackCommit
            writer.Write((ulong)0x100000);      // SizeOfHeapReserve
            writer.Write((ulong)0x1000);        // SizeOfHeapCommit
            writer.Write((uint)0);              // LoaderFlags
            writer.Write((uint)_numDataDirectories);
        }
        else
        {
            // PE32 (magic 0x010B)
            writer.Write((ushort)0x010B);      // Magic
            writer.Write((byte)14);             // MajorLinkerVersion
            writer.Write((byte)0);              // MinorLinkerVersion
            writer.Write((uint)0);              // SizeOfCode
            writer.Write((uint)0);              // SizeOfInitializedData
            writer.Write((uint)0);              // SizeOfUninitializedData
            writer.Write((uint)0x1000);         // AddressOfEntryPoint
            writer.Write((uint)0x1000);         // BaseOfCode
            writer.Write((uint)0x2000);         // BaseOfData
            writer.Write((uint)0x00400000);     // ImageBase (32-bit)
            writer.Write((uint)SectionAlignment);
            writer.Write((uint)FileAlignment);
            writer.Write((ushort)6);            // MajorOperatingSystemVersion
            writer.Write((ushort)0);            // MinorOperatingSystemVersion
            writer.Write((ushort)0);            // MajorImageVersion
            writer.Write((ushort)0);            // MinorImageVersion
            writer.Write((ushort)6);            // MajorSubsystemVersion
            writer.Write((ushort)0);            // MinorSubsystemVersion
            writer.Write((uint)0);              // Win32VersionValue
            writer.Write(sizeOfImage);
            writer.Write(sizeOfHeaders);
            writer.Write((uint)0);              // CheckSum
            writer.Write((ushort)_subsystem);
            writer.Write((ushort)0x8160);       // DllCharacteristics
            writer.Write((uint)0x100000);       // SizeOfStackReserve
            writer.Write((uint)0x1000);         // SizeOfStackCommit
            writer.Write((uint)0x100000);       // SizeOfHeapReserve
            writer.Write((uint)0x1000);         // SizeOfHeapCommit
            writer.Write((uint)0);              // LoaderFlags
            writer.Write((uint)_numDataDirectories);
        }

        // Data directories (all zeros by default)
        for (int i = 0; i < _numDataDirectories; i++)
        {
            writer.Write((uint)0); // VirtualAddress
            writer.Write((uint)0); // Size
        }

        // Section headers
        for (int i = 0; i < _sections.Count; i++)
        {
            var section = _sections[i];
            var layout = sectionLayouts[i];
            byte[] nameBytes = new byte[8];
            Encoding.ASCII.GetBytes(section.Name, 0, Math.Min(section.Name.Length, 8), nameBytes, 0);
            writer.Write(nameBytes);
            writer.Write((uint)section.Data.Length);     // VirtualSize
            writer.Write(layout.virtualAddress);          // VirtualAddress
            writer.Write((uint)layout.rawSize);           // SizeOfRawData
            writer.Write((uint)layout.fileOffset);        // PointerToRawData
            writer.Write((uint)0);                        // PointerToRelocations
            writer.Write((uint)0);                        // PointerToLinenumbers
            writer.Write((ushort)0);                      // NumberOfRelocations
            writer.Write((ushort)0);                      // NumberOfLinenumbers
            writer.Write((uint)section.Characteristics);
        }

        // Section data
        for (int i = 0; i < _sections.Count; i++)
        {
            Array.Copy(_sections[i].Data, 0, buffer, sectionLayouts[i].fileOffset, _sections[i].Data.Length);
        }

        return buffer;
    }

    /// <summary>
    /// Builds a PE with a data directory entry pointing into a section.
    /// Returns the byte array and the file offset where the section data starts.
    /// </summary>
    public (byte[] data, uint sectionVa, int sectionFileOffset) BuildWithDataDirectory(
        DataDirectoryIndex dirIndex, byte[] directoryData, uint directoryOffsetInSection = 0)
    {
        // Put the directory data in a section
        int totalSectionData = (int)directoryOffsetInSection + directoryData.Length;
        byte[] sectionData = new byte[totalSectionData];
        Array.Copy(directoryData, 0, sectionData, (int)directoryOffsetInSection, directoryData.Length);

        _sections.Add(new SectionInfo(".data", sectionData, SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData));

        int sectionIndex = _sections.Count - 1;

        // Calculate layout to determine VA
        int optionalHeaderSize = _is64Bit
            ? 112 + _numDataDirectories * 8
            : 96 + _numDataDirectories * 8;

        int headersEnd = 64 + 4 + 20 + optionalHeaderSize + _sections.Count * 40;
        int firstSectionOffset = AlignUp(headersEnd, FileAlignment);

        int currentFileOffset = firstSectionOffset;
        uint currentVa = (uint)SectionAlignment;
        int targetFileOffset = 0;
        uint targetVa = 0;

        for (int i = 0; i < _sections.Count; i++)
        {
            int rawSize = AlignUp(_sections[i].Data.Length, FileAlignment);
            if (i == sectionIndex)
            {
                targetFileOffset = currentFileOffset;
                targetVa = currentVa;
            }
            currentFileOffset += rawSize;
            currentVa += (uint)AlignUp(_sections[i].Data.Length, SectionAlignment);
        }

        // Now build the PE but patch the data directory
        byte[] peData = Build();

        // Patch the data directory entry
        int optHeaderOffset = 64 + 4 + 20; // DOS + PE sig + COFF
        int dataDirOffset = optHeaderOffset + (_is64Bit ? 112 : 96) + (int)dirIndex * 8;
        BitConverter.GetBytes(targetVa + directoryOffsetInSection).CopyTo(peData, dataDirOffset);
        BitConverter.GetBytes((uint)directoryData.Length).CopyTo(peData, dataDirOffset + 4);

        return (peData, targetVa, targetFileOffset);
    }

    // Convenience factory methods

    public static byte[] MinimalPe32Plus()
    {
        return new PeBuilder()
            .SetIs64Bit(true)
            .AddSection(".text", new byte[16], SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode)
            .Build();
    }

    public static byte[] MinimalPe32()
    {
        return new PeBuilder()
            .SetIs64Bit(false)
            .AddSection(".text", new byte[16], SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode)
            .Build();
    }

    public static byte[] MinimalDll64()
    {
        return new PeBuilder()
            .SetIs64Bit(true)
            .SetCharacteristics(FileCharacteristics.ExecutableImage | FileCharacteristics.Dll)
            .AddSection(".text", new byte[16], SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode)
            .Build();
    }

    public static byte[] MinimalDll32()
    {
        return new PeBuilder()
            .SetIs64Bit(false)
            .SetCharacteristics(FileCharacteristics.ExecutableImage | FileCharacteristics.Dll)
            .AddSection(".text", new byte[16], SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode)
            .Build();
    }

    private static int AlignUp(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private record SectionInfo(string Name, byte[] Data, SectionCharacteristics Characteristics);
}
