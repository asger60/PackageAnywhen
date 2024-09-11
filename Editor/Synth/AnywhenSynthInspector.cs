// Copyright (c) 2018 Jakob Schmid
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE."

using Anywhen.Synth;
using UnityEditor;
using UnityEngine;

namespace Editor.Synth
{
    [CustomEditor(typeof(AnywhenSynth)), CanEditMultipleObjects]
    public class AnywhenSynthInspector : UnityEditor.Editor
    {
        private enum OscilloscopeMode
        {
            None = 1,
            Small = 2,
            Large = 3
        }

        Texture2D tex = null;
        int t = 0;
        const int bufSize = 1024;
        private float[] testBuf = null;

        float[] bufCopy = null;
        //string[] sourceNames = null;
        //string[] targetNames = null;
        //float[] testMatrix = new float[8 * 8];

        /// Static Cache
        private static OscilloscopeMode oscilloscopeMode = OscilloscopeMode.None;


      

        private void RenderBuffer(float[] buf, ref Texture2D tex, int width, int height, int stride)
        {
            if (tex == null || tex.width != width || tex.height != height)
            {
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
            }

            // Check zero crossing
            float valueOld = 0.0f;
            int offset = 0;
            for (int i = 0; i < bufSize; ++i)
            {
                float valueNew = buf[i * stride];
                if (valueOld < 0 && valueNew > 0)
                {
                    offset = i;
                    break;
                }

                valueOld = valueNew;
            }

            Color col = Color.green;
            float yScale = 1.0f / height;
            float lineFocus = height * 0.3f;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float yNorm = y * yScale * 2.0f - 1.0f; // [-1;+1]
                    float oscValue = -1.0f;
                    if ((x + offset) < bufSize)
                    {
                        oscValue = buf[(x + offset) * stride]; // stereo interleaved
                    }

                    float intensity = Mathf.Pow(1.0f - Mathf.Abs(oscValue - yNorm), lineFocus);
                    col = new Color(intensity, intensity, intensity);
                    tex.SetPixel(x, y, col);
                }

                t++;
            }

            tex.Apply(false);
        }

        // Sources are vertical, targets are horizontal
        private void ModulationMatrix(float[] matrix, string[] sources, string[] targets)
        {
            const int guiWidth = 34;
            int width = targets.Length;
            int height = sources.Length;
            Debug.Assert(matrix.Length >= width * height);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("src/tgt", GUILayout.Width(guiWidth));
            for (int x = 0; x < width; ++x)
            {
                GUILayout.Label(targets[x], GUILayout.Width(guiWidth));
            }

            EditorGUILayout.EndHorizontal();
            for (int y = 0; y < height; ++y)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(sources[y], GUILayout.Width(guiWidth));
                for (int x = 0; x < width; ++x)
                {
                    matrix[y * width + x] =
                        EditorGUILayout.FloatField(matrix[y * width + x], GUILayout.Width(guiWidth));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return oscilloscopeMode != OscilloscopeMode.None;
        }
    }
}