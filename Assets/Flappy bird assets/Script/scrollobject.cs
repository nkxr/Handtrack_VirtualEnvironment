using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scroll_script : MonoBehaviour
{
    [SerializeField] private float scrollSpeed =  2f;
    [SerializeField] private float resetPositionX = -20f;
    [SerializeField] private float startPositionX = 20f;

    private void Update()
    {
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;
        if (transform.position.x <= resetPositionX)
        {
            transform.position = new Vector3(startPositionX, transform.position.y, transform.position.z);
        }
    }
}
