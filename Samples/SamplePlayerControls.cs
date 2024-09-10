using UnityEngine;

namespace Samples
{
    public class SamplePlayerControls : MonoBehaviour
    {
        [SerializeField] float moveSpeed;
        [SerializeField] float rotateSpeed;


        private Vector3 _velocity;

        private float _vertical, _horizontal;
        public void SetVerticalMovement(float value)
        {
            _vertical = value;
        }

        public void SetHorizontalMovement(float value)
        {
            _horizontal = value;
        }
        void Update()
        {
            if (Mathf.Abs(_vertical) > 0)
            {
                transform.position += transform.forward * (_vertical * Time.deltaTime * moveSpeed);
            }

            if (Mathf.Abs(_horizontal) > 0)
            {
                transform.Rotate(Vector3.up,
                    (_horizontal * Mathf.Sign(_vertical) * Time.deltaTime * rotateSpeed));
            }
        }
    }
}