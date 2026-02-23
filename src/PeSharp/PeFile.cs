using PeSharp.DataDirectories;
using PeSharp.Headers;

namespace PeSharp;

public sealed class PeFile : IDisposable
{
    private readonly PeReader _reader;
    private readonly RvaResolver _rvaResolver;

    private readonly Lazy<ExportDirectory?> _exports;
    private readonly Lazy<ImportDirectory?> _imports;
    private readonly Lazy<ResourceDirectory?> _resources;
    private readonly Lazy<BaseRelocation?> _relocations;
    private readonly Lazy<DebugDirectory?> _debugInfo;

    public DosHeader DosHeader { get; }
    public CoffHeader CoffHeader { get; }
    public OptionalHeader OptionalHeader { get; }
    public IReadOnlyList<SectionHeader> SectionHeaders { get; }

    public bool Is64Bit => OptionalHeader.IsPe32Plus;
    public bool IsDll => CoffHeader.IsDll;

    public ExportDirectory? Exports => _exports.Value;
    public ImportDirectory? Imports => _imports.Value;
    public ResourceDirectory? Resources => _resources.Value;
    public BaseRelocation? Relocations => _relocations.Value;
    public DebugDirectory? DebugInfo => _debugInfo.Value;

    public PeFile(string filePath) : this(new PeReader(filePath)) { }
    public PeFile(byte[] data) : this(new PeReader(data)) { }
    public PeFile(Stream stream) : this(new PeReader(stream)) { }

    private PeFile(PeReader reader)
    {
        _reader = reader;

        // Parse DOS header
        DosHeader = DosHeader.Parse(_reader);

        // Validate PE signature
        int peOffset = DosHeader.PeHeaderOffset;
        if (peOffset < 0 || peOffset + 4 > _reader.Length)
            throw new PeTruncatedException("PE header offset is beyond end of file.");

        uint peSignature = _reader.ReadUInt32(peOffset);
        if (peSignature != 0x00004550) // "PE\0\0"
            throw new InvalidPeSignatureException($"Invalid PE signature: 0x{peSignature:X8} (expected 0x00004550).");

        // Parse COFF header (immediately after PE signature)
        int coffOffset = peOffset + 4;
        CoffHeader = CoffHeader.Parse(_reader, coffOffset);

        // Parse Optional header (immediately after COFF header)
        int optionalOffset = coffOffset + CoffHeader.Size;
        OptionalHeader = OptionalHeader.Parse(_reader, optionalOffset);

        // Parse Section headers (immediately after Optional header)
        int sectionsOffset = optionalOffset + CoffHeader.SizeOfOptionalHeader;
        SectionHeaders = SectionHeader.ParseAll(_reader, sectionsOffset, CoffHeader.NumberOfSections);

        _rvaResolver = new RvaResolver(SectionHeaders);

        // Set up lazy data directory parsing
        _exports = new Lazy<ExportDirectory?>(() => ParseDataDirectory(
            DataDirectoryIndex.ExportTable,
            (r, res, dir) => ExportDirectory.Parse(r, res, dir)));

        _imports = new Lazy<ImportDirectory?>(() => ParseDataDirectory(
            DataDirectoryIndex.ImportTable,
            (r, res, dir) => ImportDirectory.Parse(r, res, dir, Is64Bit)));

        _resources = new Lazy<ResourceDirectory?>(() => ParseDataDirectory(
            DataDirectoryIndex.ResourceTable,
            (r, res, dir) => ResourceDirectory.Parse(r, res, dir)));

        _relocations = new Lazy<BaseRelocation?>(() => ParseDataDirectory(
            DataDirectoryIndex.BaseRelocationTable,
            (r, res, dir) => BaseRelocation.Parse(r, res, dir)));

        _debugInfo = new Lazy<DebugDirectory?>(() => ParseDataDirectory(
            DataDirectoryIndex.Debug,
            (r, res, dir) => DebugDirectory.Parse(r, res, dir)));
    }

    public uint? ResolveRva(uint rva) => _rvaResolver.ResolveRva(rva);

    public ReadOnlySpan<byte> GetSectionData(string sectionName)
    {
        foreach (var section in SectionHeaders)
        {
            if (string.Equals(section.Name, sectionName, StringComparison.Ordinal))
                return GetSectionDataCore(section);
        }
        throw new ArgumentException($"Section '{sectionName}' not found.", nameof(sectionName));
    }

    public ReadOnlySpan<byte> GetSectionData(int index)
    {
        if (index < 0 || index >= SectionHeaders.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return GetSectionDataCore(SectionHeaders[index]);
    }

    private ReadOnlySpan<byte> GetSectionDataCore(SectionHeader section)
    {
        if (section.SizeOfRawData == 0 || section.PointerToRawData == 0)
            return ReadOnlySpan<byte>.Empty;
        return _reader.GetSpan((int)section.PointerToRawData, (int)section.SizeOfRawData);
    }

    private T? ParseDataDirectory<T>(DataDirectoryIndex index, Func<PeReader, RvaResolver, DataDirectory, T?> parser) where T : class
    {
        int i = (int)index;
        if (i >= OptionalHeader.DataDirectories.Count)
            return null;

        var dir = OptionalHeader.DataDirectories[i];
        if (dir.IsEmpty)
            return null;

        try
        {
            return parser(_reader, _rvaResolver, dir);
        }
        catch (PeTruncatedException)
        {
            return null;
        }
    }

    public void Dispose() => _reader.Dispose();
}
