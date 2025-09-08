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
        int result = Regs.A + value;

        // Set flags
        bool zero = (result & 0xFF) == 0;
        bool carry = result > 0xFF;
        bool halfCarry = (Regs.A & 0x0F) + (value & 0x0F) > 0x0F;

        SetFlags(zero, false, halfCarry, carry);
        Regs.A = (byte)(result & 0xFF);
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
}
