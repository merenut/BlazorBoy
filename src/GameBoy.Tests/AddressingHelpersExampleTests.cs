using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Demonstrates real-world usage of addressing helpers in Game Boy instruction sequences.
/// </summary>
public class AddressingHelpersExampleTests
{
    [Fact]
    public void Example_CopyArrayUsingHLIncrement()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Setup: Source array at 0xD000, destination at 0xD100
        var sourceData = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };
        for (int i = 0; i < sourceData.Length; i++)
        {
            mmu.WriteByte((ushort)(0xD000 + i), sourceData[i]);
        }

        // Simulate copying 5 bytes using LD A,(HL+) and LD (DE),A with helper-based opcodes
        cpu.Regs.HL = 0xD000; // Source pointer
        cpu.Regs.DE = 0xD100; // Destination pointer
        cpu.Regs.PC = 0xC000;

        // Prepare instruction sequence
        for (int i = 0; i < 5; i++)
        {
            // LD A,(HL+) - 0x2A
            mmu.WriteByte((ushort)(0xC000 + i * 2), 0x2A);
            // LD (DE),A - 0x12  
            mmu.WriteByte((ushort)(0xC001 + i * 2), 0x12);
        }

        // Execute copy loop
        for (int i = 0; i < 5; i++)
        {
            // LD A,(HL+) - uses ReadHLI helper
            cpu.Step();
            Assert.Equal(sourceData[i], cpu.Regs.A);
            Assert.Equal((ushort)(0xD001 + i), cpu.Regs.HL); // HL incremented

            // LD (DE),A - uses WriteDE helper  
            cpu.Step();
            Assert.Equal(sourceData[i], mmu.ReadByte((ushort)(0xD100 + i)));

            // Manually increment DE (would be done by a real INC DE instruction)
            cpu.Regs.DE++;
        }

        // Verify all data was copied correctly
        for (int i = 0; i < sourceData.Length; i++)
        {
            Assert.Equal(sourceData[i], mmu.ReadByte((ushort)(0xD100 + i)));
        }
    }

    [Fact]
    public void Example_HighRAMAccess()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Demonstrate accessing I/O registers via high RAM addressing
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0xFC; // BGP palette value

        // LDH (a8),A - write A to 0xFF00 + immediate
        mmu.WriteByte(0xC000, 0xE0); // LDH (a8),A opcode
        mmu.WriteByte(0xC001, 0x47); // Offset for BGP register (0xFF47)

        cpu.Step(); // Execute LDH (a8),A - uses WriteHighImm8 helper

        // Verify the I/O register was written
        Assert.Equal(0xFC, mmu.ReadByte(0xFF47)); // BGP register
        Assert.Equal(0xC002, cpu.Regs.PC);

        // Now read it back using register C addressing
        cpu.Regs.C = 0x47; // Point to BGP register
        mmu.WriteByte(0xC002, 0xF2); // LD A,(C) opcode

        cpu.Step(); // Execute LD A,(C) - uses ReadHighC helper

        Assert.Equal(0xFC, cpu.Regs.A); // Should read back the same value
    }
}