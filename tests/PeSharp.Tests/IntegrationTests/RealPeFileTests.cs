namespace PeSharp.Tests.IntegrationTests;

[Trait("Category", "Integration")]
public class RealPeFileTests
{
    private const string Kernel32Path = @"C:\Windows\System32\kernel32.dll";
    private const string NotepadPath = @"C:\Windows\System32\notepad.exe";
    private const string Kernel32Wow64Path = @"C:\Windows\SysWOW64\kernel32.dll";

    [Fact]
    public void Kernel32_Is64BitDll()
    {
        if (!File.Exists(Kernel32Path)) return;

        using var pe = new PeFile(Kernel32Path);
        Assert.True(pe.Is64Bit);
        Assert.True(pe.IsDll);
    }

    [Fact]
    public void Kernel32_HasExports()
    {
        if (!File.Exists(Kernel32Path)) return;

        using var pe = new PeFile(Kernel32Path);
        Assert.NotNull(pe.Exports);
        Assert.True(pe.Exports!.Functions.Count > 0);

        var loadLibrary = pe.Exports.Functions.FirstOrDefault(f => f.Name == "LoadLibraryA");
        Assert.NotNull(loadLibrary);
    }

    [Fact]
    public void Kernel32_HasImports()
    {
        if (!File.Exists(Kernel32Path)) return;

        using var pe = new PeFile(Kernel32Path);
        Assert.NotNull(pe.Imports);
        Assert.True(pe.Imports!.Modules.Count > 0);

        var ntdll = pe.Imports.Modules.FirstOrDefault(m =>
            m.Name.Equals("ntdll.dll", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(ntdll);
    }

    [Fact]
    public void Notepad_Is64BitExe()
    {
        if (!File.Exists(NotepadPath)) return;

        using var pe = new PeFile(NotepadPath);
        Assert.True(pe.Is64Bit);
        Assert.False(pe.IsDll);
    }

    [Fact]
    public void Notepad_HasResources()
    {
        if (!File.Exists(NotepadPath)) return;

        using var pe = new PeFile(NotepadPath);
        Assert.NotNull(pe.Resources);
        Assert.True(pe.Resources!.Entries.Count > 0);
    }

    [Fact]
    public void Notepad_HasDebugInfo()
    {
        if (!File.Exists(NotepadPath)) return;

        using var pe = new PeFile(NotepadPath);
        Assert.NotNull(pe.DebugInfo);
        Assert.True(pe.DebugInfo!.Entries.Count > 0);
    }

    [Fact]
    public void Notepad_SectionHeaders_ContainText()
    {
        if (!File.Exists(NotepadPath)) return;

        using var pe = new PeFile(NotepadPath);
        Assert.Contains(pe.SectionHeaders, s => s.Name == ".text");
    }

    [Fact]
    public void Notepad_GetSectionData_TextSection_ReturnsNonEmpty()
    {
        if (!File.Exists(NotepadPath)) return;

        using var pe = new PeFile(NotepadPath);
        var data = pe.GetSectionData(".text");
        Assert.True(data.Length > 0);
    }

    [Fact]
    public void AllConstructors_ProduceSameResults_RealFile()
    {
        if (!File.Exists(Kernel32Path)) return;

        var bytes = File.ReadAllBytes(Kernel32Path);

        using var fromPath = new PeFile(Kernel32Path);
        using var fromBytes = new PeFile(bytes);
        using var fromStream = new PeFile(new MemoryStream(bytes));

        Assert.Equal(fromPath.DosHeader, fromBytes.DosHeader);
        Assert.Equal(fromPath.DosHeader, fromStream.DosHeader);
        Assert.Equal(fromPath.CoffHeader, fromBytes.CoffHeader);
        Assert.Equal(fromPath.Is64Bit, fromBytes.Is64Bit);
        Assert.Equal(fromPath.IsDll, fromStream.IsDll);
    }

    [Fact]
    public void SysWow64_Kernel32_Is32Bit()
    {
        if (!File.Exists(Kernel32Wow64Path)) return;

        using var pe = new PeFile(Kernel32Wow64Path);
        Assert.False(pe.Is64Bit);
        Assert.True(pe.IsDll);
    }
}
