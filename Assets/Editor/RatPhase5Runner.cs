using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RatPhase5Runner
{
    private const string ScenePath = "Assets/Project/Scenes/RatNpcBakeTest.unity";

    [MenuItem("Tools/Run Phase 5 Tests")]
    public static void RunPhase5Tests()
    {
        Debug.Log("[Phase 5] Setting up bake test scene and testing ghost simulator...");

        var (graph, nodeGround, nodeWallReachable, nodeWallTooHigh, brain, controller, target) = SetupBakeTestScene();

        // 1. Run the ghost simulator baker
        int baked = RatGraphBaker.BakeAll(graph);
        Debug.Log($"[Phase 5] Baker generated {baked} edges.");

        // Check 1: Impossible edges rejected
        bool tooHighHasEdge = false;
        RatEdge bakedEdge = null;

        foreach (var edge in graph.Edges)
        {
            if (edge.to == nodeWallTooHigh)
                tooHighHasEdge = true;
            if (edge.from == nodeGround && edge.to == nodeWallReachable)
                bakedEdge = edge;
        }

        if (tooHighHasEdge)
        {
            Debug.LogError("[Phase 5] FAILED: Impossible jump to 5m height was incorrectly created!");
            return;
        }
        else
        {
            Debug.Log("[Phase 5] PASS: Impossible edges correctly rejected (outside capability).");
        }

        if (bakedEdge == null)
        {
            Debug.LogError("[Phase 5] FAILED: Reachable wall edge was not baked.");
            return;
        }
        else
        {
            Debug.Log($"[Phase 5] PASS: Reachable edge created (type: {bakedEdge.type}, cost: {bakedEdge.cost:F2}s, speed: {bakedEdge.jumpParams.runSpeed:F1}).");
        }

        // Check 2: Baked edge succeeds at least 9/10 in real runtime playback
        int totalRuns = 10;
        int passCount = 0;
        float dt = Time.fixedDeltaTime;
        Vector3 startPos = new Vector3(0, 0.05f, -1.2f);

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

                bool attached = false;
                int maxSteps = 300;

                for (int step = 0; step < maxSteps; step++)
                {
                    brain.StepBrain(dt);
                    controller.StepPhysics();
                    Physics.Simulate(dt);

                    if (controller.IsAttached)
                    {
                        attached = true;
                        break;
                    }
                }

                if (attached)
                {
                    passCount++;
                    Debug.Log($"[Phase 5] Runtime playback run {run}/{totalRuns}: SUCCESS");
                }
                else
                {
                    Debug.LogWarning($"[Phase 5] Runtime playback run {run}/{totalRuns}: FAILED");
                }
            }
        }
        finally
        {
            Physics.simulationMode = prevSimMode;
        }

        if (passCount >= 9)
        {
            Debug.Log($"<color=green>[Phase 5] ACCEPTANCE PASSED: {passCount}/{totalRuns} runtime playback runs succeeded (requirement: >= 9/10)!</color>");
        }
        else
        {
            Debug.LogError($"[Phase 5] ACCEPTANCE FAILED: Only {passCount}/{totalRuns} runs succeeded.");
        }
    }

    [MenuItem("Tools/Build Phase 5 Bake Test Scene")]
    public static (RatGraph graph, RatNode ground, RatNode wallReachable, RatNode wallTooHigh, RatBrain brain, MovementController controller, Transform target) SetupBakeTestScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Camera
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        var camObj = new GameObject("Main Camera");
        camObj.AddComponent<Camera>();
        camObj.transform.position = new Vector3(0, 3, -4);
        camObj.transform.rotation = Quaternion.Euler(25, 0, 0);
        camObj.tag = "MainCamera";

        // 2. Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2, 1, 2);
        var floorCol = floor.GetComponent<Collider>();

        // 3. Tall Wall at z = 0 (front face at z = -0.25)
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "TallWall";
        wall.transform.position = new Vector3(0, 3f, 0);
        wall.transform.localScale = new Vector3(6f, 6f, 0.5f);
        var wallCol = wall.GetComponent<Collider>();

        // 4. Graph & Nodes
        var graphObj = new GameObject("RatGraph");
        var graph = graphObj.AddComponent<RatGraph>();

        var nodeGround = CreateNode(graphObj, "Node_Ground", new Vector3(0, 0.05f, -1.0f), Vector3.up, floorCol);
        var nodeWallReachable = CreateNode(graphObj, "Node_Wall_Reachable", new Vector3(0, 1.1f, -0.27f), -Vector3.forward, wallCol);
        var nodeWallTooHigh = CreateNode(graphObj, "Node_Wall_TooHigh", new Vector3(0, 5.0f, -0.27f), -Vector3.forward, wallCol);

        graph.AddNode(nodeGround);
        graph.AddNode(nodeWallReachable);
        graph.AddNode(nodeWallTooHigh);

        // 5. Target at reachable wall node
        var playerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        playerObj.name = "Player Target";
        playerObj.transform.position = new Vector3(0, 1.8f, -0.27f);
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

        npcObj.transform.position = new Vector3(0, 0.05f, -1.2f);

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
        Debug.Log($"[Phase 5] Bake test scene saved to {ScenePath}");

        return (graph, nodeGround, nodeWallReachable, nodeWallTooHigh, brain, controller, playerObj.transform);
    }

    private static RatNode CreateNode(GameObject parent, string name, Vector3 pos, Vector3 normal, Collider surface)
    {
        var obj = new GameObject(name);
        obj.transform.parent = parent.transform;
        obj.transform.position = pos;
        var node = obj.AddComponent<RatNode>();
        node.normal = normal;
        node.surfaceCollider = surface;
        return node;
    }
}
