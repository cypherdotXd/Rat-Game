using UnityEngine;

[CreateAssetMenu(fileName = "RatBrainSettings", menuName = "RatNPC/Brain Settings")]
public class RatBrainSettings : ScriptableObject
{
    [Header("Path Following")]
    public float repathInterval = 0.25f;
    public float reachRadius = 0.4f;
    public float inputSmoothingK = 10f;
    public float maxTurnRate = 8f;

    [Header("Jump Execution")]
    public float takeoffTolerance = 0.2f;
    public float alignAngle = 10f;
    public float jumpTimeout = 2.5f;

    [Header("Stuck Recovery")]
    public float stuckRepathTime = 1.0f;
    public float stuckJumpTime = 2.0f;
    public float stuckBlockEdgeTime = 4.0f;

    [Header("Baker")]
    public float[] bakeRunSpeeds = new float[] { 0f, 0.5f, 1.0f };
}
