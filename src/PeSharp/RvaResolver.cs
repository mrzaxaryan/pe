using PeSharp.Headers;

namespace PeSharp;

internal sealed class RvaResolver
{
    private readonly IReadOnlyList<SectionHeader> _sections;

    public RvaResolver(IReadOnlyList<SectionHeader> sections) => _sections = sections;

    public uint? ResolveRva(uint rva)
    {
        foreach (var section in _sections)
        {
            if (section.ContainsRva(rva))
            {
                return rva - section.VirtualAddress + section.PointerToRawData;
            }
        }
        return null;
    }
}
