using Anywhen.Composing;
using UnityEngine;


[CreateAssetMenu(fileName = "AnyTrackPack", menuName = "Anywhen/AnyTrackPack")]
public class AnyTrackPackObject : ScriptableObject
{
    public AnysongObject[] Songs => songs;
    [SerializeField] private AnysongObject[] songs;
}