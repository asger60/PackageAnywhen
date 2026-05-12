using Anywhen;
using UnityEngine;
using UnityEngine.UIElements;

namespace Anysong
{
    public class OscilloscopeElement : VisualElement
    {
        private float[] _samples = new float[512];
        private int _sampleCount;
        private static readonly Color LineColor = AnywhenColors.Border;

        public OscilloscopeElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
         //   AnywhenAudioGenerator.OnAudioGeneratedStatic += HandleAudioGenerated;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
          //  AnywhenAudioGenerator.OnAudioGeneratedStatic -= HandleAudioGenerated;
        }

        private void HandleAudioGenerated(float[] samples, int channels)
        {
            // We only need a slice of samples for the oscilloscope
            int count = Mathf.Min(samples.Length, _samples.Length);
            for (int i = 0; i < count; i++)
            {
                _samples[i] = samples[i];
            }
            _sampleCount = count;
            
            // Schedule a repaint
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_sampleCount <= 1)
                return;

            var paint2D = mgc.painter2D;
            paint2D.BeginPath();
            paint2D.strokeColor = LineColor;
            paint2D.lineWidth = 1f;

            float width = contentRect.width;
            float height = contentRect.height;
            float midY = height / 2f;

            for (int i = 0; i < _sampleCount; i++)
            {
                float x = (i / (float)(_sampleCount - 1)) * width;
                float y = midY - (_samples[i] * midY * 0.9f); // Scale slightly down to avoid clipping

                if (i == 0)
                    paint2D.MoveTo(new Vector2(x, y));
                else
                    paint2D.LineTo(new Vector2(x, y));
            }

            paint2D.Stroke();
        }
    }
}
