using UnityEngine;

[SelectionBase]
public class RatNode : MonoBehaviour
{
    public int id;
    public Vector3 normal = Vector3.up;
    public Collider surfaceCollider;

    public Vector3 Position => transform.position;

    private void Reset()
    {
        SnapToSurface();
    }

    [ContextMenu("Snap to Surface")]
    public void SnapToSurface()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, -Vector3.up, out var hit, 2f))
        {
            transform.position = hit.point + hit.normal * 0.02f;
            normal = hit.normal;
            surfaceCollider = hit.collider;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.06f);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, normal * 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
}
