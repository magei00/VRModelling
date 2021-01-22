using System.Collections;
using System.Collections.Generic;
using Controls;
using UnityEngine;

public class IndexFingerControl : MonoBehaviour {

    public OVRInput.Controller Controller = OVRInput.Controller.RTouch;
    
    private List<IInteractableObject> _fingerInteractableObjects;

	public SphereCollider controllerSphereCollider;

	private float lastIndexFingerState = 0;
 
    public Color defaultColor;
    public Color hoverColor;

    public GrabControl[] grabControls;
    
    private MeshRenderer _meshRenderer;

    public float lastAction = 0;

    public float minimumTimeBetweenActions = 0.3f;
    
    void Awake()
    {
        _fingerInteractableObjects = new List<IInteractableObject>();
        controllerSphereCollider = transform.childCount > 0 ? transform.GetChild(0).GetComponent<SphereCollider>() : GetComponent<SphereCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
        defaultColor = _meshRenderer.material.color;
        grabControls = FindObjectsOfType<GrabControl>();
    }
    
    private void UpdateHoverColor()
    {
        _meshRenderer.material.color = _fingerInteractableObjects.Count == 0 ? defaultColor : hoverColor;
    }

    void OnTriggerEnter(Collider collider)
    {
        var interact = collider.GetComponent<IInteractableObject>();
        if (interact != null && lastAction + minimumTimeBetweenActions < Time.time)
        {
            if (_fingerInteractableObjects.Count > 0)
            {
                _fingerInteractableObjects[_fingerInteractableObjects.Count-1].EndHighlight();                        
            }
            _fingerInteractableObjects.Add(interact);
            _fingerInteractableObjects[_fingerInteractableObjects.Count-1].StartHighlight();
            UpdateHoverColor();
            interact.StartInteraction(controllerSphereCollider, this);
            lastAction = Time.time;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        var interact = collider.GetComponent<IInteractableObject>();
        if (interact != null)
        {       
            if (_fingerInteractableObjects.Count > 0 && interact == _fingerInteractableObjects[_fingerInteractableObjects.Count-1])
            {
                _fingerInteractableObjects[_fingerInteractableObjects.Count-1].EndHighlight();
                if (_fingerInteractableObjects.Count >= 2)
                {
                    _fingerInteractableObjects[_fingerInteractableObjects.Count-2].StartHighlight();                        
                }
            }
            _fingerInteractableObjects.Remove(interact);
            UpdateHoverColor();
        }
 
    }


    void Update()
    {
        float newFingerState = GetIndexFingerState();
        if (Mathf.Abs(newFingerState - lastIndexFingerState) > 0.02 && _fingerInteractableObjects.Count > 0 &&
            grabControls[0].HandState != GrabControl.State.HOLDING && grabControls[1].HandState != GrabControl.State.HOLDING)
        {
            _fingerInteractableObjects[_fingerInteractableObjects.Count -1 ].StartInteraction(controllerSphereCollider, this);
            _fingerInteractableObjects[_fingerInteractableObjects.Count -1 ].EndHighlight(); // disallow multiple interactions with same object
            _fingerInteractableObjects.RemoveAt(_fingerInteractableObjects.Count - 1);
            UpdateHoverColor();
        }
        lastIndexFingerState = newFingerState;
    }

    private float GetIndexFingerState()
    {
        bool isTouching = OVRInput.Get(OVRInput.Touch.PrimaryIndexTrigger, Controller);
        if (!isTouching)
        {
            return -1;
        }
        return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, Controller);
        
    }
}
