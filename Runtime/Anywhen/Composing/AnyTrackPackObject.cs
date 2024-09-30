using Anywhen.Composing;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AnyTrackPack", menuName = "Anywhen/AnyTrackPack")]
public class AnyTrackPackObject : ScriptableObject
{
    public AssetLabelReference AssetLabelReference;
    public AnysongObject[] Songs => _songs;
     private AnysongObject[] _songs;

    public void SetSongs(AnysongObject[] songsList)
    {
        _songs = songsList;
    }
    public Texture packImage;
}