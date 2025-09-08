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
        Primary[0x46] = new Instruction("LD B,(HL)", 1, 8, cpu => { cpu.Regs.B = cpu.ReadHL(); return 8; });
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
        Primary[0x4E] = new Instruction("LD C,(HL)", 1, 8, cpu => { cpu.Regs.C = cpu.ReadHL(); return 8; });
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
        Primary[0x66] = new Instruction("LD H,(HL)", 1, 8, cpu => { cpu.Regs.H = cpu.ReadHL(); return 8; });
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
        Primary[0x6E] = new Instruction("LD L,(HL)", 1, 8, cpu => { cpu.Regs.L = cpu.ReadHL(); return 8; });
        // 0x6F: LD L,A
        Primary[0x6F] = new Instruction("LD L,A", 1, 4, cpu => { cpu.Regs.L = cpu.Regs.A; return 4; });

        // 0x70: LD (HL),B
        Primary[0x70] = new Instruction("LD (HL),B", 1, 8, cpu => { cpu.WriteHL(cpu.Regs.B); return 8; });
        // 0x71: LD (HL),C
        Primary[0x71] = new Instruction("LD (HL),C", 1, 8, cpu => { cpu.WriteHL(cpu.Regs.C); return 8; });
        // 0x72: LD (HL),D
        Primary[0x72] = new Instruction("LD (HL),D", 1, 8, cpu => { cpu.WriteHL(cpu.Regs.D); return 8; });
        // 0x73: LD (HL),E
        Primary[0x73] = new Instruction("LD (HL),E", 1, 8, cpu => { cpu.WriteHL(cpu.Regs.E); return 8; });
        // 0x74: LD (HL),H
        Primary[0x74] = new Instruction("LD (HL),H", 1, 8, cpu => { cpu.WriteHL(cpu.Regs.H); return 8; });
        // 0x75: LD (HL),L
        Primary[0x75] = new Instruction("LD (HL),L", 1, 8, cpu => { cpu.WriteHL(cpu.Regs.L); return 8; });
        // 0x76: HALT - skip for now
        // 0x77: LD (HL),A
        Primary[0x77] = new Instruction("LD (HL),A", 1, 8, cpu => { cpu.WriteHL(cpu.Regs.A); return 8; });

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
        Primary[0x7E] = new Instruction("LD A,(HL)", 1, 8, cpu => { cpu.Regs.A = cpu.ReadHL(); return 8; });
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
            cpu.WriteHL(value);
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
            byte value = cpu.ReadHL();
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

        // Additional addressing mode instructions
        // 0x02: LD (BC),A
        Primary[0x02] = new Instruction("LD (BC),A", 1, 8, cpu =>
        {
            cpu.WriteBC(cpu.Regs.A);
            return 8;
        });

        // 0x0A: LD A,(BC)
        Primary[0x0A] = new Instruction("LD A,(BC)", 1, 8, cpu =>
        {
            cpu.Regs.A = cpu.ReadBC();
            return 8;
        });

        // 0x12: LD (DE),A
        Primary[0x12] = new Instruction("LD (DE),A", 1, 8, cpu =>
        {
            cpu.WriteDE(cpu.Regs.A);
            return 8;
        });

        // 0x1A: LD A,(DE)
        Primary[0x1A] = new Instruction("LD A,(DE)", 1, 8, cpu =>
        {
            cpu.Regs.A = cpu.ReadDE();
            return 8;
        });

        // 0x22: LD (HL+),A
        Primary[0x22] = new Instruction("LD (HL+),A", 1, 8, cpu =>
        {
            cpu.WriteHLI(cpu.Regs.A);
            return 8;
        });

        // 0x2A: LD A,(HL+)
        Primary[0x2A] = new Instruction("LD A,(HL+)", 1, 8, cpu =>
        {
            cpu.Regs.A = cpu.ReadHLI();
            return 8;
        });

        // 0x32: LD (HL-),A
        Primary[0x32] = new Instruction("LD (HL-),A", 1, 8, cpu =>
        {
            cpu.WriteHLD(cpu.Regs.A);
            return 8;
        });

        // 0x3A: LD A,(HL-)
        Primary[0x3A] = new Instruction("LD A,(HL-)", 1, 8, cpu =>
        {
            cpu.Regs.A = cpu.ReadHLD();
            return 8;
        });

        // 0xE0: LDH (a8),A
        Primary[0xE0] = new Instruction("LDH (a8),A", 2, 12, cpu =>
        {
            cpu.WriteHighImm8(cpu.Regs.A);
            return 12;
        });

        // 0xF0: LDH A,(a8)
        Primary[0xF0] = new Instruction("LDH A,(a8)", 2, 12, cpu =>
        {
            cpu.Regs.A = cpu.ReadHighImm8();
            return 12;
        });

        // 0xE2: LD (C),A
        Primary[0xE2] = new Instruction("LD (C),A", 1, 8, cpu =>
        {
            cpu.WriteHighC(cpu.Regs.A);
            return 8;
        });

        // 0xF2: LD A,(C)
        Primary[0xF2] = new Instruction("LD A,(C)", 1, 8, cpu =>
        {
            cpu.Regs.A = cpu.ReadHighC();
            return 8;
        });

        // 0xEA: LD (a16),A
        Primary[0xEA] = new Instruction("LD (a16),A", 3, 16, cpu =>
        {
            cpu.WriteImm16Addr(cpu.Regs.A);
            return 16;
        });

        // 0xFA: LD A,(a16)
        Primary[0xFA] = new Instruction("LD A,(a16)", 3, 16, cpu =>
        {
            cpu.Regs.A = cpu.ReadImm16Addr();
            return 16;
        });

        // 16-bit Load operations
        // 0x01: LD BC,d16
        Primary[0x01] = new Instruction("LD BC,d16", 3, 12, OperandType.Immediate16, cpu =>
        {
            cpu.Regs.BC = cpu.ReadImm16();
            return 12;
        });

        // 0x11: LD DE,d16
        Primary[0x11] = new Instruction("LD DE,d16", 3, 12, OperandType.Immediate16, cpu =>
        {
            cpu.Regs.DE = cpu.ReadImm16();
            return 12;
        });

        // 0x21: LD HL,d16
        Primary[0x21] = new Instruction("LD HL,d16", 3, 12, OperandType.Immediate16, cpu =>
        {
            cpu.Regs.HL = cpu.ReadImm16();
            return 12;
        });

        // 0x31: LD SP,d16
        Primary[0x31] = new Instruction("LD SP,d16", 3, 12, OperandType.Immediate16, cpu =>
        {
            cpu.Regs.SP = cpu.ReadImm16();
            return 12;
        });

        // INC operations
        // 0x03: INC BC
        Primary[0x03] = new Instruction("INC BC", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.BC++;
            return 8;
        });

        // 0x04: INC B
        Primary[0x04] = new Instruction("INC B", 1, 4, OperandType.Register, cpu =>
        {
            cpu.IncReg8(ref cpu.Regs.B);
            return 4;
        });

        // 0x0C: INC C
        Primary[0x0C] = new Instruction("INC C", 1, 4, OperandType.Register, cpu =>
        {
            cpu.IncReg8(ref cpu.Regs.C);
            return 4;
        });

        // DEC operations
        // 0x05: DEC B
        Primary[0x05] = new Instruction("DEC B", 1, 4, OperandType.Register, cpu =>
        {
            cpu.DecReg8(ref cpu.Regs.B);
            return 4;
        });

        // 0x0B: DEC BC
        Primary[0x0B] = new Instruction("DEC BC", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.BC--;
            return 8;
        });

        // 0x0D: DEC C
        Primary[0x0D] = new Instruction("DEC C", 1, 4, OperandType.Register, cpu =>
        {
            cpu.DecReg8(ref cpu.Regs.C);
            return 4;
        });

        // Jump operations
        // 0x18: JR r8 (relative jump)
        Primary[0x18] = new Instruction("JR r8", 2, 12, OperandType.Relative8, cpu =>
        {
            cpu.JumpRelative();
            return 12;
        });

        // 0xC3: JP a16 (absolute jump)
        Primary[0xC3] = new Instruction("JP a16", 3, 16, OperandType.Immediate16, cpu =>
        {
            cpu.Regs.PC = cpu.ReadImm16();
            return 16;
        });

        // 0xE9: JP (HL) (jump to address in HL)
        Primary[0xE9] = new Instruction("JP (HL)", 1, 4, OperandType.Register, cpu =>
        {
            cpu.Regs.PC = cpu.Regs.HL;
            return 4;
        });

        // Conditional jumps
        // 0x20: JR NZ,r8
        Primary[0x20] = new Instruction("JR NZ,r8", 2, 8, OperandType.Relative8, cpu =>
        {
            if (!cpu.GetZeroFlag())
            {
                cpu.JumpRelative();
                return 12;
            }
            cpu.Regs.PC++; // Skip operand
            return 8;
        });

        // 0x28: JR Z,r8
        Primary[0x28] = new Instruction("JR Z,r8", 2, 8, OperandType.Relative8, cpu =>
        {
            if (cpu.GetZeroFlag())
            {
                cpu.JumpRelative();
                return 12;
            }
            cpu.Regs.PC++; // Skip operand
            return 8;
        });

        // Stack operations
        // 0xC1: POP BC
        Primary[0xC1] = new Instruction("POP BC", 1, 12, OperandType.Register, cpu =>
        {
            cpu.Regs.BC = cpu.PopStack();
            return 12;
        });

        // 0xC5: PUSH BC
        Primary[0xC5] = new Instruction("PUSH BC", 1, 16, OperandType.Register, cpu =>
        {
            cpu.PushStack(cpu.Regs.BC);
            return 16;
        });

        // Call operations
        // 0xCD: CALL a16
        Primary[0xCD] = new Instruction("CALL a16", 3, 24, OperandType.Immediate16, cpu =>
        {
            ushort addr = cpu.ReadImm16();
            cpu.PushStack(cpu.Regs.PC);
            cpu.Regs.PC = addr;
            return 24;
        });

        // Return operations
        // 0xC9: RET
        Primary[0xC9] = new Instruction("RET", 1, 16, OperandType.None, cpu =>
        {
            cpu.Regs.PC = cpu.PopStack();
            return 16;
        });

        // Miscellaneous operations
        // 0x76: HALT
        Primary[0x76] = new Instruction("HALT", 1, 4, OperandType.None, cpu =>
        {
            cpu.SetHalted(true);
            return 4;
        });

        // 0xF3: DI (disable interrupts)
        Primary[0xF3] = new Instruction("DI", 1, 4, OperandType.None, cpu =>
        {
            cpu.InterruptsEnabled = false;
            return 4;
        });

        // 0xFB: EI (enable interrupts)
        Primary[0xFB] = new Instruction("EI", 1, 4, OperandType.None, cpu =>
        {
            cpu.InterruptsEnabled = true;
            return 4;
        });

        // Fill remaining missing opcodes with proper implementations/stubs
        // Note: Some opcodes are intentionally left null for invalid instructions

        // 0x07: RLCA
        Primary[0x07] = new Instruction("RLCA", 1, 4, OperandType.None, cpu =>
        {
            // Placeholder: Rotate A left circular
            return 4;
        });

        // 0x08: LD (a16),SP
        Primary[0x08] = new Instruction("LD (a16),SP", 3, 20, OperandType.MemoryImmediate16, cpu =>
        {
            // Placeholder: Store SP at immediate 16-bit address
            cpu.ReadImm16(); // consume operand
            return 20;
        });

        // 0x09: ADD HL,BC
        Primary[0x09] = new Instruction("ADD HL,BC", 1, 8, OperandType.Register, cpu =>
        {
            // Placeholder: Add BC to HL
            return 8;
        });

        // 0x0F: RRCA
        Primary[0x0F] = new Instruction("RRCA", 1, 4, OperandType.None, cpu =>
        {
            // Placeholder: Rotate A right circular
            return 4;
        });

        // 0x10: STOP 0
        Primary[0x10] = new Instruction("STOP 0", 2, 4, OperandType.Immediate8, cpu =>
        {
            cpu.ReadImm8(); // consume operand
            return 4;
        });

        // 0x13: INC DE
        Primary[0x13] = new Instruction("INC DE", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.DE++;
            return 8;
        });

        // 0x14: INC D
        Primary[0x14] = new Instruction("INC D", 1, 4, OperandType.Register, cpu =>
        {
            cpu.IncReg8(ref cpu.Regs.D);
            return 4;
        });

        // 0x15: DEC D
        Primary[0x15] = new Instruction("DEC D", 1, 4, OperandType.Register, cpu =>
        {
            cpu.DecReg8(ref cpu.Regs.D);
            return 4;
        });

        // 0x17: RLA
        Primary[0x17] = new Instruction("RLA", 1, 4, OperandType.None, cpu =>
        {
            // Placeholder: Rotate A left through carry
            return 4;
        });

        // 0x19: ADD HL,DE
        Primary[0x19] = new Instruction("ADD HL,DE", 1, 8, OperandType.Register, cpu =>
        {
            // Placeholder: Add DE to HL
            return 8;
        });

        // 0x1B: DEC DE
        Primary[0x1B] = new Instruction("DEC DE", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.DE--;
            return 8;
        });

        // 0x1C: INC E
        Primary[0x1C] = new Instruction("INC E", 1, 4, OperandType.Register, cpu =>
        {
            cpu.IncReg8(ref cpu.Regs.E);
            return 4;
        });

        // 0x1D: DEC E
        Primary[0x1D] = new Instruction("DEC E", 1, 4, OperandType.Register, cpu =>
        {
            cpu.DecReg8(ref cpu.Regs.E);
            return 4;
        });

        // 0x1F: RRA
        Primary[0x1F] = new Instruction("RRA", 1, 4, OperandType.None, cpu =>
        {
            // Placeholder: Rotate A right through carry
            return 4;
        });

        // 0x23: INC HL
        Primary[0x23] = new Instruction("INC HL", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.HL++;
            return 8;
        });

        // 0x24: INC H
        Primary[0x24] = new Instruction("INC H", 1, 4, OperandType.Register, cpu =>
        {
            cpu.IncReg8(ref cpu.Regs.H);
            return 4;
        });

        // 0x25: DEC H
        Primary[0x25] = new Instruction("DEC H", 1, 4, OperandType.Register, cpu =>
        {
            cpu.DecReg8(ref cpu.Regs.H);
            return 4;
        });

        // 0x27: DAA
        Primary[0x27] = new Instruction("DAA", 1, 4, OperandType.None, cpu =>
        {
            cpu.DecimalAdjustA();
            return 4;
        });

        // 0x29: ADD HL,HL
        Primary[0x29] = new Instruction("ADD HL,HL", 1, 8, OperandType.Register, cpu =>
        {
            // Placeholder: Add HL to itself (left shift)
            return 8;
        });

        // 0x2B: DEC HL
        Primary[0x2B] = new Instruction("DEC HL", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.HL--;
            return 8;
        });

        // 0x2C: INC L
        Primary[0x2C] = new Instruction("INC L", 1, 4, OperandType.Register, cpu =>
        {
            cpu.IncReg8(ref cpu.Regs.L);
            return 4;
        });

        // 0x2D: DEC L
        Primary[0x2D] = new Instruction("DEC L", 1, 4, OperandType.Register, cpu =>
        {
            cpu.DecReg8(ref cpu.Regs.L);
            return 4;
        });

        // 0x2F: CPL
        Primary[0x2F] = new Instruction("CPL", 1, 4, OperandType.None, cpu =>
        {
            cpu.ComplementA();
            return 4;
        });

        // Conditional jumps
        // 0x30: JR NC,r8
        Primary[0x30] = new Instruction("JR NC,r8", 2, 8, OperandType.Relative8, cpu =>
        {
            if (!cpu.GetCarryFlag())
            {
                cpu.JumpRelative();
                return 12;
            }
            cpu.Regs.PC++; // Skip operand
            return 8;
        });

        // 0x33: INC SP
        Primary[0x33] = new Instruction("INC SP", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.SP++;
            return 8;
        });

        // 0x34: INC (HL)
        Primary[0x34] = new Instruction("INC (HL)", 1, 12, OperandType.Memory, cpu =>
        {
            // Placeholder: Increment value at (HL)
            return 12;
        });

        // 0x35: DEC (HL)
        Primary[0x35] = new Instruction("DEC (HL)", 1, 12, OperandType.Memory, cpu =>
        {
            // Placeholder: Decrement value at (HL)
            return 12;
        });

        // 0x37: SCF
        Primary[0x37] = new Instruction("SCF", 1, 4, OperandType.None, cpu =>
        {
            cpu.SetCarryFlag();
            return 4;
        });

        // 0x38: JR C,r8
        Primary[0x38] = new Instruction("JR C,r8", 2, 8, OperandType.Relative8, cpu =>
        {
            if (cpu.GetCarryFlag())
            {
                cpu.JumpRelative();
                return 12;
            }
            cpu.Regs.PC++; // Skip operand
            return 8;
        });

        // 0x39: ADD HL,SP
        Primary[0x39] = new Instruction("ADD HL,SP", 1, 8, OperandType.Register, cpu =>
        {
            // Placeholder: Add SP to HL
            return 8;
        });

        // 0x3B: DEC SP
        Primary[0x3B] = new Instruction("DEC SP", 1, 8, OperandType.Register, cpu =>
        {
            cpu.Regs.SP--;
            return 8;
        });

        // 0x3C: INC A
        Primary[0x3C] = new Instruction("INC A", 1, 4, OperandType.Register, cpu =>
        {
            cpu.IncReg8(ref cpu.Regs.A);
            return 4;
        });

        // 0x3D: DEC A
        Primary[0x3D] = new Instruction("DEC A", 1, 4, OperandType.Register, cpu =>
        {
            cpu.DecReg8(ref cpu.Regs.A);
            return 4;
        });

        // 0x3F: CCF
        Primary[0x3F] = new Instruction("CCF", 1, 4, OperandType.None, cpu =>
        {
            cpu.ComplementCarryFlag();
            return 4;
        });

        // Complete ALU operations with other registers
        // 0x88-0x8F: ADC A,r (add with carry)
        Primary[0x88] = new Instruction("ADC A,B", 1, 4, OperandType.Register, cpu => { /* ADC A,B */ return 4; });
        Primary[0x89] = new Instruction("ADC A,C", 1, 4, OperandType.Register, cpu => { /* ADC A,C */ return 4; });
        Primary[0x8A] = new Instruction("ADC A,D", 1, 4, OperandType.Register, cpu => { /* ADC A,D */ return 4; });
        Primary[0x8B] = new Instruction("ADC A,E", 1, 4, OperandType.Register, cpu => { /* ADC A,E */ return 4; });
        Primary[0x8C] = new Instruction("ADC A,H", 1, 4, OperandType.Register, cpu => { /* ADC A,H */ return 4; });
        Primary[0x8D] = new Instruction("ADC A,L", 1, 4, OperandType.Register, cpu => { /* ADC A,L */ return 4; });
        Primary[0x8E] = new Instruction("ADC A,(HL)", 1, 8, OperandType.Memory, cpu => { /* ADC A,(HL) */ return 8; });
        Primary[0x8F] = new Instruction("ADC A,A", 1, 4, OperandType.Register, cpu => { /* ADC A,A */ return 4; });

        // 0x90-0x97: SUB r
        Primary[0x90] = new Instruction("SUB B", 1, 4, OperandType.Register, cpu => { /* SUB B */ return 4; });
        Primary[0x91] = new Instruction("SUB C", 1, 4, OperandType.Register, cpu => { /* SUB C */ return 4; });
        Primary[0x92] = new Instruction("SUB D", 1, 4, OperandType.Register, cpu => { /* SUB D */ return 4; });
        Primary[0x93] = new Instruction("SUB E", 1, 4, OperandType.Register, cpu => { /* SUB E */ return 4; });
        Primary[0x94] = new Instruction("SUB H", 1, 4, OperandType.Register, cpu => { /* SUB H */ return 4; });
        Primary[0x95] = new Instruction("SUB L", 1, 4, OperandType.Register, cpu => { /* SUB L */ return 4; });
        Primary[0x96] = new Instruction("SUB (HL)", 1, 8, OperandType.Memory, cpu => { /* SUB (HL) */ return 8; });
        Primary[0x97] = new Instruction("SUB A", 1, 4, OperandType.Register, cpu => { /* SUB A */ return 4; });

        // 0x98-0x9F: SBC A,r (subtract with carry)
        Primary[0x98] = new Instruction("SBC A,B", 1, 4, OperandType.Register, cpu => { /* SBC A,B */ return 4; });
        Primary[0x99] = new Instruction("SBC A,C", 1, 4, OperandType.Register, cpu => { /* SBC A,C */ return 4; });
        Primary[0x9A] = new Instruction("SBC A,D", 1, 4, OperandType.Register, cpu => { /* SBC A,D */ return 4; });
        Primary[0x9B] = new Instruction("SBC A,E", 1, 4, OperandType.Register, cpu => { /* SBC A,E */ return 4; });
        Primary[0x9C] = new Instruction("SBC A,H", 1, 4, OperandType.Register, cpu => { /* SBC A,H */ return 4; });
        Primary[0x9D] = new Instruction("SBC A,L", 1, 4, OperandType.Register, cpu => { /* SBC A,L */ return 4; });
        Primary[0x9E] = new Instruction("SBC A,(HL)", 1, 8, OperandType.Memory, cpu => { /* SBC A,(HL) */ return 8; });
        Primary[0x9F] = new Instruction("SBC A,A", 1, 4, OperandType.Register, cpu => { /* SBC A,A */ return 4; });

        // 0xA0-0xA7: AND r
        Primary[0xA0] = new Instruction("AND B", 1, 4, OperandType.Register, cpu => { cpu.AndWithA(cpu.Regs.B); return 4; });
        Primary[0xA1] = new Instruction("AND C", 1, 4, OperandType.Register, cpu => { cpu.AndWithA(cpu.Regs.C); return 4; });
        Primary[0xA2] = new Instruction("AND D", 1, 4, OperandType.Register, cpu => { cpu.AndWithA(cpu.Regs.D); return 4; });
        Primary[0xA3] = new Instruction("AND E", 1, 4, OperandType.Register, cpu => { cpu.AndWithA(cpu.Regs.E); return 4; });
        Primary[0xA4] = new Instruction("AND H", 1, 4, OperandType.Register, cpu => { cpu.AndWithA(cpu.Regs.H); return 4; });
        Primary[0xA5] = new Instruction("AND L", 1, 4, OperandType.Register, cpu => { cpu.AndWithA(cpu.Regs.L); return 4; });
        Primary[0xA6] = new Instruction("AND (HL)", 1, 8, OperandType.Memory, cpu => { cpu.AndWithA(cpu.ReadHL()); return 8; });
        Primary[0xA7] = new Instruction("AND A", 1, 4, OperandType.Register, cpu => { cpu.AndWithA(cpu.Regs.A); return 4; });

        // 0xA8-0xAF: XOR r
        Primary[0xA8] = new Instruction("XOR B", 1, 4, OperandType.Register, cpu => { cpu.XorWithA(cpu.Regs.B); return 4; });
        Primary[0xA9] = new Instruction("XOR C", 1, 4, OperandType.Register, cpu => { cpu.XorWithA(cpu.Regs.C); return 4; });
        Primary[0xAA] = new Instruction("XOR D", 1, 4, OperandType.Register, cpu => { cpu.XorWithA(cpu.Regs.D); return 4; });
        Primary[0xAB] = new Instruction("XOR E", 1, 4, OperandType.Register, cpu => { cpu.XorWithA(cpu.Regs.E); return 4; });
        Primary[0xAC] = new Instruction("XOR H", 1, 4, OperandType.Register, cpu => { cpu.XorWithA(cpu.Regs.H); return 4; });
        Primary[0xAD] = new Instruction("XOR L", 1, 4, OperandType.Register, cpu => { cpu.XorWithA(cpu.Regs.L); return 4; });
        Primary[0xAE] = new Instruction("XOR (HL)", 1, 8, OperandType.Memory, cpu => { cpu.XorWithA(cpu.ReadHL()); return 8; });
        Primary[0xAF] = new Instruction("XOR A", 1, 4, OperandType.Register, cpu => { cpu.XorWithA(cpu.Regs.A); return 4; });

        // 0xB0-0xB7: OR r
        Primary[0xB0] = new Instruction("OR B", 1, 4, OperandType.Register, cpu => { cpu.OrWithA(cpu.Regs.B); return 4; });
        Primary[0xB1] = new Instruction("OR C", 1, 4, OperandType.Register, cpu => { cpu.OrWithA(cpu.Regs.C); return 4; });
        Primary[0xB2] = new Instruction("OR D", 1, 4, OperandType.Register, cpu => { cpu.OrWithA(cpu.Regs.D); return 4; });
        Primary[0xB3] = new Instruction("OR E", 1, 4, OperandType.Register, cpu => { cpu.OrWithA(cpu.Regs.E); return 4; });
        Primary[0xB4] = new Instruction("OR H", 1, 4, OperandType.Register, cpu => { cpu.OrWithA(cpu.Regs.H); return 4; });
        Primary[0xB5] = new Instruction("OR L", 1, 4, OperandType.Register, cpu => { cpu.OrWithA(cpu.Regs.L); return 4; });
        Primary[0xB6] = new Instruction("OR (HL)", 1, 8, OperandType.Memory, cpu => { cpu.OrWithA(cpu.ReadHL()); return 8; });
        Primary[0xB7] = new Instruction("OR A", 1, 4, OperandType.Register, cpu => { cpu.OrWithA(cpu.Regs.A); return 4; });

        // 0xB8-0xBF: CP r (compare)
        Primary[0xB8] = new Instruction("CP B", 1, 4, OperandType.Register, cpu => { cpu.CompareA(cpu.Regs.B); return 4; });
        Primary[0xB9] = new Instruction("CP C", 1, 4, OperandType.Register, cpu => { cpu.CompareA(cpu.Regs.C); return 4; });
        Primary[0xBA] = new Instruction("CP D", 1, 4, OperandType.Register, cpu => { cpu.CompareA(cpu.Regs.D); return 4; });
        Primary[0xBB] = new Instruction("CP E", 1, 4, OperandType.Register, cpu => { cpu.CompareA(cpu.Regs.E); return 4; });
        Primary[0xBC] = new Instruction("CP H", 1, 4, OperandType.Register, cpu => { cpu.CompareA(cpu.Regs.H); return 4; });
        Primary[0xBD] = new Instruction("CP L", 1, 4, OperandType.Register, cpu => { cpu.CompareA(cpu.Regs.L); return 4; });
        Primary[0xBE] = new Instruction("CP (HL)", 1, 8, OperandType.Memory, cpu => { cpu.CompareA(cpu.ReadHL()); return 8; });
        Primary[0xBF] = new Instruction("CP A", 1, 4, OperandType.Register, cpu => { cpu.CompareA(cpu.Regs.A); return 4; });

        // More control flow and stack operations
        // 0xC0: RET NZ
        Primary[0xC0] = new Instruction("RET NZ", 1, 8, OperandType.None, cpu =>
        {
            if (!cpu.GetZeroFlag())
            {
                cpu.Regs.PC = cpu.PopStack();
                return 20;
            }
            return 8;
        });

        // 0xC2: JP NZ,a16
        Primary[0xC2] = new Instruction("JP NZ,a16", 3, 12, OperandType.Immediate16, cpu =>
        {
            ushort addr = cpu.ReadImm16();
            if (!cpu.GetZeroFlag())
            {
                cpu.Regs.PC = addr;
                return 16;
            }
            return 12;
        });

        // 0xC4: CALL NZ,a16
        Primary[0xC4] = new Instruction("CALL NZ,a16", 3, 12, OperandType.Immediate16, cpu =>
        {
            ushort addr = cpu.ReadImm16();
            if (!cpu.GetZeroFlag())
            {
                cpu.PushStack(cpu.Regs.PC);
                cpu.Regs.PC = addr;
                return 24;
            }
            return 12;
        });

        // 0xC7: RST 00H
        Primary[0xC7] = new Instruction("RST 00H", 1, 16, OperandType.None, cpu =>
        {
            cpu.PushStack(cpu.Regs.PC);
            cpu.Regs.PC = 0x00;
            return 16;
        });

        // 0xC8: RET Z
        Primary[0xC8] = new Instruction("RET Z", 1, 8, OperandType.None, cpu =>
        {
            if (cpu.GetZeroFlag())
            {
                cpu.Regs.PC = cpu.PopStack();
                return 20;
            }
            return 8;
        });

        // 0xCA: JP Z,a16
        Primary[0xCA] = new Instruction("JP Z,a16", 3, 12, OperandType.Immediate16, cpu =>
        {
            ushort addr = cpu.ReadImm16();
            if (cpu.GetZeroFlag())
            {
                cpu.Regs.PC = addr;
                return 16;
            }
            return 12;
        });

        // 0xCB: PREFIX CB
        Primary[0xCB] = new Instruction("PREFIX CB", 1, 4, OperandType.None, cpu =>
        {
            // CB prefix is handled in CPU.Step(), this should not be executed directly
            return 4;
        });

        // 0xCC: CALL Z,a16
        Primary[0xCC] = new Instruction("CALL Z,a16", 3, 12, OperandType.Immediate16, cpu =>
        {
            ushort addr = cpu.ReadImm16();
            if (cpu.GetZeroFlag())
            {
                cpu.PushStack(cpu.Regs.PC);
                cpu.Regs.PC = addr;
                return 24;
            }
            return 12;
        });

        // Add remaining immediate ALU operations
        // 0xCE: ADC A,d8
        Primary[0xCE] = new Instruction("ADC A,d8", 2, 8, OperandType.Immediate8, cpu =>
        {
            byte operand = cpu.ReadImm8();
            cpu.AdcToA(operand);
            return 8;
        });

        // 0xCF-0xFF: RST vectors and remaining instructions
        Primary[0xCF] = new Instruction("RST 08H", 1, 16, OperandType.None, cpu => { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = 0x08; return 16; });
        Primary[0xD0] = new Instruction("RET NC", 1, 8, OperandType.None, cpu => { if (!cpu.GetCarryFlag()) { cpu.Regs.PC = cpu.PopStack(); return 20; } return 8; });
        Primary[0xD1] = new Instruction("POP DE", 1, 12, OperandType.Register, cpu => { cpu.Regs.DE = cpu.PopStack(); return 12; });
        Primary[0xD2] = new Instruction("JP NC,a16", 3, 12, OperandType.Immediate16, cpu => { ushort addr = cpu.ReadImm16(); if (!cpu.GetCarryFlag()) { cpu.Regs.PC = addr; return 16; } return 12; });
        // 0xD3 is invalid - left as null
        Primary[0xD4] = new Instruction("CALL NC,a16", 3, 12, OperandType.Immediate16, cpu => { ushort addr = cpu.ReadImm16(); if (!cpu.GetCarryFlag()) { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = addr; return 24; } return 12; });
        Primary[0xD5] = new Instruction("PUSH DE", 1, 16, OperandType.Register, cpu => { cpu.PushStack(cpu.Regs.DE); return 16; });
        Primary[0xD6] = new Instruction("SUB d8", 2, 8, OperandType.Immediate8, cpu => { byte operand = cpu.ReadImm8(); cpu.SubFromA(operand); return 8; });
        Primary[0xD7] = new Instruction("RST 10H", 1, 16, OperandType.None, cpu => { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = 0x10; return 16; });
        Primary[0xD8] = new Instruction("RET C", 1, 8, OperandType.None, cpu => { if (cpu.GetCarryFlag()) { cpu.Regs.PC = cpu.PopStack(); return 20; } return 8; });
        Primary[0xD9] = new Instruction("RETI", 1, 16, OperandType.None, cpu => { cpu.Regs.PC = cpu.PopStack(); cpu.InterruptsEnabled = true; return 16; });
        Primary[0xDA] = new Instruction("JP C,a16", 3, 12, OperandType.Immediate16, cpu => { ushort addr = cpu.ReadImm16(); if (cpu.GetCarryFlag()) { cpu.Regs.PC = addr; return 16; } return 12; });
        // 0xDB is invalid - left as null
        Primary[0xDC] = new Instruction("CALL C,a16", 3, 12, OperandType.Immediate16, cpu => { ushort addr = cpu.ReadImm16(); if (cpu.GetCarryFlag()) { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = addr; return 24; } return 12; });
        // 0xDD is invalid - left as null
        Primary[0xDE] = new Instruction("SBC A,d8", 2, 8, OperandType.Immediate8, cpu => { byte operand = cpu.ReadImm8(); cpu.SbcFromA(operand); return 8; });
        Primary[0xDF] = new Instruction("RST 18H", 1, 16, OperandType.None, cpu => { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = 0x18; return 16; });

        Primary[0xE1] = new Instruction("POP HL", 1, 12, OperandType.Register, cpu => { cpu.Regs.HL = cpu.PopStack(); return 12; });
        // 0xE3 and 0xE4 are invalid - left as null
        Primary[0xE5] = new Instruction("PUSH HL", 1, 16, OperandType.Register, cpu => { cpu.PushStack(cpu.Regs.HL); return 16; });
        Primary[0xE6] = new Instruction("AND d8", 2, 8, OperandType.Immediate8, cpu => { byte operand = cpu.ReadImm8(); cpu.AndWithA(operand); return 8; });
        Primary[0xE7] = new Instruction("RST 20H", 1, 16, OperandType.None, cpu => { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = 0x20; return 16; });
        Primary[0xE8] = new Instruction("ADD SP,r8", 2, 16, OperandType.Relative8, cpu => { cpu.ReadImm8(); return 16; });
        Primary[0xEE] = new Instruction("XOR d8", 2, 8, OperandType.Immediate8, cpu => { byte operand = cpu.ReadImm8(); cpu.XorWithA(operand); return 8; });
        Primary[0xEF] = new Instruction("RST 28H", 1, 16, OperandType.None, cpu => { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = 0x28; return 16; });

        Primary[0xF1] = new Instruction("POP AF", 1, 12, OperandType.Register, cpu => { /* POP AF with flag handling */ return 12; });
        // 0xF4 is invalid - left as null
        Primary[0xF5] = new Instruction("PUSH AF", 1, 16, OperandType.Register, cpu => { /* PUSH AF */ return 16; });
        Primary[0xF6] = new Instruction("OR d8", 2, 8, OperandType.Immediate8, cpu => { byte operand = cpu.ReadImm8(); cpu.OrWithA(operand); return 8; });
        Primary[0xF7] = new Instruction("RST 30H", 1, 16, OperandType.None, cpu => { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = 0x30; return 16; });
        Primary[0xF8] = new Instruction("LD HL,SP+r8", 2, 12, OperandType.Relative8, cpu => { cpu.ReadImm8(); return 12; });
        Primary[0xF9] = new Instruction("LD SP,HL", 1, 8, OperandType.Register, cpu => { cpu.Regs.SP = cpu.Regs.HL; return 8; });
        Primary[0xFE] = new Instruction("CP d8", 2, 8, OperandType.Immediate8, cpu => { byte operand = cpu.ReadImm8(); cpu.CompareA(operand); return 8; });
        Primary[0xFF] = new Instruction("RST 38H", 1, 16, OperandType.None, cpu => { cpu.PushStack(cpu.Regs.PC); cpu.Regs.PC = 0x38; return 16; });

        // Invalid opcodes are intentionally left as null:
        // 0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD
    }

    private static void InitializeCBTable()
    {
        // CB-prefixed instructions - Bit operations, rotates, and shifts
        string[] regNames = { "B", "C", "D", "E", "H", "L", "(HL)", "A" };

        // 0x00-0x3F: Rotate and shift operations
        string[] rotateOps = { "RLC", "RRC", "RL", "RR", "SLA", "SRA", "SWAP", "SRL" };

        for (int op = 0; op < 8; op++)
        {
            for (int reg = 0; reg < 8; reg++)
            {
                int opcode = (op << 3) + reg;
                string mnemonic = $"{rotateOps[op]} {regNames[reg]}";
                int cycles = (reg == 6) ? 16 : 8; // (HL) operations take longer
                OperandType operandType = (reg == 6) ? OperandType.Memory : OperandType.Register;

                CB[opcode] = new Instruction(mnemonic, 2, cycles, operandType, cpu =>
                {
                    // Placeholder for rotate/shift operations
                    if (reg == 6)
                    {
                        // (HL) memory operation
                        return 16;
                    }
                    else
                    {
                        // Register operation
                        return 8;
                    }
                });
            }
        }

        // 0x40-0x7F: BIT operations (test bit)
        for (int bit = 0; bit < 8; bit++)
        {
            for (int reg = 0; reg < 8; reg++)
            {
                int opcode = 0x40 + (bit << 3) + reg;
                string mnemonic = $"BIT {bit},{regNames[reg]}";
                int cycles = (reg == 6) ? 12 : 8; // (HL) operations take longer
                OperandType operandType = (reg == 6) ? OperandType.Memory : OperandType.Register;

                CB[opcode] = new Instruction(mnemonic, 2, cycles, operandType, cpu =>
                {
                    // Placeholder for bit test operations
                    if (reg == 6)
                    {
                        // Test bit in (HL)
                        cpu.TestBit(cpu.ReadHL(), bit);
                        return 12;
                    }
                    else
                    {
                        // Test bit in register - will need register access method
                        return 8;
                    }
                });
            }
        }

        // 0x80-0xBF: RES operations (reset bit)
        for (int bit = 0; bit < 8; bit++)
        {
            for (int reg = 0; reg < 8; reg++)
            {
                int opcode = 0x80 + (bit << 3) + reg;
                string mnemonic = $"RES {bit},{regNames[reg]}";
                int cycles = (reg == 6) ? 16 : 8; // (HL) operations take longer
                OperandType operandType = (reg == 6) ? OperandType.Memory : OperandType.Register;

                CB[opcode] = new Instruction(mnemonic, 2, cycles, operandType, cpu =>
                {
                    // Placeholder for bit reset operations
                    if (reg == 6)
                    {
                        // Reset bit in (HL)
                        byte value = cpu.ReadHL();
                        value = cpu.ResetBit(value, bit);
                        cpu.WriteHL(value);
                        return 16;
                    }
                    else
                    {
                        // Reset bit in register - will need register access method
                        return 8;
                    }
                });
            }
        }

        // 0xC0-0xFF: SET operations (set bit)
        for (int bit = 0; bit < 8; bit++)
        {
            for (int reg = 0; reg < 8; reg++)
            {
                int opcode = 0xC0 + (bit << 3) + reg;
                string mnemonic = $"SET {bit},{regNames[reg]}";
                int cycles = (reg == 6) ? 16 : 8; // (HL) operations take longer
                OperandType operandType = (reg == 6) ? OperandType.Memory : OperandType.Register;

                CB[opcode] = new Instruction(mnemonic, 2, cycles, operandType, cpu =>
                {
                    // Placeholder for bit set operations
                    if (reg == 6)
                    {
                        // Set bit in (HL)
                        byte value = cpu.ReadHL();
                        value = cpu.SetBit(value, bit);
                        cpu.WriteHL(value);
                        return 16;
                    }
                    else
                    {
                        // Set bit in register - will need register access method
                        return 8;
                    }
                });
            }
        }
    }
}