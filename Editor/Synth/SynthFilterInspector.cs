using System;
using Anywhen.Synth;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Synth
{
    public class SynthFilterInspector : Editor
    {
        private class FilterPreviewElement : VisualElement
        {
            private SynthSettingsObjectFilter _settings;
            private const int Resolution = 50;
            private readonly float[] _response = new float[Resolution];

            public FilterPreviewElement(SynthSettingsObjectFilter settings)
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
                // Simplified linear approximations for preview
                switch (_settings.filterType)
                {
                    case SynthSettingsObjectFilter.FilterTypes.LowPassFilter:
                    case SynthSettingsObjectFilter.FilterTypes.LadderFilter:
                    {
                        float cutoff = _settings.filterType == SynthSettingsObjectFilter.FilterTypes.LowPassFilter
                            ? _settings.lowPassSettings.cutoffFrequency
                            : _settings.ladderSettings.cutoffFrequency;
                        float resonance = _settings.filterType == SynthSettingsObjectFilter.FilterTypes.LowPassFilter
                            ? _settings.lowPassSettings.resonance
                            : _settings.ladderSettings.resonance;

                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float x = freq / cutoff;
                            // 4-pole lowpass approx: 1 / (1 + x^4)
                            float mag = 1.0f / Mathf.Sqrt(1.0f + Mathf.Pow(x, 8.0f));
                            // Simple resonance peak approx
                            if (resonance > 0)
                            {
                                float peak = resonance * 3.0f * Mathf.Exp(-Mathf.Pow(x - 1.0f, 2.0f) * 10.0f);
                                mag += peak;
                            }

                            // Avoid clipping the peak by scaling down the whole signal if it exceeds 1
                            _response[i] = mag / (1.0f + resonance * 2.0f);
                        }

                        break;
                    }
                    case SynthSettingsObjectFilter.FilterTypes.BandPassFilter:
                    {
                        float center = _settings.bandPassSettings.frequency;
                        float width = _settings.bandPassSettings.bandWidth;
                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float x = freq / center;
                            // Bandpass approx
                            float q = 100.0f / Mathf.Max(width, 1.0f);
                            float mag = 1.0f / Mathf.Sqrt(1.0f + Mathf.Pow(q * (x - 1.0f / x), 2.0f));
                            _response[i] = Mathf.Clamp01(mag);
                        }

                        break;
                    }
                    case SynthSettingsObjectFilter.FilterTypes.FormantFilter:
                    {
                        // Formant filters have multiple peaks. For preview we'll just show a generic "vowel" shape or just 3 peaks.
                        // Ideally we'd pull these from SynthFilterFormant but it's internal.
                        // Let's just draw some peaks based on vowel index.
                        int vowel = _settings.formantSettings.vowel;
                        for (int i = 0; i < Resolution; i++)
                        {
                            float freq = LogScale(i / (float)Resolution, 20, 20000);
                            float mag = 0;
                            // Fake some formant peaks
                            float[] peaks = GetVowelPeaks(vowel);
                            foreach (var p in peaks)
                            {
                                mag += 0.5f * Mathf.Exp(-Mathf.Pow((freq - p) / (p * 0.2f), 2.0f));
                            }

                            _response[i] = Mathf.Clamp01(mag);
                        }

                        break;
                    }
                }
            }

            private float[] GetVowelPeaks(int vowel)
            {
                // Rough estimates of F1, F2, F3 for a few vowels
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

                // Draw cutoff line if applicable
                float cutoff = -1;
                if (_settings.filterType == SynthSettingsObjectFilter.FilterTypes.LowPassFilter)
                    cutoff = _settings.lowPassSettings.cutoffFrequency;
                else if (_settings.filterType == SynthSettingsObjectFilter.FilterTypes.LadderFilter)
                    cutoff = _settings.ladderSettings.cutoffFrequency;
                else if (_settings.filterType == SynthSettingsObjectFilter.FilterTypes.BandPassFilter)
                    cutoff = _settings.bandPassSettings.frequency;

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

        public static VisualElement Draw(SynthSettingsObjectFilter settings)
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
                case SynthSettingsObjectFilter.FilterTypes.LowPassFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("lowPassSettings.oversampling"), "Oversampling", 1, 4, true,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("lowPassSettings.cutoffFrequency"), "CutOff", 1, 24000, false,
                        preview));
                    element.Add(
                        CreateBoundSlider(so.FindProperty("lowPassSettings.resonance"), "Resonance", 0, 1, false, preview));
                    break;
                case SynthSettingsObjectFilter.FilterTypes.BandPassFilter:
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
                case SynthSettingsObjectFilter.FilterTypes.FormantFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("formantSettings.vowel"), "Vowel", 1, 6, true, preview));
                    break;
                case SynthSettingsObjectFilter.FilterTypes.LadderFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("ladderSettings.oversampling"), "Oversampling", 1, 4, true,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("ladderSettings.cutoffFrequency"), "CutOff", 1, 24000, false,
                        preview));
                    element.Add(CreateBoundSlider(so.FindProperty("ladderSettings.resonance"), "Resonance", 0, 1, false,
                        preview));
                    break;
                case SynthSettingsObjectFilter.FilterTypes.BitcrushFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("bitcrushSettings.bitDepth"), "Bit Depth", 1, 24, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("bitcrushSettings.downsampling"), "Downsampling", 1, 100, true, preview));
                    break;
                case SynthSettingsObjectFilter.FilterTypes.SaturatorFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("saturatorSettings.drive"), "Drive", 0, 10, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("saturatorSettings.wet"), "Wet", 0, 1, false, preview));
                    break;
                case SynthSettingsObjectFilter.FilterTypes.DelayFilter:
                    element.Add(CreateBoundSlider(so.FindProperty("delaySettings.delayTime"), "Time", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("delaySettings.feedback"), "Feedback", 0, 1, false, preview));
                    element.Add(CreateBoundSlider(so.FindProperty("delaySettings.wet"), "Wet", 0, 1, false, preview));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var prop = so.FindProperty("modRouting");
            var propertyField = new PropertyField(prop, "Mods");
            propertyField.BindProperty(prop);
            element.Add(propertyField);
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

        public static void Draw(SynthSettingsInspector parent, SynthSettingsObjectFilter settings)
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                parent.DeleteElement<SynthSettingsObjectFilter>(settings, "filterSettings");
                parent.RebuildSynth();
            }

            GUILayout.EndHorizontal();


            var newFilterType =
                (SynthSettingsObjectFilter.FilterTypes)EditorGUILayout.EnumPopup("Filter type:", settings.filterType);

            if (newFilterType != settings.filterType)
            {
                settings.filterType = newFilterType;
                parent.RebuildSynth();
            }

            // Preview
            var rect = GUILayoutUtility.GetRect(10, 100);
            DrawFilterPreview(rect, settings);

            switch (settings.filterType)
            {
                case SynthSettingsObjectFilter.FilterTypes.LowPassFilter:

                    settings.lowPassSettings.oversampling =
                        EditorGUILayout.IntSlider("Oversampling", settings.lowPassSettings.oversampling, 1, 4);
                    settings.lowPassSettings.cutoffFrequency =
                        EditorGUILayout.Slider("CutOff", settings.lowPassSettings.cutoffFrequency, 1, 24000);
                    settings.lowPassSettings.resonance =
                        EditorGUILayout.Slider("Resonance", settings.lowPassSettings.resonance, 0, 1);
                    break;
                case SynthSettingsObjectFilter.FilterTypes.BandPassFilter:
                    float oldFreq = settings.bandPassSettings.frequency;
                    settings.bandPassSettings.frequency = EditorGUILayout.Slider("Frequency",
                        settings.bandPassSettings.frequency, 1, 24000);
                    if (oldFreq != settings.bandPassSettings.frequency)
                    {
                        settings.SyncBandPassFromQ();
                    }

                    float oldBW = settings.bandPassSettings.bandWidth;
                    settings.bandPassSettings.bandWidth = EditorGUILayout.Slider("Bandwidth",
                        settings.bandPassSettings.bandWidth, 1, 10000);
                    if (oldBW != settings.bandPassSettings.bandWidth)
                    {
                        settings.SyncBandPassFromBandwidth();
                    }

                    float oldQ = settings.bandPassSettings.q;
                    settings.bandPassSettings.q = EditorGUILayout.Slider("Q",
                        settings.bandPassSettings.q, 0.01f, 100);
                    if (oldQ != settings.bandPassSettings.q)
                    {
                        settings.SyncBandPassFromQ();
                    }

                    break;
                case SynthSettingsObjectFilter.FilterTypes.FormantFilter:
                    settings.formantSettings.vowel = EditorGUILayout.IntSlider("Vowel",
                        settings.formantSettings.vowel, 1, 6);
                    break;
                case SynthSettingsObjectFilter.FilterTypes.LadderFilter:
                    settings.ladderSettings.oversampling = EditorGUILayout.IntSlider("Oversampling",
                        settings.ladderSettings.oversampling, 1, 4);
                    settings.ladderSettings.cutoffFrequency =
                        EditorGUILayout.Slider("CutOff", settings.ladderSettings.cutoffFrequency, 1, 24000);
                    settings.ladderSettings.resonance =
                        EditorGUILayout.Slider("Resonance", settings.ladderSettings.resonance, 0, 1);
                    break;
                case SynthSettingsObjectFilter.FilterTypes.BitcrushFilter:
                    settings.bitcrushSettings.bitDepth =
                        EditorGUILayout.Slider("Bit Depth", settings.bitcrushSettings.bitDepth, 1, 24);
                    settings.bitcrushSettings.downsampling =
                        EditorGUILayout.IntSlider("Downsampling", settings.bitcrushSettings.downsampling, 1, 100);
                    break;
                case SynthSettingsObjectFilter.FilterTypes.SaturatorFilter:
                    settings.saturatorSettings.drive =
                        EditorGUILayout.Slider("Drive", settings.saturatorSettings.drive, 0, 10);
                    settings.saturatorSettings.wet =
                        EditorGUILayout.Slider("Wet", settings.saturatorSettings.wet, 0, 1);
                    break;
                case SynthSettingsObjectFilter.FilterTypes.DelayFilter:
                    settings.delaySettings.delayTime =
                        EditorGUILayout.Slider("Time", settings.delaySettings.delayTime, 0, 1);
                    settings.delaySettings.feedback =
                        EditorGUILayout.Slider("Feedback", settings.delaySettings.feedback, 0, 1);
                    settings.delaySettings.wet =
                        EditorGUILayout.Slider("Wet", settings.delaySettings.wet, 0, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private static void DrawFilterPreview(Rect rect, SynthSettingsObjectFilter settings)
        {
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));
            Handles.color = Color.grey;
            const int resolution = 50;
            Vector3[] points = new Vector3[resolution];

            float cutoff = -1;
            float resonance = 0;
            float bandWidth = 0;
            float q = 0;

            switch (settings.filterType)
            {
                case SynthSettingsObjectFilter.FilterTypes.LowPassFilter:
                    cutoff = settings.lowPassSettings.cutoffFrequency;
                    resonance = settings.lowPassSettings.resonance;
                    break;
                case SynthSettingsObjectFilter.FilterTypes.LadderFilter:
                    cutoff = settings.ladderSettings.cutoffFrequency;
                    resonance = settings.ladderSettings.resonance;
                    break;
                case SynthSettingsObjectFilter.FilterTypes.BandPassFilter:
                    cutoff = settings.bandPassSettings.frequency;
                    bandWidth = settings.bandPassSettings.bandWidth;
                    q = settings.bandPassSettings.q;
                    break;
            }

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1);
                float freq = 20 * Mathf.Pow(20000 / 20f, t);
                float mag = 0;

                switch (settings.filterType)
                {
                    case SynthSettingsObjectFilter.FilterTypes.LowPassFilter:
                    case SynthSettingsObjectFilter.FilterTypes.LadderFilter:
                    {
                        float x = freq / cutoff;
                        mag = 1.0f / Mathf.Sqrt(1.0f + Mathf.Pow(x, 8.0f));
                        if (resonance > 0)
                            mag += resonance * 3.0f * Mathf.Exp(-Mathf.Pow(x - 1.0f, 2.0f) * 10.0f);
                        mag /= (1.0f + resonance * 2.0f);
                        break;
                    }
                    case SynthSettingsObjectFilter.FilterTypes.BandPassFilter:
                    {
                        float x = freq / cutoff;
                        // q = 100.0f / Mathf.Max(bandWidth, 1.0f);
                        mag = 1.0f / Mathf.Sqrt(1.0f + Mathf.Pow(q * (x - 1.0f / x), 2.0f));
                        break;
                    }
                    case SynthSettingsObjectFilter.FilterTypes.FormantFilter:
                    {
                        float[] peaks = GetVowelPeaksStatic(settings.formantSettings.vowel);
                        foreach (var p in peaks)
                            mag += 0.5f * Mathf.Exp(-Mathf.Pow((freq - p) / (p * 0.2f), 2.0f));
                        break;
                    }
                }

                mag = Mathf.Clamp01(mag);
                points[i] = new Vector3(rect.x + t * rect.width,
                    rect.y + rect.height - (mag * rect.height * 0.8f) - rect.height * 0.1f, 0);
            }

            Handles.DrawAAPolyLine(1, points);

            if (cutoff > 0)
            {
                float tCutoff = Mathf.Log(cutoff / 20f) / Mathf.Log(20000f / 20f);
                float lx = rect.x + Mathf.Clamp01(tCutoff) * rect.width;
                Handles.color = new Color(1, 1, 1, 0.3f);
                Handles.DrawLine(new Vector3(lx, rect.y, 0), new Vector3(lx, rect.y + rect.height, 0));
            }
        }

        private static float[] GetVowelPeaksStatic(int vowel)
        {
            switch (vowel)
            {
                case 1: return new float[] { 270, 2290, 3010 };
                case 2: return new float[] { 390, 1990, 2550 };
                case 3: return new float[] { 730, 1090, 2440 };
                case 4: return new float[] { 570, 840, 2410 };
                case 5: return new float[] { 300, 870, 2240 };
                default: return new float[] { 500, 1500, 2500 };
            }
        }
    }
}