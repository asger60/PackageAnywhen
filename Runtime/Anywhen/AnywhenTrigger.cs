using System;
using UnityEngine;

namespace Anywhen
{
    public class AnywhenTrigger : MonoBehaviour
    {
        public Action OnTrigger;

        public enum TriggerTypes
        {
            None,
            ObjectStart,
            ObjectDestroy,
            TriggerEnter,
            TriggerExit,
            TriggerEnter2D,
            TriggerExit2D,
            CollisionEnter,
            CollisionExit,
            CollisionEnter2D,
            CollisionExit2D,
            ObjectEnable,
            ObjectDisable,
            //MouseEnter,
            //MouseExit,
            //MouseDown,
            //MouseUp,
        }

        [SerializeField] private GameObject targetObject;

        public TriggerTypes triggerType;


        void Start()
        {
            if (triggerType == TriggerTypes.ObjectStart) Trigger();
        }

        private void OnDestroy()
        {
            if (triggerType == TriggerTypes.ObjectDestroy) Trigger();
        }

        private void OnEnable()
        {
            if (triggerType == TriggerTypes.ObjectEnable) Trigger();
        }

        private void OnDisable()
        {
            if (triggerType == TriggerTypes.ObjectDisable) Trigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.TriggerEnter) Trigger();
        }


        private void OnTriggerExit(Collider other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.TriggerExit) Trigger();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.CollisionEnter) Trigger();
        }

        private void OnCollisionExit(Collision other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.CollisionExit) Trigger();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.TriggerEnter2D) Trigger();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.TriggerExit2D) Trigger();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.CollisionEnter2D) Trigger();
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (targetObject != null && other.gameObject != targetObject) return;
            if (triggerType == TriggerTypes.CollisionExit2D) Trigger();
        }


        private void Trigger()
        {
            OnTrigger?.Invoke();
        }
    }
}