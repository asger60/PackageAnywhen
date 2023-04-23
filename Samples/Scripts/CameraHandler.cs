using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    private Vector3 _inGamePosition;
    private Vector3 _menuPosition;
    private Vector3 _positionTarget;

    private void Start()
    {
        _menuPosition = transform.position;

        _inGamePosition = _menuPosition - Vector3.right * 1.5f;
        
        _positionTarget = _inGamePosition;
    }

    public void SetIsInGame(bool state)
    {
        _positionTarget = state ? _inGamePosition : _menuPosition;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _positionTarget, Time.deltaTime * 10);
    }
}
