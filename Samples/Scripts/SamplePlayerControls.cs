using Anywhen.Composing;
using UnityEngine;
using UnityEngine.Serialization;

namespace Samples.Scripts
{
    public class SamplePlayerControls : MonoBehaviour
    {
        [FormerlySerializedAs("moveSpeed")] [SerializeField]
        float moveSpeedMax;

        [SerializeField] float rotateSpeed;

        private Rigidbody _rigidbody;
        private Vector3 _velocity;

        private float _vertical, _horizontal;
        [SerializeField] SampleBullet bulletPrefab;
        private bool _isShooting;
        private float _lastBullet;
        private Vector3 _prevPosition;
        private float _currentMoveSpeed;
        public float sidewaysDampingFactor = 0.95f; // Damping factor for sideways movement, 0 means no sideways movement
        public AnimationCurve torqueCurve;
        public AnimationCurve sidewaysDragCurve;

        private void Start()
        {
            TryGetComponent(out _rigidbody);
        }

        public void SetVerticalMovement(float value)
        {
            _vertical = value;
        }

        public void SetHorizontalMovement(float value)
        {
            _horizontal = value;
        }

        public void SetShooting(bool state)
        {
            _isShooting = state;
            _lastBullet = 0;
        }

        void FixedUpdate()
        {
            if (_isShooting)
            {
                if (_lastBullet == 0)
                {
                    var newBullet = Instantiate(bulletPrefab, transform.position + transform.forward, Quaternion.identity);
                    newBullet.Shoot(transform.forward + transform.up, 10);
                }

                _lastBullet += Time.deltaTime;
                if (_lastBullet > 0.5f)
                    _lastBullet = 0;
            }

            _prevPosition = transform.position;


            _rigidbody.AddForce(transform.forward * (moveSpeedMax * _vertical), ForceMode.Acceleration);
            var localVelocity = transform.InverseTransformDirection(_rigidbody.velocity);
            AnysongPlayerBrain.SetGlobalIntensity(localVelocity.z / moveSpeedMax);

            float torque = _horizontal * rotateSpeed * Mathf.Sign(localVelocity.z) * torqueCurve.Evaluate((Mathf.Abs(localVelocity.z) / moveSpeedMax));


            _rigidbody.AddTorque(0, torque, 0, ForceMode.Acceleration);


            Vector3 forwardVelocity = transform.forward * Vector3.Dot(_rigidbody.velocity, transform.forward);
            Vector3 sidewaysVelocity = transform.right * Vector3.Dot(_rigidbody.velocity, transform.right);
            sidewaysVelocity *= sidewaysDragCurve.Evaluate((Mathf.Abs(localVelocity.z) / moveSpeedMax)) *sidewaysDampingFactor;
            _rigidbody.velocity = forwardVelocity + sidewaysVelocity + transform.up * _rigidbody.velocity.y;
        }
    }
}