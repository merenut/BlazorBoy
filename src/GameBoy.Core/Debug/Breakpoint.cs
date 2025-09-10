using System;

namespace GameBoy.Core.Debug;

/// <summary>
/// Types of breakpoints that can be set.
/// </summary>
public enum BreakpointType
{
    Execute,
    Read,
    Write,
    Conditional
}

/// <summary>
/// Represents a breakpoint at a specific memory address.
/// </summary>
public sealed class Breakpoint
{
    public ushort Address { get; }
    public BreakpointType Type { get; }
    public bool Enabled { get; set; } = true;
    public string Description { get; set; } = "";
    public DateTime Created { get; }

    public Breakpoint(ushort address, BreakpointType type, string description = "")
    {
        Address = address;
        Type = type;
        Description = description;
        Created = DateTime.UtcNow;
    }

    public override string ToString()
    {
        var typeStr = Type switch
        {
            BreakpointType.Execute => "EXEC",
            BreakpointType.Read => "READ",
            BreakpointType.Write => "WRITE",
            BreakpointType.Conditional => "COND",
            _ => "UNK"
        };
        
        var status = Enabled ? "" : " (DISABLED)";
        var desc = string.IsNullOrEmpty(Description) ? "" : $" - {Description}";
        
        return $"{typeStr} @ 0x{Address:X4}{status}{desc}";
    }
}