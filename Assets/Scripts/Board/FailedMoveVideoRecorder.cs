using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FailedMoveVideoRecorder : MonoBehaviour
{
    public const string FrameFolder = "Assets/TestEvidence/FailedMoveFrames";

    public void StartRecording()
    {
        StartCoroutine(RecordRoutine());
    }

    private IEnumerator RecordRoutine()
    {
        Debug.Log("[RECORDER] Starting Wrong-Color Hole Failed Move Recording Routine...");

        if (Directory.Exists(FrameFolder))
        {
            try { Directory.Delete(FrameFolder, true); } catch { }
        }
        Directory.CreateDirectory(FrameFolder);

        // 1. Setup clean test board and environment
        GameObject root = new GameObject("Recorder_TestRoot");
        var bmGo = new GameObject("Recorder_BoardManager");
        bmGo.transform.SetParent(root.transform);
        var board = bmGo.AddComponent<BoardManager>();
        board.SetGridDimensions(16, 16);
        BoardManager.SetTestInstance(board);

        var gmGo = new GameObject("Recorder_GameManager");
        gmGo.transform.SetParent(root.transform);
        var gm = gmGo.AddComponent<GameManager>();
        gm.SetTestingState(GameState.Playing, 3);

        var smGo = new GameObject("Recorder_SoundManager");
        smGo.transform.SetParent(root.transform);
        smGo.AddComponent<SoundManager>();

        // Ground visual floor
        var floorGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorGo.name = "Recorder_Floor";
        floorGo.transform.SetParent(root.transform);
        floorGo.transform.position = board.GridPointToWorldPosition(5.0f, 6.5f, -0.05f);
        floorGo.transform.localScale = new Vector3(1.2f, 1f, 1.5f);
        var floorRenderer = floorGo.GetComponent<MeshRenderer>();
        if (floorRenderer != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material floorMat = new Material(sh);
            floorMat.color = new Color(0.16f, 0.20f, 0.24f, 1f);
            floorRenderer.material = floorMat;
        }

        // Camera positioned for crisp close-up framing of Queue and Wrong-Color Hole
        Vector3 queuePos = board.GridToWorldPosition(new Vector2Int(5, 5));
        Vector3 holePos = board.GridToWorldPosition(new Vector2Int(5, 8));
        Vector3 focusCenter = (queuePos + holePos) * 0.5f + Vector3.forward * 0.25f;

        var camGo = new GameObject("Recorder_Camera");
        camGo.transform.SetParent(root.transform);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.15f, 0.18f, 1f);
        cam.transform.position = focusCenter + new Vector3(-1.0f, 3.6f, -2.0f);
        cam.transform.LookAt(focusCenter + Vector3.up * 0.1f);
        camGo.AddComponent<AudioListener>();

        // Directional Light
        var lightGo = new GameObject("Recorder_Light");
        lightGo.transform.SetParent(root.transform);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
        light.intensity = 1.35f;

        // 2. Setup WRONG-COLOR HOLE at (5, 8)
        // Green Queue (ColorID 3) approaches Yellow/Gold Hole (ColorID 2)
        var holeGo = new GameObject("Recorder_WrongColorHole");
        holeGo.transform.SetParent(root.transform);
        holeGo.transform.position = holePos;

        var hole = holeGo.AddComponent<HoleController>();
        hole.Initialize(
            2, // Blue
            new Vector2Int(5, 8),
            new List<Vector2Int> { new Vector2Int(5, 8) },
            3,
            true,
            1,
            1,
            1,
            true
        );
        board.RegisterActiveHole(hole);

        // 3. Create Active RED Queue at (5, 5), (5, 4) heading UP (+Z)
        var qAGo = new GameObject("Queue_Active_Red");
        qAGo.transform.SetParent(root.transform);
        var qA = qAGo.AddComponent<AntQueueController>();
        qA.Initialize(new AntQueueData("QueueA_Red", 1, 0, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) }, false, -1, false, 0, 0, false, 0));
        qA.ForwardDirection = Vector2Int.up;
        qA.SpawnAnts();
        board.RegisterActiveQueue(qA);

        int width = 720;
        int height = 1280;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        int frameIndex = 0;
        int impactFrameIndex = -1;
        float captureInterval = 0.04f; // 25 fps

        void CaptureFrame()
        {
            var prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = prevRt;

            string filePath = Path.Combine(FrameFolder, $"frame_{frameIndex:D4}.png");
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            frameIndex++;
        }

        // Capture initial idle pose (first 10 frames = 0.40s)
        for (int i = 0; i < 10; i++)
        {
            CaptureFrame();
            yield return new WaitForSeconds(captureInterval);
        }

        Debug.Log("[RECORDER] Triggering failed move toward wrong-color hole...");
        qA.TryStartMove();

        float timeout = 4.5f;
        while ((qA.MoveState != AntQueueController.QueueMoveState.Idle || frameIndex < 35) && timeout > 0f)
        {
            if (qA.MoveState == AntQueueController.QueueMoveState.Impact && impactFrameIndex == -1)
            {
                impactFrameIndex = frameIndex;
                Debug.Log($"[RECORDER] Wrong-Color Hole Rim Impact reached at frame {impactFrameIndex}!");
            }

            CaptureFrame();
            timeout -= captureInterval;
            yield return new WaitForSeconds(captureInterval);
        }

        // Capture settling frames after returning to exact initial pose (12 frames = 0.48s)
        for (int i = 0; i < 12; i++)
        {
            CaptureFrame();
            yield return new WaitForSeconds(captureInterval);
        }

        Destroy(rt);
        Destroy(tex);
        Destroy(root);

        string metaJson = $"{{\"fps\": 25, \"totalFrames\": {frameIndex}, \"impactFrame\": {impactFrameIndex}}}";
        File.WriteAllText(Path.Combine(FrameFolder, "meta.json"), metaJson);

        Debug.Log($"[RECORDER COMPLETED] Captured {frameIndex} frames for Wrong-Color Hole Failed Move. Impact frame: {impactFrameIndex}.");
        Destroy(gameObject);
    }
}
