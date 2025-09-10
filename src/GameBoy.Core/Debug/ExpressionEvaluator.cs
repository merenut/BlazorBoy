using System;
using System.Collections.Generic;
using System.Globalization;

namespace GameBoy.Core.Debug;

/// <summary>
/// Simple expression evaluator for conditional breakpoints.
/// Supports basic comparisons and register/memory access.
/// </summary>
internal sealed class ExpressionEvaluator
{
    /// <summary>
    /// Evaluates a conditional breakpoint expression.
    /// Supports: A,B,C,D,E,H,L,AF,BC,DE,HL,SP,PC,(HL),(BC),(DE)
    /// Operators: ==, !=, <, >, <=, >=, &&, ||
    /// </summary>
    public bool Evaluate(string expression, Cpu.Registers registers, IMemoryReader memory)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        // Simple tokenization and evaluation
        // This is a basic implementation - a full parser would be more robust
        
        try
        {
            // Remove whitespace and parentheses for simpler parsing
            var normalized = expression.Replace(" ", "").Replace("(", "").Replace(")", "");
            
            // Handle simple comparisons first
            if (TryEvaluateComparison(normalized, registers, memory, out bool result))
            {
                return result;
            }

            // Handle logical operators (&&, ||)
            if (normalized.Contains("&&"))
            {
                var parts = normalized.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    return Evaluate(parts[0], registers, memory) && Evaluate(parts[1], registers, memory);
                }
            }

            if (normalized.Contains("||"))
            {
                var parts = normalized.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    return Evaluate(parts[0], registers, memory) || Evaluate(parts[1], registers, memory);
                }
            }

            return false;
        }
        catch
        {
            return false; // Invalid expression
        }
    }

    private bool TryEvaluateComparison(string expression, Cpu.Registers registers, IMemoryReader memory, out bool result)
    {
        result = false;

        // Find comparison operator
        string[] operators = { "==", "!=", "<=", ">=", "<", ">" };
        
        foreach (var op in operators)
        {
            var index = expression.IndexOf(op);
            if (index > 0)
            {
                var leftStr = expression.Substring(0, index);
                var rightStr = expression.Substring(index + op.Length);

                if (TryGetValue(leftStr, registers, memory, out int leftValue) &&
                    TryGetValue(rightStr, registers, memory, out int rightValue))
                {
                    result = op switch
                    {
                        "==" => leftValue == rightValue,
                        "!=" => leftValue != rightValue,
                        "<=" => leftValue <= rightValue,
                        ">=" => leftValue >= rightValue,
                        "<" => leftValue < rightValue,
                        ">" => leftValue > rightValue,
                        _ => false
                    };
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetValue(string operand, Cpu.Registers registers, IMemoryReader memory, out int value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(operand))
            return false;

        operand = operand.ToUpperInvariant();

        // Handle register values
        switch (operand)
        {
            case "A": value = registers.A; return true;
            case "B": value = registers.B; return true;
            case "C": value = registers.C; return true;
            case "D": value = registers.D; return true;
            case "E": value = registers.E; return true;
            case "H": value = registers.H; return true;
            case "L": value = registers.L; return true;
            case "AF": value = registers.AF; return true;
            case "BC": value = registers.BC; return true;
            case "DE": value = registers.DE; return true;
            case "HL": value = registers.HL; return true;
            case "SP": value = registers.SP; return true;
            case "PC": value = registers.PC; return true;
        }

        // Handle memory access
        if (operand == "HL")
        {
            try
            {
                value = memory.ReadByte(registers.HL);
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (operand == "BC")
        {
            try
            {
                value = memory.ReadByte(registers.BC);
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (operand == "DE")
        {
            try
            {
                value = memory.ReadByte(registers.DE);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Handle immediate values (hex or decimal)
        if (operand.StartsWith("0X"))
        {
            if (int.TryParse(operand.Substring(2), NumberStyles.HexNumber, null, out value))
                return true;
        }
        else if (int.TryParse(operand, out value))
        {
            return true;
        }

        return false;
    }
}