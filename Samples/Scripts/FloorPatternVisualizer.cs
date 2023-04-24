using System.Collections.Generic;
using Anywhen;
using UnityEngine;

namespace Samples.Scripts
{
    public class FloorPatternVisualizer : MonoBehaviour
    {
        public Vector3[] circlePositions = new Vector3[16];

        public int circleStepLength = 16;
        public int ringCount;

        public AnywhenMetronome.TickRate tickRate;
        public GroundTile groundTilePrefab;
        private List<GroundTile> _groundTiles = new List<GroundTile>();

        private void Start()
        {
            for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
            {
                GeneratePositions(3 + ringIndex);
                for (var i = 0; i < 16; i++)
                {
                    var groundObject = Instantiate(groundTilePrefab, transform);
                    groundObject.transform.position = circlePositions[i];
                    groundObject.Init(i);
                    _groundTiles.Add(groundObject);
                }
            }


            //AnywhenMetronome.Instance.OnTick16 += OnTick16;
        }

        //private void OnTick16()
        //{
        //    foreach (var groundTile in _groundTiles)
        //    {
        //        if (groundTile.Index == AnywhenMetronome.Instance.Sub16)
        //            groundTile.Ping();
        //    }
        //}


        [ContextMenu("generate positions")]
        void GeneratePositions(float circleDistance)
        {
            circlePositions = new Vector3[(int)tickRate];
            for (int i = 0; i < circlePositions.Length; i++)
            {
                var x = (circleDistance * Mathf.Cos((i / (float)(int)tickRate * 360) / (180f / Mathf.PI)));
                var z = (circleDistance * Mathf.Sin((i / (float)(int)tickRate * 360) / (180f / Mathf.PI)));

                circlePositions[i] = new Vector3(-x, 0, z);
            }
        }
    }
}