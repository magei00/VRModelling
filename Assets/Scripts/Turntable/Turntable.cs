using System.Collections;
using System.Collections.Generic;
using Controls;
using UnityEngine;

public class Turntable : MonoBehaviour
{
    public GameObject handlePrefab;
    public GameObject turntableRingPrefab;
    public Transform childObject;

    public int elementCount = 12;
    public float radius = .5f;
    public float height = 0.5f;
    public float rotationSensitivity = 5;

    private void Awake()
    {
        childObject = transform.GetChild(0);
    }

    public float GetMaxYWithBounds()
    {
        return transform.position.y + height + 0.02f;
    }
    public float GetMaxY()
    {
        return transform.position.y + height;
    }

    // Use this for initialization
    void Start()
    {
        //create lower handles
        bool drawTurnTable = false;

        if (drawTurnTable)
        {
        CreateTurntableHandles(0.0f);
        CreateTurntableHandles(0.8f);
        }
        
        
    }

    void CreateTurntableHandles(float spawnHeight)
    {
        for (int i = 0; i < elementCount; i++)
        {
            float delta = i * 2 * Mathf.PI / elementCount;
            var go = Instantiate(handlePrefab);
            go.transform.localPosition = new Vector3(Mathf.Sin(delta) * radius, spawnHeight, Mathf.Cos(delta) * radius);
            go.transform.localRotation = Quaternion.Euler(0, 180 + i * 360.0f / elementCount, 0);
            go.GetComponent<TurntableController>()._turntable = this;
            go.transform.parent = this.transform;

            // hacked way of setting size, should be done better. This shares the same model as vertices which creates problems.
            //go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        }
        //create ring model
        var ring = Instantiate(turntableRingPrefab);
        ring.transform.localPosition = new Vector3(0, spawnHeight, 0);
    }
}
