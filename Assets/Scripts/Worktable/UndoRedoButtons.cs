using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Controls
{
    [RequireComponent(typeof(MeshCollider))]
    public class UndoRedoButtons : IInteractableObject
    {
        public bool undo;

        private Color initialColor;
        private UndoManager _undoManager;
        private MeshRenderer _meshRenderer;

        private bool isInteracting;
        private bool canUse;

        private bool isActive;
        private bool fromTop;
        
        // Use this for initialization
        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            initialColor = _meshRenderer.material.color;
            _undoManager = FindObjectOfType<UndoManager>();
            isInteracting = false;
            isActive = false;
            fromTop = false;
            canUse = GetComponent<MeshRenderer>().isVisible;

        }

        void Update()
        {
            canUse = GetComponent<MeshRenderer>().isVisible;
            //Check if the undo/redo action is available
            if (!isInteracting)
            {
                if ((undo && _undoManager.position - 1 < 0) || (!undo && _undoManager.position + 1 >= _undoManager.undoActions.Count))
                {
                    _meshRenderer.material.color = Color.red;
                    
                    isActive = false;
                }
                else
                {
                    _meshRenderer.material.color = Color.green;
                    isActive = true;
                }
            }
        }

        public override void ChangeInteraction(InteractionMode mode)
        {
        }

        public override void EndHighlight()
        {
        }

        public override void Interact()
        {
        }

        public override void StartHighlight()
        {
            //Since this is a button that is pushed this actually does the Interaction
            //if (isActive && fromTop && canUse)
            //{
            //    StartCoroutine(FlashButton());
            //}
        }

        public override void StopInteraction()
        {
        }

        IEnumerator FlashButton()
        {
            isInteracting = true;
            //var component = GetComponent<MeshRenderer>();
            //Material material = component.material;
            initialColor = _meshRenderer.material.color;
            _meshRenderer.material.color = Color.blue;
            var undoManager = FindObjectOfType<UndoManager>();
            if (undoManager)
            {
                if (undo)
                    undoManager.MoveBackwards();
                else
                    undoManager.MoveForward();
            }
            else
            {
                Debug.Log("Cannot find manager");
            }
            undoManager.previewPrev = false;
            undoManager.previewNext = false;
            undoManager.UpdatePreview();
            byte[] noize = { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
            OVRHaptics.Channels[1].Preempt(new OVRHapticsClip(noize, 10));
            yield return new WaitForSeconds(0.3f);

            _meshRenderer.material.color = initialColor;
            isInteracting = false;

        }

        private void OnTriggerEnter(Collider other)
        {

            if (other.GetComponent<GrabControl>()!=null && !fromTop)
            {

                if (other.transform.position.y > transform.position.y+ 0.0027)
                {
                    //Debug.Log("pressed");
                    fromTop = true;
                    if (isActive && canUse)
                        StartCoroutine(FlashButton());
                }
                else
                {
                    fromTop = false;
                }
            }

        }

        private void OnTriggerExit(Collider other)
        {

            if (other.GetComponent<GrabControl>() != null)
            {
                fromTop = false;
            }
        }
    }
}
