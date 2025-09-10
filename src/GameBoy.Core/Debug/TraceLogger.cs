using System;
using System.Collections.Generic;
using System.Text;

namespace GameBoy.Core.Debug;

/// <summary>
/// Ring buffer trace logger that captures CPU execution history.
/// </summary>
public sealed class TraceLogger
{
    private readonly TraceEntry[] _buffer;
    private readonly int _capacity;
    private int _head = 0;
    private int _count = 0;
    private ulong _totalEntries = 0;

    public bool Enabled { get; set; } = false;
    public int Count => _count;
    public int Capacity => _capacity;
    public ulong TotalEntries => _totalEntries;

    public TraceLogger(int capacity = 2048)
    {
        _capacity = capacity;
        _buffer = new TraceEntry[capacity];
    }

    /// <summary>
    /// Logs a CPU instruction execution. Only logs if Enabled is true.
    /// </summary>
    public void Log(ushort pc, byte opcode, Cpu.Registers registers, ulong cycleCount)
    {
        if (!Enabled) return;

        var entry = new TraceEntry(pc, opcode, registers, cycleCount);
        
        _buffer[_head] = entry;
        _head = (_head + 1) % _capacity;
        
        if (_count < _capacity)
            _count++;
            
        _totalEntries++;
    }

    /// <summary>
    /// Gets a snapshot of the most recent trace entries.
    /// </summary>
    public IReadOnlyList<TraceEntry> Snapshot(int maxEntries = -1)
    {
        if (_count == 0) return Array.Empty<TraceEntry>();

        var takeCount = maxEntries > 0 ? Math.Min(maxEntries, _count) : _count;
        var result = new List<TraceEntry>(takeCount);

        // Start from oldest entry and work forward
        for (int i = 0; i < takeCount; i++)
        {
            var index = (_head - _count + i + _capacity) % _capacity;
            result.Add(_buffer[index]);
        }

        return result;
    }

    /// <summary>
    /// Exports trace entries as formatted text.
    /// </summary>
    public string ExportAsText(int maxEntries = 1000)
    {
        var entries = Snapshot(maxEntries);
        if (entries.Count == 0) return "No trace entries available.";

        var sb = new StringBuilder();
        sb.AppendLine($"Trace Log Export - {entries.Count} entries (Total: {_totalEntries})");
        sb.AppendLine("Cycles    PC   OP AF   BC   DE   HL   SP");
        sb.AppendLine("---------------------------------------------");

        foreach (var entry in entries)
        {
            sb.AppendLine(entry.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Clears all trace entries.
    /// </summary>
    public void Clear()
    {
        _head = 0;
        _count = 0;
        _totalEntries = 0;
    }
}