using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class SceneVisualDebugger
{
    static SceneVisualDebugger()
    {
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        string triggerPath = Path.Combine(Application.dataPath, "Editor", "capture.trigger");
        if (File.Exists(triggerPath))
        {
            try { File.Delete(triggerPath); } catch { }
            CaptureAllRooms();
        }

        string bakeTriggerPath = Path.Combine(Application.dataPath, "Editor", "bake.trigger");
        if (File.Exists(bakeTriggerPath))
        {
            try { File.Delete(bakeTriggerPath); } catch { }
            StartBake();
        }

        string buildTriggerPath = Path.Combine(Application.dataPath, "Editor", "build.trigger");
        if (File.Exists(buildTriggerPath))
        {
            try { File.Delete(buildTriggerPath); } catch { }
            BuildAndroid();
        }

        string testTriggerPath = Path.Combine(Application.dataPath, "Editor", "test.trigger");
        if (File.Exists(testTriggerPath))
        {
            try { File.Delete(testTriggerPath); } catch { }
            RatPathfinderTests.RunAllTests();
        }
    }

    [MenuItem("Tools/Build Android")]
    public static void BuildAndroid()
    {
        string markerPath = Path.Combine(Application.dataPath, "Editor", "build_done.marker");
        try { if (File.Exists(markerPath)) File.Delete(markerPath); } catch { }

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        string[] scenes = { "Assets/Project/Scenes/World Level.unity" };
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/RatGame.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log($"[BuildAndroid] Result: {report.summary.result}, Errors: {report.summary.totalErrors}");
        File.WriteAllText(markerPath, report.summary.result.ToString());
    }

    [MenuItem("Tools/Bake Lighting")]
    public static void StartBake()
    {
        if (Lightmapping.isRunning)
        {
            Debug.LogWarning("[LightmapBaker] A bake is already running.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Project/Scenes/World Level.unity");
        }

        Debug.Log("[LightmapBaker] Clearing old bake and starting new bake with Blender UV2s...");
        Lightmapping.Clear();
        Lightmapping.bakeCompleted += OnBakeCompleted;
        Lightmapping.BakeAsync();
    }

    private static void OnBakeCompleted()
    {
        Lightmapping.bakeCompleted -= OnBakeCompleted;
        Debug.Log("[LightmapBaker] Lightmap bake finished successfully!");
        CaptureAllRooms();
        string marker = Path.Combine(Application.dataPath, "Editor", "bake_done.marker");
        File.WriteAllText(marker, "done");
    }

    [MenuItem("Tools/Capture All Rooms")]
    public static void CaptureAllRooms()
    {
        if (!EditorApplication.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Project/Scenes/World Level.unity");
        }

        // Place rat in bedroom sunbeam on the desk surface next to mouse
        GameObject rat = GameObject.Find("Rat Player");
        if (rat != null)
        {
            var smr = rat.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null)
            {
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                smr.receiveShadows = true;
            }

            rat.transform.position = new Vector3(-1.45f, 1.15f, 3.1f);
            rat.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
        }

        // 1. Bedroom view
        CaptureCameraView(new Vector3(-2.65f, 1.82f, -0.75f), Quaternion.Euler(14.0f, 42.0f, 0f), 65f, "room_bedroom.png");

        // 2. Desk Close-up view
        CaptureCameraView(new Vector3(-1.95f, 1.45f, 2.75f), Quaternion.Euler(22.0f, 45.0f, 0f), 45f, "room_desk.png");

        // 3. Game View matching user screenshot
        CaptureCameraView(new Vector3(-2.25f, 1.45f, 2.20f), Quaternion.Euler(15.0f, 48.0f, 0f), 60f, "room_gameview.png");

        // 4. Hallway view
        CaptureCameraView(new Vector3(-0.5f, 1.5f, 1.5f), Quaternion.Euler(10.0f, 90.0f, 0f), 65f, "room_hallway.png");

        // 5. Living room view
        CaptureCameraView(new Vector3(3.72f, 0.40f, 2.74f), Quaternion.Euler(5.5f, 3.5f, 0f), 64f, "room_livingroom.png");

        // 6. Bathroom view
        CaptureCameraView(new Vector3(0.3f, 1.5f, 4.5f), Quaternion.Euler(10.0f, 0.0f, 0f), 65f, "room_bathroom.png");

        Debug.Log("[SceneVisualDebugger] All room views captured!");
    }

    private static void CaptureCameraView(Vector3 pos, Quaternion rot, float fov, string filename)
    {
        GameObject tempCamGo = new GameObject("TempBedroomDebugCam");
        Camera cam = tempCamGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = Color.black;
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 100f;
        tempCamGo.transform.position = pos;
        tempCamGo.transform.rotation = rot;

        var urpData = tempCamGo.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderShadows = true;
        urpData.renderPostProcessing = true;

        int width = 1280;
        int height = 720;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();

        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tempCamGo);

        byte[] bytes = screenShot.EncodeToPNG();
        Object.DestroyImmediate(screenShot);

        string outputPath = Path.Combine(Application.dataPath, "Editor", filename);
        File.WriteAllBytes(outputPath, bytes);
        Debug.Log($"[SceneVisualDebugger] Captured {filename} ({bytes.Length} bytes)");
    }
}
