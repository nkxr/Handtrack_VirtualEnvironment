using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe_movement : MonoBehaviour
{
    private const float FallbackSpeed = 5f;

    void Update()
    {
        float speed = GameManager.instance != null ? GameManager.instance.PipeSpeed : FallbackSpeed;
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x < -10f)
        {
            Destroy(gameObject);
        }
    }
}
