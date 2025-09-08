# LR35902 Complete Opcode Table

This document describes the complete opcode table implementation for the Game Boy's LR35902 CPU in BlazorBoy.

## Overview

The opcode table now provides **complete coverage** of all LR35902 instructions:
- **245 primary opcodes** (0x00-0xFF) - all valid instructions implemented
- **256 CB-prefixed opcodes** (0xCB00-0xCBFF) - all CB instructions implemented
- **11 invalid opcodes** properly handled as null entries

## Implementation Features

### Data Structure Design
- **Primary table**: `Instruction?[256]` array for opcodes 0x00-0xFF
- **CB table**: `Instruction?[256]` array for CB-prefixed opcodes 0xCB00-0xCBFF
- **Nullable entries**: Invalid opcodes are null, valid opcodes contain complete metadata

### Instruction Metadata
Each instruction includes:
- **Mnemonic**: Human-readable assembly representation (e.g., "LD A,d8")
- **Length**: Instruction size in bytes (1, 2, or 3)
- **BaseCycles**: Execution time in CPU cycles (4-24 for primary, 8-16 for CB)
- **OperandType**: Classification for operand parsing and validation
- **Execute**: Function delegate for instruction execution

### Systematic Coverage

#### Primary Opcodes (0x00-0xFF)
- **0x00-0x3F**: Mixed operations (NOP, loads, arithmetic, jumps, control)
- **0x40-0x7F**: 8×8 LD r,r' matrix (register-to-register loads) + HALT (0x76)
- **0x80-0xBF**: ALU operations (ADD, ADC, SUB, SBC, AND, XOR, OR, CP)
- **0xC0-0xFF**: Control flow, stack operations, I/O, and RST vectors

#### CB-Prefixed Opcodes (0xCB00-0xCBFF)
- **0x00-0x3F**: Rotate and shift operations (RLC, RRC, RL, RR, SLA, SRA, SWAP, SRL)
- **0x40-0x7F**: BIT operations (test bit 0-7 in registers/memory)
- **0x80-0xBF**: RES operations (reset bit 0-7 in registers/memory)
- **0xC0-0xFF**: SET operations (set bit 0-7 in registers/memory)

### Invalid Opcodes
The following opcodes are intentionally invalid in the LR35902 and remain null:
- 0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD

## Code Generation Patterns

### Register Access Pattern
For LD r,r' instructions (0x40-0x7F):
```csharp
// Register order: B(0), C(1), D(2), E(3), H(4), L(5), (HL)(6), A(7)
int dest = (opcode >> 3) & 7;
int src = opcode & 7;
string mnemonic = $"LD {regNames[dest]},{regNames[src]}";
```

### ALU Operation Pattern
For arithmetic operations (0x80-0xBF):
```csharp
// ALU ops: ADD(0), ADC(1), SUB(2), SBC(3), AND(4), XOR(5), OR(6), CP(7)
int op = (opcode >> 3) & 7;
int reg = opcode & 7;
string mnemonic = $"{aluOps[op]},{regNames[reg]}";
```

### CB Instruction Pattern
For bit operations (0xCB40-0xCBFF):
```csharp
// Bit operations: BIT(01), RES(10), SET(11)
int bit = (opcode >> 3) & 7;
int reg = opcode & 7;
string operation = (opcode & 0xC0) == 0x40 ? "BIT" : 
                   (opcode & 0xC0) == 0x80 ? "RES" : "SET";
```

## Cycle Timing

### Primary Instructions
- **1-byte simple**: 4 cycles (register operations, simple ALU)
- **Memory access**: +4 cycles for (HL) operations
- **Immediate data**: +4 cycles per byte read
- **Conditional branches**: 8 cycles (not taken) or 12-20 cycles (taken)
- **Stack operations**: 12-16 cycles
- **Complex operations**: Up to 24 cycles (CALL)

### CB Instructions
- **Register operations**: 8 cycles
- **Memory operations**: 12-16 cycles ((HL) access)

## Testing and Validation

### Coverage Tests
- `OpcodeTableCoverageTests` validates 100% coverage of valid opcodes
- Ensures all instructions have valid metadata (mnemonic, length, cycles, execute function)
- Verifies proper handling of invalid opcodes
- Confirms instruction categorization completeness

### Existing Compatibility
- All original functionality preserved
- Backward compatible with existing CPU implementation
- Maintains test suite integrity (306 passing tests)

## Usage for Automated Decoding

The complete opcode table enables:
- **Direct lookup**: `OpcodeTable.Primary[opcode]` or `OpcodeTable.CB[cbOpcode]`
- **Metadata extraction**: Access mnemonic, length, cycles without execution
- **Validation**: Check for valid vs invalid opcodes
- **Disassembly**: Generate assembly listings from binary code
- **Debugging**: Instruction tracing and analysis

## Performance Considerations

- **Static initialization**: Tables built once at startup
- **O(1) lookup**: Direct array indexing for opcode resolution
- **Minimal overhead**: Struct-based instruction representation
- **Delegate caching**: Execute functions stored as compiled delegates

This implementation provides a solid foundation for accurate Game Boy emulation with complete instruction set support.