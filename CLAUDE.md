# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

YARG (Yet Another Rhythm Game) is a free, open-source plastic guitar game built with **Unity 6000.2.12f1** and **C#**. It supports guitar (5-fret), drums, vocals, pro-guitar, and more. Licensed under LGPL v3.0.

## Build & Development

**Prerequisites:** Unity 6000.2.12f1, .NET SDK (not runtime), Git LFS, Blender (for model loading)

**Initial setup:**
```bash
git clone -b dev --recursive <repo-url>.git
git lfs fetch && git lfs checkout
# Open in Unity Hub, then: NuGet > Restore Packages from the menu bar
```

**After pulling updates:**
```bash
git submodule update
git lfs fetch && git lfs checkout
```

**Running tests (YARG.Core only — no Unity-side automated tests):**
```bash
cd YARG.Core
dotnet test YARG.Core.UnitTests/YARG.Core.UnitTests.csproj
```

**Unity scenes (5 total):** PersistentScene, MenuScene, Gameplay, CalibrationScene, ScoreScene

## Architecture

### Two-layer design

- **YARG.Core** (git submodule at `YARG.Core/`): Standalone .NET library containing engine logic, chart parsing, audio interfaces, input abstractions, replay system, and song metadata. Has its own solution (`YARG.Core.sln`) and NUnit test project. This is shared/reusable code with no Unity dependency.
- **Unity project** (`Assets/Script/`): Game-specific code — rendering, UI, input device binding, audio playback (BASS library), menus, and venue visuals. Depends heavily on YARG.Core.

### Key source directories (`Assets/Script/`)

| Directory | Purpose |
|---|---|
| `Gameplay/` | Core game loop, `GameManager` (partial class split across `.cs`, `.Audio.cs`, `.Debug.cs`, `.Loading.cs`), HUD, player visuals |
| `Audio/` | BASS audio library wrapper — stream handles, stem mixing, FX pipeline, microphone input |
| `Input/` | Device handling via PlasticBand-Unity — guitar, drums, keyboard bindings, controller support |
| `Menu/` | All UI menus (main, music library, settings, score screen, calibration) with custom navigator/scheme system |
| `Venue/` | Stage rendering, character animation, camera system, lighting effects |
| `Settings/` | Persistent JSON-based settings with type-safe hierarchy |
| `Integration/` | External hardware: StageKit DMX, sACN lighting, RB3E, DataStream |
| `Replays/` | Replay recording and playback |
| `Scores/` | Score/rating calculation and storage |
| `Song/` | Song/chart loading and management |
| `Playback/` | Song playback and `SongRunner` |
| `Player/` | Player profiles and data |
| `Localization/` | Multi-language support (Crowdin integration) |
| `Themes/` | UI theme/customization system |

### Key patterns

- **MonoBehaviour singletons:** `GameManager`, `SettingsManager`, `MenuManager`, `DialogManager`, `Navigator` — manager pattern is used extensively
- **Partial classes:** Large managers are split across multiple files (e.g., `GameManager.cs` + `GameManager.Audio.cs` + `GameManager.Loading.cs`)
- **Multi-scene setup:** `PersistentScene` holds DontDestroyOnLoad singletons; other scenes load additively
- **Async via UniTask:** Uses Cysharp UniTask, not standard C# async/await Task
- **String optimization:** Uses Cysharp ZString to minimize allocations
- **Animation:** DOTween for UI/gameplay animation tweening
- **Custom UI navigation:** Push/pop scheme-based navigation with action routing (not Unity's default EventSystem navigation)

### Key dependencies (installed via UPM/NuGet, not bundled)

- **PlasticBand-Unity** + **HIDrogen**: Guitar/drum controller support (including Linux HID)
- **BASS audio library**: Core audio engine (free for non-commercial use)
- **Newtonsoft.Json**: Settings and data serialization
- **UniTask / ZString**: Async and string allocation optimization
- **DOTween**: Animation tweening
- **Minis**: MIDI input support

## Code Style

Enforced via `.editorconfig` — IDEs should auto-detect.

- **Indentation:** 4 spaces (not tabs) for C#; tabs for `.asmdef`/`.asmref`/`.inputactions`
- **Charset:** UTF-8 with BOM
- **Line endings:** CRLF
- **Constants:** `ALL_CAPS_WITH_UNDERSCORES`
- **Unity serialized fields (private):** `_camelCase` prefix
- **Public members/properties:** PascalCase
- **var usage:** Use when type is apparent; explicit types for built-in types
- **Braces:** Required on all control structures
- **Expression-bodied members:** Preferred where applicable
- **Trailing commas:** Required in multiline lists
- **Attributes:** Place on separate lines (not same line as declaration)
- **Modifier order:** `public private protected internal file new static abstract virtual sealed readonly override extern unsafe volatile async required`
- **Nullable reference types:** Enabled project-wide (`EnableNullability.props`); toggled per-file with `#nullable enable/disable`

## Contribution Rules

- **All PRs must target the `dev` branch** — PRs to `master` are rejected
- All assets must be original or public domain — no ripped content from other games
- Check license compatibility before adding external libraries
- Unity YAML merge tool is recommended for scene conflict resolution (see README for setup)
- Coordinate via Discord before starting work; check the [YouTrack board](https://yarg.youtrack.cloud/agiles/147-7/current) for planned features
