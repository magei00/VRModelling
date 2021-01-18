using System;
using UnityEngine;

namespace Controls
{
    public abstract class IInteractableObject : MonoBehaviour
    {
        public enum InteractionMode
        {
            SINGLE,
            DUAL
        }

        protected SphereCollider _controllerCollider;
        protected MonoBehaviour _controller;

        public void StartInteraction(SphereCollider controllerCollider, MonoBehaviour controller)
        {
            _controllerCollider = controllerCollider;
            _controller = controller;
            Interact();
        }

        public void EndInteraction(MonoBehaviour controller)
        {
            _controller = controller;
            StopInteraction();
            _controllerCollider = null;
        }

        //Update handle size based on distance to hands.
        public virtual void UpdateHandleSize()
        {
            Vector3 left_ctrl_pt = GameObject.Find("LeftHandAnchor").transform.position;
            Vector3 right_ctrl_pt = GameObject.Find("RightHandAnchor").transform.position;
            float dist_left_ctrl = (10.0f * (left_ctrl_pt - transform.position)).sqrMagnitude;
            float dist_right_ctrl = (10.0f * (right_ctrl_pt - transform.position)).sqrMagnitude;
            float sz = 0.01f + 0.09f / (Mathf.Min(dist_left_ctrl, dist_right_ctrl) + 1.0f);
            transform.localScale = new Vector3(sz, sz, sz);

        }

        public abstract void Interact();

        public abstract void ChangeInteraction(InteractionMode mode);

        public abstract void StopInteraction();

        public abstract void StartHighlight();

        public abstract void EndHighlight();
    }
}