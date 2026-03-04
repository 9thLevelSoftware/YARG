#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YARG.Core;
using YARG.Core.Audio;
using YARG.Core.Chart;
using YARG.Core.Engine.Drums;
using YARG.Core.Engine.Drums.Engines;
using YARG.Core.Game;
using YARG.Core.Input;
using YARG.Core.Logging;
using YARG.Core.Replays;
using YARG.Gameplay.HUD;
using YARG.Gameplay.Visuals;
using YARG.Helpers.Extensions;
using YARG.Player;
using YARG.Settings;
using YARG.Themes;
using static YARG.Core.Chart.EliteDrumNote;

namespace YARG.Gameplay.Player
{
    public class EliteDrumsPlayer : TrackPlayer<DrumsEngine, DrumNote>
    {
        private const float DRUM_PAD_FLASH_HOLD_DURATION = 0.2f;

        /// <summary>
        /// Number of playable lanes (excludes kick, which is off-lane).
        /// </summary>
        private const int ELITE_LANE_COUNT = 8;

        public DrumsEngineParameters EngineParams { get; private set; } = null!;

        [Header("Elite Drums Specific")]
        [SerializeField]
        private FretArray _fretArray = null!;
        [SerializeField]
        private KickFretFlash _kickFretFlash = null!;

        public override bool ShouldUpdateInputsOnResume => false;

        public override float[] StarMultiplierThresholds { get; protected set; } =
        {
            0.21f, 0.46f, 0.77f, 1.85f, 3.08f, 4.29f
        };

        public override int[] StarScoreThresholds { get; protected set; } = null!;

        // Dual-track management: engine uses DrumNote, visual uses EliteDrumNote
        private InstrumentDifficulty<EliteDrumNote>? _eliteDifficulty;

        // Path B correlation: tick → list of EliteDrumNotes at that tick
        private Dictionary<uint, List<EliteDrumNote>> _eliteNotesByTick = new();

        // Input interception: capture EliteDrumsAction before engine converts to DrumsAction
        private readonly Queue<(EliteDrumsAction action, double time)> _lastEliteActions = new();
        private const int MAX_ELITE_ACTION_QUEUE_SIZE = 32;

        // Replay elite input tracking (mirrors BasePlayer._replayInputIndex for elite actions)
        private int _replayEliteInputIndex;

        // Fret flash tracking
        private Dictionary<int, float> _fretToLastPressedTimeDelta = new();
        private Dictionary<Fret.AnimType, Dictionary<int, float>> _animTypeToFretToLastPressedDelta = new();

        // Cached enum values to avoid per-frame allocation
        private static readonly Fret.AnimType[] _animTypes =
            (Fret.AnimType[]) Enum.GetValues(typeof(Fret.AnimType));

        // Hat pedal tracking
        private HatPedalIndicator? _hatPedalIndicator;

        public override void Initialize(int index, YargPlayer player, SongChart chart, TrackView trackView,
            StemMixer mixer, int? currentHighScore)
        {
            base.Initialize(index, player, chart, trackView, mixer, currentHighScore);
        }

        protected override InstrumentDifficulty<DrumNote> GetNotes(SongChart chart)
        {
            // Store the elite track for visual correlation
            var eliteTrack = chart.EliteDrums;
            if (!eliteTrack.IsEmpty)
            {
                _eliteDifficulty = eliteTrack.GetDifficulty(Player.Profile.CurrentDifficulty);
                BuildCorrelationMap();
            }

            // Engine uses the downcharted DrumNote track
            var track = chart.GetDrumsTrack(Player.Profile.CurrentInstrument).Clone();
            var instrumentDifficulty = track.GetDifficulty(Player.Profile.CurrentDifficulty);
            return instrumentDifficulty;
        }

        /// <summary>
        /// Builds a dictionary mapping tick values to EliteDrumNotes for fast lookup
        /// during note spawning (Path B correlation).
        /// </summary>
        private void BuildCorrelationMap()
        {
            _eliteNotesByTick.Clear();

            if (_eliteDifficulty == null) return;

            foreach (var chord in _eliteDifficulty.Notes)
            {
                foreach (var note in chord.AllNotes)
                {
                    if (!_eliteNotesByTick.TryGetValue(note.Tick, out var list))
                    {
                        list = new List<EliteDrumNote>();
                        _eliteNotesByTick[note.Tick] = list;
                    }
                    list.Add(note);
                }
            }
        }

        protected override DrumsEngine CreateEngine()
        {
            if (!Player.IsReplay)
            {
                EngineParams = Player.EnginePreset.Drums.Create(
                    StarMultiplierThresholds,
                    DrumsEngineParameters.DrumMode.ProFourLane);
            }
            else
            {
                EngineParams = (DrumsEngineParameters) Player.EngineParameterOverride;
            }

            var engine = new YargDrumsEngine(
                NoteTrack, SyncTrack, EngineParams,
                Player.Profile.IsBot,
                Player.Profile.GameMode is GameMode.EliteDrums);

            EngineContainer = GameManager.EngineManager.Register(
                engine, NoteTrack.Instrument, Chart, Player.RockMeterPreset);

            HitWindow = EngineParams.HitWindow;

            engine.OnNoteHit += OnNoteHit;
            engine.OnNoteMissed += OnNoteMissed;
            engine.OnOverhit += OnOverhit;

            engine.OnSoloStart += OnSoloStart;
            engine.OnSoloEnd += OnSoloEnd;

            engine.OnStarPowerPhraseHit += OnStarPowerPhraseHit;
            engine.OnStarPowerPhraseMissed += OnStarPowerPhraseMissed;
            engine.OnStarPowerStatus += OnStarPowerStatus;

            engine.OnCountdownChange += OnCountdownChange;

            engine.OnPadHit += OnPadHit;

            return engine;
        }

        protected override void FinishInitialization()
        {
            StarScoreThresholds = PopulateStarScoreThresholds(StarMultiplierThresholds, Engine.BaseScore);

            // 8-lane fret array with EliteDrumsColors provider
            ColorProfile.IFretColorProvider colors = Player.ColorProfile.EliteDrums;

            _fretArray.FretCount = ELITE_LANE_COUNT;
            _fretArray.Initialize(
                Player.ThemePreset,
                VisualStyle.EliteDrums,
                colors,
                Player.Profile.LeftyFlip,
                false, // splitProTomsAndCymbals — not applicable to Elite
                false, // swapSnareAndHiHat — not applicable to Elite
                false  // swapCrashAndRide — not applicable to Elite
            );

            // Kick fret flash
            _kickFretFlash.Initialize(colors.GetParticleColor(0).ToUnityColor());

            // Initialize drum activation notes
            NoteTrack.SetDrumActivationFlags(Player.Profile.StarPowerActivationType);
            Notes = NoteTrack.Notes;

            // Set up drum fill lead-ups
            SetDrumFillEffects();

            // Initialize hit timestamps
            InitializeHitTimes();
            InitializeAnimTypes();

            // Initialize hat pedal indicator if present
            _hatPedalIndicator = GetComponentInChildren<HatPedalIndicator>();
            if (_hatPedalIndicator != null && _eliteDifficulty != null)
            {
                _hatPedalIndicator.Initialize(_eliteDifficulty);
            }

            base.FinishInitialization();
        }

        protected override void InitializeSpawnedNote(IPoolable poolable, DrumNote note)
        {
            if (poolable is not EliteDrumsNoteElement element)
            {
                YargLogger.LogError("NotePool prefab has wrong NoteElement type. Expected EliteDrumsNoteElement.");
                return;
            }

            element.NoteRef = note;

            // Correlate DrumNote to EliteDrumNote via tick-based lookup (Path B)
            var padInfo = ResolveElitePadInfo(note);
            element.SetElitePadInfo(padInfo);
        }

        /// <summary>
        /// Resolves the ElitePadInfo for a given DrumNote by matching it to its
        /// EliteDrumNote origin via tick-based correlation (Path B).
        /// </summary>
        private ElitePadInfo ResolveElitePadInfo(DrumNote drumNote)
        {
            if (_eliteNotesByTick.TryGetValue(drumNote.Tick, out var eliteNotes))
            {
                foreach (var eliteNote in eliteNotes)
                {
                    // Skip hat pedal notes — they don't produce visible DrumNotes
                    if (eliteNote.Pad == (int) EliteDrumPad.HatPedal) continue;

                    // Match by checking if this elite note's downchart pad would produce this DrumNote's pad
                    if (DoesEliteNoteMatchDrumNote(eliteNote, drumNote))
                    {
                        bool isCymbal = (EliteDrumPad) eliteNote.Pad
                            is EliteDrumPad.HiHat
                            or EliteDrumPad.LeftCrash
                            or EliteDrumPad.Ride
                            or EliteDrumPad.RightCrash;

                        return new ElitePadInfo
                        {
                            Pad = (EliteDrumPad) eliteNote.Pad,
                            Dynamics = eliteNote.Dynamics,
                            HatState = eliteNote.HatState,
                            IsFlam = eliteNote.IsFlam,
                            IsFlamPartner = false,
                            IsCymbal = isCymbal,
                        };
                    }
                }

                // If we reach here, this DrumNote has no direct EliteDrumNote match.
                // It is likely a flam partner (extra DrumNote generated by flam).
                // Find any flam note at this tick to get context for the partner.
                foreach (var eliteNote in eliteNotes)
                {
                    if (eliteNote.IsFlam && eliteNote.Pad != (int) EliteDrumPad.HatPedal)
                    {
                        return new ElitePadInfo
                        {
                            Pad = (EliteDrumPad) eliteNote.Pad,
                            Dynamics = eliteNote.Dynamics,
                            HatState = eliteNote.HatState,
                            IsFlam = false,
                            IsFlamPartner = true,
                            IsCymbal = false,
                        };
                    }
                }
            }

            // Fallback: no elite data available, infer from DrumNote pad
            return ElitePadInfo.FromDrumNote(drumNote);
        }

        /// <summary>
        /// Checks if an EliteDrumNote's downchart output would match the given DrumNote's pad.
        /// Uses the same mapping rules as the downchart generator.
        /// </summary>
        private static bool DoesEliteNoteMatchDrumNote(EliteDrumNote eliteNote, DrumNote drumNote)
        {
            // Get the expected downchart pad for this elite pad
            int? expectedPad = (EliteDrumPad) eliteNote.Pad switch
            {
                EliteDrumPad.Kick       => (int) FourLaneDrumPad.Kick,
                EliteDrumPad.Snare      => (int) FourLaneDrumPad.RedDrum,
                EliteDrumPad.HiHat      => (int) FourLaneDrumPad.YellowCymbal,
                EliteDrumPad.LeftCrash   => (int) FourLaneDrumPad.BlueCymbal,
                EliteDrumPad.Tom1       => (int) FourLaneDrumPad.YellowDrum,
                EliteDrumPad.Tom2       => (int) FourLaneDrumPad.BlueDrum,
                EliteDrumPad.Tom3       => (int) FourLaneDrumPad.GreenDrum,
                EliteDrumPad.Ride       => (int) FourLaneDrumPad.BlueCymbal,
                EliteDrumPad.RightCrash  => (int) FourLaneDrumPad.GreenCymbal,
                _ => null
            };

            // Channel flags can override the default mapping, but for correlation
            // we also accept the default mapping as close enough
            return expectedPad.HasValue && expectedPad.Value == drumNote.Pad;
        }

        private void SetDrumFillEffects()
        {
            int checkpoint = 0;
            var pairedFillIndexes = new HashSet<int>();

            // Find activation gems
            foreach (var chord in Notes)
            {
                DrumNote rightmostNote = chord.ParentOrSelf;
                bool foundStarpower = false;

                // Check for SP activation note
                foreach (var note in chord.AllNotes)
                {
                    if (note.IsStarPowerActivator)
                    {
                        if (note.Pad > rightmostNote.Pad)
                        {
                            rightmostNote = note;
                        }
                        foundStarpower = true;
                    }
                }

                if (!foundStarpower)
                {
                    continue;
                }

                // Elite drums: pad index maps directly to lane (no split view conversion needed)
                int fillLane = rightmostNote.Pad;

                int candidateIndex = -1;

                // Find the drum fill immediately before this note
                for (var i = checkpoint; i < _trackEffects.Count; i++)
                {
                    if (_trackEffects[i].EffectType != TrackEffectType.DrumFill)
                    {
                        continue;
                    }

                    var effect = _trackEffects[i];

                    if (effect.TimeEnd <= chord.Time)
                    {
                        candidateIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (candidateIndex != -1)
                {
                    _trackEffects[candidateIndex].FillLane = fillLane;
                    _trackEffects[candidateIndex].TotalLanes = _fretArray.FretCount;
                    pairedFillIndexes.Add(candidateIndex);
                    checkpoint = candidateIndex;

                    // Also make sure that the fill effect actually extends to the note
                    if (_trackEffects[candidateIndex].TimeEnd < chord.TimeEnd)
                    {
                        TrackEffect.ExtendEffect(candidateIndex, chord.TimeEnd, NoteSpeed, ref _trackEffects);
                    }
                }
            }

            // Remove fills that are not paired with a note
            for (var i = _trackEffects.Count - 1; i >= 0; i--)
            {
                if (_trackEffects[i].EffectType == TrackEffectType.DrumFill && !pairedFillIndexes.Contains(i))
                {
                    _trackEffects.RemoveAt(i);
                }
            }
        }

        public override void SetStemMuteState(bool muted)
        {
            if (IsStemMuted != muted)
            {
                GameManager.ChangeStemMuteState(SongStem.Drums, muted);
                IsStemMuted = muted;
            }
        }

        public override void SetStarPowerFX(bool active)
        {
            GameManager.ChangeStemReverbState(SongStem.Drums, active);
        }

        protected override void ResetVisuals()
        {
            base.ResetVisuals();

            _fretArray.ResetAll();
            _hatPedalIndicator?.Reset();
            _replayEliteInputIndex = 0;
        }

        protected override void OnNoteHit(int index, DrumNote note)
        {
            base.OnNoteHit(index, note);

            (NotePool.GetByKey(note) as EliteDrumsNoteElement)?.HitNote();

            // Fret animation for the hit note's elite lane
            AnimateEliteFret(note);
        }

        protected override void OnNoteMissed(int index, DrumNote note)
        {
            base.OnNoteMissed(index, note);

            (NotePool.GetByKey(note) as EliteDrumsNoteElement)?.MissNote();
        }

        protected override void OnStarPowerPhraseHit()
        {
            base.OnStarPowerPhraseHit();

            foreach (var note in NotePool.AllSpawned)
            {
                (note as EliteDrumsNoteElement)?.OnStarPowerUpdated();
            }
        }

        protected override void OnStarPowerPhraseMissed()
        {
            foreach (var note in NotePool.AllSpawned)
            {
                (note as EliteDrumsNoteElement)?.OnStarPowerUpdated();
            }
        }

        protected override void OnStarPowerStatus(bool status)
        {
            base.OnStarPowerStatus(status);

            foreach (var note in NotePool.AllSpawned)
            {
                (note as EliteDrumsNoteElement)?.OnStarPowerUpdated();
            }
        }

        protected override bool InterceptInput(ref GameInput input)
        {
            // Capture the EliteDrumsAction before engine converts it to DrumsAction.
            // This preserves the full Elite lane identity for fret animations.
            var eliteAction = input.GetAction<EliteDrumsAction>();

            if (input.Axis > 0)
            {
                _lastEliteActions.Enqueue((eliteAction, input.Time));

                // Prevent unbounded queue growth
                while (_lastEliteActions.Count > MAX_ELITE_ACTION_QUEUE_SIZE)
                {
                    _lastEliteActions.Dequeue();
                }
            }

            // Do NOT consume the input — let it pass through to the engine
            return false;
        }

        protected override void UpdateInputs(double time)
        {
            // For replay playback, InterceptInput is not called.
            // We need to populate _lastEliteActions from replay inputs.
            if (Player.IsReplay && GameManager.ReplayInfo != null)
            {
                // Replay inputs are processed in base.UpdateInputs,
                // but since InterceptInput isn't called, we extract elite actions here.
                // We peek at upcoming replay inputs and pre-populate the queue.
                var replayInputs = ReplayInputs;
                for (int i = _replayEliteInputIndex; i < replayInputs.Count; i++)
                {
                    var input = replayInputs[i];
                    if (input.Time > time + InputCalibration) break;
                    if (input.Axis > 0)
                    {
                        var eliteAction = input.GetAction<EliteDrumsAction>();
                        _lastEliteActions.Enqueue((eliteAction, input.Time));
                    }
                    _replayEliteInputIndex = i + 1;
                }

                while (_lastEliteActions.Count > MAX_ELITE_ACTION_QUEUE_SIZE)
                {
                    _lastEliteActions.Dequeue();
                }
            }

            base.UpdateInputs(time);
        }

        private void OnPadHit(DrumsAction action, bool wasNoteHit, bool wasNoteHitCorrectly,
            DrumNoteType type, float velocity)
        {
            if (action is not DrumsAction.Kick)
            {
                // Play fret flash animation
                int fret = GetEliteFretForDrumsAction(action);
                if (fret >= 0)
                {
                    Fret.AnimType animType = Fret.AnimType.CorrectNormal;

                    if (type == DrumNoteType.Accent)
                    {
                        animType = wasNoteHitCorrectly ? Fret.AnimType.CorrectHard : Fret.AnimType.TooHard;
                    }
                    else if (type == DrumNoteType.Ghost)
                    {
                        animType = wasNoteHitCorrectly ? Fret.AnimType.CorrectSoft : Fret.AnimType.TooSoft;
                    }

                    // Try to find a more specific elite fret from the intercepted action queue
                    int eliteFret = ConsumeEliteActionForDrumsAction(action);
                    if (eliteFret >= 0)
                    {
                        fret = eliteFret;
                    }

                    ZeroOutHitTime(fret, animType);
                }
            }

            if (wasNoteHit)
            {
                return;
            }

            if (action is not DrumsAction.Kick)
            {
                int fret = GetEliteFretForDrumsAction(action);
                int eliteFret = ConsumeEliteActionForDrumsAction(action);
                if (eliteFret >= 0) fret = eliteFret;

                if (fret >= 0)
                {
                    _fretArray.PlayMissAnimation(fret);
                }
            }
            else
            {
                _fretArray.PlayKickFretAnimation();
                _kickFretFlash.PlayHitAnimation();
                CameraPositioner.Bounce();
            }
        }

        /// <summary>
        /// Consumes the most recent EliteDrumsAction from the interception queue
        /// that maps to the given DrumsAction, and returns its Elite fret index (0-7).
        /// Returns -1 if no matching action found.
        /// </summary>
        private int ConsumeEliteActionForDrumsAction(DrumsAction drumsAction)
        {
            // Dequeue stale entries and find the first matching one
            var tempList = new List<(EliteDrumsAction action, double time)>();
            int result = -1;

            while (_lastEliteActions.Count > 0)
            {
                var (eliteAction, time) = _lastEliteActions.Dequeue();

                if (result < 0 && DoesEliteActionMapToDrumsAction(eliteAction, drumsAction))
                {
                    result = GetEliteFretForEliteAction(eliteAction);
                    // Consumed — don't re-enqueue
                }
                else
                {
                    tempList.Add((eliteAction, time));
                }
            }

            // Re-enqueue unconsumed entries
            foreach (var entry in tempList)
            {
                _lastEliteActions.Enqueue(entry);
            }

            return result;
        }

        /// <summary>
        /// Checks if a specific EliteDrumsAction would map to the given DrumsAction
        /// through the engine's conversion.
        /// </summary>
        private static bool DoesEliteActionMapToDrumsAction(EliteDrumsAction eliteAction, DrumsAction drumsAction)
        {
            DrumsAction? mapped = eliteAction switch
            {
                EliteDrumsAction.Kick             => DrumsAction.Kick,
                EliteDrumsAction.EliteSnare       => DrumsAction.Drum1,
                EliteDrumsAction.EliteClosedHiHat => DrumsAction.Cymbal1,
                EliteDrumsAction.EliteSizzleHiHat => DrumsAction.Cymbal1,
                EliteDrumsAction.EliteOpenHiHat   => DrumsAction.Cymbal1,
                EliteDrumsAction.EliteLeftCrash   => DrumsAction.Cymbal2,
                EliteDrumsAction.EliteTom1        => DrumsAction.Drum2,
                EliteDrumsAction.EliteTom2        => DrumsAction.Drum3,
                EliteDrumsAction.EliteTom3        => DrumsAction.Drum4,
                EliteDrumsAction.EliteRide        => DrumsAction.Cymbal2,
                EliteDrumsAction.EliteRightCrash  => DrumsAction.Cymbal3,
                _ => null
            };

            return mapped.HasValue && mapped.Value == drumsAction;
        }

        /// <summary>
        /// Maps an EliteDrumsAction to an Elite fret index (0-based, 0-7).
        /// </summary>
        private static int GetEliteFretForEliteAction(EliteDrumsAction action)
        {
            return action switch
            {
                // Fret indices match lane layout: HH=0, LCrash=1, Snare=2, T1=3, T2=4, T3=5, Ride=6, RCrash=7
                EliteDrumsAction.EliteClosedHiHat => 0,
                EliteDrumsAction.EliteSizzleHiHat => 0,
                EliteDrumsAction.EliteOpenHiHat   => 0,
                EliteDrumsAction.EliteLeftCrash   => 1,
                EliteDrumsAction.EliteSnare       => 2,
                EliteDrumsAction.EliteTom1        => 3,
                EliteDrumsAction.EliteTom2        => 4,
                EliteDrumsAction.EliteTom3        => 5,
                EliteDrumsAction.EliteRide        => 6,
                EliteDrumsAction.EliteRightCrash  => 7,
                _ => -1
            };
        }

        /// <summary>
        /// Fallback: maps a DrumsAction to the best-guess Elite fret index.
        /// Used when no intercepted EliteDrumsAction is available.
        /// </summary>
        private static int GetEliteFretForDrumsAction(DrumsAction action)
        {
            return action switch
            {
                // Multiple elite pads map to the same DrumsAction.
                // Without elite action data, pick the most common lane.
                DrumsAction.Drum1   => 2, // Red → Snare
                DrumsAction.Cymbal1 => 0, // Yellow Cymbal → HiHat
                DrumsAction.Drum2   => 3, // Yellow Tom → Tom1
                DrumsAction.Cymbal2 => 1, // Blue Cymbal → LeftCrash (or Ride)
                DrumsAction.Drum3   => 4, // Blue Tom → Tom2
                DrumsAction.Cymbal3 => 7, // Green Cymbal → RightCrash
                DrumsAction.Drum4   => 5, // Green Tom → Tom3
                _ => -1
            };
        }

        /// <summary>
        /// Animates the correct Elite fret for a hit DrumNote by looking up its
        /// Elite pad info from the correlation map.
        /// </summary>
        private void AnimateEliteFret(DrumNote note)
        {
            if (note.Pad == (int) FourLaneDrumPad.Kick)
            {
                _kickFretFlash.PlayHitAnimation();
                _fretArray.PlayKickFretAnimation();
                CameraPositioner.Bounce();
                return;
            }

            var padInfo = ResolveElitePadInfo(note);
            int fret = ElitePadInfo.GetFretIndex(padInfo.Pad);
            if (fret < 0) return;

            if (padInfo.IsCymbal)
            {
                _fretArray.PlayCymbalHitAnimation(fret);
            }
            else
            {
                _fretArray.PlayHitAnimation(fret);
            }
        }

        protected override void UpdateVisuals(double visualTime)
        {
            base.UpdateVisuals(visualTime);
            UpdateHitTimes();
            UpdateAnimTimes();
            UpdateFretArray();

            // Update hat pedal indicator
            if (_hatPedalIndicator != null)
            {
                _hatPedalIndicator.UpdateIndicator(visualTime);
            }
        }

        public override (ReplayFrame Frame, ReplayStats Stats) ConstructReplayData()
        {
            var frame = new ReplayFrame(Player.Profile, EngineParams, Engine.EngineStats, ReplayInputs.ToArray());
            return (frame, Engine.EngineStats.ConstructReplayStats(Player.Profile.Name));
        }

        #region Fret Flash Tracking

        private void InitializeHitTimes()
        {
            for (int fret = 0; fret < _fretArray.FretCount; fret++)
            {
                _fretToLastPressedTimeDelta[fret] = float.MaxValue;
            }
        }

        private void InitializeAnimTypes()
        {
            foreach (var animType in _animTypes)
            {
                _animTypeToFretToLastPressedDelta[animType] = new Dictionary<int, float>();

                for (int fret = 0; fret < _fretArray.FretCount; fret++)
                {
                    _animTypeToFretToLastPressedDelta[animType][fret] = float.MaxValue;
                }
            }
        }

        private void ZeroOutHitTime(int fret, Fret.AnimType animType)
        {
            if (fret < 0 || fret >= _fretArray.FretCount) return;
            _fretToLastPressedTimeDelta[fret] = 0f;
            _animTypeToFretToLastPressedDelta[animType][fret] = 0f;
        }

        private void UpdateHitTimes()
        {
            for (int fret = 0; fret < _fretArray.FretCount; fret++)
            {
                _fretToLastPressedTimeDelta[fret] += Time.deltaTime;
            }
        }

        private void UpdateAnimTimes()
        {
            foreach (var animType in _animTypes)
            {
                for (int fret = 0; fret < _fretArray.FretCount; fret++)
                {
                    _animTypeToFretToLastPressedDelta[animType][fret] += Time.deltaTime;
                }
            }
        }

        private void UpdateFretArray()
        {
            for (int fret = 0; fret < _fretArray.FretCount; fret++)
            {
                _fretArray.SetPressedDrum(fret, _fretToLastPressedTimeDelta[fret] < DRUM_PAD_FLASH_HOLD_DURATION, GetAnimType(fret));
                _fretArray.UpdateAccentColorState(fret,
                    _animTypeToFretToLastPressedDelta[Fret.AnimType.CorrectHard][fret] <
                    DRUM_PAD_FLASH_HOLD_DURATION);
            }
        }

        private Fret.AnimType GetAnimType(int fret)
        {
            if (_animTypeToFretToLastPressedDelta[Fret.AnimType.CorrectNormal][fret] < DRUM_PAD_FLASH_HOLD_DURATION)
            {
                return Fret.AnimType.CorrectNormal;
            }

            if (_animTypeToFretToLastPressedDelta[Fret.AnimType.CorrectHard][fret] < DRUM_PAD_FLASH_HOLD_DURATION)
            {
                return Fret.AnimType.CorrectHard;
            }

            if (_animTypeToFretToLastPressedDelta[Fret.AnimType.CorrectSoft][fret] < DRUM_PAD_FLASH_HOLD_DURATION)
            {
                return Fret.AnimType.CorrectSoft;
            }

            return Fret.AnimType.CorrectNormal;
        }

        #endregion
    }

    /// <summary>
    /// Contains resolved Elite Drums pad information for a single note.
    /// Populated during note spawning by correlating DrumNote to EliteDrumNote.
    /// </summary>
    public struct ElitePadInfo
    {
        public EliteDrumPad Pad;
        public DrumNoteType Dynamics;
        public EliteDrumsHatState HatState;
        public bool IsFlam;
        public bool IsFlamPartner;
        public bool IsCymbal;

        /// <summary>
        /// Maps an EliteDrumPad to a 0-based fret index for the 8-lane layout.
        /// HH=0, LCrash=1, Snare=2, T1=3, T2=4, T3=5, Ride=6, RCrash=7.
        /// Returns -1 for Kick and HatPedal (not lane notes).
        /// </summary>
        public static int GetFretIndex(EliteDrumPad pad)
        {
            return pad switch
            {
                EliteDrumPad.HiHat      => 0,
                EliteDrumPad.LeftCrash   => 1,
                EliteDrumPad.Snare      => 2,
                EliteDrumPad.Tom1       => 3,
                EliteDrumPad.Tom2       => 4,
                EliteDrumPad.Tom3       => 5,
                EliteDrumPad.Ride       => 6,
                EliteDrumPad.RightCrash  => 7,
                _ => -1
            };
        }

        /// <summary>
        /// Maps an EliteDrumPad to a 1-based lane index for GetElementX(lane, 8).
        /// HH=1, LCrash=2, Snare=3, T1=4, T2=5, T3=6, Ride=7, RCrash=8.
        /// Returns 0 for Kick (positioned at center).
        /// Returns -1 for HatPedal (not a note lane).
        /// </summary>
        public static int GetLaneIndex(EliteDrumPad pad)
        {
            return pad switch
            {
                EliteDrumPad.HiHat      => 1,
                EliteDrumPad.LeftCrash   => 2,
                EliteDrumPad.Snare      => 3,
                EliteDrumPad.Tom1       => 4,
                EliteDrumPad.Tom2       => 5,
                EliteDrumPad.Tom3       => 6,
                EliteDrumPad.Ride       => 7,
                EliteDrumPad.RightCrash  => 8,
                EliteDrumPad.Kick       => 0,
                _ => -1
            };
        }

        /// <summary>
        /// Maps an EliteDrumPad to a 0-based color index for GetNoteColor.
        /// HH=0, LCrash=1, Snare=2, T1=3, T2=4, T3=5, Ride=6, RCrash=7.
        /// </summary>
        public static int GetColorIndex(EliteDrumPad pad)
        {
            return pad switch
            {
                EliteDrumPad.HiHat      => 0,
                EliteDrumPad.LeftCrash   => 1,
                EliteDrumPad.Snare      => 2,
                EliteDrumPad.Tom1       => 3,
                EliteDrumPad.Tom2       => 4,
                EliteDrumPad.Tom3       => 5,
                EliteDrumPad.Ride       => 6,
                EliteDrumPad.RightCrash  => 7,
                _ => -1
            };
        }

        /// <summary>
        /// Fallback constructor when no EliteDrumNote correlation is available.
        /// Infers Elite pad from the DrumNote's downcharted pad value.
        /// </summary>
        public static ElitePadInfo FromDrumNote(DrumNote drumNote)
        {
            var pad = (FourLaneDrumPad) drumNote.Pad switch
            {
                FourLaneDrumPad.Kick         => EliteDrumPad.Kick,
                FourLaneDrumPad.RedDrum      => EliteDrumPad.Snare,
                FourLaneDrumPad.YellowDrum   => EliteDrumPad.Tom1,
                FourLaneDrumPad.BlueDrum     => EliteDrumPad.Tom2,
                FourLaneDrumPad.GreenDrum    => EliteDrumPad.Tom3,
                FourLaneDrumPad.YellowCymbal => EliteDrumPad.HiHat,
                FourLaneDrumPad.BlueCymbal   => EliteDrumPad.LeftCrash,
                FourLaneDrumPad.GreenCymbal  => EliteDrumPad.RightCrash,
                _ => EliteDrumPad.Snare
            };

            bool isCymbal = pad is EliteDrumPad.HiHat or EliteDrumPad.LeftCrash
                or EliteDrumPad.Ride or EliteDrumPad.RightCrash;

            return new ElitePadInfo
            {
                Pad = pad,
                Dynamics = drumNote.IsAccent ? DrumNoteType.Accent
                    : drumNote.IsGhost ? DrumNoteType.Ghost
                    : DrumNoteType.Neutral,
                HatState = EliteDrumsHatState.Indifferent,
                IsFlam = false,
                IsFlamPartner = false,
                IsCymbal = isCymbal,
            };
        }
    }
}
