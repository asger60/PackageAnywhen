using System;
using Anywhen.Synth;
using Anywhen.Synth.Filter;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Synth
{
    public class AudioProcessorInspector : Editor
    {
        private class FilterPreviewElement : VisualElement
        {
            private AudioProcessorSettings _settings;
            private const int Resolution = 50;
            private readonly float[] _response = new float[Resolution];

            public FilterPreviewElement(AudioProcessorSettings settings)
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
                    case AudioProcessorSettings.FilterTypes.LowPassFilter:
                    case AudioProcessorSettings.FilterTypes.LadderFilter:
                    {
                        float cutoff = _settings.filterType == AudioProcessorSettings.FilterTypes.LowPassFilter
                            ? _settings.lowPassSettings.cutoffFrequency
                            : _settings.ladderSettings.cutoffFrequency;
                        float resonance = _settings.filterType == AudioProcessorSettings.FilterTypes.LowPassFilter
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
                    case AudioProcessorSettings.FilterTypes.BandPassFilter:
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
                    case AudioProcessorSettings.FilterTypes.FormantFilter:
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
                    case AudioProcessorSettings.FilterTypes.BitcrushFilter:
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
                    case AudioProcessorSettings.FilterTypes.SaturatorFilter:
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
                    case AudioProcessorSettings.FilterTypes.DelayFilter:
                    {
                        float wet = _settings.delaySettings.wet;

                        for (int i = 0; i < Resolution; i++)
                        {
                            _response[i] = Mathf.Clamp01(Mathf.Lerp(0.5f, 0.8f, wet));
                        }

                        break;
                    }
                    case AudioProcessorSettings.FilterTypes.ChorusFilter:
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
                    case AudioProcessorSettings.FilterTypes.ReverbFilter:
                    {
                        float roomSize = _settings.reverbSettings.roomSize;
                        float damping = _settings.reverbSettings.damping;

                        // Visualise as a decaying tail: flat pass-through → exponential rolloff,
                        // with damping softening the high-frequency end.
                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float t = i / (float)Resolution;

                            // Room size controls how long/flat the tail is before it drops
                            float tail = Mathf.Pow(roomSize, 1f - t);

                            // Damping rolls off highs — higher damping = steeper HF drop
                            float hfRolloff = 1f - damping * Mathf.Pow(t, 0.5f);

                            _response[i] = Mathf.Clamp01(tail * hfRolloff * 0.85f);
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
                if (_settings.filterType == AudioProcessorSettings.FilterTypes.LowPassFilter)
                    cutoff = _settings.lowPassSettings.cutoffFrequency;
                else if (_settings.filterType == AudioProcessorSettings.FilterTypes.LadderFilter)
                    cutoff = _settings.ladderSettings.cutoffFrequency;
                else if (_settings.filterType == AudioProcessorSettings.FilterTypes.BandPassFilter)
                    cutoff = _settings.bandPassSettings.frequency;
                else if (_settings.filterType == AudioProcessorSettings.FilterTypes.BitcrushFilter)
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

        public static VisualElement Draw(AudioProcessorSettings settings, Action onChanged = null)
        {
            VisualElement element = new VisualElement();

            var label = new Label(settings.filterType.ToString())
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            };
            element.Add(label);

            var preview = new FilterPreviewElement(settings);
            element.Add(preview);
            preview.Refresh();

            switch (settings.filterType)
            {
                case AudioProcessorSettings.FilterTypes.LowPassFilter:
                    element.Add(CreateBoundSliderInt("Oversampling", 1, 4,
                        () => settings.lowPassSettings.oversampling,
                        v => settings.lowPassSettings.oversampling = v, preview, onChanged));

                    element.Add(CreateBoundSlider("CutOff", 1, 24000,
                        () => settings.lowPassSettings.cutoffFrequency,
                        v => settings.lowPassSettings.cutoffFrequency = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Resonance", 0, 1,
                        () => settings.lowPassSettings.resonance,
                        v => settings.lowPassSettings.resonance = v, preview, onChanged));
                    element.Add(CreateModRoutingUI("Cutoff Mod",
                        () => settings.lowPassSettings.cutoffMod,
                        v => settings.lowPassSettings.cutoffMod = v,   // struct copy — assign back
                        preview, onChanged));
                    break;

                case AudioProcessorSettings.FilterTypes.BandPassFilter:
                    element.Add(CreateBoundSlider("Frequency", 1, 24000,
                        () => settings.bandPassSettings.frequency,
                        v => settings.bandPassSettings.frequency = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Bandwidth", 1, 10000,
                        () => settings.bandPassSettings.bandWidth,
                        v =>
                        {
                            settings.bandPassSettings.bandWidth = v;
                            settings.SyncBandPassFromBandwidth();
                        },
                        preview, onChanged));
                    element.Add(CreateBoundSlider("Q", 0.01f, 100,
                        () => settings.bandPassSettings.q,
                        v =>
                        {
                            settings.bandPassSettings.q = v;
                            settings.SyncBandPassFromQ();
                        },
                        preview, onChanged));
                    break;

                case AudioProcessorSettings.FilterTypes.FormantFilter:
                    element.Add(CreateBoundSliderInt("Vowel", 1, 6,
                        () => settings.formantSettings.vowel,
                        v => settings.formantSettings.vowel = v, preview, onChanged));
                    break;

                case AudioProcessorSettings.FilterTypes.LadderFilter:
                    element.Add(CreateBoundSliderInt("Oversampling", 1, 4,
                        () => settings.ladderSettings.oversampling,
                        v => settings.ladderSettings.oversampling = v, preview, onChanged));
                    element.Add(CreateBoundSlider("CutOff", 1, 24000,
                        () => settings.ladderSettings.cutoffFrequency,
                        v => settings.ladderSettings.cutoffFrequency = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Resonance", 0, 1,
                        () => settings.ladderSettings.resonance,
                        v => settings.ladderSettings.resonance = v, preview, onChanged));
                    element.Add(CreateModRoutingUI("Cutoff Mod",
                        () => settings.ladderSettings.cutoffMod,
                        v => settings.ladderSettings.cutoffMod = v,   // struct copy — assign back
                        preview, onChanged));
                    break;

                case AudioProcessorSettings.FilterTypes.BitcrushFilter:
                    element.Add(CreateBoundSlider("Bit Depth", 1, 24,
                        () => settings.bitcrushSettings.bitDepth,
                        v => settings.bitcrushSettings.bitDepth = v, preview, onChanged));
                    element.Add(CreateBoundSliderInt("Downsampling", 1, 100,
                        () => settings.bitcrushSettings.downsampling,
                        v => settings.bitcrushSettings.downsampling = v, preview, onChanged));
                    break;

                case AudioProcessorSettings.FilterTypes.SaturatorFilter:
                    element.Add(CreateBoundSlider("Drive", 0, 10,
                        () => settings.saturatorSettings.drive,
                        v => settings.saturatorSettings.drive = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Wet", 0, 1,
                        () => settings.saturatorSettings.wet,
                        v => settings.saturatorSettings.wet = v, preview, onChanged));
                    break;

                case AudioProcessorSettings.FilterTypes.DelayFilter:
                    element.Add(CreateBoundSlider("Time", 0, 1,
                        () => settings.delaySettings.delayTime,
                        v => settings.delaySettings.delayTime = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Feedback", 0, 1,
                        () => settings.delaySettings.feedback,
                        v => settings.delaySettings.feedback = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Wet", 0, 1,
                        () => settings.delaySettings.wet,
                        v => settings.delaySettings.wet = v, preview, onChanged));
                    break;

                case AudioProcessorSettings.FilterTypes.ChorusFilter:
                    element.Add(CreateBoundSlider("Rate", 0, 1,
                        () => settings.chorusSettings.rate,
                        v => settings.chorusSettings.rate = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Depth", 0, 1,
                        () => settings.chorusSettings.depth,
                        v => settings.chorusSettings.depth = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Delay", 0, 1,
                        () => settings.chorusSettings.delay,
                        v => settings.chorusSettings.delay = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Feedback", 0, 1,
                        () => settings.chorusSettings.feedback,
                        v => settings.chorusSettings.feedback = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Wet", 0, 1,
                        () => settings.chorusSettings.wet,
                        v => settings.chorusSettings.wet = v, preview, onChanged));

                    break;
                case AudioProcessorSettings.FilterTypes.ReverbFilter:
                    element.Add(CreateBoundSlider("Room Size", 0, 1,
                        () => settings.reverbSettings.roomSize,
                        v => settings.reverbSettings.roomSize = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Damping", 0, 1,
                        () => settings.reverbSettings.damping,
                        v => settings.reverbSettings.damping = v, preview, onChanged));
                    element.Add(CreateBoundSlider("Wet", 0, 1,
                        () => settings.reverbSettings.wet,
                        v => settings.reverbSettings.wet = v, preview, onChanged));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return element;
        }

        private static VisualElement CreateBoundSlider(string label, float start, float end,
            Func<float> getter, Action<float> setter, FilterPreviewElement preview, Action onChanged)
        {
            var s = new Slider(label, start, end);
            s.labelElement.style.minWidth = 80;
            s.showInputField = true;
            s.value = getter();
            s.RegisterValueChangedCallback(evt =>
            {
                setter(evt.newValue);
                preview.Refresh();
                onChanged?.Invoke(); // <---
            });
            return s;
        }

        private static VisualElement CreateBoundSliderInt(string label, int start, int end,
            Func<int> getter, Action<int> setter, FilterPreviewElement preview, Action onChanged)
        {
            var s = new SliderInt(label, start, end);
            s.labelElement.style.minWidth = 80;
            s.showInputField = true;
            s.value = getter();
            s.RegisterValueChangedCallback(evt =>
            {
                setter(evt.newValue);
                preview.Refresh();
                onChanged?.Invoke();
            });
            return s;
        }

        private static VisualElement CreateModRoutingUI(
            string label,
            Func<SynthFilterBase.ModRouting[]> getter,
            Action<SynthFilterBase.ModRouting[]> setter,
            FilterPreviewElement preview,
            Action onChanged)
        {
            var root = new VisualElement();
            root.style.marginTop = 4;

            var header = new Label(label)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 }
            };
            root.Add(header);

            // Container that we rebuild whenever the array changes
            var listContainer = new VisualElement();
            root.Add(listContainer);

            void Rebuild()
            {
                listContainer.Clear();
                var routes = getter() ?? Array.Empty<SynthFilterBase.ModRouting>();

                for (int i = 0; i < routes.Length; i++)
                {
                    int idx = i; // capture for lambdas

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 2;

                    // Source enum picker
                    var sourceField = new EnumField(routes[idx].modSource);
                    sourceField.style.minWidth = 100;
                    sourceField.style.flexGrow = 1;
                    sourceField.RegisterValueChangedCallback(evt =>
                    {
                        var arr = getter();
                        arr[idx].modSource = (SynthFilterBase.ModRouting.ModSources)evt.newValue;
                        setter(arr);
                        preview.Refresh();
                        onChanged?.Invoke();
                    });

                    // Amount slider  (-1 → +1)
                    var amountSlider = new Slider("Amt", -1f, 1f);
                    amountSlider.showInputField = true;
                    amountSlider.style.flexGrow = 2;
                    amountSlider.labelElement.style.minWidth = 28;
                    amountSlider.value = routes[idx].modAmount;
                    amountSlider.RegisterValueChangedCallback(evt =>
                    {
                        var arr = getter();
                        arr[idx].modAmount = evt.newValue;
                        setter(arr);
                        preview.Refresh();
                        onChanged?.Invoke();
                    });

                    // Remove button
                    var removeBtn = new Button(() =>
                    {
                        var arr = getter();
                        var list = new System.Collections.Generic.List<SynthFilterBase.ModRouting>(arr);
                        list.RemoveAt(idx);
                        setter(list.ToArray());
                        Rebuild();
                        preview.Refresh();
                        onChanged?.Invoke();
                    }) { text = "✕" };
                    removeBtn.style.width = 20;
                    removeBtn.style.paddingLeft = 0;
                    removeBtn.style.paddingRight = 0;

                    row.Add(sourceField);
                    row.Add(amountSlider);
                    row.Add(removeBtn);
                    listContainer.Add(row);
                }

                // Add button
                var addBtn = new Button(() =>
                {
                    var arr = getter() ?? Array.Empty<SynthFilterBase.ModRouting>();
                    var list = new System.Collections.Generic.List<SynthFilterBase.ModRouting>(arr);
                    list.Add(new SynthFilterBase.ModRouting()); // default source + 0 amount
                    setter(list.ToArray());
                    Rebuild();
                    preview.Refresh();
                    onChanged?.Invoke();
                }) { text = "+ Add Mod" };
                addBtn.style.marginTop = 2;
                listContainer.Add(addBtn);
            }

            Rebuild();
            return root;
        }
    }
}