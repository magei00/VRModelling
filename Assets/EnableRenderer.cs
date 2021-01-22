using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableRenderer : MonoBehaviour
{

    //Scuffed script, because something turns off some renderers for no reason, I didn't want to figure out what caused it.
    void Awake()
    {
        StartCoroutine(Wait());
    }

    public IEnumerator Wait()
    {
        var elapsedTime = 0f;

        if (elapsedTime < 1)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        this.GetComponent<Renderer>().enabled = true;
        yield return null;
    }
}
