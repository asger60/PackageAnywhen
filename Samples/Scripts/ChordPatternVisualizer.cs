using System;
using System.Collections.Generic;
using Anywhen;
using Samples.Scripts;
using UnityEngine;

namespace PackageAnywhen.Samples.Scripts
{
    public class ChordPatternVisualizer : MonoBehaviour
    {
        public PartyType partyTypePrefab;
        public ChordPatternMixer chordPatternMixer;
        private List<PartyType> _partyTypes = new List<PartyType>();
        private Vector3[] _circlePositions;
        public float circleDistance;

        private void Start()
        {
            GeneratePositions();
            for (int i = 0; i < 32; i++)
            {
                var newPartyType = Instantiate(partyTypePrefab, transform);
                newPartyType.Init(i, 32, _circlePositions[i]);
                newPartyType.SetColor(Color.blue);
                AnywhenMetronome.Instance.OnTick16 += newPartyType.Tick;
                _partyTypes.Add(newPartyType);
            }
        }

        private void Update()
        {
            for (var i = 0; i < _partyTypes.Count; i++)
            {
                _partyTypes[i].SetNoteOn(false, 0);
            }

            for (var i = 0; i < _partyTypes.Count; i++)
            {
                if (chordPatternMixer.CurrentTriggerPattern[i])
                {
                    _partyTypes[i].SetChordOn(chordPatternMixer.CurrentTriggerPattern[(int)Mathf.Repeat(i, 32)],
                        chordPatternMixer.CurrentNotePattern[i], chordPatternMixer.CurrentChordPattern[i]);
                }
            }
        }

        void GeneratePositions()
        {
            _circlePositions = new Vector3[(int)32];
            for (int i = 0; i < _circlePositions.Length; i++)
            {
                var x = (circleDistance * Mathf.Cos((i / (float)(int)32 * 360) / (180f / Mathf.PI)));
                var z = (circleDistance * Mathf.Sin((i / (float)(int)32 * 360) / (180f / Mathf.PI)));

                _circlePositions[i] = new Vector3(-x, 0, z);
            }
        }

        public void SetIsTrackActive(bool state)
        {
            foreach (var partyType in _partyTypes)
            {
                partyType.SetIsTrackActive(state);
            }
        }
    }
}