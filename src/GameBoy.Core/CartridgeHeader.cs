using System;

namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy cartridge header and provides parsing functionality.
/// Header is located at ROM addresses 0x0100-0x014F.
/// </summary>
public sealed class CartridgeHeader
{
    // Header field offsets
    private const int TitleOffset = 0x0134;           // 16 bytes (0x0134-0x0143)
    private const int CartridgeTypeOffset = 0x0147;
    private const int RomSizeOffset = 0x0148;
    private const int RamSizeOffset = 0x0149;
    private const int DestinationCodeOffset = 0x014A;
    private const int OldLicenseeCodeOffset = 0x014B;
    private const int MaskRomVersionOffset = 0x014C;
    private const int HeaderChecksumOffset = 0x014D;
    private const int GlobalChecksumOffset = 0x014E; // 2 bytes

    /// <summary>
    /// Cartridge type byte that determines the MBC controller.
    /// </summary>
    public byte CartridgeType { get; }

    /// <summary>
    /// Game title extracted from the header (null-terminated, up to 16 characters).
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// ROM size code (0x00 = 32KB, 0x01 = 64KB, etc.).
    /// </summary>
    public byte RomSizeCode { get; }

    /// <summary>
    /// RAM size code (0x00 = None, 0x01 = 2KB, 0x02 = 8KB, 0x03 = 32KB, 0x04 = 128KB, 0x05 = 64KB).
    /// </summary>
    public byte RamSizeCode { get; }

    /// <summary>
    /// Destination code (0x00 = Japan, 0x01 = Non-Japan).
    /// </summary>
    public byte DestinationCode { get; }

    /// <summary>
    /// Old licensee code.
    /// </summary>
    public byte OldLicenseeCode { get; }

    /// <summary>
    /// Mask ROM version number.
    /// </summary>
    public byte MaskRomVersion { get; }

    /// <summary>
    /// Header checksum.
    /// </summary>
    public byte HeaderChecksum { get; }

    /// <summary>
    /// Global checksum (16-bit).
    /// </summary>
    public ushort GlobalChecksum { get; }

    /// <summary>
    /// ROM size in bytes.
    /// </summary>
    public int RomSize => CalculateRomSize(RomSizeCode);

    /// <summary>
    /// RAM size in bytes.
    /// </summary>
    public int RamSize => CalculateRamSize(RamSizeCode);

    /// <summary>
    /// Number of ROM banks.
    /// </summary>
    public int RomBankCount => RomSize / 0x4000; // Each ROM bank is 16KB

    /// <summary>
    /// Number of RAM banks.
    /// </summary>
    public int RamBankCount => RamSize > 0 ? Math.Max(1, RamSize / 0x2000) : 0; // Each RAM bank is 8KB

    private CartridgeHeader(byte[] rom)
    {
        if (rom == null || rom.Length < 0x0150)
            throw new ArgumentException("ROM too small to contain valid header");

        Title = ExtractTitle(rom);
        CartridgeType = rom[CartridgeTypeOffset];
        RomSizeCode = rom[RomSizeOffset];
        RamSizeCode = rom[RamSizeOffset];
        DestinationCode = rom[DestinationCodeOffset];
        OldLicenseeCode = rom[OldLicenseeCodeOffset];
        MaskRomVersion = rom[MaskRomVersionOffset];
        HeaderChecksum = rom[HeaderChecksumOffset];
        GlobalChecksum = (ushort)((rom[GlobalChecksumOffset + 1] << 8) | rom[GlobalChecksumOffset]);
    }

    /// <summary>
    /// Parses the cartridge header from ROM data.
    /// </summary>
    public static CartridgeHeader Parse(byte[] rom)
    {
        return new CartridgeHeader(rom);
    }

    /// <summary>
    /// Validates the header checksum.
    /// </summary>
    public bool IsHeaderChecksumValid(byte[] rom)
    {
        byte checksum = 0;
        for (int i = 0x0134; i <= 0x014C; i++)
        {
            checksum = (byte)(checksum - rom[i] - 1);
        }
        return checksum == HeaderChecksum;
    }

    /// <summary>
    /// Gets the MBC type from the cartridge type byte.
    /// </summary>
    public MbcType GetMbcType()
    {
        return CartridgeType switch
        {
            0x00 => MbcType.None,           // ROM ONLY
            0x01 => MbcType.Mbc1,          // MBC1
            0x02 => MbcType.Mbc1,          // MBC1+RAM
            0x03 => MbcType.Mbc1,          // MBC1+RAM+BATTERY
            0x05 => MbcType.Mbc2,          // MBC2
            0x06 => MbcType.Mbc2,          // MBC2+BATTERY
            0x0F => MbcType.Mbc3,          // MBC3+TIMER+BATTERY
            0x10 => MbcType.Mbc3,          // MBC3+TIMER+RAM+BATTERY
            0x11 => MbcType.Mbc3,          // MBC3
            0x12 => MbcType.Mbc3,          // MBC3+RAM
            0x13 => MbcType.Mbc3,          // MBC3+RAM+BATTERY
            0x15 => MbcType.Mbc4,          // MBC4 (known but unsupported)
            0x16 => MbcType.Mbc4,          // MBC4+RAM (known but unsupported)
            0x17 => MbcType.Mbc4,          // MBC4+RAM+BATTERY (known but unsupported)
            0x19 => MbcType.Mbc5,          // MBC5
            0x1A => MbcType.Mbc5,          // MBC5+RAM
            0x1B => MbcType.Mbc5,          // MBC5+RAM+BATTERY
            0x1C => MbcType.Mbc5,          // MBC5+RUMBLE
            0x1D => MbcType.Mbc5,          // MBC5+RUMBLE+RAM
            0x1E => MbcType.Mbc5,          // MBC5+RUMBLE+RAM+BATTERY
            0x20 => MbcType.Mbc6,          // MBC6 (known but unsupported)
            0x22 => MbcType.Mbc7,          // MBC7+SENSOR+RUMBLE+RAM+BATTERY (known but unsupported)
            0xFF => MbcType.HuC1,          // HuC1+RAM+BATTERY (known but unsupported)
            0xFE => MbcType.HuC3,          // HuC3+RAM+BATTERY+RTC (known but unsupported)
            0xFD => MbcType.Tama5,         // TAMA5 (known but unsupported)
            _ => MbcType.Unknown
        };
    }

    /// <summary>
    /// Checks if the cartridge has battery-backed RAM.
    /// </summary>
    public bool HasBattery()
    {
        return CartridgeType switch
        {
            0x03 => true,  // MBC1+RAM+BATTERY
            0x06 => true,  // MBC2+BATTERY
            0x0F => true,  // MBC3+TIMER+BATTERY
            0x10 => true,  // MBC3+TIMER+RAM+BATTERY
            0x13 => true,  // MBC3+RAM+BATTERY
            0x1B => true,  // MBC5+RAM+BATTERY
            0x1E => true,  // MBC5+RUMBLE+RAM+BATTERY
            _ => false
        };
    }

    /// <summary>
    /// Checks if the cartridge has a Real Time Clock.
    /// </summary>
    public bool HasRtc()
    {
        return CartridgeType switch
        {
            0x0F => true,  // MBC3+TIMER+BATTERY
            0x10 => true,  // MBC3+TIMER+RAM+BATTERY
            _ => false
        };
    }

    private static int CalculateRomSize(byte code)
    {
        return code switch
        {
            0x00 => 32 * 1024,    // 32KB (2 banks)
            0x01 => 64 * 1024,    // 64KB (4 banks)
            0x02 => 128 * 1024,   // 128KB (8 banks)
            0x03 => 256 * 1024,   // 256KB (16 banks)
            0x04 => 512 * 1024,   // 512KB (32 banks)
            0x05 => 1024 * 1024,  // 1MB (64 banks)
            0x06 => 2048 * 1024,  // 2MB (128 banks)
            0x07 => 4096 * 1024,  // 4MB (256 banks)
            0x08 => 8192 * 1024,  // 8MB (512 banks)
            0x52 => 1152 * 1024,  // 1.1MB (72 banks)
            0x53 => 1280 * 1024,  // 1.2MB (80 banks)
            0x54 => 1536 * 1024,  // 1.5MB (96 banks)
            _ => 32 * 1024        // Default to 32KB for unknown codes
        };
    }

    private static int CalculateRamSize(byte code)
    {
        return code switch
        {
            0x00 => 0,            // No RAM
            0x01 => 2 * 1024,     // 2KB (1 bank)
            0x02 => 8 * 1024,     // 8KB (1 bank)
            0x03 => 32 * 1024,    // 32KB (4 banks)
            0x04 => 128 * 1024,   // 128KB (16 banks)
            0x05 => 64 * 1024,    // 64KB (8 banks)
            _ => 0                // Default to no RAM for unknown codes
        };
    }

    /// <summary>
    /// Extracts the title from the ROM header.
    /// </summary>
    private static string ExtractTitle(byte[] rom)
    {
        // Extract title bytes (0x0134-0x0143, 16 bytes max)
        var titleBytes = new byte[16];
        Array.Copy(rom, TitleOffset, titleBytes, 0, 16);

        // Find null terminator or use full length
        int length = 0;
        for (int i = 0; i < titleBytes.Length; i++)
        {
            if (titleBytes[i] == 0)
                break;
            length++;
        }

        // Convert to ASCII string, handling non-printable characters
        var titleChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            char c = (char)titleBytes[i];
            // Replace non-printable characters with space
            titleChars[i] = char.IsControl(c) ? ' ' : c;
        }

        return new string(titleChars).TrimEnd();
    }

    /// <summary>
    /// Validates that the ROM header contains reasonable values.
    /// </summary>
    public bool IsValidHeader()
    {
        // Check if ROM size code is valid
        if (RomSizeCode > 0x08 && RomSizeCode != 0x52 && RomSizeCode != 0x53 && RomSizeCode != 0x54)
            return false;

        // Check if RAM size code is valid
        if (RamSizeCode > 0x05)
            return false;

        // Check if destination code is valid
        if (DestinationCode > 0x01)
            return false;

        return true;
    }
}

/// <summary>
/// Enumeration of supported MBC types.
/// </summary>
public enum MbcType
{
    None,
    Mbc1,
    Mbc2,
    Mbc3,
    Mbc5,
    Mbc4,      // Known but unsupported
    Mbc6,      // Known but unsupported
    Mbc7,      // Known but unsupported
    HuC1,      // Known but unsupported
    HuC3,      // Known but unsupported
    Tama5,     // Known but unsupported
    Unknown
}