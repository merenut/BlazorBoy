using System;

namespace GameBoy.Core.Debug;

/// <summary>
/// Represents a single entry in the trace log.
/// </summary>
public readonly struct TraceEntry
{
    public ushort PC { get; }
    public byte Opcode { get; }
    public ushort AF { get; }
    public ushort BC { get; }
    public ushort DE { get; }
    public ushort HL { get; }
    public ushort SP { get; }
    public DateTime Timestamp { get; }
    public ulong CycleCount { get; }

    public TraceEntry(ushort pc, byte opcode, Cpu.Registers registers, ulong cycleCount)
    {
        PC = pc;
        Opcode = opcode;
        AF = registers.AF;
        BC = registers.BC;
        DE = registers.DE;
        HL = registers.HL;
        SP = registers.SP;
        Timestamp = DateTime.UtcNow;
        CycleCount = cycleCount;
    }

    public override string ToString()
    {
        return $"{CycleCount:000000000} PC:{PC:X4} OP:{Opcode:X2} AF:{AF:X4} BC:{BC:X4} DE:{DE:X4} HL:{HL:X4} SP:{SP:X4}";
    }
}