using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Windmill_Helix : MonoBehaviour
{
    public Vector2 randomSpeed = new Vector2(1, 2);

    float rotationSpeed;

    void Start()
    {
        rotationSpeed = Random.Range(randomSpeed.x, randomSpeed.y);
    }

    void Update()
    {
        transform.Rotate(Vector3.right * (rotationSpeed * Time.deltaTime));
    }
}
