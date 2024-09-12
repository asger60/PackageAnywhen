using System.Threading.Tasks;
using Anywhen;
using UnityEngine;
using UnityEngine.Serialization;

namespace Samples.Scripts
{
    public class SampleTriggerMaterialFeedback : MonoBehaviour
    {
        private MaterialPropertyBlock _materialPropertyBlock;
        private Renderer _renderer;
        private Color _initialColor;

        [FormerlySerializedAs("_anywhenTrigger")] [SerializeField]
        private AnywhenTrigger anywhenTrigger;

        private static readonly int Color1 = Shader.PropertyToID("_Color");

        private void Start()
        {
            if (!anywhenTrigger)
                TryGetComponent(out anywhenTrigger);
            if (anywhenTrigger)
            {
                TryGetComponent(out _renderer);
                _materialPropertyBlock = new MaterialPropertyBlock();
                _initialColor = _renderer.sharedMaterial.color;
                anywhenTrigger.OnTrigger += Blink;
            }
        }

        private void OnDestroy()
        {
            if (anywhenTrigger)
            {
                anywhenTrigger.OnTrigger -= Blink;
            }
        }

        async void Blink()
        {
            float f = 0;
            float duration = 1;
            while (f < duration)
            {
                _materialPropertyBlock.SetColor(Color1, Color.Lerp(Color.white, _initialColor, f));
                _renderer.SetPropertyBlock(_materialPropertyBlock);
                f += Time.deltaTime;
                await Task.Yield();
            }
        }
    }
}