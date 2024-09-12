using UnityEngine;

public class SampleBullet : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private float _lifeTime;
    

    public void Shoot(Vector3 direction, float force)
    {
        TryGetComponent(out _rigidbody);
        _rigidbody.AddForce(direction * force, ForceMode.VelocityChange);
    }

    private void Update()
    {
        _lifeTime += Time.deltaTime;
        if (_lifeTime > 5)
            Destroy(gameObject);
    }
}