# Phase 6: PPU Pipeline Implementation

## Overview
Phase 6 focuses on implementing the complete Picture Processing Unit (PPU) pipeline to replace the current placeholder implementation. This phase will enable proper Game Boy graphics rendering including background tiles, window layer, sprites, and accurate LCD mode timing.

## Current State Analysis
- ✅ Basic PPU class exists with placeholder test pattern rendering
- ✅ VBlank interrupt generation working
- ✅ Frame buffer structure in place (160x144 int array)
- ❌ No LCD mode state machine
- ❌ No background/window tile rendering
- ❌ No sprite (OAM) support
- ❌ No palette management
- ❌ Placeholder generates test pattern instead of real graphics

## Implementation Tasks

### Core PPU State Machine
- [ ] **Task 1.1**: Implement LCD mode state machine
  - [ ] Mode 0: H-Blank (204 cycles)
  - [ ] Mode 1: V-Blank (4560 cycles, 10 scanlines)
  - [ ] Mode 2: OAM Scan (80 cycles)
  - [ ] Mode 3: Drawing (172-289 cycles, variable)
  - [ ] LY register tracking (current scanline 0-153)
  - [ ] STAT interrupt conditions

- [ ] **Task 1.2**: Implement mode timing constants
  - [ ] Define cycle counts for each mode
  - [ ] Implement scanline progression
  - [ ] Handle VBlank timing correctly

### Background Rendering
- [ ] **Task 2.1**: Implement background tile fetching
  - [ ] Read from VRAM tile data
  - [ ] Support 8x8 pixel tiles
  - [ ] Handle tile map addressing (9800-9BFF, 9C00-9FFF)
  - [ ] Implement SCX/SCY scrolling

- [ ] **Task 2.2**: Background pixel pipeline
  - [ ] Tile data decoding (2bpp format)
  - [ ] Pixel color mapping via BGP palette
  - [ ] Scanline-based rendering

### Window Layer
- [ ] **Task 3.1**: Implement window rendering
  - [ ] Window enable check (LCDC bit 5)
  - [ ] Window position (WX, WY registers)
  - [ ] Window tile map support
  - [ ] Window-background priority handling

### Sprite (OAM) System
- [ ] **Task 4.1**: Create Sprite class
  - [ ] Define OAM entry structure (Y, X, tile, attributes)
  - [ ] Implement sprite attribute parsing
  - [ ] Handle 8x8 and 8x16 sprite modes

- [ ] **Task 4.2**: Implement OAM scanning
  - [ ] Scan 40 OAM entries during Mode 2
  - [ ] Apply 10 sprites per scanline limit
  - [ ] Priority ordering by X coordinate

- [ ] **Task 4.3**: Sprite rendering
  - [ ] Sprite pixel fetching
  - [ ] Handle sprite transparency (color 0)
  - [ ] Object-background priority
  - [ ] Sprite palette support (OBP0, OBP1)

### Palette Management
- [ ] **Task 5.1**: Create Palette class
  - [ ] Background palette (BGP) support
  - [ ] Object palettes (OBP0, OBP1) support
  - [ ] 2-bit to RGB color mapping
  - [ ] Default Game Boy color scheme

### LCDC Register Support
- [ ] **Task 6.1**: Create Lcdc class
  - [ ] Bit-level LCDC register decoding
  - [ ] LCD enable/disable handling
  - [ ] Background/window/sprite enable flags
  - [ ] Tile data/map selection bits

### Integration & Memory
- [ ] **Task 7.1**: VRAM integration
  - [ ] Proper VRAM addressing (8000-9FFF)
  - [ ] Tile data reading
  - [ ] Tile map reading
  - [ ] OAM reading (FE00-FE9F)

- [ ] **Task 7.2**: Register integration
  - [ ] LY register updates
  - [ ] STAT register management
  - [ ] LYC comparison
  - [ ] Palette register handling

### Performance Optimization
- [ ] **Task 8.1**: Rendering optimizations
  - [ ] Remove per-frame full-screen pattern generation
  - [ ] Implement scanline-based rendering
  - [ ] Cache tile data when possible
  - [ ] Optimize hot paths

## New Files Required

### Core Classes
- [ ] `src/GameBoy.Core/Lcdc.cs` - LCDC register bit decoding
- [ ] `src/GameBoy.Core/Sprite.cs` - OAM entry representation
- [ ] `src/GameBoy.Core/Palette.cs` - Palette management

### Optional Enhancement Classes
- [ ] `src/GameBoy.Core/PixelFifo.cs` - Pixel FIFO implementation (advanced)
- [ ] `src/GameBoy.Core/Fetcher.cs` - Background/sprite fetcher (advanced)

## Testing Strategy

### Unit Tests
- [ ] **Test 9.1**: PPU mode timing tests
  - [ ] Verify mode durations
  - [ ] Test LY progression
  - [ ] STAT interrupt timing

- [ ] **Test 9.2**: Background rendering tests
  - [ ] Tile data decoding
  - [ ] Palette application
  - [ ] Scrolling behavior

- [ ] **Test 9.3**: Sprite tests
  - [ ] OAM parsing
  - [ ] Sprite priority
  - [ ] 10 sprite limit

### Integration Tests
- [ ] **Test 9.4**: Test ROM validation
  - [ ] Mooneye lcd_sync test
  - [ ] Mooneye sprite_priority test
  - [ ] dmg-acid2 visual test (goal image)

### Visual Validation
- [ ] **Test 9.5**: Manual testing
  - [ ] Load simple games and verify rendering
  - [ ] Check background scrolling
  - [ ] Verify sprite animations
  - [ ] Test window functionality

## Dependencies
- ✅ MMU with VRAM/OAM/Register access
- ✅ InterruptController for VBlank & STAT interrupts
- ✅ Existing Emulator.StepFrame() integration

## Success Criteria
1. **Functional**: PPU renders actual game graphics instead of test pattern
2. **Accurate**: Passes Mooneye lcd_sync and sprite_priority tests
3. **Visual**: dmg-acid2 test ROM produces correct reference image
4. **Performance**: Maintains 60 FPS target on target hardware
5. **Compatible**: Simple Game Boy games display correctly

## Implementation Order
1. Start with core state machine and mode timing (Tasks 1.1-1.2)
2. Implement basic background rendering (Tasks 2.1-2.2)
3. Add palette support (Task 5.1)
4. Integrate LCDC register handling (Task 6.1)
5. Add sprite system (Tasks 4.1-4.3)
6. Implement window layer (Task 3.1)
7. Optimize and polish (Task 8.1)
8. Add comprehensive tests (Tasks 9.1-9.5)

## Estimated Effort
- **Core Implementation**: ~15-20 hours
- **Testing & Integration**: ~5-8 hours
- **Optimization & Polish**: ~3-5 hours
- **Total**: ~25-35 hours

## Related Documentation
- See `docs/` folder for detailed Game Boy PPU specifications
- Reference `README.md` Phase 6 section for technical requirements
- Check existing test files for PPU register test patterns