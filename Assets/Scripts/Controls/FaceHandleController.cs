using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.GEL;
using UnityEngine;

namespace Controls
{
    [RequireComponent(typeof(MeshCollider))]
    public class FaceHandleController : IInteractableObject
    {
        struct AdjacentLockedFaces
        {
            public int faceid;
            public bool dragged;

            public AdjacentLockedFaces(int faceid, bool dragged)
            {
                this.faceid = faceid;
                this.dragged = dragged;
            }

            public override string ToString()
            {
                return "{faceid:" + faceid + ",dragged:" + dragged + "}";
            }
        }

        public ExtrudableMesh Extrudable;
        public int AssociatedFaceID;

        private Vector3 screenPoint;
        private Vector3 offset;

        public bool IsDragged = false;
        private Vector3 initialControllerOffset;
        private Dictionary<int, Vector3> initialVertexPositions;
        private Dictionary<int, Vector3> extrudedVertexPositions;

        public List<EdgeHandleController> ConnectedLatches = new List<EdgeHandleController>();

        private List<int> extrudingFaces = new List<int>();

        private HoverHighlight _hoverHighlight;

        private Mesh mesh;

        private Manifold initialManifold;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialFaceCenter;

        private ControlsManager controlsManager;

        private float distanceToLowestVertex;
        private bool isExtruding = false;
        private bool planeSnap = false;

        private List<AdjacentLockedFaces> selectedFaceList;
        private Quaternion lastRotation;
        private Quaternion firstRotation;
        private GameObject[] measuringBands = new GameObject[4];
        bool angleSnap = true;
        bool translateSnap = true;
        bool collision = false;
        bool tickState = false;
        private Dictionary<int, Vector3> collidedFaceHandleVertexPositions;

        int collidedFaceID;

        FaceHandleController collidedFaceHandle;

        private Vector3 initialControllerPosInLocalSpace;

        private GameObject sbsPlane;
        private UnityEngine.Object loadedSBSPlane;
        void Awake()
        {
            _hoverHighlight = GetComponent<HoverHighlight>();
            var meshFilter = GetComponent<MeshFilter>();
            mesh = meshFilter.sharedMesh;
            controlsManager = FindObjectOfType<ControlsManager>();
            loadedSBSPlane = Resources.Load("Prefabs/SBS_Plane_indicator");
        }

        public IEnumerator Buzz(OVRInput.Controller gcController)
        {
            OVRInput.SetControllerVibration(0.1f, 1, gcController);
            yield return new WaitForSeconds(0.1f);
            OVRInput.SetControllerVibration(0, 0, gcController);
        }

        private void Update()
        {
            const float SNAP_DISTANCE = 0.04f;
            const float SNAP_ANGLE = 20.0f;

            UpdateHandleSize();
            
            if (IsDragged)
            {
                if (!isExtruding && extrudingFaces.Count == 1)
                {
                    ControlsManager.Instance.updateAdjacentFaceHandles(AssociatedFaceID);
                }

                Vector3 controllerPosInLocalSpace = transform.parent.worldToLocalMatrix.MultiplyPoint(_controllerCollider.transform.position);
                var move_from_initial_pos = controllerPosInLocalSpace - initialControllerPosInLocalSpace;
                GrabControl gc = _controller as GrabControl;
                GrabControl leftGrabControl = GameObject.Find("leftGrabControl").GetComponent<GrabControl>();
                GrabControl rightGrabControl = GameObject.Find("rightGrabControl").GetComponent<GrabControl>();

                //Check if left hand is sensing, if true do SBS
                if (leftGrabControl.collidedFaceHandle != null && leftGrabControl.HandState.ToString().Equals("TOUCHING") && !OVRInput.Get(OVRInput.Touch.Any, OVRInput.Controller.LTouch))
                {
                    
                    hideMeasuringBands();
                    //destroyMeasuringBands();

                    Vector3 faceNormal = Extrudable._manifold.GetFaceNormal(leftGrabControl.collidedFaceHandle.AssociatedFaceID);
                    Vector3 facePoint = Extrudable._manifold.GetCenter(leftGrabControl.collidedFaceHandle.AssociatedFaceID);

                    foreach (KeyValuePair<int, Vector3> vertex in Extrudable._manifold.GetVertexPositionsFromFace(AssociatedFaceID))
                    {
                        Extrudable.MoveVertexAlongVector(
                        vertex.Key,
                        pointToPlaneVector(faceNormal, facePoint, Extrudable._manifold.VertexPosition(vertex.Key)));

                    }
                    planeSnap = true;
                    ControlsManager.Instance.Extrudable.rebuild = true;
                    //Debug.Log("SBS");
                    EnableSBSPlane(facePoint, faceNormal);
                }
                //Check if right hand is sensing, if true do SBS
                else if (rightGrabControl.collidedFaceHandle != null && rightGrabControl.HandState.ToString().Equals("TOUCHING") && !OVRInput.Get(OVRInput.Touch.Any, OVRInput.Controller.RTouch))
                {

                    hideMeasuringBands();
                    //destroyMeasuringBands();

                    Vector3 faceNormal = Extrudable._manifold.GetFaceNormal(rightGrabControl.collidedFaceHandle.AssociatedFaceID);
                    Vector3 facePoint = Extrudable._manifold.GetCenter(rightGrabControl.collidedFaceHandle.AssociatedFaceID);

                    foreach (KeyValuePair<int, Vector3> vertex in Extrudable._manifold.GetVertexPositionsFromFace(AssociatedFaceID))
                    {
                        Extrudable.MoveVertexAlongVector(
                        vertex.Key,
                        pointToPlaneVector(faceNormal, facePoint, Extrudable._manifold.VertexPosition(vertex.Key)));

                    }
                    planeSnap = true;
                    ControlsManager.Instance.Extrudable.rebuild = true;
                    Debug.Log("SBS");
                    EnableSBSPlane(facePoint, faceNormal);
                }
                else
                {
                    DisableSBSPlane();
                    planeSnap = false;
                    if (!isExtruding)
                    {
                        if (move_from_initial_pos.magnitude > SNAP_DISTANCE)
                        {
                            ControlsManager.Instance.SetVertexPositionsById(initialVertexPositions);
                            ControlsManager.Instance.updateAdjacentFaceHandles(AssociatedFaceID);
                            Extrudable.StartExtrusion(extrudingFaces.ToArray());
                            isExtruding = true;
                            //if (extrudingFaces.Count() == 1)  // multiple faces ???
                            //{
                            initializeMeasuringBands();
                            //extrudedVertexPositions = Extrudable._manifold.GetVertexPositionsFromFaces(new List<int> { AssociatedFaceID });
                            extrudedVertexPositions = Extrudable._manifold.GetVertexPositionsFromFaces(extrudingFaces);
                            //}

                            if (gc)
                            {
                                StartCoroutine(Buzz(gc.Controller));
                            }
                        }
                    }

                    //if (OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, gc.Controller) && extrudingFaces.Count() == 1)
                    //Measuring band
                    if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, gc.Controller) >0.2 || OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, gc.Controller))
                    {
                        tickState = true;
                    }
                    else
                    {
                        tickState = false;
                    }

                    transform.localPosition = controllerPosInLocalSpace - initialControllerOffset;
                    var translate_vector = transform.localPosition;

                    var norm = Extrudable.GetFaceNormal(AssociatedFaceID);

                    if (translateSnap && isExtruding && !tickState)
                    {
                        float d = Vector3.Dot(norm, move_from_initial_pos);
                        if ((move_from_initial_pos - d * norm).magnitude > SNAP_DISTANCE)
                        {
                            translateSnap = false;
                            destroyMeasuringBands();
                            // GrabControl gc = _controller as GrabControl;
                            if (gc)
                            {
                                StartCoroutine(Buzz(gc.Controller));
                            }
                        }
                    }

                    if (!isExtruding)
                    {
                        ControlsManager.Instance.SetVertexPositionsById(initialVertexPositions);
                    }
                    else if (extrudedVertexPositions != null)
                    {
                        ControlsManager.Instance.SetVertexPositionsById(extrudedVertexPositions);
                    }
                    

                    Extrudable.MoveTo(
                        AssociatedFaceID,
                        extrudingFaces.ToArray(),
                        translate_vector, translateSnap && isExtruding, tickState && angleSnap);

                    //if (isExtruding && translateSnap && angleSnap && extrudingFaces.Count() == 1)
                    if (isExtruding && translateSnap && angleSnap)
                    {
                        if (tickState)
                    {
                        float extrusionLength = (Extrudable._manifold.GetCenter(AssociatedFaceID) - initialFaceCenter).magnitude;
                        updateMeasuringBands(Mathf.Round(extrusionLength * 50) / 50);
                    }
                    else
                    {
                        updateMeasuringBands(Vector3.Dot(move_from_initial_pos, norm));
                    }
                    }

                    // change between previous rotation and current rotation
                    var controllerRotInLocalSpace = GetControllerRotationInLocalSpace();
                    if (angleSnap && !tickState)
                    {
                        // Compute the rotation from the original controller orientation to the present.
                        // If that rotation is greater than 10 degrees, snapping is turned off, and the face orientation
                        // tracks the hand´s orientation.
                        var fullRotation = controllerRotInLocalSpace * Quaternion.Inverse(firstRotation);
                        float angle = 0.0f;
                        Vector3 axis = Vector3.zero;
                        fullRotation.ToAngleAxis(out angle, out axis);
                        if (angle > SNAP_ANGLE)
                        {
                            angleSnap = false;
                            destroyMeasuringBands();
                            //GrabControl gc = _controller as GrabControl;
                            if (gc)
                            {
                                StartCoroutine(Buzz(gc.Controller));
                            }
                        }
                    }

                    if (!angleSnap)
                    {
                        var deltaRotation = controllerRotInLocalSpace * Quaternion.Inverse(firstRotation);
                        Extrudable.RotateFaceAroundPoint(extrudingFaces.ToArray(),
                            transform.localPosition, deltaRotation);

                    }

                    //lastRotation = GetControllerRotationInLocalSpace();
                    ControlsManager.Instance.Extrudable.rebuild = true;
                    /*
                    if (extrudingFaces.Count > 1)
                    {
                        controlsManager.UpdateFacesAndSelectedEdges(new List<int> { AssociatedFaceID});
                    }
                    */
                }
            }
        }

        private void DisableSBSPlane()
        {
            if (sbsPlane != null)
            {
                Destroy(sbsPlane);
            }
        }

        private void EnableSBSPlane(Vector3 pos, Vector3 faceNorm)
        {
            if (sbsPlane == null)
            {
                sbsPlane = (GameObject)Instantiate(loadedSBSPlane, pos, Quaternion.LookRotation(faceNorm));
                
                sbsPlane.transform.parent = ControlsManager.Instance.transform;
                sbsPlane.transform.localPosition = pos;
                sbsPlane.transform.Translate(faceNorm * -0.001f);
                sbsPlane.transform.localRotation = Quaternion.LookRotation(faceNorm);
                sbsPlane.transform.Rotate(0,90,90,Space.Self);
                //Debug.Log("Created" + sbsPlane.ToString());
                //Debug.Log(pos+"   "+faceNorm);
                //Debug.Break();
            }
        }

        public Quaternion GetControllerRotationInLocalSpace()
        {
            return Quaternion.Inverse(transform.parent.rotation) * _controller.transform.parent.parent.localRotation;
        }

        public void AttachLatch(EdgeHandleController edgeHandle)
        {
            if (!ConnectedLatches.Contains(edgeHandle))
                ConnectedLatches.Add(edgeHandle);
        }

        public override void StartHighlight()
        {
            _hoverHighlight.StartHover();
        }

        public override void EndHighlight()
        {
            _hoverHighlight.EndHover();
        }

        private List<AdjacentLockedFaces> GetAdjacentLockedFaces(int faceId, List<AdjacentLockedFaces> currentFaceSet)
        {
            var handle = ControlsManager.Instance.GetHandle(faceId);
            if (handle == null)
                return currentFaceSet.Distinct().ToList();
            currentFaceSet.Add(new AdjacentLockedFaces(faceId, handle.IsDragged));
            foreach (var latch in handle.ConnectedLatches)
            {
                if (latch != null)
                {
                    var latchController = latch.GetComponent<EdgeHandleController>();
                    if (latchController.Locked && !currentFaceSet.ConvertAll(pair => pair.faceid).Contains(latchController.GetOtherFace(faceId)))
                    {
                        GetAdjacentLockedFaces(latchController.GetOtherFace(faceId), currentFaceSet);
                    }
                }
            }
            return currentFaceSet.Distinct().ToList();
        }

        //        private bool 

        public override void ChangeInteraction(InteractionMode mode)
        {

        }

        public override void Interact()
        {
            // pointToPlaneVectorTest
            /*
            Vector3 nTest = new Vector3(1f, 1f, 0f);
            Vector3 pvTest = new Vector3(2f, 2f, 0f);

            Vector3 pointTest = new Vector3(1f, 5f, 0f);
            Vector3 pointTest2 = new Vector3(1f, 5f, 1f);
            Vector3 pointTest3 = new Vector3(6f, 3f, 0f);
            Vector3 pointTest4 = new Vector3(6f, 3f, 1f);

            Debug.Log(pointToPlaneVector(nTest, pvTest, pointTest).ToVector3f().ToString());
            Debug.Log(pointToPlaneVector(nTest, pvTest, pointTest2).ToVector3f().ToString());
            Debug.Log(pointToPlaneVector(nTest, pvTest, pointTest3).ToVector3f().ToString());
            Debug.Log(pointToPlaneVector(nTest, pvTest, pointTest4).ToVector3f().ToString());
            */

            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
            initialFaceCenter = Extrudable._manifold.GetCenter(AssociatedFaceID);
            initialManifold = Extrudable._manifold.Copy();

            selectedFaceList = GetAdjacentLockedFaces(AssociatedFaceID, new List<AdjacentLockedFaces>());

            extrudingFaces = selectedFaceList.ConvertAll(pair => pair.faceid);
            ControlsManager.Instance.RemoveLatches(extrudingFaces);
            initialVertexPositions = ControlsManager.Instance.GetVertexPositionsFromFaces(extrudingFaces);
            isExtruding = false;

            var worldToLocal = transform.parent.worldToLocalMatrix;
            initialControllerPosInLocalSpace = worldToLocal.MultiplyPoint(_controllerCollider.transform.position);
            initialControllerOffset = initialControllerPosInLocalSpace - transform.localPosition;
            IsDragged = true;

            lastRotation = GetControllerRotationInLocalSpace();
            firstRotation = lastRotation;
            angleSnap = true;
            translateSnap = true;

            ControlsManager.Instance.Extrudable.rebuild = true;
            if (extrudingFaces.Count > 1)
            {
                ControlsManager.Instance.DeleteControlsExceptSelectedFaces(new List<int> { AssociatedFaceID });
            }
            else
            {
                //ControlsManager.Instance.DeleteControlsExceptSelectedFaces(extrudingFaces);
                //ControlsManager.Instance.DeleteControlsExceptFaces(initialManifold.GetAdjacentFaceIdsAndEdgeCenters(AssociatedFaceID).faceId, AssociatedFaceID);
                ControlsManager.Instance.DeleteControlsExceptFaces(new int[0], AssociatedFaceID);
            }

            ControlsManager.FireUndoStartEvent(mesh, this, initialPosition, initialRotation);
        }

        public override void StopInteraction()
        {

            if (IsDragged)
            {

                destroyMeasuringBands();
                IsDragged = false;

                if (collision == false && !planeSnap)
                {
                    Extrudable.MoveTo(
                        AssociatedFaceID,
                        extrudingFaces.ToArray(),
                        transform.localPosition, translateSnap && isExtruding, tickState);

                }
                else if (!planeSnap)
                //else if (extrudingFaces.Count == 1) // yes/no maybe
                {
                    Debug.Log("Attempting face bridging");
                    if (true)//(hasDistinctAdjacentFaces(collidedFaceHandle))
                    {
                        //collidedFaceHandleVertexPositions = controlsManager.GetVertexPositionsFromFaces(new List<int> { collidedFaceID });
                        collidedFaceHandleVertexPositions = initialManifold.GetVertexPositionsFromFace(collidedFaceID);

                        if (collidedFaceHandleVertexPositions.Count() == initialVertexPositions.Count() && extrudingFaces.Count == 1)
                        {
                            var matches = faceBridgingVertexAssignment(initialVertexPositions, collidedFaceHandleVertexPositions);
                            
                            if (/*facingFaces(matches, AssociatedFaceID, initialVertexPositions, collidedFaceID, collidedFaceHandleVertexPositions)*/ true)
                            {
                                Extrudable.ChangeManifold(initialManifold.Copy());
                                Debug.Log("Applying face bridging");
                                if (matches.Length == 6)
                                {
                                    Extrudable.bridgeFaces(AssociatedFaceID, collidedFaceID, new int[] { matches[0], matches[1], matches[2] }, new int[] { matches[3], matches[4], matches[5] }, 3);
                                    ControlsManager.Instance.UpdateControls();
                                }
                                else if (matches.Length == 8)
                                {
                                    Extrudable.bridgeFaces(AssociatedFaceID, collidedFaceID, new int[] { matches[0], matches[1], matches[2], matches[3] }, new int[] { matches[4], matches[5], matches[6], matches[7] }, 4);
                                    ControlsManager.Instance.UpdateControls();
                                }
                            }
                        }
                    }
                    collidedFaceHandleVertexPositions = null;
                }
                

                MergeWithCollidingFaces(); //Merge extrusion if done up against other faces.

                //Check if mesh is valid
                extrudingFaces = new List<int>(); // create new list (may be referenced by other hand)

                int collapsed = Extrudable.CollapseShortEdges(0.019f);
                Extrudable.TriangulateAndDrawManifold(); // needed for collision detection in isValidMesh



                if (Extrudable.isValidMesh())
                {
                    ControlsManager.Instance.DestroyInvalidObjects();
                    ControlsManager.Instance.Extrudable.rebuild = true;
                    //ControlsManager.Instance.UpdateControls();
                    ControlsManager.FireUndoEndEvent(mesh, this, initialPosition, initialRotation);
                }
                else
                {
                    Extrudable.ChangeManifold(initialManifold);
                }
/*
                if (collapsed > 0)
                {
                    
                }
                else if (Extrudable.isValidMesh())
                {
                    //ControlsManager.Instance.DestroyInvalidObjects();
                    ControlsManager.Instance.Extrudable.rebuild = true;
                    //ControlsManager.Instance.UpdateControls();
                    ControlsManager.FireUndoEndEvent(mesh, this, initialPosition, initialRotation);

                    

                    
                }
                else
                {
                    Extrudable.ChangeManifold(initialManifold);
                }*/
                DisableSBSPlane();
            }
             

        }

        private void MergeWithCollidingFaces()
        {

            //Collecting all moved faces and static faces and remove faces if they overlap.

            var facesToRemove = new List<int>();

            Manifold manifold = Extrudable._manifold;


            int noFaces = manifold.NumberOfFaces();

            
            var vertexIds = new int[manifold.NumberOfVertices()];
            var halfedgeIds = new int[manifold.NumberOfHalfEdges()];
            int[] allFaces = new int[noFaces];
            Extrudable._manifold.GetHMeshIds(vertexIds, halfedgeIds, allFaces);

            

            int[] staticFaces = allFaces;

         
            HashSet<int> facesBeingExtruded = new HashSet<int>();

            //collect all neighbouring faces of extruded faces, theses faces are the one connecting the vertices of the old face to the face being moved. 
            foreach(int face0 in extrudingFaces)
            {
                foreach(int face1 in Extrudable._manifold.GetAdjacentFaceIdsAndEdgeCenters(face0).faceId)
                {
                    facesBeingExtruded.Add(face1);
                }
                //the moved face itself
                facesBeingExtruded.Add(face0);
            }

           

            foreach (int faceMoving in facesBeingExtruded)
            {
                foreach (int faceStatic in staticFaces)
                {
                    if (faceMoving == faceStatic) { break; }

                    if (facingFaces(faceMoving, faceStatic)) //initial cheap check
                    {
                        var verticesFromFaceMoving = Extrudable._manifold.GetVertexPositionsFromFace(faceMoving);
                        var verticesFromFaceStatic = Extrudable._manifold.GetVertexPositionsFromFace(faceStatic);
                        if (verticesFromFaceMoving.Count() == verticesFromFaceStatic.Count())
                        {
                            var matches = faceBridgingVertexAssignment(verticesFromFaceMoving, verticesFromFaceStatic);

                            if (facingFaces(matches, faceMoving, verticesFromFaceMoving, faceStatic, verticesFromFaceStatic))
                            {
                                if (matches.Length == 6)
                                {
                                    facesToRemove.Add(faceMoving);
                                    facesToRemove.Add(faceStatic);
                                }
                                else if (matches.Length == 8)
                                {
                                    facesToRemove.Add(faceMoving);
                                    facesToRemove.Add(faceStatic);
                                }
                            }
                        }



                        

                    }
                }
            }

            //remove facing faces and stich everything together
            foreach (int face in facesToRemove)
            {
                Extrudable._manifold.RemoveFace(face);
            }
            Extrudable._manifold.StitchMesh(0.05);
        }

        public void UpdatePositionAndRotation(Vector3 center, Vector3 normal, Vector3 edge)
        {
            transform.localPosition = center;
            Vector3 direction = Vector3.Cross(normal, edge);
            Quaternion rotation = Quaternion.LookRotation(direction, normal);
            transform.localRotation = rotation;
            // Adjust rotation
            transform.Rotate(90, 0, 0);
        }

        void OnTriggerEnter(Collider collider)
        {
            
            if (IsDragged)
            {
                if (collider.GetComponent<FaceHandleController>() != null)
                {
                    collision = true;
                    collidedFaceHandle = collider.GetComponent<FaceHandleController>();
                    collidedFaceID = collidedFaceHandle.AssociatedFaceID;
                    collidedFaceHandle.StartHighlight();
                }
            }

        }

        void OnTriggerExit(Collider collider)
        {
            if (collidedFaceHandle != null)
            {
                collidedFaceHandle.EndHighlight();
                collidedFaceHandle = null;
                collision = false;
            }
        }

        public int[] faceBridgingVertexAssignment(Dictionary<int, Vector3> face1vertices, Dictionary<int, Vector3> face2vertices)
        {
            List<int> keyList1 = new List<int>(face1vertices.Keys);
            List<int> keyList2 = new List<int>(face2vertices.Keys);

            float min = 0;

            if(keyList1.Count == 4) {
                int[] res = new int[8];
                res[0] = keyList1[0]; res[1] = keyList1[1]; res[2] = keyList1[2]; res[3] = keyList1[3];

                for (int a = 0; a < 4; a++)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        if (b == a) continue;

                        for (int c = 0; c < 4; c++)
                        {
                            if (c == a || c == b) continue;
                            for (int d = 0; d < 4; d++)
                            {
                                if (d == a || d == b || d == c) continue;

                                float match1 = Vector3.Distance(face1vertices[keyList1[0]], face2vertices[keyList2[a]]);
                                float match2 = Vector3.Distance(face1vertices[keyList1[1]], face2vertices[keyList2[b]]);
                                float match3 = Vector3.Distance(face1vertices[keyList1[2]], face2vertices[keyList2[c]]);
                                float match4 = Vector3.Distance(face1vertices[keyList1[3]], face2vertices[keyList2[d]]);

                                if ((match1 + match2 + match3 + match4) < min || min == 0)
                                {
                                    min = match1 + match2 + match3 + match4;
                                    res[4] = keyList2[a]; res[5] = keyList2[b]; res[6] = keyList2[c]; res[7] = keyList2[d];
                                }
                            }
                        }
                    }
                }
                return res;
            } else
            {
                int[] res = new int[6];
                res[0] = keyList1[0]; res[1] = keyList1[1]; res[2] = keyList1[2];

                for (int a = 0; a < 3; a++)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        if (b == a) continue;

                        for (int c = 0; c < 3; c++)
                        {
                            if (c == a || c == b) continue;

                            float match1 = Vector3.Distance(face1vertices[keyList1[0]], face2vertices[keyList2[a]]);
                            float match2 = Vector3.Distance(face1vertices[keyList1[1]], face2vertices[keyList2[b]]);
                            float match3 = Vector3.Distance(face1vertices[keyList1[2]], face2vertices[keyList2[c]]);

                            if ((match1 + match2 + match3) < min || min == 0)
                            {
                                min = match1 + match2 + match3;
                                res[3] = keyList2[a]; res[4] = keyList2[b]; res[5] = keyList2[c];
                            }                           
                        }
                    }
                }
                return res; 
            }
        }

        public bool facingFaces(int[] matches, int face1ID, Dictionary<int, Vector3> face1vertices, int face2ID, Dictionary<int, Vector3> face2vertices)
        {
            Vector3 f1norm = Extrudable.GetFaceNormal(face1ID);
            Vector3 f2norm = Extrudable.GetFaceNormal(face2ID);

            //Debug.Log("Normals: " +f1norm + " , " + f2norm);

            //if (Vector3.Dot(f1norm, f2norm) <= -0.8) { return false}

            for (int i = 0; i < (matches.Length/2); i++)
            {
                if((face1vertices[matches[i]] - face2vertices[matches[i + (matches.Length / 2)]]).magnitude >= 0.02f)
                {
                    return false;
                }

                if ((face2vertices[matches[i + (matches.Length / 2)]] - face1vertices[matches[i]]).magnitude >= 0.02f)
                {
                    return false;
                }
            }
            Debug.Log("Normals: " + f1norm + " , " + f2norm);
            return true;
        }

        public bool facingFaces(int face1ID, int face2ID)
        {
            

            //Vector3 f1norm = Extrudable.GetFaceNormal(face1ID);
            //Vector3 f2norm = Extrudable.GetFaceNormal(face2ID);

            Vector3 pos1 = Extrudable._manifold.GetCenter(face1ID);
            Vector3 pos2 = Extrudable._manifold.GetCenter(face2ID);

            float distanceBetweenCenters = (pos1 - pos2).magnitude;

            

            if (/*Vector3.Dot(f1norm, f2norm) <= -0.8 && */distanceBetweenCenters < 0.005) 
            {
                return true; 
            }

            return false;
        }

        public bool hasDistinctAdjacentFaces(FaceHandleController faceHandle)
        {

            foreach(int face1 in initialManifold.GetAdjacentFaceIdsAndEdgeCenters(AssociatedFaceID).faceId)
            {
                foreach (int face2 in initialManifold.GetAdjacentFaceIdsAndEdgeCenters(faceHandle.AssociatedFaceID).faceId)
                {
                    if (face1 == face2 || AssociatedFaceID == face2) // if neighbours, then false
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void initializeMeasuringBands()
        {
            /*
            var texture = new Texture2D(16, 16, TextureFormat.ARGB32, false);

            Color32[] pixelColors = new Color32[16 * 16];
            for (int j = 0; j < pixelColors.Length; j = j + 2)
            {
                pixelColors[j] = Color.black;
                pixelColors[j+1] = Color.clear;
            }
            */
            int index = 0;
            var texture = new Texture2D(80, 15, TextureFormat.ARGB32, false);

            Color32[] pixelColors = new Color32[80 * 15];

            for(int a = 0; a < 30; a++)
            {
                pixelColors[index] = Color.black;
                index++;
                for(int b = 0; b < 6; b++)
                {
                    pixelColors[index] = Color.clear;
                    index++;
                }
                pixelColors[index] = Color.black;
                index++;
            }
            
            for (int a = 0; a < 2; a++)
            {
                pixelColors[index] = Color.black;
                index++;
                for (int b = 0; b < 38; b++)
                {
                    pixelColors[index] = Color.clear;
                    index++;
                }
                pixelColors[index] = Color.black;
                index++;
            }

            for (int a = 0; a < 2; a++)
            {
                pixelColors[index] = Color.black;
                index++;
                for (int b = 0; b < 78; b++)
                {
                    pixelColors[index] = Color.clear;
                    index++;
                }
                pixelColors[index] = Color.black;
                index++;
            }

            for (int a = 0; a < 3; a++)
            {
                for (int b = 0; b < 80; b++)
                {
                    pixelColors[index] = Color.clear;
                    index++;
                }
            }

            for (int a = 0; a < 2; a++)
            {
                pixelColors[index] = Color.black;
                index++;
                for (int b = 0; b < 78; b++)
                {
                    pixelColors[index] = Color.clear;
                    index++;
                }
                pixelColors[index] = Color.black;
                index++;
            }

            for (int a = 0; a < 2; a++)
            {
                pixelColors[index] = Color.black;
                index++;
                for (int b = 0; b < 38; b++)
                {
                    pixelColors[index] = Color.clear;
                    index++;
                }
                pixelColors[index] = Color.black;
                index++;
            }

            for (int a = 0; a < 30; a++)
            {
                pixelColors[index] = Color.black;
                index++;
                for (int b = 0; b < 6; b++)
                {
                    pixelColors[index] = Color.clear;
                    index++;
                }
                pixelColors[index] = Color.black;
                index++;
            }
            
            texture.SetPixels32(pixelColors);
            texture.Apply();
            texture.wrapModeU = TextureWrapMode.Repeat;
            texture.wrapModeV = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            int i = 0;
            foreach (int faceid in Extrudable._manifold.GetAdjacentFaceIdsAndEdgeCenters(AssociatedFaceID).faceId) if (!extrudingFaces.Contains(faceid)) // maybe copy the first gameobject instead of intializing 4 objects???
                {
                GameObject measuringBand = new GameObject("measuringBand" + i);
                measuringBand.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = measuringBand.AddComponent<MeshFilter>();
                Mesh mesh = meshFilter.mesh;

                Vector3[] vertices = new Vector3[] { new Vector3(0f, 0f, 0f),
                                                        new Vector3(0f, 0f, 0f),
                                                        new Vector3(0f, 0f, 0f),
                                                        new Vector3(0f, 0f, 0f)};
      

                var triangles = new int[] { 0, 1, 2, 0, 2, 3 };

                var uvs = new Vector2[] {   new Vector2(0f, 0f),
                                            new Vector2(0f, 1f),
                                            new Vector2(1f, 0f),
                                            new Vector2(1f, 1f)};

                mesh.Clear();
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uvs;
                //mesh.RecalculateNormals();

                measuringBand.transform.position = Extrudable.transform.position;
                measuringBand.transform.rotation = Extrudable.transform.rotation;
                measuringBand.transform.localScale = Extrudable.transform.lossyScale;

                Material mbMaterial = new Material(Shader.Find("Standard"));
                mbMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mbMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mbMaterial.SetInt("_ZWrite", 0);
                mbMaterial.DisableKeyword("_ALPHATEST_ON");
                mbMaterial.EnableKeyword("_ALPHABLEND_ON");
                mbMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mbMaterial.renderQueue = 3000;

                mbMaterial.SetTexture("_MainTex", texture);
                measuringBand.GetComponent<Renderer>().material = mbMaterial;

                measuringBands[i] = measuringBand;
                i++;
          
            }

        }

        public void updateMeasuringBands(float extrusionLength)
        {
            if (measuringBands.Count() > 0) {

                Vector2[] uvs = new Vector2[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(extrusionLength * 5f, 1f), new Vector2(extrusionLength * 5f, 0f) };

                int i = 0;
                foreach (int faceid in Extrudable._manifold.GetAdjacentFaceIdsAndEdgeCenters(AssociatedFaceID).faceId) if (!extrudingFaces.Contains(faceid))
                    {
                    var faceVertices = Extrudable._manifold.GetVertexPositionsFromFace(faceid).Values.ToList();
                    for (int x = 0; x < faceVertices.Count(); x++)
                    {
                        faceVertices[x] = faceVertices[x] + Vector3.Scale(Extrudable._manifold.GetFaceNormal(faceid), new Vector3(0.0001f, 0.0001f, 0.0001f));
                    }

                    if (measuringBands[i] != null)
                    {
                        measuringBands[i].GetComponent<MeshRenderer>().enabled = true;
                        measuringBands[i].GetComponent<MeshFilter>().mesh.vertices = new Vector3[] { faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3] }; // garbage collection
                        measuringBands[i].GetComponent<MeshFilter>().mesh.uv = uvs;
                    }
                    i++;
                }
            }
        }

        public void destroyMeasuringBands()
        {
            foreach(GameObject measuringBand in measuringBands)
            {
                Destroy(measuringBand);
            }
        }

        public void hideMeasuringBands()
        {
            foreach (GameObject measuringBand in measuringBands)
            {
                if (measuringBand != null)
                {
                    measuringBand.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }

        public Vector3 pointToPlaneVector(Vector3 planeNorm, Vector3 planePos, Vector3 vertexPos)
        {
            float sb, sn, sd;

            sn = -Vector3.Dot(planeNorm, (vertexPos - planePos));
            sd = Vector3.Dot(planeNorm, planeNorm);
            sb = sn / sd;

            Vector3 result = vertexPos + sb * planeNorm;
            return result - vertexPos;
        }

    }
}