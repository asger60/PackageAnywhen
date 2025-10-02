using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Anywhen.Composing
{
    [CreateAssetMenu(fileName = "AnyTrackPack", menuName = "Anywhen/AnyTrackPack")]
    public class AnysongPackObject : ScriptableObject
    {
        public AnysongObject[] Songs => _songs.ToArray();
        private List<AnysongObject> _songs = new List<AnysongObject>();
        public string[] songNames;
        [SerializeField] string editorSongPath;
        [SerializeField] private bool isInPackage;
        public bool IsInPackage => isInPackage;

        public void AddSong(AnysongObject song)
        {
            _songs ??= new List<AnysongObject>();
            _songs.Add(song);
        }

        public Sprite packImage;
        public Color editorBackgroundColor;

        [Multiline] public string description;

        public void ClearSongs()
        {
            _songs?.Clear();
        }
#if UNITY_EDITOR


        [ContextMenu("Get songs")]
        public void FetchSongNames()
        {
            var song = GetAtPath<AnysongObject>(editorSongPath);
            songNames = new string[song.Length];

            for (var i = 0; i < song.Length; i++)
            {
                songNames[i] = song[i].name + ".asset";
                var o = song[i];
            }
        }

        private static T[] GetAtPath<T>(string path) where T : Object
        {
            var assets = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
            List<T> foundAssets = new List<T>();

            foreach (var guid in assets)
            {
                foundAssets.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return foundAssets.ToArray();
        }

#endif
    }
}