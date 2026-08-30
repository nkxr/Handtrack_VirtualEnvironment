using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private Vector3 startPosition = new Vector3(-5f, 0f, 0f);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }
    

    // Update is called once per frame
    void Update()
    {
    if (Input.GetKeyDown(KeyCode.Space))
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }
    }

    public void ResetState()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.rotation = 0f;
        transform.SetPositionAndRotation(startPosition, Quaternion.identity);
        rb.WakeUp();
    }
}
        
