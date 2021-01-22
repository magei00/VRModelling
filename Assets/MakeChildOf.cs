using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeChildOf : MonoBehaviour
{
    // Start is called before the first frame update

    public string parentName;

    void Start()
    {
        StartCoroutine(Wait(3));
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator Wait(float time)
    {
        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.parent = GameObject.Find(parentName).transform;
        yield return null;
        
    }
}
