using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundTile : MonoBehaviour
{
    private MaterialPropertyBlock _materialPropertyBlock;
    public Renderer thisRenderer;
    private Color _initialColor;
    private Color _currentColor;
    public int Index => _index;
    private int _index;
    private void Start()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        _initialColor = thisRenderer.material.color;
    }

    public void Init(int index)
    {
        _index = index;
    }

    private void Update()
    {
        _currentColor = Color.Lerp(_currentColor, _initialColor, Time.deltaTime * 4);
        _materialPropertyBlock.SetColor("_Color", _currentColor);
        thisRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    public void Ping()
    {
        _currentColor = Color.yellow;
        
    }
}
