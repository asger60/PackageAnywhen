using System;
using System.Collections;
using System.Collections.Generic;
using Samples.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GodHand : MonoBehaviour
{
    public Camera thisCamera;
    public LayerMask layerMask;
    RaycastHit _hit;
    public int currentPattern;

    public DrumPatternMixer drumPatternMixer;
    private float _lastMixTime;

    public Button[] fillButtons;
    public Renderer thisRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;
    public RainDrop rainPrefab;
    private Color _currentColor;
    private void Start()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        for (int i = 0; i < fillButtons.Length; i++)
        {
            var i1 = i;
            fillButtons[i].onClick.AddListener(() =>
            {
                currentPattern = i1;
                _materialPropertyBlock.SetColor("_Color", fillButtons[i1].image.color);
                thisRenderer.SetPropertyBlock(_materialPropertyBlock);
                _currentColor = fillButtons[i1].image.color;
            });
        }
    }

    void Update()
    {
        //Cursor.visible = false;

        Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition - Vector3.up * 150);

        if (Physics.Raycast(ray, out _hit, 100, layerMask))
        {
            transform.position = _hit.point + Vector3.up * 6;
        }

        if (Input.mousePosition.y > 100 && Input.GetMouseButton(0) && _lastMixTime + 0.1f < Time.time)
        {
            drumPatternMixer.Mix(currentPattern);
            _lastMixTime = Time.time;
            var rainDrop = Instantiate(rainPrefab, transform.position, Quaternion.identity);
            rainDrop.Init(_currentColor);
        }
    }
}