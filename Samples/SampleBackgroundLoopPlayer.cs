using UnityEngine;
using Random = UnityEngine.Random;

namespace Samples
{
    public class SampleBackgroundLoopPlayer : MonoBehaviour
    {
        private AudioSource _source;
        void Start()
        {
            TryGetComponent(out  _source);
            _source.Play();
            _source.time = Random.Range(0, _source.clip.length);

        }
    }
}
