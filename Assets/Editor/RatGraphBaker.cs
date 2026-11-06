using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GhostInput : MonoBehaviour, IRatInput
{
    public Vector3 MoveDir { get; set; }
    public bool Jump { get; set; }
    public bool Sprint { get; set; }
    public bool IsJumpHeld { get; set; }
}

public static class RatGraphBaker
{
    public const float MaxJumpRange = 2.2f;

    public static int BakeAll(RatGraph graph)
    {
        if (graph == null) return 0;

        var nodes = graph.Nodes;
        int bakedCount = 0;

        var ghost = CreateGhost();
        if (ghost == null)
        {
            Debug.LogError("[RatGraphBaker] Failed to instantiate ghost rat.");
            return 0;
        }

        var prevSimMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;

        try
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var nodeA = nodes[i];
                if (nodeA == null) continue;

                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;
                    var nodeB = nodes[j];
                    if (nodeB == null) continue;

                    // Skip same-collider pairs (walk edges handle same-collider)
                    if (nodeA.surfaceCollider != null && nodeA.surfaceCollider == nodeB.surfaceCollider)
                        continue;

                    float dist = Vector3.Distance(nodeA.Position, nodeB.Position);
                    if (dist > MaxJumpRange) continue;

                    // Check if nodeB is above or across gap
                    if (BakeCandidate(graph, nodeA, nodeB, ghost, out var bestParams, out float bestCost))
                    {
                        var edgeType = (nodeB.surfaceCollider != null && nodeB.normal.y < 0.6f)
                            ? EdgeType.JUMP_ATTACH
                            : EdgeType.JUMP_GAP;

                        var edge = graph.AddEdge(nodeA, nodeB, edgeType, bestCost);
                        edge.jumpParams = bestParams;
                        bakedCount++;
                        Debug.Log($"[RatGraphBaker] Successfully baked {edgeType} from {nodeA.name} to {nodeB.name} (cost: {bestCost:F2}s, speed: {bestParams.runSpeed:F1})");
                    }
                }
            }
        }
        finally
        {
            Physics.simulationMode = prevSimMode;
            if (ghost != null) Object.DestroyImmediate(ghost);
            EditorUtility.SetDirty(graph);
        }

        Debug.Log($"[RatGraphBaker] Finished bake. Total edges created: {bakedCount}");
        return bakedCount;
    }

    public static bool BakeCandidate(RatGraph graph, RatNode nodeA, RatNode nodeB, GameObject ghost, out JumpParams bestParams, out float bestCost)
    {
        bestParams = default;
        bestCost = float.MaxValue;
        bool found = false;

        var controller = ghost.GetComponent<MovementController>();
        var ghostInput = ghost.GetComponent<GhostInput>();
        float dt = Time.fixedDeltaTime;
        float maxRun = controller.WalkSpeed;

        float[] runSpeeds = new float[] { 0f, 0.5f * maxRun, 1.0f * maxRun };
        float[] yawOffsets = new float[] { -10f, 0f, 10f };

        Vector3 baseHeading = nodeB.Position - nodeA.Position;
        baseHeading.y = 0f;
        if (baseHeading.sqrMagnitude < 0.001f) baseHeading = Vector3.forward;
        baseHeading.Normalize();

        foreach (float speed in runSpeeds)
        {
            foreach (float yaw in yawOffsets)
            {
                Vector3 heading = Quaternion.Euler(0, yaw, 0) * baseHeading;

                // Reset ghost at nodeA
                controller.Rb.linearVelocity = Vector3.zero;
                controller.Rb.angularVelocity = Vector3.zero;
                ghost.transform.position = nodeA.Position;
                ghost.transform.rotation = Quaternion.LookRotation(heading);
                controller.Rb.position = nodeA.Position;
                controller.Rb.rotation = Quaternion.LookRotation(heading);
                controller.StepPhysics();

                // Ramp to runSpeed
                ghostInput.MoveDir = heading * (speed > 0f ? (speed / maxRun) : 0f);
                ghostInput.Jump = false;

                // Run a few steps to build initial speed
                int rampSteps = speed > 0f ? 8 : 1;
                for (int s = 0; s < rampSteps; s++)
                {
                    controller.StepPhysics();
                    Physics.Simulate(dt);
                }

                // Record takeoff position
                Vector3 takeoffPos = controller.transform.position;

                // Trigger Jump = true for one step
                ghostInput.Jump = true;
                controller.StepPhysics();
                Physics.Simulate(dt);

                // In air: clear Jump, maintain heading
                ghostInput.Jump = false;
                ghostInput.MoveDir = heading;

                bool success = false;
                int maxSteps = 100; // 2 seconds

                for (int step = 0; step < maxSteps; step++)
                {
                    controller.StepPhysics();
                    Physics.Simulate(dt);

                    float distToB = Vector3.Distance(controller.transform.position, nodeB.Position);

                    // Check attachment to B
                    if (controller.IsAttached && distToB < 0.85f)
                    {
                        success = true;
                        float elapsed = (rampSteps + 1 + step) * dt;
                        if (elapsed < bestCost)
                        {
                            bestCost = elapsed;
                            bestParams = new JumpParams(takeoffPos, heading, speed);
                            found = true;
                        }
                        break;
                    }

                    // Reached target surface across gap
                    if (controller.IsGrounded && controller.SurfaceCollider == nodeB.surfaceCollider && distToB < 0.6f)
                    {
                        success = true;
                        float elapsed = (rampSteps + 1 + step) * dt;
                        if (elapsed < bestCost)
                        {
                            bestCost = elapsed;
                            bestParams = new JumpParams(takeoffPos, heading, speed);
                            found = true;
                        }
                        break;
                    }

                    // Landed back on ground without reaching B
                    if (controller.IsGrounded && step > 10 && controller.SurfaceCollider != nodeB.surfaceCollider)
                    {
                        break;
                    }
                }
            }
        }

        return found;
    }

    private static GameObject CreateGhost()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Rat Player.prefab");
        GameObject ghost;
        if (prefab != null)
        {
            ghost = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ghost.name = "GhostRat_Baker";
        }
        else
        {
            ghost = new GameObject("GhostRat_Baker");
            ghost.AddComponent<CapsuleCollider>();
            ghost.AddComponent<Rigidbody>();
        }

        // Strip renderers and audio for headless bake performance
        var renderers = ghost.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;

        var audioSources = ghost.GetComponentsInChildren<AudioSource>();
        for (int i = 0; i < audioSources.Length; i++)
            audioSources[i].enabled = false;

        // Remove PlayerInput/RatBrain, add GhostInput
        var playerInput = ghost.GetComponent<PlayerInput>();
        if (playerInput) Object.DestroyImmediate(playerInput);
        var brain = ghost.GetComponent<RatBrain>();
        if (brain) Object.DestroyImmediate(brain);

        var ghostInput = ghost.AddComponent<GhostInput>();
        var controller = ghost.GetComponent<MovementController>();
        if (!controller) controller = ghost.AddComponent<MovementController>();

        controller.Initialize();
        return ghost;
    }
}

[CustomEditor(typeof(RatGraph))]
public class RatGraphEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var graph = (RatGraph)target;
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Bake All Jump Edges (Ghost Sim)", GUILayout.Height(30)))
        {
            RatGraphBaker.BakeAll(graph);
        }

        if (GUILayout.Button("Rebuild Node List From Children", GUILayout.Height(24)))
        {
            graph.RebuildFromChildren();
            EditorUtility.SetDirty(graph);
        }

        if (GUILayout.Button("Clear All Edges", GUILayout.Height(24)))
        {
            var edges = new List<RatEdge>(graph.Edges);
            foreach (var e in edges)
                graph.RemoveEdge(e);
            EditorUtility.SetDirty(graph);
        }
    }
}
