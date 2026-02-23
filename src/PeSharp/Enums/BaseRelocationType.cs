namespace PeSharp;

public enum BaseRelocationType : byte
{
    Absolute = 0,
    High = 1,
    Low = 2,
    HighLow = 3,
    HighAdj = 4,
    MipsJmpAddr = 5,
    ArmMov32 = 5,
    RiscVHigh20 = 5,
    ThumbMov32 = 7,
    RiscVLow12I = 7,
    RiscVLow12S = 8,
    Dir64 = 10,
}
