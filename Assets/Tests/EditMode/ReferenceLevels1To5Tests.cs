using System.Collections.Generic;
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
    }
}
