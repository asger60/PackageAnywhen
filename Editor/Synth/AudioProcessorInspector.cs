using System;
using Anywhen.Synth;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Synth
{
    public class AudioProcessorInspector : Editor
    {
        private class FilterPreviewElement : VisualElement
        {
            private AudioProcessorSettingsObject _settings;
            private const int Resolution = 50;
            private readonly float[] _response = new float[Resolution];

            public FilterPreviewElement(AudioProcessorSettingsObject settings)
            {
                _settings = settings;
                style.height = 100;
                style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
                style.marginTop = 5;
                style.marginBottom = 5;
                 generateVisualContent += OnGenerateVisualContent;
            }

            public void Refresh()
            {
                UpdateResponse();
                MarkDirtyRepaint();
            }

            private void UpdateResponse()
            {
                switch (_settings.filterType)
                {
                    case AudioProcessorSettingsObject.FilterTypes.LowPassFilter:
                    case AudioProcessorSettingsObject.FilterTypes.LadderFilter:
                    {
                        float cutoff = _settings.filterType == AudioProcessorSettingsObject.FilterTypes.LowPassFilter
                            ? _settings.lowPassSettings.cutoffFrequency
                            : _settings.ladderSettings.cutoffFrequency;
                        float resonance = _settings.filterType == AudioProcessorSettingsObject.FilterTypes.LowPassFilter
                            ? _settings.lowPassSettings.resonance
                            : _settings.ladderSettings.resonance;

                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float x = freq / cutoff;
                            float mag = 1.0f / Mathf.Sqrt(1.0f + Mathf.Pow(x, 8.0f));
                            if (resonance > 0)
                            {
                                float peak = resonance * 3.0f * Mathf.Exp(-Mathf.Pow(x - 1.0f, 2.0f) * 10.0f);
                                mag += peak;
                            }

                            _response[i] = mag / (1.0f + resonance * 2.0f);
                        }

                        break;
                    }
                    case AudioProcessorSettingsObject.FilterTypes.BandPassFilter:
                    {
                        float center = _settings.bandPassSettings.frequency;
                        float width = _settings.bandPassSettings.bandWidth;
                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float x = freq / center;
                            float q = 100.0f / Mathf.Max(width, 1.0f);
                            float mag = 1.0f / Mathf.Sqrt(1.0f + Mathf.Pow(q * (x - 1.0f / x), 2.0f));
                            _response[i] = Mathf.Clamp01(mag);
                        }

                        break;
                    }
                    case AudioProcessorSettingsObject.FilterTypes.FormantFilter:
                    {
                        int vowel = _settings.formantSettings.vowel;
                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float mag = 0;
                            float[] peaks = GetVowelPeaks(vowel);
                            foreach (var p in peaks)
                            {
                                mag += 0.5f * Mathf.Exp(-Mathf.Pow((freq - p) / (p * 0.2f), 2.0f));
                            }

                            _response[i] = Mathf.Clamp01(mag);
                        }

                        break;
                    }
                    case AudioProcessorSettingsObject.FilterTypes.BitcrushFilter:
                    {
                        float bitDepth = _settings.bitcrushSettings.bitDepth;
                        int downsampling = _settings.bitcrushSettings.downsampling;
                        float maxFreq = 20000.0f / downsampling;

                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float mag = freq < maxFreq ? 0.8f : 0.0f;

                            if (bitDepth < 16 && freq < maxFreq)
                            {
                                float steps = Mathf.Pow(2, bitDepth);
                                float noise = (Mathf.Repeat(i * 0.5f, 1.0f) - 0.5f) * (1.0f / steps) * 10.0f;
                                mag += noise;
                            }

                            _response[i] = Mathf.Clamp01(mag);
                        }

                        break;
                    }
                    case AudioProcessorSettingsObject.FilterTypes.SaturatorFilter:
                    {
                        float drive = _settings.saturatorSettings.drive;
                        float wet = _settings.saturatorSettings.wet;

                        for (int i = 0; i < Resolution; i++)
                        {
                            float mag = 0.5f + (drive / 10.0f) * 0.4f;
                            mag = mag / (1.0f + mag * 0.2f);
                            mag = Mathf.Lerp(0.5f, mag, wet);
                            _response[i] = Mathf.Clamp01(mag);
                        }

                        break;
                    }
                    case AudioProcessorSettingsObject.FilterTypes.DelayFilter:
                    {
                        float wet = _settings.delaySettings.wet;

                        for (int i = 0; i < Resolution; i++)
                        {
                            _response[i] = Mathf.Clamp01(Mathf.Lerp(0.5f, 0.8f, wet));
                        }

                        break;
                    }
                    case AudioProcessorSettingsObject.FilterTypes.ChorusFilter:
                    {
                        float depth = _settings.chorusSettings.depth;
                        float rate = _settings.chorusSettings.rate;
                        float wet = _settings.chorusSettings.wet;

                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float ripple = depth * 0.15f * Mathf.Sin(freq * 0.001f * rate * Mathf.PI);
                            float mag = Mathf.Lerp(0.5f, 0.7f + ripple, wet);
                            _response[i] = Mathf.Clamp01(mag);
                        }

                        break;
                    }
                }
            }

            private float[] GetVowelPeaks(int vowel)
            {
                switch (vowel)
                {
                    case 1: return new float[] { 270, 2290, 3010 }; // i
                    case 2: return new float[] { 390, 1990, 2550 }; // e
                    case 3: return new float[] { 730, 1090, 2440 }; // a
                    case 4: return new float[] { 570, 840, 2410 }; // o
                    case 5: return new float[] { 300, 870, 2240 }; // u
                    default: return new float[] { 500, 1500, 2500 };
                }
            }

            private float LogScale(float t, float min, float max)
            {
                return min * Mathf.Pow(max / min, t);
            }

            private void OnGenerateVisualContent(MeshGenerationContext mgc)
            {
                var painter = mgc.painter2D;
                var rect = contentRect;
                if (rect.width <= 0 || rect.height <= 0) return;

                painter.BeginPath();
                painter.lineWidth = 1;
                painter.strokeColor = Color.white;

                for (int i = 0; i < Resolution; i++)
                {
                    float x = (i / (float)(Resolution - 1)) * rect.width;
                    float y = rect.height - (_response[i] * rect.height * 0.8f) - rect.height * 0.1f;
                    if (i == 0) painter.MoveTo(new Vector2(x, y));
                    else painter.LineTo(new Vector2(x, y));
                }

                painter.Stroke();

                float cutoff = -1;
                if (_settings.filterType == AudioProcessorSettingsObject.FilterTypes.LowPassFilter)
                    cutoff = _settings.lowPassSettings.cutoffFrequency;
                else if (_settings.filterType == AudioProcessorSettingsObject.FilterTypes.LadderFilter)
                    cutoff = _settings.ladderSettings.cutoffFrequency;
                else if (_settings.filterType == AudioProcessorSettingsObject.FilterTypes.BandPassFilter)
                    cutoff = _settings.bandPassSettings.frequency;
                else if (_settings.filterType == AudioProcessorSettingsObject.FilterTypes.BitcrushFilter)
                    cutoff = 20000.0f / _settings.bitcrushSettings.downsampling;

                if (cutoff > 0)
                {
                    float t = Mathf.Log(cutoff / 20f) / Mathf.Log(20000f / 20f);
                    float lx = Mathf.Clamp01(t) * rect.width;
                    painter.BeginPath();
                    painter.lineWidth = 1;
                    painter.strokeColor = new Color(1, 1, 1, 0.3f);
                    painter.MoveTo(new Vector2(lx, 0));
                    painter.LineTo(new Vector2(lx, rect.height));
                    painter.Stroke();
                }
            }
        }

        public static VisualElement Draw(AudioProcessorSettingsObject settings)
        {
            VisualElement element = new VisualElement();
            var so = new SerializedObject(settings);

            var label = new Label(settings.filterType.ToString())
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            element.Add(label);

            var preview = new FilterPreviewElement(settings);
            element.Add(preview);
            preview.Refresh();

           


            switch (settings.filterType)
            {
                case AudioProcessorSettingsObject.FilterTypes.LowPassFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("lowPassSettings.oversampling"), "Oversampling", 1, 4, true,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("lowPassSettings.cutoffFrequency"), "CutOff", 1, 24000, false,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("lowPassSettings.resonance"), "Resonance", 0, 1, false,
                        preview));
                    break;
                case AudioProcessorSettingsObject.FilterTypes.BandPassFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("bandPassSettings.frequency"), "Frequency", 1, 24000, false,
                        preview));

                    var bwSlider = CreateBoundSlider(so.FindProperty("bandPassSettings.bandWidth"), "Bandwidth", 1, 10000, false,
                        preview, _ =>
                        {
                            settings.SyncBandPassFromBandwidth();
                            so.Update();
                        });
                    var qSlider = CreateBoundSlider(so.FindProperty("bandPassSettings.q"), "Q", 0.01f, 100, false, preview, _ =>
                    {
                        settings.SyncBandPassFromQ();
                        so.Update();
                    });

                    element.Add(bwSlider);
                    element.Add(qSlider);
                    break;
                case AudioProcessorSettingsObject.FilterTypes.FormantFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("formantSettings.vowel"), "Vowel", 1, 6, true, preview));
                    break;
                case AudioProcessorSettingsObject.FilterTypes.LadderFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("ladderSettings.oversampling"), "Oversampling", 1, 4, true,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("ladderSettings.cutoffFrequency"), "CutOff", 1, 24000, false,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("ladderSettings.resonance"), "Resonance", 0, 1, false,
                        preview));
                    break;
                case AudioProcessorSettingsObject.FilterTypes.BitcrushFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("bitcrushSettings.bitDepth"), "Bit Depth", 1, 24, false,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("bitcrushSettings.downsampling"), "Downsampling", 1, 100, true,
                        preview));
                    break;
                case AudioProcessorSettingsObject.FilterTypes.SaturatorFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("saturatorSettings.drive"), "Drive", 0, 10, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("saturatorSettings.wet"), "Wet", 0, 1, false, preview));
                    break;
                case AudioProcessorSettingsObject.FilterTypes.DelayFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("delaySettings.delayTime"), "Time", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("delaySettings.feedback"), "Feedback", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("delaySettings.wet"), "Wet", 0, 1, false, preview));
                    break;
                case AudioProcessorSettingsObject.FilterTypes.ChorusFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("chorusSettings.rate"), "Rate", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("chorusSettings.depth"), "Depth", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("chorusSettings.delay"), "Delay", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("chorusSettings.feedback"), "Feedback", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("chorusSettings.wet"), "Wet", 0, 1, false, preview));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            return element;
        }

        private static VisualElement CreateBoundSlider(SerializedProperty property, string label, float start, float end,
            bool isInt, FilterPreviewElement preview, Action<ChangeEvent<float>> onFloatChanged = null)
        {
            VisualElement slider;
            if (isInt)
            {
                var s = new SliderInt(label, (int)start, (int)end);
                s.labelElement.style.minWidth = 80;
                s.BindProperty(property);
                s.showInputField = true;
                s.RegisterValueChangedCallback(_ => preview.Refresh());
                slider = s;
            }
            else
            {
                var s = new Slider(label, start, end);
                s.labelElement.style.minWidth = 80;
                s.BindProperty(property);
                s.showInputField = true;
                s.RegisterValueChangedCallback(evt =>
                {
                    onFloatChanged?.Invoke(evt);
                    preview.Refresh();
                });
                slider = s;
            }

            return slider;
        }

       
    }
}