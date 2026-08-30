using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe_movement : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.left * 5f * Time.deltaTime;

        if (transform.position.x < -10f)
        {
            Destroy(gameObject);
        }
    }
}

