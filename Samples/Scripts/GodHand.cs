using System;
using PackageAnywhen.Samples.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

public class GodHand : MonoBehaviour
{
    public Camera thisCamera;
    public LayerMask layerMask;
    RaycastHit _hit;
    public int currentPattern;

    [FormerlySerializedAs("particleSystem")]
    public ParticleSystem thisParticleSystem;

    private float _lastMixTime;

    public Renderer thisRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;
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
        var em = thisParticleSystem.main;
        em.startColor = color;
    }

    private bool _didMix;

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
            gfxObject.transform.localScale = Vector3.one;
            _didMix = true;
        }

        if (!Input.GetMouseButton(0) && _didMix)
        {
            TrackHandler.Instance.OnMixDone();
            _didMix = false;
        }

        if (!isHidden && Input.GetMouseButton(0))
        {
            if (!_particleEmitting)
            {
                thisParticleSystem.Play();
                _particleEmitting = true;
            }
        }
        else
        {
            thisParticleSystem.Stop();
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