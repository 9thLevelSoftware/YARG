# YARG Codebase Map

**Generated**: 2026-03-03
**Unity Version**: 6000.2.12f1 | **Core Target**: .NET Standard 2.1 (C# 9)

## Metrics

| Metric | Value |
|---|---|
| Total C# Files | 772 |
| Assets/Script Files | 464 |
| YARG.Core Files | 231 |
| Test Files | 19 |
| Singleton Classes | 18+ |
| Partial Class Groupings | 8+ |
| Shaders | 40 |
| Scenes | 5 primary + 3 authoring |
| TODO/HACK/FIXME | 117 |

## Architecture: Two-Layer Design

### Layer 1: YARG.Core (Platform-Independent)
- **Path**: `YARG.Core/YARG.Core/`
- **Target**: .NET Standard 2.1 (no Unity dependency)
- **Modules**: Chart (65), Song (42), Engine (33), IO (26), Game (22), MoonscraperChartParser (18), Logging (17), Audio (13), Replays (11), Extensions (11), Utility (8)
- **Dependencies**: Melanchall.DryWetMidi.Nativeless, Newtonsoft.Json, ZString

### Layer 2: Unity Project (Game-Specific)
- **Path**: `Assets/Script/`
- **Modules**: Menu (144), Gameplay (110), Settings (46), Helpers (36), Input (29), Venue (22), Integration (16), Audio (15), Persistent (12), Themes (10), Song (6), Scores (6), Localization (4), Playback (3), Playlists (2), Player (2), Replays (1)
- **Key Dependencies**: PlasticBand-Unity, BASS audio, UniTask, DOTween, URP, Cinemachine

## Key Entry Points

| Manager | Pattern | Files |
|---|---|---|
| GameManager | Partial (4 files) | .cs, .Audio.cs, .Debug.cs, .Loading.cs |
| SettingsManager | Partial (2 files) | .cs, .Settings.cs |
| CameraManager | Partial (2 files) | .cs, .PostProcessing.cs (1,478 lines) |
| MenuManager | MonoSingleton | Menu navigation and UI state |
| Navigator | MonoSingleton | Push/pop scheme-based navigation |
| DialogManager | MonoSingleton | Modal dialogs |
| BassAudioManager | Singleton | Audio playback engine |

## Scene Architecture

| Scene | Purpose |
|---|---|
| PersistentScene | DontDestroyOnLoad singletons |
| MenuScene | Main menu, song selection, settings |
| Gameplay | In-game playback and HUD |
| CalibrationScene | Audio/video sync calibration |
| ScoreScene | Score results |

## Risk Areas

1. **No Unity-side automated tests** -- only YARG.Core has NUnit tests (19 files, ~717 cases)
2. **Large files**: CacheHandler.cs (1,651 lines), CameraManager.PostProcessing.cs (1,478 lines)
3. **117 TODO/HACK/FIXME comments** -- most in Gameplay (16), Menu (15), Integration (10)
4. **UI Dialog issues**: Multiple TODOs marked "doesn't work"
5. **Legacy serialization**: 3 binding versions (v0, v1, v2) for backward compatibility
6. **Deprecated**: PerformanceTextScaler marked `[Obsolete]`

## Dependency Graph

```
YARG.Core (Standalone .NET Standard 2.1)
  +-- Melanchall.DryWetMidi.Nativeless 7.0.0
  +-- Newtonsoft.Json 13.0.3
  +-- ZString 2.5.1

Unity Project (depends on YARG.Core)
  +-- PlasticBand-Unity 0.9.0 + HIDrogen 0.5.2 (Input)
  +-- BASS (Audio engine, native)
  +-- UniTask 2.5.3 (Async)
  +-- DOTween (Animation, bundled in Plugins)
  +-- URP 17.2.0 (Rendering)
  +-- Cinemachine 2.10.4 (Camera)
  +-- InputSystem 1.14.2
  +-- Addressables 2.7.4
  +-- Newtonsoft.Json 3.2.1 (UPM)
  +-- Hardware: StageKit DMX, sACN, RB3E, DataStream
```

## Strengths

- Clean separation between YARG.Core and Unity layers
- Comprehensive testing of core parsing/engine logic
- Consistent manager/singleton pattern with clear entry points
- Async-first (UniTask), allocation-conscious (ZString)
- Modular directory structure with clear functional boundaries
