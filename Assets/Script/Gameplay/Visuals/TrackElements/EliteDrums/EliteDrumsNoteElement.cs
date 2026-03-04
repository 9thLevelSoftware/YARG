#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using YARG.Core.Chart;
using YARG.Core.Engine;
using YARG.Core.Engine.Drums;
using YARG.Gameplay.Player;
using YARG.Helpers.Extensions;
using YARG.Themes;
using static YARG.Core.Chart.EliteDrumNote;
using Color = System.Drawing.Color;

namespace YARG.Gameplay.Visuals
{
    public sealed class EliteDrumsNoteElement : NoteElement<DrumNote, EliteDrumsPlayer>, IThemeNoteCreator
    {
        private const int ELITE_LANE_COUNT = 8;

        protected enum NoteType
        {
            Normal       = 0,
            Cymbal       = 1,
            Kick         = 2,
            Accent       = 3,
            Ghost        = 4,
            CymbalAccent = 5,
            CymbalGhost  = 6,

            Count
        }

        /// <summary>
        /// The resolved Elite Drums pad information for this note.
        /// Set by <see cref="EliteDrumsPlayer.InitializeSpawnedNote"/> during note spawning.
        /// </summary>
        private ElitePadInfo _padInfo;

        [Header("Elite Drums Indicators")]
        [SerializeField]
        private GameObject? _openHatIndicator;
        [SerializeField]
        private GameObject? _flamIndicator;

        /// <summary>
        /// Sets the Elite pad information for this note element.
        /// Called by the player during note initialization.
        /// </summary>
        public void SetElitePadInfo(ElitePadInfo padInfo)
        {
            _padInfo = padInfo;
        }

        public override void SetThemeModels(
            Dictionary<ThemeNoteType, GameObject> models,
            Dictionary<ThemeNoteType, GameObject> starpowerModels)
        {
            CreateNoteGroupArrays((int) NoteType.Count);

            AssignNoteGroup(models, starpowerModels, (int) NoteType.Normal,       ThemeNoteType.Normal);
            AssignNoteGroup(models, starpowerModels, (int) NoteType.Cymbal,       ThemeNoteType.Cymbal);
            AssignNoteGroup(models, starpowerModels, (int) NoteType.Kick,         ThemeNoteType.Kick);
            AssignNoteGroup(models, starpowerModels, (int) NoteType.Accent,       ThemeNoteType.Accent);
            AssignNoteGroup(models, starpowerModels, (int) NoteType.Ghost,        ThemeNoteType.Ghost);
            AssignNoteGroup(models, starpowerModels, (int) NoteType.CymbalAccent, ThemeNoteType.CymbalAccent);
            AssignNoteGroup(models, starpowerModels, (int) NoteType.CymbalGhost,  ThemeNoteType.CymbalGhost);
        }

        protected override void InitializeElement()
        {
            base.InitializeElement();

            // Hide indicators by default
            if (_openHatIndicator != null) _openHatIndicator.SetActive(false);
            if (_flamIndicator != null) _flamIndicator.SetActive(false);

            // If this is a flam partner, hide the entire element
            if (_padInfo.IsFlamPartner)
            {
                HideElement();
                return;
            }

            var noteGroups = IsStarPowerVisible ? StarPowerNoteGroups : NoteGroups;

            if (_padInfo.Pad != EliteDrumPad.Kick)
            {
                // Non-kick notes: position in 8-lane layout using 1-based lane index
                int lane = ElitePadInfo.GetLaneIndex(_padInfo.Pad);

                transform.localPosition = new Vector3(
                    GetElementX(lane, ELITE_LANE_COUNT), 0f, 0f) * LeftyFlipMultiplier;

                // Select note group based on dynamics and cymbal status
                NoteGroup = noteGroups[GetNoteGroup(_padInfo.IsCymbal)];

                // Hi-hat open indicator: show when pad is HiHat and hat state is Open
                if (_padInfo.Pad == EliteDrumPad.HiHat &&
                    _padInfo.HatState == EliteDrumsHatState.Open &&
                    _openHatIndicator != null)
                {
                    _openHatIndicator.SetActive(true);
                }

                // Flam indicator: show when this note has a flam
                if (_padInfo.IsFlam && _flamIndicator != null)
                {
                    _flamIndicator.SetActive(true);
                }
            }
            else
            {
                // Kick notes: center position
                transform.localPosition = Vector3.zero;
                NoteGroup = noteGroups[(int) NoteType.Kick];
            }

            // Show and initialize the selected note group
            NoteGroup.SetActive(true);
            NoteGroup.Initialize();

            // Set note color
            UpdateColor();
        }

        protected override void UpdateElement()
        {
            // Update color for activation note pulse effect
            UpdateColor();
        }

        public override void HitNote()
        {
            base.HitNote();

            ParentPool.Return(this);
        }

        protected override bool CalcStarPowerVisible()
        {
            if (!NoteRef.IsStarPower)
            {
                return false;
            }
            return !(((DrumsEngineParameters) Player.BaseParameters).NoStarPowerOverlap &&
                     Player.BaseStats.IsStarPowerActive);
        }

        protected override void HideElement()
        {
            HideNotes();
        }

        public override void OnStarPowerUpdated()
        {
            base.OnStarPowerUpdated();

            UpdateColor();
        }

        private int GetNoteGroup(bool isCymbal)
        {
            if (_padInfo.Dynamics == DrumNoteType.Accent)
            {
                return (int) (isCymbal ? NoteType.CymbalAccent : NoteType.Accent);
            }

            if (_padInfo.Dynamics == DrumNoteType.Ghost)
            {
                return (int) (isCymbal ? NoteType.CymbalGhost : NoteType.Ghost);
            }

            return (int) (isCymbal ? NoteType.Cymbal : NoteType.Normal);
        }

        private void UpdateColor()
        {
            var colors = Player.Player.ColorProfile.EliteDrums;

            // Get color index (0-based)
            int colorIndex = ElitePadInfo.GetColorIndex(_padInfo.Pad);

            // Handle kick separately
            if (_padInfo.Pad == EliteDrumPad.Kick)
            {
                var kickColor = colors.GetKickNoteColor();
                var kickColorNoSp = kickColor;

                if (NoteRef.WasMissed)
                {
                    kickColor = colors.Miss;
                }
                else if (NoteRef.IsStarPowerActivator && Player.Engine.CanStarPowerActivate &&
                         !Player.Engine.BaseStats.IsStarPowerActive)
                {
                    float pulse = (float) GameManager.BeatEventHandler.Visual.StrongBeat.CurrentPercentage;
                    var fullColor = colors.GetKickActivationNoteColor();
                    kickColor = Color.FromArgb(
                        fullColor.A,
                        GetColorFromPulse(fullColor.R, pulse),
                        GetColorFromPulse(fullColor.G, pulse),
                        GetColorFromPulse(fullColor.B, pulse));
                }
                else if (IsStarPowerVisible)
                {
                    kickColor = colors.GetKickStarPowerColor();
                }

                if (!NoteRef.WasHit)
                {
                    NoteGroup.SetColorWithEmission(kickColor.ToUnityColor(), kickColorNoSp.ToUnityColor());
                    NoteGroup.SetMetalColor(colors.GetMetalColor(IsStarPowerVisible).ToUnityColor());
                }

                return;
            }

            if (colorIndex < 0) return;

            // Handle lefty flip: reverse the color index
            if (LeftyFlip)
            {
                colorIndex = 7 - colorIndex;
            }

            // Determine the correct color
            var colorNoStarPower = colors.GetNoteColor(colorIndex);
            var color = colorNoStarPower;

            if (NoteRef.WasMissed)
            {
                color = colors.Miss;
            }
            else if (NoteRef.IsStarPowerActivator && Player.Engine.CanStarPowerActivate &&
                     !Player.Engine.BaseStats.IsStarPowerActive)
            {
                float pulse = (float) GameManager.BeatEventHandler.Visual.StrongBeat.CurrentPercentage;
                var fullColor = colors.GetActivationNoteColor(colorIndex);
                color = Color.FromArgb(
                    fullColor.A,
                    GetColorFromPulse(fullColor.R, pulse),
                    GetColorFromPulse(fullColor.G, pulse),
                    GetColorFromPulse(fullColor.B, pulse));
            }
            else if (IsStarPowerVisible)
            {
                color = colors.GetNoteStarPowerColor(colorIndex);
            }

            // Set the note color if not hidden
            if (!NoteRef.WasHit)
            {
                NoteGroup.SetColorWithEmission(color.ToUnityColor(), colorNoStarPower.ToUnityColor());
                NoteGroup.SetMetalColor(colors.GetMetalColor(IsStarPowerVisible).ToUnityColor());
            }
        }

        private static int GetColorFromPulse(int colorComponent, float pulse)
        {
            float intensity = Mathf.Pow(pulse - 1, 3) + 1f;
            return (int) (intensity * colorComponent);
        }
    }
}
