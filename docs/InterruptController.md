# Interrupt Controller Documentation

## Overview

The `InterruptController` class manages the Game Boy's interrupt system, implementing the IF (Interrupt Flag, 0xFF0F) and IE (Interrupt Enable, 0xFFFF) registers with proper hardware behavior. This document covers the public API, register semantics, edge cases, and integration patterns for maintainers and AI assistants.

## Interrupt Types and Priority

The Game Boy supports 5 interrupt types, handled in strict priority order:

| Priority | Type | Bit | Vector | Trigger Source |
|----------|------|-----|--------|----------------|
| 1 (Highest) | VBlank | 0 | 0x0040 | PPU at start of vertical blanking |
| 2 | LCDStat | 1 | 0x0048 | PPU on LCD status changes (mode transitions, LYC=LY) |
| 3 | Timer | 2 | 0x0050 | Timer on TIMA register overflow |
| 4 | Serial | 3 | 0x0058 | Serial port on data transfer complete |
| 5 (Lowest) | Joypad | 4 | 0x0060 | Joypad on button press |

## Public API

### InterruptType Enum

```csharp
public enum InterruptType : byte
{
    VBlank = 0,   // Bit 0, Vector 0x0040
    LCDStat = 1,  // Bit 1, Vector 0x0048  
    Timer = 2,    // Bit 2, Vector 0x0050
    Serial = 3,   // Bit 3, Vector 0x0058
    Joypad = 4    // Bit 4, Vector 0x0060
}
```

### InterruptController Class

#### Properties

```csharp
public byte IF { get; }  // Interrupt Flag register (0xFF0F)
public byte IE { get; }  // Interrupt Enable register (0xFFFF)
```

#### Core Methods

```csharp
// Request an interrupt by setting the corresponding IF bit
public void Request(InterruptType interruptType)

// Check for pending interrupts (returns highest priority)
public bool TryGetPending(out InterruptType interruptType)

// Service an interrupt (clears IF bit, returns vector address)
public ushort Service(InterruptType interruptType)

// Set register values directly
public void SetIF(byte value)
public void SetIE(byte value)

// Initialize to post-BIOS defaults
public void InitializePostBiosDefaults()
```

## Register Semantics

### IF Register (0xFF0F) - Interrupt Flag

- **Writable bits**: Lower 5 bits (0-4) only
- **Read behavior**: Upper 3 bits (5-7) always read as 1
- **Write behavior**: Upper 3 bits ignored, only lower 5 bits stored
- **Default value**: 0xE1 (0x01 | 0xE0)

Examples:
```csharp
controller.SetIF(0x00);  // Reads back as 0xE0
controller.SetIF(0x1F);  // Reads back as 0xFF  
controller.SetIF(0xFF);  // Reads back as 0xFF (upper bits ignored)
```

### IE Register (0xFFFF) - Interrupt Enable

- **Writable bits**: All 8 bits (0-7)
- **Read behavior**: Returns exact written value
- **Write behavior**: Stores full 8-bit value
- **Default value**: 0x00

### Interrupt Pending Logic

An interrupt is **pending** when both its IF and IE bits are set:
```csharp
bool isPending = (IF & IE & (1 << (int)interruptType)) != 0;
```

## Usage Patterns by Component

### PPU (Picture Processing Unit)
```csharp
// Request VBlank interrupt at end of frame
interruptController.Request(InterruptType.VBlank);

// Request LCD STAT interrupt on mode changes
interruptController.Request(InterruptType.LCDStat);
```

### Timer
```csharp
// Request Timer interrupt on TIMA overflow
interruptController.Request(InterruptType.Timer);
```

### Serial Port
```csharp
// Request Serial interrupt on transfer complete
interruptController.Request(InterruptType.Serial);
```

### Joypad
```csharp
// Request Joypad interrupt on button press
interruptController.Request(InterruptType.Joypad);
```

### CPU Integration
```csharp
// Check for interrupts before each instruction
if (InterruptsEnabled && interruptController.TryGetPending(out InterruptType type))
{
    // Service the interrupt
    ushort vector = interruptController.Service(type);
    // CPU pushes PC, clears IME, jumps to vector
}
```

## Edge Cases and Hardware Quirks

### 1. IME (Interrupt Master Enable) Delay

The **EI** instruction has a **1-instruction delay** before enabling interrupts:

```csharp
// Instruction sequence:
EI      // Schedules interrupt enable for after next instruction
NOP     // Interrupts still disabled during this instruction
// Interrupts enabled starting from the instruction after NOP
```

**Implementation**: The CPU tracks pending interrupt enable state and applies it after executing the next instruction.

### 2. DI Immediate Effect

The **DI** instruction **immediately** disables interrupts (no delay):

```csharp
DI      // Interrupts disabled immediately
NOP     // Interrupts remain disabled
```

### 3. HALT Behavior

#### Normal HALT with IME=1
- CPU halts until any enabled interrupt becomes pending
- When interrupt occurs: CPU wakes up, services interrupt, clears IME

#### HALT with IME=0 (Special Case)
- CPU halts until any enabled interrupt becomes pending  
- When interrupt occurs: CPU wakes up but does **not** service interrupt
- Execution continues with next instruction

```csharp
// HALT with IME=0 and pending interrupt
cpu.IsHalted = true;
if (interruptController.TryGetPending(out _))
{
    cpu.IsHalted = false;  // Wake up
    // Continue to next instruction (no interrupt service)
}
```

### 4. HALT Bug (Not Implemented)

**Description**: When HALT is executed with IME=0 and a pending interrupt exists (IE & IF ≠ 0), the instruction immediately following HALT is executed twice.

**Status**: Commented out in current implementation due to complexity. Requires detailed hardware behavior research.

```csharp
// TODO: Implement HALT bug when IME=0 and IE&IF≠0
// This is a complex hardware quirk that requires more detailed research
```

### 5. RETI Instruction

**RETI** (Return from Interrupt) performs two actions:
1. Pops return address from stack (like RET)
2. Enables interrupts (sets IME=1, no delay)

```csharp
// RETI behavior
ushort returnAddress = PopStack();
PC = returnAddress;
InterruptsEnabled = true;  // Immediate effect, no delay
```

### 6. Interrupt Service Timing

Interrupt service routine takes **20 CPU cycles**:
- 2 cycles: Detect interrupt
- 8 cycles: Push PC onto stack (2 memory writes × 4 cycles each)  
- 10 cycles: Jump to interrupt vector

## Default Values (Post-BIOS)

After BIOS execution completes, registers have these default values:

```csharp
IF = 0xE1;  // VBlank bit set (0x01), upper bits forced (0xE0)
IE = 0x00;  // No interrupts enabled
```

These defaults are set by calling `InitializePostBiosDefaults()`.

## Testing and Validation

The interrupt controller has comprehensive test coverage (see `InterruptControllerTests.cs` and `InterruptServiceRoutineTests.cs`):

- **Register semantics**: IF upper bit behavior, IE full 8-bit storage
- **Priority handling**: Multiple pending interrupts resolve to highest priority
- **Edge cases**: IME delay, HALT behavior, DI immediate effect
- **API usage**: All documented usage patterns validated
- **Integration**: CPU interrupt service routine timing and behavior

## Implementation Notes

### Thread Safety
The `InterruptController` class is **not thread-safe**. It's designed for single-threaded emulator operation where only the CPU thread modifies interrupt state.

### Performance
- All operations are O(1) constant time
- No allocations during normal operation
- Bitwise operations used for maximum efficiency

### Hardware Accuracy
The implementation matches original Game Boy hardware behavior including:
- IF register upper bit masking
- Interrupt priority ordering
- Service routine timing (20 cycles)
- Post-BIOS default values

## Common Pitfalls

1. **Forgetting IF upper bits**: Always mask to lower 5 bits when writing IF
2. **Priority confusion**: VBlank is highest priority (bit 0), not Joypad (bit 4)
3. **IME delay**: EI doesn't take effect until after the next instruction
4. **HALT with IME=0**: CPU wakes up but doesn't service interrupt
5. **Register addresses**: IF is 0xFF0F, IE is 0xFFFF (not adjacent)

## Related Documentation

- [`InstructionSetArchitecture.md`](InstructionSetArchitecture.md) - CPU instruction details (EI/DI/HALT/RETI)
- [`InstructionTiming.md`](InstructionTiming.md) - CPU timing including interrupt service cycles
- [`README.md`](../README.md) - Overall project structure and development phases