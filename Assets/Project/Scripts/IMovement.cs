using UnityEngine;


public interface IMovement
{
    public Transform Mover { get; set; } 
    public Vector3 Move(Vector3 direction, float speed);
}

public class BasicPhysicsMovement : IMovement
{
    public Transform Mover { get; set; }
    
    private Rigidbody _rb;

    public BasicPhysicsMovement(Rigidbody rb, Transform mover)
    {
        _rb = rb;
        Mover = mover;
    }
    
    public Vector3 Move(Vector3 direction, float speed)
    {
        _rb.linearVelocity = direction.normalized * speed;
        return Mover.position;
    }
}
