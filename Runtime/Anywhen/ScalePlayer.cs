using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public class ScalePlayer : MonoBehaviour
    {
        private static ScalePlayer _instance;
        public static ScalePlayer Instance => _instance;
        private void Awake()
        {
            _instance = this;
        }

        public void HandleEvent(NoteEvent e, AnywhenSettingsScale anywhenSettingsScale)
        {
            //todo reimplement this
            //int indexX = (int)(settingsObjectScale.GetUnscaledValue(e.expression1 * settingsObjectScale.scales.Length);
            //int indexY = (int)(settingsObjectScale.GetUnscaledValue(e.position.GetPosition()).y * settingsObjectScale.rootNotes.Length);
            //
            //Conductor.Instance.OverrideScale(settingsObjectScale.scales[indexX]);
            //Conductor.Instance.OverrideRootNote(settingsObjectScale.rootNotes[indexY]);
        }

        public string GetNoteName(int noteIndex)
        {
            noteIndex = (int)Mathf.Repeat(noteIndex, 11);
            switch (noteIndex)
            {
                case 0: return "C";
                case 1: return "C#";
                case 2: return "D";
                case 3: return "D#";
                case 4: return "E";
                case 5: return "F";
                case 6: return "F#";
                case 7: return "G";
                case 8: return "G#";
                case 9: return "A";
                case 10: return "A#";
                case 11: return "H";
            }

            return "";
        }
        
    }
}
