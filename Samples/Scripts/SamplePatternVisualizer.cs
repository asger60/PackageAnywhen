using System;
using System.Collections.Generic;
using Anywhen;
using UnityEngine;
using UnityEngine.Serialization;

namespace Samples.Scripts
{
    public class SamplePatternVisualizer : MonoBehaviour
    {
        public Vector3[] circlePositions = new Vector3[16];


        public int circleStepLength = 16;
        public float circleDistance = 5;

        public PartyType stepPrefab;

        [FormerlySerializedAs("patternMixer")] public DrumPatternMixer drumPatternMixer;
        public int trackIndex;
        public AnywhenMetronome.TickRate tickRate;
        private List<PartyType> _partyTypes = new();
        public GameObject groundTilePrefab;

        private void Start()
        {
            GeneratePositions();
            for (var i = 0; i < 16; i++)
            {
                var groundObject = Instantiate(groundTilePrefab, transform);
                groundObject.transform.position = circlePositions[i];
            }

            for (var i = 0; i < 16; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var stepObject = Instantiate(stepPrefab, transform);
                    stepObject.Init(i, 16, circlePositions[i],
                        drumPatternMixer.patternInstruments[j].instruments[trackIndex]);
                    _partyTypes.Add(stepObject);

                    switch (tickRate)
                    {
                        case AnywhenMetronome.TickRate.None:
                            break;
                        case AnywhenMetronome.TickRate.Sub2:
                            AnywhenMetronome.Instance.OnTick2 += stepObject.Tick;
                            break;
                        case AnywhenMetronome.TickRate.Sub4:
                            AnywhenMetronome.Instance.OnTick4 += stepObject.Tick;
                            break;
                        case AnywhenMetronome.TickRate.Sub8:
                            AnywhenMetronome.Instance.OnTick8 += stepObject.Tick;
                            break;
                        case AnywhenMetronome.TickRate.Sub16:
                            AnywhenMetronome.Instance.OnTick16 += stepObject.Tick;
                            break;
                        case AnywhenMetronome.TickRate.Sub32:
                            AnywhenMetronome.Instance.OnTick32 += stepObject.Tick;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private void Update()
        {
            var stepTriggers = drumPatternMixer.GetCurrentPattern(trackIndex);
            var instruments = drumPatternMixer.GetInstruments(trackIndex);

            for (var i = 0; i < _partyTypes.Count; i++)
            {
                _partyTypes[i].SetNoteOn(false, 0);
            }

            for (var i = 0; i < 16; i++)
            {
                if (stepTriggers[i])
                {
                    var p = GetPartyTypeForInstrument(i, instruments[i]);
                    p.SetNoteOn(stepTriggers[(int)Mathf.Repeat(i, 16)], 0);
                }
            }
        }

        PartyType GetPartyTypeForInstrument(int step, DrumPatternMixer.InstrumentObject instrumentObject)
        {
            foreach (var partyType in _partyTypes)
            {
                if (partyType.ConnectedStepIndex == step && Equals(partyType.InstrumentObject, instrumentObject))
                    return partyType;
            }

            return _partyTypes[step];
        }

        [ContextMenu("generate positions")]
        void GeneratePositions()
        {
            circlePositions = new Vector3[(int)tickRate];
            for (int i = 0; i < circlePositions.Length; i++)
            {
                var x = (circleDistance * Mathf.Cos((i / (float)(int)tickRate * 360) / (180f / Mathf.PI)));
                var z = (circleDistance * Mathf.Sin((i / (float)(int)tickRate * 360) / (180f / Mathf.PI)));

                circlePositions[i] = new Vector3(-x, 0, z);
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