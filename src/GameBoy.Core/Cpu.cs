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

    #region Register Addressing Helpers

    /// <summary>
    /// Gets an 8-bit register value by index (0=B, 1=C, 2=D, 3=E, 4=H, 5=L, 6=reserved, 7=A).
    /// </summary>
    public byte GetR8(int regIndex)
    {
        return regIndex switch
        {
            0 => Regs.B,
            1 => Regs.C,
            2 => Regs.D,
            3 => Regs.E,
            4 => Regs.H,
            5 => Regs.L,
            6 => throw new InvalidOperationException("Register index 6 is reserved for (HL) addressing"),
            7 => Regs.A,
            _ => throw new ArgumentOutOfRangeException(nameof(regIndex), "Register index must be 0-7")
        };
    }

    /// <summary>
    /// Sets an 8-bit register value by index (0=B, 1=C, 2=D, 3=E, 4=H, 5=L, 6=reserved, 7=A).
    /// </summary>
    public void SetR8(int regIndex, byte value)
    {
        switch (regIndex)
        {
            case 0: Regs.B = value; break;
            case 1: Regs.C = value; break;
            case 2: Regs.D = value; break;
            case 3: Regs.E = value; break;
            case 4: Regs.H = value; break;
            case 5: Regs.L = value; break;
            case 6: throw new InvalidOperationException("Register index 6 is reserved for (HL) addressing");
            case 7: Regs.A = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(regIndex), "Register index must be 0-7");
        }
    }

    /// <summary>
    /// Gets a 16-bit register pair value by index (0=BC, 1=DE, 2=HL, 3=SP).
    /// </summary>
    public ushort GetR16(int regIndex)
    {
        return regIndex switch
        {
            0 => Regs.BC,
            1 => Regs.DE,
            2 => Regs.HL,
            3 => Regs.SP,
            _ => throw new ArgumentOutOfRangeException(nameof(regIndex), "Register pair index must be 0-3")
        };
    }

    /// <summary>
    /// Sets a 16-bit register pair value by index (0=BC, 1=DE, 2=HL, 3=SP).
    /// </summary>
    public void SetR16(int regIndex, ushort value)
    {
        switch (regIndex)
        {
            case 0: Regs.BC = value; break;
            case 1: Regs.DE = value; break;
            case 2: Regs.HL = value; break;
            case 3: Regs.SP = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(regIndex), "Register pair index must be 0-3");
        }
    }

    #endregion

    #region Memory Addressing Helpers

    /// <summary>
    /// Reads a byte from memory at address in HL register.
    /// </summary>
    public byte ReadHL()
    {
        return _mmu.ReadByte(Regs.HL);
    }

    /// <summary>
    /// Writes a byte to memory at address in HL register.
    /// </summary>
    public void WriteHL(byte value)
    {
        _mmu.WriteByte(Regs.HL, value);
    }

    /// <summary>
    /// Reads a byte from memory at address in HL register, then increments HL.
    /// </summary>
    public byte ReadHLI()
    {
        byte value = _mmu.ReadByte(Regs.HL);
        Regs.HL++;
        return value;
    }

    /// <summary>
    /// Writes a byte to memory at address in HL register, then increments HL.
    /// </summary>
    public void WriteHLI(byte value)
    {
        _mmu.WriteByte(Regs.HL, value);
        Regs.HL++;
    }

    /// <summary>
    /// Reads a byte from memory at address in HL register, then decrements HL.
    /// </summary>
    public byte ReadHLD()
    {
        byte value = _mmu.ReadByte(Regs.HL);
        Regs.HL--;
        return value;
    }

    /// <summary>
    /// Writes a byte to memory at address in HL register, then decrements HL.
    /// </summary>
    public void WriteHLD(byte value)
    {
        _mmu.WriteByte(Regs.HL, value);
        Regs.HL--;
    }

    /// <summary>
    /// Reads a byte from memory at address in BC register.
    /// </summary>
    public byte ReadBC()
    {
        return _mmu.ReadByte(Regs.BC);
    }

    /// <summary>
    /// Writes a byte to memory at address in BC register.
    /// </summary>
    public void WriteBC(byte value)
    {
        _mmu.WriteByte(Regs.BC, value);
    }

    /// <summary>
    /// Reads a byte from memory at address in DE register.
    /// </summary>
    public byte ReadDE()
    {
        return _mmu.ReadByte(Regs.DE);
    }

    /// <summary>
    /// Writes a byte to memory at address in DE register.
    /// </summary>
    public void WriteDE(byte value)
    {
        _mmu.WriteByte(Regs.DE, value);
    }

    /// <summary>
    /// Reads a byte from high RAM at address 0xFF00 + C register.
    /// </summary>
    public byte ReadHighC()
    {
        return _mmu.ReadByte((ushort)(0xFF00 + Regs.C));
    }

    /// <summary>
    /// Writes a byte to high RAM at address 0xFF00 + C register.
    /// </summary>
    public void WriteHighC(byte value)
    {
        _mmu.WriteByte((ushort)(0xFF00 + Regs.C), value);
    }

    /// <summary>
    /// Reads a byte from high RAM at address 0xFF00 + immediate 8-bit value.
    /// </summary>
    public byte ReadHighImm8()
    {
        byte offset = ReadImm8();
        return _mmu.ReadByte((ushort)(0xFF00 + offset));
    }

    /// <summary>
    /// Writes a byte to high RAM at address 0xFF00 + immediate 8-bit value.
    /// </summary>
    public void WriteHighImm8(byte value)
    {
        byte offset = ReadImm8();
        _mmu.WriteByte((ushort)(0xFF00 + offset), value);
    }

    /// <summary>
    /// Reads a byte from memory at immediate 16-bit address.
    /// </summary>
    public byte ReadImm16Addr()
    {
        ushort addr = ReadImm16();
        return _mmu.ReadByte(addr);
    }

    /// <summary>
    /// Writes a byte to memory at immediate 16-bit address.
    /// </summary>
    public void WriteImm16Addr(byte value)
    {
        ushort addr = ReadImm16();
        _mmu.WriteByte(addr, value);
    }

    #endregion

    #region Additional CPU Operations

    /// <summary>
    /// Increments an 8-bit register and sets flags.
    /// </summary>
    internal void IncReg8(ref byte register)
    {
        var result = Alu.Inc8(register);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, GetCarryFlag());
        register = result.Result;
    }

    /// <summary>
    /// Decrements an 8-bit register and sets flags.
    /// </summary>
    internal void DecReg8(ref byte register)
    {
        var result = Alu.Dec8(register);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, GetCarryFlag());
        register = result.Result;
    }

    /// <summary>
    /// Performs a relative jump by reading an 8-bit signed offset.
    /// </summary>
    internal void JumpRelative()
    {
        sbyte offset = (sbyte)ReadImm8();
        Regs.PC = (ushort)(Regs.PC + offset);
    }

    /// <summary>
    /// Pushes a 16-bit value onto the stack.
    /// </summary>
    internal void PushStack(ushort value)
    {
        Regs.SP -= 2;
        _mmu.WriteWord(Regs.SP, value);
    }

    /// <summary>
    /// Pops a 16-bit value from the stack.
    /// </summary>
    internal ushort PopStack()
    {
        ushort value = _mmu.ReadWord(Regs.SP);
        Regs.SP += 2;
        return value;
    }

    /// <summary>
    /// Sets or clears the halted state of the CPU.
    /// </summary>
    internal void SetHalted(bool halted)
    {
        // For now, just a placeholder implementation
        // In a full implementation, this would control CPU execution state
    }

    /// <summary>
    /// Gets the value of the Zero flag.
    /// </summary>
    public bool GetZeroFlag()
    {
        return (Regs.F & 0x80) != 0;
    }

    /// <summary>
    /// Gets the value of the Carry flag.
    /// </summary>
    public bool GetCarryFlag()
    {
        return (Regs.F & 0x10) != 0;
    }

    /// <summary>
    /// Rotates a byte left circularly and sets flags.
    /// </summary>
    internal byte RotateLeftCircular(byte value)
    {
        var result = Alu.RotateLeftCircular(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Tests a specific bit in a byte and sets the Zero flag accordingly.
    /// </summary>
    internal void TestBit(byte value, int bitIndex)
    {
        bool bitSet = (value & (1 << bitIndex)) != 0;
        SetFlags(!bitSet, true, true, GetCarryFlag()); // Z=!bit, N=1, H=1, C=unchanged
    }

    /// <summary>
    /// Sets a specific bit in a byte.
    /// </summary>
    internal byte SetBit(byte value, int bitIndex)
    {
        return (byte)(value | (1 << bitIndex));
    }

    /// <summary>
    /// Resets (clears) a specific bit in a byte.
    /// </summary>
    internal byte ResetBit(byte value, int bitIndex)
    {
        return (byte)(value & ~(1 << bitIndex));
    }

    /// <summary>
    /// Shifts a byte right logically and sets flags.
    /// </summary>
    internal byte ShiftRightLogical(byte value)
    {
        var result = Alu.ShiftRightLogical(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Gets an 8-bit register by index (0=B, 1=C, 2=D, 3=E, 4=H, 5=L, 6=(HL), 7=A).
    /// </summary>
    internal byte GetReg8(int index)
    {
        return index switch
        {
            0 => Regs.B,
            1 => Regs.C,
            2 => Regs.D,
            3 => Regs.E,
            4 => Regs.H,
            5 => Regs.L,
            6 => ReadHL(),
            7 => Regs.A,
            _ => throw new ArgumentOutOfRangeException(nameof(index), "Register index must be 0-7")
        };
    }

    /// <summary>
    /// Sets an 8-bit register by index (0=B, 1=C, 2=D, 3=E, 4=H, 5=L, 6=(HL), 7=A).
    /// </summary>
    internal void SetReg8(int index, byte value)
    {
        switch (index)
        {
            case 0: Regs.B = value; break;
            case 1: Regs.C = value; break;
            case 2: Regs.D = value; break;
            case 3: Regs.E = value; break;
            case 4: Regs.H = value; break;
            case 5: Regs.L = value; break;
            case 6: WriteHL(value); break;
            case 7: Regs.A = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(index), "Register index must be 0-7");
        }
    }

    /// <summary>
    /// Gets a reference to an 8-bit register by index for direct manipulation (not valid for (HL)).
    /// </summary>
    internal ref byte GetReg8Ref(int index)
    {
        switch (index)
        {
            case 0: return ref Regs.B;
            case 1: return ref Regs.C;
            case 2: return ref Regs.D;
            case 3: return ref Regs.E;
            case 4: return ref Regs.H;
            case 5: return ref Regs.L;
            case 7: return ref Regs.A;
            default: throw new ArgumentOutOfRangeException(nameof(index), "Register index must be 0-5 or 7 for direct references");
        }
    }

    /// <summary>
    /// Performs DAA operation on A register.
    /// </summary>
    internal void DecimalAdjustA()
    {
        bool negative = (Regs.F & 0x40) != 0;
        bool halfCarry = (Regs.F & 0x20) != 0;
        bool carry = GetCarryFlag();

        var result = Alu.DecimalAdjust(Regs.A, negative, halfCarry, carry);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Complements A register (bitwise NOT).
    /// </summary>
    internal void ComplementA()
    {
        Regs.A = (byte)~Regs.A;
        // CPL sets N and H flags, leaves Z and C unchanged
        SetFlags(GetZeroFlag(), true, true, GetCarryFlag());
    }

    /// <summary>
    /// Sets the carry flag.
    /// </summary>
    internal void SetCarryFlag()
    {
        // SCF sets C, resets N and H, leaves Z unchanged
        SetFlags(GetZeroFlag(), false, false, true);
    }

    /// <summary>
    /// Complements the carry flag.
    /// </summary>
    internal void ComplementCarryFlag()
    {
        // CCF complements C, resets N and H, leaves Z unchanged
        SetFlags(GetZeroFlag(), false, false, !GetCarryFlag());
    }

    /// <summary>
    /// Performs rotate/shift operations on a register or memory location.
    /// </summary>
    internal byte PerformRotateShift(int operation, int regIndex)
    {
        byte value = GetReg8(regIndex);
        byte result = operation switch
        {
            0 => RotateLeftCircular(value),     // RLC
            1 => RotateRightCircular(value),    // RRC
            2 => RotateLeft(value),             // RL
            3 => RotateRight(value),            // RR
            4 => ShiftLeftArithmetic(value),    // SLA
            5 => ShiftRightArithmetic(value),   // SRA
            6 => SwapNibbles(value),            // SWAP
            7 => ShiftRightLogical(value),      // SRL
            _ => throw new ArgumentOutOfRangeException(nameof(operation), "Invalid rotate/shift operation")
        };
        SetReg8(regIndex, result);
        return result;
    }

    /// <summary>
    /// Rotates a byte right circularly and sets flags.
    /// </summary>
    internal byte RotateRightCircular(byte value)
    {
        var result = Alu.RotateRightCircular(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Rotates a byte left through carry and sets flags.
    /// </summary>
    internal byte RotateLeft(byte value)
    {
        var result = Alu.RotateLeft(value, GetCarryFlag());
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Rotates a byte right through carry and sets flags.
    /// </summary>
    internal byte RotateRight(byte value)
    {
        var result = Alu.RotateRight(value, GetCarryFlag());
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Shifts a byte left arithmetically and sets flags.
    /// </summary>
    internal byte ShiftLeftArithmetic(byte value)
    {
        var result = Alu.ShiftLeftArithmetic(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Shifts a byte right arithmetically and sets flags.
    /// </summary>
    internal byte ShiftRightArithmetic(byte value)
    {
        var result = Alu.ShiftRightArithmetic(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Swaps the nibbles of a byte and sets flags.
    /// </summary>
    internal byte SwapNibbles(byte value)
    {
        var result = Alu.Swap(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, result.Carry);
        return result.Result;
    }

    /// <summary>
    /// Rotates A left circular (RLCA instruction).
    /// </summary>
    internal void RotateALeftCircular()
    {
        var result = Alu.RotateLeftCircular(Regs.A);
        // RLCA always resets Zero flag
        SetFlags(false, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Rotates A right circular (RRCA instruction).
    /// </summary>
    internal void RotateARightCircular()
    {
        var result = Alu.RotateRightCircular(Regs.A);
        // RRCA always resets Zero flag
        SetFlags(false, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Rotates A left through carry (RLA instruction).
    /// </summary>
    internal void RotateALeftThroughCarry()
    {
        var result = Alu.RotateLeft(Regs.A, GetCarryFlag());
        // RLA always resets Zero flag
        SetFlags(false, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Rotates A right through carry (RRA instruction).
    /// </summary>
    internal void RotateARightThroughCarry()
    {
        var result = Alu.RotateRight(Regs.A, GetCarryFlag());
        // RRA always resets Zero flag
        SetFlags(false, result.Negative, result.HalfCarry, result.Carry);
        Regs.A = result.Result;
    }

    /// <summary>
    /// Adds a 16-bit value to HL and sets flags.
    /// </summary>
    internal void AddToHL(ushort value)
    {
        int result = Regs.HL + value;
        bool carry = result > 0xFFFF;
        bool halfCarry = (Regs.HL & 0x0FFF) + (value & 0x0FFF) > 0x0FFF;

        Regs.HL = (ushort)(result & 0xFFFF);

        // ADD HL,rr: N=0, H=half carry from bit 11, C=carry from bit 15, Z=unchanged
        SetFlags(GetZeroFlag(), false, halfCarry, carry);
    }

    /// <summary>
    /// Stores SP at immediate 16-bit address.
    /// </summary>
    internal void StoreSPAtImm16()
    {
        ushort addr = ReadImm16();
        _mmu.WriteWord(addr, Regs.SP);
    }

    /// <summary>
    /// Increments value at (HL).
    /// </summary>
    internal void IncMemoryHL()
    {
        byte value = ReadHL();
        var result = Alu.Inc8(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, GetCarryFlag());
        WriteHL(result.Result);
    }

    /// <summary>
    /// Decrements value at (HL).
    /// </summary>
    internal void DecMemoryHL()
    {
        byte value = ReadHL();
        var result = Alu.Dec8(value);
        SetFlags(result.Zero, result.Negative, result.HalfCarry, GetCarryFlag());
        WriteHL(result.Result);
    }

    #endregion
}
