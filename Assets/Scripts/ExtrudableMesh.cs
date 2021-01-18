using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.GEL;
using Controls;

// Responsible for updating the mesh based on a HMesh
[RequireComponent(typeof(MeshFilter))]
public class ExtrudableMesh : MonoBehaviour
{
    public Manifold _manifold;

    public Mesh mesh;

    public double initialSize = 0.1;

    public bool rebuild = false;

    private BoxCollider boxCollider;

    // Use this for initialization
    void Awake()
    {
        if (mesh == null)
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null)
            {
                mesh = GetComponent<MeshFilter>().mesh;
            }

            var newMesh = new Mesh();
            newMesh.vertices = mesh.vertices;
            newMesh.normals = mesh.normals;
            newMesh.subMeshCount = 2;
            newMesh.SetIndices(mesh.triangles, MeshTopology.Triangles, 0);
            newMesh.SetIndices(new int[0], MeshTopology.Lines, 1);
            mesh = newMesh;
            mesh.UploadMeshData(false);
        }
        Reset();
    }

    public void Reset()
    {
        boxCollider = GetComponent<BoxCollider>();

        _manifold = BuildInitialManifold();
        TriangulateAndDrawManifold();
        GetComponent<MeshFilter>().mesh = mesh;
        
        
    }

    public Mesh GetMeshClone()
    {
        return Instantiate(mesh);
    }
    private void LateUpdate()
    {
        if (rebuild)
        {
            TriangulateAndDrawManifold();
            rebuild = false;
        }
    }

    public void StartExtrusion(int[] faceIds)
    {
        _manifold.ExtrudeFaces(faceIds);
    }

    public Vector3 GetFaceNormal(int faceId)
    {
        return _manifold.GetFaceNormal(faceId);
    }

    public void MoveTo(
        int faceId,
        int[] faceIds,
        Vector3 destinationLocalSpace, bool snap, bool tick)
    {
        var translation = destinationLocalSpace - _manifold.GetCenter(faceId);
        var normal = _manifold.GetFaceNormal(faceId);
        float d = Vector3.Dot(normal, translation);

        if (snap)
        {
            if (tick)
            {
                Vector3 normalExtrusionVector = d * normal;
                float originalMagnitude = normalExtrusionVector.magnitude;
                float roundedMagnitude = Mathf.Round(originalMagnitude * 50) / 50;
                float scale = roundedMagnitude / originalMagnitude;
                Vector3 scaleVector = new Vector3(scale, scale, scale);

                _manifold.MoveFacesAlongVector(faceIds, Vector3.Scale(normalExtrusionVector, scaleVector));
            }
            else
            {
                _manifold.MoveFacesAlongVector(faceIds, d * normal);
            }
        }
        else
        {
            _manifold.MoveFacesAlongVector(faceIds, translation);
        }
    }
    
    public void RotateFaceAroundPoint(int[] faceIds, Vector3 position, Quaternion rotation)
    {
        _manifold.RotateVerticesAroundPoint(faceIds, position, rotation);
    }
    
    public int CollapseShortEdges(double threshold = 0.01)
    {
        int res = _manifold.CollapseShortEdges(threshold);
        return res;
    }

    public void MoveVertexTo(int vertexId, Vector3 localSpaceDestination)
    {
        var position = _manifold.VertexPosition(vertexId);
        _manifold.MoveVertexAlongVector(vertexId, localSpaceDestination - position);
    }

    public void MoveVertexAlongVector(int vertexId, Vector3 translation)
    {
        _manifold.MoveVertexAlongVector(vertexId, translation);
    }

    public void LoadMesh(Manifold manifold)
    {
        _manifold = manifold;
        _manifold.StitchMesh(1e-10);
        TriangulateAndDrawManifold();

        ControlsManager.Instance.Clear();
        ControlsManager.Instance.UpdateControls();
        
        UndoManager.Instance.undoActions.Clear();
        // add initial state to undo list
        UndoManager.Instance.position = 0;
        UndoManager.Instance.OnUndoStartAction(null, null, Vector3.zero, Quaternion.identity);
        UndoManager.Instance.OnUndoEndAction(null, null, Vector3.zero, Quaternion.identity);

    }

    public void UpdateMesh()
    {
        _manifold.StitchMesh(1e-10);
        TriangulateAndDrawManifold();

        
        mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null)
        {
            mesh = GetComponent<MeshFilter>().mesh;
        }

        var newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.normals = mesh.normals;
        newMesh.subMeshCount = 2;
        newMesh.SetIndices(mesh.triangles, MeshTopology.Triangles, 0);
        newMesh.SetIndices(new int[0], MeshTopology.Lines, 1);
        mesh = newMesh;
        mesh.UploadMeshData(false);

        ControlsManager.Instance.Clear();
        ControlsManager.Instance.UpdateControls();

    }

    public void ChangeManifold(Manifold newManifold)
    {

        _manifold = newManifold;
        _manifold.StitchMesh(1e-10);
        TriangulateAndDrawManifold();
        //ControlsManager.Instance.Clear();
        //ControlsManager.Instance.UpdateControls();
    }

    /* UTIL FUNCTIONS */

    private Manifold BuildInitialManifold()
    {
        var manifold = new Manifold();
        double s = initialSize;
        double[] bottom =
        {
            s, -s, -s,
            -s, -s, -s,
            -s, s, -s,
            s, s, -s
        };

        double[] front =
        {
            s, -s, -s,
            s, s, -s,
            s, s, s,
            s, -s, s
        };

        double[] left =
        {
            s, -s, -s,
            s, -s, s,
            -s, -s, s,
            -s, -s, -s
        };

        double[] right =
        {
            s, s, -s,
            -s, s, -s,
            -s, s, s,
            s, s, s,
        };

        double[] top =
        {
            s, -s, s,
            s, s, s,
            -s, s, s,
            -s, -s, s
        };

        double[] back =
        {
            -s, -s, -s,
            -s, -s, s,
            -s, s, s,
            -s, s, -s,
        };

        manifold.AddFace(4, bottom);
        manifold.AddFace(4, front);
        manifold.AddFace(4, left);
        manifold.AddFace(4, right);
        manifold.AddFace(4, top);
        manifold.AddFace(4, back);
        manifold.StitchMesh(1e-10);
        return manifold;
    }

   public void TriangulateAndDrawManifold()
    {

        var pointsAndQuads = _manifold.ToIdfs();

        var points = pointsAndQuads.Key;

        List<int> edges = new List<int>();
        var vertices = new Vector3[_manifold.NumberOfAllocatedVertices()];

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

        
        mesh.name = "extruded";
        mesh.SetIndices(new int[0], MeshTopology.Triangles, 0);
        mesh.SetIndices(new int[0], MeshTopology.Lines, 1);
        mesh.vertices = verticesFinal.ToArray();
        mesh.normals = normalsFinal.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetIndices(polygonsFinal.ToArray(), MeshTopology.Triangles, 0);
        mesh.SetIndices(edges.ToArray(), MeshTopology.Lines, 1);
        mesh.UploadMeshData(false);


        mesh.RecalculateBounds();
        UpdateCollider();
      
    }

  
    public void UpdateCollider()
    {
        
        boxCollider.center = mesh.bounds.center;
        boxCollider.size = mesh.bounds.size;
    }

    public bool isValidMesh()
    {
        // prototype 1, compare all triangles to all triangles
        var triangles = mesh.GetTriangles(0);
        var vertices = mesh.vertices; // property not field

        for (var i = 0; i < triangles.Length - 3; i = i + 3)
        {
            //Debug.Log("log 2: " + mesh.vertexCount);
            for (var j = i; j < triangles.Length; j = j + 3)
            {
                //Debug.Log("log 3: " + mesh.vertexCount);
                if (triangle_triangleIntersection(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]], vertices[triangles[j]], vertices[triangles[j + 1]], vertices[triangles[j + 2]]))
                {
                    /*
                    Debug.Log("found a triangle intersection");
                    Debug.Log("Triangle 1:");
                    Debug.Log("vertex 1: " + vertices[triangles[i]].ToString("F8"));
                    Debug.Log("vertex 2: " + vertices[triangles[i + 1]].ToString("F8"));
                    Debug.Log("vertex 3: " + vertices[triangles[i + 2]].ToString("F8"));
                    Debug.Log("Triangle 2:");
                    Debug.Log("vertex 1: " + vertices[triangles[j]].ToString("F8"));
                    Debug.Log("vertex 2: " + vertices[triangles[j + 1]].ToString("F8"));
                    Debug.Log("vertex 3: " + vertices[triangles[j + 2]].ToString("F8"));
                    */
                    return false;
                }
            }
        }
        return true;

    }

    public bool triangle_triangleIntersection(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6)
    {

        double epsilon = 0.00001;
        var N1 = Vector3.Cross((v2 - v1), (v3 - v1));
        float d1 = Vector3.Dot(-N1, v1);
        float sd1, sd2, sd3;

        sd1 = Vector3.Dot(N1, v4) + d1;
        sd2 = Vector3.Dot(N1, v5) + d1;
        sd3 = Vector3.Dot(N1, v6) + d1;

        if (Math.Abs(sd1) < epsilon)
            sd1 = 0.0f;
        if (Math.Abs(sd2) < epsilon)
            sd2 = 0.0f;
        if (Math.Abs(sd3) < epsilon)
            sd3 = 0.0f;

        if (sd1 * sd2 >= 0.0f && sd1 * sd3 >= 0.0f && sd2 * sd3 >= 0.0f)
        {
            return false;
        }

        var N2 = Vector3.Cross((v5 - v4), (v6 - v4));
        float d2 = Vector3.Dot(-N2, v4);
        float sd4, sd5, sd6;

        sd4 = Vector3.Dot(N2, v1) + d2;
        sd5 = Vector3.Dot(N2, v2) + d2;
        sd6 = Vector3.Dot(N2, v3) + d2;

        if (Math.Abs(sd4) < epsilon)
            sd4 = 0.0f;
        if (Math.Abs(sd5) < epsilon)
            sd5 = 0.0f;
        if (Math.Abs(sd6) < epsilon)
            sd6 = 0.0f;

        if (sd4 * sd5 > 0.0f && sd4 * sd6 > 0.0f && sd5 * sd6 >= 0.0f)
        {
            return false;
        }

        short index;
        float max, bb, cc;
        float vp1, vp2, vp3, up1, up2, up3;

        var D = Vector3.Cross(N1, N2);
        index = 0;
        max = Math.Abs(D.x);
        bb = Math.Abs(D.y);
        if (bb > max)
        {
            max = bb;
            index = 1;
        }
        cc = Math.Abs(D.z);
        if (cc > max)
        {
            max = cc;
            index = 2;
        }

        vp1 = v1[index];
        vp2 = v2[index];
        vp3 = v3[index];

        up1 = v4[index];
        up2 = v5[index];
        up3 = v6[index];

        float t1;
        float t2;
        float u1;
        float u2;

        // first triangle 
        if (sd4 * sd5 > 0.0f)
        {
            //Console.WriteLine("t - 1");
            t1 = vp1 + (vp3 - vp1) * (sd4 / (sd4 - sd6));
            t2 = vp2 + (vp3 - vp2) * (sd5 / (sd5 - sd6));

        }
        else if (sd4 * sd6 > 0.0f)
        {
            //Console.WriteLine("t - 2");
            t1 = vp1 + (vp2 - vp1) * (sd4 / (sd4 - sd5));
            t2 = vp3 + (vp2 - vp3) * (sd6 / (sd6 - sd5));

        }
        else if (sd5 * sd6 > 0.0f || sd4 != 0.0f)
        {
            //Console.WriteLine("t - 3");
            t1 = vp2 + (vp1 - vp2) * (sd5 / (sd5 - sd4));
            t2 = vp3 + (vp1 - vp3) * (sd6 / (sd6 - sd4));

        }
        else if (sd5 != 0.0f)
        {
            //Console.WriteLine("t - 4");
            t1 = vp1 + (vp2 - vp1) * (sd4 / (sd4 - sd5));
            t2 = vp3 + (vp2 - vp3) * (sd6 / (sd6 - sd5));

        }
        else if (sd6 != 0.0f)
        {
            //Console.WriteLine("t - 5");
            t1 = vp1 + (vp3 - vp1) * (sd4 / (sd4 - sd6));
            t2 = vp2 + (vp3 - vp2) * (sd5 / (sd5 - sd6));

        }
        else
        {
            //Console.WriteLine("t coplanar triangle");
            return true; // coplanar triangle
        }

        //second triangle
        if (sd1 * sd2 > 0.0f)
        {
            //Console.WriteLine("u - 1");
            u1 = up1 + (up3 - up1) * (sd1 / (sd1 - sd3));
            u2 = up2 + (up3 - up2) * (sd2 / (sd2 - sd3));

        }
        else if (sd1 * sd3 > 0.0f)
        {
            //Console.WriteLine("u - 2");
            u1 = up1 + (up2 - up1) * (sd1 / (sd1 - sd2));
            u2 = up3 + (up2 - up3) * (sd3 / (sd3 - sd2));

        }
        else if (sd2 * sd3 > 0.0f || sd1 != 0.0f)
        {
            //Console.WriteLine("u - 3");
            u1 = up2 + (up1 - up2) * (sd2 / (sd2 - sd1));
            u2 = up3 + (up1 - up3) * (sd3 / (sd3 - sd1));

        }
        else if (sd2 != 0.0f)
        {
            //Console.WriteLine("u - 4");
            u1 = up1 + (up2 - up1) * (sd1 / (sd1 - sd2));
            u2 = up3 + (up2 - up3) * (sd3 / (sd3 - sd2));

        }
        else if (sd3 != 0.0f)
        {
            //Console.WriteLine("u - 5");
            u1 = up1 + (up3 - up1) * (sd1 / (sd1 - sd3));
            u2 = up2 + (up3 - up2) * (sd2 / (sd2 - sd3));

        }
        else
        {
            //Console.WriteLine("u coplanar triangle");
            return true; // coplanar triangle
        }

        if (t1 > t2)
        {
            if (t1 <= u1 && t1 <= u2)
                return false;
            if (t2 >= u1 && t2 >= u2)
                return false;
            return true;
        }
        if (t2 > t1)
        {
            if (t2 <= u1 && t2 <= u2)
                return false;
            if (t1 >= u1 && t1 >= u2)
                return false;
            return true;
        }
        return false;
    }

    public void bridgeFaces(int faceId1, int faceId2, int[] face1Vertices, int[] face2Vertices, int noVertPairs)
    {
        _manifold.BridgeFaces(faceId1, faceId2, face1Vertices, face2Vertices, noVertPairs);
    }
}