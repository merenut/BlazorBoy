using System;

namespace GameBoy.Core;

/// <summary>
/// Opcode table for Game Boy CPU instructions.
/// Maps opcodes (0x00-0xFF) to Instruction structs.
/// </summary>
public static class OpcodeTable
{
    /// <summary>
    /// Primary instruction table for opcodes 0x00-0xFF.
    /// </summary>
    public static readonly Instruction?[] Primary = new Instruction?[256];

    /// <summary>
    /// CB-prefixed instruction table for opcodes 0xCB00-0xCBFF.
    /// </summary>
    public static readonly Instruction?[] CB = new Instruction?[256];

    static OpcodeTable()
    {
        InitializePrimaryTable();
        InitializeCBTable();
    }

    private static void InitializePrimaryTable()
    {
        // 0x00: NOP
        Primary[0x00] = new Instruction("NOP", 1, 4, cpu => 4);

        // LD r,r' instructions (0x40-0x7F, excluding 0x76 which is HALT)
        // 0x40: LD B,B
        Primary[0x40] = new Instruction("LD B,B", 1, 4, cpu => { cpu.Regs.B = cpu.Regs.B; return 4; });
        // 0x41: LD B,C
        Primary[0x41] = new Instruction("LD B,C", 1, 4, cpu => { cpu.Regs.B = cpu.Regs.C; return 4; });
        // 0x42: LD B,D
        Primary[0x42] = new Instruction("LD B,D", 1, 4, cpu => { cpu.Regs.B = cpu.Regs.D; return 4; });
        // 0x43: LD B,E
        Primary[0x43] = new Instruction("LD B,E", 1, 4, cpu => { cpu.Regs.B = cpu.Regs.E; return 4; });
        // 0x44: LD B,H
        Primary[0x44] = new Instruction("LD B,H", 1, 4, cpu => { cpu.Regs.B = cpu.Regs.H; return 4; });
        // 0x45: LD B,L
        Primary[0x45] = new Instruction("LD B,L", 1, 4, cpu => { cpu.Regs.B = cpu.Regs.L; return 4; });
        // 0x46: LD B,(HL)
        Primary[0x46] = new Instruction("LD B,(HL)", 1, 8, cpu => { cpu.Regs.B = cpu._mmu.ReadByte(cpu.Regs.HL); return 8; });
        // 0x47: LD B,A
        Primary[0x47] = new Instruction("LD B,A", 1, 4, cpu => { cpu.Regs.B = cpu.Regs.A; return 4; });

        // 0x48: LD C,B
        Primary[0x48] = new Instruction("LD C,B", 1, 4, cpu => { cpu.Regs.C = cpu.Regs.B; return 4; });
        // 0x49: LD C,C
        Primary[0x49] = new Instruction("LD C,C", 1, 4, cpu => { cpu.Regs.C = cpu.Regs.C; return 4; });
        // 0x4A: LD C,D
        Primary[0x4A] = new Instruction("LD C,D", 1, 4, cpu => { cpu.Regs.C = cpu.Regs.D; return 4; });
        // 0x4B: LD C,E
        Primary[0x4B] = new Instruction("LD C,E", 1, 4, cpu => { cpu.Regs.C = cpu.Regs.E; return 4; });
        // 0x4C: LD C,H
        Primary[0x4C] = new Instruction("LD C,H", 1, 4, cpu => { cpu.Regs.C = cpu.Regs.H; return 4; });
        // 0x4D: LD C,L
        Primary[0x4D] = new Instruction("LD C,L", 1, 4, cpu => { cpu.Regs.C = cpu.Regs.L; return 4; });
        // 0x4E: LD C,(HL)
        Primary[0x4E] = new Instruction("LD C,(HL)", 1, 8, cpu => { cpu.Regs.C = cpu._mmu.ReadByte(cpu.Regs.HL); return 8; });
        // 0x4F: LD C,A
        Primary[0x4F] = new Instruction("LD C,A", 1, 4, cpu => { cpu.Regs.C = cpu.Regs.A; return 4; });

        // 0x50: LD D,B
        Primary[0x50] = new Instruction("LD D,B", 1, 4, cpu => { cpu.Regs.D = cpu.Regs.B; return 4; });
        // 0x51: LD D,C
        Primary[0x51] = new Instruction("LD D,C", 1, 4, cpu => { cpu.Regs.D = cpu.Regs.C; return 4; });
        // 0x52: LD D,D
        Primary[0x52] = new Instruction("LD D,D", 1, 4, cpu => { cpu.Regs.D = cpu.Regs.D; return 4; });
        // 0x53: LD D,E
        Primary[0x53] = new Instruction("LD D,E", 1, 4, cpu => { cpu.Regs.D = cpu.Regs.E; return 4; });
        // 0x54: LD D,H
        Primary[0x54] = new Instruction("LD D,H", 1, 4, cpu => { cpu.Regs.D = cpu.Regs.H; return 4; });
        // 0x55: LD D,L
        Primary[0x55] = new Instruction("LD D,L", 1, 4, cpu => { cpu.Regs.D = cpu.Regs.L; return 4; });
        // 0x56: LD D,(HL)
        Primary[0x56] = new Instruction("LD D,(HL)", 1, 8, cpu => { cpu.Regs.D = cpu._mmu.ReadByte(cpu.Regs.HL); return 8; });
        // 0x57: LD D,A
        Primary[0x57] = new Instruction("LD D,A", 1, 4, cpu => { cpu.Regs.D = cpu.Regs.A; return 4; });

        // 0x58: LD E,B
        Primary[0x58] = new Instruction("LD E,B", 1, 4, cpu => { cpu.Regs.E = cpu.Regs.B; return 4; });
        // 0x59: LD E,C
        Primary[0x59] = new Instruction("LD E,C", 1, 4, cpu => { cpu.Regs.E = cpu.Regs.C; return 4; });
        // 0x5A: LD E,D
        Primary[0x5A] = new Instruction("LD E,D", 1, 4, cpu => { cpu.Regs.E = cpu.Regs.D; return 4; });
        // 0x5B: LD E,E
        Primary[0x5B] = new Instruction("LD E,E", 1, 4, cpu => { cpu.Regs.E = cpu.Regs.E; return 4; });
        // 0x5C: LD E,H
        Primary[0x5C] = new Instruction("LD E,H", 1, 4, cpu => { cpu.Regs.E = cpu.Regs.H; return 4; });
        // 0x5D: LD E,L
        Primary[0x5D] = new Instruction("LD E,L", 1, 4, cpu => { cpu.Regs.E = cpu.Regs.L; return 4; });
        // 0x5E: LD E,(HL)
        Primary[0x5E] = new Instruction("LD E,(HL)", 1, 8, cpu => { cpu.Regs.E = cpu._mmu.ReadByte(cpu.Regs.HL); return 8; });
        // 0x5F: LD E,A
        Primary[0x5F] = new Instruction("LD E,A", 1, 4, cpu => { cpu.Regs.E = cpu.Regs.A; return 4; });

        // 0x60: LD H,B
        Primary[0x60] = new Instruction("LD H,B", 1, 4, cpu => { cpu.Regs.H = cpu.Regs.B; return 4; });
        // 0x61: LD H,C
        Primary[0x61] = new Instruction("LD H,C", 1, 4, cpu => { cpu.Regs.H = cpu.Regs.C; return 4; });
        // 0x62: LD H,D
        Primary[0x62] = new Instruction("LD H,D", 1, 4, cpu => { cpu.Regs.H = cpu.Regs.D; return 4; });
        // 0x63: LD H,E
        Primary[0x63] = new Instruction("LD H,E", 1, 4, cpu => { cpu.Regs.H = cpu.Regs.E; return 4; });
        // 0x64: LD H,H
        Primary[0x64] = new Instruction("LD H,H", 1, 4, cpu => { cpu.Regs.H = cpu.Regs.H; return 4; });
        // 0x65: LD H,L
        Primary[0x65] = new Instruction("LD H,L", 1, 4, cpu => { cpu.Regs.H = cpu.Regs.L; return 4; });
        // 0x66: LD H,(HL)
        Primary[0x66] = new Instruction("LD H,(HL)", 1, 8, cpu => { cpu.Regs.H = cpu._mmu.ReadByte(cpu.Regs.HL); return 8; });
        // 0x67: LD H,A
        Primary[0x67] = new Instruction("LD H,A", 1, 4, cpu => { cpu.Regs.H = cpu.Regs.A; return 4; });

        // 0x68: LD L,B
        Primary[0x68] = new Instruction("LD L,B", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.B; return 4; });
        // 0x69: LD L,C
        Primary[0x69] = new Instruction("LD L,C", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.C; return 4; });
        // 0x6A: LD L,D
        Primary[0x6A] = new Instruction("LD L,D", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.D; return 4; });
        // 0x6B: LD L,E
        Primary[0x6B] = new Instruction("LD L,E", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.E; return 4; });
        // 0x6C: LD L,H
        Primary[0x6C] = new Instruction("LD L,H", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.H; return 4; });
        // 0x6D: LD L,L
        Primary[0x6D] = new Instruction("LD L,L", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.L; return 4; });
        // 0x6E: LD L,(HL)
        Primary[0x6E] = new Instruction("LD L,(HL)", 1, 8, cpu => { cpu.Regs.L = cpu._mmu.ReadByte(cpu.Regs.HL); return 8; });
        // 0x6F: LD L,A
        Primary[0x6F] = new Instruction("LD L,A", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.A; return 4; });

        // 0x70: LD (HL),B
        Primary[0x70] = new Instruction("LD (HL),B", 1, 8, cpu => { cpu._mmu.WriteByte(cpu.Regs.HL, cpu.Regs.B); return 8; });
        // 0x71: LD (HL),C
        Primary[0x71] = new Instruction("LD (HL),C", 1, 8, cpu => { cpu._mmu.WriteByte(cpu.Regs.HL, cpu.Regs.C); return 8; });
        // 0x72: LD (HL),D
        Primary[0x72] = new Instruction("LD (HL),D", 1, 8, cpu => { cpu._mmu.WriteByte(cpu.Regs.HL, cpu.Regs.D); return 8; });
        // 0x73: LD (HL),E
        Primary[0x73] = new Instruction("LD (HL),E", 1, 8, cpu => { cpu._mmu.WriteByte(cpu.Regs.HL, cpu.Regs.E); return 8; });
        // 0x74: LD (HL),H
        Primary[0x74] = new Instruction("LD (HL),H", 1, 8, cpu => { cpu._mmu.WriteByte(cpu.Regs.HL, cpu.Regs.H); return 8; });
        // 0x75: LD (HL),L
        Primary[0x75] = new Instruction("LD (HL),L", 1, 8, cpu => { cpu._mmu.WriteByte(cpu.Regs.HL, cpu.Regs.L); return 8; });
        // 0x76: HALT - skip for now
        // 0x77: LD (HL),A
        Primary[0x77] = new Instruction("LD (HL),A", 1, 8, cpu => { cpu._mmu.WriteByte(cpu.Regs.HL, cpu.Regs.A); return 8; });

        // 0x78: LD A,B
        Primary[0x78] = new Instruction("LD A,B", 1, 4, cpu => { cpu.Regs.A = cpu.Regs.B; return 4; });
        // 0x79: LD A,C
        Primary[0x79] = new Instruction("LD A,C", 1, 4, cpu => { cpu.Regs.A = cpu.Regs.C; return 4; });
        // 0x7A: LD A,D
        Primary[0x7A] = new Instruction("LD A,D", 1, 4, cpu => { cpu.Regs.A = cpu.Regs.D; return 4; });
        // 0x7B: LD A,E
        Primary[0x7B] = new Instruction("LD A,E", 1, 4, cpu => { cpu.Regs.A = cpu.Regs.E; return 4; });
        // 0x7C: LD A,H
        Primary[0x7C] = new Instruction("LD A,H", 1, 4, cpu => { cpu.Regs.A = cpu.Regs.H; return 4; });
        // 0x7D: LD A,L
        Primary[0x7D] = new Instruction("LD A,L", 1, 4, cpu => { cpu.Regs.A = cpu.Regs.L; return 4; });
        // 0x7E: LD A,(HL)
        Primary[0x7E] = new Instruction("LD A,(HL)", 1, 8, cpu => { cpu.Regs.A = cpu._mmu.ReadByte(cpu.Regs.HL); return 8; });
        // 0x7F: LD A,A
        Primary[0x7F] = new Instruction("LD A,A", 1, 4, cpu => { cpu.Regs.A = cpu.Regs.A; return 4; });

        // LD r,d8 instructions (Load immediate byte into register)
        // 0x06: LD B,d8
        Primary[0x06] = new Instruction("LD B,d8", 2, 8, cpu =>
        {
            cpu.Regs.B = cpu.ReadImm8();
            return 8;
        });

        // 0x0E: LD C,d8
        Primary[0x0E] = new Instruction("LD C,d8", 2, 8, cpu =>
        {
            cpu.Regs.C = cpu.ReadImm8();
            return 8;
        });

        // 0x16: LD D,d8
        Primary[0x16] = new Instruction("LD D,d8", 2, 8, cpu =>
        {
            cpu.Regs.D = cpu.ReadImm8();
            return 8;
        });

        // 0x1E: LD E,d8
        Primary[0x1E] = new Instruction("LD E,d8", 2, 8, cpu =>
        {
            cpu.Regs.E = cpu.ReadImm8();
            return 8;
        });

        // 0x26: LD H,d8
        Primary[0x26] = new Instruction("LD H,d8", 2, 8, cpu =>
        {
            cpu.Regs.H = cpu.ReadImm8();
            return 8;
        });

        // 0x2E: LD L,d8
        Primary[0x2E] = new Instruction("LD L,d8", 2, 8, cpu =>
        {
            cpu.Regs.L = cpu.ReadImm8();
            return 8;
        });

        // 0x3E: LD A,d8
        Primary[0x3E] = new Instruction("LD A,d8", 2, 8, cpu =>
        {
            cpu.Regs.A = cpu.ReadImm8();
            return 8;
        });

        // 0x36: LD (HL),d8
        Primary[0x36] = new Instruction("LD (HL),d8", 2, 12, cpu =>
        {
            byte value = cpu.ReadImm8();
            cpu._mmu.WriteByte(cpu.Regs.HL, value);
            return 12;
        });

        // Basic ALU operations
        // 0x80: ADD A,B
        Primary[0x80] = new Instruction("ADD A,B", 1, 4, cpu =>
        {
            cpu.AddToA(cpu.Regs.B);
            return 4;
        });

        // 0x81: ADD A,C
        Primary[0x81] = new Instruction("ADD A,C", 1, 4, cpu =>
        {
            cpu.AddToA(cpu.Regs.C);
            return 4;
        });

        // 0x82: ADD A,D
        Primary[0x82] = new Instruction("ADD A,D", 1, 4, cpu =>
        {
            cpu.AddToA(cpu.Regs.D);
            return 4;
        });

        // 0x83: ADD A,E
        Primary[0x83] = new Instruction("ADD A,E", 1, 4, cpu =>
        {
            cpu.AddToA(cpu.Regs.E);
            return 4;
        });

        // 0x84: ADD A,H
        Primary[0x84] = new Instruction("ADD A,H", 1, 4, cpu =>
        {
            cpu.AddToA(cpu.Regs.H);
            return 4;
        });

        // 0x85: ADD A,L
        Primary[0x85] = new Instruction("ADD A,L", 1, 4, cpu =>
        {
            cpu.AddToA(cpu.Regs.L);
            return 4;
        });

        // 0x86: ADD A,(HL)
        Primary[0x86] = new Instruction("ADD A,(HL)", 1, 8, cpu =>
        {
            byte value = cpu._mmu.ReadByte(cpu.Regs.HL);
            cpu.AddToA(value);
            return 8;
        });

        // 0x87: ADD A,A
        Primary[0x87] = new Instruction("ADD A,A", 1, 4, cpu =>
        {
            cpu.AddToA(cpu.Regs.A);
            return 4;
        });

        // 0xC6: ADD A,d8
        Primary[0xC6] = new Instruction("ADD A,d8", 2, 8, cpu =>
        {
            byte value = cpu.ReadImm8();
            cpu.AddToA(value);
            return 8;
        });
    }

    private static void InitializeCBTable()
    {
        // CB-prefixed instructions will be added later
        // For now, leave empty
    }
}