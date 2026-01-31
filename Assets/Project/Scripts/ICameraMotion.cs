using UnityEngine;


public interface ICameraMotion
{
    public void SetFov(float fov, float time = 0);
    public void SetDistance(float distance, float time = 0);
    public void StartDirectionFollow();
    public void EndDirectionFollow();
}

