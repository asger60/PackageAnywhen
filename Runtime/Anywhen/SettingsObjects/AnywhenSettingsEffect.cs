using UnityEngine;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New effect object", menuName = "Anywhen/AudioObjects/EffectObject")]
    public class AnywhenSettingsEffect : AnywhenSettingsBase
    {
        public float effectOffValue;
        public float effectMinValue, effectMaxValue;

        public AnimationCurve effectCurve;
        public string effectParameterName;


        //public override Vector2 GetScaledValue(Vector2 position)
        //{
        //    float valueX = Mathf.Lerp(effectMinValue, effectMaxValue, effectCurve.Evaluate((position.x)));
        //    return new Vector2(valueX, 0);
        //}
//
        //public override Vector2 GetUnscaledValue(Vector2 position)
        //{
        //    float valueX = (position.x);
        //    return new Vector2(valueX, 0);
        //}
    }
}