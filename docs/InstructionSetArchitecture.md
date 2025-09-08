# Game Boy CPU Instruction Set Architecture

This document describes the instruction decoding and execution infrastructure for the BlazorBoy Game Boy emulator.

## Overview

The Game Boy's Sharp LR35902 CPU instruction set is implemented using a combination of structured instruction metadata and lookup tables for fast decoding and execution.

## Core Components

### Instruction Structure

Each instruction is represented by the `Instruction` struct which contains:

- **Mnemonic**: Human-readable assembly representation (e.g., "LD A,d8")
- **Length**: Instruction size in bytes (1, 2, or 3 bytes)
- **BaseCycles**: Base execution time in CPU cycles
- **OperandType**: Classification of the instruction's operand pattern
- **Execute**: Function delegate that performs the instruction's operation

### Operand Types

The `OperandType` enum categorizes instructions by their operand patterns:

| Type | Description | Length | Examples |
|------|-------------|--------|----------|
| `None` | No operands | 1 byte | NOP, HALT, EI, DI |
| `Register` | Register-to-register operations | 1 byte | LD B,C; ADD A,B |
| `Immediate8` | 8-bit immediate values | 2 bytes | LD A,d8; ADD A,d8 |
| `Immediate16` | 16-bit immediate values | 3 bytes | LD BC,d16; JP a16 |
| `Memory` | Register-indirect memory access | 1 byte | LD A,(HL); LD (BC),A |
| `MemoryImmediate8` | High memory addressing | 2 bytes | LDH A,(a8) |
| `MemoryImmediate16` | Absolute memory addressing | 3 bytes | LD A,(a16) |
| `Relative8` | Relative jumps | 2 bytes | JR r8; JR Z,r8 |

## Instruction Coverage Status

### Primary Opcodes (0x00-0xFF) - 245/245 Valid Implemented (100%)

The primary instruction table is **completely implemented** with all 245 valid opcodes. The following table shows the coverage by range:

| Range | Description | Status | Count |
|-------|-------------|--------|-------|
| 0x00-0x3F | Mixed operations (NOP, loads, arithmetic, jumps, control) | ✅ Complete | 64/64 |
| 0x40-0x7F | 8×8 LD r,r' matrix + HALT (0x76) | ✅ Complete | 64/64 |
| 0x80-0xBF | ALU operations (ADD, ADC, SUB, SBC, AND, XOR, OR, CP) | ✅ Complete | 64/64 |
| 0xC0-0xFF | Control flow, stack operations, I/O, RST vectors | ✅ Complete | 53/64 |
| **Invalid** | Intentionally undefined opcodes | ✅ Properly handled | 11 opcodes |

**Invalid opcodes** (handled as null entries): 0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD

### CB-Prefixed Opcodes (0xCB00-0xCBFF) - 256/256 Implemented (100%)

The CB instruction table is **completely implemented** using systematic code generation:

| Range | Description | Status | Count |
|-------|-------------|--------|-------|
| 0x00-0x3F | Rotate and shift operations (8 ops × 8 registers) | ✅ Complete | 64/64 |
| 0x40-0x7F | BIT operations (8 bits × 8 registers) | ✅ Complete | 64/64 |
| 0x80-0xBF | RES operations (8 bits × 8 registers) | ✅ Complete | 64/64 |
| 0xC0-0xFF | SET operations (8 bits × 8 registers) | ✅ Complete | 64/64 |

### Implementation Methodology

**Primary Table**: All valid opcodes explicitly implemented with individual instruction definitions.

**CB Table**: Systematically generated using nested loops:
- Register order: B(0), C(1), D(2), E(3), H(4), L(5), (HL)(6), A(7)
- Operations handled via pattern matching and delegation to CPU methods
- Memory operations (HL) properly handled with correct cycle counts

### Testing Coverage

All implemented instructions are validated by:
- `OpcodeTableCoverageTests` - Ensures 100% coverage of valid opcodes
- `InstructionDecodingTests` - Validates instruction metadata and execution
- Individual CPU instruction tests in test suite (411 tests passing)

## Instruction Tables

### Primary Table (0x00-0xFF)

The primary instruction table handles all standard opcodes. It's implemented as a 256-entry array where each index corresponds to the opcode value.

**Current Implementation Status:**
- **245/245 valid instructions implemented** (100% coverage of valid opcodes)
- **11 invalid opcodes** properly handled as null entries (0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD)
- **Complete coverage of all instruction categories:**
  - Load operations (LD variants - all combinations)
  - Arithmetic operations (ADD, ADC, SUB, SBC, INC, DEC)
  - Logical operations (AND, XOR, OR, CP)
  - Jump and branch operations (JP, JR, CALL, RET - all variants)
  - Stack operations (PUSH, POP - all register pairs)
  - Control operations (NOP, HALT, EI, DI, STOP)
  - I/O operations (LDH variants)
  - RST vectors (all 8 vectors)

### CB-Prefixed Table (0xCB00-0xCBFF)

CB-prefixed instructions handle bit manipulation, rotates, and shifts. When opcode 0xCB is encountered, the CPU reads the next byte to index into the CB table.

**Current Implementation Status:**
- **256/256 instructions implemented** (100% coverage)
- **Systematically generated** via loops for regular patterns
- **Complete coverage of all CB instruction categories:**
  - Rotate operations (RLC, RRC, RL, RR)
  - Shift operations (SLA, SRA, SRL)
  - Swap operations (SWAP)
  - Bit testing (BIT 0-7)
  - Bit setting (SET 0-7)
  - Bit clearing (RES 0-7)
- **All register and memory variants** implemented for each operation type

## Decoding Process

The instruction decoding follows this pattern:

1. **Fetch**: Read opcode byte from memory at PC
2. **Decode**: Look up instruction in appropriate table
3. **Execute**: Call the instruction's execute function
4. **Advance**: Update PC and return cycle count

```csharp
public int Step()
{
    byte opcode = _mmu.ReadByte(Regs.PC++);

    // Handle CB-prefixed instructions
    if (opcode == 0xCB)
    {
        byte cbOpcode = _mmu.ReadByte(Regs.PC++);
        var instruction = OpcodeTable.CB[cbOpcode];
        if (instruction.HasValue)
            return instruction.Value.Execute(this);
        return 8; // Default cycles for unknown CB opcodes
    }

    // Handle primary instruction table
    var primaryInstruction = OpcodeTable.Primary[opcode];
    if (primaryInstruction.HasValue)
        return primaryInstruction.Value.Execute(this);
    
    return 4; // Default cycles for unknown opcodes
}
```

## Robustness Features

### Unknown Opcode Handling

- **Primary opcodes**: Return 4 cycles (typical 1-byte instruction)
- **CB-prefixed opcodes**: Return 8 cycles (typical CB instruction)
- **PC advancement**: Correctly handled for both known and unknown opcodes
- **No crashes**: Graceful degradation for unimplemented instructions

### Automatic Operand Type Deduction

For backward compatibility, operand types can be automatically deduced from mnemonic patterns:

```csharp
// Explicit operand type (recommended)
new Instruction("LD BC,d16", 3, 12, OperandType.Immediate16, executeFunc);

// Automatic deduction (backward compatible)
new Instruction("LD BC,d16", 3, 12, executeFunc); // Deduces Immediate16
```

### Validation and Testing

The instruction infrastructure includes comprehensive validation:

- **Metadata consistency**: Length matches operand type expectations
- **Cycle counts**: Reasonable bounds checking (4-24 cycles typical)
- **Mnemonic validation**: Non-empty, descriptive names
- **Execute function validity**: Non-null function pointers

## Performance Considerations

### Fast Lookup

- **Array-based tables**: O(1) instruction lookup
- **Pre-computed tables**: Static initialization prevents runtime overhead
- **Minimal allocations**: Struct-based design reduces GC pressure

### Cycle Accuracy

- **Base cycles**: Each instruction specifies its base execution time
- **Conditional cycles**: Instructions can return different cycle counts based on execution path
- **Precise timing**: Critical for accurate Game Boy emulation

## Adding New Instructions

To add a new instruction:

1. **Choose the opcode**: Identify the hex value (e.g., 0x3C for INC A)
2. **Define metadata**: Mnemonic, length, cycles, operand type
3. **Implement logic**: Create the execute function
4. **Add to table**: Insert into Primary or CB array
5. **Add tests**: Validate the instruction works correctly

Example:
```csharp
// 0x3C: INC A
Primary[0x3C] = new Instruction("INC A", 1, 4, OperandType.Register, cpu =>
{
    cpu.IncReg8(ref cpu.Regs.A);
    return 4;
});
```

## Maintainability Guidelines

### Naming Conventions

- **Mnemonics**: Use official Game Boy assembly syntax
- **Function names**: Descriptive, action-oriented names
- **Comments**: Include opcode hex values for reference

### Code Organization

- **Group by category**: Keep related instructions together
- **Consistent formatting**: Align similar instruction patterns
- **Clear separation**: Distinguish primary vs CB instructions

### Testing Strategy

- **Unit tests**: Test individual instruction execution
- **Integration tests**: Test instruction sequences
- **Validation tests**: Verify metadata consistency
- **Regression tests**: Ensure existing instructions still work

## Maintainer Notes

### Current Implementation Status (Updated: Phase 3)

The BlazorBoy CPU instruction set implementation is **100% COMPLETE** as of Phase 3:

- ✅ **All 245 valid primary opcodes** implemented and tested
- ✅ **All 256 CB-prefixed opcodes** implemented via systematic generation
- ✅ **All invalid opcodes** properly handled (return null)
- ✅ **Complete test coverage** with 411 passing tests
- ✅ **Backward compatible** with existing codebase

### For Future Maintainers (AI or Human)

**What's Done:**
- The instruction set is feature-complete for Game Boy emulation
- No additional opcodes need to be implemented
- The existing test suite validates all implemented instructions

**Implementation Quality:**
- Primary opcodes: Hand-crafted for clarity and maintainability
- CB opcodes: Generated systematically to ensure consistency
- Error handling: Graceful degradation for edge cases
- Performance: O(1) lookup with minimal overhead

**If You Need to Debug Instructions:**
1. Check `OpcodeTableCoverageTests.cs` for validation tests
2. Use `InstructionDecodingTests.cs` for specific instruction behavior
3. The CPU Step() method handles both primary and CB instruction dispatch
4. All instructions include proper cycle timing and flag handling

**Do NOT:**
- Add duplicate opcode implementations
- Modify the invalid opcode list (it's per LR35902 specification)
- Change the CB table generation loops without understanding the full impact

This architecture provides a solid foundation for implementing the complete Game Boy instruction set while maintaining performance, robustness, and maintainability.