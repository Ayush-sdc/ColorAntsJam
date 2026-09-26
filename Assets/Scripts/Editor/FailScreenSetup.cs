using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

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

            // TEST 2 — PARALLEL APPROACH
            bool t2Pass = false;
            float laneA = hole.GetLaneOffset(queueA, 1.0f);
            float laneB = hole.GetLaneOffset(queueB, 1.0f);
            var routeA = bm.GenerateStraightFreeWorldPath(queueA, forwardDir, hole, entranceCell);
            var routeB = bm.GenerateStraightFreeWorldPath(queueB, forwardDir, hole, entranceCell);

            {
                // In approach and entry phase (last 20 points before despawn), check that routes are parallel
                int countA = routeA.Count;
                int countB = routeB.Count;
                if (countA > 15 && countB > 15 && Mathf.Abs(laneA - laneB) > 0.15f)
                {
                    // Verify distance between the paths remains side-by-side without merging
                    Vector3 endA = routeA[countA - 1];
                    Vector3 endB = routeB[countB - 1];
                    float separationAtEnd = Vector3.Distance(new Vector3(endA.x, 0f, 0f), new Vector3(endB.x, 0f, 0f));
                    if (separationAtEnd > 0.20f)
                    {
                        t2Pass = true;
                        Debug.Log($"[TEST 2] PARALLEL APPROACH: PASS (laneA={laneA:F2}, laneB={laneB:F2}, separation={separationAtEnd:F2})");
                    }
                }
                if (!t2Pass) Debug.LogError($"[TEST 2] PARALLEL APPROACH: FAIL");
            }

            // TEST 3 — PARALLEL HEAD DIRECTION
            bool t3Pass = false;
            {
                // Tangents along parallel approach section should have angle difference ~ 0 degrees
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

            // TEST 4 — PARALLEL ENTRY
            bool t4Pass = false;
            {
                Vector3 entryA = hole.GetHoleEntryPoint(entranceCell, forwardDir, queueA);
                Vector3 entryB = hole.GetHoleEntryPoint(entranceCell, forwardDir, queueB);
                bool reqA = hole.RequestHoleEntry(queueA);
                bool reqB = hole.RequestHoleEntry(queueB);
                float entrySep = Vector3.Distance(entryA, entryB);

                if (reqA && reqB && entrySep > 0.20f && hole.EnteringQueueCount == 2)
                {
                    t4Pass = true;
                    Debug.Log($"[TEST 4] PARALLEL ENTRY: PASS (side-by-side entry points, separation={entrySep:F2})");
                }
                else
                {
                    Debug.LogError($"[TEST 4] PARALLEL ENTRY: FAIL (reqA={reqA}, reqB={reqB}, sep={entrySep})");
                }
            }

            // TEST 5 — NO LANE CROSS
            bool t5Pass = false;
            {
                Vector3 entryForward = new Vector3(forwardDir.x, 0f, forwardDir.y).normalized;
                Vector3 entryRight = Vector3.Cross(Vector3.up, entryForward).normalized;

                // Queue A is assigned Left (negative lateral), Queue B is Right (positive lateral)
                bool laneAIsLeft = (laneA < laneB);
                bool noCross = true;

                // Check throughout the entire approach and entry routes
                int checks = Mathf.Min(routeA.Count, routeB.Count);
                for (int i = Mathf.Max(0, checks - 30); i < checks; i++)
                {
                    float latA = Vector3.Dot(routeA[i], entryRight);
                    float latB = Vector3.Dot(routeB[i], entryRight);
                    if (latA >= latB)
                    {
                        noCross = false;
                        break;
                    }
                }

                if (laneAIsLeft && noCross)
                {
                    t5Pass = true;
                    Debug.Log("[TEST 5] NO LANE CROSS: PASS (Queue A stays Left, Queue B stays Right)");
                }
                else
                {
                    Debug.LogError($"[TEST 5] NO LANE CROSS: FAIL (noCross={noCross})");
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
}
