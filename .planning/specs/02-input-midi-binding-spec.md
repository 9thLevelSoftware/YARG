# Spec: Phase 2 — Input & MIDI Binding

**Phase**: 2 of 4
**Created**: 2026-03-03
**Status**: Final

## 1. Objective

Replace the placeholder Elite Drums binding dialog (which delegates to 4-lane/5-lane bindings) with a proper 13-action binding flow for all Elite Drums pads, and wire up default General MIDI drum note mappings. After this phase, a MIDI drum kit can be fully bound to all Elite Drums actions with bindings that persist across sessions.

## 2. Current State Analysis

### What exists and works:
- `EliteDrumsAction` enum (13 actions): Kick, Stomp, Splash, Snare, ClosedHiHat, SizzleHiHat, OpenHiHat, LeftCrash, Tom1-3, Ride, RightCrash
- `DialogManager.cs` routes `GameMode.EliteDrums` to the correct prefab
- Localization keys in `en-US.json` fully cover all Elite Drums binding names
- `FriendlyBindingDialog` base class provides visual binding loop (highlight → wait → bind → next)
- MIDI input arrives as normalized float velocity via Minis library → `MidiNoteControl` → `DrumPadButtonBinding`
- Profile serialization is generic per GameMode — bindings auto-persist once defined
- `GameModeExtensions.cs` settings for EliteDrums already configured

### What's broken/placeholder:
- `BindingCollection.Templates.cs:CreateEliteDrumsBindings()` has all 13 Elite-specific bindings **commented out** with 4L/5L fallback bindings active instead
- `EliteDrumsBindingDialog.cs` implements a 4L↔5L mode-switching workflow instead of Elite-specific binding
- `FriendlyBindingDialog.cs` has a hardcoded Elite→4L/5L mapping in `GetHighlightByName()` (lines 243-267)
- `FriendlyEliteDrumsBindingDialog.prefab` has visual layout for 4L/5L drum kits, not Elite 10-pad layout

### Phase 1 architectural context:
The engine uses downchart-first architecture — `EliteDrumsAction` inputs get converted to `DrumsAction` via `ConvertMidiDrumsInput()`. Stomp/Splash are intentionally unhandled (return null) because native EliteDrumNote engine support is deferred to Phase 3. Binding Stomp/Splash in Phase 2 is still correct — the bindings exist for when Phase 3 wires them.

## 3. Technical Design

### 3.1 Binding Template Changes

**File**: `Assets/Script/Input/Bindings/BindingCollection.Templates.cs`

Uncomment all 13 Elite-specific bindings in `CreateEliteDrumsBindings()`:
```
Kick (action 0) — keep as-is
EliteStomp (action 1) — uncomment
EliteSplash (action 2) — uncomment
EliteSnare (action 3) — uncomment
EliteClosedHiHat (action 4) — uncomment
EliteSizzleHiHat (action 5) — uncomment
EliteOpenHiHat (action 6) — uncomment
EliteLeftCrash (action 7) — uncomment
EliteTom1 (action 8) — uncomment
EliteTom2 (action 9) — uncomment
EliteTom3 (action 10) — uncomment
EliteRide (action 11) — uncomment
EliteRightCrash (action 12) — uncomment
```

**Decision: Keep or remove 4L/5L fallback bindings?**
**Remove them.** The 4L/5L fallbacks exist because Elite-specific bindings were commented out. Once Elite bindings are active, the fallbacks are redundant and confusing. The downchart engine path (`ConvertMidiDrumsInput`) already maps Elite actions → DrumsAction, so the 4L/5L actions in the enum are only needed for backward compatibility of saved profiles — and no one has Elite Drums profiles saved yet (the mode was never functional).

**Binding type**: All 13 actions use `DrumPadButtonBinding`. Stomp/Splash will work with this because:
- MIDI pedals send note-on with velocity (Minis normalizes to float)
- The "no velocity" annotation means the *engine* doesn't score velocity, not that the input has none
- If a non-MIDI device is used, button press → value 1.0 (binary), which is fine

### 3.2 Default MIDI Note Mappings

Standard General MIDI drum note numbers to use as defaults:

| Action | GM Note | GM Name |
|--------|---------|---------|
| Kick | 36 | Bass Drum 1 |
| Snare | 38 | Acoustic Snare |
| ClosedHiHat | 42 | Closed Hi-Hat |
| OpenHiHat | 46 | Open Hi-Hat |
| Stomp | 44 | Pedal Hi-Hat |
| SizzleHiHat | 42 | Closed Hi-Hat (no standard; reuse closed) |
| LeftCrash | 49 | Crash Cymbal 1 |
| RightCrash | 57 | Crash Cymbal 2 |
| Ride | 51 | Ride Cymbal 1 |
| Tom1 | 50 | High Tom |
| Tom2 | 48 | Mid Tom |
| Tom3 | 43 | Low Floor Tom |
| Splash | 44 | Pedal Hi-Hat (same as stomp; different gesture) |

**Note**: SizzleHiHat has no GM standard. Default to closed hi-hat (42) — users with modules that output distinct sizzle notes will rebind. Splash shares note 44 with Stomp — different physical gesture (lift vs press), same MIDI note on most kits.

**Implementation**: Look for how other game modes set default MIDI bindings. If there's a default binding mechanism, wire it. If not, the binding dialog handles initial binding interactively (which is the current pattern — no drum mode has default MIDI mappings pre-assigned).

### 3.3 Dialog Refactoring

**File**: `Assets/Script/Menu/Common/Dialogs/Onboarding/EliteDrumsBindingDialog.cs`

Changes:
1. Remove `SetFiveLaneDrums()` method and 4L/5L mode switching
2. Remove `_fourLaneDrumKitImage`, `_fiveLaneDrumKitImage` references
3. Override `GetHighlightByName()` to map 13 Elite binding names to highlight indices
4. Override `IsKeyValid()` to accept only Elite-specific keys (not 4L/5L)
5. Update `Initialize()` to set up Elite-specific binding flow
6. Update dialog messages for Elite Drums context

**Binding order** (matches physical kit layout, left-to-right):
1. Kick (foot)
2. Stomp (hi-hat pedal)
3. Splash (hi-hat pedal release)
4. LeftCrash
5. ClosedHiHat
6. SizzleHiHat
7. OpenHiHat
8. Snare
9. Tom1
10. Tom2
11. Tom3
12. Ride
13. RightCrash

### 3.4 Base Class Cleanup

**File**: `Assets/Script/Menu/Common/Dialogs/Onboarding/FriendlyBindingDialog.cs`

Remove the `GameMode.EliteDrums` case from `GetHighlightByName()` (lines 243-267). This was the 4L/5L mapping that EliteDrumsBindingDialog now fully overrides.

### 3.5 Prefab Update

**File**: `Assets/Prefabs/Menu/Common/Dialogs/FriendlyEliteDrumsBindingDialog.prefab`

This is a Unity YAML prefab. Programmatic editing of Unity prefabs is fragile. Options:
- **Option A (recommended)**: Make C# code changes that work with the existing prefab structure. Add serialized fields for highlights and wire them in Unity Editor manually.
- **Option B**: Generate prefab modifications in YAML (risky — GUID references, file IDs, transforms all interconnected).

The C# code should define `[SerializeField]` arrays for highlight images. The actual prefab wiring is manual Unity Editor work documented as a task instruction.

### 3.6 Engine Input Path Verification

The `ConvertMidiDrumsInput()` method in `YargDrumsEngine.cs` currently maps:
- `EliteDrumsAction.Kick` → `DrumsAction.Kick` (works)
- `EliteDrumsAction.FourLane*` → corresponding `DrumsAction.*` (works)
- `EliteDrumsAction.FiveLane*` → corresponding `DrumsAction.*` (works)
- Elite-specific actions (Snare, Tom1, etc.) → **null** (not mapped to DrumsAction)

After Phase 2 changes (removing 4L/5L fallback bindings), the input flow becomes:
1. User hits snare → MIDI note 38 → `EliteDrumsAction.EliteSnare`
2. Engine receives `EliteSnare` in `ConvertMidiDrumsInput()`
3. Current code returns **null** for Elite-specific actions
4. **This means gameplay won't register hits** until `ConvertMidiDrumsInput` is updated

**Action required**: Add Elite→DrumsAction mappings to `ConvertMidiDrumsInput()` for the downchart path. Example: `EliteSnare → Drum1 (red)`, matching how the downchart maps Elite notes to 4-lane.

## 4. Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Removing 4L/5L fallbacks breaks existing downcharted input path | High | Must add Elite→DrumsAction mappings to `ConvertMidiDrumsInput()` simultaneously |
| Prefab can't be edited programmatically | Medium | Document as manual step; C# code should be testable without prefab changes |
| Stomp/Splash ignored by engine (Axis > 0 check with binary input) | Low | Expected — Phase 3 will add native engine support. Binding them now is future-proofing. |
| SizzleHiHat has no standard MIDI note | Low | Default to note 42 (same as closed); users rebind |
| Splash and Stomp share MIDI note 44 | Low | Different physical gestures; binding dialog can bind same note to both actions |

## 5. Success Criteria Mapping

| Criterion | Implementation |
|-----------|---------------|
| Dialog shows all 10 pads + kick + hi-hat pedal | EliteDrumsBindingDialog refactoring (13 bindings in order) |
| Each pad bindable to MIDI note | DrumPadButtonBinding + Minis MidiNoteControl (already works) |
| Bindings persist across sessions | ProfileBindings serialization (already generic) |
| Default MIDI mappings match GM | Default binding configuration or documented in dialog |
| Dialog accessible from profile setup | DialogManager routing (already wired) |
| Bound inputs translate to EliteDrumsAction | BindingCollection.Templates + ConvertMidiDrumsInput update |

## 6. Files Changed

| File | Change Type | Description |
|------|------------|-------------|
| `Assets/Script/Input/Bindings/BindingCollection.Templates.cs` | Modify | Uncomment Elite bindings, remove 4L/5L fallbacks |
| `Assets/Script/Menu/Common/Dialogs/Onboarding/EliteDrumsBindingDialog.cs` | Rewrite | Elite-specific binding flow, remove 4L/5L mode switching |
| `Assets/Script/Menu/Common/Dialogs/Onboarding/FriendlyBindingDialog.cs` | Modify | Remove Elite→4L/5L case from GetHighlightByName() |
| `YARG.Core/YARG.Core/Engine/Drums/Engines/YargDrumsEngine.cs` | Modify | Add Elite→DrumsAction mappings in ConvertMidiDrumsInput() |
| `Assets/Prefabs/Menu/Common/Dialogs/FriendlyEliteDrumsBindingDialog.prefab` | Manual | Rewire highlight images for Elite layout (Unity Editor) |

## 7. Out of Scope

- Native EliteDrumNote engine (Phase 3)
- Hi-hat pedal analog/CC control (post-MVP)
- Visual highway rendering (Phase 3)
- Velocity threshold calibration UI (nice-to-have, not required)
