using System.Collections.Generic;
using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "AnywhenTrackTypeAsset", menuName = "Anywhen/AnywhenTrackType")]
    public class AnywhenTrackTypeAsset : ScriptableObject
    {
        public List<string> items = new List<string>();
    }
}