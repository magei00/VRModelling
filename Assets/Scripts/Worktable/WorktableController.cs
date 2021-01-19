using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.GEL;
using System.IO;
using System;
using Controls;


public class WorktableController : MonoBehaviour
{
    //public Transform userBody;
    [SerializeField]
    private ExtrudableMesh _extrudableMesh;

    //private Vector3 _offset = new Vector3(-0.2f, -0.4f, 0.1f);
    //private Vector3 _rotation = new Vector3(0f, 190f, 0f);

    public Transform modellingObject;

    public bool enableMeshRotation = true;
    private bool worktableActive = false;

    //[SerializeField]
    private WorktableController _worktableController;
    private GameObject baseChild;

    private OVRInput.Controller lController = OVRInput.Controller.LTouch;
    // Use this for initialization
    void Start()
    {
        //this.transform.position = userBody.position - _offset;
        //this.transform.rotation = Quaternion.Euler(_rotation);
        baseChild = transform.GetChild(0).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        //this.transform.position = userBody.position + _offset;
        //this.transform.rotation = Quaternion.Euler(_rotation);
        if (enableMeshRotation) {
            updateObjectRotation();
        }


        //Show or hide worktable based on hand direction
        Debug.DrawLine(ControlsManager.Instance.handCenters[0].position, ControlsManager.Instance.handCenters[0].position + OVRInput.GetLocalControllerRotation(lController) * Vector3.right, Color.red);

        Debug.DrawLine(ControlsManager.Instance.handCenters[0].position, (Vector3.up * 10) + ControlsManager.Instance.handCenters[0].position);

        if (Vector3.Dot(OVRInput.GetLocalControllerRotation(lController) * Vector3.right, Vector3.up) > 0.95f )
        {
            if (!worktableActive)
            {
                baseChild.SetActive(true);
                worktableActive = true;
                StartCoroutine(Grow(new Vector3(0.1f, 0.1f, 0.1f), new Vector3(2f, 2f, 2f)));
            }
            transform.position = ControlsManager.Instance.handCenters[0].position + Vector3.up*0.05f;  
        }
        else if (worktableActive)
        {
            worktableActive = false;
            StartCoroutine(Grow(new Vector3(2f, 2f, 2f), new Vector3(0.1f, 0.1f, 0.1f)));
            
        }
        //if (Input.GetKeyDown("s"))
        //{
        //    //bool test = RefinementTestLoop(30, 40);
        //    bool test = RefineMentTest(0);
        //    Debug.Log("Refinement test:");
        //    Debug.Log(test);
        //}

    }

    public IEnumerator Grow(Vector3 startScale, Vector3 endScale)
    {
        var elapsedTime = 0f;
        var waitTime = 0.2f;

        while (elapsedTime < waitTime)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, (elapsedTime / waitTime));
            elapsedTime += Time.deltaTime;

            // Yield here
            yield return null;
        }
        // Make sure we got there
        transform.localScale = endScale;

        if (startScale.x > endScale.x)
        {
            baseChild.SetActive(false);
        }

        yield return null;
    }

    public void updateObjectRotation()
    {
        GameObject[] dials = GameObject.FindGameObjectsWithTag("RotationDial");
        modellingObject.rotation = Quaternion.Euler(0f, 0f, 0f);
        //modellingObject.parent.parent.rotation = Quaternion.Euler(0f, 0f, 0f);
        foreach (GameObject gobj in dials)
        {
            if (gobj.name == "HorizontalDial")
            {
                //modellingObject.parent.parent.rotation = Quaternion.Euler(0f, 0f, 0f);
                //modellingObject.parent.parent.Rotate(new Vector3(0f, gobj.transform.rotation.eulerAngles.y, 0f), relativeTo: Space.World);
                modellingObject.Rotate(new Vector3(0f, gobj.transform.rotation.eulerAngles.y, 0f), relativeTo: Space.World);
            }
            else if (gobj.name == "VerticalDial")
            {
                modellingObject.Rotate(new Vector3(gobj.transform.rotation.eulerAngles.y, 0f, 0f), relativeTo: Space.World);
            }
            else
            {
                Debug.LogWarning("Dial not recognized!");
            }
        }
    }

    public bool LoadMesh(string filename)
    {
        Debug.Log("worktableController here... trying to load..." + " " + filename);


#if UNITY_STANDALONE && !UNITY_EDITOR
        filename = String.Format("Saved/{0}.obj", filename);
#else
        filename = String.Format("Assets/Models/Saved/{0}.obj", filename);
#endif
        _extrudableMesh._manifold = new Manifold();
        var manifold = new Manifold();

        bool loaded = manifold.LoadFromOBJ(filename);

        if (loaded)
        {
            manifold.StitchMesh(1e-10);
            _extrudableMesh.LoadMesh(manifold);
        }

        return loaded;
    }

    public bool SaveMesh(string fname)
    {
#if UNITY_STANDALONE && !UNITY_EDITOR
        string fileWithPath = String.Format("Saved/{0}.obj", fname);
#else
        string fileWithPath = String.Format("Assets/Models/Saved/{0}.obj", fname);
#endif


        bool saved = _extrudableMesh._manifold.SaveToOBJ(fileWithPath);
      
        return saved;

    }

    public bool RefinementTestLoop(int halfedge1, int halfedge2)
    {        
        if (!ValidateFaceLoop(halfedge1, halfedge2))
        {
            return false;
        }
        ExtrudableMesh _extrudableMesh = modellingObject.GetComponentInChildren<ExtrudableMesh>();
        Manifold manifold = _extrudableMesh._manifold;

        int h = halfedge1;
        int initital_f = manifold.GetIncidentFace(halfedge1);
        int f = initital_f;
        
        List<int> faceloop = new List<int>();
        List<int> edgeloop = new List<int>();

        while (true)
        {
            if (manifold.IsFaceInUse(f))
            {
                if (manifold.IsHalfedgeInUse(h))
                {
                    int h2 = manifold.GetNextHalfEdge(h);
                    h2 = manifold.GetNextHalfEdge(h2);
                    if (manifold.IsHalfedgeInUse(h2))
                    {
                        int w = manifold.SplitEdge(h);
                        edgeloop.Add(w);
                        faceloop.Add(f);
                        
                        h = manifold.GetOppHalfEdge(h2);
                        f = manifold.GetIncidentFace(h);
                        if (f == initital_f)
                        {
                            int[] farray = faceloop.ToArray();
                            int[] earray = edgeloop.ToArray();
                            for(int i = 0; i < farray.Length; i++)
                            {
                                if (i == farray.Length - 1)
                                {
                                    int output = manifold.SplitFaceByEdges(farray[i], earray[i], earray[0]); // earray[1]);
                                }
                                else
                                {
                                    int output = manifold.SplitFaceByEdges(farray[i], earray[i], earray[i + 1]);
                                }
                                
                            }
                            _extrudableMesh.UpdateMesh();
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

    public bool ValidateFaceLoop(int halfedgeId1, int halfedgeId2)
    {
        Manifold manifold = _extrudableMesh._manifold;

        //if (manifold.GetIncidentFace(halfedgeId1) == manifold.GetIncidentFace(halfedgeId2))
        //{
        //    return false;
        //}

        if(!manifold.IsHalfedgeInUse(halfedgeId1) || !manifold.IsHalfedgeInUse(halfedgeId2))
        {
            return false;
        }

        int f = manifold.GetIncidentFace(halfedgeId1);
        int h = halfedgeId1;
        while (true)
        {
            int h2 = manifold.GetNextHalfEdge(h);
            h2 = manifold.GetNextHalfEdge(h2);
            int h3 = manifold.GetOppHalfEdge(h2);

            
            if (h2 == halfedgeId2 || h3 == halfedgeId2)
            {
                return true;
            }

            else if (manifold.GetIncidentFace(h3) == f)
            {
                return false;
            }
            h = h3;
        }
    }

    public bool RefineMentTest(int halfedge)
    {
        
        ExtrudableMesh _extrudableMesh = modellingObject.GetComponentInChildren<ExtrudableMesh>();
        Manifold manifold = _extrudableMesh._manifold;

        if (manifold.IsHalfedgeInUse(halfedge))
        {
            int h2 = manifold.GetNextHalfEdge(halfedge);
            h2 = manifold.GetNextHalfEdge(h2);

            if (manifold.IsHalfedgeInUse(h2))
            {
                int v1 = _extrudableMesh._manifold.SplitEdge(halfedge);
                int v2 = _extrudableMesh._manifold.SplitEdge(h2);
                int output = _extrudableMesh._manifold.SplitFaceByEdges(manifold.GetIncidentFace(halfedge), v1, v2);
                _extrudableMesh.UpdateMesh();

                ControlsManager man = modellingObject.GetComponentInChildren<Controls.ControlsManager>();
                man.UpdateControls();

                return true;
            }
        }

        return false;
    }
}
