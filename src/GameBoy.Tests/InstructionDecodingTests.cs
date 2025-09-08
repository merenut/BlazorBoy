using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for instruction decoding correctness and robustness.
/// </summary>
public class InstructionDecodingTests
{
    [Fact]
    public void OpcodeTable_Primary_Has256Entries()
    {
        Assert.Equal(256, OpcodeTable.Primary.Length);
    }

    [Fact]
    public void OpcodeTable_CB_Has256Entries()
    {
        Assert.Equal(256, OpcodeTable.CB.Length);
    }

    [Fact]
    public void Primary_KnownOpcodes_AreNotNull()
    {
        // Test some known implemented opcodes
        var knownOpcodes = new byte[] { 0x00, 0x06, 0x0E, 0x16, 0x1E, 0x26, 0x2E, 0x3E, 0x40, 0x41, 0x80, 0x81 };

        foreach (var opcode in knownOpcodes)
        {
            Assert.NotNull(OpcodeTable.Primary[opcode]);
            Assert.NotEmpty(OpcodeTable.Primary[opcode]!.Value.Mnemonic);
            Assert.True(OpcodeTable.Primary[opcode]!.Value.Length > 0);
            Assert.True(OpcodeTable.Primary[opcode]!.Value.BaseCycles > 0);
            Assert.NotNull(OpcodeTable.Primary[opcode]!.Value.Execute);
        }
    }

    [Fact]
    public void Instruction_OperandTypes_AreCorrectlyDeduced()
    {
        // Test that operand types are correctly deduced for various instruction patterns
        var nopInstruction = new Instruction("NOP", 1, 4, cpu => 4);
        Assert.Equal(OperandType.None, nopInstruction.OperandType);

        var ldRegInstruction = new Instruction("LD B,C", 1, 4, cpu => 4);
        Assert.Equal(OperandType.Register, ldRegInstruction.OperandType);

        var ldImmInstruction = new Instruction("LD A,d8", 2, 8, cpu => 8);
        Assert.Equal(OperandType.Immediate8, ldImmInstruction.OperandType);

        var ldMemInstruction = new Instruction("LD A,(HL)", 1, 8, cpu => 8);
        Assert.Equal(OperandType.Memory, ldMemInstruction.OperandType);

        var ldhInstruction = new Instruction("LDH A,(a8)", 2, 12, cpu => 12);
        Assert.Equal(OperandType.MemoryImmediate8, ldhInstruction.OperandType);

        var ldAddrInstruction = new Instruction("LD A,(a16)", 3, 16, cpu => 16);
        Assert.Equal(OperandType.MemoryImmediate16, ldAddrInstruction.OperandType);
    }

    [Fact]
    public void Instruction_ExplicitOperandType_OverridesDeduction()
    {
        var instruction = new Instruction("TEST", 1, 4, OperandType.Immediate16, cpu => 4);
        Assert.Equal(OperandType.Immediate16, instruction.OperandType);
    }

    [Fact]
    public void CPU_Step_HandlesKnownPrimaryOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test NOP (0x00)
        cpu.Regs.PC = 0x8000;
        mmu.WriteByte(0x8000, 0x00);
        var cycles = cpu.Step();
        Assert.Equal(4, cycles);
        Assert.Equal(0x8001, cpu.Regs.PC);
    }

    [Fact]
    public void CPU_Step_HandlesUnknownPrimaryOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test an unimplemented opcode (find one that's null)
        cpu.Regs.PC = 0x8000;

        // Find an unimplemented opcode
        byte unknownOpcode = 0;
        for (byte i = 0; i < 255; i++)
        {
            if (OpcodeTable.Primary[i] == null)
            {
                unknownOpcode = i;
                break;
            }
        }

        // If we found an unknown opcode, test it
        if (OpcodeTable.Primary[unknownOpcode] == null)
        {
            mmu.WriteByte(0x8000, unknownOpcode);
            var cycles = cpu.Step();
            Assert.Equal(4, cycles); // Should default to 4 cycles for unknown opcodes
            Assert.Equal(0x8001, cpu.Regs.PC);
        }
    }

    [Fact]
    public void CPU_Step_HandlesCBPrefixedOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test CB prefix with unknown CB opcode
        cpu.Regs.PC = 0x8000;
        mmu.WriteByte(0x8000, 0xCB); // CB prefix
        mmu.WriteByte(0x8001, 0x00); // Some CB opcode (likely unimplemented)

        var cycles = cpu.Step();
        Assert.Equal(8, cycles); // Should default to 8 cycles for CB opcodes
        Assert.Equal(0x8002, cpu.Regs.PC); // PC should advance by 2
    }

    [Fact]
    public void Instruction_Properties_AreValid()
    {
        // Test that all implemented instructions have valid properties
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.Primary[i];
            if (instruction.HasValue)
            {
                var instr = instruction.Value;

                // Mnemonic should not be empty
                Assert.NotEmpty(instr.Mnemonic);

                // Length should be 1, 2, or 3
                Assert.True(instr.Length >= 1 && instr.Length <= 3,
                    $"Invalid length {instr.Length} for opcode 0x{i:X2}");

                // Base cycles should be positive and reasonable (4-24 cycles typical)
                Assert.True(instr.BaseCycles > 0 && instr.BaseCycles <= 24,
                    $"Invalid cycles {instr.BaseCycles} for opcode 0x{i:X2}");

                // Execute function should not be null
                Assert.NotNull(instr.Execute);

                // Operand type should be valid
                Assert.True(Enum.IsDefined(typeof(OperandType), instr.OperandType),
                    $"Invalid operand type {instr.OperandType} for opcode 0x{i:X2}");
            }
        }
    }

    [Fact]
    public void Instruction_LengthMatchesOperandType()
    {
        // Test that instruction length matches the expected operand type
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.Primary[i];
            if (instruction.HasValue)
            {
                var instr = instruction.Value;

                switch (instr.OperandType)
                {
                    case OperandType.None:
                    case OperandType.Register:
                    case OperandType.Memory:
                        // These should be 1-byte instructions
                        Assert.True(instr.Length == 1,
                            $"Opcode 0x{i:X2} ({instr.Mnemonic}) with operand type {instr.OperandType} should have length 1, but has {instr.Length}");
                        break;

                    case OperandType.Immediate8:
                    case OperandType.MemoryImmediate8:
                    case OperandType.Relative8:
                        // These should be 2-byte instructions
                        Assert.True(instr.Length == 2,
                            $"Opcode 0x{i:X2} ({instr.Mnemonic}) with operand type {instr.OperandType} should have length 2, but has {instr.Length}");
                        break;

                    case OperandType.Immediate16:
                    case OperandType.MemoryImmediate16:
                        // These should be 3-byte instructions
                        Assert.True(instr.Length == 3,
                            $"Opcode 0x{i:X2} ({instr.Mnemonic}) with operand type {instr.OperandType} should have length 3, but has {instr.Length}");
                        break;
                }
            }
        }
    }

    [Theory]
    [InlineData(0x00, "NOP", 1, 4, OperandType.None)]
    [InlineData(0x06, "LD B,d8", 2, 8, OperandType.Immediate8)]
    [InlineData(0x40, "LD B,B", 1, 4, OperandType.Register)]
    [InlineData(0x46, "LD B,(HL)", 1, 8, OperandType.Memory)]
    [InlineData(0x3E, "LD A,d8", 2, 8, OperandType.Immediate8)]
    [InlineData(0xF0, "LDH A,(a8)", 2, 12, OperandType.MemoryImmediate8)]
    [InlineData(0xFA, "LD A,(a16)", 3, 16, OperandType.MemoryImmediate16)]
    public void SpecificInstructions_HaveCorrectMetadata(byte opcode, string expectedMnemonic,
        int expectedLength, int expectedCycles, OperandType expectedOperandType)
    {
        var instruction = OpcodeTable.Primary[opcode];
        Assert.NotNull(instruction);

        var instr = instruction.Value;
        Assert.Equal(expectedMnemonic, instr.Mnemonic);
        Assert.Equal(expectedLength, instr.Length);
        Assert.Equal(expectedCycles, instr.BaseCycles);
        Assert.Equal(expectedOperandType, instr.OperandType);
    }
}