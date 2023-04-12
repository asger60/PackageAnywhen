using System;
using Samples.Scripts;
using UnityEngine;
using Random = UnityEngine.Random;

public class PartyType : MonoBehaviour
{
    private int _stepIndex;
    private SamplePatternVisualizer _patternVisualizer;
    private Vector3 _onLookPosition, _inCirclePosition, _currentPositionTarget;
    public void Init(int index, SamplePatternVisualizer visualizer)
    {
        _stepIndex = index;
        _patternVisualizer = visualizer;
        var rnd = Random.onUnitSphere;
        rnd.y = 0;
        _inCirclePosition = visualizer.circlePositions[_stepIndex];

        _onLookPosition = _inCirclePosition.normalized * 10 + (rnd * 0.75f);
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _currentPositionTarget, Time.deltaTime * 6);
    }

    public void SetNoteOn(bool stepTrigger)
    {
        _currentPositionTarget = stepTrigger ? _inCirclePosition : _onLookPosition;
    }
}
