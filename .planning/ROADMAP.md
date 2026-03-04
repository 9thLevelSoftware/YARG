# ROADMAP: Elite Drums for YARG

## Phase Overview

| # | Phase | Est. Plans | Status |
|---|-------|-----------|--------|
| 1 | Core Engine Integration | 2 | Complete (reviewed) |
| 2 | Input & MIDI Binding | 2 | Complete |
| 3 | Visual Highway | 3 | Complete (reviewed) |
| 4 | End-to-End Integration & Polish | 2 | Complete |

**Total phases**: 4
**Total estimated plans**: 9

---

## Phase 1: Core Engine Integration

**Goal**: Make Elite Drums selectable and engine-runnable — charts load, engine processes inputs, scores are calculated. No visual highway yet, but the game loop works.

**Key files**:
- `YARG.Core/YARG.Core/Chart/SongChart.cs` — add EliteDrums to DrumsTracks, Append, GetDrumsTrack
- `Assets/Script/Gameplay/Player/DrumsPlayer.cs` — wire CreateEngine() for EliteDrums
- `YARG.Core/YARG.Core/Chart/Loaders/MoonSong/MoonSongLoader.EliteDrums.cs` — verify loader completeness
- `YARG.Core/YARG.Core/Engine/Drums/Engines/YargDrumsEngine.cs` — verify input conversion paths

**Success criteria**:
- [x] `SongChart.DrumsTracks` includes EliteDrums track (via downcharted tracks; EliteDrumNote type constraint documented)
- [x] `SongChart.GetDrumsTrack(Instrument.EliteDrums)` returns the correct track (returns FourLaneDrums downchart)
- [x] `SongChart.Append()` handles EliteDrums
- [x] `DrumsPlayer.CreateEngine()` creates engine for `GameMode.EliteDrums` without throwing
- [x] YARG.Core unit tests pass (existing + any new ones for Elite Drums paths)
- [x] Elite Drums instrument appears in song metadata when chart contains PART ELITE DRUMS

**Recommended agents**: Backend Architect, Senior Developer, API Tester
**Estimated plans**: 2

---

## Phase 2: Input & MIDI Binding

**Goal**: Real MIDI drum kit binding — map all 10 pads + kick + pedal to Elite Drums actions, with a proper binding dialog that shows the full kit layout.

**Key files**:
- `Assets/Script/Menu/Common/Dialogs/Onboarding/EliteDrumsBindingDialog.cs` — replace 4L/5L placeholder
- `Assets/Prefabs/Menu/Common/Dialogs/FriendlyEliteDrumsBindingDialog.prefab` — update prefab
- `Assets/Script/Menu/Persistent/DialogManager.cs` — verify dialog routing
- `Assets/Script/Helpers/Extensions/GameModeExtensions.cs` — default bindings setup
- `YARG.Core/YARG.Core/Input/InputActions.cs` — verify EliteDrumsAction completeness
- `Assets/Script/Input/` — MIDI device binding for Elite Drums

**Success criteria**:
- [ ] Binding dialog shows all 10 pads + kick + hi-hat pedal with visual layout
- [ ] Each pad can be bound to a MIDI note from a real drum kit
- [ ] Bindings persist across sessions (saved to profile)
- [ ] Default MIDI mappings match General MIDI drum note numbers
- [ ] Dialog accessible from player profile setup when EliteDrums game mode selected
- [ ] Bound inputs correctly translate to EliteDrumsAction values

**Recommended agents**: Senior Developer, Frontend Developer, Evidence Collector
**Estimated plans**: 2

---

## Phase 3: Visual Highway

**Goal**: Dedicated 10-lane visual highway — notes scroll down per-pad lanes, frets light up on hit, dynamics and hi-hat states are visually distinct. This is the largest phase.

**Key files (new)**:
- `Assets/Script/Gameplay/Visuals/TrackElements/EliteDrums/EliteDrumsNoteElement.cs` — note rendering
- `Assets/Script/Gameplay/Visuals/Fret/EliteDrumsFretArray.cs` — 10-lane fret display
- `Assets/Script/Gameplay/Player/EliteDrumsPlayer.cs` — or extend DrumsPlayer for Elite visual track

**Key files (modify)**:
- `Assets/Script/Gameplay/Visuals/TrackPlayer.cs` — may need Elite Drums track variant
- `Assets/Script/Gameplay/HUD/TrackView.cs` / `TrackViewManager.cs` — track view allocation
- `Assets/Script/Helpers/VisualStyleHelpers.cs` — visual style for EliteDrums
- `Assets/Script/Gameplay/Visuals/CameraPositioner.cs` — wider track camera position

**Design considerations**:
- 10 lanes is significantly wider than 4/5-lane — camera FOV and track width must accommodate
- Lane ordering should match physical kit layout (L-crash, HH, snare, toms L→R, ride, R-crash)
- Hi-hat pedal lane needs special treatment (pedal indicator, not a note lane)
- Accent notes: larger/brighter; ghost notes: smaller/dimmer
- Color coding per pad type (cymbals vs toms vs snare vs kick)
- Flam notes need a visual indicator (double-strike marker)

**Success criteria**:
- [ ] 10-lane highway renders with correct lane ordering
- [ ] Notes scroll at correct speed matching song tempo
- [ ] Each pad type has distinct color/shape
- [ ] Accent notes visually larger, ghost notes visually smaller
- [ ] Hi-hat open/closed/sizzle states shown on hi-hat lane notes
- [ ] Flam notes have visible double-hit indicator
- [ ] Frets light up on hit with correct pad color
- [ ] Beatlines render correctly across all 10 lanes
- [ ] Track width and camera accommodate the wider layout
- [ ] No z-fighting or visual artifacts at standard play distances

**Recommended agents**: Senior Developer, Frontend Developer, UI Designer, Evidence Collector
**Estimated plans**: 3

---

## Phase 4: End-to-End Integration & Polish

**Goal**: Complete the flow from song selection through score screen. Everything works together — select an Elite Drums chart, bind your kit, play through a song, see your score.

**Key files**:
- `Assets/Script/Menu/MusicLibrary/MusicLibraryMenu.cs` — show Elite Drums availability
- `Assets/Script/Gameplay/HUD/ScoreBox/` — score display for Elite Drums
- `Assets/Script/Gameplay/HUD/SoloBox.cs` — solo section handling
- `Assets/Script/Gameplay/HUD/StarpowerBar.cs` — star power for Elite Drums
- `Assets/Script/Gameplay/HUD/ComboMeter.cs` — combo display
- `Assets/Script/Gameplay/HUD/InputViewer/` — may need Elite Drums input viewer
- `Assets/StreamingAssets/lang/en-US.json` — verify all localization strings

**Success criteria**:
- [ ] Music library shows Elite Drums difficulty/availability per song
- [ ] Can select Elite Drums as game mode and pick a song with Elite Drums chart
- [ ] Full gameplay loop: song loads → notes render → inputs register → hits/misses scored
- [ ] Star power activation works
- [ ] Solo sections display correctly
- [ ] Score screen shows correct stats after song completion
- [ ] Combo meter and multiplier work correctly
- [ ] No crashes or exceptions through the entire flow
- [ ] Tested on real MIDI drum kit end-to-end

**Recommended agents**: Senior Developer, Evidence Collector, Reality Checker
**Estimated plans**: 2

---

## Dependency Chain

```
Phase 1 (Core Engine) ──> Phase 2 (Input & Binding)
         │                         │
         └────────> Phase 3 (Visual Highway)
                              │
Phase 2 ──────────────────────┘
                              │
                              v
                    Phase 4 (End-to-End Polish)
```

Phase 1 is prerequisite for everything. Phases 2 and 3 can partially overlap (input work doesn't block early visual prototyping). Phase 4 requires both 2 and 3 complete.
