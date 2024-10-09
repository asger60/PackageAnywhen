using System.Collections.Generic;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AnyTrackPack", menuName = "Anywhen/AnyTrackPack")]
public class AnysongPackObject : ScriptableObject
{
    public AssetLabelReference AssetLabelReference;
    public AnysongObject[] Songs => _songs.ToArray();
    private List<AnysongObject> _songs = new List<AnysongObject>();

    public void SetSongs(AnysongObject[] songsList)
    {
        _songs.AddRange( songsList);
    }

    public void AddSong(AnysongObject song)
    {
        //Debug.Log("adding " + song.name);
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