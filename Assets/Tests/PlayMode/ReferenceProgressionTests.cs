using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class ReferenceProgressionTests
    {
        private GameObject gameManagersObj;
        private BoardManager boardManager;
        private LevelManager levelManager;
        private LevelDatabase referenceDatabase;
        private Camera mainCamera;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (Camera.main == null)
            {
                var existingCam = GameObject.FindWithTag("MainCamera");
                if (existingCam != null)
                {
                    mainCamera = existingCam.GetComponent<Camera>();
                }
                else
                {
                    GameObject camObj = new GameObject("Main Camera");
                    camObj.tag = "MainCamera";
                    mainCamera = camObj.AddComponent<Camera>();
                }
            }
            else
            {
                mainCamera = Camera.main;
            }

            gameManagersObj = new GameObject("GameManagers_RefTest");
            boardManager = gameManagersObj.AddComponent<BoardManager>();
            levelManager = gameManagersObj.AddComponent<LevelManager>();

            #if UNITY_EDITOR
            referenceDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/ReferenceLevelDatabase.asset");
            #endif

            var refDbField = typeof(LevelManager).GetField("referenceLevelDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (refDbField != null && referenceDatabase != null)
            {
                refDbField.SetValue(levelManager, referenceDatabase);
            }
            levelManager.UseReferenceLevelDatabase = true;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (boardManager != null)
            {
                boardManager.ClearBoard();
            }

            if (gameManagersObj != null)
            {
                Object.Destroy(gameManagersObj);
            }
            yield return null;
        }

        [Test]
        public void Test01_ReferenceDatabaseContains45Levels()
        {
            Assert.IsNotNull(referenceDatabase, "ReferenceLevelDatabase.asset must be present!");
            Assert.AreEqual(45, referenceDatabase.LevelCount, "ReferenceLevelDatabase must contain exactly 45 levels!");
        }

        [UnityTest]
        public IEnumerator Test02_NavigationBounds_Level1ClampsQ_Level45ClampsE()
        {
            // Reset to Level 1
            levelManager.LoadLevel(1);
            yield return null;
            Assert.AreEqual(1, levelManager.CurrentLevelNumber, "Initial loaded level should be 1.");

            // Q (Previous) from Level 1 should remain on Level 1
            levelManager.LoadPreviousLevel();
            yield return null;
            Assert.AreEqual(1, levelManager.CurrentLevelNumber, "From Level 1, LoadPreviousLevel must remain on Level 1.");

            // Jump to Level 45
            levelManager.LoadLevel(45);
            yield return null;
            Assert.AreEqual(45, levelManager.CurrentLevelNumber, "Level 45 should load correctly.");

            // E (Next) from Level 45 should remain on Level 45
            levelManager.AdvanceToNextLevel();
            yield return null;
            Assert.AreEqual(45, levelManager.CurrentLevelNumber, "From Level 45, AdvanceToNextLevel must remain on Level 45.");

            // Restart reloads current level
            levelManager.RestartCurrentLevel();
            yield return null;
            Assert.AreEqual(45, levelManager.CurrentLevelNumber, "Restart should reload current level 45.");
        }

        [UnityTest]
        public IEnumerator Test03_ValidateLevel001()
        {
            yield return ValidateReferenceLevel(1, new Vector2Int(8, 5), expectedHoles: 5, expectedQueues: 5);
        }

        [UnityTest]
        public IEnumerator Test04_ValidateLevel002()
        {
            yield return ValidateReferenceLevel(2, new Vector2Int(10, 10), expectedHoles: 5, expectedQueues: 10);
        }

        [UnityTest]
        public IEnumerator Test05_ValidateLevel003()
        {
            yield return ValidateReferenceLevel(3, new Vector2Int(10, 10), expectedHoles: 5, expectedQueues: 15);
        }

        [UnityTest]
        public IEnumerator Test06_ValidateLevel007()
        {
            yield return ValidateReferenceLevel(7, new Vector2Int(15, 15), expectedHoles: 5, expectedQueues: 28);
        }

        [UnityTest]
        public IEnumerator Test07_ValidateLevel015()
        {
            yield return ValidateReferenceLevel(15, new Vector2Int(13, 13), expectedHoles: 4, expectedQueues: 10);
        }

        [UnityTest]
        public IEnumerator Test08_ValidateLevel021()
        {
            yield return ValidateReferenceLevel(21, new Vector2Int(12, 12), expectedHoles: 6, expectedQueues: 8);
        }

        [UnityTest]
        public IEnumerator Test09_ValidateLevel030()
        {
            yield return ValidateReferenceLevel(30, new Vector2Int(11, 11), expectedHoles: 2, expectedQueues: 11);
        }

        [UnityTest]
        public IEnumerator Test10_ValidateLevel045()
        {
            yield return ValidateReferenceLevel(45, new Vector2Int(40, 40), expectedHoles: 22, expectedQueues: 150);
        }

        private IEnumerator ValidateReferenceLevel(int levelNum, Vector2Int expectedGrid, int expectedHoles, int expectedQueues)
        {
            levelManager.LoadLevel(levelNum);
            yield return null;

            Level levelAsset = levelManager.ActiveLevel;
            Assert.IsNotNull(levelAsset, $"Level {levelNum} asset must not be null!");
            Assert.AreEqual(expectedGrid.x, levelAsset.GridWidth, $"Level {levelNum} GridWidth mismatch.");
            Assert.AreEqual(expectedGrid.y, levelAsset.GridHeight, $"Level {levelNum} GridHeight mismatch.");

            boardManager.SetupBoard(levelAsset);
            yield return null;

            // Verify queue count
            Assert.AreEqual(expectedQueues, boardManager.ActiveQueues.Count, $"Level {levelNum} active queue count mismatch!");

            // Verify hole count (distinct HoleController instances in holesMap)
            var distinctHoles = new HashSet<HoleController>(boardManager.HolesMap.Values);
            Assert.AreEqual(expectedHoles, distinctHoles.Count, $"Level {levelNum} hole count mismatch!");

            // Verify no duplicate managers exist
            var boardManagers = Object.FindObjectsByType<BoardManager>(FindObjectsSortMode.None);
            Assert.AreEqual(1, boardManagers.Length, "Expected exactly 1 BoardManager!");
            var levelManagers = Object.FindObjectsByType<LevelManager>(FindObjectsSortMode.None);
            Assert.AreEqual(1, levelManagers.Length, "Expected exactly 1 LevelManager!");
        }
    }
}
