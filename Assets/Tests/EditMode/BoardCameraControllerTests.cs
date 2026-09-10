using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public class BoardCameraControllerTests
    {
        private GameObject camRigObj;
        private BoardCameraController cameraController;
        private Camera mainCamera;
        private Level level1;
        private Level level2;
        private Level level3;
        private Level level7;
        private Level level15;
        private Level level45;

        [SetUp]
        public void SetUp()
        {
            camRigObj = new GameObject("TestCamRig");
            mainCamera = camRigObj.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";

            cameraController = camRigObj.AddComponent<BoardCameraController>();
            cameraController.EnsureReferences();

            level1 = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_001.asset");
            level2 = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_002.asset");
            level3 = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_003.asset");
            level7 = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_007.asset");
            level15 = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_015.asset");
            level45 = AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_045.asset");
        }

        [TearDown]
        public void TearDown()
        {
            if (camRigObj != null)
            {
                Object.DestroyImmediate(camRigObj);
            }
        }

        [Test]
        public void Test01_DefaultProjectionIsOrthographicAndCinemachineEnabled()
        {
            Assert.AreEqual(CameraProjectionMode.Orthographic, cameraController.ProjectionMode);
            Assert.IsTrue(cameraController.UseCinemachine);
        }

        [Test]
        public void Test02_CameraPitchIsNearTopDown()
        {
            Assert.GreaterOrEqual(cameraController.CameraPitch, 88f);
            Assert.LessOrEqual(cameraController.CameraPitch, 90f);
        }

        [Test]
        public void Test03_DynamicFraming_Level1To7To1_CycleParity()
        {
            Assert.IsNotNull(level1, "Level_001 asset not found!");
            Assert.IsNotNull(level7, "Level_007 asset not found!");

            // 1. Frame Level 1 first time
            cameraController.FrameLevel(level1);
            float l1_OrthoSize_1 = cameraController.LastCalculatedOrthoSize;
            Vector3 l1_CamPos_1 = cameraController.LastCalculatedCameraPosition;
            float l1_HOcc_1 = cameraController.LastCalculatedHorizontalOccupancy;

            // 2. Frame Level 7 (significantly larger level)
            cameraController.FrameLevel(level7);
            float l7_OrthoSize = cameraController.LastCalculatedOrthoSize;
            Vector3 l7_CamPos = cameraController.LastCalculatedCameraPosition;

            Assert.Greater(l7_OrthoSize, l1_OrthoSize_1, "Level 7 must require a larger ortho size than Level 1");
            Assert.AreNotEqual(l1_CamPos_1, l7_CamPos, "Level 7 camera position should differ from Level 1");

            // 3. Frame Level 1 second time
            cameraController.FrameLevel(level1);
            float l1_OrthoSize_2 = cameraController.LastCalculatedOrthoSize;
            Vector3 l1_CamPos_2 = cameraController.LastCalculatedCameraPosition;
            float l1_HOcc_2 = cameraController.LastCalculatedHorizontalOccupancy;

            // Verify EXACT match between first and second Level 1 framing
            Assert.AreEqual(l1_OrthoSize_1, l1_OrthoSize_2, 0.0001f, "Second Level 1 load must have identical ortho size to first Level 1 load!");
            Assert.AreEqual(l1_CamPos_1.x, l1_CamPos_2.x, 0.0001f, "Second Level 1 load must have identical camera X pos!");
            Assert.AreEqual(l1_CamPos_1.y, l1_CamPos_2.y, 0.0001f, "Second Level 1 load must have identical camera Y pos!");
            Assert.AreEqual(l1_CamPos_1.z, l1_CamPos_2.z, 0.0001f, "Second Level 1 load must have identical camera Z pos!");
            Assert.AreEqual(l1_HOcc_1, l1_HOcc_2, 0.0001f, "Second Level 1 load must have identical horizontal occupancy!");
        }

        [Test]
        public void Test04_ReferenceLevelsFramingParity_NoClipping()
        {
            Level[] levels = new Level[] { level1, level2, level3, level7, level15, level45 };

            foreach (var lvl in levels)
            {
                if (lvl == null) continue;

                cameraController.FrameLevel(lvl);

                float orthoSize = cameraController.LastCalculatedOrthoSize;
                Vector3 camPos = cameraController.LastCalculatedCameraPosition;
                float hOcc = cameraController.LastCalculatedHorizontalOccupancy;
                float vOcc = cameraController.LastCalculatedVerticalOccupancy;

                Assert.Greater(orthoSize, 0f, $"Level '{lvl.name}' orthoSize must be positive");
                Assert.GreaterOrEqual(hOcc, 70f, $"Level '{lvl.name}' horizontal occupancy ({hOcc:F1}%) is too small");
                Assert.LessOrEqual(hOcc, 95f, $"Level '{lvl.name}' horizontal occupancy ({hOcc:F1}%) is too large (clipping risk)");
                Assert.Greater(vOcc, 0f, $"Level '{lvl.name}' vertical occupancy must be positive");
                Assert.LessOrEqual(vOcc, 95f, $"Level '{lvl.name}' vertical occupancy ({vOcc:F1}%) is too large");

                Debug.Log($"[Camera Test] Level '{lvl.name}': OrthoSize={orthoSize:F3}, CamPos=({camPos.x:F2}, {camPos.y:F2}, {camPos.z:F2}), HOcc={hOcc:F1}%, VOcc={vOcc:F1}%");
            }
        }
    }
}
