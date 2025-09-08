using System;

namespace GameBoy.Core;

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
    /// Function that executes the instruction on the given CPU and returns actual cycles consumed.
    /// </summary>
    public Func<Cpu, int> Execute { get; }

    /// <summary>
    /// Initializes a new instruction.
    /// </summary>
    public Instruction(string mnemonic, int length, int baseCycles, Func<Cpu, int> execute)
    {
        Mnemonic = mnemonic;
        Length = length;
        BaseCycles = baseCycles;
        Execute = execute;
    }
}