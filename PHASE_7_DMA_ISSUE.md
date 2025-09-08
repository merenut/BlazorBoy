# Phase 7: Implement DMA (Direct Memory Access) Transfer

## 🎯 Overview

Implement Game Boy DMA (Direct Memory Access) functionality as specified in **Phase 7** of the BlazorBoy development plan. DMA is a critical Game Boy feature that allows rapid copying of sprite data from main memory to OAM (Object Attribute Memory) for efficient sprite rendering.

## 📋 Background

The Game Boy's DMA controller allows the CPU to initiate a high-speed transfer of 160 bytes (0xA0 bytes) from any page-aligned address in memory to OAM (0xFE00-0xFE9F). This is essential for sprite animation and is used by virtually all Game Boy games.

### Current State
- ✅ DMA register (0xFF46) is defined and basic read/write works
- ✅ MMU infrastructure supports I/O register handling
- ❌ **Missing**: Actual DMA transfer logic when register is written
- ❌ **Missing**: DMA transfer timing and CPU bus restrictions
- ❌ **Missing**: Comprehensive test coverage for DMA semantics

## 🔧 Technical Requirements

### Hardware Specification
- **Register**: DMA at 0xFF46
- **Transfer Source**: `(DMA_value << 8) + 0x00` to `(DMA_value << 8) + 0x9F`
- **Transfer Destination**: 0xFE00 to 0xFE9F (OAM region)
- **Transfer Size**: 160 bytes (0xA0)
- **Transfer Time**: ~640 cycles (160 bytes × 4 cycles/byte)
- **Source Restrictions**: Cannot be 0xE000-0xFFFF range (echo RAM, I/O, HRAM)

### Implementation Details
```
DMA Transfer Process:
1. Write to 0xFF46 triggers immediate transfer
2. Copy 160 bytes from source to OAM
3. During transfer (~640 cycles):
   - CPU can only access HRAM (0xFF80-0xFFFE) and I/O registers
   - All other memory reads return 0xFF
   - All other memory writes are ignored
4. Transfer completes after 640 cycles
```

## 📝 Implementation Tasks

### Task 1: Core DMA Transfer Logic
- [ ] **File**: `src/GameBoy.Core/Mmu.cs` (or new `DmaController.cs`)
- [ ] Implement `StartDmaTransfer(byte sourcePageHigh)` method
- [ ] Add validation for source address restrictions
- [ ] Perform immediate 160-byte copy from source to OAM (0xFE00-0xFE9F)
- [ ] Update `WriteIoRegister` DMA case to trigger transfer

### Task 2: DMA State Management
- [ ] Add private fields for DMA state tracking:
  - `bool _dmaActive` - whether transfer is in progress
  - `int _dmaRemainingCycles` - cycles remaining for current transfer
- [ ] Add public property `bool IsDmaActive { get; }` for external access

### Task 3: DMA Timing Integration
- [ ] **File**: `src/GameBoy.Core/Emulator.cs` or `Mmu.cs`
- [ ] Add `StepDma(int cycles)` method to progress DMA timing
- [ ] Integrate DMA step into main emulator cycle loop
- [ ] Initially implement simple timing (no CPU blocking for Phase 7)

### Task 4: Memory Access Restrictions (Advanced)
*Note: This can be deferred to later phases for MVP*
- [ ] Modify memory read/write methods to check DMA state
- [ ] Return 0xFF for invalid reads during DMA
- [ ] Ignore invalid writes during DMA
- [ ] Allow only HRAM/I/O access during DMA

### Task 5: Comprehensive Unit Tests
- [ ] **File**: `src/GameBoy.Tests/DmaTests.cs` (new file)
- [ ] Test DMA register write triggers transfer
- [ ] Test successful transfer from various source addresses
- [ ] Test source address validation (reject invalid ranges)
- [ ] Test transfer copies exactly 160 bytes
- [ ] Test DMA timing progression
- [ ] Test edge cases (boundary addresses, invalid sources)

### Task 6: Integration Tests
- [ ] **File**: `src/GameBoy.Tests/Integration/MooneyeDmaTests.cs` (new file)
- [ ] Integrate with existing Mooneye test harness
- [ ] Add test methods for `oam_dma_restart.gb` and `oam_dma_timing.gb`
- [ ] Validate against hardware-accurate test ROMs

## ✅ Acceptance Criteria

### Core Functionality
- [ ] Writing to DMA register (0xFF46) immediately copies 160 bytes to OAM
- [ ] Source address calculation: `sourceAddr = (dmaValue << 8)`
- [ ] Transfer copies from `sourceAddr+0x00` to `sourceAddr+0x9F`
- [ ] Destination is always OAM region (0xFE00 to 0xFE9F)
- [ ] Invalid source addresses (0xE000-0xFFFF) are rejected or handled gracefully

### Testing Requirements
- [ ] All new unit tests pass (minimum 10 test cases)
- [ ] Existing test suite continues to pass (743+ tests)
- [ ] `dotnet test` completes successfully with no failures
- [ ] `dotnet format --verify-no-changes` passes (code formatting)

### Integration Requirements
- [ ] DMA timing integrates with emulator cycle progression
- [ ] Mooneye DMA test ROMs can be loaded and executed
- [ ] Test harness detects DMA-related test completion signals

### Documentation
- [ ] Add XML documentation comments to all new public methods
- [ ] Update relevant comments explaining DMA transfer semantics
- [ ] Add code examples demonstrating DMA usage

## 🧪 Testing Strategy

### Unit Test Cases
```csharp
[Theory]
[InlineData(0x80, 0x8000)] // Copy from VRAM
[InlineData(0xC0, 0xC000)] // Copy from Work RAM
[InlineData(0xD0, 0xD000)] // Copy from Work RAM high
public void DMA_ValidSourceAddresses_CopiesCorrectly(byte dmaValue, ushort expectedSource);

[Theory]
[InlineData(0xE0)] // Echo RAM - invalid
[InlineData(0xFF)] // I/O/HRAM - invalid
public void DMA_InvalidSourceAddresses_HandledGracefully(byte dmaValue);

[Fact]
public void DMA_Transfer_Copies160Bytes();

[Fact]
public void DMA_Timing_ProgressesCorrectly();
```

### Integration Tests
- **Test ROM**: `mooneye-gb/acceptance/oam_dma/oam_dma_timing.gb`
- **Test ROM**: `mooneye-gb/acceptance/oam_dma/oam_dma_restart.gb`
- **Validation**: Test completion detected via Mooneye harness

## 📚 References

### Implementation Guidance
- **Game Boy CPU Manual**: Section on DMA transfer timing
- **Phase 7 Requirements**: See `README.md` lines 315-327
- **Test ROM Strategy**: See `README.md` line 481 (Mooneye: oam_dma_*)
- **Existing Code**: `src/GameBoy.Core/Mmu.cs` WriteIoRegister method

### Related Files
- `src/GameBoy.Core/Mmu.cs` - Memory management and I/O registers
- `src/GameBoy.Core/IoRegs.cs` - DMA register constant (0xFF46)
- `src/GameBoy.Tests/MmuTests.cs` - Existing DMA register tests
- `src/GameBoy.Tests/Integration/MooneyeHarness.cs` - Test ROM harness

### Development Context
- **Current Phase**: Phase 3+ complete (CPU, interrupts, timers implemented)
- **Target Milestone**: v0.5 (MBC1/3/5 + Joypad + DMA - games boot)
- **Build Requirements**: .NET 8.0, xUnit testing framework
- **Code Style**: Use `dotnet format` for consistent formatting

## 🎯 Definition of Done

This issue is complete when:
1. ✅ All acceptance criteria are met
2. ✅ Unit tests achieve >90% code coverage for new DMA functionality
3. ✅ Integration tests with Mooneye test ROMs pass
4. ✅ No regressions in existing test suite
5. ✅ Code follows project formatting standards
6. ✅ Implementation documented with clear XML comments
7. ✅ Manual testing confirms DMA works in emulator context

---

**Priority**: High  
**Effort**: Medium (2-3 days)  
**Dependencies**: Phase 3 (CPU), Phase 4 (Interrupts) - ✅ Complete  
**Assignee**: AI Coding Agent  
**Labels**: `enhancement`, `phase-7`, `dma`, `game-boy-hardware`