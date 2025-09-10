# Phase 12: Debug Tooling Implementation - Comprehensive Task Breakdown

## Issue Summary

Implement a comprehensive debug tooling system for the BlazorBoy Game Boy emulator, enabling step-by-step debugging, memory inspection, disassembly viewing, and performance analysis. This phase transforms the emulator from a game player into a powerful development tool for Game Boy homebrew developers and emulator enthusiasts.

## Background & Current State

**Existing Infrastructure:**
- ✅ Complete CPU instruction set with 813 passing tests
- ✅ Full MMU, PPU, APU, Timer, and Interrupt systems implemented
- ✅ Blazor WebAssembly frontend with basic emulator controls
- ✅ JavaScript interop for canvas rendering and audio
- ✅ Basic debug info display in current UI

**Missing Debug Infrastructure:**
- ❌ Step-by-step execution capabilities
- ❌ Breakpoint system for debugging Game Boy ROMs
- ❌ Memory viewer/editor for RAM inspection
- ❌ CPU register panel with real-time updates
- ❌ Disassembly view with instruction highlighting
- ❌ PPU state visualization for graphics debugging
- ❌ Execution trace logging system
- ❌ Performance profiling tools

## 🎯 Objectives

- Create a comprehensive debugging interface accessible via separate debug page
- Implement core debug infrastructure (breakpoints, disassembler, trace logger)
- Provide step-by-step execution control (step into/over/out)
- Enable real-time inspection of CPU, memory, and hardware state
- Support advanced debugging features for homebrew development
- Maintain emulator performance while debugging is inactive
- Create educational tool for understanding Game Boy internals

## 📋 Epic Tasks

### Task 1: Core Debug Infrastructure
**Priority**: High  
**Estimated Effort**: 8-10 hours

#### Implementation Requirements
- [ ] Create `BreakpointManager.cs` for managing execution breakpoints
- [ ] Create `Disassembler.cs` for converting opcodes to assembly mnemonics
- [ ] Create `TraceLogger.cs` with ring buffer for execution history
- [ ] Create `DebugState.cs` for capturing complete emulator state
- [ ] Extend `Emulator.cs` with debug methods and step execution
- [ ] Add debug mode toggle to control debugging overhead

#### Technical Details
```csharp
public class BreakpointManager
{
    public Dictionary<ushort, Breakpoint> AddressBreakpoints { get; private set; }
    public List<ConditionalBreakpoint> ConditionalBreakpoints { get; private set; }
    
    public bool ShouldBreak(ushort pc, CpuState cpu, MmuState mmu)
    public void SetBreakpoint(ushort address, BreakpointType type = BreakpointType.Execute)
    public void RemoveBreakpoint(ushort address)
    public void SetConditionalBreakpoint(string condition, string description)
}

public class Disassembler  
{
    public DisassemblyLine Disassemble(byte[] data, ushort startAddress)
    public string DisassembleInstruction(byte opcode, byte? param1 = null, byte? param2 = null)
    public List<DisassemblyLine> DisassembleRange(ushort startAddr, ushort endAddr, Mmu mmu)
}

public class TraceLogger
{
    private readonly CircularBuffer<TraceEntry> _traceBuffer;
    
    public void LogInstruction(ushort pc, byte opcode, CpuState cpuState)
    public List<TraceEntry> GetRecentTrace(int count = 100)
    public void SaveTraceToFile(string filename)
}
```

#### Core Debug State Classes
```csharp
public class DebugState
{
    public DateTime Timestamp { get; set; }
    public CpuDebugState Cpu { get; set; }
    public MmuDebugState Memory { get; set; }
    public PpuDebugState Ppu { get; set; }
    public ApuDebugState Apu { get; set; }
    public TimerDebugState Timer { get; set; }
    public long TotalCycles { get; set; }
    public int FrameCount { get; set; }
}

public class CpuDebugState
{
    public ushort PC { get; set; }
    public ushort SP { get; set; }
    public byte A, B, C, D, E, F, H, L { get; set; }
    public bool IME { get; set; }
    public bool Halted { get; set; }
    public string CurrentInstruction { get; set; }
    public int InstructionCycles { get; set; }
}
```

#### Acceptance Criteria
- [ ] Breakpoints can be set/removed on any memory address
- [ ] Conditional breakpoints support CPU register and memory conditions
- [ ] Disassembler produces accurate Game Boy assembly mnemonics
- [ ] Trace logger captures last 10,000 instructions without performance impact
- [ ] Debug state capture includes all relevant emulator state
- [ ] Step execution works for single instruction and frame stepping

---

### Task 2: Debug UI Infrastructure and Layout
**Priority**: High  
**Estimated Effort**: 6-8 hours

#### Implementation Requirements
- [ ] Create `Pages/Debug.razor` as dedicated debug interface
- [ ] Create responsive layout with collapsible panels
- [ ] Add navigation between normal and debug modes
- [ ] Implement resizable panel system for customizable layout
- [ ] Add debug toolbar with execution controls
- [ ] Create shared debug services for component communication

#### Debug Page Layout
```
+------------------+------------------+------------------+
| CPU Registers    | Memory Viewer    | Disassembly      |
| - PC, SP, AF     | - Hex editor     | - Current PC     |
| - BC, DE, HL     | - ASCII view     | - Breakpoints    |
| - Flags          | - Memory map     | - Jump targets   |
| - IME, HALT      | - Watch list     | - Annotations    |
+------------------+------------------+------------------+
| PPU State        | APU Channels     | Execution Trace  |
| - LCDC, LY, SCX  | - Square 1 & 2   | - Instruction    |
| - Palette data   | - Wave & Noise   | - Registers      |
| - VRAM viewer    | - Mixing levels  | - Memory access  |
| - Sprite list    | - Visualizer     | - Jump history   |
+------------------+------------------+------------------+
| Debug Controls   | Performance      | Game Boy Screen  |
| - Play/Pause     | - FPS counter    | - Reduced size   |
| - Step Into/Over | - Cycle counter  | - Always visible |
| - Reset/Restart  | - Hot spots      | - Quick access   |
| - Save/Load      | - Memory usage   | - Screenshot     |
+------------------+------------------+------------------+
```

#### Blazor Components Structure
```csharp
// Debug.razor - Main debug page
@page "/debug"
@using GameBoy.Blazor.Components.Debug

<div class="debug-layout">
    <DebugToolbar @ref="_toolbar" />
    <div class="debug-panels">
        <CpuRegisterPanel />
        <MemoryViewerPanel />
        <DisassemblyPanel />
        <PpuStatePanel />
        <ApuChannelPanel />
        <TraceLogPanel />
    </div>
    <div class="debug-screen">
        <GameBoyCanvas Size="320x288" />
    </div>
</div>
```

#### Acceptance Criteria
- [ ] Debug page loads without impacting normal emulator functionality
- [ ] Responsive layout works on desktop and tablet devices
- [ ] Panel resizing and docking system is intuitive
- [ ] Real-time updates work smoothly at 60 FPS when debugging
- [ ] Navigation between normal and debug modes is seamless
- [ ] Debug state persists across page navigation

---

### Task 3: CPU Register Panel and Execution Controls
**Priority**: High  
**Estimated Effort**: 4-6 hours

#### Implementation Requirements
- [ ] Create `CpuRegisterPanel.razor` with real-time register display
- [ ] Add execution control buttons (play, pause, step, reset)
- [ ] Implement step modes: instruction, scanline, frame
- [ ] Add register value editing capabilities
- [ ] Create flag register breakdown with individual bit display
- [ ] Add execution speed control (1x, 0.5x, 0.1x speeds)

#### CPU Register Display
```csharp
<div class="cpu-panel">
    <h5>CPU Registers</h5>
    <div class="register-grid">
        <div class="register-pair">
            <label>PC:</label>
            <input @bind="_debugState.Cpu.PC" @bind:format="X4" />
        </div>
        <div class="register-pair">  
            <label>SP:</label>
            <input @bind="_debugState.Cpu.SP" @bind:format="X4" />
        </div>
        <div class="register-pair">
            <label>AF:</label>
            <input @bind="_debugState.Cpu.A" @bind:format="X2" />
            <input @bind="_debugState.Cpu.F" @bind:format="X2" />
        </div>
    </div>
    
    <div class="flags-display">
        <h6>Flags (F Register)</h6>
        <label><input type="checkbox" @bind="_flags.Zero" /> Z (Zero)</label>
        <label><input type="checkbox" @bind="_flags.Subtract" /> N (Subtract)</label>
        <label><input type="checkbox" @bind="_flags.HalfCarry" /> H (Half Carry)</label>
        <label><input type="checkbox" @bind="_flags.Carry" /> C (Carry)</label>
    </div>
    
    <div class="cpu-state">
        <p>IME: @(_debugState.Cpu.IME ? "Enabled" : "Disabled")</p>
        <p>HALT: @(_debugState.Cpu.Halted ? "Yes" : "No")</p>
        <p>Next: @_debugState.Cpu.CurrentInstruction</p>
    </div>
</div>
```

#### Execution Control Interface
```csharp
<div class="execution-controls">
    <button @onclick="PlayPause" class="btn @(_isRunning ? "btn-warning" : "btn-success")">
        @(_isRunning ? "⏸ Pause" : "▶ Play")
    </button>
    <button @onclick="StepInstruction" class="btn btn-info" disabled="@_isRunning">
        ↘ Step Into
    </button>
    <button @onclick="StepOver" class="btn btn-info" disabled="@_isRunning">
        ↗ Step Over  
    </button>
    <button @onclick="StepFrame" class="btn btn-secondary" disabled="@_isRunning">
        ⏭ Step Frame
    </button>
    <button @onclick="Reset" class="btn btn-danger">
        🔄 Reset
    </button>
    
    <div class="speed-control">
        <label>Speed:</label>
        <select @bind="_executionSpeed">
            <option value="1.0">1x</option>
            <option value="0.5">0.5x</option>
            <option value="0.1">0.1x</option>
            <option value="0.01">0.01x</option>
        </select>
    </div>
</div>
```

#### Acceptance Criteria
- [ ] All CPU registers update in real-time during execution
- [ ] Register values can be manually edited and applied to emulator
- [ ] Flag register shows individual bit states clearly
- [ ] Step execution works reliably for instruction and frame stepping
- [ ] Speed control allows slow-motion debugging
- [ ] Play/pause state synchronizes across all debug panels

---

### Task 4: Memory Viewer and Editor
**Priority**: High  
**Estimated Effort**: 6-8 hours

#### Implementation Requirements
- [ ] Create `MemoryViewerPanel.razor` with hex editor functionality
- [ ] Implement memory region navigation (ROM, RAM, VRAM, OAM, I/O)
- [ ] Add memory search and find functionality
- [ ] Create memory watch system for tracking specific addresses
- [ ] Add ASCII side panel for text data inspection
- [ ] Implement memory export/import capabilities

#### Memory Viewer Interface
```csharp
<div class="memory-viewer">
    <div class="memory-toolbar">
        <select @bind="_currentRegion">
            <option value="ROM">ROM Bank 0-1 (0x0000-0x7FFF)</option>
            <option value="VRAM">Video RAM (0x8000-0x9FFF)</option>
            <option value="ERAM">External RAM (0xA000-0xBFFF)</option>
            <option value="WRAM">Work RAM (0xC000-0xDFFF)</option>
            <option value="OAM">OAM (0xFE00-0xFE9F)</option>
            <option value="IO">I/O Registers (0xFF00-0xFF7F)</option>
            <option value="HRAM">High RAM (0xFF80-0xFFFE)</option>
        </select>
        
        <input @bind="_gotoAddress" placeholder="0x0000" />
        <button @onclick="GotoAddress" class="btn btn-sm btn-primary">Go</button>
        
        <input @bind="_searchPattern" placeholder="Search hex..." />
        <button @onclick="SearchMemory" class="btn btn-sm btn-info">Find</button>
    </div>
    
    <div class="memory-display">
        <div class="hex-editor">
            @for (int row = 0; row < _displayRows; row++)
            {
                <div class="hex-row">
                    <span class="address">@((_baseAddress + row * 16).ToString("X4")):</span>
                    @for (int col = 0; col < 16; col++)
                    {
                        var addr = _baseAddress + row * 16 + col;
                        var value = _memoryCache[addr];
                        <input @bind="value" @bind:format="X2" class="hex-byte @GetByteClass(addr)" 
                               @onchange="(e) => OnByteChanged(addr, e)" />
                    }
                    <span class="ascii-view">
                        @for (int col = 0; col < 16; col++)
                        {
                            var addr = _baseAddress + row * 16 + col;
                            var ascii = GetAsciiChar(_memoryCache[addr]);
                            <span class="ascii-char">@ascii</span>
                        }
                    </span>
                </div>
            }
        </div>
    </div>
    
    <div class="memory-watches">
        <h6>Memory Watches</h6>
        @foreach (var watch in _memoryWatches)
        {
            <div class="watch-item">
                <span>@watch.Name (0x@watch.Address.ToString("X4")):</span>
                <span class="watch-value">0x@watch.CurrentValue.ToString("X2")</span>
                <button @onclick="() => RemoveWatch(watch)" class="btn btn-sm btn-outline-danger">×</button>
            </div>
        }
        <div class="add-watch">
            <input @bind="_newWatchAddress" placeholder="0x0000" />
            <input @bind="_newWatchName" placeholder="Name" />
            <button @onclick="AddWatch" class="btn btn-sm btn-success">Add Watch</button>
        </div>
    </div>
</div>
```

#### Memory Watch System
```csharp
public class MemoryWatch
{
    public string Name { get; set; }
    public ushort Address { get; set; }
    public byte CurrentValue { get; set; }
    public byte PreviousValue { get; set; }
    public bool HasChanged => CurrentValue != PreviousValue;
    public WatchType Type { get; set; } // Byte, Word, Signed, etc.
}

public class MemoryDiff
{
    public ushort Address { get; set; }
    public byte OldValue { get; set; }
    public byte NewValue { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### Acceptance Criteria
- [ ] Memory viewer displays hex data with proper formatting
- [ ] ASCII panel shows readable characters and dots for non-printable
- [ ] Memory editing works and updates emulator state immediately
- [ ] Navigation between memory regions is intuitive
- [ ] Memory watches highlight changed values in real-time
- [ ] Search functionality finds hex patterns and ASCII strings
- [ ] Performance remains smooth with large memory regions

---

### Task 5: Disassembly Viewer with Breakpoints
**Priority**: High  
**Estimated Effort**: 8-10 hours

#### Implementation Requirements
- [ ] Create `DisassemblyPanel.razor` with scrollable instruction list
- [ ] Implement syntax highlighting for different instruction types
- [ ] Add breakpoint setting/removing by clicking line numbers
- [ ] Create jump target highlighting and navigation
- [ ] Add instruction search and symbol support
- [ ] Implement program counter tracking and auto-scrolling

#### Disassembly Interface
```csharp
<div class="disassembly-panel">
    <div class="disassembly-toolbar">
        <button @onclick="GotoPC" class="btn btn-sm btn-primary">📍 Go to PC</button>
        <input @bind="_gotoAddress" placeholder="0x0000" />
        <button @onclick="GotoAddress" class="btn btn-sm btn-secondary">Go</button>
        <input @bind="_searchInstruction" placeholder="Search instruction..." />
        <button @onclick="SearchInstruction" class="btn btn-sm btn-info">Find</button>
        
        <label><input type="checkbox" @bind="_autoFollow" /> Auto-follow PC</label>
        <label><input type="checkbox" @bind="_showAddresses" /> Show addresses</label>
        <label><input type="checkbox" @bind="_showBytes" /> Show bytes</label>
    </div>
    
    <div class="disassembly-view" @ref="_disassemblyContainer">
        @for (int i = 0; i < _visibleLines.Count; i++)
        {
            var line = _visibleLines[i];
            var isCurrentPC = line.Address == _currentPC;
            var hasBreakpoint = _breakpoints.ContainsKey(line.Address);
            
            <div class="disasm-line @GetLineClass(line, isCurrentPC, hasBreakpoint)">
                <span class="line-number" @onclick="() => ToggleBreakpoint(line.Address)">
                    @if (hasBreakpoint)
                    {
                        <span class="breakpoint-marker">🔴</span>
                    }
                    else
                    {
                        <span class="breakpoint-space">○</span>
                    }
                </span>
                
                @if (isCurrentPC)
                {
                    <span class="pc-marker">➤</span>
                }
                else
                {
                    <span class="pc-space">&nbsp;&nbsp;</span>
                }
                
                @if (_showAddresses)
                {
                    <span class="address">@line.Address.ToString("X4"):</span>
                }
                
                @if (_showBytes)
                {
                    <span class="instruction-bytes">@line.BytesHex</span>
                }
                
                <span class="mnemonic @GetMnemonicClass(line.Mnemonic)">@line.Mnemonic</span>
                <span class="operands">@line.Operands</span>
                
                @if (!string.IsNullOrEmpty(line.Comment))
                {
                    <span class="comment">; @line.Comment</span>
                }
            </div>
        }
    </div>
    
    <div class="disassembly-status">
        <span>PC: 0x@(_currentPC.ToString("X4"))</span>
        <span>Bank: @_currentRomBank</span>
        <span>Breakpoints: @_breakpoints.Count</span>
    </div>
</div>
```

#### Disassembly Data Structures
```csharp
public class DisassemblyLine
{
    public ushort Address { get; set; }
    public byte[] InstructionBytes { get; set; }
    public string BytesHex => string.Join(" ", InstructionBytes.Select(b => b.ToString("X2")));
    public string Mnemonic { get; set; }
    public string Operands { get; set; }
    public string Comment { get; set; }
    public InstructionType Type { get; set; } // Jump, Call, Load, Arithmetic, etc.
    public ushort? JumpTarget { get; set; }
    public bool IsJumpTarget { get; set; }
}

public enum InstructionType
{
    Load, Arithmetic, Logic, Rotate, Jump, Call, Return, 
    Stack, Control, Prefix, Invalid
}

public class Breakpoint
{
    public ushort Address { get; set; }
    public BreakpointType Type { get; set; }
    public bool Enabled { get; set; }
    public string Condition { get; set; }
    public string Description { get; set; }
    public int HitCount { get; set; }
}
```

#### Acceptance Criteria
- [ ] Disassembly shows accurate Game Boy assembly code
- [ ] Syntax highlighting helps distinguish instruction types
- [ ] Breakpoints can be set/removed by clicking line numbers
- [ ] Program counter auto-scrolls and highlights current instruction
- [ ] Jump targets are clickable for easy navigation
- [ ] Search finds instructions by mnemonic or operand
- [ ] Performance is smooth when scrolling through large ROM sections

---

### Task 6: PPU State Visualization Panel
**Priority**: Medium  
**Estimated Effort**: 6-8 hours

#### Implementation Requirements
- [ ] Create `PpuStatePanel.razor` for graphics debugging
- [ ] Add LCDC register breakdown with visual indicators
- [ ] Create tilemap viewer for background and window layers
- [ ] Implement tile data viewer with palette preview
- [ ] Add sprite (OAM) inspector with positioning overlay
- [ ] Create scanline timing visualization

#### PPU Debug Interface
```csharp
<div class="ppu-panel">
    <div class="ppu-registers">
        <h6>LCD Control (LCDC - 0xFF40)</h6>
        <div class="lcdc-bits">
            <label><input type="checkbox" @bind="_lcdc.LcdEnable" disabled /> LCD Enable</label>
            <label><input type="checkbox" @bind="_lcdc.WindowTileMapSelect" disabled /> Window Tilemap</label>
            <label><input type="checkbox" @bind="_lcdc.WindowEnable" disabled /> Window Enable</label>
            <label><input type="checkbox" @bind="_lcdc.BgWindowTileDataSelect" disabled /> Tile Data Select</label>
            <label><input type="checkbox" @bind="_lcdc.BgTileMapSelect" disabled /> BG Tilemap</label>
            <label><input type="checkbox" @bind="_lcdc.ObjSize" disabled /> Sprite Size</label>
            <label><input type="checkbox" @bind="_lcdc.ObjEnable" disabled /> Sprite Enable</label>
            <label><input type="checkbox" @bind="_lcdc.BgWindowEnable" disabled /> BG/Window Enable</label>
        </div>
        
        <div class="ppu-status">
            <p>LY (Scanline): @_ppuState.CurrentScanline</p>
            <p>LYC (Compare): @_ppuState.ScanlineCompare</p>
            <p>Mode: @_ppuState.CurrentMode (@GetModeDescription(_ppuState.CurrentMode))</p>
            <p>SCX: @_ppuState.ScrollX, SCY: @_ppuState.ScrollY</p>
            <p>WX: @_ppuState.WindowX, WY: @_ppuState.WindowY</p>
        </div>
    </div>
    
    <div class="tilemap-viewer">
        <h6>Background Tilemap</h6>
        <div class="tilemap-controls">
            <label>
                <input type="radio" name="tilemap" @bind="_selectedTilemap" value="bg" /> 
                Background (0x9800-0x9BFF)
            </label>
            <label>
                <input type="radio" name="tilemap" @bind="_selectedTilemap" value="window" /> 
                Window (0x9C00-0x9FFF)
            </label>
        </div>
        <canvas @ref="_tilemapCanvas" width="256" height="256" class="tilemap-canvas"></canvas>
    </div>
    
    <div class="tile-data-viewer">
        <h6>Tile Data</h6>
        <div class="tile-grid">
            @for (int tileId = 0; tileId < 384; tileId++)
            {
                <div class="tile-preview @GetTileClass(tileId)" @onclick="() => SelectTile(tileId)">
                    <canvas width="8" height="8" @ref="_tileCanvases[tileId]"></canvas>
                    <span class="tile-id">@tileId.ToString("X2")</span>
                </div>
            }
        </div>
    </div>
    
    <div class="sprite-inspector">
        <h6>Sprites (OAM)</h6>
        <div class="sprite-list">
            @for (int spriteIndex = 0; spriteIndex < 40; spriteIndex++)
            {
                var sprite = _sprites[spriteIndex];
                <div class="sprite-entry @(sprite.IsVisible ? "visible" : "hidden")">
                    <span class="sprite-id">#@spriteIndex.ToString("D2")</span>
                    <span class="sprite-pos">(@sprite.X, @sprite.Y)</span>
                    <span class="sprite-tile">Tile: @sprite.TileId.ToString("X2")</span>
                    <span class="sprite-flags">
                        @(sprite.FlipX ? "H" : "-")@(sprite.FlipY ? "V" : "-")@(sprite.Priority ? "P" : "-")
                    </span>
                </div>
            }
        </div>
    </div>
</div>
```

#### PPU State Data Structures
```csharp
public class PpuDebugState
{
    public byte CurrentScanline { get; set; }
    public byte ScanlineCompare { get; set; }
    public PpuMode CurrentMode { get; set; }
    public byte ScrollX { get; set; }
    public byte ScrollY { get; set; }
    public byte WindowX { get; set; }
    public byte WindowY { get; set; }
    public LcdcFlags Lcdc { get; set; }
    public StatFlags Stat { get; set; }
    public PaletteData BackgroundPalette { get; set; }
    public PaletteData SpritePalette0 { get; set; }
    public PaletteData SpritePalette1 { get; set; }
}

public class SpriteDebugInfo
{
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte TileId { get; set; }
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public bool Priority { get; set; }
    public byte Palette { get; set; }
    public bool IsVisible => Y > 0 && Y < 160 && X > 0 && X < 168;
}
```

#### Acceptance Criteria
- [ ] LCDC register shows all control bits with descriptions
- [ ] Tilemap viewer displays background and window tilemaps accurately
- [ ] Tile data viewer shows all 384 possible tiles with correct rendering
- [ ] Sprite inspector lists all 40 sprites with position and attributes
- [ ] Palette preview shows current 4-color palettes
- [ ] PPU mode and scanline information updates in real-time
- [ ] Visual overlays help understand sprite positioning and priorities

---

### Task 7: APU Channel Visualization Panel
**Priority**: Low  
**Estimated Effort**: 4-6 hours

#### Implementation Requirements
- [ ] Create `ApuChannelPanel.razor` for audio debugging
- [ ] Add visual waveform displays for each audio channel
- [ ] Create register breakdown for all audio registers (NR10-NR52)
- [ ] Implement volume level meters for each channel
- [ ] Add frequency analysis and spectrum display
- [ ] Create channel mute/solo controls for isolation testing

#### APU Debug Interface
```csharp
<div class="apu-panel">
    <div class="apu-master">
        <h6>Master Control (NR50-NR52)</h6>
        <div class="master-controls">
            <label><input type="checkbox" @bind="_apu.MasterEnable" disabled /> Master Enable (NR52)</label>
            <label>Left Volume: <input type="range" min="0" max="7" @bind="_apu.LeftVolume" disabled /></label>
            <label>Right Volume: <input type="range" min="0" max="7" @bind="_apu.RightVolume" disabled /></label>
        </div>
        <div class="channel-routing">
            <h6>Channel Routing (NR51)</h6>
            <div class="routing-grid">
                <span></span><span>Left</span><span>Right</span>
                <span>Square 1</span><input type="checkbox" @bind="_routing.Square1Left" disabled /><input type="checkbox" @bind="_routing.Square1Right" disabled />
                <span>Square 2</span><input type="checkbox" @bind="_routing.Square2Left" disabled /><input type="checkbox" @bind="_routing.Square2Right" disabled />
                <span>Wave</span><input type="checkbox" @bind="_routing.WaveLeft" disabled /><input type="checkbox" @bind="_routing.WaveRight" disabled />
                <span>Noise</span><input type="checkbox" @bind="_routing.NoiseLeft" disabled /><input type="checkbox" @bind="_routing.NoiseRight" disabled />
            </div>
        </div>
    </div>
    
    <div class="channel-panels">
        <div class="channel-panel">
            <h6>Square 1 (NR10-NR14)</h6>
            <div class="channel-visualizer">
                <canvas @ref="_square1Canvas" width="200" height="60" class="waveform-canvas"></canvas>
                <div class="level-meter">
                    <div class="level-bar" style="width: @(_square1Level * 100)%"></div>
                </div>
            </div>
            <div class="channel-controls">
                <button @onclick="() => ToggleMute(0)" class="btn btn-sm @(_channelMutes[0] ? "btn-danger" : "btn-outline-secondary")">
                    @(_channelMutes[0] ? "🔇 Muted" : "🔊 Active")
                </button>
                <button @onclick="() => ToggleSolo(0)" class="btn btn-sm @(_channelSolos[0] ? "btn-warning" : "btn-outline-secondary")">
                    @(_channelSolos[0] ? "🎯 Solo" : "Solo")
                </button>
            </div>
            <div class="channel-registers">
                <small>
                    Sweep: Period=@_square1.SweepPeriod Dir=@(_square1.SweepDecrease?"Dec":"Inc") Shift=@_square1.SweepShift<br/>
                    Duty: @_square1.DutyCycle Length: @_square1.LengthCounter<br/>
                    Envelope: Vol=@_square1.Volume Dir=@(_square1.EnvelopeIncrease?"Inc":"Dec") Period=@_square1.EnvelopePeriod<br/>
                    Frequency: @_square1.Frequency Hz
                </small>
            </div>
        </div>
        
        <!-- Similar panels for Square 2, Wave, and Noise channels -->
    </div>
    
    <div class="spectrum-analyzer">
        <h6>Frequency Spectrum</h6>
        <canvas @ref="_spectrumCanvas" width="400" height="200" class="spectrum-canvas"></canvas>
    </div>
</div>
```

#### Acceptance Criteria
- [ ] All four audio channels show live waveform visualization
- [ ] Register values update in real-time during audio playback
- [ ] Volume level meters respond to audio output levels
- [ ] Mute/solo controls work for debugging individual channels
- [ ] Frequency spectrum analyzer shows audio content distribution
- [ ] Channel routing matrix displays left/right pan settings

---

### Task 8: Execution Trace and Performance Panel
**Priority**: Medium  
**Estimated Effort**: 5-7 hours

#### Implementation Requirements
- [ ] Create `TraceLogPanel.razor` for execution history
- [ ] Add performance profiling with cycle counting
- [ ] Implement instruction frequency analysis
- [ ] Create call stack visualization
- [ ] Add memory access pattern tracking
- [ ] Implement hot spot detection for performance analysis

#### Trace Log Interface
```csharp
<div class="trace-panel">
    <div class="trace-controls">
        <button @onclick="StartTrace" class="btn btn-sm btn-success" disabled="@_tracing">▶ Start Trace</button>
        <button @onclick="StopTrace" class="btn btn-sm btn-danger" disabled="@(!_tracing)">⏹ Stop Trace</button>
        <button @onclick="ClearTrace" class="btn btn-sm btn-warning">🗑 Clear</button>
        <button @onclick="ExportTrace" class="btn btn-sm btn-info">💾 Export</button>
        
        <label>Buffer Size: 
            <select @bind="_traceBufferSize">
                <option value="1000">1,000</option>
                <option value="10000">10,000</option>
                <option value="100000">100,000</option>
            </select>
        </label>
        
        <label><input type="checkbox" @bind="_traceMemoryAccess" /> Memory Access</label>
        <label><input type="checkbox" @bind="_traceJumps" /> Jumps Only</label>
    </div>
    
    <div class="trace-display">
        <div class="trace-header">
            <span class="col-cycle">Cycle</span>
            <span class="col-pc">PC</span>
            <span class="col-instruction">Instruction</span>
            <span class="col-registers">Registers</span>
            <span class="col-memory">Memory</span>
        </div>
        
        <div class="trace-entries" @ref="_traceContainer">
            @foreach (var entry in _visibleTraceEntries)
            {
                <div class="trace-entry @GetTraceEntryClass(entry)">
                    <span class="col-cycle">@entry.Cycle</span>
                    <span class="col-pc">@entry.PC.ToString("X4")</span>
                    <span class="col-instruction">@entry.Instruction</span>
                    <span class="col-registers">A:@entry.Registers.A.ToString("X2") F:@entry.Registers.F.ToString("X2")</span>
                    <span class="col-memory">@entry.MemoryAccess</span>
                </div>
            }
        </div>
    </div>
    
    <div class="performance-stats">
        <h6>Performance Analysis</h6>
        <div class="stats-grid">
            <div class="stat-item">
                <label>Total Instructions:</label>
                <span>@_performanceStats.TotalInstructions.ToString("N0")</span>
            </div>
            <div class="stat-item">
                <label>Instructions/Second:</label>
                <span>@_performanceStats.InstructionsPerSecond.ToString("N0")</span>
            </div>
            <div class="stat-item">
                <label>Most Used Instruction:</label>
                <span>@_performanceStats.MostUsedInstruction (@_performanceStats.MostUsedCount times)</span>
            </div>
            <div class="stat-item">
                <label>Jump Instructions:</label>
                <span>@_performanceStats.JumpCount (@(_performanceStats.JumpPercentage.ToString("F1"))%)</span>
            </div>
        </div>
        
        <div class="hotspot-analysis">
            <h6>Execution Hot Spots</h6>
            @foreach (var hotspot in _performanceStats.TopHotSpots.Take(5))
            {
                <div class="hotspot-entry">
                    <span class="hotspot-address">0x@hotspot.Address.ToString("X4"):</span>
                    <span class="hotspot-instruction">@hotspot.Instruction</span>
                    <span class="hotspot-count">(@hotspot.Count times)</span>
                    <span class="hotspot-percentage">@hotspot.Percentage.ToString("F1")%</span>
                </div>
            }
        </div>
    </div>
</div>
```

#### Trace Data Structures
```csharp
public class TraceEntry
{
    public long Cycle { get; set; }
    public ushort PC { get; set; }
    public string Instruction { get; set; }
    public CpuRegisters Registers { get; set; }
    public string MemoryAccess { get; set; }
    public TraceEntryType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PerformanceStats
{
    public long TotalInstructions { get; set; }
    public double InstructionsPerSecond { get; set; }
    public string MostUsedInstruction { get; set; }
    public long MostUsedCount { get; set; }
    public long JumpCount { get; set; }
    public double JumpPercentage { get; set; }
    public List<HotSpot> TopHotSpots { get; set; }
    public Dictionary<string, long> InstructionFrequency { get; set; }
}

public class HotSpot
{
    public ushort Address { get; set; }
    public string Instruction { get; set; }
    public long Count { get; set; }
    public double Percentage { get; set; }
}
```

#### Acceptance Criteria
- [ ] Trace log captures execution history without impacting performance
- [ ] Performance statistics show meaningful emulator metrics
- [ ] Hot spot analysis identifies most executed code regions
- [ ] Call stack visualization shows function call hierarchy
- [ ] Memory access patterns help identify bottlenecks
- [ ] Export functionality saves trace data for external analysis

---

### Task 9: Advanced Debug Features and Integration
**Priority**: Low  
**Estimated Effort**: 6-8 hours

#### Implementation Requirements
- [ ] Implement conditional breakpoints with expression evaluation
- [ ] Add watch expressions for complex debugging scenarios
- [ ] Create debug macro system for automated testing
- [ ] Add screenshot and state capture for regression testing
- [ ] Implement debug script execution for automated scenarios
- [ ] Create debug plugin architecture for extensibility

#### Advanced Breakpoint System
```csharp
public class ConditionalBreakpoint
{
    public string Name { get; set; }
    public string Expression { get; set; } // "A == 0x42 && HL > 0x8000"
    public bool Enabled { get; set; }
    public int HitCount { get; set; }
    public int BreakAfterHits { get; set; } // Break only after N hits
    public string Action { get; set; } // "Log", "Break", "Script"
    
    public bool Evaluate(DebugContext context)
    {
        // Parse and evaluate expression against current state
        return _evaluator.Evaluate(Expression, context);
    }
}

public class DebugContext
{
    public CpuState Cpu { get; set; }
    public MmuState Memory { get; set; }
    public PpuState Ppu { get; set; }
    public long Cycle { get; set; }
    public int Frame { get; set; }
    
    // Convenience properties for expressions
    public byte A => Cpu.A;
    public byte F => Cpu.F;
    public ushort HL => (ushort)((Cpu.H << 8) | Cpu.L);
    public byte this[ushort address] => Memory.ReadByte(address);
}
```

#### Debug Script System
```csharp
public class DebugScript
{
    public string Name { get; set; }
    public string Content { get; set; }
    public ScriptLanguage Language { get; set; } // JavaScript, Lua, or built-in
    
    public async Task<ScriptResult> ExecuteAsync(DebugContext context)
    {
        // Execute script with access to emulator state
    }
}

// Example debug script:
// "Run until A register equals 0x42 or 1000 instructions executed"
```

#### Debug Session Management
```csharp
public class DebugSession
{
    public string Name { get; set; }
    public DateTime Created { get; set; }
    public List<Breakpoint> Breakpoints { get; set; }
    public List<MemoryWatch> Watches { get; set; }
    public List<DebugScript> Scripts { get; set; }
    public Dictionary<string, object> Variables { get; set; }
    
    public void SaveSession(string filename)
    public static DebugSession LoadSession(string filename)
}
```

#### Acceptance Criteria
- [ ] Conditional breakpoints support complex boolean expressions
- [ ] Watch expressions can evaluate multi-part conditions
- [ ] Debug macros automate common debugging workflows
- [ ] Screenshots capture exact visual state for regression testing
- [ ] Debug scripts enable automated testing scenarios
- [ ] Plugin architecture allows custom debug extensions

---

### Task 10: Debug Testing and Documentation
**Priority**: Medium  
**Estimated Effort**: 4-6 hours

#### Implementation Requirements
- [ ] Create unit tests for all debug infrastructure components
- [ ] Add integration tests for debug UI functionality
- [ ] Create performance benchmarks for debug mode overhead
- [ ] Write comprehensive debug tooling documentation
- [ ] Create video tutorials for debug workflows
- [ ] Add keyboard shortcut reference

#### Test Coverage Areas
```csharp
[TestClass]
public class DebugInfrastructureTests
{
    [Test] void BreakpointManager_SetBreakpoint_WorksCorrectly()
    [Test] void Disassembler_DisassembleInstruction_ReturnsCorrectMnemonic()
    [Test] void TraceLogger_LogInstruction_CapturesState()
    [Test] void ConditionalBreakpoint_Evaluate_WorksCorrectly()
    [Test] void MemoryWatch_DetectChange_TriggersCorrectly()
    [Test] void DebugState_Serialize_RoundTripWorks()
}

[TestClass]
public class DebugUITests
{
    [Test] void CpuRegisterPanel_UpdateRegisters_DisplaysCorrectly()
    [Test] void MemoryViewer_EditByte_UpdatesEmulator()
    [Test] void DisassemblyPanel_SetBreakpoint_WorksCorrectly()
    [Test] void DebugToolbar_StepExecution_WorksCorrectly()
}

[TestClass]
public class DebugPerformanceTests
{
    [Test] void DebugMode_PerformanceOverhead_LessThan10Percent()
    [Test] void TraceLogging_LargeBuffer_NoMemoryLeak()
    [Test] void BreakpointChecking_PerInstruction_FastEnough()
}
```

#### Documentation Requirements
- **User Guide**: Step-by-step debugging workflows
- **API Reference**: Debug infrastructure for advanced users
- **Keyboard Shortcuts**: Complete shortcut reference
- **Video Tutorials**: Common debugging scenarios
- **Troubleshooting**: Common issues and solutions

#### Acceptance Criteria
- [ ] All debug components have >90% unit test coverage
- [ ] Integration tests cover major debug workflows
- [ ] Performance overhead with debugging enabled is <10%
- [ ] Documentation covers all debug features comprehensively
- [ ] Video tutorials demonstrate practical debugging scenarios
- [ ] Keyboard shortcuts work consistently across all debug panels

---

## 🏗️ Implementation Architecture

### Component Hierarchy
```
Debug Infrastructure (Core)
├── BreakpointManager
├── Disassembler  
├── TraceLogger
├── DebugStateCapture
└── PerformanceProfiler

Debug UI (Blazor)
├── Pages/Debug.razor
├── Components/
│   ├── CpuRegisterPanel.razor
│   ├── MemoryViewerPanel.razor
│   ├── DisassemblyPanel.razor
│   ├── PpuStatePanel.razor
│   ├── ApuChannelPanel.razor
│   ├── TraceLogPanel.razor
│   └── DebugToolbar.razor
└── Services/
    ├── DebugService.cs
    └── DebugSessionManager.cs
```

### Integration Points
1. **Emulator.cs**: Add debug mode flag and step execution methods
2. **Cpu.cs**: Integrate breakpoint checking and trace logging
3. **Mmu.cs**: Add memory watch notifications
4. **Index.razor**: Add navigation to debug mode
5. **JavaScript**: Extend for debug UI interactions

### Performance Considerations
- Debug infrastructure adds <5% overhead when inactive
- Trace logging uses circular buffer to limit memory usage
- Breakpoint checking optimized for minimal instruction impact
- UI updates throttled to maintain 60 FPS during debugging
- Large memory operations use background workers

## 📋 Definition of Done

### Functional Requirements
- [ ] Complete step-by-step debugging with pause/step/continue controls
- [ ] Real-time viewing and editing of CPU registers and memory
- [ ] Breakpoint system with address and conditional breakpoints
- [ ] Disassembly view with syntax highlighting and navigation
- [ ] PPU state visualization for graphics debugging
- [ ] APU channel visualization for audio debugging
- [ ] Execution trace logging with performance analysis
- [ ] Memory watch system for tracking specific addresses

### Technical Requirements  
- [ ] Debug mode integrates cleanly with existing emulator architecture
- [ ] No performance regression when debugging is disabled
- [ ] All debug components are properly unit tested
- [ ] Memory usage remains reasonable during extended debugging
- [ ] Debug UI is responsive and intuitive

### Quality Requirements
- [ ] Code follows existing project style and patterns
- [ ] Comprehensive documentation for all debug features
- [ ] Debug tools are accessible to both novice and expert users
- [ ] Proper error handling for all debug operations
- [ ] Mobile-friendly responsive design for debug interface

## 🧪 Testing Strategy

### Unit Tests
- Breakpoint manager functionality and conditional evaluation
- Disassembler accuracy against known instruction set
- Trace logger performance and memory management
- Debug state serialization and round-trip testing

### Integration Tests
- End-to-end debugging workflows with test ROMs
- Debug UI interaction with emulator state
- Performance benchmarking with debugging enabled
- Memory leak detection during extended debug sessions

### Manual Testing
- Debug workflow testing with actual Game Boy ROMs
- UI responsiveness during intensive debugging operations
- Cross-browser compatibility testing
- Accessibility testing for keyboard navigation

## 📚 Reference Documentation

### Game Boy Development Resources
- [Game Boy Programming Manual](https://ia801906.us.archive.org/19/items/GameBoyProgammingManual/GameBoyProgammingManual.pdf)
- [Game Boy CPU Manual](http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf)
- [Game Boy Development Wiki](https://gbdev.gg8.se/wiki/)
- [Pan Docs - Game Boy Technical Reference](https://gbdev.io/pandocs/)

### Debugging and Disassembly Tools
- [RGBDS - Game Boy Development Suite](https://rgbds.gbdev.io/)
- [BGB Emulator - Reference debugger](https://bgb.bircd.org/)
- [Visual Studio Code Game Boy Extensions](https://marketplace.visualstudio.com/items?itemName=donaldhays.vscode-gameboy)

### Technical Implementation Guides
- [Writing a Game Boy Emulator](https://cturt.github.io/cinoop.html)
- [Game Boy Emulation Guide](https://realboyemulator.wordpress.com/2013/01/03/a-look-at-the-game-boy-bootstrap-let-the-fun-begin/)
- [Imran Nazar's Game Boy Emulation Guide](http://imrannazar.com/GameBoy-Emulation-in-JavaScript:-The-CPU)

## 🚀 Success Metrics

### Objective Measures
- [ ] All 813 existing tests continue to pass
- [ ] Debug infrastructure unit tests achieve >90% code coverage  
- [ ] Debug mode adds <10% performance overhead when active
- [ ] Memory usage increase <50MB for complete debug interface
- [ ] Debug UI responds within 100ms for all user interactions

### Subjective Measures
- [ ] Debug tools feel intuitive to Game Boy developers
- [ ] Debugging workflows are efficient and productive
- [ ] Debug interface is visually clear and informative
- [ ] Documentation enables users to quickly become productive
- [ ] Debug tools help users understand Game Boy hardware

---

**Note**: This implementation represents a significant enhancement that transforms BlazorBoy from a game player into a comprehensive Game Boy development tool. Priority should be given to core debugging infrastructure and CPU-focused tools before implementing visualization panels and advanced features.