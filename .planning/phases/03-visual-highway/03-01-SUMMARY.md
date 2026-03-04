# Plan 03-01 Summary: Visual Infrastructure Foundation

**Status**: Complete
**Date**: 2026-03-03
**Agent**: EngineeringSeniorDeveloper

---

## Files Modified

| File | Change |
|------|--------|
| `Assets/Script/Themes/ThemeManager.cs` | Added `EliteDrums` to `VisualStyle` enum (after `ProKeys`) |
| `Assets/Script/Themes/Authoring/ThemeComponent.cs` | Added `VisualStyle.EliteDrums` case in `GetNoteModelsForVisualStyle()` (reuses `_fourLaneNotes`) and `GetFretModelForVisualStyle()` (reuses `_fourLaneFret`) |
| `Assets/Script/Themes/ThemePreset.Defaults.cs` | Added `VisualStyle.EliteDrums` to Rectangular preset's `SupportedStyles` |
| `Assets/Script/Helpers/VisualStyleHelpers.cs` | Changed `GameMode.EliteDrums` case to return `VisualStyle.EliteDrums` (was fallback to FourLane/FiveLane) |
| `YARG.Core/YARG.Core/Game/Presets/ColorProfile.cs` | Added `[SettingSubSection] public EliteDrumsColors EliteDrums;` field, wired into constructor, `CopyWithNewName()`, `Serialize()`, `Deserialize()` |
| `Assets/Script/Gameplay/Visuals/Fret/FretArray.cs` | Fixed `PlayMissAnimation` bounds check: `index <= _frets.Count` changed to `index < _frets.Count` |

## Files Created

| File | Description |
|------|-------------|
| `YARG.Core/YARG.Core/Game/Presets/ColorProfile.EliteDrums.cs` | `EliteDrumsColors` class implementing `IFretColorProvider` with 8-lane + kick color mapping |

---

## Task Details

### Task 1: VisualStyle.EliteDrums

- Added `EliteDrums` enum value to `VisualStyle` in `ThemeManager.cs`
- Both `ThemeComponent.cs` switch statements now handle `EliteDrums` (no throw path) -- reuses FourLaneDrums models
- Rectangular preset in `ThemePreset.Defaults.cs` includes `EliteDrums` in `SupportedStyles`
- `VisualStyleHelpers.GetVisualStyle(GameMode.EliteDrums, _)` now returns `VisualStyle.EliteDrums` directly

### Task 2: EliteDrumsColors Color Provider

Created `ColorProfile.EliteDrums.cs` with full `IFretColorProvider` + `IBinarySerializable` implementation.

#### IFretColorProvider Index Mapping (1-based, used by FretArray)

| Index | Pad | Color |
|-------|-----|-------|
| 0 | Kick | Orange |
| 1 | HiHat | Yellow |
| 2 | LeftCrash | Blue |
| 3 | Snare | Red |
| 4 | Tom1 | Yellow |
| 5 | Tom2 | Blue |
| 6 | Tom3 | Green |
| 7 | Ride | Blue |
| 8 | RightCrash | Green |

#### GetNoteColor Index Mapping (0-based, used by note elements)

| Index | Pad | Color |
|-------|-----|-------|
| 0 | HiHat | Yellow (cymbal variant) |
| 1 | LeftCrash | Blue (cymbal variant) |
| 2 | Snare | Red |
| 3 | Tom1 | Yellow |
| 4 | Tom2 | Blue |
| 5 | Tom3 | Green |
| 6 | Ride | Blue (cymbal variant) |
| 7 | RightCrash | Green (cymbal variant) |

Methods implemented:
- `GetFretColor(int)`, `GetFretInnerColor(int)`, `GetParticleColor(int)` (IFretColorProvider)
- `GetNoteColor(int)`, `GetNoteStarPowerColor(int)`, `GetActivationNoteColor(int)`
- `GetMetalColor(bool)`, `GetKickFretColor()`, `GetKickNoteColor()`, `GetKickStarPowerColor()`, `GetKickActivationNoteColor()`
- `Miss` color field
- Full `Serialize()`/`Deserialize()`/`Copy()` support

### Task 3: FretArray PlayMissAnimation Bug Fix

- Fixed off-by-one: `index <= _frets.Count` changed to `index < _frets.Count`
- Verified no other off-by-one bugs in FretArray (all other `_frets.Count` uses are correct)
- Verified 8-lane fret scaling: `(2/2) / (8/5) = 0.625` (acceptable, ProDrums uses 0.714 at 7 lanes)

---

## Pre-existing Issues Found

- `ThemeComponent.GetNoteModelsForVisualStyle()` and `GetFretModelForVisualStyle()` do not handle `VisualStyle.SixFretGuitar` -- it falls through to `_ => throw new Exception("Unreachable.")`. This is pre-existing and not related to Elite Drums.
- All build warnings (33 total) are pre-existing -- none from new code.

---

## Verification Results

| Check | Result |
|-------|--------|
| `VisualStyle.EliteDrums` in ThemeComponent `GetNoteModelsForVisualStyle` | PASS |
| `VisualStyle.EliteDrums` in ThemeComponent `GetFretModelForVisualStyle` | PASS |
| `VisualStyle.EliteDrums` in Rectangular SupportedStyles | PASS |
| `VisualStyleHelpers` returns `EliteDrums` for `GameMode.EliteDrums` | PASS |
| `ColorProfile.EliteDrumsColors` implements `IFretColorProvider` | PASS |
| `ColorProfile.EliteDrums` field with `[SettingSubSection]` | PASS |
| `dotnet build YARG.Core/YARG.Core.csproj` | PASS (0 errors, 33 pre-existing warnings) |
| FretArray `PlayMissAnimation` uses `< _frets.Count` | PASS |
| No other off-by-one bugs in FretArray | PASS |
