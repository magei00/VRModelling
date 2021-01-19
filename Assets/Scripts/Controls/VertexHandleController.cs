using UnityEngine;
using System.Collections.Generic;
using Assets.GEL;


namespace Controls
{
    public class VertexHandleController : IInteractableObject
    {
        public enum RefinementMode
        {
            LOOP,
            EDGE,
            NO      
        }

        public bool IsDragged = false;
        public int AssociatedVertexID;

        public ExtrudableMesh Extrudable;

        public OvrAvatar Avatar;

        private Vector3 initialControllerOffset;

        private HoverHighlight _hoverHighlight;

        private Mesh mesh;

        private Manifold initialManifold;

        private Vector3 initialPosition;
        private float minDeltaY;
        private Quaternion initialRotation;

        private Quaternion initialControllerRotation;

        private static List<VertexHandleController> activeControllers = new List<VertexHandleController>();
        private int updateInFrame;
        private Transform interactingControllerCollider;

        private InteractionMode mode = InteractionMode.SINGLE;

        private RefinementMode refinementMode = RefinementMode.NO;

        public bool refinementActive = true;
        private GrabControl gc;

        private Manifold splitManifold;
        private RefinementMode lastRefinementMode = RefinementMode.NO;
        private Dictionary<int, int> adjacentVertices;

        public GameObject edgebarPrefab;

        private GameObject edgebar;

        public GameObject lineObjectBP;
        private GameObject lineObject;

        void Awake()
        {
            _hoverHighlight = GetComponent<HoverHighlight>();
            var meshFilter = GetComponent<MeshFilter>();
            mesh = meshFilter.sharedMesh;
            
        }

        // Update is called once per frame
        void Update()
        {

            Vector3 left_ctrl_pt = GameObject.Find("LeftHandAnchor").transform.position;
            Vector3 right_ctrl_pt = GameObject.Find("RightHandAnchor").transform.position;
            float dist_left_ctrl = (10.0f * (left_ctrl_pt - transform.position)).sqrMagnitude;
            float dist_right_ctrl = (10.0f * (right_ctrl_pt - transform.position)).sqrMagnitude;
            float sz = 0.1f + 0.9f / (Mathf.Min(dist_left_ctrl, dist_right_ctrl) + 1.0f);
            transform.localScale = new Vector3(sz, sz, sz);

            if (updateInFrame == Time.frameCount) return;
            if (activeControllers.Count == 1 && activeControllers[0] == this)
            {
                if (IsDragged)
                {
                    if (ControlsManager.Instance.GetHandle(1) != null)
                    {
                        ControlsManager.Instance.ClearFaceHandlesAndEdges();
                    }

                    Vector3 controllerPosInLocalSpace = transform.parent.worldToLocalMatrix.MultiplyPoint(_controllerCollider.transform.position);
                    Vector3 targetPos = controllerPosInLocalSpace - initialControllerOffset;
                    //targetPos.y = Mathf.Max(minDeltaY, targetPos.y); I don't know what this line was supposed to do, if something is broken in the futere maybe this is needed.
                    transform.localPosition = targetPos;

                    Extrudable.MoveVertexTo(
                    AssociatedVertexID,
                    transform.localPosition);

                    GrabControl leftGrabControl = GameObject.Find("leftGrabControl").GetComponent<GrabControl>();
                    GrabControl rightGrabControl = GameObject.Find("rightGrabControl").GetComponent<GrabControl>();

                    //SBS
                    if (leftGrabControl.collidedVertexHandle != null && leftGrabControl.HandState.ToString().Equals("TOUCHING") && !OVRInput.Get(OVRInput.Touch.Any, OVRInput.Controller.LTouch))
                    {
                        Matrix4x4 LtoW = leftGrabControl.collidedVertexHandle.transform.localToWorldMatrix;
                        Vector3 touchPoint = leftGrabControl.collidedVertexHandle.transform.localPosition;
                        Vector3 distanceVector = initialPosition - touchPoint ;
                        Vector3 translationVector = new Vector3(0f, 0f, 0f);

                        if (Mathf.Abs(distanceVector.x) >= Mathf.Abs(distanceVector.y) && Mathf.Abs(distanceVector.x) >= Mathf.Abs(distanceVector.z))
                            translationVector = new Vector3(distanceVector.x, 0f, 0f);

                        else if (Mathf.Abs(distanceVector.y) >= Mathf.Abs(distanceVector.x) && Mathf.Abs(distanceVector.y) >= Mathf.Abs(distanceVector.z))
                            translationVector = new Vector3(0f, distanceVector.y, 0f);

                        else if (Mathf.Abs(distanceVector.z) >= Mathf.Abs(distanceVector.x) && Mathf.Abs(distanceVector.z) >= Mathf.Abs(distanceVector.y))
                            translationVector = new Vector3(0f, 0f, distanceVector.z);

                        Extrudable.MoveVertexTo(
                            AssociatedVertexID,
                            leftGrabControl.collidedVertexHandle.transform.localPosition + translationVector);

                        Debug.Log(leftGrabControl.collidedVertexHandle.transform.position + "  :  " + leftGrabControl.collidedVertexHandle.transform.TransformPoint(touchPoint));
                        EnableSBSLine(leftGrabControl.collidedVertexHandle.transform.localPosition, translationVector, leftGrabControl.collidedVertexHandle.transform.root.localToWorldMatrix);
                    }
                    else if (rightGrabControl.collidedVertexHandle != null && rightGrabControl.HandState.ToString().Equals("TOUCHING") && !OVRInput.Get(OVRInput.Touch.Any, OVRInput.Controller.RTouch))
                    {
                        Vector3 touchPoint = rightGrabControl.collidedVertexHandle.transform.localPosition;
                        Vector3 distanceVector = initialPosition - touchPoint;
                        Vector3 translationVector = new Vector3(0f, 0f, 0f);

                        if (Mathf.Abs(distanceVector.x) >= Mathf.Abs(distanceVector.y) && Mathf.Abs(distanceVector.x) >= Mathf.Abs(distanceVector.z))
                            translationVector = new Vector3(distanceVector.x, 0f, 0f);

                        else if (Mathf.Abs(distanceVector.y) >= Mathf.Abs(distanceVector.x) && Mathf.Abs(distanceVector.y) >= Mathf.Abs(distanceVector.z))
                            translationVector = new Vector3(0f, distanceVector.y, 0f);

                        else if (Mathf.Abs(distanceVector.z) >= Mathf.Abs(distanceVector.x) && Mathf.Abs(distanceVector.z) >= Mathf.Abs(distanceVector.y))
                            translationVector = new Vector3(0f, 0f, distanceVector.z);

                        Extrudable.MoveVertexTo(
                            AssociatedVertexID,
                            rightGrabControl.collidedVertexHandle.transform.localPosition + translationVector);

                        EnableSBSLine(rightGrabControl.collidedVertexHandle.transform.localPosition, translationVector, leftGrabControl.collidedVertexHandle.transform.root.localToWorldMatrix );
                    }
                    else
                    {
                        DisableSBSLine();
                    }

                    ControlsManager.Instance.Extrudable.rebuild = true;
                    //ControlsManager.Instance.UpdateControls();
                }
            }
            else
            {
                DisableSBSLine();
                if (activeControllers.Count == 2 && refinementActive)
                {
                    if (mode == InteractionMode.DUAL)
                    {
                        Dictionary<string, float> angleDict = new Dictionary<string, float>();
                        foreach (VertexHandleController v in activeControllers)
                        {
                            float angle = Mathf.Abs(v.interactingControllerCollider.transform.rotation.eulerAngles.z - v.initialControllerRotation.eulerAngles.z) % 180f;

                            if (v.interactingControllerCollider.gameObject.name.StartsWith("right"))
                            {
                                angleDict["right"] = angle;
                            }
                            else if (v.interactingControllerCollider.gameObject.name.StartsWith("left"))
                            {
                                angleDict["left"] = angle;
                            }
                        }

                        refinementMode = DetermineRefinement(angleDict);
                        //Show what refinement would do
                        if (refinementMode != lastRefinementMode)
                        {
                            if (refinementMode == RefinementMode.LOOP)
                            {
                                ShowRefinement(activeControllers[0].AssociatedVertexID, activeControllers[1].AssociatedVertexID);
                            }
                            else if (refinementMode == RefinementMode.NO)
                            {
                                GameObject refinePreview = GameObject.FindGameObjectWithTag("RefinePreview");
                                refinePreview.GetComponent<MeshRenderer>().enabled = false;
                            }
                        }

                        lastRefinementMode = refinementMode;
                    }
                }
            }
        }

        private void DisableSBSLine()
        {
            if(lineObject != null)
            {
                Destroy(lineObject);
            }
        }

        private void EnableSBSLine(Vector3 point, Vector3 translation, Matrix4x4 m)
        {
            if (lineObject == null)
            {
                lineObject = Instantiate(lineObjectBP);
                lineObject.transform.parent = ControlsManager.Instance.transform;
                lineObject.transform.localPosition = new Vector3(0,0,0);
                lineObject.transform.localRotation = new Quaternion(0,0,0,0);
                lineObject.transform.localScale = new Vector3(1, 1, 1);

                LineRenderer lr = lineObject.GetComponent<LineRenderer>();
                lr.enabled = true;

                Vector3 p1 = point;

                Vector3 p2 = (point + translation);

                Vector3 direction = p1 - p2;
                //lr.startWidth = lr.endWidth = 0.5f;
                //lr.startColor = lr.endColor = Color.green;
                //lr.positionCount = 2;
                //Debug.Log(lr);
                Vector3 yOffset = new Vector3(0, 0.0f, 0);
                lr.SetPosition(0, p1 + yOffset + direction * 10);
                lr.SetPosition(1, p2 + yOffset + direction * -10);
                
                
            }
        }

        public override void Interact()
        {

            //Debug.Log(AssociatedVertexID);
            interactingControllerCollider = _controllerCollider.transform;
            activeControllers.Add(this);

            //splitManifold = Extrudable._manifold.Copy();
            initialManifold = Extrudable._manifold.Copy();

            //Debug.Log(Extrudable._manifold.)
            adjacentVertices = AdjacentVertices();

            if (activeControllers.Count == 1)
            {
                ChangeInteraction(InteractionMode.SINGLE);
                IsDragged = true;

                initialPosition = transform.localPosition;
                initialRotation = transform.localRotation;

                Vector3 controllerPosInLocalSpace = transform.parent.worldToLocalMatrix.MultiplyPoint(_controllerCollider.transform.position);
                initialControllerOffset = controllerPosInLocalSpace - transform.localPosition;
                ControlsManager.Instance.Extrudable.rebuild = true;
                ControlsManager.FireUndoStartEvent(mesh, this, initialPosition, initialRotation);
                if (ControlsManager.Instance.turntable)
                    minDeltaY = transform.parent.InverseTransformPoint(Vector3.one * ControlsManager.Instance.turntable.GetMaxYWithBounds()).y;
                else
                    minDeltaY = -100f;

                // Display two handed options
            }
            else if (activeControllers.Count == 2 && refinementActive && adjacentVertices.ContainsKey(activeControllers[0].AssociatedVertexID) && adjacentVertices.ContainsKey(activeControllers[1].AssociatedVertexID))
            {
                activeControllers[0].ChangeInteraction(InteractionMode.DUAL);
                ChangeInteraction(InteractionMode.DUAL);
                Extrudable.MoveVertexTo(activeControllers[0].AssociatedVertexID, activeControllers[0].transform.localPosition);
                Extrudable.rebuild = true;
                initialRotation = transform.localRotation;
                AddEdgeBarSignifier();
            }

            ControlsManager.Instance.DestroyFacesAndEdgeHandles();

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
            //if (activeControllers.Count == 1)
            //{
            //    activeControllers[0].ChangeInteraction(InteractionMode.SINGLE);
            //}
            //else
            if (mode == InteractionMode.SINGLE)
            {
                activeControllers.Remove(this);
                if (IsDragged)
                {
                    IsDragged = false;

                    int collapsed = Extrudable.CollapseShortEdges(0.019f);
                    if (collapsed > 0)
                    {
                        Extrudable.TriangulateAndDrawManifold();
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
                    }
                    else if (Extrudable.isValidMesh())
                    {
                        ControlsManager.Instance.Extrudable.rebuild = true;
                        //ControlsManager.Instance.UpdateControls();
                        ControlsManager.FireUndoEndEvent(mesh, this, initialPosition, initialRotation);
                    }                
                    else
                    {
                        Extrudable.ChangeManifold(initialManifold);
                    }          
                }
            }
            else
            {

                if (mode == InteractionMode.DUAL && refinementActive)
                {

                    if (refinementMode == RefinementMode.LOOP)
                    {
                        // This or TriangulateAndDraw + ControlsManager.Instance.the function that clears and updates
                        //Extrudable._manifold = splitManifold;
                        //Extrudable.UpdateMesh();
                        Extrudable.ChangeManifold(splitManifold);
                        ControlsManager.FireUndoEndEvent(mesh, this, initialPosition, initialRotation);
                    }

                    if (activeControllers.Count == 2)
                    {
                        // Do the refinement and clear the splitManifold
                        //ShowRefinement(activeControllers[0].AssociatedVertexID, activeControllers[1].AssociatedVertexID);
                        activeControllers.Remove(this);

                        activeControllers[0].ChangeInteraction(InteractionMode.SINGLE);
                        activeControllers[0].EndHighlight();
                        activeControllers[0].IsDragged = false;
                        activeControllers[0].StopInteraction();
                    }

                    GameObject refinePreview = GameObject.FindGameObjectWithTag("RefinePreview");
                    refinePreview.GetComponent<MeshRenderer>().enabled = false;

                    ChangeInteraction(InteractionMode.SINGLE);
                    refinementMode = RefinementMode.NO;
                }
                
            }
            //ControlsManager.Instance.UpdateControls();

            if (edgebar)
            Destroy(edgebar);

            DisableSBSLine();
        }

        private RefinementMode DetermineRefinement(Dictionary<string, float> angleDict)
        {
            RefinementMode refmode = RefinementMode.NO;

            //if ((angleDict["left"] > 20f && angleDict["left"] < 90f) && (angleDict["right"] > 20f && angleDict["right"] < 90f))
            if ((angleDict["left"] > 30f) && (angleDict["right"] > 30f))
            {
                refmode = RefinementMode.LOOP;
            }

            return refmode;
        }

        private void ShowRefinement(int vertexid1, int vertexid2)
        {
            if (activeControllers.Count == 2)
            {
                //splitManifold = Extrudable._manifold.Copy();

                //int edge = adjacentVertices[activeControllers[1].AssociatedVertexID];

                /*
                Debug.Log("vertexid2: " + vertexid2);

                string text = "";

                foreach (KeyValuePair<int, int> kvp in adjacentVertices)
                {
                    //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                    text += string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                }
                Debug.Log(text);
                */
                int edge = adjacentVertices[vertexid2];

                splitManifold = Extrudable._manifold.Copy();

                int edge2 = splitManifold.GetNextHalfEdge(edge);

                edge2 = splitManifold.GetNextHalfEdge(edge2);

                splitManifold.RefineFaceloop(edge, edge2);

                //Extrudable._manifold.RefineFaceloop(edge, edge2);
                splitManifold.StitchMesh(1e-10);
                var newMesh = new Mesh();
                newMesh.vertices = mesh.vertices;
                newMesh.normals = mesh.normals;
                newMesh.subMeshCount = 2;
                newMesh.SetIndices(mesh.triangles, MeshTopology.Triangles, 0);
                //newMesh.SetIndices(new int[0], MeshTopology.Lines, 1);
                mesh = newMesh;
                mesh.UploadMeshData(false);
                GameObject refinePreview = GameObject.FindGameObjectWithTag("RefinePreview");
                refinePreview.GetComponent<MeshFilter>().mesh = CreatePreviewMesh();
                refinePreview.GetComponent<MeshRenderer>().enabled = true;

            }
        }

        private Dictionary<int, int> AdjacentVertices()
        {
            var faceIds = new int[Extrudable._manifold.NumberOfFaces()];
            var vertexIds = new int[Extrudable._manifold.NumberOfVertices()];
            var halfedgeIds = new int[Extrudable._manifold.NumberOfHalfEdges()];
            Extrudable._manifold.GetHMeshIds(vertexIds, halfedgeIds, faceIds);
            List<int> halfedgeToVertex = new List<int>();

            
            //Debug.Log(halfedgeIds.Length);

            for (int i = 0; i < halfedgeIds.Length; i++)
            {
                int h = halfedgeIds[i];
                //Debug.Log("halfedgeID: " + h + " oppositeHalfedgeID: " + Extrudable._manifold.GetOppHalfEdge(h));
                if (Extrudable._manifold.IsHalfedgeInUse(h))
                {
                    int v = Extrudable._manifold.GetVertexId(h);
                    if (v == AssociatedVertexID)
                    {
                        halfedgeToVertex.Add(h);
                    }
                }
            }

            Dictionary<int, int> adjacentVertexIds = new Dictionary<int, int>();

            foreach (int h in halfedgeToVertex)
            {
                int nextHalfEdge = Extrudable._manifold.GetNextHalfEdge(h);
                if (!adjacentVertexIds.ContainsKey(Extrudable._manifold.GetVertexId(h)))
                    adjacentVertexIds.Add(Extrudable._manifold.GetVertexId(h), nextHalfEdge);
                int lastHalfedge = nextHalfEdge;
                while (true)
                {
                    nextHalfEdge = Extrudable._manifold.GetNextHalfEdge(lastHalfedge);
                    if (nextHalfEdge == h)
                    {
                        if (!adjacentVertexIds.ContainsKey(Extrudable._manifold.GetVertexId(lastHalfedge)))
                            adjacentVertexIds.Add(Extrudable._manifold.GetVertexId(lastHalfedge), Extrudable._manifold.GetNextHalfEdge(lastHalfedge));
                        break;
                    }
                    lastHalfedge = nextHalfEdge;
                }
            }

            return adjacentVertexIds;

        }

        private Mesh CreatePreviewMesh()
        {
            var pointsAndQuads = splitManifold.ToIdfs();

            var points = pointsAndQuads.Key;

            List<int> edges = new List<int>();
            var vertices = new Vector3[splitManifold.NumberOfAllocatedVertices()];

            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3((float)points[3 * i], (float)points[3 * i + 1], (float)points[3 * i + 2]);
            }

            int[] polygons = pointsAndQuads.Value;
            List<int> polygonsFinal = new List<int>();
            List<Vector3> verticesFinal = new List<Vector3>();
            List<Vector3> normalsFinal = new List<Vector3>();

            for (var i = 0; i < polygons.Length;)
            {
                int polyCount = polygons[i];
                i++;

                // add final vertices
                int vertexBase = verticesFinal.Count;
                for (int j = 0; j < polyCount; j++)
                {
                    verticesFinal.Add(vertices[polygons[i + j]]);
                }
                Vector3 normal = Vector3.zero;

                // triangulate polygon
                for (int j = 1; j + 1 < polyCount; j++)
                {
                    polygonsFinal.Add(vertexBase);
                    polygonsFinal.Add(vertexBase + j);
                    polygonsFinal.Add(vertexBase + j + 1);

                    normal += -Vector3.Cross(verticesFinal[vertexBase] - verticesFinal[vertexBase + j],
                        verticesFinal[vertexBase + j + 1] - verticesFinal[vertexBase + j]).normalized;
                }
                normal.Normalize();

                for (int j = 0; j < polyCount; j++)
                {
                    normalsFinal.Add(normal);
                    edges.Add(vertexBase + j);
                    edges.Add(vertexBase + (j + 1) % polyCount);
                }

                i += polyCount;
            }

            Mesh splitMesh = new Mesh();
            splitMesh.name = "extruded";
            splitMesh.SetIndices(new int[0], MeshTopology.Triangles, 0);
            //splitMesh.SetIndices(new int[0], MeshTopology.Lines, 1);
            splitMesh.vertices = verticesFinal.ToArray();
            //splitMesh.normals = normalsFinal.ToArray();
            splitMesh.subMeshCount = 2;
            splitMesh.SetIndices(polygonsFinal.ToArray(), MeshTopology.Triangles, 0);
            splitMesh.SetIndices(edges.ToArray(), MeshTopology.Lines, 1);
            splitMesh.UploadMeshData(false); 

            return splitMesh;
        }

        private void AddEdgeBarSignifier()
        {
            if (activeControllers.Count == 2)
            {
                edgebar = Instantiate(edgebarPrefab);
                //edgebar.transform.parent = activeControllers[0].transform;
                edgebar.transform.position = activeControllers[0].transform.position + (activeControllers[1].transform.position - activeControllers[0].transform.position) / 2.0f;
                float length = (Extrudable._manifold.VertexPosition(activeControllers[1].AssociatedVertexID) - Extrudable._manifold.VertexPosition(activeControllers[0].AssociatedVertexID)).magnitude;
                Vector3 tempScale = edgebar.transform.localScale;
                tempScale.y = 1.55f / 0.2f * length;
                edgebar.transform.localScale = tempScale;
                //edgebar.transform.rotation = Quaternion.LookRotation((activeControllers[1].transform.position - activeControllers[0].transform.position)).normalized);
                //set rotation
                Vector3 v = activeControllers[1].transform.position - activeControllers[0].transform.position;
                Quaternion q = Quaternion.LookRotation(v);

                edgebar.transform.rotation = q * edgebar.transform.rotation;
            }
        }
    }
}
