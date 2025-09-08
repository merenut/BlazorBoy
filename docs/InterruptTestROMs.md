# Interrupt Test ROM Harness Documentation

This document describes the interrupt-specific test ROM harness implementation in BlazorBoy, including how to run and interpret results for both Blargg and Mooneye interrupt test ROMs.

## Overview

The interrupt test harness provides specialized testing infrastructure for validating Game Boy interrupt behavior. It extends the existing test framework with interrupt-specific detection logic, monitoring capabilities, and reporting features.

## Key Components

### InterruptTestHarness

The `InterruptTestHarness` class provides enhanced functionality specifically for interrupt testing:

- **Interrupt Event Monitoring**: Tracks changes to IF (Interrupt Flags) and IE (Interrupt Enable) registers
- **Test-Type-Specific Detection**: Different completion detection logic for Blargg vs Mooneye ROMs
- **Enhanced Timeout**: Longer execution limits for complex interrupt timing tests
- **Detailed Reporting**: Comprehensive results with interrupt event history

### Test Classes

- **BlarggInterruptTests**: Tests for Blargg interrupt validation ROMs
- **MooneyeInterruptTests**: Tests for Mooneye interrupt timing and edge case ROMs

## Setting Up Interrupt Test ROMs

### Directory Structure

Create the following directory structure for interrupt test ROMs:

```
src/GameBoy.Tests/
├── TestRoms/
│   ├── Blargg/
│   │   ├── cpu_instrs/
│   │   │   └── individual/
│   │   │       └── 02-interrupts.gb
│   │   ├── interrupt_tests/
│   │   │   ├── ie_push.gb
│   │   │   ├── if_ie_registers.gb
│   │   │   └── intr_timing.gb
│   │   ├── interrupt_timing.gb
│   │   ├── interrupt_priority.gb
│   │   └── interrupt_tests.gb
│   └── Mooneye/
│       ├── interrupts/
│       │   ├── ei_sequence.gb
│       │   ├── ei_timing.gb
│       │   ├── halt_ime0_ei.gb
│       │   ├── halt_ime1_timing.gb
│       │   ├── if_ie_registers.gb
│       │   ├── intr_timing.gb
│       │   ├── rapid_di_ei.gb
│       │   ├── reti_intr_timing.gb
│       │   └── reti_timing.gb
│       ├── timer/
│       │   ├── div_write.gb
│       │   ├── tim00.gb
│       │   ├── tim01.gb
│       │   ├── tim10.gb
│       │   ├── tim11.gb
│       │   ├── tima_reload.gb
│       │   └── rapid_toggle.gb
│       ├── ppu/
│       │   ├── intr_1_2_timing-GS.gb
│       │   ├── intr_2_0_timing.gb
│       │   ├── intr_2_mode0_timing.gb
│       │   ├── intr_2_mode3_timing.gb
│       │   ├── stat_irq_blocking.gb
│       │   └── vblank_stat_intr-GS.gb
│       └── serial/
│           └── boot_sclk_align-dmgABCmgb.gb
```

### Obtaining Interrupt Test ROMs

**Blargg Test ROMs:**
- Original Blargg CPU test suite includes `02-interrupts.gb`
- Standalone interrupt test collections may be available from emulation communities
- Some tests can be built from available source code

**Mooneye Test ROMs:**
- Available from the Mooneye-GB test suite project
- Comprehensive collection covering interrupt timing edge cases
- Organized by component (interrupts, timer, ppu, serial)

## Running Interrupt Tests

### Automated Test Execution

Run all interrupt tests:
```bash
dotnet test --filter "Category=InterruptTests"
```

Run only Blargg interrupt tests:
```bash
dotnet test --filter "ClassName~BlarggInterruptTests"
```

Run only Mooneye interrupt tests:
```bash
dotnet test --filter "ClassName~MooneyeInterruptTests"
```

### Manual Test Execution

You can run individual interrupt tests programmatically:

```csharp
var harness = new InterruptTestHarness();

// Run a Blargg interrupt test
var blarggResult = harness.RunInterruptTest(
    "TestRoms/Blargg/cpu_instrs/individual/02-interrupts.gb",
    InterruptTestType.Blargg);

// Run a Mooneye interrupt test
var mooneyeResult = harness.RunInterruptTest(
    "TestRoms/Mooneye/interrupts/ei_timing.gb", 
    InterruptTestType.Mooneye);

// Examine results
Console.WriteLine($"Test passed: {blarggResult.Passed}");
Console.WriteLine($"Output: {blarggResult.Output}");
Console.WriteLine($"Execution time: {blarggResult.ExecutionTime}");
Console.WriteLine($"Interrupt events captured: {blarggResult.InterruptEvents?.Count ?? 0}");
```

## Test Result Interpretation

### InterruptTestResult Structure

```csharp
public record InterruptTestResult(
    bool Passed,                           // Test pass/fail status
    string Output,                         // Test output message
    TimeSpan ExecutionTime,                // Time taken to complete
    InterruptTestType TestType,            // Blargg or Mooneye
    List<InterruptEvent>? InterruptEvents  // Captured interrupt events
);
```

### InterruptEvent Structure

```csharp
public record InterruptEvent(
    int Frame,                // Frame number when event occurred
    byte IFBefore,           // IF register value before step
    byte IFAfter,            // IF register value after step  
    byte IEBefore,           // IE register value before step
    byte IEAfter             // IE register value after step
);
```

### Pass Criteria

#### Blargg Interrupt Tests
- Serial output indicates success ("passed", "ok", "success")
- Memory signature at 0xA000 contains "PASS" (0x50415353)
- Test-specific completion patterns (e.g., 0xA002 == 0x00)
- No failure indicators present

#### Mooneye Interrupt Tests
- Memory signature at 0xFF80 matches expected completion pattern
- Register-based completion with correct patterns
- VRAM pattern validation for visual tests
- Test-specific success indicators

### Debugging Failed Tests

#### Common Interrupt Test Failures

| Failure Type | Likely Cause | Debug Steps |
|--------------|--------------|-------------|
| Timeout | Infinite loop in interrupt handler | Check interrupt vector implementation |
| Timing Error | Incorrect interrupt timing | Verify cycle-accurate interrupt processing |
| Flag Issues | IF/IE register bugs | Check interrupt flag handling logic |
| Priority Error | Wrong interrupt priority | Verify interrupt priority implementation |
| HALT Issues | HALT instruction bugs | Check HALT behavior with/without IME |

#### Using Interrupt Events for Debugging

Examine the `InterruptEvents` list to understand interrupt behavior:

```csharp
foreach (var evt in result.InterruptEvents)
{
    Console.WriteLine($"Frame {evt.Frame}:");
    Console.WriteLine($"  IF: {evt.IFBefore:X2} -> {evt.IFAfter:X2}");
    Console.WriteLine($"  IE: {evt.IEBefore:X2} -> {evt.IEAfter:X2}");
    
    // Decode interrupt types
    var ifChanges = evt.IFBefore ^ evt.IFAfter;
    if ((ifChanges & 0x01) != 0) Console.WriteLine("  VBlank interrupt");
    if ((ifChanges & 0x02) != 0) Console.WriteLine("  LCD STAT interrupt");
    if ((ifChanges & 0x04) != 0) Console.WriteLine("  Timer interrupt");
    if ((ifChanges & 0x08) != 0) Console.WriteLine("  Serial interrupt");
    if ((ifChanges & 0x10) != 0) Console.WriteLine("  Joypad interrupt");
}
```

## Specific Test Categories

### Blargg Interrupt Tests

#### 02-interrupts.gb
- **Purpose**: Basic interrupt handling validation
- **Tests**: All interrupt types, basic timing
- **Expected Events**: Multiple interrupt flag changes
- **Typical Duration**: 30-60 seconds

#### interrupt_timing.gb (if available)
- **Purpose**: Precise interrupt timing validation
- **Tests**: Cycle-accurate interrupt handling
- **Expected Events**: Timing-sensitive interrupt sequences
- **Typical Duration**: 1-3 minutes

#### interrupt_priority.gb (if available)
- **Purpose**: Interrupt priority handling
- **Tests**: Multiple simultaneous interrupts
- **Expected Events**: Priority-ordered interrupt processing
- **Typical Duration**: 30-90 seconds

### Mooneye Interrupt Tests

#### EI/DI Timing Tests
- **Files**: `ei_timing.gb`, `ei_sequence.gb`, `rapid_di_ei.gb`
- **Purpose**: Enable/disable interrupt instruction timing
- **Critical**: IME (Interrupt Master Enable) timing behavior
- **Expected**: Precise timing of interrupt enable/disable

#### HALT Instruction Tests  
- **Files**: `halt_ime0_*.gb`, `halt_ime1_*.gb`
- **Purpose**: HALT behavior with different IME states
- **Critical**: HALT bug behavior, interrupt awakening
- **Expected**: Correct HALT/wake sequences

#### Timer Interrupt Tests
- **Files**: `tim00.gb` through `tim11.gb`, `tima_reload.gb`
- **Purpose**: Timer interrupt timing and behavior
- **Critical**: TIMA overflow, reload timing
- **Expected**: Timer interrupt events at specific intervals

#### PPU Interrupt Tests
- **Files**: `intr_2_*.gb`, `stat_*.gb`, `vblank_*.gb`
- **Purpose**: PPU-related interrupt timing
- **Critical**: LCD STAT interrupt conditions, VBlank timing
- **Expected**: PPU-synchronized interrupt events

## Performance Considerations

### Expected Execution Times

| Test Category | Typical Duration | Maximum Duration |
|---------------|------------------|------------------|
| Basic interrupt tests | 30-90 seconds | 3 minutes |
| Timing-sensitive tests | 1-3 minutes | 5 minutes |
| Comprehensive suites | 2-5 minutes | 10 minutes |

### Optimization Tips

1. **Run in Release Mode**: Use optimized builds for faster execution
2. **Selective Testing**: Focus on failing tests during development
3. **Parallel Execution**: Independent tests can run simultaneously
4. **Monitor Frequency**: Adjust `ResultsCheckInterval` for performance vs accuracy

## Integration with CI/CD

The interrupt test framework is designed for automated environments:

- **Graceful Skipping**: Tests skip when ROM files unavailable
- **Standard Reporting**: xUnit-compatible test results
- **Performance Metrics**: Execution time monitoring
- **Event Logging**: Detailed interrupt behavior logging

### GitHub Actions Example

```yaml
- name: Run Interrupt Tests
  run: dotnet test --filter "Category=InterruptTests" --logger trx
  
- name: Upload Interrupt Test Results
  uses: actions/upload-artifact@v3
  if: always()
  with:
    name: interrupt-test-results
    path: TestResults/*.trx
```

## Troubleshooting

### Common Issues

#### Tests Always Skip
- **Cause**: ROM files not found
- **Solution**: Verify directory structure and file permissions
- **Check**: Use diagnostic tests to list available ROMs

#### Tests Timeout
- **Cause**: Infinite loops or missing interrupt implementation
- **Solution**: Check interrupt vector handling and HALT behavior
- **Debug**: Examine interrupt events to identify stuck states

#### Inconsistent Results
- **Cause**: Timing-dependent behavior or race conditions
- **Solution**: Verify cycle-accurate interrupt timing
- **Debug**: Compare interrupt event timing across runs

#### Wrong Pass/Fail Detection
- **Cause**: Incorrect completion detection logic
- **Solution**: Verify memory signatures and output patterns
- **Debug**: Examine ROM-specific completion methods

### Debug Output

Enable verbose output for debugging:

```csharp
var harness = new InterruptTestHarness();
var result = harness.RunInterruptTest(romPath, testType);

if (!result.Passed)
{
    Console.WriteLine($"Test failed: {result.Output}");
    Console.WriteLine($"Execution time: {result.ExecutionTime}");
    
    if (result.InterruptEvents != null)
    {
        Console.WriteLine($"Captured {result.InterruptEvents.Count} interrupt events");
        // Log detailed interrupt event analysis
    }
}
```

## Contributing

When adding new interrupt test ROMs or enhancing the harness:

1. **Follow Naming Conventions**: Use descriptive test names
2. **Add Appropriate Timeouts**: Account for test complexity
3. **Document Expected Behavior**: Include purpose and expected events
4. **Test Both Pass and Fail Cases**: Verify detection logic
5. **Update Documentation**: Keep this document current

## References

- [Pan Docs - Interrupts](https://gbdev.github.io/pandocs/Interrupts.html) - Comprehensive interrupt documentation
- [Blargg's Test ROMs](http://slack.net/~ant/libs/audio.html#Blargg_Test_ROMs) - Original test ROM documentation  
- [Mooneye-GB Test Suite](https://github.com/Gekkio/mooneye-gb) - Mooneye test ROM collection
- [Game Boy CPU Manual](https://gekkio.fi/files/gb-ctr.pdf) - Official interrupt behavior documentation
- [BlazorBoy Test ROM Documentation](TestROMs.md) - General test ROM usage guide