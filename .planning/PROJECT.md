# PROJECT: Elite Drums for YARG

## Vision
Complete the Elite Drums implementation in YARG so that MIDI drum kit players get a first-class, dedicated 10-lane gameplay experience — from song selection through scoring — with full pad dynamics, hi-hat articulation, and a custom visual highway.

## Value Proposition
YARG's existing drum modes (4-lane, Pro, 5-lane) map MIDI kits down to simplified lane layouts. Elite Drums preserves the full 10-pad MIDI drumkit mapping (hi-hat pedal, kick, snare, hi-hat, 2 crashes, 3 toms, ride) with dynamics (accent/ghost), hat states (open/closed/sizzle), and flam support — giving real drummers a 1:1 representation of their kit on screen.

## Target Users
- MIDI drum kit owners who play YARG
- Drummers who want per-pad accuracy beyond 4/5-lane simplification
- The YARG open-source community (contribution to the `dev` branch)

## Requirements

### Must-Have (v1)
1. **SongChart integration**: Wire EliteDrums into `DrumsTracks` enumerable, `Append()`, and `GetDrumsTrack()`
2. **Engine wiring**: Fix `DrumsPlayer.CreateEngine()` to handle `GameMode.EliteDrums` instead of throwing
3. **MIDI binding dialog**: Replace the placeholder 4L/5L binding dialog with a real Elite Drums binding flow for all 10 pads + pedal
4. **10-lane visual highway**: Dedicated track rendering with notes, frets, and beatlines for all 10 drum pads
5. **Hi-hat pedal visualization**: Visual indicator for open/closed/sizzle hat states on the highway
6. **Dynamics visualization**: Accent and ghost note visual differentiation on the highway
7. **Song selection**: Show Elite Drums availability in the music library and allow selection
8. **End-to-end flow**: Select song → bind MIDI kit → play → score screen works completely

### Out of Scope (Post-MVP)
- Snare stem positional mode events (marked post-MVP in existing code)
- Practice mode specific to Elite Drums
- Replay visualization for Elite Drums
- Custom theme support for Elite Drums highway
- Venue/character animation responses to Elite Drums inputs

## Constraints
- **Unity 6000.2.12f1** — must target this version
- **YARG.Core is a git submodule** — changes there are in a separate repo/solution
- **PRs target `dev` branch** — not `master`
- **Existing patterns**: Must follow MonoBehaviour singleton, partial class, UniTask async, and ZString conventions
- **No Unity-side automated tests** — YARG.Core changes can be NUnit tested; Unity-side is manual
- **Code style**: `.editorconfig` enforced (4-space indent, PascalCase publics, `_camelCase` privates, etc.)

## Architecture Notes
- EliteDrumNote class is already fully implemented in YARG.Core (10 pads, dynamics, hat states, flams)
- MIDI preparser and chart loader exist and work
- Downchart generation (Elite → 4-lane/5-lane) is implemented
- Engine input conversion (EliteDrumsAction → DrumsAction) exists in YargDrumsEngine
- The visual highway is the biggest net-new work — must follow the pattern of existing TrackPlayer/FretArray/NoteElement classes
- Binding dialog has a prefab (`FriendlyEliteDrumsBindingDialog.prefab`) that needs updating

## Decisions
- **Execution mode**: Autonomous — plan and execute independently, check in at milestones
- **Planning depth**: Deep Analysis — thorough architecture, edge cases, risk assessment per task
- **Cost profile**: Premium — maximize parallelism and agent specialization
- **Testing**: Real MIDI drum kit available for validation
