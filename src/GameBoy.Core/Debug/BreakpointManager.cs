using System;
using System.Collections.Generic;
using System.Linq;

namespace GameBoy.Core.Debug;

/// <summary>
/// Manages breakpoints and determines when execution should be paused.
/// </summary>
public sealed class BreakpointManager
{
    private readonly Dictionary<ushort, Breakpoint> _addressBreakpoints = new();
    private readonly List<ConditionalBreakpoint> _conditionalBreakpoints = new();
    private readonly ExpressionEvaluator _expressionEvaluator = new();

    public IReadOnlyDictionary<ushort, Breakpoint> AddressBreakpoints => _addressBreakpoints;
    public IReadOnlyCollection<ConditionalBreakpoint> ConditionalBreakpoints => _conditionalBreakpoints.AsReadOnly();

    /// <summary>
    /// Sets an execution breakpoint at the specified address.
    /// </summary>
    public void SetExecuteBreakpoint(ushort address, string description = "")
    {
        var breakpoint = new Breakpoint(address, BreakpointType.Execute, description);
        _addressBreakpoints[address] = breakpoint;
    }

    /// <summary>
    /// Sets a memory read breakpoint at the specified address.
    /// </summary>
    public void SetReadBreakpoint(ushort address, string description = "")
    {
        var breakpoint = new Breakpoint(address, BreakpointType.Read, description);
        _addressBreakpoints[address] = breakpoint;
    }

    /// <summary>
    /// Sets a memory write breakpoint at the specified address.
    /// </summary>
    public void SetWriteBreakpoint(ushort address, string description = "")
    {
        var breakpoint = new Breakpoint(address, BreakpointType.Write, description);
        _addressBreakpoints[address] = breakpoint;
    }

    /// <summary>
    /// Removes a breakpoint at the specified address.
    /// </summary>
    public bool RemoveBreakpoint(ushort address)
    {
        return _addressBreakpoints.Remove(address);
    }

    /// <summary>
    /// Adds a conditional breakpoint with the specified expression.
    /// </summary>
    public ConditionalBreakpoint AddConditional(string expression, string description = "")
    {
        var conditional = new ConditionalBreakpoint(expression, description);
        _conditionalBreakpoints.Add(conditional);
        return conditional;
    }

    /// <summary>
    /// Removes a conditional breakpoint by ID.
    /// </summary>
    public bool RemoveConditional(Guid id)
    {
        var conditional = _conditionalBreakpoints.FirstOrDefault(c => c.Id == id);
        if (conditional != null)
        {
            _conditionalBreakpoints.Remove(conditional);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Enables or disables a breakpoint at the specified address.
    /// </summary>
    public bool SetBreakpointEnabled(ushort address, bool enabled)
    {
        if (_addressBreakpoints.TryGetValue(address, out var breakpoint))
        {
            breakpoint.Enabled = enabled;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Enables or disables a conditional breakpoint by ID.
    /// </summary>
    public bool SetConditionalEnabled(Guid id, bool enabled)
    {
        var conditional = _conditionalBreakpoints.FirstOrDefault(c => c.Id == id);
        if (conditional != null)
        {
            conditional.Enabled = enabled;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if execution should break at the current state.
    /// </summary>
    public bool ShouldBreak(ushort pc, Cpu.Registers registers, IMemoryReader memory)
    {
        // Check address breakpoints for execution
        if (_addressBreakpoints.TryGetValue(pc, out var breakpoint)
            && breakpoint.Enabled
            && breakpoint.Type == BreakpointType.Execute)
        {
            return true;
        }

        // Check conditional breakpoints
        foreach (var conditional in _conditionalBreakpoints)
        {
            if (!conditional.Enabled) continue;

            try
            {
                if (_expressionEvaluator.Evaluate(conditional.Expression, registers, memory))
                {
                    conditional.HitCount++;
                    return true;
                }
            }
            catch
            {
                // Ignore expression evaluation errors
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if execution should break on a memory read.
    /// </summary>
    public bool ShouldBreakOnRead(ushort address)
    {
        return _addressBreakpoints.TryGetValue(address, out var breakpoint)
            && breakpoint.Enabled
            && breakpoint.Type == BreakpointType.Read;
    }

    /// <summary>
    /// Checks if execution should break on a memory write.
    /// </summary>
    public bool ShouldBreakOnWrite(ushort address)
    {
        return _addressBreakpoints.TryGetValue(address, out var breakpoint)
            && breakpoint.Enabled
            && breakpoint.Type == BreakpointType.Write;
    }

    /// <summary>
    /// Clears all breakpoints.
    /// </summary>
    public void ClearAll()
    {
        _addressBreakpoints.Clear();
        _conditionalBreakpoints.Clear();
    }

    /// <summary>
    /// Gets total number of breakpoints (address + conditional).
    /// </summary>
    public int TotalCount => _addressBreakpoints.Count + _conditionalBreakpoints.Count;
}