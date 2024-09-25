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
        }

        public enum TriggerObjectTypes
        {
            Any,
            GameObject,
            Tag,
            Name,
        }

        [SerializeField] private GameObject triggerObject;

        [SerializeField] private TriggerObjectTypes triggerObjectType;

        [SerializeField] private string triggerObjectTag;

        [SerializeField] private string triggerObjectName;

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
            if (triggerType != TriggerTypes.TriggerEnter) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }


        private void OnTriggerExit(Collider other)
        {
            if (triggerType != TriggerTypes.TriggerExit) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (triggerType != TriggerTypes.CollisionEnter) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }

        private void OnCollisionExit(Collision other)
        {
            if (triggerType != TriggerTypes.CollisionExit) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerType != TriggerTypes.TriggerEnter2D) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (triggerType != TriggerTypes.TriggerExit2D) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (triggerType != TriggerTypes.CollisionEnter2D) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (triggerType != TriggerTypes.CollisionExit2D) return;
            if (EvaluateTrigger(other.gameObject))
                Trigger();
        }

        bool EvaluateTrigger(GameObject other)
        {
            switch (triggerObjectType)
            {
                case TriggerObjectTypes.GameObject:
                    return other == triggerObject;
                case TriggerObjectTypes.Tag:
                    return other.CompareTag(other.tag);
                case TriggerObjectTypes.Name:
                    return triggerObjectName == other.name;
                case TriggerObjectTypes.Any:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }


        private void Trigger()
        {
            OnTrigger?.Invoke();
        }
    }
}