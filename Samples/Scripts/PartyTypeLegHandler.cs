using System;
using UnityEngine;

public class PartyTypeLegHandler : MonoBehaviour
{
    [Serializable]
    public class Leg
    {
        public Transform footTransform;
        public LineRenderer lineRenderer;
        private Vector3[] _linePositions = new Vector3[3];
        public Vector3 attachLocalPosition;
        private Transform _transform;
        private float _legStretch;

        public enum LegDirections
        {
            Left,
            Right
        }

        public LegDirections legDirection;

        public void Init(Transform attachedTo, float legStretch)
        {
            _transform = attachedTo;
            lineRenderer.positionCount = _linePositions.Length;
            _legStretch = legStretch;
        }

        public void Update()
        {
            var legDir = legDirection == LegDirections.Right ? _transform.right : _transform.right * -1;
            var footPosition = _transform.position + (legDir * _legStretch);
            footPosition.y = 0;
            //footTransform.position = footPosition;
            _linePositions[0] = _transform.TransformPoint(attachLocalPosition);
            _linePositions[2] = footTransform.position;
            _linePositions[1] = Vector3.Lerp(_linePositions[0] + (legDir * (_legStretch * .5f)), _linePositions[2],
                0.5f);
            lineRenderer.SetPositions(_linePositions);
            footTransform.SetParent(null);
        }

        public void Step(Vector3 direction)
        {
            var nextFootPos = _transform.position + direction * 0.5f;
            nextFootPos.y = 0;
            footTransform.position = nextFootPos;
        }

        public void Stand()
        {
            var legDir = legDirection == LegDirections.Right ? _transform.right : _transform.right * -1;
            var footPosition = _transform.position + (legDir * _legStretch);
            footPosition.y = 0;
            footTransform.position = footPosition;
        }
    }

    public Leg[] legs;

    public float legStretch;
    private float _moveMagnitude = 0.5f;
    private Vector3 _lastPosition;
    private int _currentLegIndex;
    private float _noMoveTimer;

    private void Start()
    {
        _lastPosition = transform.position;
        foreach (var leg in legs)
        {
            leg.Init(transform, legStretch);
        }
    }

    private void Update()
    {
        foreach (var leg in legs)
        {
            leg.Update();
        }

        var thisPos = transform.position;
        thisPos.y = 0;
        var moveDir = (thisPos - _lastPosition);
        if (moveDir.magnitude > _moveMagnitude)
        {
            GetLeg().Step(moveDir.normalized);
            _lastPosition = transform.position;
            _lastPosition.y = 0;
            _noMoveTimer = 0;
        }
        else
        {
            _noMoveTimer += Time.deltaTime;
            if (_noMoveTimer > 0.25f)
            {
                GetLeg().Stand();
                _noMoveTimer = 0;
            }
        }
    }

    Leg GetLeg()
    {
        _currentLegIndex++;
        _currentLegIndex = (int)Mathf.Repeat(_currentLegIndex, 2);
        return legs[_currentLegIndex];
    }
}