using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Anywhen.Composing
{
    [CreateAssetMenu(fileName = "AnyTrackPack", menuName = "Anywhen/AnyTrackPack")]
    public class AnysongPackObject : ScriptableObject
    {
        public AssetLabelReference AssetLabelReference;
        public AnysongObject[] Songs => _songs.ToArray();
        private List<AnysongObject> _songs = new List<AnysongObject>();
    

        public void AddSong(AnysongObject song)
        {
            _songs ??= new List<AnysongObject>();
            _songs.Add(song);
        }

        public Sprite packImage;
        public Color editorBackgroundColor;
    
        public string description;

        public void ClearSongs()
        {
            _songs?.Clear();
        }
    }
}