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

    private readonly Mmu _mmu;

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
        // Minimal placeholder: NOP for all opcodes.
        return ExecutePlaceholder(opcode);
    }

    private int ExecutePlaceholder(byte opcode)
    {
        // Provide a couple of actually working ops for tests: LD r,r'
        // We'll implement LD B,B .. LD A,A via a simple mapping for tests.
        if (opcode >= 0x40 && opcode <= 0x7F && opcode != 0x76)
        {
            int dst = (opcode >> 3) & 0x07;
            int src = opcode & 0x07;
            byte GetReg(int idx) => idx switch
            {
                0 => Regs.B,
                1 => Regs.C,
                2 => Regs.D,
                3 => Regs.E,
                4 => Regs.H,
                5 => Regs.L,
                6 => _mmu.ReadByte(Regs.HL),
                7 => Regs.A,
                _ => 0
            };
            void SetReg(int idx, byte val)
            {
                switch (idx)
                {
                    case 0: Regs.B = val; break;
                    case 1: Regs.C = val; break;
                    case 2: Regs.D = val; break;
                    case 3: Regs.E = val; break;
                    case 4: Regs.H = val; break;
                    case 5: Regs.L = val; break;
                    case 6: _mmu.WriteByte(Regs.HL, val); break;
                    case 7: Regs.A = val; break;
                }
            }
            SetReg(dst, GetReg(src));
            return src == 6 || dst == 6 ? 8 : 4;
        }

        // 0x00: NOP
        if (opcode == 0x00)
            return 4;

        // Unimplemented opcodes act as NOP in placeholder
        return 4;
    }
}
