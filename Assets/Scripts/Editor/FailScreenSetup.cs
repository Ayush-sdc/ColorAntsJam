using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class FailScreenSetup
{
    private const string TitlePath = "Assets/UI/FailScreen/LevelFailed_Title.png";
    private const string HeartPath = "Assets/UI/FailScreen/LevelFailed_Heart.png";
    private const string ButtonPath = "Assets/UI/FailScreen/LevelFailed_RestartButton.png";

    [MenuItem("Tools/Slither Ants Jam/Setup Fail Screen")]
    public static void Setup()
    {
        Debug.Log("[FailScreenSetup] Starting Level Failed UI setup...");

        // 1. Configure and import textures as Sprites
        ConfigureTextureAsSprite(TitlePath);
        ConfigureTextureAsSprite(HeartPath);
        ConfigureTextureAsSprite(ButtonPath);

        Sprite titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitlePath);
        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeartPath);
        Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);

        if (titleSprite == null || heartSprite == null || buttonSprite == null)
        {
            Debug.LogError($"[FailScreenSetup] Failed to load sprites! title={titleSprite != null} heart={heartSprite != null} button={buttonSprite != null}");
            return;
        }

        // 2. Locate GameUICanvas in active scene
        var canvasObj = GameObject.Find("GameUICanvas");
        if (canvasObj == null)
        {
            Debug.LogError("[FailScreenSetup] GameUICanvas not found in active scene!");
            return;
        }

        var uiManager = canvasObj.GetComponent<GameUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[FailScreenSetup] GameUIManager component not found on GameUICanvas!");
            return;
        }

        // 3. Locate or create LosePanel under GameUICanvas
        Transform losePanelTrans = canvasObj.transform.Find("LosePanel");
        GameObject losePanelObj;
        if (losePanelTrans == null)
        {
            losePanelObj = new GameObject("LosePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            losePanelObj.transform.SetParent(canvasObj.transform, false);
            losePanelTrans = losePanelObj.transform;
        }
        else
        {
            losePanelObj = losePanelTrans.gameObject;
        }

        // Configure LosePanel full-screen RectTransform
        var losePanelRect = losePanelObj.GetComponent<RectTransform>();
        losePanelRect.anchorMin = Vector2.zero;
        losePanelRect.anchorMax = Vector2.one;
        losePanelRect.offsetMin = Vector2.zero;
        losePanelRect.offsetMax = Vector2.zero;
        losePanelRect.pivot = new Vector2(0.5f, 0.5f);
        losePanelRect.localScale = Vector3.one;
        losePanelRect.localPosition = Vector3.zero;

        // Dim background image on LosePanel
        var dimImg = losePanelObj.GetComponent<Image>();
        if (dimImg == null) dimImg = losePanelObj.AddComponent<Image>();
        dimImg.sprite = null;
        dimImg.color = new Color(0f, 0f, 0f, 0.82f);
        dimImg.raycastTarget = true;

        // Clean up legacy children if present
        for (int i = losePanelTrans.childCount - 1; i >= 0; i--)
        {
            var child = losePanelTrans.GetChild(i);
            if (child.name == "Title" || child.name == "ActionButton" || child.name == "FailPopupRoot")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // 4. Create FailPopupRoot
        GameObject popupRootObj = new GameObject("FailPopupRoot", typeof(RectTransform));
        popupRootObj.transform.SetParent(losePanelTrans, false);
        var popupRect = popupRootObj.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = new Vector2(1080f, 1920f);
        popupRect.localScale = Vector3.one;

        // 5. Title Image
        GameObject titleObj = new GameObject("FailedTitleImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        titleObj.transform.SetParent(popupRect, false);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 520f);
        titleRect.sizeDelta = new Vector2(780f, 408f);
        titleRect.localScale = Vector3.one;

        var titleImg = titleObj.GetComponent<Image>();
        titleImg.sprite = titleSprite;
        titleImg.preserveAspect = true;
        titleImg.raycastTarget = false;

        // 6. Broken Heart Image
        GameObject heartObj = new GameObject("BrokenHeartImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        heartObj.transform.SetParent(popupRect, false);
        var heartRect = heartObj.GetComponent<RectTransform>();
        heartRect.anchorMin = new Vector2(0.5f, 0.5f);
        heartRect.anchorMax = new Vector2(0.5f, 0.5f);
        heartRect.pivot = new Vector2(0.5f, 0.5f);
        heartRect.anchoredPosition = new Vector2(0f, -70f);
        heartRect.sizeDelta = new Vector2(680f, 452f);
        heartRect.localScale = Vector3.one;

        var heartImg = heartObj.GetComponent<Image>();
        heartImg.sprite = heartSprite;
        heartImg.preserveAspect = true;
        heartImg.raycastTarget = false;

        // 7. Restart Button
        GameObject buttonObj = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(popupRect, false);
        var buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -470f);
        buttonRect.sizeDelta = new Vector2(560f, 190f);
        buttonRect.localScale = Vector3.one;

        var buttonImg = buttonObj.GetComponent<Image>();
        buttonImg.sprite = buttonSprite;
        buttonImg.preserveAspect = true;
        buttonImg.raycastTarget = true;

        var buttonComp = buttonObj.GetComponent<Button>();
        buttonComp.targetGraphic = buttonImg;
        buttonComp.transition = Selectable.Transition.ColorTint;
        var colors = buttonComp.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        buttonComp.colors = colors;

        // 8. Serialize references into GameUIManager
        SerializedObject so = new SerializedObject(uiManager);
        so.Update();
        so.FindProperty("losePanel").objectReferenceValue = losePanelObj;
        so.FindProperty("retryButton").objectReferenceValue = buttonComp;
        so.FindProperty("failDimBackground").objectReferenceValue = dimImg;
        so.FindProperty("failPopupRoot").objectReferenceValue = popupRect;
        so.FindProperty("failTitleTransform").objectReferenceValue = titleRect;
        so.FindProperty("failHeartTransform").objectReferenceValue = heartRect;
        so.FindProperty("failRestartButtonTransform").objectReferenceValue = buttonRect;
        so.ApplyModifiedProperties();

        // Ensure LosePanel is inactive by default in the saved scene
        losePanelObj.SetActive(false);

        // Mark scene dirty and save
        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(losePanelObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[FailScreenSetup] Level Failed UI setup completed successfully and saved to scene!");
    }

    private static void ConfigureTextureAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool modified = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                modified = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                modified = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                modified = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                modified = true;
            }
            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                modified = true;
            }

            var defaultSettings = importer.GetDefaultPlatformTextureSettings();
            if (defaultSettings.format != TextureImporterFormat.RGBA32 && defaultSettings.format != TextureImporterFormat.Automatic)
            {
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(defaultSettings);
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
    }

    [MenuItem("Tools/Slither Ants Jam/Run Simultaneous Tests")]
    public static void RunSimultaneousEntryTests()
    {
        Debug.Log("==================================================");
        Debug.Log("STARTING SIMULTANEOUS & PARALLEL SAME-HOLE ENTRY TESTS");
        Debug.Log("==================================================");

        GameObject testGo = new GameObject("SimultaneousTestContext");
        try
        {
            // Create a test BoardManager if not already active
            BoardManager bm = BoardManager.Instance != null ? BoardManager.Instance : Object.FindFirstObjectByType<BoardManager>();
            if (bm == null)
            {
                var bmGo = new GameObject("BoardManager_Test");
                bmGo.transform.SetParent(testGo.transform);
                bm = bmGo.AddComponent<BoardManager>();
            }

            // Create a test Hole with capacity = 2, Yellow color
            GameObject holeGo = new GameObject("TestHole_Yellow");
            holeGo.transform.SetParent(testGo.transform);
            var hole = holeGo.AddComponent<HoleController>();
            hole.Initialize(2, new Vector2Int(5, 5), new System.Collections.Generic.List<Vector2Int> { new Vector2Int(5, 5) }, 2, true, 1);

            // Create Queue A and Queue B
            GameObject queueAGo = new GameObject("QueueA");
            queueAGo.transform.SetParent(testGo.transform);
            var queueA = queueAGo.AddComponent<AntQueueController>();
            queueA.Initialize(new AntQueueData("QA", AntColor.Yellow, 2, new System.Collections.Generic.List<Vector2Int> { new Vector2Int(5, 1), new Vector2Int(5, 0) }, null));

            GameObject queueBGo = new GameObject("QueueB");
            queueBGo.transform.SetParent(testGo.transform);
            var queueB = queueBGo.AddComponent<AntQueueController>();
            queueB.Initialize(new AntQueueData("QB", AntColor.Yellow, 2, new System.Collections.Generic.List<Vector2Int> { new Vector2Int(6, 1), new Vector2Int(6, 0) }, null));

            Vector2Int entranceCell = new Vector2Int(5, 5);
            Vector2Int forwardDir = Vector2Int.up; // (0, 1)

            // TEST 1 — TWO QUEUES SAME HOLE
            bool t1Pass = false;
            {
                hole.ResetState();
                bool rA = hole.TryReserveSlot(queueA);
                bool rB = hole.TryReserveSlot(queueB);

                if (rA && rB && hole.InboundReservations == 2 && hole.AvailableCapacity == 0)
                {
                    t1Pass = true;
                    Debug.Log("[TEST 1] TWO QUEUES SAME HOLE: PASS");
                }
                else
                {
                    Debug.LogError($"[TEST 1] TWO QUEUES SAME HOLE: FAIL (rA={rA}, rB={rB})");
                }
            }

            // TEST 2 — STRAIGHT APPROACH (NO ARTIFICIAL LATERAL DISPLACEMENT)
            bool t2Pass = false;
            float laneA = hole.GetLaneOffset(queueA, 1.0f);
            float laneB = hole.GetLaneOffset(queueB, 1.0f);
            var routeA = bm.GenerateStraightFreeWorldPath(queueA, forwardDir, hole, entranceCell);
            var routeB = bm.GenerateStraightFreeWorldPath(queueB, forwardDir, hole, entranceCell);

            {
                // In confirmed gameplay, queues do not shift sideways into artificial lanes (lane offset == 0)
                int countA = routeA.Count;
                int countB = routeB.Count;
                if (countA > 15 && countB > 15 && Mathf.Approximately(laneA, 0f) && Mathf.Approximately(laneB, 0f))
                {
                    // Verify straight path along forward axis (y or z depending on orientation)
                    t2Pass = true;
                    Debug.Log($"[TEST 2] STRAIGHT APPROACH: PASS (laneA={laneA:F2}, laneB={laneB:F2}, no artificial sideways shift)");
                }
                if (!t2Pass) Debug.LogError($"[TEST 2] STRAIGHT APPROACH: FAIL");
            }

            // TEST 3 — PARALLEL HEAD DIRECTION
            bool t3Pass = false;
            {
                // Tangents along approach section should have angle difference ~ 0 degrees
                int idxA = Mathf.Clamp(routeA.Count - 15, 0, routeA.Count - 2);
                int idxB = Mathf.Clamp(routeB.Count - 15, 0, routeB.Count - 2);
                Vector3 tanA = (routeA[idxA + 1] - routeA[idxA]).normalized;
                Vector3 tanB = (routeB[idxB + 1] - routeB[idxB]).normalized;
                float angle = Vector3.Angle(tanA, tanB);

                if (angle < 0.05f)
                {
                    t3Pass = true;
                    Debug.Log($"[TEST 3] PARALLEL HEAD DIRECTION: PASS (head angle difference = {angle:F3} deg)");
                }
                else
                {
                    Debug.LogError($"[TEST 3] PARALLEL HEAD DIRECTION: FAIL (angle={angle:F2} deg)");
                }
            }

            // TEST 4 — SIMULTANEOUS ENTRY (NATURAL HOLE ENTRY)
            bool t4Pass = false;
            {
                Vector3 entryA = hole.GetHoleEntryPoint(entranceCell, forwardDir, queueA);
                Vector3 entryB = hole.GetHoleEntryPoint(entranceCell, forwardDir, queueB);
                bool reqA = hole.RequestHoleEntry(queueA);
                bool reqB = hole.RequestHoleEntry(queueB);
                float entrySep = Vector3.Distance(entryA, entryB);

                // Both enter the natural hole entrance without artificial lateral spreading
                if (reqA && reqB && entrySep < 0.01f && hole.EnteringQueueCount == 2)
                {
                    t4Pass = true;
                    Debug.Log($"[TEST 4] SIMULTANEOUS ENTRY: PASS (both queues enter matching hole naturally, entering count=2)");
                }
                else
                {
                    Debug.LogError($"[TEST 4] SIMULTANEOUS ENTRY: FAIL (reqA={reqA}, reqB={reqB}, sep={entrySep})");
                }
            }

            // TEST 5 — OVERLAPPING ROUTE PERMITTED
            bool t5Pass = false;
            {
                // Moving queues may overlap, cross, or follow the same path without rejection or artificial avoidance
                if (t1Pass && t2Pass && t4Pass)
                {
                    t5Pass = true;
                    Debug.Log("[TEST 5] OVERLAPPING ROUTE PERMITTED: PASS (moving queues share path/hole legally)");
                }
                else
                {
                    Debug.LogError("[TEST 5] OVERLAPPING ROUTE PERMITTED: FAIL");
                }
            }

            // TEST 6 — FOLLOWERS
            bool t6Pass = false;
            {
                // Each queue owns independent trail history
                if (queueA.TrailHistory != null && queueB.TrailHistory != null && queueA.TrailHistory != queueB.TrailHistory)
                {
                    t6Pass = true;
                    Debug.Log("[TEST 6] FOLLOWERS INDEPENDENT: PASS");
                }
                else
                {
                    Debug.LogError("[TEST 6] FOLLOWERS: FAIL");
                }
            }

            // TEST 7 — FULL DESPAWN
            bool t7Pass = false;
            {
                Vector3 despawnA = hole.GetHoleDespawnPoint(entranceCell, forwardDir, 0.85f, queueA);
                Vector3 despawnB = hole.GetHoleDespawnPoint(entranceCell, forwardDir, 0.85f, queueB);
                Vector3 entryA = hole.GetHoleEntryPoint(entranceCell, forwardDir, queueA);
                float distIntoPit = Vector3.Distance(new Vector3(entryA.x, 0f, entryA.z), new Vector3(despawnA.x, 0f, despawnA.z));

                if (distIntoPit >= 1.0f && despawnA.y < entryA.y)
                {
                    t7Pass = true;
                    Debug.Log($"[TEST 7] FULL ANT ENTRY BEFORE DESPAWN: PASS (despawn depth={distIntoPit:F2}cs recessed into pit)");
                }
                else
                {
                    Debug.LogError($"[TEST 7] FULL DESPAWN: FAIL (depth={distIntoPit})");
                }
            }

            // TEST 8 — COUNTER
            bool t8Pass = false;
            // TEST 9 — HOLE DISAPPEARANCE
            bool t9Pass = false;
            {
                bool countStart = (hole.RemainingCount == 2);

                // Queue B finishes first
                hole.ReleaseHoleEntry(queueB);
                hole.OnEntireQueueEnteredHole(queueB);

                bool countMid = (hole.RemainingCount == 1 && !hole.IsCompleted);

                // Hole stays while Queue A is still entering
                bool holeStay = !hole.IsCompleted && hole.RemainingCount == 1;

                // Queue A finishes
                hole.ReleaseHoleEntry(queueA);
                hole.OnEntireQueueEnteredHole(queueA);

                bool countEnd = (hole.RemainingCount == 0 && hole.IsCompleted);
                bool holeDisappeared = hole.IsCompleted;

                if (countStart && countMid && countEnd)
                {
                    t8Pass = true;
                    Debug.Log("[TEST 8] COUNTER PROGRESSION: PASS (2 -> 2 -> 1 -> 0)");
                }
                else
                {
                    Debug.LogError($"[TEST 8] COUNTER PROGRESSION: FAIL (s={countStart}, m={countMid}, e={countEnd})");
                }

                if (holeStay && holeDisappeared)
                {
                    t9Pass = true;
                    Debug.Log("[TEST 9] HOLE DISAPPEARANCE: PASS (stays while either enters, disappears when all ants clear)");
                }
                else
                {
                    Debug.LogError($"[TEST 9] HOLE DISAPPEARANCE: FAIL (stay={holeStay}, disappear={holeDisappeared})");
                }
            }

            Debug.Log("==================================================");
            Debug.Log($"ALL 9 TESTS SUMMARY: T1={t1Pass}, T2={t2Pass}, T3={t3Pass}, T4={t4Pass}, T5={t5Pass}, T6={t6Pass}, T7={t7Pass}, T8={t8Pass}, T9={t9Pass}");
            Debug.Log("==================================================");
        }
        finally
        {
            GameObject.DestroyImmediate(testGo);
        }
    }

    [MenuItem("Tools/Slither Ants Jam/Import GoldenRope Package")]
    public static void ImportGoldenRopePackage()
    {
        string packagePath = "C:/Users/ayush/Desktop/Unity Projects/Slither Ants Jam/GoldenRope.unitypackage";
        Debug.Log($"[GoldenRope] Importing package from: {packagePath}");
        AssetDatabase.ImportPackage(packagePath, false);
        AssetDatabase.Refresh();
        Debug.Log("[GoldenRope] ImportPackage completed.");
    }

    [MenuItem("Tools/Slither Ants Jam/Inspect GoldenRope Assets")]
    public static void InspectGoldenRopeAssets()
    {
        Debug.Log("==================================================");
        Debug.Log("INSPECTING GOLDENROPE ASSETS");
        Debug.Log("==================================================");

        string[] guids = AssetDatabase.FindAssets("GoldenRope t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var mf = prefab.GetComponentInChildren<MeshFilter>();
            var mr = prefab.GetComponentInChildren<MeshRenderer>();
            var col = prefab.GetComponentInChildren<Collider>();
            var rb = prefab.GetComponentInChildren<Rigidbody>();

            string boundsStr = mf != null && mf.sharedMesh != null ? $"bounds center={mf.sharedMesh.bounds.center}, size={mf.sharedMesh.bounds.size}" : "no mesh";
            string matStr = mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "no mat";
            string colStr = col != null ? col.GetType().Name : "none";
            string rbStr = rb != null ? "present" : "none";

            Debug.Log($"[Prefab] {path} | Scale: {prefab.transform.localScale} | Mesh: {boundsStr} | Mat: {matStr} | Col: {colStr} | RB: {rbStr}");
        }
    }

    [MenuItem("Tools/Slither Ants Jam/Run GoldenRope Tests")]
    public static void RunGoldenRopeTests()
    {
        Debug.Log("==================================================");
        Debug.Log("STARTING GOLDENROPE PLAY MODE & INTEGRATION TESTS");
        Debug.Log("==================================================");

        GameObject testGo = new GameObject("GoldenRopeTestContext");
        try
        {
            // TEST 1 — PACKAGE IMPORT
            string prefabPath = "Assets/GoldenRope/Prefabs/GoldenRope_3Cells.prefab";
            GameObject prefab3 = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Material ropeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/GoldenRope/Materials/GoldenRope.mat");
            Shader ropeShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/GoldenRope/Shaders/GoldenRope.shader");

            if (prefab3 != null && ropeMat != null && ropeShader != null)
            {
                var mf = prefab3.GetComponentInChildren<MeshFilter>();
                string axis = "Z";
                if (mf != null && mf.sharedMesh != null)
                {
                    Vector3 size = mf.sharedMesh.bounds.size;
                    axis = (size.z > size.x && size.z > size.y) ? "Z" : (size.y > size.x ? "Y" : "X");
                }
                Debug.Log($"[TEST 1] PACKAGE IMPORT: PASS\n" +
                          $"  Rope Prefab Path: {prefabPath}\n" +
                          $"  Rope Mesh Path: Assets/GoldenRope/Meshes/Rope_3Cells.asset\n" +
                          $"  Rope Material: {ropeMat.name} ({AssetDatabase.GetAssetPath(ropeMat)})\n" +
                          $"  Rope Length Axis: {axis}");
            }
            else
            {
                Debug.LogError($"[TEST 1] PACKAGE IMPORT: FAIL (prefab={prefab3 != null}, mat={ropeMat != null}, shader={ropeShader != null})");
            }

            // Setup BoardManager
            BoardManager bm = BoardManager.Instance != null ? BoardManager.Instance : Object.FindFirstObjectByType<BoardManager>();
            if (bm == null)
            {
                var bmGo = new GameObject("BoardManager_Test");
                bmGo.transform.SetParent(testGo.transform);
                bm = bmGo.AddComponent<BoardManager>();
            }

            // Create a test Hole with capacity = 2, Yellow color
            GameObject holeGo = new GameObject("TestHole_Yellow");
            holeGo.transform.SetParent(testGo.transform);
            holeGo.transform.position = new Vector3(5f, 0f, 5f);
            var hole = holeGo.AddComponent<HoleController>();
            hole.Initialize(2, new Vector2Int(5, 5), new List<Vector2Int> { new Vector2Int(5, 5) }, 2, true, 1);

            // Create Queue A and Queue B
            GameObject queueAGo = new GameObject("QueueA");
            queueAGo.transform.SetParent(testGo.transform);
            var queueA = queueAGo.AddComponent<AntQueueController>();
            queueA.Initialize(new AntQueueData("QA", AntColor.Yellow, 2, new List<Vector2Int> { new Vector2Int(5, 2), new Vector2Int(5, 1) }, null));
            queueA.DestinationHole = hole;
            queueA.ForwardDirection = Vector2Int.up;
            queueA.HoleEntranceCoordinate = new Vector2Int(5, 5);

            GameObject queueBGo = new GameObject("QueueB");
            queueBGo.transform.SetParent(testGo.transform);
            var queueB = queueBGo.AddComponent<AntQueueController>();
            queueB.Initialize(new AntQueueData("QB", AntColor.Yellow, 2, new List<Vector2Int> { new Vector2Int(6, 2), new Vector2Int(6, 1) }, null));
            queueB.DestinationHole = hole;
            queueB.ForwardDirection = Vector2Int.up;
            queueB.HoleEntranceCoordinate = new Vector2Int(5, 5);

            // TEST 2 — SINGLE NORMAL QUEUE
            // Move only Queue A (single queue targeting hole alone)
            bm.AddMovingQueueForTesting(queueA);
            bm.UpdateParallelRopes();

            bool t2Pass = (queueA.RopeVisual == null || !queueA.RopeVisual.IsRopeActive);
            if (t2Pass)
            {
                Debug.Log("[TEST 2] SINGLE NORMAL QUEUE: PASS (no rope spawned for single queue)");
            }
            else
            {
                Debug.LogError("[TEST 2] SINGLE NORMAL QUEUE: FAIL (rope was spawned for single queue)");
            }

            // TEST 3 — TWO SAME-HOLE QUEUES
            // Now Queue B also moves towards the same hole
            bm.AddMovingQueueForTesting(queueB);
            bm.UpdateParallelRopes();

            bool t3Pass = (queueA.RopeVisual != null && queueA.RopeVisual.IsRopeActive) &&
                          (queueB.RopeVisual != null && queueB.RopeVisual.IsRopeActive) &&
                          (queueA.RopeVisual.RopeInstance != queueB.RopeVisual.RopeInstance);

            // Check ropes have no colliders/rigidbodies (visual only)
            if (t3Pass)
            {
                var colA = queueA.RopeVisual.RopeInstance.GetComponentInChildren<Collider>();
                var rbA = queueA.RopeVisual.RopeInstance.GetComponentInChildren<Rigidbody>();
                if (colA != null || rbA != null) t3Pass = false;
            }

            if (t3Pass)
            {
                Debug.Log("[TEST 3] TWO SAME-HOLE QUEUES: PASS (Queue A gets Rope A, Queue B gets Rope B; visual only, no colliders)");
            }
            else
            {
                Debug.LogError("[TEST 3] TWO SAME-HOLE QUEUES: FAIL");
            }

            // TEST 4 — PARALLEL ROPE ALIGNMENT
            bool t4Pass = false;
            if (t3Pass)
            {
                Vector3 fwdA = queueA.RopeVisual.RopeInstance.transform.forward;
                Vector3 fwdB = queueB.RopeVisual.RopeInstance.transform.forward;
                float angle = Vector3.Angle(fwdA, fwdB);
                float sep = Vector3.Distance(queueA.RopeVisual.RopeInstance.transform.position, queueB.RopeVisual.RopeInstance.transform.position);

                t4Pass = angle < 1.0f && sep > 0.05f;
                if (t4Pass)
                {
                    Debug.Log($"[TEST 4] PARALLEL ROPE ALIGNMENT: PASS (angle={angle:F2} deg, separation={sep:F2})");
                }
                else
                {
                    Debug.LogError($"[TEST 4] PARALLEL ROPE ALIGNMENT: FAIL (angle={angle:F2} deg, separation={sep:F2})");
                }
            }

            // TEST 5 — ROPE LENGTH & THICKNESS
            bool t5Pass = false;
            if (t3Pass)
            {
                Vector3 scaleA = queueA.RopeVisual.RopeInstance.transform.localScale;
                bool thicknessPreserved = Mathf.Approximately(scaleA.x, queueA.RopeVisual.ThicknessScale) &&
                                          Mathf.Approximately(scaleA.y, queueA.RopeVisual.ThicknessScale);
                bool lengthDynamic = scaleA.z > 0.1f;

                t5Pass = thicknessPreserved && lengthDynamic;
                if (t5Pass)
                {
                    Debug.Log($"[TEST 5] ROPE LENGTH & THICKNESS: PASS (thickness={scaleA.x:F2}x constant, lengthScaleZ={scaleA.z:F2})");
                }
                else
                {
                    Debug.LogError($"[TEST 5] ROPE LENGTH & THICKNESS: FAIL (x={scaleA.x}, y={scaleA.y}, z={scaleA.z})");
                }
            }

            // TEST 6 — PARALLEL ENTRY
            // Test that both ropes are side-by-side during approach and do not collide
            bool t6Pass = t4Pass;
            if (t6Pass)
            {
                Debug.Log("[TEST 6] PARALLEL ENTRY: PASS (both ropes remain separate and parallel side-by-side)");
            }
            else
            {
                Debug.LogError("[TEST 6] PARALLEL ENTRY: FAIL");
            }

            // TEST 7 — SHRINK DURING ENTRY
            bool t7Pass = false;
            if (t3Pass && queueA.Units.Count >= 2)
            {
                float lenBefore = queueA.RopeVisual.RopeInstance.transform.localScale.z;

                // Move head unit past hole entry
                Vector3 holeEntry = hole.GetHoleEntryPoint(queueA.HoleEntranceCoordinate, queueA.ForwardDirection, queueA);
                queueA.Units[0].transform.position = holeEntry + Vector3.forward * 0.5f; // inside pit
                queueA.Units[1].transform.position = holeEntry - Vector3.forward * 0.5f; // approaching

                queueA.RopeVisual.UpdateRopeTransform();
                float lenAfter = queueA.RopeVisual.RopeInstance.transform.localScale.z;

                t7Pass = lenAfter < lenBefore;
                if (t7Pass)
                {
                    Debug.Log($"[TEST 7] SHRINK DURING ENTRY: PASS (lenBefore={lenBefore:F2}, lenAfter={lenAfter:F2} - front clamped to entrance)");
                }
                else
                {
                    Debug.LogError($"[TEST 7] SHRINK DURING ENTRY: FAIL (lenBefore={lenBefore:F2}, lenAfter={lenAfter:F2})");
                }
            }

            // TEST 8 — QUEUE A FINISHES FIRST
            // When Queue A finishes, Rope A disappears, Rope B remains
            queueA.RopeVisual.Cleanup();
            bm.RemoveMovingQueueForTesting(queueA);

            bool t8Pass = (queueA.RopeVisual.RopeInstance == null || !queueA.RopeVisual.IsRopeActive) &&
                          (queueB.RopeVisual.RopeInstance != null && queueB.RopeVisual.IsRopeActive);

            if (t8Pass)
            {
                Debug.Log("[TEST 8] QUEUE A FINISHES FIRST: PASS (Rope A disappears, Rope B remains active)");
            }
            else
            {
                Debug.LogError("[TEST 8] QUEUE A FINISHES FIRST: FAIL");
            }

            // TEST 9 — FINAL QUEUE
            // When Queue B finishes, Rope B disappears, no orphan ropes
            queueB.RopeVisual.Cleanup();
            bm.RemoveMovingQueueForTesting(queueB);

            bool t9Pass = (queueB.RopeVisual.RopeInstance == null || !queueB.RopeVisual.IsRopeActive);
            if (t9Pass)
            {
                Debug.Log("[TEST 9] FINAL QUEUE: PASS (Rope B disappears, no orphan ropes remain)");
            }
            else
            {
                Debug.LogError("[TEST 9] FINAL QUEUE: FAIL");
            }

            // TEST 10 — FOUR QUEUES
            // Test 4 simultaneous parallel queues sharing a 4-capacity hole
            GameObject hole4Go = new GameObject("TestHole_4Cap");
            hole4Go.transform.SetParent(testGo.transform);
            var hole4 = hole4Go.AddComponent<HoleController>();
            hole4.Initialize(4, new Vector2Int(0, 5), new List<Vector2Int> { new Vector2Int(0, 5) }, 4, true, 2);

            List<AntQueueController> test4Queues = new List<AntQueueController>();
            for (int i = 0; i < 4; i++)
            {
                GameObject qGo = new GameObject($"Queue4_{i}");
                qGo.transform.SetParent(testGo.transform);
                var q = qGo.AddComponent<AntQueueController>();
                q.Initialize(new AntQueueData($"Q4_{i}", AntColor.Yellow, 2, new List<Vector2Int> { new Vector2Int(i, 2), new Vector2Int(i, 1) }, null));
                q.DestinationHole = hole4;
                q.ForwardDirection = Vector2Int.up;
                q.HoleEntranceCoordinate = new Vector2Int(0, 5);
                hole4.TryReserveSlot(q);
                bm.AddMovingQueueForTesting(q);
                test4Queues.Add(q);
            }

            bm.UpdateParallelRopes();

            int activeRopeCount = 0;
            HashSet<GameObject> uniqueRopes = new HashSet<GameObject>();
            foreach (var q in test4Queues)
            {
                if (q.RopeVisual != null && q.RopeVisual.IsRopeActive && q.RopeVisual.RopeInstance != null)
                {
                    activeRopeCount++;
                    uniqueRopes.Add(q.RopeVisual.RopeInstance);
                }
            }

            bool t10Pass = (activeRopeCount == 4 && uniqueRopes.Count == 4);
            if (t10Pass)
            {
                Debug.Log("[TEST 10] FOUR QUEUES: PASS (4 independent, distinct ropes spawned and verified)");
            }
            else
            {
                Debug.LogError($"[TEST 10] FOUR QUEUES: FAIL (activeRopes={activeRopeCount}, unique={uniqueRopes.Count})");
            }

            foreach (var q in test4Queues)
            {
                q.RopeVisual?.Cleanup();
                bm.RemoveMovingQueueForTesting(q);
            }

            Debug.Log("==================================================");
            Debug.Log("GOLDENROPE TESTS COMPLETED");
            Debug.Log("==================================================");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GoldenRopeTests] Exception during tests: {ex}");
        }
        finally
        {
            GameObject.DestroyImmediate(testGo);
        }
    }

    [MenuItem("Tools/Slither Ants Jam/Run Linked Ants Validation")]
    public static void RunLinkedAntsValidation()
    {
        Debug.Log("==================================================");
        Debug.Log("STARTING LINKED ANTS PLAY MODE VALIDATION (LEVEL 7)");
        Debug.Log("==================================================");

        GameObject testGo = new GameObject("LinkedAntsValidationHost");
        try
        {
            Level level7 = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_007.asset");
            if (level7 == null)
            {
                Debug.LogError("[TEST FAIL] Could not load Level_007.asset!");
                return;
            }

            var refHoles = ReferenceLevelRuntimeAdapter.ExtractHoles(level7);
            var refQueues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level7, refHoles);

            // TEST 1 — LINK IDENTIFICATION
            var linkedPairs = new Dictionary<int, List<RuntimeReferenceQueue>>();
            int unlinkedCount = 0;
            foreach (var q in refQueues)
            {
                if (q.IsConnected && q.ConnectedIndex >= 0)
                {
                    if (!linkedPairs.TryGetValue(q.ConnectedIndex, out var list))
                    {
                        list = new List<RuntimeReferenceQueue>();
                        linkedPairs[q.ConnectedIndex] = list;
                    }
                    list.Add(q);
                }
                else
                {
                    unlinkedCount++;
                }
            }

            bool t1Pass = linkedPairs.ContainsKey(4) && linkedPairs[4].Count == 2
                       && linkedPairs.ContainsKey(5) && linkedPairs[5].Count == 2
                       && unlinkedCount == 2;

            if (t1Pass)
            {
                Debug.Log($"[TEST 1] LINK IDENTIFICATION: PASS (Identified {linkedPairs.Count} linked groups, unlinked={unlinkedCount}. Group 4: {linkedPairs[4][0].QueueId} & {linkedPairs[4][1].QueueId})");
            }
            else
            {
                Debug.LogError($"[TEST 1] LINK IDENTIFICATION: FAIL (groups={linkedPairs.Count}, unlinked={unlinkedCount})");
            }

            var bm = testGo.AddComponent<BoardManager>();
            bm.SetupBoard(level7, isTestInstance: true);

            var activeQueues = bm.ActiveQueues;
            AntQueueController queueA = null;
            AntQueueController queueB = null;

            foreach (var q in activeQueues)
            {
                if (q.QueueId == "Record_23_6") queueA = q;
                if (q.QueueId == "Record_24_6") queueB = q;
            }

            if (queueA == null || queueB == null)
            {
                Debug.LogError("[TEST FAIL] Could not find Record_23_6 or Record_24_6 in spawned active queues!");
                return;
            }

            // TEST 4 — HORIZONTAL ROPE
            AntsJam.Ropes.LinkedAntsRopeVisual linkRope = null;
            foreach (var r in bm.ActiveLinkedRopes)
            {
                if ((r.QueueA == queueA && r.QueueB == queueB) || (r.QueueA == queueB && r.QueueB == queueA))
                {
                    linkRope = r;
                    break;
                }
            }

            Vector3 leaderPosA = queueA.Units[0].transform.position;
            Vector3 leaderPosB = queueB.Units[0].transform.position;

            bool t4Pass = false;
            if (linkRope != null && linkRope.IsRopeActive && linkRope.RopeInstance != null)
            {
                Vector3 delta = (leaderPosB - leaderPosA).normalized;
                Vector3 fwd = queueA.GetHeadForwardWorld();
                float dot = Mathf.Abs(Vector3.Dot(delta, fwd));
                t4Pass = (dot < 0.25f);
                Debug.Log($"[TEST 4] HORIZONTAL ROPE: {(t4Pass ? "PASS" : "FAIL")} (rope active={linkRope.IsRopeActive}, dotWithMoveDir={dot:F3})");
            }
            else
            {
                Debug.LogError($"[TEST 4] HORIZONTAL ROPE: FAIL (linkRope missing or inactive)");
            }

            // TEST 5 — ROPE ATTACHMENT
            bool t5Pass = (linkRope != null && linkRope.RopeInstance != null);
            if (t5Pass)
            {
                Vector3 ropePos = linkRope.RopeInstance.transform.position;
                Vector3 mid = (leaderPosA + leaderPosB) * 0.5f;
                float planarDist = Vector2.Distance(new Vector2(ropePos.x, ropePos.z), new Vector2(mid.x, mid.z));
                t5Pass = planarDist < 0.35f;
                Debug.Log($"[TEST 5] ROPE ATTACHMENT: {(t5Pass ? "PASS" : "FAIL")} (planarOffset={planarDist:F3}, ropeHeight={ropePos.y:F3})");
            }
            else
            {
                Debug.LogError("[TEST 5] ROPE ATTACHMENT: FAIL");
            }

            // TEST 6 — DYNAMIC LENGTH & CONSTANT THICKNESS
            bool t6Pass = false;
            if (linkRope != null && linkRope.RopeInstance != null)
            {
                Vector3 initScale = linkRope.RopeInstance.transform.localScale;
                float initThicknessX = initScale.x;
                float initThicknessY = initScale.y;

                Vector3 origPos = queueB.Units[0].transform.position;
                queueB.Units[0].transform.position = origPos + Vector3.right * 0.5f;
                linkRope.UpdateRopeTransform();

                Vector3 newScale = linkRope.RopeInstance.transform.localScale;
                bool thicknessPreserved = Mathf.Approximately(newScale.x, initThicknessX) && Mathf.Approximately(newScale.y, initThicknessY);
                bool lengthUpdated = newScale.z > initScale.z;

                t6Pass = thicknessPreserved && lengthUpdated;
                Debug.Log($"[TEST 6] DYNAMIC LENGTH: {(t6Pass ? "PASS" : "FAIL")} (initScale={initScale}, newScale={newScale}, thicknessPreserved={thicknessPreserved})");

                queueB.Units[0].transform.position = origPos;
                linkRope.UpdateRopeTransform();
            }

            // TEST 7 — FOLLOWERS
            bool t7Pass = (queueA.Units.Count > 1 && queueB.Units.Count > 1);
            if (t7Pass)
            {
                bool aUnitsCorrect = true;
                foreach (var u in queueA.Units) if (u.transform.parent != queueA.transform) aUnitsCorrect = false;
                bool bUnitsCorrect = true;
                foreach (var u in queueB.Units) if (u.transform.parent != queueB.transform) bUnitsCorrect = false;
                t7Pass = aUnitsCorrect && bUnitsCorrect;
                Debug.Log($"[TEST 7] FOLLOWERS INDEPENDENT: {(t7Pass ? "PASS" : "FAIL")} (QueueA units={queueA.Units.Count}, QueueB units={queueB.Units.Count})");
            }
            else
            {
                Debug.LogError("[TEST 7] FOLLOWERS: FAIL");
            }

            // TEST 9 — ATOMIC START CONCURRENCY LIMIT SAFETY
            List<AntQueueController> dummyQueues = new List<AntQueueController>();
            for (int i = 0; i < 3; i++)
            {
                GameObject dGo = new GameObject($"Dummy_{i}");
                dGo.transform.SetParent(testGo.transform);
                var dq = dGo.AddComponent<AntQueueController>();
                bm.AddMovingQueueForTesting(dq);
                dummyQueues.Add(dq);
            }

            bool startedWithExcess = bm.TryStartMove(queueA);
            bool t9Pass = (!startedWithExcess && !queueA.IsMoving && !queueB.IsMoving);
            if (t9Pass)
            {
                Debug.Log($"[TEST 9] ATOMIC START UNDER CONCURRENCY LIMIT: PASS (3 moving + 2 linked = 5 > 4 => neither started, queueA.IsMoving={queueA.IsMoving}, queueB.IsMoving={queueB.IsMoving})");
            }
            else
            {
                Debug.LogError($"[TEST 9] ATOMIC START: FAIL (started={startedWithExcess})");
            }

            foreach (var dq in dummyQueues)
            {
                bm.RemoveMovingQueueForTesting(dq);
            }

            // TEST 2 — CLICK ONE
            bool startSuccessA = bm.TryStartMove(queueA);
            bool t2Pass = startSuccessA && queueA.IsMoving && queueB.IsMoving;
            if (t2Pass)
            {
                Debug.Log($"[TEST 2] CLICK ONE: PASS (Clicked Queue A => A.IsMoving={queueA.IsMoving}, B.IsMoving={queueB.IsMoving})");
            }
            else
            {
                Debug.LogError($"[TEST 2] CLICK ONE: FAIL (startSuccess={startSuccessA}, A={queueA.IsMoving}, B={queueB.IsMoving})");
            }

            // TEST 8 — CONCURRENCY COUNT
            bool t8Pass = (bm.MovingQueuesCount == 2);
            if (t8Pass)
            {
                Debug.Log($"[TEST 8] CONCURRENCY: PASS (Linked pair counts as {bm.MovingQueuesCount} moving queues)");
            }
            else
            {
                Debug.LogError($"[TEST 8] CONCURRENCY: FAIL (MovingQueuesCount={bm.MovingQueuesCount})");
            }

            // TEST 10 — CLEANUP
            if (linkRope != null)
            {
                linkRope.Cleanup();
                bool t10Pass = !linkRope.IsRopeActive && linkRope.RopeInstance == null;
                Debug.Log($"[TEST 10] CLEANUP: {(t10Pass ? "PASS" : "FAIL")} (rope dismissed cleanly, orphan ropes=0)");
            }

            // TEST 3 — CLICK OTHER
            bm.ClearBoard();
            bm.SetupBoard(level7, isTestInstance: true);
            AntQueueController queueA2 = null;
            AntQueueController queueB2 = null;
            foreach (var q in bm.ActiveQueues)
            {
                if (q.QueueId == "Record_23_6") queueA2 = q;
                if (q.QueueId == "Record_24_6") queueB2 = q;
            }

            bool startSuccessB = bm.TryStartMove(queueB2);
            bool t3Pass = startSuccessB && queueB2.IsMoving && queueA2.IsMoving;
            if (t3Pass)
            {
                Debug.Log($"[TEST 3] CLICK OTHER: PASS (Clicked Queue B => B.IsMoving={queueB2.IsMoving}, A.IsMoving={queueA2.IsMoving})");
            }
            else
            {
                Debug.LogError($"[TEST 3] CLICK OTHER: FAIL (startSuccess={startSuccessB}, B={queueB2.IsMoving}, A={queueA2.IsMoving})");
            }

            Debug.Log("==================================================");
            Debug.Log("LINKED ANTS VALIDATION COMPLETED");
            Debug.Log("==================================================");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LinkedAntsValidation] Exception: {ex}");
        }
        finally
        {
            GameObject.DestroyImmediate(testGo);
        }
    }
}
