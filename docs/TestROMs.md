# Test ROM Documentation

This document describes how to use test ROMs with the BlazorBoy emulator, particularly the Blargg test ROMs for validating CPU instruction correctness.

## Overview

BlazorBoy includes automated test harnesses for running Game Boy test ROMs to validate emulator accuracy. The test infrastructure is designed to run CPU validation tests automatically and detect pass/fail status without manual intervention.

## Test ROM Categories

### Blargg Test ROMs (CPU Validation)

The Blargg test ROMs are comprehensive CPU validation tests that exercise every Game Boy instruction and verify correct behavior. These tests are essential for ensuring CPU implementation accuracy.

#### Required ROMs for Phase 3 (CPU Core Implementation)

| ROM File | Purpose | Expected Results |
|----------|---------|------------------|
| `cpu_instrs.gb` | Master CPU instruction test suite | All sub-tests must pass |
| `instr_timing.gb` | Instruction cycle timing validation | Timing accuracy verification |
| `mem_timing.gb` | Memory access timing | Memory timing accuracy |
| `mem_timing-2.gb` | Advanced memory timing | Extended timing tests |
| `halt_bug.gb` | HALT instruction edge cases | HALT behavior validation |

#### Individual CPU Instruction Tests

Located in `cpu_instrs/individual/` directory:

| ROM File | Instructions Tested |
|----------|-------------------|
| `01-special.gb` | NOP, STOP, HALT, DI, EI |
| `02-interrupts.gb` | Interrupt handling and timing |
| `03-op sp,hl.gb` | LD SP,HL; ADD SP,r8; LD HL,SP+r8 |
| `04-op r,imm.gb` | All immediate value operations |
| `05-op rp.gb` | 16-bit register pair operations |
| `06-ld r,r.gb` | 8-bit register-to-register loads |
| `07-jr,jp,call,ret,rst.gb` | Jump, call, return, restart |
| `08-misc instrs.gb` | Miscellaneous instructions |
| `09-op r,r.gb` | 8-bit register arithmetic/logic |
| `10-bit ops.gb` | CB-prefixed bit operations |
| `11-op a,(hl).gb` | Operations with A and (HL) |

## Setting Up Test ROMs

### Directory Structure

Create the following directory structure in your test project:

```
src/GameBoy.Tests/
├── TestRoms/
│   └── Blargg/
│       ├── cpu_instrs.gb
│       ├── instr_timing.gb
│       ├── mem_timing.gb
│       ├── mem_timing-2.gb
│       ├── halt_bug.gb
│       └── cpu_instrs/
│           └── individual/
│               ├── 01-special.gb
│               ├── 02-interrupts.gb
│               ├── 03-op sp,hl.gb
│               ├── 04-op r,imm.gb
│               ├── 05-op rp.gb
│               ├── 06-ld r,r.gb
│               ├── 07-jr,jp,call,ret,rst.gb
│               ├── 08-misc instrs.gb
│               ├── 09-op r,r.gb
│               ├── 10-bit ops.gb
│               └── 11-op a,(hl).gb
```

### Obtaining Test ROMs

**Important**: The actual ROM files are not included in this repository due to licensing considerations. You need to obtain them separately:

1. **Blargg Test ROMs**: Available from various Game Boy development resources
2. **Alternative Sources**: Look for "Game Boy test ROMs" or "GB test suite"
3. **Building from Source**: Some test ROMs can be built from source code if available

### Legal Notice

Test ROMs may be subject to copyright. Ensure you have the right to use any ROM files in your testing environment. Many test ROMs are released by their authors for emulator development purposes.

## Running Tests

### Automated Test Execution

Run all tests including ROM-based tests:

```bash
dotnet test
```

The test framework will automatically:
- Detect if ROM files are present
- Skip ROM-based tests if files are not available
- Run all available tests and report results

### Individual Test Categories

Run only unit tests (no ROMs required):
```bash
dotnet test --filter "Category!=Integration"
```

Run only integration tests (requires ROMs):
```bash
dotnet test --filter "Category=Integration"
```

Run specific ROM tests:
```bash
dotnet test --filter "MethodName~CpuInstrs"
```

### Manual ROM Testing

You can also run individual ROMs manually using the BlarggHarness:

```csharp
var harness = new BlarggHarness();
var result = harness.RunTest("path/to/cpu_instrs.gb");
Console.WriteLine($"Test result: {result.Passed}");
Console.WriteLine($"Output: {result.Output}");
Console.WriteLine($"Execution time: {result.ExecutionTime}");
```

## Test Result Interpretation

### Pass Criteria

A Blargg test ROM passes when:
1. The ROM signals completion within the timeout period (10,000 frames)
2. The test output indicates success (contains "passed", "ok", or "success")
3. No failure indicators are present ("failed", "error", "fail")

### Failure Analysis

When a test fails:
1. **Check Output**: The failure message often indicates which instruction failed
2. **Review Timing**: Some failures are due to incorrect instruction timing
3. **Verify Implementation**: Cross-reference with Pan Docs for correct behavior
4. **Debug Step-by-Step**: Use the emulator's step debugger for detailed analysis

### Common Issues

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| Test timeout | Infinite loop or missing instruction | Implement missing opcode |
| Incorrect timing | Wrong cycle counts | Verify instruction timing tables |
| Flag errors | ALU flag calculation bugs | Check flag setting logic |
| Memory issues | MMU implementation bugs | Verify memory mapping |

## Test Performance

### Expected Execution Times

| Test ROM | Typical Duration | Max Duration |
|----------|------------------|--------------|
| Individual tests | 10-30 seconds | 2 minutes |
| cpu_instrs.gb | 30-60 seconds | 5 minutes |
| Timing tests | 1-2 minutes | 5 minutes |

Tests that exceed these durations likely indicate implementation issues.

### Optimization Tips

1. **Run in Release Mode**: Use optimized builds for faster execution
2. **Parallel Execution**: Run independent tests in parallel
3. **Selective Testing**: Focus on failing tests during development

## Troubleshooting

### ROM Files Not Found

If tests are skipped due to missing ROMs:
1. Verify the directory structure matches the expected layout
2. Check file permissions (ROMs must be readable)
3. Ensure ROM files have the correct extensions (.gb)

### Test Failures

For systematic test failures:
1. Start with individual ROM tests to isolate issues
2. Verify basic instructions work before testing complex ones
3. Use unit tests to validate specific instruction behavior
4. Check that the emulator reset state matches Game Boy specifications

### Performance Issues

If tests run very slowly:
1. Profile the CPU step execution
2. Optimize instruction dispatch (hot path)
3. Reduce debug output during test runs
4. Consider test timeout adjustments

## Integration with CI/CD

The test framework is designed to work in CI environments:

- Tests automatically skip when ROM files are not available
- No manual intervention required
- Test results are reported in standard xUnit format
- Performance metrics are captured for regression detection

### GitHub Actions Example

```yaml
- name: Run Tests
  run: dotnet test --logger trx --results-directory TestResults
  
- name: Publish Test Results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Test Results
    path: TestResults/*.trx
    reporter: dotnet-trx
```

## Contributing

When adding new test ROMs or test categories:

1. Follow the existing directory structure
2. Add appropriate skip logic for missing ROMs
3. Document expected behavior and timing
4. Include both positive and negative test cases
5. Update this documentation with new ROM information

## References

- [Pan Docs](https://gbdev.github.io/pandocs/) - Comprehensive Game Boy documentation
- [Blargg's Test ROMs](http://slack.net/~ant/libs/audio.html#Blargg_Test_ROMs) - Original test ROM documentation
- [Game Boy CPU Manual](https://gekkio.fi/files/gb-ctr.pdf) - Official CPU documentation
- [BlazorBoy README](../README.md) - Project overview and development phases