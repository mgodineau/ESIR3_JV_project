using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ProjectileBounce : MonoBehaviour
{
    // Start is called before the first frame update

    private Rigidbody rb;
    Vector3 lastVelocity;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        lastVelocity = rb.velocity;
    }


    private void OnCollisionEnter(Collision collision)
    {
        var speed = lastVelocity.magnitude;
        var direction = Vector3.Reflect(lastVelocity.normalized, collision.contacts[0].normal);

        rb.velocity = direction*Mathf.Max(speed, 0f);
    }
}
