# Game Boy CPU Instruction Timing Reference

This document provides the authoritative timing reference for all Game Boy (LR35902) CPU instructions based on the Pan Docs and hardware testing.

## Timing Rules

### Primary Instructions (0x00-0xFF)

#### Basic Categories:
- **1-byte register operations**: 4 cycles
- **Memory loads/stores (HL)**: 8 cycles
- **16-bit register operations**: 8 cycles
- **Immediate 8-bit loads**: 8 cycles (4 cycles fetch + 4 cycles operand)
- **Immediate 16-bit loads**: 12 cycles (4 cycles fetch + 8 cycles operands)

#### Control Flow:
- **Unconditional jumps (JP a16)**: 16 cycles
- **Conditional jumps taken (JP cc,a16)**: 16 cycles
- **Conditional jumps not taken (JP cc,a16)**: 12 cycles
- **Relative jumps taken (JR r8)**: 12 cycles
- **Relative jumps not taken (JR r8)**: 8 cycles
- **Unconditional call (CALL a16)**: 24 cycles
- **Conditional call taken (CALL cc,a16)**: 24 cycles
- **Conditional call not taken (CALL cc,a16)**: 12 cycles
- **Unconditional return (RET)**: 16 cycles
- **Conditional return taken (RET cc)**: 20 cycles
- **Conditional return not taken (RET cc)**: 8 cycles
- **Return from interrupt (RETI)**: 16 cycles

#### Stack Operations:
- **PUSH rr**: 16 cycles
- **POP rr**: 12 cycles

#### Memory Operations:
- **LD A,(a16)**: 16 cycles
- **LD (a16),A**: 16 cycles
- **LD A,(C)**: 8 cycles (LDH A,($FF00+C))
- **LD (C),A**: 8 cycles (LDH ($FF00+C),A)
- **LD A,(a8)**: 12 cycles (LDH A,($FF00+a8))
- **LD (a8),A**: 12 cycles (LDH ($FF00+a8),A)

#### Special Instructions:
- **NOP**: 4 cycles
- **HALT**: 4 cycles
- **STOP**: 4 cycles
- **EI/DI**: 4 cycles
- **RST nn**: 16 cycles

### CB-Prefixed Instructions (0xCB00-0xCBFF)

#### Register Operations (8 cycles):
- **RLC r, RRC r, RL r, RR r**: 8 cycles
- **SLA r, SRA r, SRL r**: 8 cycles
- **SWAP r**: 8 cycles
- **BIT n,r**: 8 cycles
- **SET n,r**: 8 cycles
- **RES n,r**: 8 cycles

#### Memory Operations (involving (HL)):
- **RLC (HL), RRC (HL), RL (HL), RR (HL)**: 16 cycles
- **SLA (HL), SRA (HL), SRL (HL)**: 16 cycles
- **SWAP (HL)**: 16 cycles
- **BIT n,(HL)**: 12 cycles
- **SET n,(HL)**: 16 cycles
- **RES n,(HL)**: 16 cycles

## Detailed Instruction Timing

### Load Instructions
| Instruction | Cycles | Notes |
|-------------|--------|-------|
| LD r,r' | 4 | Register to register |
| LD r,d8 | 8 | Load immediate 8-bit |
| LD rr,d16 | 12 | Load immediate 16-bit |
| LD A,(rr) | 8 | Load from (BC) or (DE) |
| LD (rr),A | 8 | Store to (BC) or (DE) |
| LD A,(HL) | 8 | Load from (HL) |
| LD (HL),r | 8 | Store register to (HL) |
| LD (HL),d8 | 12 | Store immediate to (HL) |
| LD A,(a16) | 16 | Load from absolute address |
| LD (a16),A | 16 | Store to absolute address |
| LDH A,(a8) | 12 | Load from high memory |
| LDH (a8),A | 12 | Store to high memory |
| LD HL,SP+r8 | 12 | Load HL with SP+offset |
| LD SP,HL | 8 | Load SP from HL |
| LD (a16),SP | 20 | Store SP at address |

### Arithmetic Instructions
| Instruction | Cycles | Notes |
|-------------|--------|-------|
| ADD A,r | 4 | Add register to A |
| ADD A,d8 | 8 | Add immediate to A |
| ADD A,(HL) | 8 | Add memory to A |
| ADC A,r | 4 | Add with carry |
| SUB r | 4 | Subtract register |
| SBC A,r | 4 | Subtract with carry |
| AND r | 4 | Logical AND |
| OR r | 4 | Logical OR |
| XOR r | 4 | Logical XOR |
| CP r | 4 | Compare |
| INC r | 4 | Increment register |
| DEC r | 4 | Decrement register |
| INC (HL) | 12 | Increment memory |
| DEC (HL) | 12 | Decrement memory |
| ADD HL,rr | 8 | Add 16-bit to HL |
| INC rr | 8 | Increment 16-bit |
| DEC rr | 8 | Decrement 16-bit |

### Jump and Branch Instructions
| Instruction | Cycles (taken/not taken) | Notes |
|-------------|--------|-------|
| JP a16 | 16 | Unconditional jump |
| JP cc,a16 | 16/12 | Conditional jump |
| JR r8 | 12 | Unconditional relative jump |
| JR cc,r8 | 12/8 | Conditional relative jump |
| JP (HL) | 4 | Jump to address in HL |
| CALL a16 | 24 | Unconditional call |
| CALL cc,a16 | 24/12 | Conditional call |
| RET | 16 | Unconditional return |
| RET cc | 20/8 | Conditional return |
| RETI | 16 | Return and enable interrupts |
| RST nn | 16 | Restart |

## Common Timing Errors to Avoid

1. **Memory operations**: Always add +4 cycles for (HL) access
2. **Conditional branches**: Must return different cycles based on condition
3. **CB instructions**: Memory operations are 12-16 cycles, not 8
4. **16-bit operations**: Most take 8 cycles, not 4
5. **Immediate data**: Add +4 cycles per byte read after opcode
6. **Stack operations**: PUSH takes 16 cycles, POP takes 12 cycles

## Testing Strategy

Use these reference timings to validate instruction implementations:
1. Each instruction should return the exact cycle count specified
2. Conditional instructions should return correct cycles for both paths
3. Memory operations should account for additional access time
4. CB-prefixed instructions should use CB timing rules