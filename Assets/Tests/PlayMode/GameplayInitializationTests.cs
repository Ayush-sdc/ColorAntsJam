using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class GameplayInitializationTests
    {
        private GameObject gameManagersObj;
        private BoardManager boardManager;
        private LevelManager levelManager;
        private LevelDatabase levelDatabase;
        private Camera mainCamera;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Ensure a Main Camera exists
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

            // Create Manager GameObject
            gameManagersObj = new GameObject("GameManagers_PlayModeTest");
            boardManager = gameManagersObj.AddComponent<BoardManager>();
            levelManager = gameManagersObj.AddComponent<LevelManager>();

            #if UNITY_EDITOR
            levelDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/Data/LevelDatabase.asset");
            #endif

            var dbField = typeof(LevelManager).GetField("levelDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (dbField != null && levelDatabase != null)
            {
                dbField.SetValue(levelManager, levelDatabase);
            }

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

        [UnityTest]
        public IEnumerator Test01_BoardManagerInitializes()
        {
            Assert.IsNotNull(boardManager, "BoardManager component must be initialized!");
            Assert.AreEqual(boardManager, BoardManager.Instance, "BoardManager.Instance should point to the active instance!");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test02_Level1LoadsWithoutException()
        {
            Level level1 = null;
            if (levelDatabase != null && levelDatabase.Levels != null && levelDatabase.Levels.Count > 0)
            {
                level1 = levelDatabase.Levels[0];
            }
            #if UNITY_EDITOR
            if (level1 == null)
            {
                level1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/Levels/Level_01.asset");
            }
            #endif

            Assert.IsNotNull(level1, "Level 1 asset could not be loaded!");
            Assert.DoesNotThrow(() =>
            {
                boardManager.SetupBoard(level1);
            }, "BoardManager.SetupBoard(level1) should not throw any exception!");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test03_Exactly5QueuesSpawnInHandmadeLevel1()
        {
            Level level1 = null;
            if (levelDatabase != null && levelDatabase.Levels != null && levelDatabase.Levels.Count > 0)
            {
                level1 = levelDatabase.Levels[0];
            }
            #if UNITY_EDITOR
            if (level1 == null)
            {
                level1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/Levels/Level_01.asset");
            }
            #endif

            boardManager.SetupBoard(level1);
            yield return null;

            var queues = Object.FindObjectsByType<AntQueueController>(FindObjectsSortMode.None);
            Assert.AreEqual(5, queues.Length, $"Expected exactly 5 AntQueueController objects in Level 1, found {queues.Length}!");
        }

        [UnityTest]
        public IEnumerator Test04_Exactly35AntUnitsSpawnInHandmadeLevel1()
        {
            Level level1 = null;
            if (levelDatabase != null && levelDatabase.Levels != null && levelDatabase.Levels.Count > 0)
            {
                level1 = levelDatabase.Levels[0];
            }
            #if UNITY_EDITOR
            if (level1 == null)
            {
                level1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/Levels/Level_01.asset");
            }
            #endif

            boardManager.SetupBoard(level1);
            yield return null;

            var units = Object.FindObjectsByType<AntUnit>(FindObjectsSortMode.None);
            Assert.AreEqual(35, units.Length, $"Expected exactly 35 AntUnit objects in Level 1, found {units.Length}!");
        }

        [UnityTest]
        public IEnumerator Test05_NoDuplicateBoardManagerLevelManagerMainCamera()
        {
            Level level1 = null;
            if (levelDatabase != null && levelDatabase.Levels != null && levelDatabase.Levels.Count > 0)
            {
                level1 = levelDatabase.Levels[0];
            }
            #if UNITY_EDITOR
            if (level1 == null)
            {
                level1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/Levels/Level_01.asset");
            }
            #endif

            boardManager.SetupBoard(level1);
            yield return null;

            var boardManagers = Object.FindObjectsByType<BoardManager>(FindObjectsSortMode.None);
            Assert.AreEqual(1, boardManagers.Length, $"Expected exactly 1 BoardManager, found {boardManagers.Length}!");

            var levelManagers = Object.FindObjectsByType<LevelManager>(FindObjectsSortMode.None);
            Assert.AreEqual(1, levelManagers.Length, $"Expected exactly 1 LevelManager, found {levelManagers.Length}!");

            var mainCams = GameObject.FindGameObjectsWithTag("MainCamera");
            Assert.AreEqual(1, mainCams.Length, $"Expected exactly 1 GameObject with tag 'MainCamera', found {mainCams.Length}!");
        }

        [UnityTest]
        public IEnumerator Test06_ActiveInputHandling_ClassicInputAccessibleWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                Vector3 mousePos = Input.mousePosition;
                bool btn = Input.GetMouseButton(0);
                int touchCount = Input.touchCount;
            }, "Classic UnityEngine.Input APIs must not throw InvalidOperationException when Active Input Handling is set to Both or Input Manager!");

            yield return null;
        }
    }
}
