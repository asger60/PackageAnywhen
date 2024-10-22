using System;
using Anywhen.Composing;
using UnityEngine;

namespace Anywhen
{
    [AddComponentMenu("Anywhen/AnywhenIntensitySetter")]
    public class AnywhenIntensitySetter : MonoBehaviour
    {
        [SerializeField] private AnywhenTrigger anywhenTrigger;


        public enum ValueChangeModes
        {
            Set,
            Modify
        }

        [SerializeField] private ValueChangeModes intensityUpdateMode;

        [Range(0, 1f)] [SerializeField] private float intensitySetValue;

        [Range(-1, 1f)] [SerializeField] private float intensityModifyValue;

        void Start()
        {
            if (!anywhenTrigger)
                TryGetComponent(out anywhenTrigger);
            anywhenTrigger.OnTrigger += Trigger;
        }

        private void Trigger()
        {
            switch (intensityUpdateMode)
            {
                case ValueChangeModes.Set:
                    AnysongPlayerBrain.SetGlobalIntensity(intensitySetValue);
                    break;
                case ValueChangeModes.Modify:
                    AnysongPlayerBrain.ModifyGlobalIntensity(intensityModifyValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}