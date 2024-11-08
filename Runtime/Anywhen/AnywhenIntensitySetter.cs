using System;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    [AddComponentMenu("Anywhen/AnywhenIntensitySetter")]
    public class AnywhenIntensitySetter : MonoBehaviour
    {
        [FormerlySerializedAs("anywhenTrigger")] [SerializeField]
        private AnywhenTrigger trigger;


        enum IntensityTarget
        {
            Global,
            AnywhenPlayer
        }

        [SerializeField] private IntensityTarget intensityTarget;

        [SerializeField] private AnywhenPlayer targetPlayer;

        public enum ValueChangeModes
        {
            Set,
            Modify
        }

        [SerializeField] private ValueChangeModes intensityUpdateMode;

        [Range(0, 100f)] [SerializeField] private float intensitySetValue;

        [Range(-100, 100f)] [SerializeField] private float intensityModifyValue;

        void Start()
        {
            if (!trigger)
                TryGetComponent(out trigger);
            trigger.OnTrigger += Trigger;
        }

        private void Trigger()
        {
            switch (intensityUpdateMode)
            {
                case ValueChangeModes.Set:
                    switch (intensityTarget)
                    {
                        case IntensityTarget.Global:
                            AnysongPlayerBrain.SetGlobalIntensity(intensitySetValue / 100f);
                            break;
                        case IntensityTarget.AnywhenPlayer:
                            targetPlayer.SetIntensity(intensitySetValue / 100f);
                            break;
                    }

                    break;
                case ValueChangeModes.Modify:
                    switch (intensityTarget)
                    {
                        case IntensityTarget.Global:
                            AnysongPlayerBrain.ModifyGlobalIntensity(intensityModifyValue / 100f);
                            break;
                        case IntensityTarget.AnywhenPlayer:
                            targetPlayer.ModifyIntensity(intensityModifyValue / 100f);
                            break;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}