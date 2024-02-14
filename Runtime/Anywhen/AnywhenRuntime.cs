using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AnywhenMetronome))]
    [RequireComponent(typeof(AnywhenConductor))]
    [RequireComponent(typeof(AnywhenSamplePlayer))]
    [RequireComponent(typeof(AnywhenEventFunnel))]
    [RequireComponent(typeof(AnywhenNoteClipPreviewer))]
    public class AnywhenRuntime : MonoBehaviour
    {
        private static AnywhenMetronome _metronome;
        public static AnywhenMetronome Metronome => _metronome;
        
        private static AnywhenConductor _conductor;
        public static AnywhenConductor Conductor => _conductor;
        
        private static AnywhenNoteClipPreviewer _previewer;
        
        static AnywhenEventFunnel _eventFunnel;
        public static AnywhenSamplePlayer AnywhenSamplerHandler => _anywhenSamplerHandler;
        
        private static AnywhenSamplePlayer _anywhenSamplerHandler;
        
        
        public static AnywhenSynthPlayer AnywhenSynthHandler => _anywhenSynthHandler;
        private static AnywhenSynthPlayer _anywhenSynthHandler;
        public static AnywhenEventFunnel EventFunnel => _eventFunnel;

        
        
        private void OnDestroy()
        {
            //AudioConfiguration config = AudioSettings.GetConfiguration();
            //AudioSettings.Reset(config);
        }

       

        public static AnywhenNoteClipPreviewer ClipPreviewer
        {
            get
            {
                if (_previewer != null)
                    return _previewer;
                _previewer = FindObjectOfType<AnywhenRuntime>().GetComponent<AnywhenNoteClipPreviewer>();
                return _previewer;
            }
        }


        public void EditorSetup()
        {
        }

        private void Awake()
        {
            TryGetComponent(out _metronome);
            TryGetComponent(out _conductor);
            TryGetComponent(out _anywhenSamplerHandler);
            TryGetComponent(out _eventFunnel);
            TryGetComponent(out _anywhenSynthHandler);
        }
    }
}