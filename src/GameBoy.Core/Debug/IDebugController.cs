using System;

namespace GameBoy.Core.Debug;

/// <summary>
/// Represents a block of memory data for debugging.
/// </summary>
public readonly struct MemoryBlock
{
    public ushort StartAddress { get; }
    public byte[] Data { get; }
    public int Length => Data.Length;

    public MemoryBlock(ushort startAddress, byte[] data)
    {
        StartAddress = startAddress;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public byte this[int index] => Data[index];
}

/// <summary>
/// Debug controller interface for managing emulator execution and state inspection.
/// </summary>
public interface IDebugController
{
    /// <summary>
    /// Gets whether debug mode is currently enabled.
    /// </summary>
    bool DebugMode { get; }

    /// <summary>
    /// Enables debug mode with optional trace logging.
    /// </summary>
    void EnableDebugMode(bool enableTracing = true);

    /// <summary>
    /// Disables debug mode and clears debug state.
    /// </summary>
    void DisableDebugMode();

    /// <summary>
    /// Executes exactly one CPU instruction.
    /// </summary>
    void StepInstruction();

    /// <summary>
    /// Steps over CALL instructions (runs until return to current call level).
    /// </summary>
    void StepOver();

    /// <summary>
    /// Steps out of current function (runs until call depth decreases).
    /// </summary>
    void StepOut();

    /// <summary>
    /// Continues execution until a breakpoint is hit or manually paused.
    /// </summary>
    void ContinueUntilBreak();

    /// <summary>
    /// Pauses execution if currently running.
    /// </summary>
    void Pause();

    /// <summary>
    /// Captures the current complete debug state.
    /// </summary>
    DebugState CaptureState();

    /// <summary>
    /// Reads a block of memory for debugging purposes.
    /// </summary>
    MemoryBlock ReadMemory(ushort startAddress, int length);

    /// <summary>
    /// Writes a byte to memory for debugging purposes.
    /// </summary>
    void WriteMemory(ushort address, byte value);

    /// <summary>
    /// Gets access to the breakpoint manager.
    /// </summary>
    BreakpointManager Breakpoints { get; }

    /// <summary>
    /// Gets access to the trace logger.
    /// </summary>
    TraceLogger TraceLogger { get; }

    /// <summary>
    /// Gets whether execution is currently paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets access to the MMU for memory reading operations.
    /// </summary>
    Mmu Mmu { get; }
}