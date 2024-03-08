using System;
using UnityEngine;

public class SampleCameraController : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followSpeed;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, followTarget.position, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, followTarget.rotation, Time.deltaTime * followSpeed);
    }
}