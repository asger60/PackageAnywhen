using System;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anywhen
{
    public class AnywhenIntensitySetter : MonoBehaviour
    {
        [FormerlySerializedAs("trigger")] [SerializeField]
        private AnywhenTrigger anywhenTrigger;


        private enum ValueChangeModes
        {
            Set,
            Modify
        }

        [SerializeField] private ValueChangeModes valueChangeMode;

        [FormerlySerializedAs("intensityValue")] [SerializeField]
        private float intensitySetValue;

        [SerializeField] private float intensityModifyValue;

        void Start()
        {
            if (!anywhenTrigger)
                TryGetComponent(out anywhenTrigger);
            anywhenTrigger.OnTrigger += Trigger;
        }

        private void Trigger()
        {
            switch (valueChangeMode)
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