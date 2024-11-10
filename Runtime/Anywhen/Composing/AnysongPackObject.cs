using System.Collections.Generic;
using System.IO;
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

        public string description;

        public void ClearSongs()
        {
            _songs?.Clear();
        }


        [ContextMenu("Get songs")]
        public void FetchSongNames()
        {
            var song = GetAtPath<AnysongObject>(editorSongPath);
            songNames = new string[song.Length];

            for (var i = 0; i < song.Length; i++)
            {
                songNames[i] = song[i].name + ".asset";
                var o = song[i];
                Debug.Log(o);
            }
        }

        public static T[] GetAtPath<T>(string path) where T : UnityEngine.Object
        {
            var assets = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
            List<T> foundAssets = new List<T>();

            foreach (var guid in assets)
            {
                foundAssets.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            // if you want to skip the convertion to array, simply change method return type
            return foundAssets.ToArray();


            //List<T> al = new List<T>();
            //string[] fileEntries = Directory.GetFiles(Application.dataPath + "/" + path);
            //foreach (string fileName in fileEntries)
            //{
            //    int index = fileName.LastIndexOf("/");
            //    string localPath = "Assets/" + path;
//
            //    if (index > 0)
            //        localPath += fileName.Substring(index);
//
            //    var t = (T)AssetDatabase.LoadAssetAtPath(localPath, typeof(T));
//
            //    if (t != null)
            //        al.Add(t);
            //}
//
            //T[] result = new T[al.Count];
            //for (int i = 0; i < al.Count; i++)
            //    result[i] = (T)al[i];
//
            //return result;
        }
    }
}