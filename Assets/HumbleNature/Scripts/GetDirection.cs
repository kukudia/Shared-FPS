using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GetDirection : MonoBehaviour
{
    public Vector3 dir;

    // Update is called once per frame
    void Update()
    {
        dir = transform.forward;
    }
}
