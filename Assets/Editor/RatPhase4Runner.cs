using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RatPhase4Runner
{
    private const string ScenePath = "Assets/Project/Scenes/RatNpcJumpTest.unity";

    [MenuItem("Tools/Run Phase 4 Tests")]
    public static void RunPhase4Tests()
    {
        Debug.Log("[Phase 4] Setting up jump test scene and executing 10 simulation runs...");

        var (brain, controller, target) = SetupJumpTestScene();

        if (brain == null || controller == null || target == null)
        {
            Debug.LogError("[Phase 4] Setup failed: Missing Brain, Controller, or Target.");
            return;
        }

        Vector3 startPos = new Vector3(0f, 0.05f, -1.2f);
        int passCount = 0;
        int totalRuns = 10;
        float dt = Time.fixedDeltaTime;

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
                int maxSteps = 300; // 6 seconds

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
                    Debug.Log($"[Phase 4] Run {run}/{totalRuns}: SUCCESS - Wall attached and climbing!");
                }
                else
                {
                    Debug.LogWarning($"[Phase 4] Run {run}/{totalRuns}: FAILED - Did not attach to wall.");
                }
            }
        }
        finally
        {
            Physics.simulationMode = prevSimMode;
        }

        if (passCount >= 8)
        {
            Debug.Log($"<color=green>[Phase 4] ACCEPTANCE PASSED: {passCount}/{totalRuns} climb runs succeeded (requirement: >= 8/10)!</color>");
        }
        else
        {
            Debug.LogError($"[Phase 4] ACCEPTANCE FAILED: Only {passCount}/{totalRuns} climb runs succeeded.");
        }
    }

    [MenuItem("Tools/Build Phase 4 Jump Test Scene")]
    public static (RatBrain brain, MovementController controller, Transform target) SetupJumpTestScene()
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

        // 3. Wall at z = 0 (front face at z = -0.25)
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "ClimbWall";
        wall.transform.position = new Vector3(0, 1.5f, 0);
        wall.transform.localScale = new Vector3(4f, 3f, 0.5f);
        var wallCol = wall.GetComponent<Collider>();

        // 4. Graph & Nodes
        var graphObj = new GameObject("RatGraph");
        var graph = graphObj.AddComponent<RatGraph>();

        var nodeGround = CreateNode(graphObj, "Node_Ground", new Vector3(0, 0.05f, -1.0f), Vector3.up, floorCol);
        var nodeWall = CreateNode(graphObj, "Node_Wall", new Vector3(0, 1.2f, -0.27f), -Vector3.forward, wallCol);

        graph.AddNode(nodeGround);
        graph.AddNode(nodeWall);

        // JUMP_ATTACH edge from ground to wall
        var jumpEdge = graph.AddEdge(nodeGround, nodeWall, EdgeType.JUMP_ATTACH, 1.0f);
        jumpEdge.jumpParams = new JumpParams(
            takeoffPos: new Vector3(0, 0.05f, -0.65f),
            heading: Vector3.forward,
            runSpeed: 1.4f
        );

        // 5. Target on the wall
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
        Debug.Log($"[Phase 4] Jump test scene saved to {ScenePath}");

        return (brain, controller, playerObj.transform);
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
