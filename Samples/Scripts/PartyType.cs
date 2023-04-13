using System;
using Anywhen;
using Samples.Scripts;
using UnityEngine;
using Random = UnityEngine.Random;

public class PartyType : MonoBehaviour
{
    private SamplePatternVisualizer _patternVisualizer;
    private Vector3 _onLookPosition, _inCirclePosition, _currentPositionTarget;
    private bool _noteOn;
    private int _currentStepIndex;
    private Renderer _objectRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;
    private float _wobbleSpeed;
    private float _wobbleAmount;
    private int _connectedStepIndex;
    public int ConnectedStepIndex => _connectedStepIndex;
    private Vector3 _currentScaleTarget;
    private DrumPatternMixer.InstrumentObject _instrumentObject;
    public DrumPatternMixer.InstrumentObject InstrumentObject => _instrumentObject;

    public void Init(int connectedStep, SamplePatternVisualizer visualizer,
        DrumPatternMixer.InstrumentObject connectedInstrument)
    {
        _connectedStepIndex = connectedStep;
        _patternVisualizer = visualizer;

        _inCirclePosition = visualizer.circlePositions[_connectedStepIndex];


        float offset = 0.3f;
        Vector3 rndDir = new Vector3(Random.Range(-offset, offset), 0, Random.Range(-offset, offset));
        _onLookPosition = Vector3.Slerp(_inCirclePosition.normalized, rndDir, 0.25f) * 12;


        _objectRenderer = GetComponent<Renderer>();
        _materialPropertyBlock = new MaterialPropertyBlock();
        _instrumentObject = connectedInstrument;
        SetColor(connectedInstrument.color);

        _connectedStepIndex = (int)Mathf.Repeat(connectedStep, 16);

        _currentStepIndex = _patternVisualizer.circleStepLength - connectedStep;
        _currentScaleTarget = Vector3.one * 0.4f;
        _currentPositionTarget = _onLookPosition;
        _wobbleSpeed = Random.Range(1, 3f);
        _wobbleAmount = Random.Range(0.05f, 0.2f);
    }

    private void SetColor(Color color)
    {
        _materialPropertyBlock.SetColor("_Color", color);
        _objectRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _currentScaleTarget, Time.deltaTime * 5);

        Vector3 wobbleAdd = Vector3.up * (Mathf.Sin(Time.time * _wobbleSpeed) * _wobbleAmount);
        transform.position = Vector3.Lerp(transform.position, _currentPositionTarget + wobbleAdd, Time.deltaTime * 5);
    }

    public void SetNoteOn(bool stepTrigger)
    {
        _noteOn = stepTrigger;
        _currentPositionTarget = stepTrigger ? _inCirclePosition : _onLookPosition;
        _currentScaleTarget = _noteOn ? Vector3.one * 0.4f : Vector3.one * 0.3f;
        _wobbleAmount = _noteOn ? Random.Range(0.1f, 0.2f) : 0.1f;
    }

    public void Tick()
    {
        _currentStepIndex = (int)Mathf.Repeat(_currentStepIndex, (int)_patternVisualizer.tickRate);

        if (_noteOn && (int)Mathf.Repeat(_currentStepIndex, (int)_patternVisualizer.tickRate) == 0)
        {
            transform.position += Vector3.up;
            transform.localScale = Vector3.one * 0.7f;
        }


        _currentStepIndex++;
    }
}