# Implement Phase 7: DMA (Direct Memory Access) Transfer

## 🎯 Overview

Implement Game Boy DMA functionality as specified in **Phase 7** of the development plan. DMA enables rapid copying of sprite data from main memory to OAM for efficient sprite rendering.

## 📋 Current State & Problem

- ✅ DMA register (0xFF46) exists with basic read/write
- ❌ **Missing**: Actual DMA transfer logic when register is written  
- ❌ **Missing**: DMA transfer timing (~640 cycles)
- ❌ **Missing**: Comprehensive test coverage

**Impact**: Games cannot efficiently update sprites, blocking progress toward milestone v0.5

## 🔧 Technical Requirements

### DMA Specification
- **Register**: 0xFF46 (already defined)
- **Transfer**: Copy 160 bytes from `(DMA_value << 8) + 0x00-0x9F` to OAM (0xFE00-0xFE9F)
- **Timing**: ~640 cycles (160 bytes × 4 cycles/byte)
- **Restrictions**: Source cannot be 0xE000-0xFFFF (echo RAM, I/O, HRAM)

## 📝 Implementation Tasks

### Core Implementation
- [ ] **Modify** `src/GameBoy.Core/Mmu.cs` WriteIoRegister DMA case
- [ ] Add `StartDmaTransfer(byte sourcePageHigh)` method
- [ ] Implement immediate 160-byte copy from source to OAM
- [ ] Add DMA state tracking fields (`_dmaActive`, `_dmaRemainingCycles`)
- [ ] Add `StepDma(int cycles)` for timing progression
- [ ] Integrate DMA step into emulator cycle loop

### Testing Requirements  
- [ ] **Create** `src/GameBoy.Tests/DmaTests.cs`
- [ ] Test DMA register write triggers transfer
- [ ] Test transfer copies exactly 160 bytes correctly
- [ ] Test source address validation
- [ ] Test DMA timing progression
- [ ] **Create** `src/GameBoy.Tests/Integration/MooneyeDmaTests.cs`
- [ ] Integrate Mooneye `oam_dma_*` test ROMs

## ✅ Acceptance Criteria

### Functional Requirements
- [ ] Writing 0xFF46 immediately copies 160 bytes to OAM (0xFE00-0xFE9F)
- [ ] Source calculation: `sourceAddr = (dmaValue << 8) + offset`
- [ ] Invalid source addresses (0xE000-0xFFFF) handled gracefully
- [ ] DMA timing integrates with emulator cycle progression

### Quality Requirements
- [ ] All new unit tests pass (minimum 8 test cases)
- [ ] All existing tests continue passing (743+ tests)
- [ ] `dotnet format --verify-no-changes` passes
- [ ] Mooneye DMA test ROMs can execute via test harness

### Implementation Quality
- [ ] XML documentation on all new public methods
- [ ] Code follows existing project patterns (see `Timer.cs` for reference)
- [ ] No breaking changes to existing public APIs

## 🧪 Key Test Cases

```csharp
[Theory]
[InlineData(0x80, 0x8000)] // VRAM source
[InlineData(0xC0, 0xC000)] // Work RAM source
public void DMA_ValidSources_CopiesCorrectly(byte dmaValue, ushort expectedSource);

[Theory]  
[InlineData(0xE0)] // Echo RAM - invalid
[InlineData(0xFF)] // I/O region - invalid
public void DMA_InvalidSources_HandledGracefully(byte dmaValue);

[Fact]
public void DMA_Transfer_Copies160BytesToOAM();

[Fact]
public void DMA_Timing_Takes640Cycles();
```

## 📚 References

- **Phase 7 Spec**: `README.md` lines 315-327
- **Test Strategy**: `README.md` line 481 (Mooneye: oam_dma_*)
- **Existing Code**: `src/GameBoy.Core/Mmu.cs` WriteIoRegister method
- **Test Infrastructure**: `src/GameBoy.Tests/Integration/MooneyeHarness.cs`

## 🎯 Definition of Done

- ✅ All acceptance criteria met
- ✅ Unit test coverage >90% for DMA functionality  
- ✅ Mooneye DMA test ROMs pass
- ✅ No regressions in existing 743+ test suite
- ✅ Code formatted and documented

---

**Priority**: High | **Effort**: 2-3 days | **Phase**: 7 | **Milestone**: v0.5