using UnityEngine;
using UnityEngine.UI;
using YARG.Core;
using YARG.Core.Logging;

namespace YARG.Menu.Dialogs
{
    // Prefab wiring (FriendlyEliteDrumsBindingDialog.prefab):
    // _eliteDrumHighlights array must have 13 elements:
    //   [0] Kick, [1] Stomp, [2] Splash, [3] LeftCrash,
    //   [4] ClosedHiHat, [5] SizzleHiHat, [6] OpenHiHat,
    //   [7] Snare, [8] Tom1, [9] Tom2, [10] Tom3,
    //   [11] Ride, [12] RightCrash
    public class EliteDrumsBindingDialog : FriendlyBindingDialog
    {
        [Header("Elite Drums Kit")]
        [SerializeField]
        private Image _drumKitImage;

        [Header("Elite Drums Highlights (13 pads)")]
        [SerializeField]
        private Image[] _eliteDrumHighlights;

        protected override (string initial, string complete) BindingMessages { get; set; } = (
            "When a pad/cymbal is highlighted, strike the corresponding input on your drum kit.\n\nClick the Start button when you're ready to begin.",
            "Binding complete.\n\nYou will still need to manually set menu navigation bindings if you have not already."
        );

        public override void Initialize()
        {
            if (_eliteDrumHighlights == null || _eliteDrumHighlights.Length != 13)
            {
                YargLogger.LogError($"EliteDrumsBindingDialog: Expected 13 highlights, " +
                    $"got {_eliteDrumHighlights?.Length ?? 0}. Check prefab wiring.");
            }

            Image = _drumKitImage;
            _keyHighlights = _eliteDrumHighlights;

            base.Initialize();

            Title.text = "Elite Drums Binding";
        }

        protected override void CheckForModeSwitch(string key, GameMode mode)
        {
            // Elite Drums is a single-mode dialog — no mode switching required
        }

        protected override bool IsKeyValid(GameMode mode, string key)
        {
            return key == "Drums.Kick" || key.StartsWith("EliteDrums.");
        }

        protected override Image GetHighlightByName(GameMode mode, string bindingName)
        {
            if (_eliteDrumHighlights == null || _eliteDrumHighlights.Length != 13)
            {
                return null;
            }

            var highlight = bindingName switch
            {
                "Drums.Kick"              => _eliteDrumHighlights[0],
                "EliteDrums.Stomp"        => _eliteDrumHighlights[1],
                "EliteDrums.Splash"       => _eliteDrumHighlights[2],
                "EliteDrums.LeftCrash"    => _eliteDrumHighlights[3],
                "EliteDrums.ClosedHiHat"  => _eliteDrumHighlights[4],
                "EliteDrums.SizzleHiHat"  => _eliteDrumHighlights[5],
                "EliteDrums.OpenHiHat"    => _eliteDrumHighlights[6],
                "EliteDrums.Snare"        => _eliteDrumHighlights[7],
                "EliteDrums.Tom1"         => _eliteDrumHighlights[8],
                "EliteDrums.Tom2"         => _eliteDrumHighlights[9],
                "EliteDrums.Tom3"         => _eliteDrumHighlights[10],
                "EliteDrums.Ride"         => _eliteDrumHighlights[11],
                "EliteDrums.RightCrash"   => _eliteDrumHighlights[12],
                _ => null
            };

            if (highlight is null)
            {
                YargLogger.LogWarning($"EliteDrumsBindingDialog: Unrecognized binding name '{bindingName}'.");
            }

            return highlight;
        }
    }
}
