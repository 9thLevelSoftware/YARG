# Phase 2 Context: Input & MIDI Binding

## Phase Goal

Replace the placeholder Elite Drums binding dialog (which delegates to 4-lane/5-lane bindings) with a proper 13-action binding flow for all Elite Drums pads. Wire up default General MIDI drum note mappings. After this phase, a MIDI drum kit can be fully bound to all Elite Drums actions with bindings that persist across sessions.

## Requirements

From ROADMAP.md Phase 2 success criteria:
- Binding dialog shows all 10 pads + kick + hi-hat pedal with visual layout
- Each pad can be bound to a MIDI note from a real drum kit
- Bindings persist across sessions (saved to profile)
- Default MIDI mappings match General MIDI drum note numbers
- Dialog accessible from player profile setup when EliteDrums game mode selected
- Bound inputs correctly translate to EliteDrumsAction values

## Existing Assets

### Already complete (Phase 1):
- `EliteDrumsAction` enum with 13 actions (Kick, Stomp, Splash, Snare, ClosedHiHat, SizzleHiHat, OpenHiHat, LeftCrash, Tom1-3, Ride, RightCrash)
- `DialogManager.cs` routing for `GameMode.EliteDrums` → correct prefab
- Localization keys in `en-US.json` for all Elite Drums binding names
- MIDI input pipeline: Minis → MidiNoteControl → DrumPadButtonBinding → GameInput
- Profile serialization (generic per GameMode)
- GameModeExtensions.cs settings for EliteDrums
- Downchart engine with `ConvertMidiDrumsInput()` (needs Elite action mappings)

### Placeholder code to replace:
- `BindingCollection.Templates.cs:CreateEliteDrumsBindings()` — Elite bindings commented out, 4L/5L fallbacks active
- `EliteDrumsBindingDialog.cs` — 4L↔5L mode-switching instead of Elite binding
- `FriendlyBindingDialog.cs` — hardcoded Elite→4L/5L mapping in GetHighlightByName()
- `FriendlyEliteDrumsBindingDialog.prefab` — 4L/5L drum kit visuals

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| Remove 4L/5L fallback bindings entirely | No saved Elite profiles exist (mode was never functional); fallbacks add confusion |
| Use DrumPadButtonBinding for all 13 actions | MIDI pedals send velocity via Minis; "no velocity" for Stomp/Splash means engine ignores it, not that input lacks it |
| Must add Elite→DrumsAction mappings simultaneously | Removing 4L/5L fallbacks without engine mappings breaks gameplay |
| Prefab changes are manual Unity Editor work | Unity YAML prefabs are fragile to edit programmatically |
| Stomp/Splash binding is future-proofing | Engine ignores them in Phase 1 downchart arch; Phase 3 will consume them |

## General MIDI Drum Note Defaults

| Action | GM Note | GM Name |
|--------|---------|---------|
| Kick | 36 | Bass Drum 1 |
| Stomp | 44 | Pedal Hi-Hat |
| Splash | 44 | Pedal Hi-Hat |
| Snare | 38 | Acoustic Snare |
| ClosedHiHat | 42 | Closed Hi-Hat |
| SizzleHiHat | 42 | Closed Hi-Hat (no standard) |
| OpenHiHat | 46 | Open Hi-Hat |
| LeftCrash | 49 | Crash Cymbal 1 |
| Tom1 | 50 | High Tom |
| Tom2 | 48 | Mid Tom |
| Tom3 | 43 | Low Floor Tom |
| Ride | 51 | Ride Cymbal 1 |
| RightCrash | 57 | Crash Cymbal 2 |

## Plan Structure

| Plan | Wave | Description |
|------|------|-------------|
| 02-01 | 1 | Elite Drums Binding Infrastructure |
| 02-02 | 2 | Elite Drums Binding Dialog |

## Spec Document

Full technical spec at: `.planning/specs/02-input-midi-binding-spec.md`
