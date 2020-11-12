using System.Collections.Generic;
using UnityEngine;

namespace Controls
{
    public class TurntableController : IInteractableObject
    {
        private HoverHighlight _hoverHighlight;

        public Turntable _turntable;
        // rotation
        private Quaternion startRotation;
        private Quaternion initialRotation;
        // scale and translate
        private Vector3 initialPosition;
        private Vector3 initialControllerCenter;
        public float initialScale;
        public float initialDistanceBetweenControllers;
        

        public Transform handTransform;

        private InteractionMode mode = InteractionMode.SINGLE;

        private ControlsManager controlsManager;

        private static List<TurntableController> activeControllers = new List<TurntableController>();
        private int updateInFrame;

        void Awake()
        {
            _hoverHighlight = GetComponent<HoverHighlight>();
            controlsManager = FindObjectOfType<ControlsManager>();
        }

        // Update is called once per frame
        void Update()
        {
            if (updateInFrame == Time.frameCount) return;
            if (activeControllers.Count > 0 && activeControllers[0] == this)
            {
                if (mode == InteractionMode.SINGLE)
                {
                    var direction = (_turntable.transform.position - handTransform.position) * GetComponentInParent<Turntable>().rotationSensitivity;
                    direction.y = 0;
                    //Debug.Log(direction);
                    //Debug.Log(GetComponentInParent<Turntable>().rotationSensitivity);
                    _turntable.transform.rotation = initialRotation * startRotation * Quaternion.LookRotation(direction);
                }
                else
                {
                    if (activeControllers.Count == 2)
                    {
                        float distanceBetweenControllers = Vector3.Distance(activeControllers[1].handTransform.position,
                            activeControllers[0].handTransform.position);

                        controlsManager.SetGlobalScale(initialScale *
                                                       (distanceBetweenControllers /
                                                        initialDistanceBetweenControllers));

                        var deltaMovement = (activeControllers[1].handTransform.position +
                                             activeControllers[0].handTransform.position) * 0.5f -
                                            initialControllerCenter;
                        _turntable.childObject.position =
                            initialPosition + deltaMovement;
                    }
                }
            }
        }

        public override void Interact()
        {
            updateInFrame = Time.frameCount;
            handTransform = _controllerCollider.GetComponentInParent<OvrAvatarHand>().transform;
            activeControllers.Add(this);

            if (activeControllers.Count == 1)
            {
                InitRotationInteraction();
            }
            else if (activeControllers.Count == 2)
            {
                InitScaleInteraction();
                ChangeInteraction(InteractionMode.DUAL);
                activeControllers[0].ChangeInteraction(InteractionMode.DUAL);
                activeControllers[0].InitScaleInteraction();
                activeControllers[0].initialScale = _turntable.childObject.localScale.x;
                activeControllers[1].initialScale = _turntable.childObject.localScale.x;
                activeControllers[1].initialDistanceBetweenControllers = Vector3.Distance(activeControllers[1].handTransform.position,
                    activeControllers[0].handTransform.position);
                activeControllers[0].initialDistanceBetweenControllers =
                    activeControllers[1].initialDistanceBetweenControllers;
            }
        }

        private void InitScaleInteraction()
        {
            initialPosition = _turntable.childObject.position;
            initialControllerCenter = (activeControllers[1].handTransform.position +
                                       activeControllers[0].handTransform.position) * .5f;
        }

        private void InitRotationInteraction()
        {
            var direction = _turntable.transform.position - handTransform.position;
            direction.y = 0;
            startRotation = Quaternion.Inverse(Quaternion.LookRotation(direction));
            initialRotation = _turntable.transform.rotation;
        }

        public override void StartHighlight()
        {
            _hoverHighlight.StartHover();
        }

        public override void EndHighlight()
        {
            _hoverHighlight.EndHover();
        }

        public override void ChangeInteraction(InteractionMode mode)
        {
            this.mode = mode;
            updateInFrame = Time.frameCount;
        }

        public override void StopInteraction()
        {
            updateInFrame = Time.frameCount;
            activeControllers.Remove(this);
            if (activeControllers.Count == 1)
            {
                activeControllers[0].ChangeInteraction(InteractionMode.SINGLE);
                activeControllers[0].InitRotationInteraction();
            }
            ChangeInteraction(InteractionMode.SINGLE);
        }
    }
}