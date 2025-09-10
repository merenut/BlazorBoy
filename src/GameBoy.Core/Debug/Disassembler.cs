using System;
using System.Collections.Generic;

namespace GameBoy.Core.Debug;

/// <summary>
/// Interface for reading memory during disassembly.
/// </summary>
public interface IMemoryReader
{
    byte ReadByte(ushort address);
}

/// <summary>
/// Game Boy CPU disassembler that converts machine code to human-readable assembly.
/// </summary>
public sealed class Disassembler
{
    /// <summary>
    /// Disassembles a single instruction at the specified address.
    /// </summary>
    public static DisassemblyLine DisassembleAt(ushort address, IMemoryReader memory)
    {
        try
        {
            byte opcode = memory.ReadByte(address);

            // Handle CB-prefixed instructions
            if (opcode == 0xCB)
            {
                if (address + 1 > 0xFFFF)
                {
                    // Address wrap-around, invalid instruction
                    return DisassemblyLine.Invalid(address, opcode);
                }

                byte cbOpcode = memory.ReadByte((ushort)(address + 1));
                var cbInstruction = OpcodeTable.CB[cbOpcode];

                if (cbInstruction == null)
                {
                    return DisassemblyLine.Invalid(address, opcode);
                }

                var cbBytes = new byte[] { opcode, cbOpcode };
                return new DisassemblyLine(address, cbBytes, cbInstruction.Value.Mnemonic);
            }

            // Handle primary instructions
            var instruction = OpcodeTable.Primary[opcode];
            if (instruction == null)
            {
                return DisassemblyLine.Invalid(address, opcode);
            }

            var inst = instruction.Value;
            var bytes = new List<byte> { opcode };
            string operands = "";

            // Read additional bytes and format operands based on instruction length
            if (inst.Length >= 2)
            {
                if (address + 1 > 0xFFFF)
                {
                    return DisassemblyLine.Invalid(address, opcode);
                }

                byte operand1 = memory.ReadByte((ushort)(address + 1));
                bytes.Add(operand1);

                if (inst.Length == 3)
                {
                    if (address + 2 > 0xFFFF)
                    {
                        return DisassemblyLine.Invalid(address, opcode);
                    }

                    byte operand2 = memory.ReadByte((ushort)(address + 2));
                    bytes.Add(operand2);

                    ushort word = (ushort)(operand1 | (operand2 << 8));
                    operands = FormatOperands(inst.Mnemonic, operand1, word);
                }
                else
                {
                    operands = FormatOperands(inst.Mnemonic, operand1, null);
                }
            }

            // Split mnemonic and operands if not already formatted
            string mnemonic;
            if (string.IsNullOrEmpty(operands))
            {
                var parts = inst.Mnemonic.Split(' ', 2);
                mnemonic = parts[0];
                if (parts.Length > 1)
                {
                    operands = parts[1];
                }
            }
            else
            {
                var parts = inst.Mnemonic.Split(' ', 2);
                mnemonic = parts[0];
            }

            return new DisassemblyLine(address, bytes.ToArray(), mnemonic, operands);
        }
        catch
        {
            // If anything goes wrong, return invalid instruction
            return DisassemblyLine.Invalid(address, 0x00);
        }
    }

    /// <summary>
    /// Disassembles a range of instructions starting at the specified address.
    /// </summary>
    public static IReadOnlyList<DisassemblyLine> DisassembleRange(ushort startAddress, int count, IMemoryReader memory)
    {
        var result = new List<DisassemblyLine>();
        ushort currentAddress = startAddress;

        for (int i = 0; i < count; i++)
        {
            var line = DisassembleAt(currentAddress, memory);
            result.Add(line);

            // Move to next instruction
            currentAddress = (ushort)(currentAddress + line.Length);

            // Handle address wrap-around
            if (currentAddress < startAddress)
            {
                break; // Wrapped around, stop disassembly
            }
        }

        return result;
    }

    /// <summary>
    /// Formats operands for display, replacing placeholders with actual values.
    /// </summary>
    private static string FormatOperands(string mnemonic, byte operand1, ushort? operand16)
    {
        string operands = "";

        // Extract operands from mnemonic if it contains them
        var parts = mnemonic.Split(' ', 2);
        if (parts.Length > 1)
        {
            operands = parts[1];
        }

        if (operand16.HasValue)
        {
            // 16-bit operand
            operands = operands.Replace("d16", $"0x{operand16.Value:X4}");
            operands = operands.Replace("a16", $"0x{operand16.Value:X4}");
            operands = operands.Replace("(a16)", $"(0x{operand16.Value:X4})");
        }
        else
        {
            // 8-bit operand
            operands = operands.Replace("d8", $"0x{operand1:X2}");
            operands = operands.Replace("a8", $"0x{operand1:X2}");
            operands = operands.Replace("(a8)", $"(0x{operand1:X2})");

            // Handle relative jumps (signed 8-bit)
            if (operands.Contains("r8"))
            {
                sbyte relativeJump = (sbyte)operand1;
                operands = operands.Replace("r8", $"{relativeJump:+0;-#}");
            }
        }

        return operands;
    }
}