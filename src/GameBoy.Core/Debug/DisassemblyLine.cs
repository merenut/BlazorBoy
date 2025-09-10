using System;

namespace GameBoy.Core.Debug;

/// <summary>
/// Represents a single disassembled instruction line.
/// </summary>
public readonly struct DisassemblyLine
{
    public ushort Address { get; }
    public byte[] Bytes { get; }
    public string Mnemonic { get; }
    public string Operands { get; }
    public int Length { get; }
    public bool IsValid { get; }

    public DisassemblyLine(ushort address, byte[] bytes, string mnemonic, string operands = "")
    {
        Address = address;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Mnemonic = mnemonic ?? throw new ArgumentNullException(nameof(mnemonic));
        Operands = operands ?? "";
        Length = bytes.Length;
        IsValid = true;
    }

    public static DisassemblyLine Invalid(ushort address, byte opcode)
    {
        return new DisassemblyLine(
            address: address,
            bytes: new[] { opcode },
            mnemonic: "???",
            operands: $"0x{opcode:X2}",
            length: 1,
            isValid: false
        );
    }

    private DisassemblyLine(ushort address, byte[] bytes, string mnemonic, string operands, int length, bool isValid)
    {
        Address = address;
        Bytes = bytes;
        Mnemonic = mnemonic;
        Operands = operands;
        Length = length;
        IsValid = isValid;
    }

    public string GetBytesString()
    {
        if (Bytes == null || Bytes.Length == 0) return "";
        
        var result = "";
        for (int i = 0; i < Bytes.Length; i++)
        {
            if (i > 0) result += " ";
            result += $"{Bytes[i]:X2}";
        }
        return result;
    }

    public override string ToString()
    {
        var bytesStr = GetBytesString().PadRight(8);
        var fullInstruction = string.IsNullOrEmpty(Operands) ? Mnemonic : $"{Mnemonic} {Operands}";
        return $"{Address:X4}: {bytesStr} {fullInstruction}";
    }
}