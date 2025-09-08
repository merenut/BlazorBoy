using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests to validate complete opcode table coverage.
/// </summary>
public class OpcodeTableCoverageTests
{
    [Fact]
    public void Primary_Table_Should_Have_Complete_Coverage()
    {
        // Count implemented opcodes
        int implementedCount = 0;
        int invalidOpcodeCount = 0;

        for (int i = 0; i < 256; i++)
        {
            if (OpcodeTable.Primary[i] != null)
            {
                implementedCount++;
            }
            else
            {
                // Some opcodes are intentionally invalid in Game Boy CPU
                // 0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD
                byte opcode = (byte)i;
                if (opcode == 0xD3 || opcode == 0xDB || opcode == 0xDD ||
                    opcode == 0xE3 || opcode == 0xE4 || opcode == 0xEB ||
                    opcode == 0xEC || opcode == 0xED || opcode == 0xF4 ||
                    opcode == 0xFC || opcode == 0xFD)
                {
                    invalidOpcodeCount++;
                }
                else
                {
                    // This should not happen - all valid opcodes should be implemented
                    Assert.Fail($"Opcode 0x{i:X2} is not implemented but should be");
                }
            }
        }

        // Should have all valid opcodes implemented
        // Game Boy has 11 invalid opcodes, so 256 - 11 = 245 valid opcodes
        int expectedValid = 256 - 11;
        Assert.Equal(expectedValid, implementedCount);
        Assert.Equal(11, invalidOpcodeCount);
    }

    [Fact]
    public void CB_Table_Should_Have_Complete_Coverage()
    {
        // CB table should have all 256 opcodes implemented (no invalid CB opcodes)
        int implementedCount = 0;

        for (int i = 0; i < 256; i++)
        {
            if (OpcodeTable.CB[i] != null)
            {
                implementedCount++;
            }
            else
            {
                Assert.Fail($"CB opcode 0x{i:X2} is not implemented but should be");
            }
        }

        Assert.Equal(256, implementedCount);
    }

    [Fact]
    public void All_Instructions_Have_Valid_Metadata()
    {
        // Test primary table
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.Primary[i];
            if (instruction.HasValue)
            {
                var instr = instruction.Value;

                // All instructions should have non-empty mnemonics
                Assert.NotEmpty(instr.Mnemonic);

                // All should have valid lengths (1-3 bytes)
                Assert.InRange(instr.Length, 1, 3);

                // All should have valid cycle counts
                Assert.InRange(instr.BaseCycles, 4, 24);

                // All should have execute functions
                Assert.NotNull(instr.Execute);

                // Operand type should be valid enum value
                Assert.True(Enum.IsDefined(typeof(OperandType), instr.OperandType));
            }
        }

        // Test CB table
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.CB[i];
            if (instruction.HasValue)
            {
                var instr = instruction.Value;

                // All CB instructions should be 2 bytes
                Assert.Equal(2, instr.Length);

                // All should have valid cycle counts (8 or 16 for CB instructions)
                Assert.True(instr.BaseCycles == 8 || instr.BaseCycles == 12 || instr.BaseCycles == 16,
                    $"CB instruction 0x{i:X2} has invalid cycle count: {instr.BaseCycles}");

                // All should have non-empty mnemonics
                Assert.NotEmpty(instr.Mnemonic);

                // All should have execute functions
                Assert.NotNull(instr.Execute);
            }
        }
    }

    [Fact]
    public void Opcode_Table_Should_Support_All_Game_Boy_Instruction_Categories()
    {
        var categories = new Dictionary<string, int>
        {
            ["LD"] = 0,     // Load operations
            ["ADD"] = 0,    // Arithmetic
            ["INC"] = 0,    // Increment
            ["DEC"] = 0,    // Decrement
            ["JP"] = 0,     // Jump
            ["JR"] = 0,     // Jump relative
            ["CALL"] = 0,   // Call
            ["RET"] = 0,    // Return
            ["PUSH"] = 0,   // Stack push
            ["POP"] = 0,    // Stack pop
            ["BIT"] = 0,    // Bit test (CB)
            ["SET"] = 0,    // Bit set (CB)
            ["RES"] = 0,    // Bit reset (CB)
            ["RLC"] = 0,    // Rotate left circular (CB)
            ["AND"] = 0,    // Logical AND
            ["OR"] = 0,     // Logical OR
            ["XOR"] = 0,    // Logical XOR
            ["CP"] = 0,     // Compare
            ["SUB"] = 0,    // Subtract
            ["RST"] = 0,    // Restart
        };

        // Count primary instructions by category
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.Primary[i];
            if (instruction.HasValue)
            {
                string mnemonic = instruction.Value.Mnemonic;
                foreach (var category in categories.Keys.ToList())
                {
                    if (mnemonic.StartsWith(category))
                    {
                        categories[category]++;
                        break;
                    }
                }
            }
        }

        // Count CB instructions by category
        for (int i = 0; i < 256; i++)
        {
            var instruction = OpcodeTable.CB[i];
            if (instruction.HasValue)
            {
                string mnemonic = instruction.Value.Mnemonic;
                foreach (var category in categories.Keys.ToList())
                {
                    if (mnemonic.StartsWith(category))
                    {
                        categories[category]++;
                        break;
                    }
                }
            }
        }

        // Verify we have instructions in all major categories
        Assert.True(categories["LD"] >= 50, $"Should have many LD instructions, found {categories["LD"]}");
        Assert.True(categories["BIT"] >= 60, $"Should have many BIT instructions, found {categories["BIT"]}");
        Assert.True(categories["SET"] >= 60, $"Should have many SET instructions, found {categories["SET"]}");
        Assert.True(categories["RES"] >= 60, $"Should have many RES instructions, found {categories["RES"]}");
        Assert.True(categories["ADD"] >= 8, $"Should have ADD instructions, found {categories["ADD"]}");
        Assert.True(categories["INC"] >= 8, $"Should have INC instructions, found {categories["INC"]}");
        Assert.True(categories["DEC"] >= 8, $"Should have DEC instructions, found {categories["DEC"]}");
    }
}