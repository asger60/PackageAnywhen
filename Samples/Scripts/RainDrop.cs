using UnityEngine;
using UnityEngine.Serialization;

public class RainDrop : MonoBehaviour
{
    [FormerlySerializedAs("thisrigidbody")] [FormerlySerializedAs("rigidbody")]
    public Rigidbody thisRigidbody;

    [FormerlySerializedAs("renderer")] public Renderer thisRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;

    public void Init(Color color)
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetColor("_Color", color);
        thisRenderer.SetPropertyBlock(_materialPropertyBlock);
        var forceAdd = (Random.onUnitSphere + Vector3.down) * 20;
        thisRigidbody.AddForce(forceAdd, ForceMode.Impulse);
    }
}