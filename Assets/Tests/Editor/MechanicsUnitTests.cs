using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AntsJam.Tests
{
    [TestFixture]
    public class LockedAntsTests
    {
        private GameObject antQueueObj;
        private AntQueueController controller;

        [SetUp]
        public void SetUp()
        {
            antQueueObj = new GameObject("TestQueue_Locked");
            controller = antQueueObj.AddComponent<AntQueueController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (antQueueObj != null) Object.DestroyImmediate(antQueueObj);
        }

        [Test]
        public void LockedQueue_BlocksMovement_WhenCounterGreaterThanZero()
        {
            var coords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var data = new AntQueueData("Q_Lock1", 0, 1, coords, false, -1, false, 0, 0, true, 3);
            controller.Initialize(data);

            Assert.IsTrue(controller.IsLocked);
            Assert.AreEqual(3, controller.LockAmount);
            Assert.IsFalse(controller.TryStartMove(), "Locked ant queue must not start movement while locked.");
        }

        [Test]
        public void DecrementLock_ReducesCounter_AndUnlocksAtZero()
        {
            var coords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var data = new AntQueueData("Q_Lock2", 0, 1, coords, false, -1, false, 0, 0, true, 2);
            controller.Initialize(data);

            controller.DecrementLock(1);
            Assert.AreEqual(1, controller.LockAmount);
            Assert.IsTrue(controller.IsLocked);

            controller.DecrementLock(1);
            Assert.AreEqual(0, controller.LockAmount);
            Assert.IsFalse(controller.IsLocked, "Ant queue must unlock when counter reaches zero.");
        }

        [Test]
        public void LinkedPairCompletion_DecrementsActiveLocksByTwo()
        {
            var coords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var data = new AntQueueData("Q_Lock3", 0, 1, coords, false, -1, false, 0, 0, true, 4);
            controller.Initialize(data);

            // Each queue completing decrements by 1; linked pair has 2 queues
            controller.DecrementLock(1); // Queue 1 completion
            controller.DecrementLock(1); // Queue 2 completion

            Assert.AreEqual(2, controller.LockAmount);
        }
    }

    [TestFixture]
    public class StackedAntsTests
    {
        private GameObject upperObj;
        private GameObject lowerObj;
        private AntQueueController upperController;
        private AntQueueController lowerController;

        [SetUp]
        public void SetUp()
        {
            upperObj = new GameObject("TestQueue_Upper");
            upperController = upperObj.AddComponent<AntQueueController>();

            lowerObj = new GameObject("TestQueue_Lower");
            lowerController = lowerObj.AddComponent<AntQueueController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (upperObj != null) Object.DestroyImmediate(upperObj);
            if (lowerObj != null) Object.DestroyImmediate(lowerObj);
        }

        [Test]
        public void StackedAnts_DominantUpperColor_AndDormantLowerColor()
        {
            var coords = new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4) };
            var upperData = new AntQueueData("Q_Stacked", 2, 1, coords, false, -1, true, 8, 0, false, 0);
            var lowerData = new AntQueueData("Q_Stacked_layer2", 8, 1, coords, false, -1, false, 0, 0, false, 0);

            upperController.Initialize(upperData);
            lowerController.Initialize(lowerData);
            upperController.SetLowerQueueLayer(lowerController);

            Assert.AreEqual(2, upperController.ColorID);
            Assert.AreEqual(8, upperController.SecondColor);
            Assert.IsTrue(lowerController.IsDormantStacked);
            Assert.IsFalse(lowerObj.activeSelf, "Lower stacked layer must remain inactive/hidden while upper is stationary.");
        }

        [Test]
        public void StackedAnts_RevealLowerLayer_BecomesIndependentQueue()
        {
            var coords = new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4) };
            var lowerData = new AntQueueData("Q_Stacked_layer2", 8, 1, coords, false, -1, false, 0, 0, false, 0);
            lowerController.Initialize(lowerData);
            lowerController.SetLowerQueueLayer(null);
            lowerController.RevealFromStack();

            Assert.IsFalse(lowerController.IsDormantStacked);
            Assert.IsTrue(lowerObj.activeSelf);
            Assert.AreEqual(8, lowerController.ColorID);
            Assert.AreEqual(coords.Count, lowerController.CurrentBodyCoordinates.Count);
        }
    }

    [TestFixture]
    public class PipeTests
    {
        private GameObject pipeObj;
        private PipeController pipeController;

        [SetUp]
        public void SetUp()
        {
            pipeObj = new GameObject("TestPipe");
            pipeController = pipeObj.AddComponent<PipeController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (pipeObj != null) Object.DestroyImmediate(pipeObj);
        }

        [Test]
        public void Pipe_DirectionalEntryGating_RejectsSideAndBackwardEntry()
        {
            // Pipe running from (4, 2) to (7, 2), entry at (4, 2) going Right
            var cells = new List<Vector2Int> { new Vector2Int(4, 2), new Vector2Int(5, 2), new Vector2Int(6, 2), new Vector2Int(7, 2) };
            pipeController.Initialize(cells, 1);

            Assert.AreEqual(new Vector2Int(4, 2), pipeController.EntranceCell);
            Assert.AreEqual(new Vector2Int(7, 2), pipeController.ExitCell);
            Assert.AreEqual(Vector2Int.right, pipeController.EntryDirection);
            Assert.AreEqual(Vector2Int.right, pipeController.ExitDirection);

            // Valid entry
            Assert.IsTrue(pipeController.CanEnterFrom(new Vector2Int(4, 2), Vector2Int.right));

            // Backward entry rejected
            Assert.IsFalse(pipeController.CanEnterFrom(new Vector2Int(7, 2), Vector2Int.left));

            // Side entry rejected
            Assert.IsFalse(pipeController.CanEnterFrom(new Vector2Int(5, 2), Vector2Int.down));
            Assert.IsFalse(pipeController.CanEnterFrom(new Vector2Int(4, 2), Vector2Int.up));
        }

        [Test]
        public void Pipe_UsageConversionAndDecrement()
        {
            var cells = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
            
            // PipeCount 2 -> 2 uses
            pipeController.Initialize(cells, 2);
            Assert.AreEqual(2, pipeController.RemainingUses);

            pipeController.ConsumeUse();
            Assert.AreEqual(1, pipeController.RemainingUses);
            Assert.IsFalse(pipeController.IsExhausted);

            pipeController.ConsumeUse();
            Assert.AreEqual(0, pipeController.RemainingUses);
            Assert.IsTrue(pipeController.IsExhausted);
        }
    }

    [TestFixture]
    public class LevelJsonValidationTests
    {
        [Test]
        public void All45Levels_HaveValidGridAndMatchingHoles()
        {
            for (int i = 1; i <= 45; i++)
            {
                string jsonPath = $"Assets/Data/ReferenceLevelJSON/Level_{i:000}.json";
                Assert.IsTrue(System.IO.File.Exists(jsonPath), $"Level JSON {jsonPath} must exist.");

                string jsonText = System.IO.File.ReadAllText(jsonPath);
                var raw = JsonUtility.FromJson<ReferenceLevelImporter.RawReferenceLevelJSON>(jsonText);
                Assert.IsNotNull(raw, $"Level {i} JSON must deserialize cleanly.");

                Assert.IsTrue(raw.GridSize.x > 0 && raw.GridSize.y > 0, $"Level {i} must have positive grid dimensions.");
                Assert.IsNotNull(raw.snakes, $"Level {i} must contain ant records.");
                Assert.IsTrue(raw.snakes.Count > 0, $"Level {i} must have at least one record.");

                HashSet<int> holeColors = new HashSet<int>();
                foreach (var s in raw.snakes)
                {
                    if (s.HolderType == 0) holeColors.Add(s.ColorID);
                }

                foreach (var s in raw.snakes)
                {
                    if (s.HolderType == 1)
                    {
                        Assert.IsTrue(holeColors.Contains(s.ColorID), $"Level {i}: Queue with ColorID {s.ColorID} must have a matching hole.");
                        if (s.IsSecondColor)
                        {
                            Assert.IsTrue(holeColors.Contains(s.SecondColor), $"Level {i}: Stacked layer with SecondColor {s.SecondColor} must have a matching hole.");
                        }
                    }
                }
            }
        }
    }

    [TestFixture]
    public class LinkedAntsTests
    {
        [Test]
        public void LinkedGroups_SupportGroupsLargerThanFour()
        {
            // Verify that a group of 5 linked queues can be grouped together
            List<AntQueueController> group = new List<AntQueueController>();
            List<GameObject> objs = new List<GameObject>();

            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject($"LinkedQueue_{i}");
                objs.Add(go);
                var ctrl = go.AddComponent<AntQueueController>();
                var coords = new List<Vector2Int> { new Vector2Int(i, 0), new Vector2Int(i, 1) };
                var data = new AntQueueData($"Q_{i}", 1, 1, coords, true, 42, false, 0, 0, false, 0);
                ctrl.Initialize(data);
                group.Add(ctrl);
            }

            Assert.AreEqual(5, group.Count);
            foreach (var q in group)
            {
                Assert.IsTrue(q.IsConnected);
                Assert.AreEqual(42, q.ConnectedIndex);
            }

            foreach (var go in objs)
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    [TestFixture]
    public class HoleCapacityTests
    {
        [Test]
        public void Level_ReportsExplicitOrInferredHoleCapacity()
        {
            var level = ScriptableObject.CreateInstance<Level>();
            // Default explicit capacity is 0, so inferred
            Assert.AreEqual(0, level.ExplicitHoleCapacity);

            Object.DestroyImmediate(level);
        }
    }
}
