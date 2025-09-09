# Implement Phase 9: Serial Communication Port

## 🎯 Overview

Implement Game Boy Serial Communication Port functionality as specified in **Phase 9** of the development plan. The serial port enables data transfer between Game Boy units via link cable, though this implementation will be a compatibility stub focusing on register semantics and interrupt generation.

## 📋 Current State & Problem

- ✅ MMU I/O region supports 0xFF01-0xFF02 range
- ✅ InterruptController supports Serial interrupt type
- ❌ **Missing**: Serial port register implementation (SB, SC)
- ❌ **Missing**: Transfer timing and interrupt generation  
- ❌ **Missing**: Serial port integration with emulator cycle loop
- ❌ **Missing**: Comprehensive test coverage

**Impact**: Games using serial communication (debugging, multiplayer) may not function correctly. Blocks progress toward milestone v0.5.

## 🔧 Technical Requirements

### Serial Port Specification
- **SB Register (0xFF01)**: Serial transfer data (8-bit read/write)
- **SC Register (0xFF02)**: Serial transfer control
  - Bit 7: Transfer start flag (1=in progress, 0=complete)
  - Bit 1: Clock speed (unused on DMG, always 0)
  - Bit 0: Clock source (1=internal, 0=external)
  - Bits 2-6: Unused, read as 1 on DMG hardware
- **Transfer Timing**: ~4096 cycles total when using internal clock
- **Interrupt**: Triggered when transfer completes (SC bit 7 cleared)

### Hardware Accuracy Requirements
- SC register upper bits (2-6) must read as 1 on DMG
- Transfer start bit automatically clears on completion
- Serial interrupt triggered only after full transfer timing
- External clock mode should handle gracefully (no hanging)

## 📝 Implementation Tasks

### Core Implementation
- [ ] **Create** `src/GameBoy.Core/SerialPort.cs`
- [ ] Add private fields: `_sbRegister`, `_scRegister`, `_transferActive`, `_transferCycles`
- [ ] Implement `ReadRegister(ushort address)` method
- [ ] Implement `WriteRegister(ushort address, byte value)` method
- [ ] Implement `Step(int cycles)` method for transfer timing
- [ ] Add transfer completion logic with interrupt triggering
- [ ] **Modify** `src/GameBoy.Core/Mmu.cs` to integrate SerialPort
- [ ] **Modify** `src/GameBoy.Core/Emulator.cs` to step SerialPort in main loop

### I/O Register Integration
- [ ] Route 0xFF01 reads/writes to SerialPort.ReadRegister/WriteRegister
- [ ] Route 0xFF02 reads/writes to SerialPort.ReadRegister/WriteRegister
- [ ] Ensure proper integration with existing I/O register framework
- [ ] Validate register access doesn't break existing functionality

### Testing Requirements  
- [ ] **Create** `src/GameBoy.Tests/SerialPortTests.cs`
- [ ] Test SB register read/write behavior
- [ ] Test SC register bit semantics (especially upper bits read as 1)
- [ ] Test transfer start bit triggers timing countdown
- [ ] Test transfer completion clears start bit and triggers interrupt
- [ ] Test external clock mode handling
- [ ] Test integration with MMU I/O routing
- [ ] **Create** `src/GameBoy.Tests/Integration/SerialIntegrationTests.cs`
- [ ] Test serial functionality within full emulator context

## ✅ Acceptance Criteria

### Functional Requirements
- [ ] SB register (0xFF01) supports full 8-bit read/write
- [ ] SC register (0xFF02) implements correct bit semantics
- [ ] SC bits 2-6 always read as 1 (DMG hardware behavior)
- [ ] Writing SC bit 7=1 starts transfer timing (~4096 cycles)
- [ ] Transfer completion automatically clears SC bit 7
- [ ] Serial interrupt triggered on transfer completion (if IE bit 3 set)
- [ ] External clock mode handled without hanging emulator

### Quality Requirements
- [ ] All new unit tests pass (minimum 10 test cases)
- [ ] All existing tests continue passing (765+ tests)
- [ ] `dotnet format --verify-no-changes` passes
- [ ] No performance regression in emulator cycle timing
- [ ] XML documentation on all public methods and properties

### Implementation Quality
- [ ] SerialPort class follows existing project patterns (see `Timer.cs`)
- [ ] Proper integration with InterruptController API
- [ ] No breaking changes to existing MMU or Emulator APIs
- [ ] Code coverage >90% for SerialPort functionality

## 🧪 Key Test Cases

```csharp
[Fact]
public void SB_Register_SupportsFullReadWrite()
{
    // Test 0xFF01 register stores and returns written values
}

[Fact] 
public void SC_UpperBits_ReadAsOnes()
{
    // Test bits 2-6 of SC register always read as 1
}

[Theory]
[InlineData(0x80)] // Internal clock, transfer start
[InlineData(0x81)] // External clock, transfer start  
public void SC_TransferStart_TriggersTransferTiming(byte scValue)
{
    // Test SC bit 7 starts transfer countdown
}

[Fact]
public void SerialTransfer_Completion_ClearsBitAndTriggersInterrupt()
{
    // Test full transfer cycle with interrupt generation
}

[Fact]
public void SerialPort_ExternalClock_DoesNotHang()
{
    // Test external clock mode handles gracefully
}

[Fact]
public void SerialPort_Integration_WithEmulatorCycleLoop()
{
    // Test SerialPort.Step called from Emulator.StepFrame
}
```

## 📚 References

- **Phase 9 Spec**: `README.md` lines 341-377 (newly expanded)
- **DMG Hardware Manual**: Serial Data Transfer section
- **Existing Code**: `src/GameBoy.Core/Timer.cs` (similar cycle-based component)
- **I/O Integration**: `src/GameBoy.Core/Mmu.cs` ReadIoRegister/WriteIoRegister
- **Interrupt System**: `src/GameBoy.Core/InterruptController.cs`

## 🎯 AI Agent Implementation Guidelines

### Code Structure Pattern
Follow the established pattern from `Timer.cs`:
- Private state fields with clear naming
- Public `Step(int cycles)` method for timing progression  
- Register read/write methods with address validation
- Proper XML documentation on public members

### Integration Points
1. **MMU Integration**: Add SerialPort instance to Mmu constructor
2. **Emulator Integration**: Add `_serialPort.Step(cycles)` to emulator loop
3. **Interrupt Integration**: Use `_interruptController.RequestInterrupt(InterruptType.Serial)`

### Testing Strategy
- Start with isolated SerialPort unit tests
- Add MMU integration tests for register routing
- Create emulator-level integration tests
- Ensure no regressions in existing test suite

### Common Pitfalls to Avoid
- Don't implement actual link cable communication (out of scope)
- Don't forget SC upper bits must read as 1 on DMG
- Don't block emulator when external clock selected
- Don't trigger interrupt immediately on transfer start

## 🎯 Definition of Done

- ✅ All acceptance criteria met
- ✅ Unit test coverage >90% for SerialPort functionality  
- ✅ Integration tests demonstrate proper emulator integration
- ✅ No regressions in existing 765+ test suite
- ✅ Code formatted and documented per project standards
- ✅ Performance validated (no cycle timing regression)

---

**Priority**: Medium | **Effort**: 1-2 days | **Phase**: 9 | **Milestone**: v0.5
**Dependencies**: MMU I/O framework, InterruptController
**AI Agent Complexity**: Low - Well-defined register semantics with clear hardware specification