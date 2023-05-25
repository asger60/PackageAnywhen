using System;
using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AnywhenMetronome))]
    [RequireComponent(typeof(AnywhenConductor))]
    [RequireComponent(typeof(AnywhenSamplePlayer))]
    [RequireComponent(typeof(AnywhenEventFunnel))]
    public class AnywhenRuntime : MonoBehaviour
    {
        private static AnywhenMetronome _metronome;
        public static AnywhenMetronome Metronome => _metronome;
        private static AnywhenConductor _conductor;
        public static AnywhenConductor Conductor => _conductor;
        
        
        public void EditorSetup()
        {
            
        }

        private void Awake()
        {
            TryGetComponent(out _metronome);
            TryGetComponent(out _conductor);
        }
    }
}
