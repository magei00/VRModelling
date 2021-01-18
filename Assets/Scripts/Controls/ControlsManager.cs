using System;
using System.Collections.Generic;
using System.Linq;
using Assets.GEL;
using UnityEngine;

namespace Controls
{
    // Overall responsible for all handle controls on the model
    public class ControlsManager : MonoBehaviour
    {
        public GameObject FaceHandlePrefab;
        public GameObject LatchPrefab;
        public GameObject VertexHandlePrefab;

        public ExtrudableMesh Extrudable;

        public static ControlsManager Instance;

        public float globalScale = 1.0f;

        private Dictionary<int, FaceHandleController> _faceHandles = new Dictionary<int, FaceHandleController>();
        private Dictionary<int, EdgeHandleController> _edgeHandles = new Dictionary<int, EdgeHandleController>();
        private Dictionary<int, VertexHandleController> _vertexHandles = new Dictionary<int, VertexHandleController>();

        private List<KeyValuePair<int, int>> _closedEdgeBackup = new List<KeyValuePair<int, int>>();

        public event Action undoReset = delegate () { };

        public event Action<Mesh, IInteractableObject, Vector3, Quaternion> undoStartEventHandlers =
            delegate (Mesh m, IInteractableObject io, Vector3 initialPosition, Quaternion initialRotation) { };

        public event Action<Mesh, IInteractableObject, Vector3, Quaternion> undoEndEventHandlers =
            delegate (Mesh m, IInteractableObject io, Vector3 initialPosition, Quaternion initialRotation) { };

        public event Action<float> globalScaleChange = delegate (float f) { };

        public Transform[] handCenters;

        public Turntable turntable;

        public UndoManager undoManager;

        public bool hideControls = false;

        public void Update() //check if hands are below turntable, if true hide controls
        {
            bool doHideControls = false;
            if (turntable)
            {
                var turntableHeight = turntable.GetMaxY();
                doHideControls = true;
                foreach (var c in handCenters)
                {
                    if (c.position.y >= turntableHeight)
                    {
                        doHideControls = false;
                    }
                }
            }
            
            if (hideControls != doHideControls && false) //Disabled for now
            {
                hideControls = doHideControls;
                undoManager.hideUndo = hideControls;
                undoManager.UpdateVisuals();

                foreach (var fh in _faceHandles)
                {
                    fh.Value.GetComponent<MeshRenderer>().enabled = !hideControls;
                }
                foreach (var fh in _edgeHandles)
                {
                    fh.Value.GetComponent<MeshRenderer>().enabled = !hideControls;
                }
                foreach (var fh in _vertexHandles)
                {
                    fh.Value.GetComponent<MeshRenderer>().enabled = !hideControls;
                }
            }
        }

        public void SetGlobalScale(float val)
        {
            if (val == globalScale) return;
            globalScaleChange(val);
        }

        public static void FireUndoStartEvent(Mesh m, IInteractableObject io, Vector3 initialPosition, Quaternion initialRotation)
        {
            Instance.undoStartEventHandlers(m, io, initialPosition, initialRotation);
        }

        public static void FireUndoEndEvent(Mesh m, IInteractableObject io, Vector3 initialPosition, Quaternion initialRotation)
        {
            Instance.undoEndEventHandlers(m, io, initialPosition, initialRotation);
        }

        void Awake()
        {
            Instance = this;
            _faceHandles = new Dictionary<int, FaceHandleController>();
            _edgeHandles = new Dictionary<int, EdgeHandleController>();
            _vertexHandles = new Dictionary<int, VertexHandleController>();
            _closedEdgeBackup = new List<KeyValuePair<int, int>>();
        }

        void Start()
        {
            UpdateControls();
            undoReset();
        }

        public void Reset()
        {
            Extrudable.Reset();
            UpdateControls();
            undoReset();
        }

        public void Clear()
        {
            foreach (var fhc in _faceHandles)
            {
                Destroy(fhc.Value.gameObject);
            }
            _faceHandles.Clear();
            foreach (var l in _edgeHandles)
            {
                Destroy(l.Value.gameObject);
            }
            _edgeHandles.Clear();
            foreach (var vh in _vertexHandles)
            {
                Destroy(vh.Value.gameObject);
            }
            _vertexHandles.Clear();
        }

        public void ClearFaceHandlesAndEdges()
        {
            foreach (var fhc in _faceHandles)
            {
                //Destroy(fhc.Value.gameObject);
                fhc.Value.gameObject.SetActive(false);
            }
            //_faceHandles.Clear();
            foreach (var l in _edgeHandles)
            {
                //Destroy(l.Value.gameObject);
                l.Value.gameObject.SetActive(false);

            }
            //_edgeHandles.Clear();
        }

        public FaceHandleController GetHandle(int faceId)
        {
            FaceHandleController reqHandle = null;
            _faceHandles.TryGetValue(faceId, out reqHandle);
            return reqHandle;
        }

        public Dictionary<int, Vector3> GetVertexPositionsFromFaces(List<int> faces)
        {
            Dictionary<int, Vector3> res = new Dictionary<int, Vector3>();
            foreach (int faceid in faces)
            {
                var vertexIds = Extrudable._manifold.GetVertexIds(faceid);
                foreach (var vertexId in vertexIds)
                {
                    if (!res.ContainsKey(vertexId))
                    {
                        res.Add(vertexId, _vertexHandles[vertexId].transform.localPosition);
                    }
                }
            }

            return res;
        }

        public void SetVertexPositionsById(Dictionary<int, Vector3> positions)
        {
            foreach (var vertexId in positions.Keys)
            {
                Extrudable.MoveVertexTo(
                    vertexId,
                    positions[vertexId]);
            }
        }

        public void RemoveLatches(List<int> faces)
        {
            foreach (var face in faces)
            {
                var handle = GetHandle(face);
                if (handle == null)
                    continue;

                var handleController = handle.GetComponent<FaceHandleController>();

                List<EdgeHandleController> latchesToRemove = new List<EdgeHandleController>();
                foreach (var latch in handleController.ConnectedLatches)
                {
                    if (latch != null && !latch.Locked)
                    {
                        latchesToRemove.Add(latch);
                    }
                }

                foreach (var latch in latchesToRemove)
                {
                    //_edgeHandles.Remove(latch.AssociatedEdgeID);
                    //handleController.ConnectedLatches.Remove(latch);
                    //GetHandle(latch.GetComponent<EdgeHandleController>().GetOtherFace(handleController.AssociatedFaceID)).ConnectedLatches.Remove(latch);

                    //Destroy(latch.gameObject);
                    latch.gameObject.SetActive(false);
                }
            }
        }

        public void UpdateControls()
        {
            Manifold manifold = Extrudable._manifold;
            var faceIds = new int[manifold.NumberOfFaces()];
            var vertexIds = new int[manifold.NumberOfVertices()];
            var halfedgeIds = new int[manifold.NumberOfHalfEdges()];
            manifold.GetHMeshIds(vertexIds, halfedgeIds, faceIds);

            // update face handles
            foreach (var faceHandleController in _faceHandles)
            {
                var handleFace = faceHandleController.Value.AssociatedFaceID;

                if (!faceHandleController.Value.IsDragged)
                {
                    var center = manifold.GetCenterTriangulated(handleFace);

                    //Use average of edgecenters if we have more than 4 edges.
                    var temp = manifold.GetAdjacentFaceIdsAndEdgeCenters(handleFace);
                    if (temp.edgeCenter.Length > 4)
                    {
                        Vector3 cent = new Vector3();
                        foreach (Vector3 v in temp.edgeCenter)
                        {
                            cent[0] += v[0];
                            cent[1] += v[1];
                            cent[2] += v[2];
                        }
                        cent[0] /= temp.edgeCenter.Length;
                        cent[1] /= temp.edgeCenter.Length;
                        cent[2] /= temp.edgeCenter.Length;

                        center = cent;
                    }

                    // Rotate the handle to look in the opposite direction of face's normal
                    var normal = manifold.GetFaceNormal(handleFace);
                    var edgeNormal = manifold.GetFirstEdgeDirection(handleFace);
                    faceHandleController.Value.UpdatePositionAndRotation(center, normal, edgeNormal);
                    faceHandleController.Value.gameObject.SetActive(true);
                }

                var adjacentFaces = manifold.GetAdjacentFaceIdsAndEdgeCenters(handleFace);

                for (int j = 0; j < adjacentFaces.edgeId.Length; j++)
                {
                    EdgeHandleController latch;
                    if (_edgeHandles.TryGetValue(adjacentFaces.edgeId[j], out latch))
                    {
                        var otherFace = latch.GetOtherFace(handleFace);

                        var edgeCenter = adjacentFaces.edgeCenter[j];
                        var normal = manifold.GetFaceNormal(handleFace);
                        var adjacentFaceNormal = manifold.GetFaceNormal(otherFace);

                        latch.UpdatePositionAndRotation(edgeCenter, adjacentFaces.edgeNormals[j],
                            adjacentFaceNormal + normal);
                        latch.gameObject.SetActive(true);

                    }
                }
            }

            for (int i = 0; i < faceIds.Length; i++)
            {
                int id = faceIds[i];
                FaceHandleController visitedHandleController = null;
                _faceHandles.TryGetValue(id, out visitedHandleController);

                var newHandle = visitedHandleController != null
                    ? visitedHandleController
                    : InstantiateFaceHandle(manifold, id);
                InstantiateLatches(manifold, newHandle);
            }

            foreach (var vertexHandleController in _vertexHandles)
            {
                if (!vertexHandleController.Value.IsDragged)
                {
                    Vector3 vertexPosition = manifold.VertexPosition(vertexHandleController.Key);
                    vertexHandleController.Value.transform.localPosition = vertexPosition;

                    Vector3 vertexNormal = manifold.GetVertexNormal(vertexHandleController.Key);
                    //Debug.Log("VertexHandleNormal: " + vertexNormal);
                    Quaternion handleRotation = Quaternion.LookRotation(-vertexNormal);
                    vertexHandleController.Value.transform.localRotation = handleRotation;
                }
                vertexHandleController.Value.gameObject.SetActive(true);
            }

            for (int i = 0; i < vertexIds.Length; i++)
            {
                int id = vertexIds[i];
                VertexHandleController vertexHandle = null;
                _vertexHandles.TryGetValue(id, out vertexHandle);
                if (!manifold.IsVertexInUse(id) || vertexHandle != null)
                    continue;

                InstantiateVertexHandle(manifold, id);
            }

            //update sizes
            foreach(var faceHandleController in _faceHandles.Values)
            {
                faceHandleController.UpdateHandleSize();
            }
            foreach (var edgeHandleController in _edgeHandles.Values)
            {
                edgeHandleController.UpdateHandleSize();
            }
            foreach (var vertexHandleController in _vertexHandles.Values)
            {
                vertexHandleController.UpdateHandleSize();
            }
        }

        public void updateAdjacentFaceHandles(int face)
        {
            Manifold manifold = Extrudable._manifold;

            //Use average of edgecenters if we have more than 4 edges.
            var temp = manifold.GetAdjacentFaceIdsAndEdgeCenters(face);
            foreach (int faceHandleId in temp.faceId) {

                var center = manifold.GetCenterTriangulated(faceHandleId);
                FaceHandleController faceHandleController = _faceHandles[faceHandleId];

                if (temp.edgeCenter.Length > 4)
                {
                    Vector3 cent = new Vector3();
                    foreach (Vector3 v in temp.edgeCenter)
                    {
                        cent[0] += v[0];
                        cent[1] += v[1];
                        cent[2] += v[2];
                    }
                    cent[0] /= temp.edgeCenter.Length;
                    cent[1] /= temp.edgeCenter.Length;
                    cent[2] /= temp.edgeCenter.Length;

                    center = cent;
                }

                // Rotate the handle to look in the opposite direction of face's normal
                var normal = manifold.GetFaceNormal(faceHandleId);
                var edgeNormal = manifold.GetFirstEdgeDirection(faceHandleId);
                faceHandleController.UpdatePositionAndRotation(center, normal, edgeNormal);
            }
        }

        private FaceHandleController InstantiateFaceHandle(Manifold manifold, int faceId)
        {
            var newHandle = Instantiate(FaceHandlePrefab, transform, false);
            //newHandle.transform.localScale = Vector3.Scale(transform.lossyScale,newHandle.transform.localScale);
            newHandle.transform.SetParent(transform);

            // Assign handle position to the center of a face
            var center = manifold.GetCenterTriangulated(faceId);

            // Rotate the handle to look in the opposite direction of face's normal
            var normal = manifold.GetFaceNormal(faceId);
            var edgeNormal = manifold.GetFirstEdgeDirection(faceId);

            var faceHandleController = newHandle.GetComponent<FaceHandleController>();
            faceHandleController.UpdatePositionAndRotation(center, normal, edgeNormal);

            // Assign FaceID and Extrudable reference to handle
            faceHandleController.AssociatedFaceID = faceId;
            faceHandleController.Extrudable = Extrudable;

            _faceHandles.Add(faceId, faceHandleController);

            return faceHandleController;
        }

        public void InstantiateLatches(Manifold manifold, FaceHandleController handleController)
        {
            
            var neighbourFaces = manifold.GetAdjacentFaceIdsAndEdgeCenters(handleController.AssociatedFaceID);
            var edgeCenters = neighbourFaces.edgeCenter;

            for (int j = 0; j < neighbourFaces.edgeId.Length; j++)
            {
                var adjacentFace = neighbourFaces.faceId[j];

                int uniqueHalfedgeId = Mathf.Max(manifold.GetOppHalfEdge(neighbourFaces.edgeId[j]), neighbourFaces.edgeId[j]);

                EdgeHandleController existingEdgeHandle = null;
                var latchExists = _edgeHandles.TryGetValue(uniqueHalfedgeId, out existingEdgeHandle);

                if (latchExists)
                {
                    handleController.AttachLatch(existingEdgeHandle);
                }
                else
                {
                    var newLatch = Instantiate(LatchPrefab, transform, false);
                    //newLatch.transform.localScale = Vector3.Scale(transform.lossyScale, newLatch.transform.localScale);
                    newLatch.transform.SetParent(transform);

                    var edgeCenter = edgeCenters[j];

                    var edgeHandleController = newLatch.GetComponent<EdgeHandleController>();

                    var adjacentFaceNormal = manifold.GetFaceNormal(adjacentFace);
                    var normal = manifold.GetFaceNormal(handleController.AssociatedFaceID);

                    edgeHandleController.UpdatePositionAndRotation(edgeCenter, neighbourFaces.edgeNormals[j],
                        adjacentFaceNormal + normal);

                    edgeHandleController.FirstFace = handleController.AssociatedFaceID;
                    edgeHandleController.SecondFace = adjacentFace;
                    // always use max halfedge id - to uniquely identify halfedge
                    edgeHandleController.AssociatedEdgeID = uniqueHalfedgeId;

                    KeyValuePair<int, int> pairToRemove = new KeyValuePair<int, int>();
                    foreach (var pair in _closedEdgeBackup)
                    {
                        if (edgeHandleController.IsAdjacent(pair.Key, pair.Value))
                        {
                            pairToRemove = pair;
                            edgeHandleController.Locked = true;
                            break;
                        }
                    }

                    _closedEdgeBackup.Remove(pairToRemove);

                    handleController.AttachLatch(edgeHandleController);
                    _edgeHandles.Add(edgeHandleController.AssociatedEdgeID, edgeHandleController);
                }
            }
        }

        private GameObject InstantiateVertexHandle(Manifold manifold, int vertexId)
        {
            var newHandle = Instantiate(VertexHandlePrefab, transform, false);

            Vector3 vertexPosition = manifold.VertexPosition(vertexId);

            Vector3 vertexNormal = manifold.GetVertexNormal(vertexId);

            newHandle.transform.localPosition = vertexPosition;
            Quaternion handleRotation = Quaternion.LookRotation(-vertexNormal);
            newHandle.transform.localRotation = handleRotation;

            var handleController = newHandle.GetComponent<VertexHandleController>();
            handleController.AssociatedVertexID = vertexId;
            handleController.Extrudable = Extrudable;

            _vertexHandles.Add(vertexId, handleController);

            return newHandle;
        }

        public void DestroyInvalidObjects()
        {
            Manifold manifold = Extrudable._manifold;
            var toDeleteV = new List<int>(_vertexHandles.Keys).FindAll(key => !manifold.IsVertexInUse(key));
            foreach (var v in toDeleteV)
            {
                var obj = _vertexHandles[v];
                _vertexHandles.Remove(v);
                Destroy(obj.gameObject);
            }
            var toDeleteF = new List<int>(_faceHandles.Keys).FindAll(key => !manifold.IsFaceInUse(key));
            foreach (var f in toDeleteF)
            {
                var obj = _faceHandles[f];
                _faceHandles.Remove(f);
                Destroy(obj.gameObject);
            }
            var toDeleteE = new List<int>(_edgeHandles.Keys).FindAll(key => !manifold.IsHalfedgeInUse(key) || key < manifold.GetOppHalfEdge(key));
            foreach (var e in toDeleteE)
            {
                var obj = _edgeHandles[e];
                var faces = new[] { obj.FirstFace, obj.SecondFace };
                foreach (var face in faces)
                {
                    FaceHandleController fhc;
                    if (_faceHandles.TryGetValue(face, out fhc))
                    {
                        fhc.ConnectedLatches.Remove(obj);
                    }
                }
                _edgeHandles.Remove(e);
                Destroy(obj.gameObject);
            }

            // Brute force delete all edge controllers every cleanup, might not be effecient but circumvents bug that doesn't remove all invalid edgecontrollers.
            int[] ekeys = _edgeHandles.Keys.ToArray();
            foreach(int key in ekeys)
            {
                var obj = _edgeHandles[key];
                if (!obj.Locked)
                {
                    
                }
                _edgeHandles.Remove(key);
                Destroy(obj.gameObject);

            }

            int[] fkeys = _faceHandles.Keys.ToArray();
            foreach (int key in fkeys)
            {
                var obj = _faceHandles[key];
                _faceHandles.Remove(key);
                Destroy(obj.gameObject);
            }

            int[] vkeys = _vertexHandles.Keys.ToArray();
            foreach (int key in vkeys)
            {
                var obj = _vertexHandles[key];
                _vertexHandles.Remove(key);
                Destroy(obj.gameObject);
            }
        }

        public void UpdateFacesAndSelectedEdges(List<int> extrudingFaces)
        {
            Manifold manifold = Extrudable._manifold;
            foreach (var handleFace in extrudingFaces)
            {
                var center = manifold.GetCenterTriangulated(handleFace);

                // Rotate the handle to look in the opposite direction of face's normal
                var normal = manifold.GetFaceNormal(handleFace);
                var edgeNormal = manifold.GetFirstEdgeDirection(handleFace);
                _faceHandles[handleFace].UpdatePositionAndRotation(center, normal, edgeNormal);
            }
        }

        public void DeleteControlsExceptSelectedFaces(List<int> extrudingFaces)
        {
            var toDeleteV = new List<int>(_vertexHandles.Keys);
            foreach (var v in toDeleteV)
            {
                var obj = _vertexHandles[v];
                //_vertexHandles.Remove(v);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
            var toDeleteF = new List<int>(_faceHandles.Keys).FindAll(key => !extrudingFaces.Contains(key));
            foreach (var f in toDeleteF)
            {
                var obj = _faceHandles[f];
                //_faceHandles.Remove(f);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
            var toDeleteE = new List<int>(_edgeHandles.Keys);
            foreach (var e in toDeleteE)
            {
                var obj = _edgeHandles[e];
                var faces = new[] { obj.FirstFace, obj.SecondFace };
                foreach (var face in faces)
                {
                    FaceHandleController fhc;
                    if (_faceHandles.TryGetValue(face, out fhc))
                    {
                        //fhc.ConnectedLatches.Remove(obj);
                    }
                }
                //_edgeHandles.Remove(e);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
        }

        public void DeleteControlsExceptFaces(int[] extrudingFaces, int faceId)
        {
            var toDeleteV = new List<int>(_vertexHandles.Keys);
            foreach (var v in toDeleteV)
            {
                var obj = _vertexHandles[v];
                //_vertexHandles.Remove(v);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
            var toDeleteF = new List<int>(_faceHandles.Keys).FindAll(key => extrudingFaces.Contains(key));
            foreach (var f in toDeleteF)
            {
                if (f != faceId)
                {
                    var obj = _faceHandles[f];
                    //_faceHandles.Remove(f);
                    //Destroy(obj.gameObject);
                    obj.gameObject.SetActive(false);
                }
            }
            var toDeleteE = new List<int>(_edgeHandles.Keys);
            foreach (var e in toDeleteE)
            {
                var obj = _edgeHandles[e];
                var faces = new[] { obj.FirstFace, obj.SecondFace };
                foreach (var face in faces)
                {
                    FaceHandleController fhc;
                    if (_faceHandles.TryGetValue(face, out fhc))
                    {
                        //fhc.ConnectedLatches.Remove(obj);
                    }
                }
                //_edgeHandles.Remove(e);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
        }

        public void HideNonAdjacentVertices(Dictionary<int, int> adjacentVertices)
        {
            var toDeleteV = new List<int>(_vertexHandles.Keys).FindAll(key => !adjacentVertices.ContainsKey(key));
            foreach (var v in toDeleteV)
            {
                var obj = _vertexHandles[v];
                //_vertexHandles.Remove(v);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
            var toDeleteF = new List<int>(_faceHandles.Keys);
            foreach (var f in toDeleteF)
            {
                var obj = _faceHandles[f];
                //_faceHandles.Remove(f);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
            var toDeleteE = new List<int>(_edgeHandles.Keys);
            foreach (var e in toDeleteE)
            {
                var obj = _edgeHandles[e];
                var faces = new[] { obj.FirstFace, obj.SecondFace };
                foreach (var face in faces)
                {
                    FaceHandleController fhc;
                    if (_faceHandles.TryGetValue(face, out fhc))
                    {
                        //fhc.ConnectedLatches.Remove(obj);
                    }
                }
                //_edgeHandles.Remove(e);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);

            }
        }

        public void DestroyFacesAndEdgeHandles()
        {
            var toDeleteF = new List<int>(_faceHandles.Keys);
            foreach (var f in toDeleteF)
            {
                var obj = _faceHandles[f];
                //_faceHandles.Remove(f);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);
            }
            var toDeleteE = new List<int>(_edgeHandles.Keys);
            foreach (var e in toDeleteE)
            {
                var obj = _edgeHandles[e];
                var faces = new[] { obj.FirstFace, obj.SecondFace };
                foreach (var face in faces)
                {
                    FaceHandleController fhc;
                    if (_faceHandles.TryGetValue(face, out fhc))
                    {
                        //fhc.ConnectedLatches.Remove(obj);
                    }
                }
                //_edgeHandles.Remove(e);
                //Destroy(obj.gameObject);
                obj.gameObject.SetActive(false);

            }
        }

    }
}