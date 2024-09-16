using System;
using UnityEditor;
using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AnywhenMetronome))]
    [RequireComponent(typeof(AnywhenConductor))]
    [RequireComponent(typeof(AnywhenSamplePlayer))]
    [RequireComponent(typeof(AnywhenEventFunnel))]
    [RequireComponent(typeof(AnywhenNoteClipPreviewer))]
    [InitializeOnLoad]
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


        private static AnywhenRuntime _instance;
        private static bool _executeInEditMode;
        private static AnysongPlayer _targetPlayer;

        static AnywhenRuntime()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            if (_executeInEditMode)
            {
                Metronome.Update();
            }
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


        private void Awake()
        {
            _instance = this;
            TryGetComponent(out _metronome);
            TryGetComponent(out _conductor);
            TryGetComponent(out _anywhenSamplerHandler);
            TryGetComponent(out _eventFunnel);
            TryGetComponent(out _anywhenSynthHandler);
            _anywhenSynthHandler.ClearPresets();
            SetPreviewMode(false, null);
        }


        public static void SetPreviewMode(bool state, AnysongPlayer targetPlayer)
        {
            _instance = FindObjectOfType<AnywhenRuntime>();
            if (state)
            {
                _instance.TryGetComponent(out _metronome);
                _instance.TryGetComponent(out _conductor);
                _instance.TryGetComponent(out _anywhenSamplerHandler);
                _instance.TryGetComponent(out _eventFunnel);
                _instance.TryGetComponent(out _anywhenSynthHandler);
                _anywhenSynthHandler.ClearPresets();
                _anywhenSamplerHandler.Init();
                _anywhenSynthHandler.Init();
                Metronome.Play();
                targetPlayer.Load();
                targetPlayer.Play();
            }
            else
            {
                _anywhenSynthHandler.ClearPresets();
            }

            _executeInEditMode = state;
        }
    }
}