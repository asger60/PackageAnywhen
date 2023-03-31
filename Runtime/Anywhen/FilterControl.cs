using UnityEngine;
using UnityEngine.Audio;

public class FilterControl : MonoBehaviour
{
    public AudioMixer audioMixer;
    public AudioMixerSnapshot snap1, snap2;

    private AudioMixerSnapshot[] _snapshots;
    private float[] _snapWeights = new float[] {1, 0};
    private void Start()
    {
        _snapshots = new[] {snap1, snap2};
    }

    void Update()
    {
        audioMixer.TransitionToSnapshots(_snapshots, _snapWeights, Time.deltaTime * 3);
    }
}
