using GameBoy.Core;

namespace GameBoy.Tests;

public class DebugStackTests
{
    [Fact]
    public void DebugStackOperations()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        
        // Test stack operations by using push/pop via instructions
        cpu.Regs.SP = 0xFFFE;
        cpu.Regs.PC = 0xC000;
        cpu.Regs.BC = 0x1234;
        
        // PUSH BC instruction (0xC5)
        mmu.WriteByte(0xC000, 0xC5);
        cpu.Step();
        
        // Should have pushed 0x1234 onto stack
        Assert.Equal(0xFFFC, cpu.Regs.SP);
        Assert.Equal(0x1234, mmu.ReadWord(0xFFFC));
        
        // POP BC instruction (0xC1) 
        mmu.WriteByte(0xC001, 0xC1);
        cpu.Regs.BC = 0x0000; // Clear BC
        cpu.Step();
        
        // Should have popped 0x1234 back into BC
        Assert.Equal(0x1234, cpu.Regs.BC);
        Assert.Equal(0xFFFE, cpu.Regs.SP);
    }
    
    [Fact]
    public void DebugMemoryReads()
    {
        var mmu = new Mmu();
        
        // Test memory operations
        mmu.WriteByte(0xFFFC, 0x34);
        mmu.WriteByte(0xFFFD, 0x12);
        
        var readWord = mmu.ReadWord(0xFFFC);
        Assert.Equal(0x1234, readWord);
        
        // Test at a different location
        mmu.WriteByte(0x8000, 0x34);
        mmu.WriteByte(0x8001, 0x12);
        
        var readWord2 = mmu.ReadWord(0x8000);
        Assert.Equal(0x1234, readWord2);
    }
    
    [Fact]
    public void DebugSimpleRET()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        
        // Test a simple RET instruction (0xC9) to see if PopStack works
        cpu.Regs.SP = 0xFFFC;
        cpu.Regs.PC = 0x8000;
        
        // Put return address on stack
        mmu.WriteByte(0xFFFC, 0x34); // Low byte of 0x1234
        mmu.WriteByte(0xFFFD, 0x12); // High byte of 0x1234
        
        mmu.WriteByte(0x8000, 0xC9); // RET
        
        // Execute RET
        int cycles = cpu.Step();
        
        // Check RET behavior (should be same as RETI but without IME enable)
        Assert.Equal(0x1234, cpu.Regs.PC);
        Assert.Equal(0xFFFE, cpu.Regs.SP);
        Assert.Equal(16, cycles); // RET takes 16 cycles
    }
    
    [Fact]
    public void DebugVectorAddressMemory()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        
        // Test reading/writing at interrupt vector address
        mmu.WriteByte(0x0040, 0xD9); // Write RETI
        var readBack = mmu.ReadByte(0x0040);
        
        // What do we actually read back?
        Assert.True(readBack == 0xD9, $"Expected 0xD9 but read 0x{readBack:X2}");
        
        // Test the same thing at a different vector address
        mmu.WriteByte(0x0048, 0xD9); // Write RETI at LCD STAT vector
        var readBack2 = mmu.ReadByte(0x0048);
        Assert.True(readBack2 == 0xD9, $"Expected 0xD9 but read 0x{readBack2:X2}");
        
        // Test at 0x0038 specifically (the value we're getting)
        mmu.WriteByte(0x0038, 0xAB); // Write something distinctive  
        var readBack3 = mmu.ReadByte(0x0038);
        Assert.True(readBack3 == 0xAB, $"Expected 0xAB but read 0x{readBack3:X2}");
        
        // Now check what happens when we read from 0x0040 again
        var readAgain = mmu.ReadByte(0x0040);
        Assert.True(readAgain == 0xD9, $"Expected 0xD9 but read 0x{readAgain:X2} when reading 0x0040 again");
    }
}