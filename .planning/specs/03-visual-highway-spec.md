# Phase 3 Spec: Elite Drums Visual Highway

**Phase**: 3 — Visual Highway
**Status**: Reviewed (post-critique)
**Date**: 2026-03-03

---

## 1. Overview

Build a dedicated 10-lane visual highway for Elite Drums that renders notes for all 10 drum pads with dynamics (accent/ghost), hi-hat articulation (open/closed/sizzle), flam indicators, and a hi-hat pedal state indicator. This is the largest phase — primarily net-new code.

### Scope

**In scope:**
- 8 note lanes (HiHat, LeftCrash, Snare, Tom1, Tom2, Tom3, Ride, RightCrash) + Kick (off-lane) + HatPedal (indicator)
- Accent/ghost note visual differentiation (size + brightness)
- Hi-hat state visualization (open/closed/sizzle note variants on hi-hat lane)
- Flam note double-hit indicator
- Per-pad color coding (cymbals vs toms vs snare vs kick)
- Fret array with 8 main frets + kick fret
- Beatlines spanning full track width
- Camera accommodation for the wider layout

**Out of scope:**
- Native EliteDrumsEngine (engine continues to use DrumsEngine + DrumNote downchart)
- Custom theme models (reuse existing Rectangular note shapes initially)
- Separate star power/solo handling (reuse existing DrumsPlayer logic)

---

## 2. Core Architecture Decision

### The Type Gap Problem

The engine operates on `DrumNote` (downcharted from `EliteDrumNote`). Visual rendering needs `EliteDrumNote` data because the downchart loses information:
- All hi-hat variants (closed/sizzle/open) collapse to `YellowCymbal`
- LeftCrash and Ride both map to `BlueCymbal` (ambiguous)
- Hat pedal notes (stomp/splash) have no DrumNote equivalent
- Flam state is not preserved in DrumNote

### Solution: Supplemental Lookup Pattern

```
EliteDrumsPlayer : TrackPlayer<DrumsEngine, DrumNote>
├── Engine: DrumsEngine + InstrumentTrack<DrumNote> (scoring, hit/miss state)
├── Visual supplement: InstrumentTrack<EliteDrumNote> reference + tick index
├── Note elements: EliteDrumsNoteElement : NoteElement<DrumNote, EliteDrumsPlayer>
│   ├── NoteRef = DrumNote (pool lifecycle, hit/miss from engine)
│   └── ElitePadInfo = struct { Pad, Dynamics, HatState, IsFlam } (from lookup)
└── Hat pedal: Visual-only indicator (state derived from EliteDrumNote sequence)
```

**Why this approach:**
1. `NoteElement<DrumNote, ...>` keeps pool management and hit/miss state working (engine sets `WasHit`/`WasMissed` on DrumNote objects)
2. Supplemental EliteDrumNote lookup provides correct pad assignment, dynamics, hat state, and flam data
3. Avoids modifying YARG.Core engine or the generic TrackPlayer infrastructure
4. Hat pedal is a positional indicator (not scored), rendered separately from the note pool

### DrumNote → EliteDrumNote Correlation

**Critique finding**: Runtime reverse-engineering of the downchart mapping is fragile. Flam notes produce extra "ghost partner" DrumNotes with no 1:1 correspondence. Collision resolution can change DrumNote pads from the default mapping. Channel flags can force any pad to any color.

**Revised approach — Build-time correlation via downchart origin tracking**:

The `MoonSongLoader.EliteDrumsDownchart.cs` already has the `Origin` (source EliteDrumNote) available during downcharting. Two implementation paths:

**Path A (Preferred): Store origin reference during downchart**
1. Add an `EliteDrumOrigin` field (or tag) to `DrumNote` during the downchart pass
2. Each DrumNote remembers which EliteDrumNote it came from
3. At visual spawn time, read `note.EliteDrumOrigin` directly — no mapping needed
4. **Trade-off**: Requires a small YARG.Core change (adding a nullable field to DrumNote)

**Path B (Fallback): Parallel iteration at load time**
1. During `EliteDrumsPlayer.Initialize()`, iterate both tracks (DrumNote + EliteDrumNote) in tick order
2. Build a `Dictionary<DrumNote, EliteDrumNote>` by matching notes at the same tick
3. Use the downchart mapping rules (including collision resolution and channel flags) to disambiguate
4. For flam-generated DrumNotes (extra notes at same tick), mark them as "flam partner" — don't assign an EliteDrumNote origin
5. **Trade-off**: Complex matching logic, but no YARG.Core changes

**For either path, flam handling**:
- A flam EliteDrumNote produces 2 DrumNotes: the "real" note on the correct pad, and a "flam partner" on an adjacent pad
- The visual system shows ONE flam indicator on the real note's lane — the flam partner DrumNote is visually hidden or merged
- The engine scores both DrumNotes independently (existing behavior)

**Decision**: Implementation should try Path A first. If YARG.Core changes are blocked, fall back to Path B.

---

## 3. Lane Layout

### Physical Kit Layout (Left → Right, Drummer's Perspective)

```
Lane:    0        1         2       3      4      5       6       7
Pad:   HiHat  LeftCrash  Snare   Tom1   Tom2   Tom3   Ride  RightCrash
Type:  cymbal  cymbal     pad     pad    pad    pad    cymbal  cymbal

         [Kick] ← off-lane, below the highway (existing KickFret system)
    [HatPedal] ← indicator adjacent to HiHat lane (visual-only)
```

### Lane Count: 8 Main + Kick + Hat Pedal Indicator

- **8 main lanes** use the existing `FretArray` system with `FretCount = 8`
- **Kick** uses the existing `KickFret` system (off-lane, at bottom of highway)
- **Hat pedal** is a visual indicator next to the HiHat lane (lane 0), not a note lane

### FretArray Scaling at 8 Lanes

With `TRACK_WIDTH = 2f` and `FretCount = 8`:
- Fret scale: `(2/2) / (8/5) = 0.625` (acceptable — ProDrums split view uses 0.714 at 7 lanes)
- Note spacing: `2/8 = 0.25` units between lane centers (dense but readable)

### Note/Fret Alignment (Critique Fix)

**Critique finding**: `GetElementX(index, subdivisions)` and `FretArray` positioning use different offset signs (`-1/sub` vs `+1/sub`). Existing drum note elements compensate by using **1-based lane indices** (lane 1 = first pad, not lane 0), which aligns with FretArray's 0-based fret position indexing.

**Rule**: `EliteDrumsNoteElement` must pass **1-based lane indices** to `GetElementX()`:
- `GetElementX(1, 8)` aligns with FretArray fret position 0 (HiHat)
- `GetElementX(2, 8)` aligns with FretArray fret position 1 (LeftCrash)
- ...through `GetElementX(8, 8)` aligns with FretArray fret position 7 (RightCrash)

The pad-to-lane mapping becomes:

| Elite Pad | Note Lane Index (1-based) | Fret Position (0-based) |
|---|---|---|
| HiHat | 1 | 0 |
| LeftCrash | 2 | 1 |
| Snare | 3 | 2 |
| Tom1 | 4 | 3 |
| Tom2 | 5 | 4 |
| Tom3 | 6 | 5 |
| Ride | 7 | 6 |
| RightCrash | 8 | 7 |

### FretArray Swap Logic

**Critique finding**: FretArray.Initialize() has `swapSnareAndHiHat` and `swapCrashAndRide` parameters hardcoded for the 7-fret split layout. For Elite Drums, pass `false` for both to avoid incorrect fret position swapping.

### FretArray Pre-existing Bug

**Critique finding**: `FretArray.PlayMissAnimation()` has an off-by-one: `index <= _frets.Count` should be `index < _frets.Count`. Fix this to prevent IndexOutOfRangeException at 8 frets.

### Lefty Flip

Lane order reverses (RightCrash on left, HiHat on right). Handled by existing `LeftyFlipMultiplier` in `GetElementX()`.

---

## 4. Note Visual Design

### Note Types (ThemeNoteType Groups)

| Group | Visual | ThemeNoteType Mapping |
|---|---|---|
| Pad (Snare, Toms) | Standard drum gem | `ThemeNoteType.Normal` |
| Cymbal (HH, Crashes, Ride) | Diamond/cymbal gem | `ThemeNoteType.Cymbal` |
| Kick | Wide bar across bottom | `ThemeNoteType.Kick` |
| Accent Pad | Larger + brighter | `ThemeNoteType.Accent` |
| Accent Cymbal | Larger + brighter diamond | `ThemeNoteType.CymbalAccent` |
| Ghost Pad | Smaller + dimmer | `ThemeNoteType.Ghost` |
| Ghost Cymbal | Smaller + dimmer diamond | `ThemeNoteType.CymbalGhost` |

These 7 `ThemeNoteType` values already exist in `DrumsNoteElement.NoteType`. Reuse the same model assignment pattern.

### Hi-Hat State Variants

The hi-hat lane (lane 0) notes need visual distinction for hat state.

**Critique finding**: The `EliteDrumsHatState` enum has only `Open`, `Closed`, and `Indifferent` — there is no `Sizzle` value in the data model. The sizzle hi-hat exists as an input action (`EliteSizzleHiHat`) and a MIDI note mapping, but the chart loader does not parse sizzle into a separate hat state. Sizzle visualization would require a YARG.Core loader change.

**Revised scope**: Phase 3 supports Open/Closed/Indifferent only. Sizzle is deferred to post-MVP (requires YARG.Core chart loader change to add `EliteDrumsHatState.Sizzle`).

| Hat State | Visual Treatment |
|---|---|
| Closed | Standard cymbal gem (default) |
| Open | Cymbal gem + open circle indicator (ring around the gem) |
| Indifferent | Standard cymbal gem (same as closed) |

**Implementation**: Use a child GameObject on the note element that shows/hides based on hat state. The open indicator is a ring mesh. Attach to the note prefab.

### Flam Indicator

Flam notes show a double-hit marker: a smaller "echo" gem offset slightly before the main note (similar to a grace note visual).

**Implementation**: A second smaller NoteGroup instance positioned slightly ahead (+Z offset) of the main note. Shown when `ElitePadInfo.IsFlam == true`.

### Hat Pedal Indicator

A small visual element adjacent to the hi-hat lane that shows current pedal state.

**Data source (critique clarification)**: Hat pedal state is derived from `EliteDrumNote` entries where `Pad == EliteDrumPad.HatPedal`. These are discrete chart events with `HatPedalType`:
- `Stomp`: Transition to closed (pedal pressed down)
- `Splash`: Brief press then release (pedal bounces)
- `InvisibleTerminator`: Silent state reset — must NOT produce a visual

**State machine**:
```
Initial state: Closed (pedal down)
Stomp event → transition to Closed (with pulse animation)
Splash event → brief Closed, then transition to Open (with pulse animation)
Between events → maintain current state
InvisibleTerminator → reset state silently (no visual)
```

| State | Visual |
|---|---|
| Pedal down (closed hat) | Indicator in "closed" position (lowered) |
| Pedal up (open hat) | Indicator in "open" position (raised) |
| Stomp transition | Brief pulse animation on close |
| Splash transition | Brief pulse animation, then return to open |

**Implementation**: A persistent animated sprite/mesh near lane 0 (hi-hat). Not pooled. State changes driven by scanning EliteDrumNote hat pedal events as visual time progresses (separate from the DrumNote iteration).

---

## 5. Color Scheme

### New `EliteDrumsColors` Color Provider

Add `EliteDrumsColors : IFretColorProvider` to `ColorProfile` (partial class in new file `ColorProfile.EliteDrums.cs`).

### Default Color Assignments

| Lane | Pad | Fret Color | Note Color | Category |
|---|---|---|---|---|
| 0 | HiHat | Yellow | Yellow | Cymbal |
| 1 | LeftCrash | Blue | Blue | Cymbal |
| 2 | Snare | Red | Red | Pad |
| 3 | Tom1 | Yellow | Yellow | Pad |
| 4 | Tom2 | Blue | Blue | Pad |
| 5 | Tom3 | Green | Green | Pad |
| 6 | Ride | Blue | Blue | Cymbal |
| 7 | RightCrash | Green | Green | Cymbal |
| - | Kick | Orange | Orange | Kick |

Colors follow established YARG drum color conventions (Red=Snare, Yellow=HiHat/Tom1, Blue=Tom2/Ride/LeftCrash, Green=Tom3/RightCrash, Orange=Kick). Cymbals vs pads are distinguished by model shape (diamond vs round), not color.

### Star Power Colors

All notes shift to star power blue/yellow when SP active (same as existing drums behavior).

### Accent/Ghost Color Modifiers

- **Accent**: Fret color whitened by 30% (existing `UpdateAccentColorState` pattern)
- **Ghost**: Note color dimmed by 40% (reduced alpha or brightness)

---

## 6. Files to Create

### 6.1 `EliteDrumsNoteElement.cs`
**Path**: `Assets/Script/Gameplay/Visuals/TrackElements/EliteDrums/EliteDrumsNoteElement.cs`
**Inherits**: `NoteElement<DrumNote, EliteDrumsPlayer>, IThemeNoteCreator`
**Responsibilities**:
- Store `ElitePadInfo` struct (pad, dynamics, hatState, isFlam)
- Position notes in 8-lane layout using `GetElementX(lane, 8)`
- Select note group based on pad type (cymbal vs pad) + dynamics
- Render hi-hat state variants (open/closed indicators)
- Render flam indicator
- Apply per-pad color from `EliteDrumsColors`
- Handle hit/miss visual state from `NoteRef` (DrumNote)
- Handle star power visual swap
- Handle activation note pulsing

**Note**: Does NOT inherit from `DrumsNoteElement` because that class is typed `NoteElement<DrumNote, DrumsPlayer>` (wrong TPlayer). Instead, shares similar logic (note groups, hit handling) implemented independently.

### 6.2 `EliteDrumsPlayer.cs`
**Path**: `Assets/Script/Gameplay/Player/EliteDrumsPlayer.cs`
**Inherits**: `TrackPlayer<DrumsEngine, DrumNote>`
**Responsibilities**:
- Initialize with DrumsEngine (DrumNote track for scoring) + EliteDrumNote track reference
- Build tick→EliteDrumNote correlation mapping
- Override `InitializeSpawnedNote()` to set ElitePadInfo on note elements
- Manage 8-lane FretArray with proper color provider
- Handle kick fret (reuse existing KickFret system)
- Manage hat pedal indicator state
- Map engine OnNoteHit/OnNoteMissed callbacks to fret animations (8 lanes)
- Handle input visualization for 10 pad types

**Relationship to DrumsPlayer**: Does NOT extend DrumsPlayer. Parallel class that shares similar engine setup but has different visual management. This avoids polluting DrumsPlayer with conditional Elite logic.

### 6.3 `ColorProfile.EliteDrums.cs`
**Path**: `YARG.Core/YARG.Core/Game/Presets/ColorProfile.EliteDrums.cs`
**Partial class of**: `ColorProfile`
**Contains**: `EliteDrumsColors` class implementing `IFretColorProvider`
**IFretColorProvider index mapping (1-based, matches FretArray.InitializeColor `i+1` convention)**: 0=Kick, 1=HiHat, 2=LeftCrash, 3=Snare, 4=Tom1, 5=Tom2, 6=Tom3, 7=Ride, 8=RightCrash
**GetNoteColor index mapping (0-based, used by note elements)**: 0=HiHat, 1=LeftCrash, 2=Snare, 3=Tom1, 4=Tom2, 5=Tom3, 6=Ride, 7=RightCrash
**Additional**: `GetNoteColor(int)`, `GetNoteStarPowerColor(int)`, `GetActivationNoteColor(int)`, `Miss` color, `GetKickFretColor()`, `GetMetalColor(bool)`

---

## 7. Files to Modify

### 7.1 `ThemeManager.cs` + `ThemeComponent.cs` — Add VisualStyle.EliteDrums
- `ThemeManager.cs`: Add `EliteDrums` to `VisualStyle` enum
- `ThemeComponent.cs` (critique fix): Add `EliteDrums` case in `GetNoteModelsForVisualStyle()` → reuse `_fourLaneNotes` models initially. Also add case in `GetFretModelForVisualStyle()` → reuse `_fourLaneFret`. Both methods throw on unhandled values — MUST add the case.
- `ThemePreset.Defaults.cs`: Add `VisualStyle.EliteDrums` to Rectangular preset's SupportedStyles

### 7.2 `VisualStyleHelpers.cs` — Route EliteDrums
- Change `GameMode.EliteDrums` case to return `VisualStyle.EliteDrums` (currently falls back to FourLane/FiveLane)

### 7.3 `GameManager.Loading.cs` — Add EliteDrums Prefab
- Add `[SerializeField] private GameObject _eliteDrumsPrefab;` field
- Update prefab selection switch: `GameMode.EliteDrums => _eliteDrumsPrefab`
- Wire prefab reference in Gameplay scene

### 7.4 `ColorProfile.cs` — Add EliteDrumsColors Field
- Add `[SettingSubSection] public EliteDrumsColors EliteDrums;` to ColorProfile class

### 7.5 `FretArray.cs` — Verify 8-Lane Support
- No code changes expected — FretCount is runtime-configurable
- Verify scaling formula works at 8 (scale = 0.625, acceptable)
- May need to adjust `WIDTH_NUMERATOR`/`WIDTH_DENOMINATOR` if frets appear too thin

### 7.6 Localization (`en-US.json`)
- Add any new Elite Drums-specific UI strings (lane names for accessibility, etc.)

---

## 8. Prefab Structure

### EliteDrums TrackPlayer Prefab

Create by duplicating the FourLaneDrums prefab and modifying:

```
EliteDrumsTrackPlayer (root)
├── Track (mesh + material)
│   ├── Beatlines (pool)
│   └── Notes (pool)
├── FretArray
│   ├── Fret[0..7] (8 main frets: HH, LCrash, Snare, T1, T2, T3, Ride, RCrash)
│   └── KickFret (existing system)
├── HatPedalIndicator (new: adjacent to lane 0)
├── TrackCamera
├── CameraPositioner
├── TrackMaterial
└── Effects (starpower, combo, etc.)
```

**EliteDrumsPlayer component** replaces `DrumsPlayer` on the root GameObject.
**EliteDrumsNoteElement prefab** attached to the note pool.

---

## 9. Engine Integration Details

### Engine Creation (in EliteDrumsPlayer)

Reuse the exact same engine creation as Phase 1/2:
```csharp
var mode = DrumsEngineParameters.DrumMode.ProFourLane; // or NonProFourLane based on chart
var engine = new YargDrumsEngine(..., isEliteDrums: true);
```

The engine processes the downcharted DrumNote track. All scoring logic is unchanged.

### Input Action Mapping

EliteDrumsAction inputs are already converted to DrumsAction by `YargDrumsEngine.ConvertMidiDrumsInput()` (Phase 2 work). The engine correctly processes all 13 Elite pad inputs.

### Hit Feedback Flow

```
Player hits MIDI pad
  → EliteDrumsAction generated
  → ConvertMidiDrumsInput() → DrumsAction
  → Engine processes against DrumNote track
  → Engine sets DrumNote.WasHit = true
  → Visual system reads DrumNote.WasHit
  → EliteDrumsNoteElement animates hit on the correct Elite lane
```

The last step requires the note element to know which Elite lane it's on (from ElitePadInfo), not which DrumNote pad it represents.

### Fret Animation Flow

**Critique finding**: `OnPadHit` provides `DrumsAction` (not `EliteDrumsAction`). The engine converts Elite inputs to DrumsAction before firing. This loses Elite pad identity (e.g., all hi-hat variants → `DrumsAction.Cymbal1`).

**Solution**: `EliteDrumsPlayer` must intercept input BEFORE the engine converts it. Override `InterceptInput()` to capture the original `EliteDrumsAction` and record which Elite lane was struck. Then the fret animation uses this recorded Elite lane, not the engine's DrumsAction.

```
Player hits MIDI pad (e.g., EliteDrums.Tom2)
  → EliteDrumsPlayer.InterceptInput(EliteDrumsAction.EliteTom2)
  → Record: lastEliteAction = EliteTom2, eliteLane = 4
  → Engine converts to DrumsAction.Drum3
  → Engine fires OnPadHit(DrumsAction.Drum3)
  → EliteDrumsPlayer.OnPadHit() uses recorded eliteLane (4)
  → FretArray.PlayHitAnimation(4) + SetPressedDrum(4, true, ...)
```

---

## 10. Edge Cases & Risks

### E1: EliteDrumNote ↔ DrumNote Correlation Failures
**Risk**: Tick-based lookup misses a match due to timing edge cases.
**Mitigation**: Log warnings when no EliteDrumNote found for a DrumNote. Fall back to DrumNote-only rendering (position by DrumNote pad, lose hat state/flam data). This is degraded but not broken.

### E2: Hat Pedal Notes Not in DrumNote Track
**Risk**: Stomp/Splash notes exist only in EliteDrumNote. They map to null in ConvertMidiDrumsInput.
**Mitigation**: Hat pedal is visual-only (indicator element, not scored notes). Read directly from EliteDrumNote track on a time-based scan, independent of DrumNote iteration.

### E3: 8-Lane Visual Density
**Risk**: Notes may appear too small or dense at 8 lanes within TRACK_WIDTH=2f.
**Mitigation**:
- ProDrums split view already works at 7 lanes (scale 0.714) — 8 lanes (0.625) is only slightly denser
- Note height can be tuned via `HighwayPreset.NoteHeight`
- If too dense, consider widening only the Elite track mesh (not changing the constant)

### E4: No Existing EliteDrums Theme Models
**Risk**: ThemeManager won't have dedicated EliteDrums note/fret models.
**Mitigation**: Initially reuse FourLaneDrums models (same gem shapes). The VisualStyle mapping can point to existing model arrays. Custom Elite models are post-MVP.

### E5: Lefty Flip with 8 Lanes
**Risk**: Lane order reversal with hat pedal indicator on wrong side.
**Mitigation**: `LeftyFlipMultiplier` handles lane position reversal. Hat pedal indicator position must also flip (bind to lane 0's position after flip).

### E6: Songs Without EliteDrumNote Track
**Risk**: EliteDrumsPlayer initialization with no EliteDrumNote data (edge case: bug or corrupt chart).
**Mitigation**: Guard in Initialize(): if `SongChart.EliteDrums.IsEmpty`, fall back to standard DrumsPlayer rendering with warning log.

### E7: Multi-Player with Mixed Drum Modes
**Risk**: One player on Elite, another on 4-lane drums. Camera/HUD layout must handle different track widths.
**Mitigation**: HighwayCameraRendering already tiles highways independently. Each track has its own camera. No cross-track interference expected.

---

## 11. Success Criteria Mapping

| ROADMAP Criterion | Spec Section |
|---|---|
| 10-lane highway renders with correct lane ordering | Section 3 (Lane Layout) |
| Notes scroll at correct speed matching song tempo | Section 9 (Engine Integration — uses existing TrackPlayer scrolling) |
| Each pad type has distinct color/shape | Sections 4 (Note Visual Design) + 5 (Color Scheme) |
| Accent notes visually larger, ghost notes visually smaller | Section 4 (Note Types) |
| Hi-hat open/closed/sizzle states shown | Section 4 (Hi-Hat State Variants) |
| Flam notes have visible double-hit indicator | Section 4 (Flam Indicator) |
| Frets light up on hit with correct pad color | Sections 5 (Colors) + 9 (Fret Animation Flow) |
| Beatlines render correctly across all 10 lanes | Section 3 (reuse existing, spans TRACK_WIDTH) |
| Track width and camera accommodate wider layout | Section 3 (8 lanes at TRACK_WIDTH=2f) + Section 10 E3 |
| No z-fighting or visual artifacts | Section 10 E3 (density) + standard Unity rendering |

---

## 12. Estimated Complexity

| Component | New Lines (est.) | Risk |
|---|---|---|
| EliteDrumsPlayer.cs | ~400 | Medium — parallel to DrumsPlayer, not trivial |
| EliteDrumsNoteElement.cs | ~300 | Medium — note group logic + hat/flam indicators |
| ColorProfile.EliteDrums.cs | ~120 | Low — follows established pattern |
| VisualStyle additions | ~30 | Low — enum + switch cases |
| GameManager.Loading changes | ~10 | Low — prefab wire-up |
| Prefab creation | Unity Editor | Medium — manual scene work |
| Hat pedal indicator | ~100 | Medium — new visual element |
| **Total** | **~960** | **Medium overall** |
