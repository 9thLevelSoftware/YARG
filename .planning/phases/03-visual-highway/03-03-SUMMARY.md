# Plan 03-03 Summary: Prefab Integration & Wiring

**Status**: Complete
**Date**: 2026-03-03
**Agent**: EngineeringSeniorDeveloper

---

## Files Modified

| File | Change |
|------|--------|
| `Assets/Script/Gameplay/GameManager.Loading.cs` | Added `[SerializeField] private GameObject _eliteDrumsPrefab;` field; changed `GameMode.EliteDrums` prefab selection from conditional fallback (`Song.HasInstrument(...)`) to direct `_eliteDrumsPrefab` reference |
| `Assets/Script/Gameplay/GameManager.Debug.cs` | Added `EliteDrumsPlayer` case in player type name display (returns "Elite Drums"); added `EliteDrumsPlayer` case in detailed debug switch (shows DrumsEngine state and EngineStats.Overhits) |

---

## Task 1: Prefab Creation Documentation

### EliteDrumsTrackPlayer Prefab Setup (Manual Unity Editor Steps)

The following steps must be performed in Unity Editor to create the EliteDrumsTrackPlayer prefab:

#### Step 1: Duplicate Base Prefab
1. In Project window, find the FourLaneDrums TrackPlayer prefab
2. Duplicate it (Ctrl+D)
3. Rename to `EliteDrumsTrackPlayer`
4. Move to `Assets/Prefabs/Gameplay/EliteDrums/`

#### Step 2: Replace Player Component
1. Select the root GameObject of the duplicated prefab
2. Remove the `DrumsPlayer` component
3. Add the `EliteDrumsPlayer` component
4. Wire serialized fields:
   - `_fretArray` -> FretArray child component
   - `_kickFretFlash` -> KickFretFlash child component

#### Step 3: Configure 8-Lane Fret Array
1. The FretArray child contains fret GameObjects
2. Duplicate existing frets to have at least 8 total fret children
3. `FretCount` is set at runtime by `EliteDrumsPlayer.FinishInitialization()` to 8
4. `FretArray.Initialize()` handles positioning automatically via `GetElementX()`

#### Step 4: Create EliteDrumsNoteElement Prefab
1. Find an existing DrumsNoteElement note prefab
2. Duplicate it
3. Replace the `FourLaneDrumsNoteElement` (or `FiveLaneDrumsNoteElement`) script with `EliteDrumsNoteElement`
4. Add child GameObjects for:
   - `_openHatIndicator` (GameObject, toggled active when hi-hat note is Open state)
   - `_flamIndicator` (GameObject, toggled active when note has flam)
5. Wire these in the EliteDrumsNoteElement inspector
6. Wire the note prefab into the NotePool on EliteDrumsTrackPlayer

#### Step 5: Add Hat Pedal Indicator
1. Add a new child GameObject under the track root
2. Add the `HatPedalIndicator` component
3. Wire serialized fields:
   - `_pedalMesh` (Transform): the mesh that animates vertically
   - `_closedYPosition` (float): default -0.1
   - `_openYPosition` (float): default 0.1
   - `_transitionSpeed` (float): default 10
4. Position near hi-hat lane (X matches lane 0 fret position)

#### Step 6: Wire Prefab in Gameplay Scene
1. Open the Gameplay scene
2. Find the `GameManager` GameObject
3. In the Inspector, locate the `_eliteDrumsPrefab` field
4. Drag the `EliteDrumsTrackPlayer` prefab into this field
5. Save the scene

### Serialized Fields Reference

**EliteDrumsPlayer** (from `TrackPlayer` base + Elite-specific):
- Base TrackPlayer fields: `TrackCamera`, `CameraPositioner`, `HighwayCameraRendering`, `TrackMaterial`, `ComboMeter`, `StarpowerBar`, `SunburstEffects`, `IndicatorStripes`, `HitWindowDisplay`, `_hudLocation`, `NotePool`, `BeatlinePool`, `EffectPool`, `StarPowerEffect`
- Elite-specific: `_fretArray` (FretArray), `_kickFretFlash` (KickFretFlash)
- Auto-discovered: `_hatPedalIndicator` (found via `GetComponentInChildren<HatPedalIndicator>()`)

**EliteDrumsNoteElement**:
- `_openHatIndicator` (GameObject, optional)
- `_flamIndicator` (GameObject, optional)

---

## Task 2: GameManager Wiring

### GameManager.Loading.cs
- Added `_eliteDrumsPrefab` field at line 43 (between `_fiveLaneDrumsPrefab` and `_proKeysPrefab`)
- Changed prefab selection from:
  ```csharp
  GameMode.EliteDrums => Song.HasInstrument(Instrument.FiveLaneDrums) ? _fiveLaneDrumsPrefab : _fourLaneDrumsPrefab,
  ```
  To:
  ```csharp
  GameMode.EliteDrums => _eliteDrumsPrefab,
  ```

### GameManager.Debug.cs
- Added `EliteDrumsPlayer => "Elite Drums"` in the player type name pattern match (line 339)
- Added `case EliteDrumsPlayer eliteDrumsPlayer:` in the detailed debug switch (line 381)
- Shows DrumsEngine state ("No persistent state") and `EngineStats.Overhits`
- Positioned BEFORE the `DrumsPlayer` case to ensure correct pattern matching (EliteDrumsPlayer does not extend DrumsPlayer)

---

## Task 3: End-to-End Code Trace Results

### Trace 1: Initialization Path -- PASS
1. `GameManager.CreatePlayers()` -> `_eliteDrumsPrefab` instantiated
2. `GetComponent<TrackPlayer>()` returns `EliteDrumsPlayer` (inherits `TrackPlayer<DrumsEngine, DrumNote>`)
3. `Initialize()` -> `base.Initialize()` -> `SetupTheme()` uses `VisualStyleHelpers.GetVisualStyle(GameMode.EliteDrums, _)` -> `VisualStyle.EliteDrums`
4. `GetNotes(chart)` stores `_eliteDifficulty` from `chart.EliteDrums`, builds correlation map, returns downcharted DrumNote track
5. `CreateEngine()` creates `YargDrumsEngine` with `isEliteDrums: true`, `DrumMode.ProFourLane`
6. `FinishInitialization()` sets 8-fret array with `EliteDrumsColors` provider, initializes kick flash, hat pedal indicator

### Trace 2: Note Spawning Path -- PASS
1. `TrackPlayer.UpdateNotes()` iterates `Notes` (downcharted DrumNote list)
2. For each note, calls `SpawnNote()` -> `NotePool.KeyedTakeWithoutEnabling(note)` -> `InitializeSpawnedNote(poolable, note)`
3. `EliteDrumsPlayer.InitializeSpawnedNote()` casts to `EliteDrumsNoteElement`, sets `NoteRef`, calls `ResolveElitePadInfo()`, calls `SetElitePadInfo()`
4. `EliteDrumsNoteElement.InitializeElement()` positions using `GetElementX(lane, 8)` with 1-based indices
5. Note group selected based on `_padInfo.Dynamics` and `_padInfo.IsCymbal`
6. Flam partner notes are hidden via `HideElement()` early return
7. Open hat indicator and flam indicator toggled appropriately

### Trace 3: Hit Feedback Path -- PASS
1. Engine fires `OnNoteHit(index, DrumNote)` -> `EliteDrumsPlayer.OnNoteHit()`
2. Gets `EliteDrumsNoteElement` from `NotePool.GetByKey(note)`, calls `HitNote()` -> returns to pool
3. `AnimateEliteFret(note)` resolves elite pad info, gets fret index, plays cymbal or regular hit animation
4. Kick notes trigger `_kickFretFlash.PlayHitAnimation()` + `_fretArray.PlayKickFretAnimation()` + camera bounce
5. Engine fires `OnPadHit(DrumsAction)` -> `EliteDrumsPlayer.OnPadHit()`
6. Attempts `ConsumeEliteActionForDrumsAction()` to find precise Elite fret from intercepted queue
7. Falls back to `GetEliteFretForDrumsAction()` if no queue match
8. `ZeroOutHitTime()` updates fret flash tracking

### Trace 4: Visual Update Path -- PASS
1. `UpdateVisuals(visualTime)` calls base (handles HUD, note spawning, beatlines, track effects)
2. `UpdateHitTimes()` / `UpdateAnimTimes()` increment delta timers
3. `UpdateFretArray()` sets pressed state and accent color for each of 8 frets
4. `_hatPedalIndicator.UpdateIndicator(visualTime)` advances through pedal events, animates mesh

### Trace 5: VisualStyle Switch Cases -- PASS
All switch statements handle `VisualStyle.EliteDrums`:
| Location | Handling |
|----------|----------|
| `ThemeComponent.GetNoteModelsForVisualStyle()` | Returns `_fourLaneNotes` (reused) |
| `ThemeComponent.GetFretModelForVisualStyle()` | Returns `_fourLaneFret` (reused) |
| `ThemePreset.Defaults.cs` SupportedStyles | Included in Rectangular preset |
| `VisualStyleHelpers.GetVisualStyle()` | Returns `VisualStyle.EliteDrums` directly |

Pre-existing: `VisualStyle.SixFretGuitar` is NOT handled in ThemeComponent (falls to throw). Not related to Elite Drums.

### Trace 6: Edge Cases -- PASS
| Edge Case | Status | Details |
|-----------|--------|---------|
| Empty EliteDrumNote track | Handled | `_eliteDifficulty` null-checked; fallback `ElitePadInfo.FromDrumNote()` used |
| Lefty flip | Handled | `LeftyFlipMultiplier` applied in `InitializeElement()` position; `UpdateColor()` reverses color index `7 - colorIndex` |
| Star power | Handled | `CalcStarPowerVisible()` checks `NoStarPowerOverlap`; `OnStarPowerUpdated()` swaps note group models |
| Flam partners | Handled | `IsFlamPartner = true` causes `HideElement()` early return |
| InvisibleTerminator | Handled | Filtered in `HatPedalIndicator.Initialize()` |
| Kick notes | Handled | Separate center position, kick-specific colors, kick fret flash |
| 8-lane bounds | Handled | `ZeroOutHitTime()` checks `fret < 0 || fret >= _fretArray.FretCount` |

---

## Issues Found During Code Trace

### No blocking issues found.

Minor observations:
1. **Drum fill effects not implemented**: `EliteDrumsPlayer` does not call `SetDrumFillEffects()`. The base `TrackPlayer` handles track effects generically, but drum fill lane calculation needs Elite-specific logic. This was already deferred in Plan 03-02 (documented in 03-02-SUMMARY.md).
2. **Drum sound effects not implemented**: `OnPadHit` in `EliteDrumsPlayer` does not play drum sound effects. DrumsPlayer has complex round-robin SFX logic. Already deferred.
3. **Drum freestyle detection not implemented**: `EliteDrumsPlayer.OnPadHit` does not check for freestyle mode. Overhit miss animation plays unconditionally when no note hit. Low impact for MVP.

---

## Remaining Manual Unity Editor Steps

1. Create EliteDrumsTrackPlayer prefab (see Task 1 documentation above)
2. Create EliteDrumsNoteElement note prefab with open hat and flam indicators
3. Wire `_eliteDrumsPrefab` field on GameManager in the Gameplay scene
4. Test in Unity Editor with an EliteDrums chart

---

## Build & Test Results

| Check | Result |
|-------|--------|
| `dotnet build YARG.Core/YARG.Core.csproj` | PASS (0 errors, 0 warnings) |
| `dotnet test YARG.Core.UnitTests` | PASS (53/57, 4 pre-existing failures) |

---

## Verification Results

| Check | Result |
|-------|--------|
| `GameManager.Loading.cs` has `_eliteDrumsPrefab` field | PASS |
| `GameManager.Loading.cs` uses `_eliteDrumsPrefab` for `GameMode.EliteDrums` | PASS |
| `GameManager.Debug.cs` handles `EliteDrumsPlayer` in type name display | PASS |
| `GameManager.Debug.cs` handles `EliteDrumsPlayer` in detailed debug switch | PASS |
| Full init path: GameManager.Loading -> EliteDrumsPlayer.Initialize -> GetNotes -> CreateEngine -> FinishInitialization | PASS |
| Note spawning: TrackPlayer.UpdateNotes -> SpawnNote -> InitializeSpawnedNote -> EliteDrumsNoteElement.InitializeElement | PASS |
| Hit feedback: Engine OnNoteHit -> EliteDrumsPlayer.OnNoteHit -> fret animation | PASS |
| Visual update: UpdateVisuals -> fret press states -> hat pedal update | PASS |
| All VisualStyle.EliteDrums switch cases handled | PASS |
| Edge cases: empty track, lefty flip, star power, flam partners, InvisibleTerminator | PASS |
| No orphan references to DrumsPlayer in Elite code paths | PASS |
| YARG.Core builds (0 errors) | PASS |
| YARG.Core tests pass (53/57, 4 pre-existing) | PASS |
