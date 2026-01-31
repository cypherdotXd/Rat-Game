using UnityEngine;

public class ProceduralAnimControls : MonoBehaviour
{
    public Transform target;

    public Transform mesh;
    public float meshSmoothness;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    
    // Update is called once per frame
    public void FixedUpdate()
    {
        var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
        mesh.rotation = Quaternion.Slerp(mesh.rotation, target.rotation, meshSmoothness * Time.fixedDeltaTime);
        // headAim.rotation = Quaternion.Euler(0, angle, 0);
    }
}
