using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public class ReferenceLevels1To5Tests
    {
        private const string NormalDbPath = "Assets/Data/Levels/LevelDatabase.asset";

        [Test]
        public void Test01_NormalLevelDatabaseLoadsLevels1To5AsReferenceLevels()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDbPath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{NormalDbPath}'!");
            Assert.IsTrue(db.LevelCount >= 5, $"Expected at least 5 levels, got {db.LevelCount}");

            string[] expectedNames = { "Level_001", "Level_002", "Level_003", "Level_004", "Level_005" };
            int[] expectedW = { 8, 12, 14, 14, 14 };
            int[] expectedH = { 10, 10, 10, 12, 12 };

            for (int i = 0; i < 5; i++)
            {
                Level level = db.GetLevel(i + 1);
                Assert.IsNotNull(level, $"Level {i + 1} is null!");
                Assert.AreEqual(expectedNames[i], level.name, $"Level {i + 1} asset name mismatch!");
                Assert.AreEqual(expectedW[i], level.GridWidth, $"Level {i + 1} GridWidth mismatch!");
                Assert.AreEqual(expectedH[i], level.GridHeight, $"Level {i + 1} GridHeight mismatch!");
                Assert.IsTrue(ReferenceLevelRuntimeAdapter.IsReferenceLevel(level), $"Level {i + 1} must be detected as reference level!");
            }
        }

        [Test]
        public void Test02_ColorMappingPreservesAllDistinctColorsWithoutCollapsing()
        {
            // Verify all 12 authoritative mappings
            Assert.AreEqual(AntColor.SkyBlue, ReferenceColorMapper.ToAntColor(0), "ID 0 must be SkyBlue");
            Assert.AreEqual(AntColor.Lime, ReferenceColorMapper.ToAntColor(1), "ID 1 must be Lime");
            Assert.AreEqual(AntColor.Yellow, ReferenceColorMapper.ToAntColor(2), "ID 2 must be Yellow");
            Assert.AreEqual(AntColor.Purple, ReferenceColorMapper.ToAntColor(3), "ID 3 must be Purple");
            Assert.AreEqual(AntColor.Orange, ReferenceColorMapper.ToAntColor(4), "ID 4 must be Orange");
            Assert.AreEqual(AntColor.Pink, ReferenceColorMapper.ToAntColor(5), "ID 5 must be Pink");
            Assert.AreEqual(AntColor.Indigo, ReferenceColorMapper.ToAntColor(6), "ID 6 must be Indigo");
            Assert.AreEqual(AntColor.Green, ReferenceColorMapper.ToAntColor(7), "ID 7 must be Green");
            Assert.AreEqual(AntColor.Brown, ReferenceColorMapper.ToAntColor(8), "ID 8 must be Brown");
            Assert.AreEqual(AntColor.White, ReferenceColorMapper.ToAntColor(9), "ID 9 must be White");
            Assert.AreEqual(AntColor.Red, ReferenceColorMapper.ToAntColor(10), "ID 10 must be Red");
            Assert.AreEqual(AntColor.Turquoise, ReferenceColorMapper.ToAntColor(11), "ID 11 must be Turquoise");

            // Verify distinction
            Assert.AreNotEqual(AntColor.SkyBlue, AntColor.Indigo, "SkyBlue must not equal Indigo");
            Assert.AreNotEqual(AntColor.SkyBlue, AntColor.Turquoise, "SkyBlue must not equal Turquoise");
            Assert.AreNotEqual(AntColor.Indigo, AntColor.Turquoise, "Indigo must not equal Turquoise");
            Assert.AreNotEqual(AntColor.Lime, AntColor.Green, "Lime must not equal Green");

            // Verify palette colors are defined
            for (int id = 0; id <= 11; id++)
            {
                AntColor ac = ReferenceColorMapper.ToAntColor(id);
                Color col = AntColorPalette.GetColor(ac);
                Assert.IsTrue(col.a > 0f, $"Color for {ac} must have valid alpha!");
            }
        }

        [Test]
        public void Test03_HoleCapacityMatchesPlayableQueuesForEachLevel()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDbPath);
            int[] expectedHoleCounts = { 5, 5, 5, 9, 6 };
            int[] expectedQueueCounts = { 5, 10, 15, 14, 23 };

            for (int i = 0; i < 5; i++)
            {
                Level level = db.GetLevel(i + 1);
                var holes = ReferenceLevelRuntimeAdapter.ExtractHoles(level);
                var queues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level, holes);

                Assert.AreEqual(expectedHoleCounts[i], holes.Count, $"Level {i + 1} hole count mismatch!");
                Assert.AreEqual(expectedQueueCounts[i], queues.Count, $"Level {i + 1} queue count mismatch!");

                int sumCapacity = 0;
                foreach (var h in holes)
                {
                    sumCapacity += h.Capacity;
                    Assert.IsTrue(h.Capacity > 0, $"Level {i + 1} hole for color {h.ColorID} should have capacity > 0!");
                }

                Assert.AreEqual(expectedQueueCounts[i], sumCapacity, $"Level {i + 1} total hole capacity must equal queue count!");
            }
        }

        [Test]
        public void Test04_HoleFootprintsAndDimensionsCalculatedAccurately()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDbPath);

            for (int i = 0; i < 5; i++)
            {
                Level level = db.GetLevel(i + 1);
                var holes = ReferenceLevelRuntimeAdapter.ExtractHoles(level);

                foreach (var h in holes)
                {
                    Assert.IsTrue(h.OccupiedCells.Count > 0, $"Level {i + 1} hole has empty occupied cells!");
                    Assert.AreEqual(h.MaxX - h.MinX + 1, h.WidthCells, $"Level {i + 1} hole WidthCells mismatch!");
                    Assert.AreEqual(h.MaxY - h.MinY + 1, h.HeightCells, $"Level {i + 1} hole HeightCells mismatch!");
                    Assert.IsTrue(h.WidthCells >= 1 && h.HeightCells >= 1, $"Level {i + 1} dimensions must be >= 1!");
                }
            }
        }

        [Test]
        public void Test05_BoardManagerSpawnsCenteredHolesWithCorrectBadges()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDbPath);

            for (int i = 0; i < 5; i++)
            {
                Level level = db.GetLevel(i + 1);
                var rHoles = ReferenceLevelRuntimeAdapter.ExtractHoles(level);

                GameObject boardObj = new GameObject($"TestBoard_{level.name}");
                var bm = boardObj.AddComponent<BoardManager>();

                try
                {
                    bm.SetupBoard(level, true);

                    var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
                    Assert.AreEqual(rHoles.Count, spawnedHoles.Length, $"Spawned hole count mismatch in {level.name}!");

                    foreach (var sh in spawnedHoles)
                    {
                        Assert.IsTrue(sh.RemainingCount > 0, $"Hole {sh.name} remainingCount must be > 0!");
                        Assert.IsTrue(sh.WidthCells >= 1, $"Hole {sh.name} widthCells must be >= 1!");
                        Assert.IsTrue(sh.HeightCells >= 1, $"Hole {sh.name} heightCells must be >= 1!");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(boardObj);
                }
            }
        }

        [Test]
        public void Test06_SingleQueueHolderVisualOverrideAppliedAccurately()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDbPath);
            int[] expectedOverrides = { 5, 0, 1, 4, 0 };
            int totalOverrides = 0;

            for (int i = 0; i < 5; i++)
            {
                Level level = db.GetLevel(i + 1);
                GameObject boardObj = new GameObject($"TestBoard_Overrides_{level.name}");
                var bm = boardObj.AddComponent<BoardManager>();

                try
                {
                    bm.SetupBoard(level, true);
                    var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
                    int levelOverrides = 0;

                    foreach (var sh in spawnedHoles)
                    {
                        if (sh.InitialQueueCount == 1)
                        {
                            levelOverrides++;
                            totalOverrides++;
                            Assert.IsTrue(sh.SingleQueueVisualOverrideApplied, $"Hole {sh.name} in {level.name} should have override applied!");

                            // Verify exact child transforms
                            Transform root = sh.transform.Find("HoleSlotRoot");
                            Assert.IsNotNull(root, $"HoleSlotRoot must exist on {sh.name}");

                            Transform rim = root.Find("SlotRim");
                            Assert.IsNotNull(rim, $"SlotRim must exist on {sh.name}");
                            Assert.AreEqual(1.9205f, rim.localScale.x, 0.001f, $"SlotRim scale X mismatch on {sh.name}");

                            Transform bevel = root.Find("SlotInnerBevel");
                            Assert.IsNotNull(bevel, $"SlotInnerBevel must exist on {sh.name}");
                            Assert.AreEqual(2.3861f, bevel.localScale.x, 0.001f, $"SlotInnerBevel scale X mismatch on {sh.name}");

                            Transform pit = root.Find("SlotPit");
                            Assert.IsNotNull(pit, $"SlotPit must exist on {sh.name}");
                            Assert.AreEqual(2.6535f, pit.localScale.x, 0.001f, $"SlotPit scale X mismatch on {sh.name}");

                            Transform badge = root.Find("SlotCounterBadge");
                            Assert.IsNotNull(badge, $"SlotCounterBadge must exist on {sh.name}");
                            Assert.AreEqual(0.35f, badge.localPosition.x, 0.001f, $"SlotCounterBadge pos X mismatch on {sh.name}");
                            Assert.AreEqual(0.0f, badge.localPosition.y, 0.001f, $"SlotCounterBadge pos Y mismatch on {sh.name}");
                            Assert.AreEqual(0.0f, badge.localPosition.z, 0.001f, $"SlotCounterBadge pos Z mismatch on {sh.name}");

                            Transform badgeMesh = badge.Find("BadgeMesh");
                            Assert.IsNotNull(badgeMesh, $"BadgeMesh must exist on {sh.name}");
                            Assert.AreEqual(0.25f, badgeMesh.localPosition.x, 0.001f, $"BadgeMesh pos X mismatch on {sh.name}");
                            Assert.AreEqual(0.0f, badgeMesh.localPosition.y, 0.001f, $"BadgeMesh pos Y mismatch on {sh.name}");
                            Assert.AreEqual(-0.25f, badgeMesh.localPosition.z, 0.001f, $"BadgeMesh pos Z mismatch on {sh.name}");

                            Transform badgeText = badge.Find("BadgeCountText");
                            Assert.IsNotNull(badgeText, $"BadgeCountText must exist on {sh.name}");
                            Assert.AreEqual(0.25f, badgeText.localPosition.x, 0.001f, $"BadgeCountText pos X mismatch on {sh.name}");
                            Assert.AreEqual(0.22f, badgeText.localPosition.y, 0.001f, $"BadgeCountText pos Y mismatch on {sh.name}");
                            Assert.AreEqual(-0.25f, badgeText.localPosition.z, 0.001f, $"BadgeCountText pos Z mismatch on {sh.name}");
                        }
                        else
                        {
                            Assert.IsFalse(sh.SingleQueueVisualOverrideApplied, $"Hole {sh.name} in {level.name} (count {sh.InitialQueueCount}) should NOT have override applied!");
                        }
                    }

                    Assert.AreEqual(expectedOverrides[i], levelOverrides, $"Level {i + 1} override count mismatch!");
                }
                finally
                {
                    Object.DestroyImmediate(boardObj);
                }
            }

            Assert.AreEqual(10, totalOverrides, "Total overrides across Levels 1-5 must equal exactly 10!");
        }

        [Test]
        public void Test07_ColorConsistencyBetweenHoldersAndMatchingQueues()
        {
            // 1. Verify Authoritative Raw Color Map 0..11 and 12 fallback
            Assert.AreEqual(AntColor.SkyBlue, ReferenceColorMapper.ToAntColor(0), "0 must be SkyBlue");
            Assert.AreEqual(AntColor.Lime, ReferenceColorMapper.ToAntColor(1), "1 must be Lime");
            Assert.AreEqual(AntColor.Yellow, ReferenceColorMapper.ToAntColor(2), "2 must be Yellow");
            Assert.AreEqual(AntColor.Purple, ReferenceColorMapper.ToAntColor(3), "3 must be Purple");
            Assert.AreEqual(AntColor.Orange, ReferenceColorMapper.ToAntColor(4), "4 must be Orange");
            Assert.AreEqual(AntColor.Pink, ReferenceColorMapper.ToAntColor(5), "5 must be Pink");
            Assert.AreEqual(AntColor.Indigo, ReferenceColorMapper.ToAntColor(6), "6 must be Indigo");
            Assert.AreEqual(AntColor.Green, ReferenceColorMapper.ToAntColor(7), "7 must be Green");
            Assert.AreEqual(AntColor.Brown, ReferenceColorMapper.ToAntColor(8), "8 must be Brown");
            Assert.AreEqual(AntColor.White, ReferenceColorMapper.ToAntColor(9), "9 must be White");
            Assert.AreEqual(AntColor.Red, ReferenceColorMapper.ToAntColor(10), "10 must be Red");
            Assert.AreEqual(AntColor.Turquoise, ReferenceColorMapper.ToAntColor(11), "11 must be Turquoise");
            Assert.AreEqual(AntColor.White, ReferenceColorMapper.ToAntColor(12), "12 must fall back to White without guessing");

            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDbPath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{NormalDbPath}'!");

            HashSet<int> loggedColorIds = new HashSet<int>();

            for (int l = 0; l < 5; l++)
            {
                int levelNum = l + 1;
                Level level = db.GetLevel(levelNum);
                Assert.IsNotNull(level, $"Level {levelNum} is null!");

                GameObject boardObj = new GameObject($"TestBoard_ColorConsistency_Level_{levelNum}");
                var bm = boardObj.AddComponent<BoardManager>();

                try
                {
                    bm.SetupBoard(level, true);

                    var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
                    var spawnedQueues = boardObj.GetComponentsInChildren<AntQueueController>(true);

                    Assert.IsTrue(spawnedHoles.Length > 0, $"Level {levelNum} has no spawned holes!");
                    Assert.IsTrue(spawnedQueues.Length > 0, $"Level {levelNum} has no spawned queues!");

                    // Map holes by raw ColorID
                    Dictionary<int, HoleController> holeByColorId = new Dictionary<int, HoleController>();
                    foreach (var h in spawnedHoles)
                    {
                        if (!holeByColorId.ContainsKey(h.ColorID))
                        {
                            holeByColorId[h.ColorID] = h;
                        }
                    }

                    foreach (var q in spawnedQueues)
                    {
                        int rawId = q.ColorID;
                        AntColor resolvedColor = ReferenceColorMapper.ToAntColor(rawId);

                        Assert.AreEqual(resolvedColor, q.Color, $"Queue {q.QueueId} resolved color mismatch!");
                        Assert.IsTrue(holeByColorId.ContainsKey(rawId), $"Level {levelNum} Queue {q.QueueId} has no matching holder for raw ColorID {rawId}!");

                        HoleController matchingHole = holeByColorId[rawId];
                        Assert.AreEqual(rawId, matchingHole.ColorID, $"Holder {matchingHole.name} ColorID mismatch!");
                        Assert.AreEqual(resolvedColor, matchingHole.Color, $"Holder {matchingHole.name} resolved color mismatch!");

                        // Check materials and base colors
                        Material antMat = AntColorPalette.GetMaterial(resolvedColor);
                        Material rimMat = AntColorPalette.GetHoleMaterial(resolvedColor);
                        Color expectedBaseColor = AntColorPalette.GetColor(resolvedColor);

                        // Ant material base color
                        Color antBaseColor = antMat.HasProperty("_BaseColor") ? antMat.GetColor("_BaseColor") : antMat.color;
                        Assert.AreEqual(expectedBaseColor.r, antBaseColor.r, 0.001f, $"Ant material red mismatch for {resolvedColor}!");
                        Assert.AreEqual(expectedBaseColor.g, antBaseColor.g, 0.001f, $"Ant material green mismatch for {resolvedColor}!");
                        Assert.AreEqual(expectedBaseColor.b, antBaseColor.b, 0.001f, $"Ant material blue mismatch for {resolvedColor}!");

                        // Holder rim material base color
                        Transform slotRoot = matchingHole.transform.Find("HoleSlotRoot");
                        Assert.IsNotNull(slotRoot, $"HoleSlotRoot must exist on {matchingHole.name}!");
                        Transform rimTr = slotRoot.Find("SlotRim");
                        Assert.IsNotNull(rimTr, $"SlotRim must exist on {matchingHole.name}!");
                        var rimRen = rimTr.GetComponent<MeshRenderer>();
                        Assert.IsNotNull(rimRen, $"SlotRim MeshRenderer missing on {matchingHole.name}!");
                        Material actualRimMat = rimRen.sharedMaterial != null ? rimRen.sharedMaterial : rimRen.material;

                        Color rimBaseColor = actualRimMat.HasProperty("_BaseColor") ? actualRimMat.GetColor("_BaseColor") : actualRimMat.color;
                        Assert.AreEqual(expectedBaseColor.r, rimBaseColor.r, 0.001f, $"Holder rim red mismatch for {resolvedColor}!");
                        Assert.AreEqual(expectedBaseColor.g, rimBaseColor.g, 0.001f, $"Holder rim green mismatch for {resolvedColor}!");
                        Assert.AreEqual(expectedBaseColor.b, rimBaseColor.b, 0.001f, $"Holder rim blue mismatch for {resolvedColor}!");

                        // Base RGB match check
                        bool rgbMatch = Mathf.Approximately(antBaseColor.r, rimBaseColor.r) &&
                                        Mathf.Approximately(antBaseColor.g, rimBaseColor.g) &&
                                        Mathf.Approximately(antBaseColor.b, rimBaseColor.b);
                        Assert.IsTrue(rgbMatch, $"Base RGB mismatch between ant and rim for ColorID {rawId} ({resolvedColor})!");

                        // Check SlotInnerBevel maintains same base hue
                        Transform bevelTr = slotRoot.Find("SlotInnerBevel");
                        Assert.IsNotNull(bevelTr, $"SlotInnerBevel missing on {matchingHole.name}!");
                        var bevelRen = bevelTr.GetComponent<MeshRenderer>();
                        Assert.IsNotNull(bevelRen, $"SlotInnerBevel MeshRenderer missing on {matchingHole.name}!");
                        Material actualBevelMat = bevelRen.sharedMaterial != null ? bevelRen.sharedMaterial : bevelRen.material;
                        Color bevelBaseColor = actualBevelMat.HasProperty("_BaseColor") ? actualBevelMat.GetColor("_BaseColor") : actualBevelMat.color;
                        Color expectedBevelColor = Color.Lerp(expectedBaseColor, Color.black, 0.38f);
                        Assert.AreEqual(expectedBevelColor.r, bevelBaseColor.r, 0.01f, $"Bevel red mismatch for {resolvedColor}!");
                        Assert.AreEqual(expectedBevelColor.g, bevelBaseColor.g, 0.01f, $"Bevel green mismatch for {resolvedColor}!");
                        Assert.AreEqual(expectedBevelColor.b, bevelBaseColor.b, 0.01f, $"Bevel blue mismatch for {resolvedColor}!");

                        // Diagnostic log for one holder + queue of every distinct ColorID
                        if (loggedColorIds.Add(rawId))
                        {
                            Debug.Log($"[ColorConsistency]\nRawID={rawId}\nResolved={resolvedColor}\nQueueMaterial={antMat.name}\nHolderBaseColor=RGBA({rimBaseColor.r:F3}, {rimBaseColor.g:F3}, {rimBaseColor.b:F3}, {rimBaseColor.a:F3})\nHolderRimMaterial={actualRimMat.name}\nMATCH={rgbMatch}");
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(boardObj);
                }
            }

            // Verify Level 1 exact 5 expected colors
            Assert.IsTrue(loggedColorIds.Contains(2), "Level 1 must contain Raw 2 (Yellow)");
            Assert.IsTrue(loggedColorIds.Contains(7), "Level 1 must contain Raw 7 (Green)");
            Assert.IsTrue(loggedColorIds.Contains(0), "Level 1 must contain Raw 0 (SkyBlue)");
            Assert.IsTrue(loggedColorIds.Contains(8), "Level 1 must contain Raw 8 (Brown)");
            Assert.IsTrue(loggedColorIds.Contains(10), "Level 1 must contain Raw 10 (Red)");
        }

        [Test]
        public void Test08_HolderRemovalAndFootprintReleaseLevel1()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDbPath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{NormalDbPath}'!");

            Level level1 = db.GetLevel(1);
            Assert.IsNotNull(level1, "Level 1 is null!");

            GameObject boardObj = new GameObject("TestBoard_HolderRemoval_Level1");
            var bm = boardObj.AddComponent<BoardManager>();

            try
            {
                bm.SetupBoard(level1, true);

                var spawnedHoles = boardObj.GetComponentsInChildren<HoleController>(true);
                var spawnedQueues = boardObj.GetComponentsInChildren<AntQueueController>(true);

                Assert.AreEqual(5, spawnedHoles.Length, "Level 1 must spawn exactly 5 holders!");
                Assert.AreEqual(5, spawnedQueues.Length, "Level 1 must spawn exactly 5 queues!");

                int[] expectedRawColors = { 2, 7, 0, 8, 10 }; // Yellow, Green, SkyBlue, Brown, Red
                Dictionary<int, HoleController> holesByColor = new Dictionary<int, HoleController>();
                Dictionary<int, AntQueueController> queuesByColor = new Dictionary<int, AntQueueController>();

                foreach (var h in spawnedHoles) holesByColor[h.ColorID] = h;
                foreach (var q in spawnedQueues) queuesByColor[q.ColorID] = q;

                // 1. Verify BEFORE state for all 5 holders
                foreach (int rawId in expectedRawColors)
                {
                    Assert.IsTrue(holesByColor.ContainsKey(rawId), $"Missing holder for ColorID {rawId}");
                    HoleController hole = holesByColor[rawId];

                    Assert.AreEqual(1, hole.RemainingCount, $"Holder {hole.Color} initial counter must be 1!");
                    Assert.IsFalse(hole.IsCompleted, $"Holder {hole.Color} must not be completed before queue entry!");
                    Assert.AreEqual(2, hole.OccupiedCells.Count, $"Holder {hole.Color} must occupy exactly 2 footprint cells (2x1)!");

                    foreach (var cell in hole.OccupiedCells)
                    {
                        Assert.IsTrue(bm.HolesMap.ContainsKey(cell), $"HolesMap must contain footprint cell ({cell.x},{cell.y}) for {hole.Color}!");
                        Assert.AreEqual(hole, bm.GetHoleAt(cell), $"GetHoleAt must return holder for footprint cell ({cell.x},{cell.y})!");
                        Assert.IsFalse(bm.IsCellFree(cell), $"Cell ({cell.x},{cell.y}) must be blocked while holder {hole.Color} is active!");
                    }
                }

                // 2. Simulate sequential completion of each matching queue
                int completedCount = 0;
                foreach (int rawId in expectedRawColors)
                {
                    HoleController hole = holesByColor[rawId];
                    AntQueueController queue = queuesByColor[rawId];
                    var footprintCells = new List<Vector2Int>(hole.OccupiedCells);

                    // Complete the queue
                    bm.OnQueueCompleted(queue);
                    completedCount++;

                    // Verify AFTER state for this completed holder
                    Assert.AreEqual(0, hole.RemainingCount, $"Holder {hole.Color} counter must reach 0!");
                    Assert.IsTrue(hole.IsCompleted, $"Holder {hole.Color} must be marked IsCompleted = true!");
                    Assert.AreEqual(completedCount, bm.CompletedHoleCount, "CompletedHoleCount must increment!");
                    Assert.IsFalse(bm.ActiveHoles.Contains(hole), $"Holder {hole.Color} must be removed from ActiveHoles!");

                    // Verify BOTH cells of the 2x1 footprint are completely released
                    foreach (var cell in footprintCells)
                    {
                        Assert.IsFalse(bm.HolesMap.ContainsKey(cell), $"Cell ({cell.x},{cell.y}) must be removed from HolesMap after {hole.Color} completion!");
                        Assert.IsNull(bm.GetHoleAt(cell), $"GetHoleAt(({cell.x},{cell.y})) must return null after {hole.Color} completion!");
                        Assert.IsTrue(bm.IsCellFree(cell), $"Cell ({cell.x},{cell.y}) must be immediately free and walkable after {hole.Color} completion!");
                    }
                }

                // 3. Verify final board and win condition
                Assert.AreEqual(0, bm.ActiveQueues.Count, "All queues must be completed (ActiveQueues.Count == 0)!");
                Assert.AreEqual(0, bm.ActiveHoles.Count, "All holders must be removed (ActiveHoles.Count == 0)!");
                Assert.AreEqual(5, bm.CompletedHoleCount, "All 5 holders must be recorded as completed!");
                Assert.AreEqual(0, bm.HolesMap.Count, "HolesMap must have 0 remaining occupied cells!");
            }
            finally
            {
                Object.DestroyImmediate(boardObj);
            }
        }
    }
}
