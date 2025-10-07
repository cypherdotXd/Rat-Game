using System;
using UnityEngine;

public class Bouncy : MonoBehaviour
{
    [SerializeField] private float _bounceForce = 2;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        if (!other.collider.TryGetComponent(out Rigidbody rb)) return;
        var t = Vector3.Dot(rb.linearVelocity.normalized, -Vector3.up);
        Debug.DrawRay(other.contacts[0].point, -rb.linearVelocity.normalized, Color.blue, 5);
        // print($"t {t}");
        if(t < 0.6f) return;
        
        var force = Vector3.Reflect(rb.linearVelocity.normalized, Vector3.up);
        Debug.DrawRay(other.contacts[0].point, force, Color.red, 5);
        rb.AddForce(_bounceForce * force, ForceMode.Impulse);
    }
}
