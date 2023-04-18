using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RainDrop : MonoBehaviour
{
    [FormerlySerializedAs("thisrigidbody")] [FormerlySerializedAs("rigidbody")]
    public Rigidbody thisRigidbody;

    public Renderer renderer;
    private MaterialPropertyBlock _materialPropertyBlock;

    public void Init(Color color)
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetColor("_Color", color);
        renderer.SetPropertyBlock(_materialPropertyBlock);
        thisRigidbody.AddForce(Random.onUnitSphere, ForceMode.Impulse);
    }
}