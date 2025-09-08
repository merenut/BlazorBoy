namespace GameBoy.Core;

/// <summary>
/// ALU helpers for flag calculation according to LR35902 specifications.
/// Provides reusable, cycle-accurate flag computation for all ALU operations.
/// </summary>
public static class Alu
{
    /// <summary>
    /// Represents the result of an ALU operation including computed flags.
    /// </summary>
    public readonly struct AluResult
    {
        public readonly byte Result;
        public readonly bool Zero;
        public readonly bool Negative;
        public readonly bool HalfCarry;
        public readonly bool Carry;

        public AluResult(byte result, bool zero, bool negative, bool halfCarry, bool carry)
        {
            Result = result;
            Zero = zero;
            Negative = negative;
            HalfCarry = halfCarry;
            Carry = carry;
        }
    }

    /// <summary>
    /// ADD operation: A + operand
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Set if carry from bit 3
    /// C: Set if carry from bit 7
    /// </summary>
    public static AluResult Add(byte a, byte operand)
    {
        int result = a + operand;

        bool zero = (result & 0xFF) == 0;
        bool negative = false; // Always reset for ADD
        bool halfCarry = (a & 0x0F) + (operand & 0x0F) > 0x0F;
        bool carry = result > 0xFF;

        return new AluResult((byte)(result & 0xFF), zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// ADC operation: A + operand + carry
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Set if carry from bit 3
    /// C: Set if carry from bit 7
    /// </summary>
    public static AluResult AddWithCarry(byte a, byte operand, bool carryIn)
    {
        int carryValue = carryIn ? 1 : 0;
        int result = a + operand + carryValue;

        bool zero = (result & 0xFF) == 0;
        bool negative = false; // Always reset for ADC
        bool halfCarry = (a & 0x0F) + (operand & 0x0F) + carryValue > 0x0F;
        bool carry = result > 0xFF;

        return new AluResult((byte)(result & 0xFF), zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// SUB operation: A - operand
    /// Z: Set if result is 0
    /// N: Set (1)
    /// H: Set if no borrow from bit 4
    /// C: Set if no borrow (A < operand)
    /// </summary>
    public static AluResult Subtract(byte a, byte operand)
    {
        int result = a - operand;

        bool zero = (result & 0xFF) == 0;
        bool negative = true; // Always set for SUB
        bool halfCarry = (a & 0x0F) < (operand & 0x0F); // No borrow from bit 4
        bool carry = a < operand; // No borrow (underflow)

        return new AluResult((byte)(result & 0xFF), zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// SBC operation: A - operand - carry
    /// Z: Set if result is 0
    /// N: Set (1)
    /// H: Set if no borrow from bit 4
    /// C: Set if no borrow (underflow)
    /// </summary>
    public static AluResult SubtractWithCarry(byte a, byte operand, bool carryIn)
    {
        int carryValue = carryIn ? 1 : 0;
        int result = a - operand - carryValue;

        bool zero = (result & 0xFF) == 0;
        bool negative = true; // Always set for SBC
        bool halfCarry = (a & 0x0F) < (operand & 0x0F) + carryValue; // No borrow from bit 4
        bool carry = a < operand + carryValue; // No borrow (underflow)

        return new AluResult((byte)(result & 0xFF), zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// AND operation: A & operand
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Set (1)
    /// C: Reset (0)
    /// </summary>
    public static AluResult And(byte a, byte operand)
    {
        byte result = (byte)(a & operand);

        bool zero = result == 0;
        bool negative = false; // Always reset for AND
        bool halfCarry = true; // Always set for AND
        bool carry = false; // Always reset for AND

        return new AluResult(result, zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// OR operation: A | operand
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Reset (0)
    /// C: Reset (0)
    /// </summary>
    public static AluResult Or(byte a, byte operand)
    {
        byte result = (byte)(a | operand);

        bool zero = result == 0;
        bool negative = false; // Always reset for OR
        bool halfCarry = false; // Always reset for OR
        bool carry = false; // Always reset for OR

        return new AluResult(result, zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// XOR operation: A ^ operand
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Reset (0)
    /// C: Reset (0)
    /// </summary>
    public static AluResult Xor(byte a, byte operand)
    {
        byte result = (byte)(a ^ operand);

        bool zero = result == 0;
        bool negative = false; // Always reset for XOR
        bool halfCarry = false; // Always reset for XOR
        bool carry = false; // Always reset for XOR

        return new AluResult(result, zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// CP operation: A - operand (compare, don't store result)
    /// Z: Set if A == operand
    /// N: Set (1)
    /// H: Set if no borrow from bit 4
    /// C: Set if A < operand
    /// </summary>
    public static AluResult Compare(byte a, byte operand)
    {
        // CP is the same as SUB but doesn't store the result
        // We return the original A value as the "result" since it's not stored
        var subResult = Subtract(a, operand);
        return new AluResult(a, subResult.Zero, subResult.Negative, subResult.HalfCarry, subResult.Carry);
    }

    /// <summary>
    /// INC operation: operand + 1 (8-bit increment)
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Set if carry from bit 3
    /// C: Not affected
    /// </summary>
    public static AluResult Inc8(byte operand)
    {
        int result = operand + 1;

        bool zero = (result & 0xFF) == 0;
        bool negative = false; // Always reset for INC
        bool halfCarry = (operand & 0x0F) == 0x0F; // Carry from bit 3
        // Carry flag is not affected by INC

        return new AluResult((byte)(result & 0xFF), zero, negative, halfCarry, false);
    }

    /// <summary>
    /// DEC operation: operand - 1 (8-bit decrement)
    /// Z: Set if result is 0
    /// N: Set (1)
    /// H: Set if no borrow from bit 4
    /// C: Not affected
    /// </summary>
    public static AluResult Dec8(byte operand)
    {
        int result = operand - 1;

        bool zero = (result & 0xFF) == 0;
        bool negative = true; // Always set for DEC
        bool halfCarry = (operand & 0x0F) == 0; // No borrow from bit 4
        // Carry flag is not affected by DEC

        return new AluResult((byte)(result & 0xFF), zero, negative, halfCarry, false);
    }

    /// <summary>
    /// RLC operation: Rotate left circular (9-bit rotation through carry)
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Reset (0)
    /// C: Old bit 7
    /// </summary>
    public static AluResult RotateLeftCircular(byte operand)
    {
        bool oldBit7 = (operand & 0x80) != 0;
        byte result = (byte)((operand << 1) | (oldBit7 ? 1 : 0));

        bool zero = result == 0;
        bool negative = false; // Always reset for rotates
        bool halfCarry = false; // Always reset for rotates
        bool carry = oldBit7; // Old bit 7 goes to carry

        return new AluResult(result, zero, negative, halfCarry, carry);
    }

    /// <summary>
    /// SRL operation: Shift right logical
    /// Z: Set if result is 0
    /// N: Reset (0)
    /// H: Reset (0)
    /// C: Old bit 0
    /// </summary>
    public static AluResult ShiftRightLogical(byte operand)
    {
        bool oldBit0 = (operand & 0x01) != 0;
        byte result = (byte)(operand >> 1);

        bool zero = result == 0;
        bool negative = false; // Always reset for shifts
        bool halfCarry = false; // Always reset for shifts
        bool carry = oldBit0; // Old bit 0 goes to carry

        return new AluResult(result, zero, negative, halfCarry, carry);
    }
}