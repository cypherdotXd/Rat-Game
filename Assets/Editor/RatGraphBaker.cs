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

    public static int GenerateGroundNodesFromNavMesh(RatGraph graph, float spacing = 1.0f)
    {
        var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
        if (tri.vertices == null || tri.vertices.Length == 0)
        {
            Debug.LogWarning("[RatGraphBaker] No NavMesh found in scene. Bake NavMesh or place nodes manually.");
            return 0;
        }

        var selectedPoints = new List<Vector3>();
        float sqrSpacing = spacing * spacing;

        for (int i = 0; i < tri.vertices.Length; i++)
        {
            Vector3 v = tri.vertices[i];
            bool tooClose = false;
            for (int j = 0; j < selectedPoints.Count; j++)
            {
                if ((v - selectedPoints[j]).sqrMagnitude < sqrSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
                selectedPoints.Add(v);
        }

        // Create child nodes under RatGraph
        var createdNodes = new List<RatNode>();
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            var nodeObj = new GameObject($"GroundNode_{i}");
            nodeObj.transform.parent = graph.transform;
            nodeObj.transform.position = selectedPoints[i] + Vector3.up * 0.05f;
            var node = nodeObj.AddComponent<RatNode>();
            node.id = i;
            node.SnapToSurface();
            graph.AddNode(node);
            createdNodes.Add(node);
        }

        // Auto-link adjacent walking nodes with clear line-of-sight
        float maxWalkDist = spacing * 1.7f;
        int edgesAdded = 0;
        for (int i = 0; i < createdNodes.Count; i++)
        {
            for (int j = i + 1; j < createdNodes.Count; j++)
            {
                var a = createdNodes[i];
                var b = createdNodes[j];
                float dist = Vector3.Distance(a.Position, b.Position);
                if (dist <= maxWalkDist)
                {
                    if (!Physics.Linecast(a.Position + Vector3.up * 0.08f, b.Position + Vector3.up * 0.08f, out var hit) || hit.collider == a.surfaceCollider || hit.collider == b.surfaceCollider)
                    {
                        float cost = dist / 1.4f;
                        graph.AddEdge(a, b, EdgeType.WALK, cost);
                        graph.AddEdge(b, a, EdgeType.WALK, cost);
                        edgesAdded++;
                    }
                }
            }
        }

        Debug.Log($"[RatGraphBaker] Auto-generated {createdNodes.Count} ground nodes and {edgesAdded * 2} walk edges from NavMesh!");
        EditorUtility.SetDirty(graph);
        return createdNodes.Count;
    }
}

[CustomEditor(typeof(RatGraph))]
public class RatGraphEditor : Editor
{
    private float _navMeshSpacing = 1.0f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var graph = (RatGraph)target;
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("AI Graph Generation & Baking", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _navMeshSpacing = EditorGUILayout.FloatField("Ground Node Spacing", _navMeshSpacing);
        if (GUILayout.Button("Auto-Generate from NavMesh", GUILayout.Height(24)))
        {
            RatGraphBaker.GenerateGroundNodesFromNavMesh(graph, _navMeshSpacing);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Bake All Jump Edges (Ghost Sim)", GUILayout.Height(30)))
        {
            RatGraphBaker.BakeAll(graph);
        }

        if (GUILayout.Button("Rebuild Node List From Children", GUILayout.Height(24)))
        {
            graph.RebuildFromChildren();
            EditorUtility.SetDirty(graph);
        }

        if (GUILayout.Button("Clear All Nodes & Edges", GUILayout.Height(24)))
        {
            if (EditorUtility.DisplayDialog("Clear Graph", "Are you sure you want to delete all child nodes and edges?", "Yes", "No"))
            {
                var edges = new List<RatEdge>(graph.Edges);
                foreach (var e in edges)
                    graph.RemoveEdge(e);

                for (int i = graph.transform.childCount - 1; i >= 0; i--)
                    DestroyImmediate(graph.transform.GetChild(i).gameObject);

                graph.RebuildFromChildren();
                EditorUtility.SetDirty(graph);
            }
        }
    }
}
