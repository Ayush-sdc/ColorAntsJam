using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public class LevelDatabaseTests
    {
        private const string RefDatabasePath = "Assets/Data/ReferenceLevelDatabase.asset";
        private const string NormalDatabasePath = "Assets/Data/Levels/LevelDatabase.asset";
        private const string Level1Path = "Assets/Data/Levels/Level_01.asset";
        private const string Level2Path = "Assets/Data/Levels/Level_02.asset";

        [Test]
        public void Test01_ReferenceLevelDatabaseContainsExactly45Levels()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(RefDatabasePath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{RefDatabasePath}'!");
            Assert.IsNotNull(db.Levels, "Levels list is null in ReferenceLevelDatabase!");
            Assert.AreEqual(45, db.Levels.Count, $"ReferenceLevelDatabase must contain exactly 45 levels, but has {db.Levels.Count}!");
        }

        [Test]
        public void Test01b_NormalLevelDatabaseContainsExactly45Levels()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(NormalDatabasePath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{NormalDatabasePath}'!");
            Assert.IsNotNull(db.Levels, "Levels list is null in normal LevelDatabase!");
            Assert.AreEqual(45, db.Levels.Count, $"Normal LevelDatabase must contain exactly 45 levels, but has {db.Levels.Count}!");
            Assert.AreEqual(45, db.LevelCount, $"Normal LevelDatabase.LevelCount must be 45, but was {db.LevelCount}!");
            
            // Check first and last levels
            Assert.AreEqual("Level_001", db.Levels[0].name, "First level must be Level_001");
            Assert.AreEqual("Level_045", db.Levels[44].name, "Last level must be Level_045");
            
            // Check all levels non-null
            for (int i = 0; i < db.Levels.Count; i++)
            {
                Assert.IsNotNull(db.Levels[i], $"Level at index {i} in normal LevelDatabase is null!");
            }
        }

        [Test]
        public void Test02_Level001IsIndex0()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(RefDatabasePath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{RefDatabasePath}'!");
            Assert.IsTrue(db.Levels.Count > 0, "ReferenceLevelDatabase has no levels!");
            Level level0 = db.Levels[0];
            Assert.IsNotNull(level0, "Level at index 0 is null!");
            Assert.AreEqual("Level_001", level0.name, "Index 0 level asset must be Level_001!");
            Assert.AreEqual(1, level0.LevelNumber, "Index 0 LevelNumber must be 1!");
        }

        [Test]
        public void Test03_Level045IsIndex44()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(RefDatabasePath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{RefDatabasePath}'!");
            Assert.IsTrue(db.Levels.Count >= 45, "ReferenceLevelDatabase does not have 45 levels!");
            Level level44 = db.Levels[44];
            Assert.IsNotNull(level44, "Level at index 44 is null!");
            Assert.AreEqual("Level_045", level44.name, "Index 44 level asset must be Level_045!");
            Assert.AreEqual(45, level44.LevelNumber, "Index 44 LevelNumber must be 45!");
        }

        [Test]
        public void Test04_NoNullLevels()
        {
            LevelDatabase db = AssetDatabase.LoadAssetAtPath<LevelDatabase>(RefDatabasePath);
            Assert.IsNotNull(db, $"Could not load LevelDatabase at '{RefDatabasePath}'!");
            Assert.IsNotNull(db.Levels, "Levels list is null!");
            for (int i = 0; i < db.Levels.Count; i++)
            {
                Assert.IsNotNull(db.Levels[i], $"Level at index [{i}] is null!");
            }
        }

        [Test]
        public void Test05_Level1HandmadeAntTotalIs35()
        {
            Level level1 = AssetDatabase.LoadAssetAtPath<Level>(Level1Path);
            Assert.IsNotNull(level1, $"Could not load Level asset at '{Level1Path}'!");
            Assert.IsNotNull(level1.AntQueues, "Level 1 AntQueues is null!");

            int totalAnts = 0;
            foreach (var q in level1.AntQueues)
            {
                totalAnts += q.AntCount;
            }

            Assert.AreEqual(35, totalAnts, $"Level 1 handmade ant total must be 35, but was {totalAnts}!");
        }

        [Test]
        public void Test06_Level2HandmadeAntTotalIs60()
        {
            Level level2 = AssetDatabase.LoadAssetAtPath<Level>(Level2Path);
            Assert.IsNotNull(level2, $"Could not load Level asset at '{Level2Path}'!");
            Assert.IsNotNull(level2.AntQueues, "Level 2 AntQueues is null!");

            int totalAnts = 0;
            foreach (var q in level2.AntQueues)
            {
                totalAnts += q.AntCount;
            }

            Assert.AreEqual(60, totalAnts, $"Level 2 handmade ant total must be 60, but was {totalAnts}!");
        }
    }
}
