using System;
using System.Threading.Tasks;
using Anywhen;
using UnityEngine;

namespace Samples
{
    public class SampleTriggerMaterialFeedback : MonoBehaviour
    {
        private MaterialPropertyBlock _materialPropertyBlock;
        private Renderer _renderer;
        private Color _initialColor;
        private AnywhenSongTrigger _anywhenSongTrigger;
        private static readonly int Color1 = Shader.PropertyToID("_Color");

        private void Start()
        {
            TryGetComponent(out _anywhenSongTrigger);
            if (_anywhenSongTrigger)
            {
                TryGetComponent(out _renderer);
                _materialPropertyBlock = new MaterialPropertyBlock();
                _initialColor = _renderer.sharedMaterial.color;
                _anywhenSongTrigger.OnTrigger += Blink;
            }
        }

        private void OnDestroy()
        {
            if (_anywhenSongTrigger)
            {
                _anywhenSongTrigger.OnTrigger -= Blink;
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
