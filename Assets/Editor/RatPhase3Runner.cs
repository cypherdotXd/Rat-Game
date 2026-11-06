using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RatPhase3Runner
{
    private const string ScenePath = "Assets/Project/Scenes/RatNpcTest.unity";

    [MenuItem("Tools/Run Phase 3 Tests")]
    public static void RunPhase3Tests()
    {
        Debug.Log("[Phase 3] Setting up test scene and executing 10 simulation runs...");

        var (brain, controller, target) = SetupTestScene();

        if (brain == null || controller == null || target == null)
        {
            Debug.LogError("[Phase 3] Setup failed: Missing Brain, Controller, or Target in test scene.");
            return;
        }

        Vector3 startPos = new Vector3(-2.8f, 0.05f, 0f);
        Vector3 goalPos = new Vector3(2.8f, 0.05f, 0f);

        int passCount = 0;
        int totalRuns = 10;
        float dt = Time.fixedDeltaTime;
        float maxAllowedStepDist = 2.0f * dt + 0.02f;

        var prevSimMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;

        try
        {
            for (int run = 1; run <= totalRuns; run++)
            {
                controller.Rb.linearVelocity = Vector3.zero;
                controller.Rb.angularVelocity = Vector3.zero;
                controller.transform.position = startPos;
                controller.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                controller.Rb.position = startPos;
                controller.Rb.rotation = Quaternion.LookRotation(Vector3.forward);
                controller.StepPhysics();
                brain.ResetPath();

                // Target moves slightly per run to test dynamic player positions
                float targetOffsetZ = (run - 5) * 0.15f;
                target.position = goalPos + new Vector3(0, 0, targetOffsetZ);

                bool reached = false;
                Vector3 lastPos = startPos;
                int maxSteps = 500; // 10 seconds of simulation

                for (int step = 0; step < maxSteps; step++)
                {
                    brain.StepBrain(dt);
                    controller.StepPhysics();
                    Physics.Simulate(dt);

                    Vector3 newPos = controller.transform.position;
                    float stepDist = Vector3.Distance(newPos, lastPos);

                    // Acceptance check: NO teleporting
                    if (stepDist > maxAllowedStepDist)
                    {
                        Debug.LogError($"[Phase 3] Run {run}: Teleport detected! Step distance {stepDist:F3}m exceeds threshold.");
                        break;
                    }
                    lastPos = newPos;

                    // Acceptance check: Reached target
                    if (Vector3.Distance(newPos, target.position) <= 0.6f)
                    {
                        reached = true;
                        break;
                    }
                }

                if (reached)
                {
                    passCount++;
                    Debug.Log($"[Phase 3] Run {run}/{totalRuns}: SUCCESS - Reached player around obstacle.");
                }
                else
                {
                    Debug.LogWarning($"[Phase 3] Run {run}/{totalRuns}: FAILED - Did not reach target in time.");
                }
            }
        }
        finally
        {
            Physics.simulationMode = prevSimMode;
        }

        if (passCount == totalRuns)
        {
            Debug.Log($"<color=green>[Phase 3] ACCEPTANCE PASSED: {passCount}/{totalRuns} runs succeeded! No teleporting, movement driven strictly via IRatInput.</color>");
        }
        else
        {
            Debug.LogError($"[Phase 3] ACCEPTANCE FAILED: Only {passCount}/{totalRuns} runs passed.");
        }
    }

    [MenuItem("Tools/Build Phase 3 Test Scene")]
    public static (RatBrain brain, MovementController controller, Transform target) SetupTestScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Light & Camera
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        var camObj = new GameObject("Main Camera");
        camObj.AddComponent<Camera>();
        camObj.transform.position = new Vector3(0, 7, -6);
        camObj.transform.rotation = Quaternion.Euler(50, 0, 0);
        camObj.tag = "MainCamera";

        // 2. Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2, 1, 2);

        // 3. Obstacle (in the middle between Start (-2.8,0,0) and Goal (2.8,0,0))
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Obstacle";
        obstacle.transform.position = new Vector3(0, 0.5f, 0);
        obstacle.transform.localScale = new Vector3(1.5f, 1f, 3f);

        // 4. Graph & Nodes
        var graphObj = new GameObject("RatGraph");
        var graph = graphObj.AddComponent<RatGraph>();

        var floorCol = floor.GetComponent<Collider>();
        var nodeStart = CreateNode(graphObj, "Node_Start", new Vector3(-2.8f, 0.05f, 0), floorCol);
        var nodeLeft = CreateNode(graphObj, "Node_Left", new Vector3(0f, 0.05f, -2.4f), floorCol);
        var nodeRight = CreateNode(graphObj, "Node_Right", new Vector3(0f, 0.05f, 2.4f), floorCol);
        var nodeGoal = CreateNode(graphObj, "Node_Goal", new Vector3(2.8f, 0.05f, 0), floorCol);

        graph.AddNode(nodeStart);
        graph.AddNode(nodeLeft);
        graph.AddNode(nodeRight);
        graph.AddNode(nodeGoal);

        ConnectTwoWay(graph, nodeStart, nodeLeft, EdgeType.WALK);
        ConnectTwoWay(graph, nodeLeft, nodeGoal, EdgeType.WALK);
        ConnectTwoWay(graph, nodeStart, nodeRight, EdgeType.WALK);
        ConnectTwoWay(graph, nodeRight, nodeGoal, EdgeType.WALK);

        // 5. Target / Player
        var playerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        playerObj.name = "Player Target";
        playerObj.transform.position = new Vector3(2.8f, 0.15f, 0);
        playerObj.transform.localScale = new Vector3(0.2f, 0.15f, 0.2f);

        // 6. NPC Rat
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Rat Player.prefab");
        GameObject npcObj;
        if (playerPrefab != null)
        {
            npcObj = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            npcObj.name = "Rat NPC";
        }
        else
        {
            npcObj = new GameObject("Rat NPC");
            npcObj.AddComponent<CapsuleCollider>();
            npcObj.AddComponent<Rigidbody>();
        }

        npcObj.transform.position = new Vector3(-2.8f, 0.05f, 0);

        var controller = npcObj.GetComponent<MovementController>();
        if (!controller) controller = npcObj.AddComponent<MovementController>();

        var playerInput = npcObj.GetComponent<PlayerInput>();
        if (playerInput) Object.DestroyImmediate(playerInput);

        var brain = npcObj.GetComponent<RatBrain>();
        if (!brain) brain = npcObj.AddComponent<RatBrain>();

        controller.Initialize();
        brain.Initialize();

        brain.SetTarget(playerObj.transform);
        brain.SetGraph(graph);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[Phase 3] Test scene saved to {ScenePath}");

        return (brain, controller, playerObj.transform);
    }

    private static RatNode CreateNode(GameObject parent, string name, Vector3 pos, Collider surface)
    {
        var obj = new GameObject(name);
        obj.transform.parent = parent.transform;
        obj.transform.position = pos;
        var node = obj.AddComponent<RatNode>();
        node.normal = Vector3.up;
        node.surfaceCollider = surface;
        return node;
    }

    private static void ConnectTwoWay(RatGraph graph, RatNode a, RatNode b, EdgeType type)
    {
        float cost = Vector3.Distance(a.Position, b.Position) / 1.4f;
        graph.AddEdge(a, b, type, cost);
        graph.AddEdge(b, a, type, cost);
    }
}
