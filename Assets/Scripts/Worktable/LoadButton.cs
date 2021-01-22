using System.Collections;
using UnityEngine;

namespace Controls
{
    [RequireComponent(typeof(MeshCollider))]
    public class LoadButton : IInteractableObject
    {
        private Color initialColor;
        private MeshRenderer _meshRenderer;

        //private bool isInteracting;

        private bool fromTop;
        private bool canUse;

        private bool isOpen;

        // Use this for initialization
        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            initialColor = _meshRenderer.material.color;
            //isInteracting = false;
            canUse = GetComponent<MeshRenderer>().isVisible;
        }

        // Update is called once per frame
        void Update()
        {
            canUse = GetComponent<MeshRenderer>().isVisible;
        }

        public override void Interact()
        {
        }

        public override void ChangeInteraction(InteractionMode mode)
        {
        }

        public override void StopInteraction()
        {
        }

        public override void StartHighlight()
        {
            //if (fromTop && canUse)
            //{
            //    StartCoroutine(LoadModel());

            //}
        }

        IEnumerator LoadModel()
        {
            Debug.Log("Load");
            //isInteracting = true;
            var component = GetComponent<MeshRenderer>();
            Material material = component.material;
            material.color = Color.blue;

            WorktableController _worktableController = transform.root.gameObject.GetComponent<WorktableController>();

            bool success = _worktableController.LoadMesh(transform.parent.name);

            Debug.Log(success);
            //ScreenController _screenController = UIPanel.GetComponent<ScreenController>();
            //_screenController.OpenImportUI(isOpen);
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

        public override void EndHighlight()
        {
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log(other.transform.parent.name);
            //Check if the trigger is entering the button from above
            if (other.GetComponent<GrabControl>() != null && !fromTop)
            {
                Debug.Log(other.transform.position.y);
                Debug.Log(transform.position.y);
                if (other.transform.position.y > transform.position.y)
                {
                    fromTop = true;
                    if (canUse)
                    {
                        StartCoroutine(LoadModel());

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
