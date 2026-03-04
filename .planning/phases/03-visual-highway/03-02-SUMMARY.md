# Plan 03-02 Summary: Core Player & Note Element

**Status**: Complete
**Date**: 2026-03-03
**Agent**: EngineeringSeniorDeveloper

---

## Files Created

| File | Description |
|------|-------------|
| `Assets/Script/Gameplay/Player/EliteDrumsPlayer.cs` | Player class inheriting `TrackPlayer<DrumsEngine, DrumNote>` with 8-lane fret management, dual-track correlation, and input interception |
| `Assets/Script/Gameplay/Visuals/TrackElements/EliteDrums/EliteDrumsNoteElement.cs` | Note element with 7 NoteGroups, 8-lane positioning, per-pad colors, open hat indicator, and flam indicator |
| `Assets/Script/Gameplay/Visuals/TrackElements/EliteDrums/HatPedalIndicator.cs` | Hat pedal visual state machine (Closed/Open) driven by chart events |

---

## Architecture Decisions

### Correlation: Path B (tick-based lookup)

Used **Path B** — `Dictionary<uint, List<EliteDrumNote>>` keyed by tick, built during initialization from `chart.EliteDrums`. No changes to YARG.Core submodule were needed.

**Matching algorithm:**
1. For each spawned DrumNote, look up all EliteDrumNotes at the same tick
2. Match by comparing the EliteDrumNote's expected downchart pad to the DrumNote's actual pad
3. If no match found at a tick with flam notes, mark as flam partner (visually hidden)
4. Fallback: if no elite data exists, infer Elite pad from DrumNote pad

### Input Interception

`InterceptInput(ref GameInput)` captures `EliteDrumsAction` from game inputs before the engine converts them to `DrumsAction`. Actions are stored in a bounded queue. When `OnPadHit` fires, the queue is consumed to find the specific Elite lane for fret animation.

For replay playback (where `InterceptInput` is not called), the `UpdateInputs` override pre-populates the queue from replay inputs.

### ElitePadInfo Struct

```csharp
public struct ElitePadInfo
{
    public EliteDrumPad Pad;           // Which of the 10 pads
    public DrumNoteType Dynamics;      // Neutral, Accent, Ghost
    public EliteDrumsHatState HatState; // Open, Closed, Indifferent
    public bool IsFlam;                // Has a flam grace note
    public bool IsFlamPartner;         // If true, this note is visually hidden
    public bool IsCymbal;              // HiHat, LeftCrash, Ride, RightCrash
}
```

Static helper methods:
- `GetFretIndex(pad)` → 0-based fret (0-7), -1 for kick/hatpedal
- `GetLaneIndex(pad)` → 1-based lane (1-8) for GetElementX, 0 for kick
- `GetColorIndex(pad)` → 0-based color (0-7) for GetNoteColor
- `FromDrumNote(drumNote)` → fallback constructor when no Elite data

---

## Lane Mapping

| Pad | EliteDrumPad Value | Lane (1-based, GetElementX) | Fret (0-based) | Color (0-based, GetNoteColor) |
|-----|--------------------|-----------------------------|-----------------|-------------------------------|
| HiHat | 3 | 1 | 0 | 0 |
| LeftCrash | 4 | 2 | 1 | 1 |
| Snare | 2 | 3 | 2 | 2 |
| Tom1 | 5 | 4 | 3 | 3 |
| Tom2 | 6 | 5 | 4 | 4 |
| Tom3 | 7 | 6 | 5 | 5 |
| Ride | 8 | 7 | 6 | 6 |
| RightCrash | 9 | 8 | 7 | 7 |
| Kick | 1 | 0 (center) | — | — (separate) |
| HatPedal | 0 | -1 (indicator) | — | — |

---

## Task Details

### Task 1: EliteDrumsPlayer.cs

**All abstract methods implemented:**
- `GetNotes` — returns downcharted DrumNote track, stores EliteDrumNote track reference
- `CreateEngine` — creates YargDrumsEngine with `isEliteDrums: true`, `DrumMode.ProFourLane`
- `InitializeSpawnedNote` — casts to EliteDrumsNoteElement, resolves ElitePadInfo, sets on element
- `StarMultiplierThresholds` / `StarScoreThresholds` — copied from DrumsPlayer
- `ShouldUpdateInputsOnResume` — returns false
- `SetStemMuteState` — targets SongStem.Drums
- `InterceptInput` — captures EliteDrumsAction, stores in queue, returns false
- `ConstructReplayData` — creates ReplayFrame with EngineParams and EngineStats

**Virtual overrides:**
- `Initialize` — calls base
- `FinishInitialization` — 8-fret setup, EliteDrumsColors provider, kick flash, hat pedal init
- `OnNoteHit` / `OnNoteMissed` — visual feedback on EliteDrumsNoteElement
- `OnStarPowerPhraseHit` / `OnStarPowerPhraseMissed` / `OnStarPowerStatus` — iterate NotePool
- `SetStarPowerFX` — targets SongStem.Drums
- `ResetVisuals` — calls _fretArray.ResetAll()
- `UpdateVisuals` — fret press states, anim times, hat pedal indicator
- `UpdateInputs` — replay elite action extraction

**Key implementation details:**
- Correlation map built during `GetNotes` via `BuildCorrelationMap()`
- `OnPadHit` handler resolves Elite fret via queue consumption then fallback mapping
- `AnimateEliteFret` uses correlation map for note hit animations
- Fret flash tracking mirrors DrumsPlayer pattern with 8 frets

### Task 2: EliteDrumsNoteElement.cs

- 7 NoteGroups: Normal, Cymbal, Kick, Accent, Ghost, CymbalAccent, CymbalGhost
- `SetThemeModels` follows DrumsNoteElement pattern exactly
- `InitializeElement` positions notes using `GetElementX(lane, 8)` with 1-based indices
- `GetNoteGroup` uses `_padInfo.Dynamics` instead of `NoteRef.IsAccent/IsGhost`
- `UpdateColor` uses `EliteDrumsColors` with 0-based color indices and lefty flip
- Kick notes use dedicated kick color methods
- Open hat indicator: `_openHatIndicator` GameObject, shown when HiHat + Open
- Flam indicator: `_flamIndicator` GameObject, shown when IsFlam
- Flam partner hiding: `IsFlamPartner` causes `HideElement()` early return
- `HitNote` returns to pool immediately (drums have no sustains)
- `CalcStarPowerVisible` mirrors DrumsNoteElement's NoStarPowerOverlap check

### Task 3: HatPedalIndicator.cs

- State machine: `PedalState.Closed` / `PedalState.Open`
- `Initialize` scans EliteDrumNote track for HatPedal events, filters InvisibleTerminator
- `UpdateIndicator` advances through events, sets target state
- Stomp → Closed, Splash → Open
- Animated mesh Y position lerp between `_closedYPosition` and `_openYPosition`
- `Reset()` method for practice section restarts

---

## Deviations from Plan

1. **Drum fill effects**: Not implemented in EliteDrumsPlayer (complex logic in DrumsPlayer). The base `TrackPlayer` handles track effects generically. Drum fill lane calculation would need Elite-specific logic, deferred to Plan 03-03 or post-MVP.

2. **Drum sound effects**: Not implemented. DrumsPlayer has complex round-robin SFX logic with velocity thresholds. Elite Drums would need its own sample mapping. Deferred.

3. **Drum freestyle detection**: Not implemented (DrumsPlayer's `IsDrumFreestyle` method). Can be added when needed.

---

## Verification Results

| Check | Result |
|-------|--------|
| EliteDrumsPlayer implements all abstract methods from TrackPlayer | PASS |
| EliteDrumsPlayer implements all abstract methods from BasePlayer | PASS |
| EliteDrumsNoteElement has all 7 NoteGroup types | PASS |
| Lane positioning uses 1-based indices (1-8) | PASS |
| Color indices use 0-based indices (0-7) | PASS |
| Flam partner hiding implemented | PASS |
| Open hat indicator conditional on HiHat + Open | PASS |
| Hat pedal filters InvisibleTerminator | PASS |
| `dotnet build YARG.Core/YARG.Core.csproj` | PASS (0 errors, 33 pre-existing warnings) |
| `dotnet test YARG.Core.UnitTests` | PASS (53/57, 4 pre-existing failures) |
| No new errors or warnings | PASS |
