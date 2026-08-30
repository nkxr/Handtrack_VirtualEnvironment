using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float jumpForce = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    

    // Update is called once per frame
    void Update()
    {
    if (Input.GetKeyDown(KeyCode.Space))
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }
    }
}
        