using System;

namespace GameBoy.Core;

/// <summary>
/// Represents operand types for Game Boy CPU instructions.
/// </summary>
public enum OperandType
{
    None,       // No operand (e.g., NOP)
    Register,   // Single register (e.g., LD A,B)
    Immediate8, // 8-bit immediate value (e.g., LD A,d8)
    Immediate16,// 16-bit immediate value (e.g., LD BC,d16)
    Memory,     // Memory address (e.g., LD A,(HL))
    MemoryImmediate8,  // Memory at immediate 8-bit offset (e.g., LDH A,(a8))
    MemoryImmediate16, // Memory at immediate 16-bit address (e.g., LD A,(a16))
    Relative8   // 8-bit relative address (e.g., JR r8)
}

/// <summary>
/// Represents a single CPU instruction with its metadata and execution logic.
/// </summary>
public readonly struct Instruction
{
    /// <summary>
    /// Assembly mnemonic for the instruction (e.g., "LD A,d8").
    /// </summary>
    public string Mnemonic { get; }

    /// <summary>
    /// Instruction length in bytes (1, 2, or 3).
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Base cycle count for the instruction.
    /// </summary>
    public int BaseCycles { get; }

    /// <summary>
    /// Primary operand type for this instruction.
    /// </summary>
    public OperandType OperandType { get; }

    /// <summary>
    /// Function that executes the instruction on the given CPU and returns actual cycles consumed.
    /// </summary>
    public Func<Cpu, int> Execute { get; }

    /// <summary>
    /// Initializes a new instruction.
    /// </summary>
    public Instruction(string mnemonic, int length, int baseCycles, OperandType operandType, Func<Cpu, int> execute)
    {
        Mnemonic = mnemonic;
        Length = length;
        BaseCycles = baseCycles;
        OperandType = operandType;
        Execute = execute;
    }

    /// <summary>
    /// Initializes a new instruction with default operand type detection.
    /// </summary>
    public Instruction(string mnemonic, int length, int baseCycles, Func<Cpu, int> execute)
    {
        Mnemonic = mnemonic;
        Length = length;
        BaseCycles = baseCycles;
        OperandType = DeduceOperandType(mnemonic, length);
        Execute = execute;
    }

    /// <summary>
    /// Deduces operand type from mnemonic and length for backward compatibility.
    /// </summary>
    private static OperandType DeduceOperandType(string mnemonic, int length)
    {
        if (length == 1)
        {
            if (mnemonic.Contains("(HL)") || mnemonic.Contains("(BC)") || mnemonic.Contains("(DE)"))
                return OperandType.Memory;
            if (mnemonic == "NOP" || mnemonic.StartsWith("HALT") || mnemonic.StartsWith("EI") || mnemonic.StartsWith("DI"))
                return OperandType.None;
            return OperandType.Register;
        }
        else if (length == 2)
        {
            if (mnemonic.Contains("d8") || mnemonic.Contains("r8"))
                return OperandType.Immediate8;
            if (mnemonic.Contains("(a8)"))
                return OperandType.MemoryImmediate8;
            return OperandType.Immediate8;
        }
        else if (length == 3)
        {
            if (mnemonic.Contains("(a16)"))
                return OperandType.MemoryImmediate16;
            return OperandType.Immediate16;
        }
        return OperandType.None;
    }
}