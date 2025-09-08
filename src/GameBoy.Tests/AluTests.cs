using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Unit tests for ALU flag calculation helpers.
/// Tests cover all ALU operations and edge cases to ensure LR35902 compliance.
/// </summary>
public class AluTests
{
    #region ADD Tests

    [Fact]
    public void Add_BasicOperation_CorrectResult()
    {
        var result = Alu.Add(0x10, 0x20);

        Assert.Equal(0x30, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Add_ZeroResult_SetsZeroFlag()
    {
        var result = Alu.Add(0x00, 0x00);

        Assert.Equal(0x00, result.Result);
        Assert.True(result.Zero);
        Assert.False(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Add_Overflow_SetsCarryFlag()
    {
        var result = Alu.Add(0xFF, 0x01);

        Assert.Equal(0x00, result.Result);
        Assert.True(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.True(result.Carry);
    }

    [Fact]
    public void Add_HalfCarryOnly_SetsHalfCarryFlag()
    {
        var result = Alu.Add(0x0F, 0x01);

        Assert.Equal(0x10, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Add_HalfCarryBoundary_CorrectFlags()
    {
        // Test the boundary of half-carry: 0x08 + 0x08 = 0x10 (should set half-carry)
        var result = Alu.Add(0x08, 0x08);

        Assert.Equal(0x10, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.False(result.Carry);
    }

    #endregion

    #region ADC Tests

    [Fact]
    public void AddWithCarry_NoCarryIn_SameAsAdd()
    {
        var result = Alu.AddWithCarry(0x10, 0x20, false);
        var addResult = Alu.Add(0x10, 0x20);

        Assert.Equal(addResult.Result, result.Result);
        Assert.Equal(addResult.Zero, result.Zero);
        Assert.Equal(addResult.Negative, result.Negative);
        Assert.Equal(addResult.HalfCarry, result.HalfCarry);
        Assert.Equal(addResult.Carry, result.Carry);
    }

    [Fact]
    public void AddWithCarry_WithCarryIn_AddsOne()
    {
        var result = Alu.AddWithCarry(0x10, 0x20, true);

        Assert.Equal(0x31, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void AddWithCarry_CarryInCausesHalfCarry()
    {
        var result = Alu.AddWithCarry(0x0F, 0x00, true);

        Assert.Equal(0x10, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void AddWithCarry_CarryInCausesOverflow()
    {
        var result = Alu.AddWithCarry(0xFF, 0x00, true);

        Assert.Equal(0x00, result.Result);
        Assert.True(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.True(result.Carry);
    }

    #endregion

    #region SUB Tests

    [Fact]
    public void Subtract_BasicOperation_CorrectResult()
    {
        var result = Alu.Subtract(0x30, 0x20);

        Assert.Equal(0x10, result.Result);
        Assert.False(result.Zero);
        Assert.True(result.Negative); // Always set for SUB
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Subtract_ZeroResult_SetsZeroFlag()
    {
        var result = Alu.Subtract(0x50, 0x50);

        Assert.Equal(0x00, result.Result);
        Assert.True(result.Zero);
        Assert.True(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Subtract_Underflow_SetsCarryFlag()
    {
        var result = Alu.Subtract(0x00, 0x01);

        Assert.Equal(0xFF, result.Result);
        Assert.False(result.Zero);
        Assert.True(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.True(result.Carry);
    }

    [Fact]
    public void Subtract_HalfCarryBorrow_SetsHalfCarryFlag()
    {
        var result = Alu.Subtract(0x10, 0x01);

        Assert.Equal(0x0F, result.Result);
        Assert.False(result.Zero);
        Assert.True(result.Negative);
        Assert.True(result.HalfCarry); // Borrow from bit 4
        Assert.False(result.Carry);
    }

    #endregion

    #region SBC Tests

    [Fact]
    public void SubtractWithCarry_NoCarryIn_SameAsSub()
    {
        var result = Alu.SubtractWithCarry(0x30, 0x20, false);
        var subResult = Alu.Subtract(0x30, 0x20);

        Assert.Equal(subResult.Result, result.Result);
        Assert.Equal(subResult.Zero, result.Zero);
        Assert.Equal(subResult.Negative, result.Negative);
        Assert.Equal(subResult.HalfCarry, result.HalfCarry);
        Assert.Equal(subResult.Carry, result.Carry);
    }

    [Fact]
    public void SubtractWithCarry_WithCarryIn_SubtractsOne()
    {
        var result = Alu.SubtractWithCarry(0x30, 0x20, true);

        Assert.Equal(0x0F, result.Result);
        Assert.False(result.Zero);
        Assert.True(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void SubtractWithCarry_CarryInCausesUnderflow()
    {
        var result = Alu.SubtractWithCarry(0x00, 0x00, true);

        Assert.Equal(0xFF, result.Result);
        Assert.False(result.Zero);
        Assert.True(result.Negative);
        Assert.True(result.HalfCarry);
        Assert.True(result.Carry);
    }

    #endregion

    #region AND Tests

    [Fact]
    public void And_BasicOperation_CorrectResult()
    {
        var result = Alu.And(0xF0, 0x0F);

        Assert.Equal(0x00, result.Result);
        Assert.True(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry); // Always set for AND
        Assert.False(result.Carry);
    }

    [Fact]
    public void And_NonZeroResult_CorrectFlags()
    {
        var result = Alu.And(0xFF, 0x55);

        Assert.Equal(0x55, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry); // Always set for AND
        Assert.False(result.Carry);
    }

    [Fact]
    public void And_AllBitsSet_CorrectFlags()
    {
        var result = Alu.And(0xFF, 0xFF);

        Assert.Equal(0xFF, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.True(result.HalfCarry); // Always set for AND
        Assert.False(result.Carry);
    }

    #endregion

    #region OR Tests

    [Fact]
    public void Or_BasicOperation_CorrectResult()
    {
        var result = Alu.Or(0xF0, 0x0F);

        Assert.Equal(0xFF, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Or_ZeroResult_SetsZeroFlag()
    {
        var result = Alu.Or(0x00, 0x00);

        Assert.Equal(0x00, result.Result);
        Assert.True(result.Zero);
        Assert.False(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    #endregion

    #region XOR Tests

    [Fact]
    public void Xor_BasicOperation_CorrectResult()
    {
        var result = Alu.Xor(0xF0, 0x0F);

        Assert.Equal(0xFF, result.Result);
        Assert.False(result.Zero);
        Assert.False(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Xor_SameValues_ZeroResult()
    {
        var result = Alu.Xor(0x55, 0x55);

        Assert.Equal(0x00, result.Result);
        Assert.True(result.Zero);
        Assert.False(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    #endregion

    #region CP (Compare) Tests

    [Fact]
    public void Compare_Equal_SetsZeroFlag()
    {
        var result = Alu.Compare(0x50, 0x50);

        Assert.Equal(0x50, result.Result); // Original A value preserved
        Assert.True(result.Zero);
        Assert.True(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Compare_ALarger_NoFlags()
    {
        var result = Alu.Compare(0x50, 0x30);

        Assert.Equal(0x50, result.Result); // Original A value preserved
        Assert.False(result.Zero);
        Assert.True(result.Negative);
        Assert.False(result.HalfCarry);
        Assert.False(result.Carry);
    }

    [Fact]
    public void Compare_ASmaller_SetsCarryFlag()
    {
        var result = Alu.Compare(0x30, 0x50);

        Assert.Equal(0x30, result.Result); // Original A value preserved
        Assert.False(result.Zero);
        Assert.True(result.Negative);
        Assert.False(result.HalfCarry); // 0x30 & 0x0F = 0, 0x50 & 0x0F = 0, no borrow needed
        Assert.True(result.Carry);
    }

    #endregion

    #region Edge Cases and Flag Transitions

    [Theory]
    [InlineData(0x00, 0x00)] // Zero + Zero
    [InlineData(0xFF, 0xFF)] // Max + Max
    [InlineData(0x7F, 0x01)] // No half-carry, no carry
    [InlineData(0x80, 0x80)] // Overflow
    public void Add_EdgeCases_BehavesCorrectly(byte a, byte operand)
    {
        var result = Alu.Add(a, operand);

        // Verify basic properties
        int expected = a + operand;
        Assert.Equal((byte)(expected & 0xFF), result.Result);
        Assert.Equal(result.Result == 0, result.Zero);
        Assert.False(result.Negative); // Never set for ADD
        Assert.Equal(expected > 0xFF, result.Carry);
        Assert.Equal((a & 0x0F) + (operand & 0x0F) > 0x0F, result.HalfCarry);
    }

    [Theory]
    [InlineData(0x00, 0x00)] // Zero - Zero
    [InlineData(0xFF, 0x01)] // Large - Small
    [InlineData(0x10, 0x01)] // Half-carry borrow
    [InlineData(0x00, 0xFF)] // Underflow
    public void Subtract_EdgeCases_BehavesCorrectly(byte a, byte operand)
    {
        var result = Alu.Subtract(a, operand);

        // Verify basic properties
        int expected = a - operand;
        Assert.Equal((byte)(expected & 0xFF), result.Result);
        Assert.Equal(result.Result == 0, result.Zero);
        Assert.True(result.Negative); // Always set for SUB
        Assert.Equal(a < operand, result.Carry);
        Assert.Equal((a & 0x0F) < (operand & 0x0F), result.HalfCarry);
    }

    #endregion
}