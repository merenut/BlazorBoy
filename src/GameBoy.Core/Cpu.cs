using System;

namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy CPU (Sharp LR35902) and its execution state.
/// </summary>
public sealed class Cpu
{
    /// <summary>
    /// CPU 8/16-bit register file.
    /// </summary>
    public struct Registers
    {
        public byte A;
        public byte F; // Flags: Z N H C in high nibble
        public byte B;
        public byte C;
        public byte D;
        public byte E;
        public byte H;
        public byte L;
        public ushort SP;
        public ushort PC;

        public ushort AF
        {
            readonly get => (ushort)((A << 8) | F);
            set { A = (byte)(value >> 8); F = (byte)(value & 0xF0); }
        }

        public ushort BC
        {
            readonly get => (ushort)((B << 8) | C);
            set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
        }

        public ushort DE
        {
            readonly get => (ushort)((D << 8) | E);
            set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
        }

        public ushort HL
        {
            readonly get => (ushort)((H << 8) | L);
            set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
        }
    }

    internal readonly Mmu _mmu;

    /// <summary>
    /// The registers for this CPU instance.
    /// </summary>
    public Registers Regs;

    /// <summary>
    /// Indicates whether interrupts are enabled.
    /// </summary>
    public bool InterruptsEnabled { get; set; }

    /// <summary>
    /// Initializes the CPU with the given memory management unit.
    /// </summary>
    public Cpu(Mmu mmu)
    {
        _mmu = mmu;
        Reset();
    }

    /// <summary>
    /// Resets CPU registers to power-on defaults.
    /// </summary>
    public void Reset()
    {
        Regs = default;
        // Common post-BIOS state is not set here; assume BIOS ran.
        Regs.SP = 0xFFFE;
        Regs.PC = 0x0100;
        Regs.AF = 0x01B0; // DMG assumed; flags masked to upper nibble of F
        Regs.BC = 0x0013;
        Regs.DE = 0x00D8;
        Regs.HL = 0x014D;
        InterruptsEnabled = true;
    }

    /// <summary>
    /// Executes a single instruction and returns consumed cycles.
    /// </summary>
    public int Step()
    {
        byte opcode = _mmu.ReadByte(Regs.PC++);

        // Handle CB-prefixed instructions
        if (opcode == 0xCB)
        {
            byte cbOpcode = _mmu.ReadByte(Regs.PC++);
            var instruction = OpcodeTable.CB[cbOpcode];
            if (instruction.HasValue)
            {
                return instruction.Value.Execute(this);
            }
            // Unknown CB instruction - treat as NOP for now
            return 8;
        }

        // Handle primary instruction table
        var primaryInstruction = OpcodeTable.Primary[opcode];
        if (primaryInstruction.HasValue)
        {
            return primaryInstruction.Value.Execute(this);
        }

        // Unknown instruction - treat as NOP for now  
        return 4;
    }

    /// <summary>
    /// Reads an immediate 8-bit value from memory at PC and advances PC.
    /// </summary>
    internal byte ReadImm8()
    {
        return _mmu.ReadByte(Regs.PC++);
    }

    /// <summary>
    /// Reads an immediate 16-bit value from memory at PC and advances PC by 2.
    /// </summary>
    internal ushort ReadImm16()
    {
        byte low = _mmu.ReadByte(Regs.PC++);
        byte high = _mmu.ReadByte(Regs.PC++);
        return (ushort)(low | (high << 8));
    }

    /// <summary>
    /// Adds a value to register A and sets flags accordingly.
    /// </summary>
    internal void AddToA(byte value)
    {
        var result = Alu.Add(Regs.A, value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Adds a value plus carry to register A and sets flags accordingly.
    /// </summary>
    internal void AdcToA(byte value)
    {
        bool carryIn = (Regs.F & 0x10) != 0; // Extract carry flag
        var result = Alu.AddWithCarry(Regs.A, value, carryIn);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Subtracts a value from register A and sets flags accordingly.
    /// </summary>
    internal void SubFromA(byte value)
    {
        var result = Alu.Subtract(Regs.A, value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Subtracts a value and carry from register A and sets flags accordingly.
    /// </summary>
    internal void SbcFromA(byte value)
    {
        bool carryIn = (Regs.F & 0x10) != 0; // Extract carry flag
        var result = Alu.SubtractWithCarry(Regs.A, value, carryIn);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Performs bitwise AND with register A and sets flags accordingly.
    /// </summary>
    internal void AndWithA(byte value)
    {
        var result = Alu.And(Regs.A, value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Performs bitwise OR with register A and sets flags accordingly.
    /// </summary>
    internal void OrWithA(byte value)
    {
        var result = Alu.Or(Regs.A, value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Performs bitwise XOR with register A and sets flags accordingly.
    /// </summary>
    internal void XorWithA(byte value)
    {
        var result = Alu.Xor(Regs.A, value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Compares register A with a value and sets flags accordingly (CP operation).
    /// </summary>
    internal void CompareA(byte value)
    {
        var result = Alu.Compare(Regs.A, value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        // Note: A register is not modified in CP operation
    }

    /// <summary>
    /// Sets CPU flags in the F register.
    /// </summary>
    internal void SetFlags(bool zero, bool negative, bool halfCarry, bool carry)
    {
        Regs.F = (byte)(
            (zero ? 0x80 : 0) |
            (negative ? 0x40 : 0) |
            (halfCarry ? 0x20 : 0) |
            (carry ? 0x10 : 0)
        );
    }
}
