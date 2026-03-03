# Phase 1 Context: Core Engine Integration

## Goal
Make Elite Drums selectable and engine-runnable — charts load, engine processes inputs, scores are calculated. No visual highway yet, but the game loop works via the downchart path.

## Architecture Decision
**Downchart-first approach**: Elite Drums charts are loaded as `InstrumentTrack<EliteDrumNote>`, then downcharted to `InstrumentTrack<DrumNote>` (4L/5L/Pro) at chart load time. The `DrumsEngine` operates on `DrumNote` as usual, with `isMidiDrumsInput=true` enabling `EliteDrumsAction` → `DrumsAction` conversion. Native `EliteDrumNote` engine processing is deferred to Phase 3.

**Why**: `DrumsEngine : BaseEngine<DrumNote, ...>` is hardcoded to `DrumNote`. The downcharting infrastructure already exists and works (SongChart.cs:134-137). Building a native `EliteDrumsEngine` now would be premature — it's Phase 3 work alongside the visual highway.

## Key Type Relationships
```
EliteDrumNote : Note<EliteDrumNote>   (10-pad, dynamics, hat state)
DrumNote : Note<DrumNote>              (4/5-lane, simplified)

InstrumentTrack<EliteDrumNote> EliteDrums   → stored in SongChart
InstrumentTrack<DrumNote> FourLaneDrums     → downcharted from EliteDrums at load time
InstrumentTrack<DrumNote> ProDrums          → downcharted from EliteDrums at load time
InstrumentTrack<DrumNote> FiveLaneDrums     → downcharted from EliteDrums at load time

DrumsEngine : BaseEngine<DrumNote, ...>     → only understands DrumNote
YargDrumsEngine.IsMidiDrumsInput = true     → converts EliteDrumsAction → DrumsAction
```

## Requirements (from ROADMAP.md)
1. `SongChart.DrumsTracks` includes EliteDrums track → N/A for Phase 1 (different type)
2. `SongChart.GetDrumsTrack(Instrument.EliteDrums)` → N/A (type mismatch, use GetEliteDrumsTrack or direct property access)
3. `SongChart.Append()` handles EliteDrums → **Plan 1, Task 1**
4. `DrumsPlayer.CreateEngine()` works for EliteDrums game mode → **Plan 2, Task 2**
5. YARG.Core unit tests pass → **Plan 2, Task 3**
6. Elite Drums instrument appears in song metadata → **Plan 2, Task 1**

## Existing Assets
- `EliteDrumNote.cs` — fully implemented (10 pads, dynamics, hat states, flams)
- `MidiEliteDrumsPreparser.cs` — complete (returns elite + downchart difficulty masks)
- `MoonSongLoader.EliteDrums.cs` — chart loader working
- `MoonSongLoader.EliteDrumsDownchart.cs` — downchart generation working
- `EliteDrumsAction` enum — 12 actions defined
- `YargDrumsEngine.ConvertMidiDrumsInput()` — converts EliteDrumsAction to DrumsAction
- `GameManager.Loading.cs:413-415` — prefab selection already handles EliteDrums game mode

## Risk Areas
- **SongChart timing methods**: EliteDrums is `InstrumentTrack<EliteDrumNote>`, can't be added to `IEnumerable<InstrumentTrack<DrumNote>>`. Need separate handling.
- **Song scanning**: Must verify `MidiEliteDrumsPreparser` populates BOTH `AvailableParts.EliteDrums` AND downchart parts, or songs with only Elite Drums charts won't appear in the music library.
- **PossibleInstrumentsForSong**: `Instrument.EliteDrums` is explicitly commented out. Keeping it commented is intentional for Phase 1 (downchart path), but the instrument selection flow must still work.

## Plan Structure
- **Plan 1 (Wave 1)**: SongChart Integration Fixes — YARG.Core layer
- **Plan 2 (Wave 2)**: Engine Wiring & Song Availability — Unity + verification (depends on Plan 1)
