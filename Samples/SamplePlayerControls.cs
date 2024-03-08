using Anywhen.Composing;
using UnityEngine;

public class SamplePlayerControls : MonoBehaviour
{
    [SerializeField] float moveSpeed;
    [SerializeField] float rotateSpeed;


    private Vector3 _velocity;
    Vector3 _prevPosition;

    void Start()
    {
    }

    void Update()
    {
        _prevPosition = transform.position;
        if (Mathf.Abs(Input.GetAxis("Vertical")) > 0)
        {
            transform.position += transform.forward * (Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed);
        }

        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0)
        {
            transform.Rotate(Vector3.up,
                (Input.GetAxis("Horizontal") * Mathf.Sign(Input.GetAxis("Vertical")) * Time.deltaTime * rotateSpeed));
        }

        //_velocity = (transform.position - _prevPosition) / Time.deltaTime; 
        
        //AnysongPlayerBrain.SetGlobalIntensity(_velocity.magnitude);
    }
}