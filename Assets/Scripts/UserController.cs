using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserController : MonoBehaviour {

    private float speedPerSec = 0.005f*60;

    public static UserController instance = null;

    public bool movementEnabled = true;

	// Use this for initialization

    void Awake()
    {
        transform.localPosition = new Vector3(0, 1f, -1f);
    }
	void Start () {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        /*Vector3 cameraPos = new Vector3(
            PlayerPrefs.GetFloat("cam.x", transform.localPosition.x),
            PlayerPrefs.GetFloat("cam.y", transform.localPosition.y),
            PlayerPrefs.GetFloat("cam.z", transform.localPosition.z)
        );
	    transform.localPosition = cameraPos;*/

	    //movementEnabled = true;
	}
	
	// Update is called once per frame
	void Update () {

        if(OVRInput.Get(OVRInput.RawButton.A) && OVRInput.Get(OVRInput.RawButton.B))
        {
            movementEnabled = true;
        }

        if (movementEnabled)
        {
            Vector2 touchThumbPrimary = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            Vector2 touchThumbSecondary = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

            Vector3 pos = transform.position;
            pos.y += touchThumbPrimary.y * speedPerSec * Time.deltaTime;
            pos.x += touchThumbSecondary.x * speedPerSec * Time.deltaTime;
            pos.z += touchThumbSecondary.y * speedPerSec * Time.deltaTime;
            if (pos.sqrMagnitude > 0){
                transform.position = pos;
                PlayerPrefs.SetFloat("cam.x", pos.x);
                PlayerPrefs.SetFloat("cam.y", pos.y);
                PlayerPrefs.SetFloat("cam.z", pos.z);

            }
        }
    }

    void ChangeMovementState()
    {
        movementEnabled = !movementEnabled;
    }
}
