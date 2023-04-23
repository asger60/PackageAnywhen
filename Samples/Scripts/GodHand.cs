using System;
using PackageAnywhen.Samples.Scripts;
using UnityEngine;

public class GodHand : MonoBehaviour
{
    public Camera thisCamera;
    public LayerMask layerMask;
    RaycastHit _hit;
    public int currentPattern;
    public ParticleSystem particleSystem;

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

        var em = particleSystem.main;
        em.startColor = color;
    }

    void Update()
    {
        gfxObject.transform.localScale =
            Vector3.Lerp(gfxObject.transform.localScale, Vector3.one * 0.7f, Time.deltaTime * 10);

        float relativeMousePos = Input.mousePosition.x / Screen.width;
        bool isHidden = relativeMousePos < 0.15f || relativeMousePos > 0.93f;
        
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
            //var rainDrop = Instantiate(rainPrefab, transform.position, Quaternion.identity);
            //rainDrop.Init(_currentColor);
            gfxObject.transform.localScale = Vector3.one;
        }

        if (!isHidden && Input.GetMouseButton(0))
        {
            if (!_particleEmitting)
            {
                particleSystem.Play();
                _particleEmitting = true;
            }
        }
        else
        {
            particleSystem.Stop();
            _particleEmitting = false;
        }
    }

    private bool _particleEmitting;

    public void SetIsActive(bool state)
    {
        gameObject.SetActive(state);
        Cursor.visible = !state;
    }
}