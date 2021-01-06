using System;
using System.Collections;
using System.Collections.Generic;
using Controls;
using UnityEngine;

public class GrabControl : MonoBehaviour {
	public enum State
	{
		EMPTY,
		TOUCHING,
		HOLDING
	};

	public OVRInput.Controller Controller = OVRInput.Controller.RTouch;

    public State HandState = State.EMPTY;
    private State PrevHandState = State.EMPTY;

    public GrabControl OtherHand;
	
	private List<IInteractableObject> _interactableObjects;
	private IInteractableObject currentInteraction;
	
	private ControlsManager controls;

	public SphereCollider controllerSphereCollider;
    public VertexHandleController collidedVertexHandle;
    public FaceHandleController collidedFaceHandle;


    private float lastIndexFingerState = 0;

    public Color defaultColor;
    public Color hoverColor;

    private MeshRenderer _meshRenderer;

    private Vector3 prevPos;
    private Quaternion prevRot;

    void Awake()
    {
        _interactableObjects = new List<IInteractableObject>();
        controls = FindObjectOfType<ControlsManager>();
        controllerSphereCollider = transform.childCount > 0 ? transform.GetChild(0).GetComponent<SphereCollider>() : GetComponent<SphereCollider>();
 
        _meshRenderer = GetComponent<MeshRenderer>();
        defaultColor = _meshRenderer.material.color;
    }

    public IInteractableObject GetCurrentInteraction()
    {
        return currentInteraction;
    }

    private void UpdateHoverColor()
    {
        _meshRenderer.material.color = _interactableObjects.Count == 0 ? defaultColor : hoverColor;
    }

    void OnTriggerEnter(Collider collider)
    {
        var iobj = collider.GetComponent<IInteractableObject>();
        var colliderGameObject = collider.gameObject;

        if(colliderGameObject.GetComponent<VertexHandleController>() != null)
        {
            collidedVertexHandle = colliderGameObject.GetComponent<VertexHandleController>();
        }
        else if (colliderGameObject.GetComponent<FaceHandleController>() != null)
        {
            collidedFaceHandle = colliderGameObject.GetComponent<FaceHandleController>();
        }


        if (HandState == State.EMPTY || HandState == State.TOUCHING)
        {
            if (iobj != null)
            {
                if (_interactableObjects.Count > 0)
                {
                    _interactableObjects[_interactableObjects.Count-1].EndHighlight();
                }

                iobj.StartHighlight();
                _interactableObjects.Add(iobj);
                HandState = State.TOUCHING;
                UpdateHoverColor();
            }
        } 
        
    }

    void OnTriggerExit(Collider collider)
    {
        var iobj = collider.GetComponent<IInteractableObject>();
        if (iobj != null)
        {
            if (HandState != State.HOLDING)
            {
                if (_interactableObjects.Count > 0)
                {
                    
                    iobj.EndHighlight();
                    _interactableObjects.Remove(iobj);
                
                }
                if (_interactableObjects.Count > 0)
                {
                    _interactableObjects[_interactableObjects.Count-1].StartHighlight();
                }
                HandState = State.EMPTY;
            }
            else // remove while holding
            {
                _interactableObjects.Remove(iobj);
            }
            UpdateHoverColor();
        }

        collidedVertexHandle = null;
        collidedFaceHandle = null;

    }


    void Update()
    {
        while (_interactableObjects.Count > 0 && _interactableObjects[_interactableObjects.Count - 1] == null)
        {
            _interactableObjects.RemoveAt(_interactableObjects.Count - 1);
            UpdateHoverColor();
        }
        switch (HandState)
        {
            case State.TOUCHING:
                if (_interactableObjects.Count > 0 &&
                    OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller) >= 0.5f)
                {
                    _interactableObjects[_interactableObjects.Count-1].StartInteraction(controllerSphereCollider, this);
                    currentInteraction = _interactableObjects[_interactableObjects.Count - 1]; 
                    HandState = State.HOLDING;
                }

                break;
            case State.HOLDING:
                
                if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller) < 0.5f)
                {
                    currentInteraction.EndInteraction();
                    if (currentInteraction != null && !_interactableObjects.Contains(currentInteraction))
                    {
                        currentInteraction.EndHighlight();
                    }

                    // clean up deleted objects
                    _interactableObjects.RemoveAll(obj => (obj == null));

                    HandState = _interactableObjects.Count == 0? State.EMPTY : State.TOUCHING;
                    controls.DestroyInvalidObjects();
                    controls.UpdateControls();
                    UpdateHoverColor();
                    currentInteraction = null;
                }

                break;
        }

        //Movement of model
        if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller) > 0.5f 
            && IsInsideModel()
            && currentInteraction == null)
        {
            controls.Extrudable.transform.root.Translate(transform.position - prevPos, Space.World);
            Quaternion deltaRot = transform.rotation * Quaternion.Inverse(prevRot);
            controls.Extrudable.transform.root.position = RotatePointAroundPivot(controls.Extrudable.transform.root.position, transform.position, deltaRot);
            controls.Extrudable.transform.root.Rotate(deltaRot.eulerAngles,Space.World);
            //controls.UpdateControls();
        }
        
        
        prevPos = transform.position;
        prevRot = transform.rotation;

        //Debug state changes
            if (PrevHandState != HandState)
        {
            //Debug.Log(Controller.ToString() +": " + HandState.ToString());
        }
        PrevHandState = HandState;

    }

    private bool IsInsideModel()
    {
        return true;
    }

    public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }
}
