using System;
using Anywhen.Composing;
using UnityEngine;

namespace Anywhen
{
    [AddComponentMenu("Anywhen/AnywhenIntensityTrigger")]
    public class AnywhenIntensityTrigger : AnywhenTrigger
    {
        
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

        [Range(0, 100f)] [SerializeField] private float intensitySetValue = 100;
        [Range(-100, 100f)] [SerializeField] private float intensityModifyValue;
        
        protected override void Trigger()
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