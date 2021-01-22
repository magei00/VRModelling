using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Controls
{
    [RequireComponent(typeof(MeshCollider))]
    public class SaveButton : IInteractableObject
    {
        private Color initialColor;
        private MeshRenderer _meshRenderer;

        //private bool isInteracting;

        private bool fromTop;
        private bool canUse;


        // Use this for initialization
        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            initialColor = _meshRenderer.material.color;
            //isInteracting = false;
            canUse = GetComponent<MeshRenderer>().isVisible;
        }

        void Update()
        {
            canUse = GetComponent<MeshRenderer>().isVisible;
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
            //if (fromTop && canUse)
            //{
            //    StartCoroutine(SaveModel());

            //}
        }

        public override void StopInteraction()
        {
        }

        IEnumerator SaveModel()
        {
            //isInteracting = true;
            var component = GetComponent<MeshRenderer>();
            Material material = component.material;
            material.color = Color.blue;

            WorktableController _worktableController = transform.root.gameObject.GetComponent<WorktableController>();
            bool success = _worktableController.SaveMesh(transform.parent.name);

            //ScreenController _screenController = UIPanel.GetComponent<ScreenController>();
            //_screenController.ExportMesh();
            //_screenController.OpenExportUI(isOpen);
            //_screenController.ExportMesh("TestExport");

            //_screenController.disableScreen();

            if (success)
            {
                byte[] noize = { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
                OVRHaptics.Channels[1].Preempt(new OVRHapticsClip(noize, 10));
                yield return new WaitForSeconds(0.3f);
            }          

            material.color = initialColor;
            //isInteracting = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            //Check if the trigger is entering the button from above
            if (other.GetComponent<GrabControl>() != null && !fromTop)
            {
                if (other.transform.position.y > transform.position.y)
                {
                    fromTop = true;
                    if (canUse)
                    {
                        StartCoroutine(SaveModel());

                    }
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
