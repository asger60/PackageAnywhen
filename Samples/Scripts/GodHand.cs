using System;
using System.Collections;
using System.Collections.Generic;
using PackageAnywhen.Samples.Scripts;
using Samples.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GodHand : MonoBehaviour
{
    public Camera thisCamera;
    public LayerMask layerMask;
    RaycastHit _hit;
    public int currentPattern;


    private float _lastMixTime;

    public Renderer thisRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;
    public RainDrop rainPrefab;
    private Color _currentColor;
    public GameObject gfxObject;

    private static GodHand _instance;
    public static GodHand Instance => _instance;

    private void Awake()
    {
        _instance = this;
        _materialPropertyBlock = new MaterialPropertyBlock();
    }


    public void SetFillIndex(int index, Color color)
    {
        currentPattern = index;
        _materialPropertyBlock.SetColor("_Color", color);
        thisRenderer.SetPropertyBlock(_materialPropertyBlock);
        _currentColor = color;
    }

    void Update()
    {
        gfxObject.transform.localScale =
            Vector3.Lerp(gfxObject.transform.localScale, Vector3.one * 0.7f, Time.deltaTime * 10);

        bool isHidden = Input.mousePosition.y < 100 || Input.mousePosition.y > Screen.height - 100 ||
                        Input.mousePosition.x < 200;
        Cursor.visible = isHidden;
        gfxObject.SetActive(!isHidden);

        Ray ray = thisCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out _hit, 100, layerMask))
        {
            transform.position = _hit.point;
        }

        if (!isHidden && Input.GetMouseButton(0) && _lastMixTime + 0.1f < Time.time)
        {
            Vector2 direction = new Vector2(transform.position.x, transform.position.z).normalized;
            float angle = Mathf.Repeat(Vector2.SignedAngle(Vector2.left, direction) * -1, 360);
            TrackHandler.Instance.Mix(currentPattern, (int)(angle / 360 * 32));
            _lastMixTime = Time.time;
            var rainDrop = Instantiate(rainPrefab, transform.position, Quaternion.identity);
            rainDrop.Init(_currentColor);
            gfxObject.transform.localScale = Vector3.one;
        }
    }

    public void SetIsActive(bool state)
    {
        gameObject.SetActive(state);
        Cursor.visible = !state;
    }
}