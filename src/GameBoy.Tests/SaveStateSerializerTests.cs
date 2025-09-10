using Xunit;
using GameBoy.Core.Persistence;
using System;
using System.Text;

namespace GameBoy.Tests;

/// <summary>
/// Tests for save state serialization functionality.
/// </summary>
public class SaveStateSerializerTests
{
    [Fact]
    public void Serialize_ValidSaveState_ReturnsCompressedData()
    {
        // Arrange
        var saveState = CreateTestSaveState();

        // Act
        byte[] serialized = SaveStateSerializer.Serialize(saveState);

        // Assert
        Assert.NotNull(serialized);
        Assert.True(serialized.Length > 10); // Should have header + data + checksum
        
        // First byte should be magic number
        Assert.Equal(0x47, serialized[0]); // 'G' for Game Boy
        Assert.Equal(1, serialized[1]); // Version
    }

    [Fact]
    public void Deserialize_ValidData_ReturnsOriginalSaveState()
    {
        // Arrange
        var originalSaveState = CreateTestSaveState();
        byte[] serialized = SaveStateSerializer.Serialize(originalSaveState);

        // Act
        SaveState deserialized = SaveStateSerializer.Deserialize(serialized);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(originalSaveState.Version, deserialized.Version);
        Assert.Equal(originalSaveState.CartridgeTitle, deserialized.CartridgeTitle);
        Assert.Equal(originalSaveState.CartridgeHash, deserialized.CartridgeHash);
        Assert.Equal(originalSaveState.Cpu.PC, deserialized.Cpu.PC);
        Assert.Equal(originalSaveState.Cpu.SP, deserialized.Cpu.SP);
        Assert.Equal(originalSaveState.Cpu.AF, deserialized.Cpu.AF);
        Assert.Equal(originalSaveState.Timer.DIV, deserialized.Timer.DIV);
        Assert.Equal(originalSaveState.Ppu.LCDC, deserialized.Ppu.LCDC);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var originalSaveState = CreateTestSaveState();

        // Act
        byte[] serialized = SaveStateSerializer.Serialize(originalSaveState);
        SaveState roundTrip = SaveStateSerializer.Deserialize(serialized);

        // Assert - Key data should match exactly
        Assert.Equal(originalSaveState.WorkRam.Length, roundTrip.WorkRam.Length);
        Assert.Equal(originalSaveState.VideoRam.Length, roundTrip.VideoRam.Length);
        Assert.Equal(originalSaveState.OamRam.Length, roundTrip.OamRam.Length);
        Assert.Equal(originalSaveState.HighRam.Length, roundTrip.HighRam.Length);
        Assert.Equal(originalSaveState.IoRegisters.Count, roundTrip.IoRegisters.Count);
    }

    [Fact]
    public void Deserialize_CorruptedChecksum_ThrowsException()
    {
        // Arrange
        var saveState = CreateTestSaveState();
        byte[] serialized = SaveStateSerializer.Serialize(saveState);
        
        // Corrupt the last byte (part of checksum)
        serialized[^1] = (byte)(serialized[^1] ^ 0xFF);

        // Act & Assert
        Assert.Throws<System.IO.InvalidDataException>(() => SaveStateSerializer.Deserialize(serialized));
    }

    [Fact]
    public void Deserialize_InvalidMagicNumber_ThrowsException()
    {
        // Arrange
        var saveState = CreateTestSaveState();
        byte[] serialized = SaveStateSerializer.Serialize(saveState);
        
        // Remove checksum first to avoid checksum error
        byte[] dataWithoutChecksum = new byte[serialized.Length - 4];
        Array.Copy(serialized, 0, dataWithoutChecksum, 0, dataWithoutChecksum.Length);
        
        // Corrupt magic number
        dataWithoutChecksum[0] = 0x00;
        
        // Recalculate checksum
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(dataWithoutChecksum);
            byte[] checksum = new byte[4];
            Array.Copy(hash, 0, checksum, 0, 4);
            
            byte[] corruptedData = new byte[dataWithoutChecksum.Length + 4];
            Array.Copy(dataWithoutChecksum, 0, corruptedData, 0, dataWithoutChecksum.Length);
            Array.Copy(checksum, 0, corruptedData, dataWithoutChecksum.Length, 4);
            serialized = corruptedData;
        }

        // Act & Assert
        Assert.Throws<System.IO.InvalidDataException>(() => SaveStateSerializer.Deserialize(serialized));
    }

    [Fact]
    public void Deserialize_NullData_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SaveStateSerializer.Deserialize(null!));
    }

    [Fact]
    public void Deserialize_EmptyData_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SaveStateSerializer.Deserialize(Array.Empty<byte>()));
    }

    [Fact]
    public void Deserialize_TooShortData_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SaveStateSerializer.Deserialize(new byte[5]));
    }

    [Fact]
    public void GetSaveStateInfo_ValidData_ReturnsCorrectInfo()
    {
        // Arrange
        var saveState = CreateTestSaveState();
        byte[] serialized = SaveStateSerializer.Serialize(saveState);

        // Act
        SaveStateInfo info = SaveStateSerializer.GetSaveStateInfo(serialized);

        // Assert
        Assert.True(info.IsValid);
        Assert.Equal(1, info.Version);
        Assert.Equal(serialized.Length, info.CompressedSize);
        Assert.True(info.UncompressedSize > 0);
        Assert.True(info.CompressionRatio > 0 && info.CompressionRatio <= 1);
    }

    [Fact]
    public void ValidateCompatibility_SameRom_ReturnsTrue()
    {
        // Arrange
        byte[] testRom = CreateTestRom();
        var saveState = CreateTestSaveState();
        
        // Set correct hash
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            saveState.CartridgeHash = sha256.ComputeHash(testRom[..Math.Min(testRom.Length, 32768)]);
        }

        // Act
        bool isCompatible = SaveStateSerializer.ValidateCompatibility(saveState, testRom);

        // Assert
        Assert.True(isCompatible);
    }

    [Fact]
    public void ValidateCompatibility_DifferentRom_ReturnsFalse()
    {
        // Arrange
        byte[] testRom1 = CreateTestRom();
        byte[] testRom2 = CreateTestRom();
        testRom2[0x100] = 0xFF; // Make it different
        
        var saveState = CreateTestSaveState();
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            saveState.CartridgeHash = sha256.ComputeHash(testRom1[..Math.Min(testRom1.Length, 32768)]);
        }

        // Act
        bool isCompatible = SaveStateSerializer.ValidateCompatibility(saveState, testRom2);

        // Assert
        Assert.False(isCompatible);
    }

    private static SaveState CreateTestSaveState()
    {
        return new SaveState
        {
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CartridgeTitle = "TESTGAME",
            CartridgeHash = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
            Cpu = new CpuState
            {
                PC = 0x0100,
                SP = 0xFFFE,
                AF = 0x01B0,
                BC = 0x0013,
                DE = 0x00D8,
                HL = 0x014D,
                InterruptsEnabled = true,
                IsHalted = false
            },
            WorkRam = new byte[0x2000], // 8KB
            VideoRam = new byte[0x2000], // 8KB
            OamRam = new byte[0xA0], // 160 bytes
            HighRam = new byte[0x7F], // 127 bytes
            Timer = new TimerState
            {
                DIV = 0xAB,
                TIMA = 0x00,
                TMA = 0x00,
                TAC = 0x00
            },
            Ppu = new PpuState
            {
                LCDC = 0x91,
                STAT = 0x85,
                SCY = 0x00,
                SCX = 0x00,
                LY = 0x00,
                BGP = 0xFC
            },
            IoRegisters = new System.Collections.Generic.Dictionary<ushort, byte>
            {
                { 0xFF00, 0xFF }, // JOYP
                { 0xFF01, 0x00 }, // Serial data
                { 0xFF02, 0x7E }  // Serial control
            }
        };
    }

    private static byte[] CreateTestRom()
    {
        byte[] rom = new byte[0x8000]; // 32KB ROM
        
        // Set title
        byte[] title = Encoding.ASCII.GetBytes("TESTGAME");
        Array.Copy(title, 0, rom, 0x134, title.Length);
        
        // Set some ROM data
        rom[0x100] = 0x00; // NOP
        rom[0x101] = 0xC3; // JP
        rom[0x102] = 0x50;
        rom[0x103] = 0x01;
        
        return rom;
    }
}