# Plan 02-02 Summary: Elite Drums Binding Dialog

## Status: Complete

## Files Modified

| File | Change |
|------|--------|
| `Assets/Script/Menu/Common/Dialogs/Onboarding/EliteDrumsBindingDialog.cs` | Full rewrite — removed 4L/5L mode-switching, added 13-pad Elite binding flow |

## Key Decisions

1. **Used YargLogger instead of Debug.Log**: Follows YARG convention for logging
2. **Binding order follows template**: Kick → Stomp → Splash → Snare → HiHats → LeftCrash → Toms → Ride → RightCrash
3. **Null-safe GetHighlightByName**: Returns null with warning for unrecognized binding names, plus null guard for misconfigured highlight array
4. **No mode-switching**: CheckForModeSwitch() is empty — Elite Drums is a single-mode dialog

## Changes Detail

### Removed
- `_proDrumsKit`, `_fiveLaneKit` serialized Image fields
- `_proDrumsHighlights`, `_fiveLaneHighlights` Image arrays
- `_currentMode` field and `CurrentMode` enum
- `SetFiveLaneDrums()` method
- 4L/5L mode-switching logic in `CheckForModeSwitch()`
- `using Cysharp.Threading.Tasks` (unused after rewrite)

### Added
- `_drumKitImage` (single kit image for Elite Drums)
- `_eliteDrumHighlights` (13-element Image array with Header attribute)
- Prefab wiring documentation comment (array index mapping)
- Runtime validation in `Initialize()` (warns if array size != 13)
- 13-binding switch expression in `GetHighlightByName()`
- `using YARG.Core.Logging` for YargLogger

## Binding Name Consistency Matrix

| # | Template Name | Dialog Index | Localization Key | Match |
|---|---|---|---|---|
| 1 | Drums.Kick | [0] | Drums.Kick (shared) | Yes |
| 2 | EliteDrums.Stomp | [1] | EliteDrums.Stomp | Yes |
| 3 | EliteDrums.Splash | [2] | EliteDrums.Splash | Yes |
| 4 | EliteDrums.Snare | [7] | EliteDrums.Snare | Yes |
| 5 | EliteDrums.ClosedHiHat | [4] | EliteDrums.ClosedHiHat | Yes |
| 6 | EliteDrums.SizzleHiHat | [5] | EliteDrums.SizzleHiHat | Yes |
| 7 | EliteDrums.OpenHiHat | [6] | EliteDrums.OpenHiHat | Yes |
| 8 | EliteDrums.LeftCrash | [3] | EliteDrums.LeftCrash | Yes |
| 9 | EliteDrums.Tom1 | [8] | EliteDrums.Tom1 | Yes |
| 10 | EliteDrums.Tom2 | [9] | EliteDrums.Tom2 | Yes |
| 11 | EliteDrums.Tom3 | [10] | EliteDrums.Tom3 | Yes |
| 12 | EliteDrums.Ride | [11] | EliteDrums.Ride | Yes |
| 13 | EliteDrums.RightCrash | [12] | EliteDrums.RightCrash | Yes |

## Verification Results

- EliteDrumsBindingDialog rewritten with 13-action binding flow
- Runtime validation catches array size mismatch
- All 13 binding names match across template, dialog, and localization
- No 4L/5L references remain in EliteDrumsBindingDialog.cs
- Class inherits from FriendlyBindingDialog correctly
- YARG.Core unit tests: 53/57 pass (4 pre-existing SongScanningTests failures)

## Prefab Wiring Instructions

The following must be done manually in Unity Editor:
1. Open `Assets/Prefabs/Menu/Common/Dialogs/FriendlyEliteDrumsBindingDialog.prefab`
2. Select the EliteDrumsBindingDialog component
3. Set `_drumKitImage` to the Elite Drums kit image
4. Set `_eliteDrumHighlights` array to 13 elements in order:
   [0] Kick, [1] Stomp, [2] Splash, [3] LeftCrash,
   [4] ClosedHiHat, [5] SizzleHiHat, [6] OpenHiHat,
   [7] Snare, [8] Tom1, [9] Tom2, [10] Tom3,
   [11] Ride, [12] RightCrash

## Issues

None.
