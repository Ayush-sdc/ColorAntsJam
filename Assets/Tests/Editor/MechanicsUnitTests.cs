using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AntsJam.Tests
{
    // =========================================================================
    // 1. LOCK MECHANICS INTEGRATION TESTS
    // =========================================================================
    [TestFixture]
    public class LockedAntsIntegrationTests
    {
        private GameObject boardObj;
        private BoardManager board;
        private List<GameObject> spawnedObjs;

        [SetUp]
        public void SetUp()
        {
            boardObj = new GameObject("TestBoardManager");
            board = boardObj.AddComponent<BoardManager>();
            board.SetGridDimensions(20, 20);
            BoardManager.SetTestInstance(board);
            spawnedObjs = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (board != null)
            {
                board.ClearBoard();
                BoardManager.SetTestInstance(null);
            }
            foreach (var go in spawnedObjs)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            if (boardObj != null) Object.DestroyImmediate(boardObj);
        }

        private AntQueueController CreateQueue(string id, int colorId, List<Vector2Int> coords, bool isLocked = false, int lockAmount = 0, bool isConnected = false, int connIdx = -1)
        {
            var go = new GameObject($"Queue_{id}");
            spawnedObjs.Add(go);
            var ctrl = go.AddComponent<AntQueueController>();
            var data = new AntQueueData(id, colorId, 1, coords, isConnected, connIdx, false, 0, 0, isLocked, lockAmount);
            ctrl.Initialize(data);
            board.RegisterActiveQueue(ctrl);
            return ctrl;
        }

        private HoleController CreateHole(string id, int colorId, Vector2Int coord, int capacity = 5)
        {
            var go = new GameObject($"Hole_{id}");
            spawnedObjs.Add(go);
            var hole = go.AddComponent<HoleController>();
            hole.Initialize(colorId, coord, capacity);
            board.RegisterActiveHole(hole);
            return hole;
        }

        [Test]
        public void LockedQueue_CannotMove_WhileLocked()
        {
            var hole = CreateHole("H1", 0, new Vector2Int(5, 10), 2);
            var lockedCoords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var lockedQueue = CreateQueue("LockedQ", 0, lockedCoords, isLocked: true, lockAmount: 2);

            Assert.IsTrue(lockedQueue.IsLocked);
            Assert.AreEqual(2, lockedQueue.LockAmount);

            // Real BoardManager move attempt must be rejected
            bool moved = board.TryStartMove(lockedQueue);
            Assert.IsFalse(moved, "Locked queue must not be accepted for movement.");
            Assert.IsFalse(lockedQueue.IsMoving);
        }

        [Test]
        public void LockCounter_DecrementsExactlyOncePerCompletedRescue()
        {
            var hole = CreateHole("H1", 0, new Vector2Int(5, 10), 2);
            var lockedCoords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var lockedQueue = CreateQueue("LockedQ", 0, lockedCoords, isLocked: true, lockAmount: 3);

            var normalCoords = new List<Vector2Int> { new Vector2Int(3, 5), new Vector2Int(3, 4) };
            var normalQueue = CreateQueue("NormalQ", 0, normalCoords);

            Assert.AreEqual(3, lockedQueue.LockAmount);

            // Complete the normal queue via real BoardManager rescue completion
            board.OnQueueCompleted(normalQueue);

            // Lock counter must decrease by exactly 1
            Assert.AreEqual(2, lockedQueue.LockAmount, "Active lock counter must decrement by exactly 1 per completed rescue.");
        }

        [Test]
        public void RescuedLinkedPair_DecrementsCountersByTwo()
        {
            var hole = CreateHole("H1", 0, new Vector2Int(5, 10), 4);
            var lockedCoords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var lockedQueue = CreateQueue("LockedQ", 0, lockedCoords, isLocked: true, lockAmount: 4);

            var pairCoords1 = new List<Vector2Int> { new Vector2Int(1, 5), new Vector2Int(1, 4) };
            var pairCoords2 = new List<Vector2Int> { new Vector2Int(2, 5), new Vector2Int(2, 4) };
            var pairQ1 = CreateQueue("Pair1", 0, pairCoords1, isConnected: true, connIdx: 99);
            var pairQ2 = CreateQueue("Pair2", 0, pairCoords2, isConnected: true, connIdx: 99);

            Assert.AreEqual(4, lockedQueue.LockAmount);

            // Rescuing both members of the linked pair completes two queues
            board.OnQueueCompleted(pairQ1);
            Assert.AreEqual(3, lockedQueue.LockAmount);

            board.OnQueueCompleted(pairQ2);
            Assert.AreEqual(2, lockedQueue.LockAmount, "Rescuing a linked pair (2 queues) must decrement active locks by 2.");
        }

        [Test]
        public void NonRescueEvents_DoNotDecrementLockCounter()
        {
            var hole = CreateHole("H1", 0, new Vector2Int(5, 10), 2);
            var lockedCoords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var lockedQueue = CreateQueue("LockedQ", 0, lockedCoords, isLocked: true, lockAmount: 3);

            // 1. Blocked tap does not decrement lock
            var blockedCoords = new List<Vector2Int> { new Vector2Int(7, 5), new Vector2Int(7, 4) };
            var blockedQueue = CreateQueue("BlockedQ", 1, blockedCoords); // Color 1 has no hole!
            bool moveResult = board.TryStartMove(blockedQueue);
            Assert.IsFalse(moveResult);
            Assert.AreEqual(3, lockedQueue.LockAmount, "Blocked tap must not decrement lock counter.");

            // 2. Stopped early / canceled move does not decrement lock
            var movableCoords = new List<Vector2Int> { new Vector2Int(5, 3), new Vector2Int(5, 2) };
            var movableQueue = CreateQueue("MovableQ", 0, movableCoords);
            board.OnQueueMoveStoppedEarly(movableQueue);
            Assert.AreEqual(3, lockedQueue.LockAmount, "Canceled move must not decrement lock counter.");

            // 3. Duplicate completion callback does not decrement lock counter twice
            board.OnQueueCompleted(movableQueue);
            Assert.AreEqual(2, lockedQueue.LockAmount);

            board.OnQueueCompleted(movableQueue); // Duplicate callback!
            Assert.AreEqual(2, lockedQueue.LockAmount, "Duplicate completion callback must not decrement lock counter.");
        }

        [Test]
        public void Lock_UnlocksAtZero_AndRestoresOnRestart()
        {
            var hole = CreateHole("H1", 0, new Vector2Int(5, 10), 2);
            var lockedCoords = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) };
            var lockedQueue = CreateQueue("LockedQ", 0, lockedCoords, isLocked: true, lockAmount: 1);

            var rescueCoords = new List<Vector2Int> { new Vector2Int(3, 5), new Vector2Int(3, 4) };
            var rescueQueue = CreateQueue("RescueQ", 0, rescueCoords);

            Assert.IsTrue(lockedQueue.IsLocked);

            // Rescue completes
            board.OnQueueCompleted(rescueQueue);
            Assert.AreEqual(0, lockedQueue.LockAmount);
            Assert.IsFalse(lockedQueue.IsLocked, "Queue must unlock when counter reaches zero.");

            // On level restart/re-initialization, queue restores original lock counter
            var originalData = new AntQueueData("LockedQ", 0, 1, lockedCoords, false, -1, false, 0, 0, true, 1);
            lockedQueue.Initialize(originalData);
            Assert.IsTrue(lockedQueue.IsLocked);
            Assert.AreEqual(1, lockedQueue.LockAmount, "Original lock counter must restore on level restart.");
        }
    }

    // =========================================================================
    // 2. LINKED QUEUES INTEGRATION TESTS
    // =========================================================================
    [TestFixture]
    public class LinkedAntsIntegrationTests
    {
        private GameObject boardObj;
        private BoardManager board;
        private List<GameObject> spawnedObjs;

        [SetUp]
        public void SetUp()
        {
            boardObj = new GameObject("TestBoardManager");
            board = boardObj.AddComponent<BoardManager>();
            board.SetGridDimensions(20, 20);
            BoardManager.SetTestInstance(board);
            spawnedObjs = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (board != null)
            {
                board.ClearBoard();
                BoardManager.SetTestInstance(null);
            }
            foreach (var go in spawnedObjs)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            if (boardObj != null) Object.DestroyImmediate(boardObj);
        }

        private HoleController CreateHole(string id, int colorId, Vector2Int coord, int capacity = 10)
        {
            var go = new GameObject($"Hole_{id}");
            spawnedObjs.Add(go);
            var hole = go.AddComponent<HoleController>();
            hole.Initialize(colorId, coord, capacity);
            board.RegisterActiveHole(hole);
            return hole;
        }

        private List<AntQueueController> CreateLinkedGroup(int size, int groupId, int colorId, int startX, int startY, int holeY)
        {
            List<AntQueueController> group = new List<AntQueueController>();
            for (int i = 0; i < size; i++)
            {
                int x = startX + (i * 2);
                CreateHole($"Hole_{groupId}_{i}", colorId, new Vector2Int(x, holeY), 1);

                var go = new GameObject($"LinkedQ_{groupId}_{i}");
                spawnedObjs.Add(go);
                var ctrl = go.AddComponent<AntQueueController>();
                var coords = new List<Vector2Int> { new Vector2Int(x, startY), new Vector2Int(x, startY - 1) };
                var data = new AntQueueData($"Q_{groupId}_{i}", colorId, 1, coords, true, groupId, false, 0, 0, false, 0);
                ctrl.Initialize(data);
                board.RegisterActiveQueue(ctrl);
                group.Add(ctrl);
            }
            return group;
        }

        [TestCase(2)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void LinkedGroups_ExecuteRealSimultaneousMovement_Groups2_5_6_7(int groupSize)
        {
            int colorId = 0;
            var group = CreateLinkedGroup(groupSize, 100 + groupSize, colorId, startX: 1, startY: 4, holeY: 10);

            Assert.AreEqual(groupSize, group.Count);

            // Execute real movement through BoardManager
            bool moveStarted = board.TryStartMove(group[0]);
            Assert.IsTrue(moveStarted, $"Linked group of size {groupSize} must start movement successfully.");

            // Verify simultaneous movement across all members
            foreach (var member in group)
            {
                Assert.IsTrue(member.IsMoving, $"Member {member.QueueId} in group of size {groupSize} must be moving simultaneously.");
            }

            // Verify all members complete and clean up
            foreach (var member in group)
            {
                board.OnQueueCompleted(member);
            }
            Assert.AreEqual(0, board.ActiveMovingQueuesCount);
        }

        [Test]
        public void LinkedGroup_AtomicBlock_StaysStationaryWithoutReservations()
        {
            // Group of 5 queues: Member 3 is blocked by an obstacle queue of wrong color
            int groupSize = 5;
            int colorId = 0;
            var group = CreateLinkedGroup(groupSize, 200, colorId, startX: 1, startY: 4, holeY: 10);

            // Place blocking obstacle in member 2's path (x=5, y=7)
            var obstGo = new GameObject("ObstacleQueue");
            spawnedObjs.Add(obstGo);
            var obst = obstGo.AddComponent<AntQueueController>();
            var obstCoords = new List<Vector2Int> { new Vector2Int(5, 7), new Vector2Int(5, 6) };
            var obstData = new AntQueueData("Obstacle", 1, 1, obstCoords, false, -1, false, 0, 0, false, 0);
            obst.Initialize(obstData);
            board.RegisterActiveQueue(obst);

            // Try to move linked group
            bool moveStarted = board.TryStartMove(group[0]);
            Assert.IsFalse(moveStarted, "Linked group must NOT start movement if any member is blocked.");

            // Entire group must stay stationary
            foreach (var member in group)
            {
                Assert.IsFalse(member.IsMoving, $"Member {member.QueueId} must remain stationary.");
            }

            // Zero route cells or hole slots reserved
            Assert.AreEqual(0, board.ReservedRouteCells.Count, "No route cells should be reserved after blocked attempt.");
            foreach (var hole in board.ActiveHoles)
            {
                Assert.AreEqual(0, hole.InboundReservations, "No hole reservations should remain after blocked attempt.");
            }
        }

        [Test]
        public void LinkedGroup_SimultaneousMovement_UnequalQueueAndRouteLengths()
        {
            int colorId = 0;
            CreateHole("HoleA", colorId, new Vector2Int(2, 8), 1);
            CreateHole("HoleB", colorId, new Vector2Int(6, 12), 1); // Longer route

            // Member 1: length 2, route to y=8
            var go1 = new GameObject("Q_Unequal_1");
            spawnedObjs.Add(go1);
            var q1 = go1.AddComponent<AntQueueController>();
            var coords1 = new List<Vector2Int> { new Vector2Int(2, 4), new Vector2Int(2, 3) };
            q1.Initialize(new AntQueueData("Q1", colorId, 1, coords1, true, 300, false, 0, 0, false, 0));
            board.RegisterActiveQueue(q1);

            // Member 2: length 4, route to y=12
            var go2 = new GameObject("Q_Unequal_2");
            spawnedObjs.Add(go2);
            var q2 = go2.AddComponent<AntQueueController>();
            var coords2 = new List<Vector2Int> { new Vector2Int(6, 4), new Vector2Int(6, 3), new Vector2Int(6, 2), new Vector2Int(6, 1) };
            q2.Initialize(new AntQueueData("Q2", colorId, 1, coords2, true, 300, false, 0, 0, false, 0));
            board.RegisterActiveQueue(q2);

            bool moveStarted = board.TryStartMove(q1);
            Assert.IsTrue(moveStarted, "Linked group with unequal queue and route lengths must start movement.");
            Assert.IsTrue(q1.IsMoving);
            Assert.IsTrue(q2.IsMoving);

            board.OnQueueCompleted(q1);
            board.OnQueueCompleted(q2);
            Assert.AreEqual(0, board.ActiveMovingQueuesCount);
        }

        [Test]
        public void LinkedGroup_CleanupOnRestartAndInterruption()
        {
            var group = CreateLinkedGroup(3, 400, 0, startX: 1, startY: 4, holeY: 10);
            board.TryStartMove(group[0]);
            Assert.IsTrue(group[0].IsMoving);

            // Interrupt and clear board (simulating restart)
            board.ClearBoard();

            Assert.AreEqual(0, board.ActiveMovingQueuesCount);
            Assert.AreEqual(0, board.ReservedRouteCells.Count);
            Assert.AreEqual(0, board.ActiveQueues.Count);
            Assert.AreEqual(0, board.ActiveHoles.Count);
        }
    }

    // =========================================================================
    // 3. STACKED QUEUES INTEGRATION TESTS
    // =========================================================================
    [TestFixture]
    public class StackedAntsIntegrationTests
    {
        private GameObject boardObj;
        private BoardManager board;
        private List<GameObject> spawnedObjs;

        [SetUp]
        public void SetUp()
        {
            boardObj = new GameObject("TestBoardManager");
            board = boardObj.AddComponent<BoardManager>();
            board.SetGridDimensions(20, 20);
            BoardManager.SetTestInstance(board);
            spawnedObjs = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (board != null)
            {
                board.ClearBoard();
                BoardManager.SetTestInstance(null);
            }
            foreach (var go in spawnedObjs)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            if (boardObj != null) Object.DestroyImmediate(boardObj);
        }

        private HoleController CreateHole(string id, int colorId, Vector2Int coord, int capacity = 1)
        {
            var go = new GameObject($"Hole_{id}");
            spawnedObjs.Add(go);
            var hole = go.AddComponent<HoleController>();
            hole.Initialize(colorId, coord, capacity);
            board.RegisterActiveHole(hole);
            return hole;
        }

        private (AntQueueController upper, AntQueueController lower) CreateStackedPair(List<Vector2Int> coords, int upperColor, int lowerColor)
        {
            var upperGo = new GameObject("UpperQueue");
            spawnedObjs.Add(upperGo);
            var upper = upperGo.AddComponent<AntQueueController>();

            var lowerGo = new GameObject("LowerQueue");
            spawnedObjs.Add(lowerGo);
            var lower = lowerGo.AddComponent<AntQueueController>();

            var upperData = new AntQueueData("UpperQ", upperColor, 1, coords, false, -1, true, lowerColor, 0, false, 0);
            var lowerData = new AntQueueData("LowerQ", lowerColor, 1, coords, false, -1, false, 0, 0, false, 0);

            upper.Initialize(upperData);
            lower.Initialize(lowerData);
            upper.SetLowerQueueLayer(lower);

            board.RegisterActiveQueue(upper);
            return (upper, lower);
        }

        [Test]
        public void StackedQueues_BlockedAttempt_DoesNotRevealLowerLayer()
        {
            var coords = new List<Vector2Int> { new Vector2Int(3, 4), new Vector2Int(3, 3) };
            var (upper, lower) = CreateStackedPair(coords, upperColor: 2, lowerColor: 8);

            // Obstacle blocks upper queue (no matching hole for upperColor 2)
            bool moved = board.TryStartMove(upper);
            Assert.IsFalse(moved, "Blocked upper move must return false.");
            Assert.IsFalse(upper.IsMoving);

            // Lower layer must remain dormant and inactive
            Assert.IsTrue(lower.IsDormantStacked, "Lower layer must remain dormant when upper attempt is blocked.");
            Assert.IsFalse(lower.gameObject.activeSelf, "Lower layer GameObject must remain inactive.");
        }

        [Test]
        public void StackedQueues_AcceptedUpperMove_RevealsLowerLayerAtOriginalFootprint()
        {
            var coords = new List<Vector2Int> { new Vector2Int(3, 4), new Vector2Int(3, 3) };
            CreateHole("UpperHole", 2, new Vector2Int(3, 8), 1);
            var (upper, lower) = CreateStackedPair(coords, upperColor: 2, lowerColor: 8);

            Assert.IsTrue(lower.IsDormantStacked);
            Assert.IsFalse(lower.gameObject.activeSelf);

            // Accepted move
            bool moved = board.TryStartMove(upper);
            Assert.IsTrue(moved, "Valid upper move must be accepted.");
            Assert.IsTrue(upper.IsMoving);

            // Lower layer revealed at original footprint
            Assert.IsFalse(lower.IsDormantStacked, "Lower layer must no longer be dormant after accepted upper move.");
            Assert.IsTrue(lower.gameObject.activeSelf, "Lower layer GameObject must be active.");
            Assert.AreEqual(coords.Count, lower.CurrentBodyCoordinates.Count);
            for (int i = 0; i < coords.Count; i++)
            {
                Assert.AreEqual(coords[i], lower.CurrentBodyCoordinates[i], "Lower layer coordinates must match original footprint.");
            }
        }

        [Test]
        public void StackedQueues_LowerLayer_CannotMoveThroughDepartingUpperLayer()
        {
            var coords = new List<Vector2Int> { new Vector2Int(3, 4), new Vector2Int(3, 3) };
            CreateHole("UpperHole", 2, new Vector2Int(3, 8), 1);
            CreateHole("LowerHole", 8, new Vector2Int(3, 10), 1); // Same corridor!
            var (upper, lower) = CreateStackedPair(coords, upperColor: 2, lowerColor: 8);

            board.TryStartMove(upper);
            Assert.IsTrue(upper.IsMoving);

            // While upper is departing, lower layer cannot move through departing upper layer's corridor
            bool lowerMoved = board.TryStartMove(lower);
            Assert.IsFalse(lowerMoved, "Lower layer cannot move through the departing upper layer.");
        }

        [Test]
        public void StackedQueues_RescueEachLayerIndependently_ColorCapacityAndLocks()
        {
            var coords = new List<Vector2Int> { new Vector2Int(3, 4), new Vector2Int(3, 3) };
            var upperHole = CreateHole("UpperHole", 2, new Vector2Int(3, 8), 1);
            var lowerHole = CreateHole("LowerHole", 8, new Vector2Int(3, 10), 1);

            // Active lock that requires 2 rescues to unlock
            var lockGo = new GameObject("LockedQ");
            spawnedObjs.Add(lockGo);
            var lockedQ = lockGo.AddComponent<AntQueueController>();
            lockedQ.Initialize(new AntQueueData("Lock", 0, 1, new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) }, false, -1, false, 0, 0, true, 2));
            board.RegisterActiveQueue(lockedQ);

            var (upper, lower) = CreateStackedPair(coords, upperColor: 2, lowerColor: 8);

            Assert.AreEqual(2, lockedQ.LockAmount);

            // Rescue upper layer
            board.TryStartMove(upper);
            board.OnQueueCompleted(upper);

            // Lock counter decremented to 1
            Assert.AreEqual(1, lockedQ.LockAmount);
            Assert.IsTrue(lockedQ.IsLocked);

            // Rescue lower layer
            board.TryStartMove(lower);
            board.OnQueueCompleted(lower);

            // Lock counter decremented to 0 and unlocked!
            Assert.AreEqual(0, lockedQ.LockAmount);
            Assert.IsFalse(lockedQ.IsLocked, "Lock must unlock when both stacked layers are independently rescued.");
        }
    }

    // =========================================================================
    // 4. PIPES INTEGRATION TESTS
    // =========================================================================
    [TestFixture]
    public class PipeIntegrationTests
    {
        private GameObject boardObj;
        private BoardManager board;
        private List<GameObject> spawnedObjs;

        [SetUp]
        public void SetUp()
        {
            boardObj = new GameObject("TestBoardManager");
            board = boardObj.AddComponent<BoardManager>();
            board.SetGridDimensions(20, 20);
            BoardManager.SetTestInstance(board);
            spawnedObjs = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (board != null)
            {
                board.ClearBoard();
                BoardManager.SetTestInstance(null);
            }
            foreach (var go in spawnedObjs)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            if (boardObj != null) Object.DestroyImmediate(boardObj);
        }

        private PipeController CreatePipe(List<Vector2Int> cells, int uses)
        {
            var go = new GameObject("Pipe");
            spawnedObjs.Add(go);
            var pipe = go.AddComponent<PipeController>();
            pipe.Initialize(cells, uses);
            board.RegisterActivePipe(pipe);
            return pipe;
        }

        private HoleController CreateHole(string id, int colorId, Vector2Int coord, int capacity = 5)
        {
            var go = new GameObject($"Hole_{id}");
            spawnedObjs.Add(go);
            var hole = go.AddComponent<HoleController>();
            hole.Initialize(colorId, coord, capacity);
            board.RegisterActiveHole(hole);
            return hole;
        }

        private AntQueueController CreateQueue(string id, int colorId, List<Vector2Int> coords, bool isConnected = false, int connIdx = -1)
        {
            var go = new GameObject($"Queue_{id}");
            spawnedObjs.Add(go);
            var ctrl = go.AddComponent<AntQueueController>();
            var data = new AntQueueData(id, colorId, 1, coords, isConnected, connIdx, false, 0, 0, false, 0);
            ctrl.Initialize(data);
            board.RegisterActiveQueue(ctrl);
            return ctrl;
        }

        [Test]
        public void Pipe_StraightAndBentPipe_PathGenerationAndFollowers()
        {
            // Straight pipe: (3, 2) -> (5, 2) heading Right
            var straightCells = new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(4, 2), new Vector2Int(5, 2) };
            var straightPipe = CreatePipe(straightCells, 1);
            Assert.AreEqual(new Vector2Int(3, 2), straightPipe.EntranceCell);
            Assert.AreEqual(new Vector2Int(5, 2), straightPipe.ExitCell);
            Assert.AreEqual(Vector2Int.right, straightPipe.EntryDirection);
            Assert.AreEqual(Vector2Int.right, straightPipe.ExitDirection);

            // Bent pipe (L-shape): (2, 4) -> (2, 6) -> (4, 6)
            var bentCells = new List<Vector2Int> { new Vector2Int(2, 4), new Vector2Int(2, 5), new Vector2Int(2, 6), new Vector2Int(3, 6), new Vector2Int(4, 6) };
            var bentPipe = CreatePipe(bentCells, 1);
            Assert.AreEqual(new Vector2Int(2, 4), bentPipe.EntranceCell);
            Assert.AreEqual(new Vector2Int(4, 6), bentPipe.ExitCell);
            Assert.AreEqual(Vector2Int.up, bentPipe.EntryDirection);
            Assert.AreEqual(Vector2Int.right, bentPipe.ExitDirection);
        }

        [Test]
        public void Pipe_DirectionalEntryRules_RejectsSideAndBackwardEntry()
        {
            // Reference assumption note: PipeCount conversion and entry gating are documented assumptions.
            var cells = new List<Vector2Int> { new Vector2Int(4, 2), new Vector2Int(5, 2), new Vector2Int(6, 2) };
            var pipe = CreatePipe(cells, 1);

            // Legitimate entry at entrance cell heading Right
            Assert.IsTrue(pipe.CanEnterFrom(new Vector2Int(4, 2), Vector2Int.right));

            // Backward entry rejected
            Assert.IsFalse(pipe.CanEnterFrom(new Vector2Int(6, 2), Vector2Int.left));

            // Side entry rejected
            Assert.IsFalse(pipe.CanEnterFrom(new Vector2Int(5, 2), Vector2Int.up));
            Assert.IsFalse(pipe.CanEnterFrom(new Vector2Int(4, 2), Vector2Int.down));
        }

        [Test]
        public void Pipe_PreventsConcurrentQueues_ExceedingRemainingUses()
        {
            // Pipe with only 1 use remaining
            var pipeCells = new List<Vector2Int> { new Vector2Int(4, 2), new Vector2Int(5, 2) };
            var pipe = CreatePipe(pipeCells, uses: 1);

            CreateHole("Hole", 0, new Vector2Int(7, 2), 2);

            // Two queues in a linked group both wanting to enter this pipe in the same tap
            var q1 = CreateQueue("Q1", 0, new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(2, 2) }, isConnected: true, connIdx: 50);
            var q2 = CreateQueue("Q2", 0, new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(2, 2) }, isConnected: true, connIdx: 50);

            // Validation must prevent 2 concurrent queues from exceeding the 1 remaining use!
            var group = new List<AntQueueController> { q1, q2 };
            var pendingHoles = new Dictionary<HoleController, int>();
            var pendingPipes = new Dictionary<PipeController, int>();

            bool v1 = board.ValidateForwardMove(q1, group, pendingHoles, out _, out _, out _, out _, out _, out _, pendingPipes);
            Assert.IsTrue(v1);
            pendingPipes[pipe] = 1;

            bool v2 = board.ValidateForwardMove(q2, group, pendingHoles, out _, out _, out _, out var reason, out _, out _, pendingPipes);
            Assert.IsFalse(v2, "Second queue cannot reserve pipe when only 1 use is remaining.");
            Assert.AreEqual("BlockedByPipeObstacle", reason);
        }

        [Test]
        public void Pipe_ReservationsConsumptionAndExhaustion()
        {
            var pipeCells = new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(4, 2) };
            var pipe = CreatePipe(pipeCells, uses: 2);

            Assert.AreEqual(2, pipe.RemainingUses);
            Assert.IsFalse(pipe.IsExhausted);

            // Traverse once
            pipe.ConsumeUse();
            Assert.AreEqual(1, pipe.RemainingUses);
            Assert.IsFalse(pipe.IsExhausted);

            // Traverse second time -> exhausted
            pipe.ConsumeUse();
            Assert.AreEqual(0, pipe.RemainingUses);
            Assert.IsTrue(pipe.IsExhausted);
        }
    }

    // =========================================================================
    // 5. HOLES AND LEVEL DATA INTEGRATION TESTS
    // =========================================================================
    [TestFixture]
    public class HoleCapacityAndLevelDataTests
    {
        [Test]
        public void HoleCapacity_ColorGroupedAllocation_SatisfiesAllQueuesIncludingStacked()
        {
            // Level 15 has 10 queues (4 stacked = 14 required rescues) and 4 holes
            string jsonPath = "Assets/Data/ReferenceLevelJSON/Level_015.json";
            Assert.IsTrue(File.Exists(jsonPath));

            string jsonText = File.ReadAllText(jsonPath);
            var raw = JsonUtility.FromJson<ReferenceLevelImporter.RawReferenceLevelJSON>(jsonText);

            var level = ScriptableObject.CreateInstance<Level>();
            List<AntQueueData> queues = new List<AntQueueData>();
            foreach (var s in raw.snakes)
            {
                queues.Add(new AntQueueData("Q", s.ColorID, s.HolderType, s.Cells, s.IsConnected, s.ConnectedIndex, s.IsSecondColor, s.SecondColor, s.PipeCount, s.IsLocked, s.LockAmount));
            }
            level.SetReferenceLevelData(15, "Level_015", "0", raw.Duration, raw.GridSize, raw.MaxCapacity, raw.DisabledGridCells, null, queues);

            var holes = ReferenceLevelRuntimeAdapter.ExtractHoles(level);
            Assert.IsTrue(holes.Count > 0);

            // Verify capacity per color satisfies all expected queues
            var requiredPerColor = new Dictionary<int, int>();
            foreach (var q in queues)
            {
                if (q.HolderType == 1)
                {
                    requiredPerColor[q.ColorID] = requiredPerColor.GetValueOrDefault(q.ColorID, 0) + 1;
                    if (q.IsSecondColor)
                    {
                        requiredPerColor[q.SecondColor] = requiredPerColor.GetValueOrDefault(q.SecondColor, 0) + 1;
                    }
                }
            }

            var capacityPerColor = new Dictionary<int, int>();
            foreach (var h in holes)
            {
                capacityPerColor[h.ColorID] = capacityPerColor.GetValueOrDefault(h.ColorID, 0) + h.Capacity;
            }

            foreach (var kvp in requiredPerColor)
            {
                int colorId = kvp.Key;
                int required = kvp.Value;
                int available = capacityPerColor.GetValueOrDefault(colorId, 0);
                Assert.GreaterOrEqual(available, required, $"ColorID {colorId} must have capacity >= required queues.");
            }

            Object.DestroyImmediate(level);
        }

        [Test]
        public void JsonRoundTrip_PreservesAllMechanicsFieldsAndCellOrder()
        {
            // Verify round-trip preserves compatibility keys ('snakes'), ordered cells, locks, pipes, stacks, connections
            var originalRaw = new ReferenceLevelImporter.RawReferenceLevelJSON
            {
                GridSize = new Vector2Int(10, 10),
                Duration = 120f,
                LevelDifficulty = 1,
                MaxCapacity = 4,
                DisabledGridCells = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 1) },
                snakes = new List<ReferenceLevelImporter.RawSnakeJSON>
                {
                    new ReferenceLevelImporter.RawSnakeJSON
                    {
                        HolderType = 1,
                        ColorID = 5,
                        Cells = new List<Vector2Int> { new Vector2Int(2, 3), new Vector2Int(2, 4), new Vector2Int(2, 5) },
                        IsConnected = true,
                        ConnectedIndex = 77,
                        IsSecondColor = true,
                        SecondColor = 9,
                        PipeCount = 0,
                        IsLocked = true,
                        LockAmount = 3
                    },
                    new ReferenceLevelImporter.RawSnakeJSON
                    {
                        HolderType = 2,
                        ColorID = 0,
                        Cells = new List<Vector2Int> { new Vector2Int(4, 2), new Vector2Int(5, 2), new Vector2Int(6, 2) },
                        PipeCount = 2
                    }
                }
            };

            string exportedJson = JsonUtility.ToJson(originalRaw, true);
            Assert.IsTrue(exportedJson.Contains("\"snakes\""), "JSON must preserve reference compatibility key 'snakes'.");

            var importedRaw = JsonUtility.FromJson<ReferenceLevelImporter.RawReferenceLevelJSON>(exportedJson);
            Assert.AreEqual(originalRaw.GridSize, importedRaw.GridSize);
            Assert.AreEqual(originalRaw.Duration, importedRaw.Duration);
            Assert.AreEqual(originalRaw.LevelDifficulty, importedRaw.LevelDifficulty);
            Assert.AreEqual(originalRaw.snakes.Count, importedRaw.snakes.Count);

            var s0 = importedRaw.snakes[0];
            Assert.AreEqual(5, s0.ColorID);
            Assert.AreEqual(1, s0.HolderType);
            Assert.IsTrue(s0.IsConnected);
            Assert.AreEqual(77, s0.ConnectedIndex);
            Assert.IsTrue(s0.IsSecondColor);
            Assert.AreEqual(9, s0.SecondColor);
            Assert.IsTrue(s0.IsLocked);
            Assert.AreEqual(3, s0.LockAmount);
            Assert.AreEqual(3, s0.Cells.Count);
            Assert.AreEqual(new Vector2Int(2, 3), s0.Cells[0]);
            Assert.AreEqual(new Vector2Int(2, 4), s0.Cells[1]);
            Assert.AreEqual(new Vector2Int(2, 5), s0.Cells[2]);

            var s1 = importedRaw.snakes[1];
            Assert.AreEqual(2, s1.HolderType);
            Assert.AreEqual(2, s1.PipeCount);
        }

        [Test]
        public void LevelMetadata_HardLevelOverlays_DoNotAppearOnNormalLevels()
        {
            var normalLevel = ScriptableObject.CreateInstance<Level>();
            normalLevel.SetReferenceLevelData(1, "Level 1", "0", 60f, new Vector2Int(10, 10), 4, null, null, null, difficultyInt: 0);

            Assert.IsFalse(normalLevel.ShowHardIntro, "Normal levels (difficulty 0) must NOT show hard level intro.");

            var hardLevel = ScriptableObject.CreateInstance<Level>();
            hardLevel.SetReferenceLevelData(50, "Level 50", "2", 60f, new Vector2Int(10, 10), 4, null, null, null, difficultyInt: 2);

            Assert.IsTrue(hardLevel.ShowHardIntro, "Hard levels (difficulty >= 2) must show hard level intro.");

            Object.DestroyImmediate(normalLevel);
            Object.DestroyImmediate(hardLevel);
        }
    }
}
