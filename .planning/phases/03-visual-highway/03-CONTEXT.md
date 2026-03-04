# Phase 3 Context: Visual Highway

## Phase Goal

Build a dedicated 10-lane visual highway for Elite Drums — 8 note lanes (HiHat, LeftCrash, Snare, Tom1, Tom2, Tom3, Ride, RightCrash) + Kick (off-lane) + HatPedal (visual indicator). Notes render with dynamics (accent/ghost), hi-hat open/closed state, and flam indicators.

## Requirements

From ROADMAP.md Phase 3 success criteria:
- 10-lane highway renders with correct lane ordering
- Notes scroll at correct speed matching song tempo
- Each pad type has distinct color/shape
- Accent notes visually larger, ghost notes visually smaller
- Hi-hat open/closed states shown on hi-hat lane notes (sizzle deferred — no data model support)
- Flam notes have visible double-hit indicator
- Frets light up on hit with correct pad color
- Beatlines render correctly across all lanes
- Track width and camera accommodate the wider layout
- No z-fighting or visual artifacts

## Existing Assets (from Phases 1-2)

### Phase 1 Output
- `SongChart.EliteDrums` track loads and downcharts to 4L/5L/Pro correctly
- `DrumsPlayer.CreateEngine()` handles `GameMode.EliteDrums` (creates DrumsEngine with `isEliteDrums: true`)
- Engine processes downcharted DrumNote track for scoring
- Unit tests pass (53/57, 4 pre-existing)

### Phase 2 Output
- 13 Elite Drums bindings active in `BindingCollection.Templates.cs`
- `ConvertMidiDrumsInput()` maps all 13 Elite→DrumsAction (verified against downchart)
- `EliteDrumsBindingDialog.cs` rewritten for 13-pad binding flow
- Binding dialog prefab has art assets wired

## Architecture Decisions

### Core Architecture: Supplemental Lookup Pattern
```
EliteDrumsPlayer : TrackPlayer<DrumsEngine, DrumNote>
├── Engine: DrumsEngine + InstrumentTrack<DrumNote> (scoring)
├── Visual: InstrumentTrack<EliteDrumNote> reference + tick mapping
├── Notes: EliteDrumsNoteElement : NoteElement<DrumNote, EliteDrumsPlayer>
│   ├── NoteRef = DrumNote (hit/miss from engine)
│   └── ElitePadInfo = struct (pad, dynamics, hatState, isFlam)
└── HatPedal: Visual-only indicator (not scored)
```

**Why**: Engine operates on DrumNote (downcharted). NoteElement needs DrumNote for pool lifecycle and hit/miss state. EliteDrumNote data provides correct pad assignment, dynamics, and hat state that downchart loses.

### Correlation: DrumNote → EliteDrumNote
- Path A (preferred): Store origin reference during downchart (small YARG.Core change)
- Path B (fallback): Parallel iteration + matching at load time (no YARG.Core changes)
- Flam notes produce extra DrumNotes — visual system shows one flam indicator, hides partner

### Lane Layout (1-based note indices for GetElementX alignment)
| Pad | Note Lane (1-based) | Fret Position (0-based) |
|---|---|---|
| HiHat | 1 | 0 |
| LeftCrash | 2 | 1 |
| Snare | 3 | 2 |
| Tom1 | 4 | 3 |
| Tom2 | 5 | 4 |
| Tom3 | 6 | 5 |
| Ride | 7 | 6 |
| RightCrash | 8 | 7 |

### Input Interception
`EliteDrumsPlayer.InterceptInput()` captures `EliteDrumsAction` BEFORE engine converts to `DrumsAction`. This preserves pad identity for fret animations.

### Sizzle Hat State: DEFERRED
`EliteDrumsHatState` has no `Sizzle` value. Chart loader doesn't parse it. Deferred to post-MVP.

## Key Files Reference

### Existing patterns to follow
- `DrumsPlayer.cs` — parallel player class (do NOT extend, create new)
- `FourLaneDrumsNoteElement.cs` / `FiveLaneDrumsNoteElement.cs` — note element patterns
- `DrumsNoteElement.cs` — base note group logic (7 ThemeNoteTypes)
- `FretArray.cs` — fret initialization, positioning, animation
- `ColorProfile.FourLaneDrums.cs` — color provider pattern
- `ThemeComponent.cs` — VisualStyle switch cases (throws on unhandled!)

### Pre-existing bugs to fix
- `FretArray.PlayMissAnimation()`: `index <= _frets.Count` → `index < _frets.Count`

## Plan Structure

| Plan | Wave | Description |
|------|------|-------------|
| 03-01 | 1 | Visual Infrastructure Foundation (VisualStyle, ColorProfile, FretArray fix) |
| 03-02 | 2 | Core Player & Note Element (EliteDrumsPlayer, EliteDrumsNoteElement, HatPedal) |
| 03-03 | 3 | Prefab Integration & Wiring (prefab creation, GameManager, verification) |

## Spec Reference

Full specification at: `.planning/specs/03-visual-highway-spec.md`
