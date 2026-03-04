#nullable enable
using System.Collections.Generic;
using UnityEngine;
using YARG.Core.Chart;
using YARG.Gameplay.Player;
using static YARG.Core.Chart.EliteDrumNote;

namespace YARG.Gameplay.Visuals
{
    /// <summary>
    /// Visual indicator for the hi-hat pedal state. Positioned adjacent to the hi-hat lane,
    /// it animates between open (raised) and closed (lowered) states based on chart events.
    /// </summary>
    public class HatPedalIndicator : MonoBehaviour
    {
        public enum PedalState
        {
            Closed,
            Open
        }

        [SerializeField]
        private Transform? _pedalMesh;

        [SerializeField]
        private float _closedYPosition = -0.1f;

        [SerializeField]
        private float _openYPosition = 0.1f;

        [SerializeField]
        private float _transitionSpeed = 10f;

        private PedalState _currentState = PedalState.Closed;
        private PedalState _targetState = PedalState.Closed;

        private List<(double time, EliteDrumsHatPedalType type)> _pedalEvents = new();
        private int _eventIndex;

        private float _currentY;
        private bool _isInitialized;

        /// <summary>
        /// Initializes the hat pedal indicator by scanning the EliteDrumNote track
        /// for hat pedal events (Stomp, Splash). InvisibleTerminator events are filtered out.
        /// </summary>
        public void Initialize(InstrumentDifficulty<EliteDrumNote> eliteDifficulty)
        {
            _pedalEvents.Clear();
            _eventIndex = 0;

            foreach (var chord in eliteDifficulty.Notes)
            {
                foreach (var note in chord.AllNotes)
                {
                    if (note.Pad != (int) EliteDrumPad.HatPedal) continue;

                    // Filter out invisible terminators — they produce no visual
                    if (note.HatPedalType == EliteDrumsHatPedalType.InvisibleTerminator) continue;

                    _pedalEvents.Add((note.Time, note.HatPedalType));
                }
            }

            _currentState = PedalState.Closed;
            _targetState = PedalState.Closed;
            _currentY = _closedYPosition;

            if (_pedalMesh != null)
            {
                var pos = _pedalMesh.localPosition;
                _pedalMesh.localPosition = new Vector3(pos.x, _closedYPosition, pos.z);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Called each frame by EliteDrumsPlayer to advance through hat pedal events
        /// and animate the pedal mesh position.
        /// </summary>
        public void UpdateIndicator(double visualTime)
        {
            if (!_isInitialized) return;

            // Advance through events that have passed
            while (_eventIndex < _pedalEvents.Count && _pedalEvents[_eventIndex].time <= visualTime)
            {
                var (_, pedalType) = _pedalEvents[_eventIndex];

                switch (pedalType)
                {
                    case EliteDrumsHatPedalType.Stomp:
                        _targetState = PedalState.Closed;
                        _currentState = PedalState.Closed;
                        break;

                    case EliteDrumsHatPedalType.Splash:
                        // Splash: close briefly then open
                        _targetState = PedalState.Open;
                        _currentState = PedalState.Open;
                        break;
                }

                _eventIndex++;
            }

            // Animate pedal mesh position
            AnimatePedal();
        }

        private void AnimatePedal()
        {
            if (_pedalMesh == null) return;

            float targetY = _targetState == PedalState.Open ? _openYPosition : _closedYPosition;
            _currentY = Mathf.Lerp(_currentY, targetY, Time.deltaTime * _transitionSpeed);

            var pos = _pedalMesh.localPosition;
            _pedalMesh.localPosition = new Vector3(pos.x, _currentY, pos.z);
        }

        /// <summary>
        /// Resets the indicator to its initial closed state.
        /// </summary>
        public void Reset()
        {
            _eventIndex = 0;
            _currentState = PedalState.Closed;
            _targetState = PedalState.Closed;
            _currentY = _closedYPosition;

            if (_pedalMesh != null)
            {
                var pos = _pedalMesh.localPosition;
                _pedalMesh.localPosition = new Vector3(pos.x, _closedYPosition, pos.z);
            }
        }
    }
}
