# BlazorBoy Game Boy Emulator
BlazorBoy is a Game Boy (DMG) emulator built with .NET 8.0 and Blazor WebAssembly. The project is currently in early development with skeleton implementations in place.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively
- Bootstrap, build, and test the repository:
  - `dotnet restore` -- takes 1-2 seconds when packages cached, up to 30 seconds on first run. NEVER CANCEL. Set timeout to 60+ seconds.
  - `dotnet build` -- takes 3-15 seconds depending on configuration. NEVER CANCEL. Set timeout to 60+ seconds.
  - `dotnet build --configuration Release` -- takes 3-5 seconds, optimized for production.
- `dotnet test` -- takes 2-5 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
  - **EXPECTED**: 14 tests fail, 10 pass. This is normal - emulator implementation is incomplete.
  - Failing tests indicate MMU register handling is not fully implemented yet.
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
- Do not attempt to fix the 14 failing tests unless specifically working on MMU implementation - they fail due to incomplete core emulator logic

## Common tasks
The following are outputs from frequently run commands. Reference them instead of viewing, searching, or running bash commands to save time.

### Repository structure
```
BlazorBoy/
├── src/
│   ├── GameBoy.Core/           # Core emulator logic (11 C# files)
│   │   ├── Cpu.cs              # Game Boy CPU implementation
│   │   ├── Mmu.cs              # Memory Management Unit
│   │   ├── Ppu.cs              # Picture Processing Unit
│   │   ├── Emulator.cs         # Main emulator facade
│   │   ├── Cartridge.cs        # ROM cartridge handling
│   │   ├── Mbc0.cs            # Memory Bank Controller
│   │   ├── Timer.cs           # Timer implementation
│   │   ├── Joypad.cs          # Input handling
│   │   ├── InterruptController.cs
│   │   └── IoRegs.cs          # I/O register definitions
│   ├── GameBoy.Blazor/         # WebAssembly frontend (4 Razor files)
│   │   ├── Pages/Index.razor   # Main Game Boy interface
│   │   ├── wwwroot/js/emulator.js # JavaScript interop for rendering
│   │   └── Program.cs          # Blazor app startup
│   └── GameBoy.Tests/          # xUnit test project (12 C# files)
│       ├── CpuTests.cs
│       ├── MmuTests.cs
│       ├── EmulatorTests.cs
│       └── TimerTests.cs
├── BlazorBoy.sln              # Solution file
└── README.md                  # Comprehensive development plan
```

### Key project details
- **Target Framework**: .NET 8.0
- **Frontend**: Blazor WebAssembly with Canvas2D rendering
- **Test Framework**: xUnit with 24 tests (10 pass, 14 fail - expected)
- **Dependencies**: Microsoft.AspNetCore.Components.WebAssembly, xunit packages
- **Development Status**: Phase 1-2 of 16-phase plan (see README.md)

### Build configuration details
- **Debug build**: Includes debugging symbols, faster compilation (~3-15 seconds)
- **Release build**: Optimized for production, includes compressed assets (~3-5 seconds)
- **No additional linting tools**: Use `dotnet format` for code formatting
- **No CI/CD setup**: Planned for Phase 16 (see README.md)

### Current implementation status
- ✅ Basic project structure and Blazor frontend
- ✅ Skeleton CPU, MMU, PPU, Timer classes
- ✅ File upload interface for ROM loading
- ✅ JavaScript interop for canvas rendering
- ✅ Real-time frame stepping with FPS counter
- ❌ Complete CPU instruction set (placeholder implementation)
- ❌ Full MMU register handling (causes test failures)  
- ❌ PPU pixel rendering (stub implementation)
- ❌ Cartridge bank switching beyond MBC0
- ❌ Audio Processing Unit (APU)

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
- **Test failures**: 14 failures are expected due to incomplete MMU implementation
- **Build errors after changes**: Check that all using statements are correct and project references are intact
- **Blazor app won't start**: Verify GameBoy.Core builds successfully first
- **Performance issues**: Use Release configuration for production testing

### Development workflow
1. Make changes to GameBoy.Core for emulator logic
2. Update GameBoy.Blazor for UI changes  
3. Add tests to GameBoy.Tests for new functionality
4. Run `dotnet format` to fix any style issues
5. Build and test: `dotnet build && dotnet test`
6. Manual validation: Start Blazor app and test functionality
7. Commit changes with descriptive messages

### Important implementation notes
- Follow the 16-phase development plan outlined in README.md
- Emulator uses cycle-accurate timing (70,224 cycles per frame)
- JavaScript interop required for canvas rendering and audio (future)
- ROMs are loaded via browser file upload (no built-in test ROMs)
- Game Boy register values and timing must match original hardware specifications