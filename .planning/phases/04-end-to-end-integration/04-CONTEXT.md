# Phase 4 Context: End-to-End Integration & Polish

## Phase Goal

Complete the flow from song selection through score screen. Everything works together -- select an Elite Drums chart, bind your kit, play through a song, see your score. Fix any remaining integration gaps.

## Requirements

From ROADMAP.md Phase 4 success criteria:
- Music library shows Elite Drums difficulty/availability per song
- Can select Elite Drums as game mode and pick a song with Elite Drums chart
- Full gameplay loop: song loads -> notes render -> inputs register -> hits/misses scored
- Star power activation works
- Solo sections display correctly
- Score screen shows correct stats after song completion
- Combo meter and multiplier work correctly
- No crashes or exceptions through the entire flow
- Tested on real MIDI drum kit end-to-end

## Existing Assets (from Phases 1-3)

### Phase 1 Output
- `SongChart.EliteDrums` track loads and downcharts to 4L/5L/Pro correctly
- `DrumsPlayer.CreateEngine()` handles `GameMode.EliteDrums` (creates DrumsEngine with `isEliteDrums: true`)
- Engine processes downcharted DrumNote track for scoring
- Unit tests pass (53/57, 4 pre-existing)

### Phase 2 Output
- 13 Elite Drums bindings active in `BindingCollection.Templates.cs`
- `ConvertMidiDrumsInput()` maps all 13 Elite->DrumsAction (verified against downchart)
- `EliteDrumsBindingDialog.cs` rewritten for 13-pad binding flow
- Binding dialog prefab has art assets wired

### Phase 3 Output
- `EliteDrumsPlayer.cs` — full 10-lane track player with supplemental EliteDrumNote lookup
- `EliteDrumsNoteElement.cs` — note rendering with dynamics, hat state, flam indicators
- `EliteDrumsFretArray.cs` / `EliteDrumsFretColorProvider.cs` — 8-lane fret display
- `ColorProfile.EliteDrums.cs` — full color provider
- `VisualStyle.EliteDrums` and `ThemeComponent` cases wired
- Prefab integration and GameManager wiring complete

## Integration Analysis (Pre-Planning)

### Already Wired (no changes needed)
| Component | File | Status |
|-----------|------|--------|
| Score screen | `ScoreScreenMenu.cs:194` | EliteDrums case present |
| Profile sidebar | `ProfileSidebar.cs:446` | EliteDrums case present |
| Theme/visual style | `VisualStyleHelpers.cs:21` | EliteDrums mapped |
| Dialog routing | `DialogManager.cs:128` | EliteDrums routed |
| Game loading/prefab | `GameManager.Loading.cs:417` | EliteDrums prefab assigned |
| Difficulty rings | `Sidebar.cs:244` | EliteDrums ring present |
| Localization | `en-US.json` | All strings present |
| Binding templates | `BindingCollection.Templates.cs` | 13 bindings active |

### Gaps Found (changes needed)
| Gap | File | Lines | Impact |
|-----|------|-------|--------|
| `Instrument.EliteDrums` commented out in `PossibleInstrumentsForSong()` | `YARG.Core/InstrumentEnums.cs` | 240, 245 | Cannot select Elite Drums instrument in difficulty menu |
| Missing `case GameMode.EliteDrums:` in `SpawnEnginePresetIcons()` | `ModifierIcon.cs` | 75-76 | No modifier icons on score screen for Elite Drums |

### Not Needed (consistent with codebase)
| Item | Reason |
|------|--------|
| EliteDrumsInputViewer | No DrumsInputViewer exists for any drum mode. InputViewer is optional (null-checked). |
| CharterEliteDrums credit | Commented alongside ProGuitar/ProBass as "for future use" |

## Plan Structure

| Plan | Wave | Description |
|------|------|-------------|
| 04-01 | 1 | Song Selection & Integration Completion |
| 04-02 | 2 | End-to-End Verification & Polish |
