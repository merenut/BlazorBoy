# BlazorBoy Game Boy Emulator
BlazorBoy is a Game Boy (DMG) emulator built with .NET 8.0 and Blazor WebAssembly. The project has advanced significantly through Phase 3 of development with a complete CPU instruction set implementation and comprehensive test coverage.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively
- Bootstrap, build, and test the repository:
  - `dotnet restore` -- takes 1-2 seconds when packages cached, up to 30 seconds on first run. NEVER CANCEL. Set timeout to 60+ seconds.
  - `dotnet build` -- takes 3-15 seconds depending on configuration. NEVER CANCEL. Set timeout to 60+ seconds.
  - `dotnet build --configuration Release` -- takes 3-5 seconds, optimized for production.
- `dotnet test` -- takes 2-5 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
  - **EXPECTED**: All 460 tests pass. This indicates the CPU instruction set is complete and core functionality is working.
  - CPU instruction set implementation is 100% complete with comprehensive test coverage.
- Run the web application:
  - ALWAYS run the build steps first.
  - Development: `cd src/GameBoy.Blazor && dotnet run --urls="http://0.0.0.0:5000"`
  - Production: `cd src/GameBoy.Blazor && dotnet run --configuration Release --urls="http://0.0.0.0:5000"`
- **CRITICAL TIMING**: All builds complete in under 30 seconds. If any command takes longer than 60 seconds, investigate.

## Validation
- ALWAYS manually validate the Blazor application after making changes:
  - Navigate to `http://localhost:5000` after starting the app
  - Verify the Game Boy interface loads with canvas and file upload button
  - Check that FPS counter shows ~58-60 FPS
  - Click on the canvas area and test keyboard inputs (arrow keys, Z, X, Enter, Shift)
  - Verify no console errors in browser developer tools
- Cannot load actual ROM files for testing without user-provided ROMs - this is expected
- ALWAYS run `dotnet format` before committing changes or CI will fail due to formatting violations
- Tests should all pass (460 passing tests) - investigate if any tests start failing unexpectedly

## Common tasks
The following are outputs from frequently run commands. Reference them instead of viewing, searching, or running bash commands to save time.

### Repository structure
```
BlazorBoy/
├── src/
│   ├── GameBoy.Core/           # Core emulator logic (19 C# files)
│   │   ├── Cpu.cs              # Game Boy CPU implementation  
│   │   ├── Mmu.cs              # Memory Management Unit
│   │   ├── Ppu.cs              # Picture Processing Unit
│   │   ├── Emulator.cs         # Main emulator facade
│   │   ├── Cartridge.cs        # ROM cartridge handling
│   │   ├── CartridgeHeader.cs  # ROM header parsing
│   │   ├── Mbc0.cs            # Memory Bank Controller Type 0
│   │   ├── Mbc1.cs            # Memory Bank Controller Type 1
│   │   ├── Mbc3.cs            # Memory Bank Controller Type 3
│   │   ├── Mbc5.cs            # Memory Bank Controller Type 5
│   │   ├── Timer.cs           # Timer implementation
│   │   ├── Joypad.cs          # Input handling
│   │   ├── InterruptController.cs # Interrupt management
│   │   ├── IoRegs.cs          # I/O register definitions
│   │   ├── OpcodeTable.cs     # Complete CPU instruction table
│   │   ├── Instruction.cs     # Instruction metadata structure
│   │   ├── Alu.cs             # Arithmetic Logic Unit helpers
│   │   └── IBatteryBacked.cs  # Battery backup interface
│   ├── GameBoy.Blazor/         # WebAssembly frontend (12 files)
│   │   ├── Pages/Index.razor   # Main Game Boy interface
│   │   ├── wwwroot/js/emulator.js # JavaScript interop for rendering
│   │   └── Program.cs          # Blazor app startup
│   └── GameBoy.Tests/          # xUnit test project (24 C# files)
│       ├── CpuTests.cs         # CPU instruction tests
│       ├── MmuTests.cs         # Memory management tests
│       ├── EmulatorTests.cs    # Integration tests
│       ├── TimerTests.cs       # Timer functionality tests
│       ├── OpcodeTableCoverageTests.cs # Instruction coverage validation
│       ├── InstructionDecodingTests.cs # Instruction metadata tests
│       ├── CartridgeTests.cs   # ROM loading tests
│       ├── Mbc1Tests.cs        # MBC1 implementation tests
│       ├── Mbc3Tests.cs        # MBC3 implementation tests
│       └── Mbc5Tests.cs        # MBC5 implementation tests
├── docs/                       # Comprehensive documentation
│   ├── InstructionCoverageSummary.md  # CPU instruction implementation status
│   ├── InstructionSetArchitecture.md # Complete instruction documentation
│   ├── OpcodeTableImplementation.md  # Technical implementation details
│   ├── InstructionTiming.md    # CPU timing documentation
│   └── TestROMs.md            # Test ROM usage guide
├── BlazorBoy.sln              # Solution file
└── README.md                  # Comprehensive development plan
```

### Key project details
- **Target Framework**: .NET 8.0
- **Frontend**: Blazor WebAssembly with Canvas2D rendering
- **Test Framework**: xUnit with 460 tests (all passing)
- **Dependencies**: Microsoft.AspNetCore.Components.WebAssembly, xunit packages
- **Development Status**: Phase 3 complete - CPU instruction set 100% implemented
- **Documentation**: Comprehensive technical documentation in docs/ folder

### Build configuration details
- **Debug build**: Includes debugging symbols, faster compilation (~3-15 seconds)
- **Release build**: Optimized for production, includes compressed assets (~3-5 seconds)
- **No additional linting tools**: Use `dotnet format` for code formatting
- **No CI/CD setup**: Planned for Phase 16 (see README.md)

### Current implementation status
- ✅ Basic project structure and Blazor frontend
- ✅ Complete CPU instruction set (245/245 primary + 256/256 CB opcodes)
- ✅ Full MMU with memory banking and I/O register handling
- ✅ Multiple Memory Bank Controllers (MBC0, MBC1, MBC3, MBC5)
- ✅ Cartridge loading and header parsing
- ✅ Timer system (DIV/TIMA/TMA/TAC registers)
- ✅ Interrupt controller with proper priority handling
- ✅ Joypad input system
- ✅ File upload interface for ROM loading
- ✅ JavaScript interop for canvas rendering
- ✅ Real-time frame stepping with FPS counter
- ✅ Comprehensive test coverage (460 passing tests)
- ✅ Extensive technical documentation
- ❌ PPU pixel rendering (stub implementation)
- ❌ Audio Processing Unit (APU)
- ❌ Save state functionality
- ❌ Debug tooling and disassembler UI

### Manual validation checklist
After making changes, always verify:
1. `dotnet build` succeeds without warnings
2. `dotnet format --verify-no-changes` passes (or run `dotnet format` to fix)
3. Blazor app starts without errors (`dotnet run` from GameBoy.Blazor folder)
4. Browser loads Game Boy interface at http://localhost:5000
5. FPS counter displays and updates (~58-60 FPS)
6. Canvas area accepts keyboard focus when clicked
7. No JavaScript errors in browser console
8. File upload button is visible and functional

### Common issues and solutions
- **Formatting errors**: Run `dotnet format` to automatically fix whitespace and style issues
- **Unexpected test failures**: All 460 tests should pass - investigate immediately if any fail
- **Build errors after changes**: Check that all using statements are correct and project references are intact
- **Blazor app won't start**: Verify GameBoy.Core builds successfully first
- **Performance issues**: Use Release configuration for production testing
- **ROM compatibility**: Most common Game Boy ROMs should work with current MBC implementations

### Development workflow
1. Make changes to GameBoy.Core for emulator logic
2. Update GameBoy.Blazor for UI changes  
3. Add tests to GameBoy.Tests for new functionality
4. Run `dotnet format` to fix any style issues
5. Build and test: `dotnet build && dotnet test`
6. Verify all 460 tests still pass
7. Manual validation: Start Blazor app and test functionality
8. Check relevant documentation in docs/ folder for implementation guidance
9. Commit changes with descriptive messages

### Documentation Resources
The project includes comprehensive technical documentation:
- **docs/InstructionCoverageSummary.md**: Quick reference for CPU instruction implementation status
- **docs/InstructionSetArchitecture.md**: Complete instruction tables and implementation details  
- **docs/OpcodeTableImplementation.md**: Technical details of opcode table structure
- **docs/InstructionTiming.md**: CPU timing and cycle accuracy documentation
- **docs/TestROMs.md**: Guide for using test ROMs to validate emulator accuracy
- **README.md**: Complete development plan and phase breakdown

Always consult these docs before making changes to CPU, instruction handling, or timing-critical code.

### Important implementation notes
- Follow the 16-phase development plan outlined in README.md (currently in Phase 3+)
- CPU instruction set is 100% complete - focus should be on PPU, APU, and advanced features
- Emulator uses cycle-accurate timing (70,224 cycles per frame)
- JavaScript interop required for canvas rendering and audio (future)
- ROMs are loaded via browser file upload (no built-in test ROMs)
- Game Boy register values and timing must match original hardware specifications
- Comprehensive documentation available in docs/ folder for technical details
- Test coverage validates instruction correctness and timing accuracy