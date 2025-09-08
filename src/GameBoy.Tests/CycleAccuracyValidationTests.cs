using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Validation tests to ensure all instruction timing is correct according to Pan Docs.
/// These tests verify edge cases and validate against the comprehensive timing reference.
/// </summary>
public class CycleAccuracyValidationTests
{
    #region Unknown Opcode Timing Tests

    [Fact]
    public void UnknownPrimaryOpcode_Returns4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Find an invalid opcode
        byte unknownOpcode = 0xD3; // Known invalid opcode

        mmu.WriteByte(0xC000, unknownOpcode);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles); // Should default to 4 cycles
    }

    [Fact]
    public void UnknownCBOpcode_Returns8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test unknown CB opcode
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0xFF); // Invalid CB opcode
        var cycles = cpu.Step();

        Assert.Equal(8, cycles); // Should default to 8 cycles for CB
    }

    #endregion

    #region Timing Consistency Tests

    [Fact]
    public void AllLoadRegisterToRegister_Take4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test all LD r,r' instructions (0x40-0x7F except 0x76 HALT)
        var testOpcodes = new List<byte>();
        for (byte i = 0x40; i <= 0x7F; i++)
        {
            if (i != 0x76) // Skip HALT
            {
                var instruction = OpcodeTable.Primary[i];
                if (instruction.HasValue && instruction.Value.Mnemonic.StartsWith("LD") &&
                    !instruction.Value.Mnemonic.Contains("(HL)"))
                {
                    testOpcodes.Add(i);
                }
            }
        }

        foreach (var opcode in testOpcodes)
        {
            cpu.Regs.PC = 0xC000;
            mmu.WriteByte(0xC000, opcode);
            var cycles = cpu.Step();

            Assert.Equal(4, cycles);
        }
    }

    [Fact]
    public void AllMemoryAccessHL_Take8Or12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.HL = 0xD000;

        // Test memory access instructions
        var memoryOpcodes = new Dictionary<byte, int>
        {
            { 0x46, 8 },  // LD B,(HL)
            { 0x4E, 8 },  // LD C,(HL)
            { 0x56, 8 },  // LD D,(HL)
            { 0x5E, 8 },  // LD E,(HL)
            { 0x66, 8 },  // LD H,(HL)
            { 0x6E, 8 },  // LD L,(HL)
            { 0x7E, 8 },  // LD A,(HL)
            { 0x70, 8 },  // LD (HL),B
            { 0x71, 8 },  // LD (HL),C
            { 0x72, 8 },  // LD (HL),D
            { 0x73, 8 },  // LD (HL),E
            { 0x74, 8 },  // LD (HL),H
            { 0x75, 8 },  // LD (HL),L
            { 0x77, 8 },  // LD (HL),A
            { 0x34, 12 }, // INC (HL)
            { 0x35, 12 }, // DEC (HL)
            { 0x36, 12 }, // LD (HL),d8
            { 0x86, 8 },  // ADD A,(HL)
            { 0x8E, 8 },  // ADC A,(HL)
            { 0x96, 8 },  // SUB (HL)
            { 0x9E, 8 },  // SBC A,(HL)
            { 0xA6, 8 },  // AND (HL)
            { 0xAE, 8 },  // XOR (HL)
            { 0xB6, 8 },  // OR (HL)
            { 0xBE, 8 }   // CP (HL)
        };

        foreach (var kvp in memoryOpcodes)
        {
            cpu.Regs.PC = 0xC000;
            mmu.WriteByte(0xC000, kvp.Key);
            if (kvp.Key == 0x36) // LD (HL),d8 needs immediate byte
            {
                mmu.WriteByte(0xC001, 0x42);
            }
            mmu.WriteByte(0xD000, 0x42); // Set up memory content

            var cycles = cpu.Step();

            Assert.Equal(kvp.Value, cycles);
        }
    }

    [Fact]
    public void All16BitOperations_Take8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test 16-bit register operations
        var opcodes16Bit = new List<byte>
        {
            0x03, // INC BC
            0x0B, // DEC BC
            0x13, // INC DE
            0x1B, // DEC DE
            0x23, // INC HL
            0x2B, // DEC HL
            0x33, // INC SP
            0x3B, // DEC SP
            0x09, // ADD HL,BC
            0x19, // ADD HL,DE
            0x29, // ADD HL,HL
            0x39  // ADD HL,SP
        };

        foreach (var opcode in opcodes16Bit)
        {
            cpu.Regs.PC = 0xC000;
            cpu.Regs.BC = 0x1234;
            cpu.Regs.DE = 0x5678;
            cpu.Regs.HL = 0x9ABC;
            cpu.Regs.SP = 0xFFFE;

            mmu.WriteByte(0xC000, opcode);
            var cycles = cpu.Step();

            Assert.Equal(8, cycles);
        }
    }

    [Fact]
    public void AllStackOperations_TakeCorrectCycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.SP = 0xFFFE;

        // PUSH operations should take 16 cycles
        var pushOpcodes = new List<byte> { 0xC5, 0xD5, 0xE5, 0xF5 }; // PUSH BC, DE, HL, AF

        foreach (var opcode in pushOpcodes)
        {
            cpu.Regs.PC = 0xC000;
            cpu.Regs.SP = 0xFFFE; // Reset SP

            mmu.WriteByte(0xC000, opcode);
            var cycles = cpu.Step();

            Assert.Equal(16, cycles);
        }

        // POP operations should take 12 cycles
        var popOpcodes = new List<byte> { 0xC1, 0xD1, 0xE1, 0xF1 }; // POP BC, DE, HL, AF

        foreach (var opcode in popOpcodes)
        {
            cpu.Regs.PC = 0xC000;
            cpu.Regs.SP = 0xFFFC; // Set SP with data
            mmu.WriteWord(0xFFFC, 0x1234); // Put data on stack

            mmu.WriteByte(0xC000, opcode);
            var cycles = cpu.Step();

            Assert.Equal(12, cycles);
        }
    }

    #endregion

    #region Instruction Coverage Validation

    [Fact]
    public void AllImplementedInstructions_HaveValidTiming()
    {
        // Verify all implemented instructions have reasonable cycle counts
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.Primary[i];
            if (instruction.HasValue)
            {
                var instr = instruction.Value;

                // Primary instructions should be 4-24 cycles
                Assert.True(instr.BaseCycles >= 4 && instr.BaseCycles <= 24);
            }
        }

        // Check CB instructions
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.CB[i];
            if (instruction.HasValue)
            {
                var instr = instruction.Value;

                // CB instructions should be 8-16 cycles
                Assert.True(instr.BaseCycles >= 8 && instr.BaseCycles <= 16);
            }
        }
    }

    [Fact]
    public void RSTInstructions_Take16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.SP = 0xFFFE;

        // RST instructions: 0xC7, 0xCF, 0xD7, 0xDF, 0xE7, 0xEF, 0xF7, 0xFF
        var rstOpcodes = new List<byte> { 0xC7, 0xCF, 0xD7, 0xDF, 0xE7, 0xEF, 0xF7, 0xFF };

        foreach (var opcode in rstOpcodes)
        {
            cpu.Regs.PC = 0xC000;
            cpu.Regs.SP = 0xFFFE; // Reset SP

            mmu.WriteByte(0xC000, opcode);
            var cycles = cpu.Step();

            Assert.Equal(16, cycles);
        }
    }

    #endregion

    #region Blargg Test ROM Preparation Tests

    [Fact]
    public void CriticalTimingInstructions_AreImplemented()
    {
        // Verify that critical instructions used by Blargg tests are implemented
        var criticalOpcodes = new List<byte>
        {
            0x00, // NOP
            0x76, // HALT
            0xC3, // JP a16
            0xCD, // CALL a16
            0xC9, // RET
            0xF0, // LDH A,(a8)
            0xE0, // LDH (a8),A
            0x3E, // LD A,d8
            0x06, // LD B,d8
            0x0E, // LD C,d8
            0x16, // LD D,d8
            0x1E, // LD E,d8
            0x26, // LD H,d8
            0x2E  // LD L,d8
        };

        foreach (var opcode in criticalOpcodes)
        {
            var instruction = OpcodeTable.Primary[opcode];
            Assert.True(instruction.HasValue);
        }
    }

    #endregion
}