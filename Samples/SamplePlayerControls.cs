using System;
using UnityEngine;

namespace Samples
{
    public class SamplePlayerControls : MonoBehaviour
    {
        [SerializeField] float moveSpeed;
        [SerializeField] float rotateSpeed;

        private Rigidbody _rigidbody;
        private Vector3 _velocity;

        private float _vertical, _horizontal;
        [SerializeField] SampleBullet bulletPrefab;
        private bool _isShooting;
        private float _lastBullet;
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

        void Update()
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
            
            if (Mathf.Abs(_vertical) > 0)
            {
                
                transform.position += transform.forward * (_vertical * Time.deltaTime * moveSpeed);
                _rigidbody.MovePosition(transform.position);
            }

            if (Mathf.Abs(_horizontal) > 0)
            {
                transform.Rotate(Vector3.up,
                    (_horizontal * Mathf.Sign(_vertical) * Time.deltaTime * rotateSpeed));
            }
        }
    }
}