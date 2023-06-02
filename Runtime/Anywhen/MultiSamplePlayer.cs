using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Anywhen
{
    public class MultiSamplePlayer : MonoBehaviour
    {
        [FormerlySerializedAs("samplerPrefab")] public AnywhenSampler anywhenSamplerPrefab;

        private readonly List<AnywhenSampler> _allSamplers = new List<AnywhenSampler>(50);

        private bool _isInit;
        public bool IsInit => _isInit;
        public static MultiSamplePlayer Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!AnywhenMetronome.Instance.IsInit) AnywhenMetronome.Instance.Init();


            for (int i = 0; i < 50; i++)
            {
                _allSamplers.Add(Instantiate(anywhenSamplerPrefab, transform));
                _allSamplers.Last().Init(AnywhenMetronome.TickRate.Sub32);
            }

            _isInit = true;
        }





        private AnywhenSampler GetSampler()
        {
            foreach (var thisSampler in _allSamplers)
            {
                if (thisSampler.IsReady && !thisSampler.IsStopping)
                    return thisSampler;
            }

            print("#AudioSystem#didn't find a free sampler - returning the one with the oldest source");
            
            //didn't find a free sampler - returning the one with the oldest source
            float shortestDuration = float.MaxValue;
            AnywhenSampler oldestAnywhenSampler = null;
            foreach (var thisSampler in _allSamplers)
            {
                float thisDuration = thisSampler.GetDurationToEnd();
                if (thisDuration < shortestDuration)
                {
                    shortestDuration = thisDuration;
                    oldestAnywhenSampler = thisSampler;
                }
            }

            return oldestAnywhenSampler;
        }

        

        
        
    }
}