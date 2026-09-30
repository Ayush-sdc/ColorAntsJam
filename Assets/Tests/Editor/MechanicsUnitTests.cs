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
        public void StackedQueues_LowerLayer_CanMoveThroughCorridorWhileUpperIsDeparting()
        {
            var coords = new List<Vector2Int> { new Vector2Int(3, 4), new Vector2Int(3, 3) };
            CreateHole("UpperHole", 2, new Vector2Int(3, 8), 1);
            CreateHole("LowerHole", 8, new Vector2Int(3, 10), 1); // Same corridor!
            var (upper, lower) = CreateStackedPair(coords, upperColor: 2, lowerColor: 8);

            board.TryStartMove(upper);
            Assert.IsTrue(upper.IsMoving);

            // Confirmed gameplay: Once moving, queues do not block other moving queues.
            // Lower layer CAN start moving along the same corridor while upper is departing.
            bool lowerMoved = board.TryStartMove(lower);
            Assert.IsTrue(lowerMoved, "Lower layer can legally move through the same corridor while upper is moving.");
            Assert.IsTrue(lower.IsMoving);
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

    // =========================================================================
    // 6. STRAIGHT MOVEMENT & LANE PRESERVATION INTEGRATION TESTS
    // =========================================================================
    [TestFixture]
    public class StraightMovementAndLanesIntegrationTests
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

        private AntQueueController CreateQueue(string id, int colorId, List<Vector2Int> coords)
        {
            var go = new GameObject($"Queue_{id}");
            spawnedObjs.Add(go);
            var ctrl = go.AddComponent<AntQueueController>();
            var data = new AntQueueData(id, colorId, 1, coords, false, 0, false, 0, 0, false, 0);
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
        public void StraightApproach_FromAllFourDirections_HasZeroSidewaysDisplacement()
        {
            // Test North, South, East, West
            var directions = new (Vector2Int dir, Vector2Int head, Vector2Int neck, Vector2Int holePos)[]
            {
                (new Vector2Int(0, 1),  new Vector2Int(5, 5), new Vector2Int(5, 4), new Vector2Int(5, 10)), // North
                (new Vector2Int(0, -1), new Vector2Int(5, 10), new Vector2Int(5, 11), new Vector2Int(5, 5)), // South
                (new Vector2Int(1, 0),  new Vector2Int(5, 5), new Vector2Int(4, 5), new Vector2Int(10, 5)), // East
                (new Vector2Int(-1, 0), new Vector2Int(10, 5), new Vector2Int(11, 5), new Vector2Int(5, 5))  // West
            };

            for (int i = 0; i < directions.Length; i++)
            {
                var d = directions[i];
                var hole = CreateHole($"H_{i}", 0, d.holePos, 2);
                var queue = CreateQueue($"Q_{i}", 0, new List<Vector2Int> { d.head, d.neck });

                var route = board.GenerateStraightFreeWorldPath(queue, d.dir, hole, d.holePos);
                Assert.IsNotNull(route);
                Assert.Greater(route.Count, 0, $"Route in direction {d.dir} must contain waypoints.");

                Vector3 headWorldPos = board.GridToWorldPosition(d.head);
                Vector3 entryForward = new Vector3(d.dir.x, 0f, d.dir.y).normalized;
                Vector3 entryRight = Vector3.Cross(Vector3.up, entryForward).normalized;

                // Every single point in the route must have ZERO lateral displacement
                foreach (var pt in route)
                {
                    float lateralDisplacement = Mathf.Abs(Vector3.Dot(pt - headWorldPos, entryRight));
                    Assert.Less(lateralDisplacement, 0.0001f, 
                        $"Route in direction {d.dir} at pt {pt} must have ZERO sideways steering (lateral delta={lateralDisplacement}).");
                }
            }
        }

        [Test]
        public void LinkedQueues_PreserveParallelLanes_WithoutCrossingOrLateralShift()
        {
            // Two queues moving side-by-side North in parallel columns (x=4 and x=5)
            var holeA = CreateHole("H_A", 0, new Vector2Int(4, 10), 2);
            var holeB = CreateHole("H_B", 0, new Vector2Int(5, 10), 2);

            var queueA = CreateQueue("Q_A", 0, new List<Vector2Int> { new Vector2Int(4, 5), new Vector2Int(4, 4) });
            var queueB = CreateQueue("Q_B", 0, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });

            Vector2Int fwd = new Vector2Int(0, 1);
            var routeA = board.GenerateStraightFreeWorldPath(queueA, fwd, holeA, new Vector2Int(4, 10));
            var routeB = board.GenerateStraightFreeWorldPath(queueB, fwd, holeB, new Vector2Int(5, 10));

            Vector3 right = Vector3.right;
            Vector3 headA = board.GridToWorldPosition(new Vector2Int(4, 5));
            Vector3 headB = board.GridToWorldPosition(new Vector2Int(5, 5));

            for (int i = 0; i < Mathf.Min(routeA.Count, routeB.Count); i++)
            {
                float latA = Mathf.Abs(Vector3.Dot(routeA[i] - headA, right));
                float latB = Mathf.Abs(Vector3.Dot(routeB[i] - headB, right));

                Assert.Less(latA, 0.0001f, "Queue A must have zero sideways steering.");
                Assert.Less(latB, 0.0001f, "Queue B must have zero sideways steering.");

                float laneDistance = Mathf.Abs(routeB[i].x - routeA[i].x);
                Assert.AreEqual(board.CellSize, laneDistance, 0.001f, "Parallel lanes must remain exactly 1 cellSize apart.");
            }
        }

        [Test]
        public void Pipes_RedirectQueueCorrectly_WithMechanicallyStraightSegments()
        {
            // Queue at (3, 5) facing East (1, 0)
            // Pipe entrance at (5, 5), exit at (6, 7) facing North (0, 1)
            // Hole at (6, 9)
            var pipeObj = new GameObject("TestPipe");
            spawnedObjs.Add(pipeObj);
            var pipe = pipeObj.AddComponent<PipeController>();
            var pipeCells = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(6, 6), new Vector2Int(6, 7) };
            pipe.Initialize(pipeCells, 3);
            board.RegisterActivePipe(pipe);

            var hole = CreateHole("H_Pipe", 0, new Vector2Int(6, 9), 2);
            var queue = CreateQueue("Q_Pipe", 0, new List<Vector2Int> { new Vector2Int(3, 5), new Vector2Int(2, 5) });

            var route = board.GeneratePipeFreeWorldPath(queue, new Vector2Int(1, 0), pipe, hole, new Vector2Int(6, 9));
            Assert.IsNotNull(route);
            Assert.Greater(route.Count, 0);

            // First waypoint is headWorldPos
            Vector3 headPos = board.GridToWorldPosition(new Vector2Int(3, 5));
            Assert.AreEqual(headPos.x, route[0].x, 0.01f);
            Assert.AreEqual(headPos.z, route[0].z, 0.01f);

            // Final waypoint descends into pit at column x=6
            Vector3 holePos = board.GridToWorldPosition(new Vector2Int(6, 9));
            Vector3 lastPt = route[route.Count - 1];
            Assert.AreEqual(holePos.x, lastPt.x, 0.01f, "Final despawn point must be aligned with column 6.");
            Assert.Less(lastPt.y, headPos.y, "Final despawn point must descend in Y into pit.");
        }
    }

    // =========================================================================
    // 7. WHITE HOLE READABILITY TESTS
    // =========================================================================
    [TestFixture]
    public class WhiteHoleReadabilityTests
    {
        private GameObject testObj;
        private HoleController hole;

        [SetUp]
        public void SetUp()
        {
            testObj = new GameObject("TestWhiteHole");
            hole = testObj.AddComponent<HoleController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObj != null) Object.DestroyImmediate(testObj);
        }

        [Test]
        public void WhiteHole_UsesContrastingDarkText_SingleAndTwoDigit()
        {
            // Initialize hole as White
            hole.Initialize(AntColor.White, new Vector2Int(5, 5), 8);

            Color textColor = hole.GetBadgeTextColor();

            // Assert contrasting dark text (luminance < 0.25)
            Assert.Less(textColor.r, 0.25f, "White hole counter text must be dark for contrast.");
            Assert.Less(textColor.g, 0.25f, "White hole counter text must be dark for contrast.");
            Assert.Less(textColor.b, 0.25f, "White hole counter text must be dark for contrast.");

            // Test two-digit counter (12)
            hole.Initialize(AntColor.White, new Vector2Int(5, 5), 12);
            Color twoDigitColor = hole.GetBadgeTextColor();
            Assert.Less(twoDigitColor.r, 0.25f, "Two-digit counter on white hole must remain dark.");

            // Compare with non-white hole (Red)
            var redObj = new GameObject("TestRedHole");
            var redHole = redObj.AddComponent<HoleController>();
            redHole.Initialize(AntColor.Red, new Vector2Int(3, 3), 5);

            Color redTextColor = redHole.GetBadgeTextColor();
            Assert.AreEqual(Color.white, redTextColor, "Red hole counter text should remain white.");

            Object.DestroyImmediate(redObj);
        }
    }

    // =========================================================================
    // 8. LEVEL 6 REGRESSION INTEGRATION TESTS
    // =========================================================================
    [TestFixture]
    public class Level6RegressionIntegrationTests
    {
        private GameObject boardObj;
        private BoardManager board;
        private GameObject gameManagerObj;
        private GameManager gameManager;
        private Level level6Asset;

        [SetUp]
        public void SetUp()
        {
            boardObj = new GameObject("TestBoardManager");
            board = boardObj.AddComponent<BoardManager>();
            BoardManager.SetTestInstance(board);

            gameManagerObj = new GameObject("TestGameManager");
            gameManager = gameManagerObj.AddComponent<GameManager>();

            level6Asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Level>("Assets/Data/ReferenceLevels/Level_006.asset");
        }

        [TearDown]
        public void TearDown()
        {
            if (board != null)
            {
                board.ClearBoard();
                BoardManager.SetTestInstance(null);
            }
            if (boardObj != null) Object.DestroyImmediate(boardObj);
            if (gameManagerObj != null) Object.DestroyImmediate(gameManagerObj);
        }

        [Test]
        public void Level6_InitialCapacity_AllocatesCorrectCountsToIndigoHoles()
        {
            Assert.IsNotNull(level6Asset, "Level_006.asset must exist in Assets/Data/ReferenceLevels/");

            var holes = ReferenceLevelRuntimeAdapter.ExtractHoles(level6Asset);
            var queues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level6Asset, holes);

            // Find Indigo holes: ColorID = 6
            RuntimeReferenceHole hole9_8 = holes.Find(h => h.ColorID == 6 && h.OccupiedCells.Contains(new Vector2Int(9, 8)));
            RuntimeReferenceHole hole12_15 = holes.Find(h => h.ColorID == 6 && h.OccupiedCells.Contains(new Vector2Int(12, 15)));

            Assert.IsNotNull(hole9_8, "Hole_Indigo_9_8 must be extracted.");
            Assert.IsNotNull(hole12_15, "Hole_Indigo_12_15 must be extracted.");

            // Hole (9,8) must have capacity 3 (approached by Records 22, 28, 38)
            Assert.AreEqual(3, hole9_8.Capacity, "Hole_Indigo_9_8 must have initial capacity 3.");

            // Hole (12,15) must have capacity 2 (approached by Records 29, 41)
            Assert.AreEqual(2, hole12_15.Capacity, "Hole_Indigo_12_15 must have initial capacity 2.");

            // Total indigo capacity must be exactly 5
            int totalIndigoCap = hole9_8.Capacity + hole12_15.Capacity;
            int totalIndigoQueues = queues.FindAll(q => q.ColorID == 6).Count;
            Assert.AreEqual(totalIndigoQueues, totalIndigoCap, "Total indigo hole capacity must exactly equal 5 indigo queues.");
        }

        [Test]
        public void Level6_FullPlaythrough_AllQueuesReachDestination_WinStateTriggers_RestartRestoresInitialState()
        {
            Assert.IsNotNull(level6Asset, "Level_006.asset must exist.");

            // 1. Setup board
            board.SetupBoard(level6Asset);
            gameManager.SetGameState(GameState.Playing);

            Assert.AreEqual(32, board.ActiveQueues.Count, "Level 6 must instantiate 32 playable ant queues.");

            // Verify initial hole capacities
            HoleController hole9_8 = null;
            HoleController hole12_15 = null;
            foreach (var kvp in board.HolesMap)
            {
                if (kvp.Value != null && kvp.Value.ColorID == 6)
                {
                    if (kvp.Value.GridCoordinate == new Vector2Int(9, 8)) hole9_8 = kvp.Value;
                    if (kvp.Value.GridCoordinate == new Vector2Int(12, 15)) hole12_15 = kvp.Value;
                }
            }

            Assert.IsNotNull(hole9_8, "Hole_Indigo_9_8 must be on board.");
            Assert.IsNotNull(hole12_15, "Hole_Indigo_12_15 must be on board.");
            Assert.AreEqual(3, hole9_8.RemainingCount, "Hole_Indigo_9_8 must start with 3 capacity.");
            Assert.AreEqual(2, hole12_15.RemainingCount, "Hole_Indigo_12_15 must start with 2 capacity.");

            // 2. Play through level by repeatedly selecting an unblocked queue until all 32 complete
            int movesCompleted = 0;
            int maxIterations = 100;
            int iteration = 0;

            while (board.ActiveQueues.Count > 0 && iteration++ < maxIterations)
            {
                AntQueueController candidate = null;
                foreach (var q in board.ActiveQueues)
                {
                    if (board.IsPathClearForQueue(q))
                    {
                        candidate = q;
                        break;
                    }
                }

                Assert.IsNotNull(candidate, $"Deadlock detected at iteration {iteration}! {board.ActiveQueues.Count} queues remain, but none can move.");

                // Execute move
                bool started = board.TryStartMove(candidate);
                Assert.IsTrue(started, $"TryStartMove failed for unblocked queue {candidate.QueueId}");

                // Simulate rescue completion
                board.OnQueueCompleted(candidate);
                movesCompleted++;
            }

            Assert.AreEqual(32, movesCompleted, "All 32 ant queues in Level 6 must be successfully rescued.");
            Assert.AreEqual(0, board.ActiveQueues.Count, "No ant queues should remain on the board.");
            Assert.AreEqual(GameState.LevelWon, gameManager.CurrentState, "GameManager must transition to LevelWon state.");

            // 3. Verify restart restores the correct initial state
            board.SetupBoard(level6Asset);
            gameManager.SetGameState(GameState.Playing);

            Assert.AreEqual(32, board.ActiveQueues.Count, "Restart must restore all 32 ant queues.");

            HoleController restored9_8 = null;
            HoleController restored12_15 = null;
            foreach (var kvp in board.HolesMap)
            {
                if (kvp.Value != null && kvp.Value.ColorID == 6)
                {
                    if (kvp.Value.GridCoordinate == new Vector2Int(9, 8)) restored9_8 = kvp.Value;
                    if (kvp.Value.GridCoordinate == new Vector2Int(12, 15)) restored12_15 = kvp.Value;
                }
            }

            Assert.IsNotNull(restored9_8, "Hole_Indigo_9_8 must be restored on restart.");
            Assert.IsNotNull(restored12_15, "Hole_Indigo_12_15 must be restored on restart.");
            Assert.AreEqual(3, restored9_8.RemainingCount, "Hole_Indigo_9_8 must restore capacity 3 on restart.");
            Assert.AreEqual(2, restored12_15.RemainingCount, "Hole_Indigo_12_15 must restore capacity 2 on restart.");
        }
    }

    // =========================================================================
    // 8. MOVING-QUEUE COLLISION AND LEVEL 14 VERIFICATION TESTS
    // =========================================================================
    [TestFixture]
    public class MovingQueueAndLevel14Tests
    {
        private GameObject boardObj;
        private BoardManager board;
        private GameObject gameManagerObj;
        private GameManager gameManager;
        private List<GameObject> spawnedObjs;

        [SetUp]
        public void SetUp()
        {
            boardObj = new GameObject("TestBoardManager");
            board = boardObj.AddComponent<BoardManager>();
            board.SetGridDimensions(35, 35);
            BoardManager.SetTestInstance(board);

            gameManagerObj = new GameObject("TestGameManager");
            gameManager = gameManagerObj.AddComponent<GameManager>();
            gameManager.SetGameState(GameState.Playing);

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
            if (gameManagerObj != null) Object.DestroyImmediate(gameManagerObj);
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
        public void TwoMovingQueues_CanShareExactSamePath_WithoutRejectionOrLifeLoss()
        {
            // Confirmed gameplay: Once queues have started moving, they do not collide with each other.
            // Multiple ant queues can move simultaneously along the exact same path.
            var hole = CreateHole("Hole_Red", 0, new Vector2Int(5, 15), capacity: 2);

            // Queue 1 is in front at (5, 8)-(5, 7) heading North
            var q1 = CreateQueue("Q1", 0, new List<Vector2Int> { new Vector2Int(5, 8), new Vector2Int(5, 7) });
            // Queue 2 is directly behind Queue 1 at (5, 5)-(5, 4) heading North
            var q2 = CreateQueue("Q2", 0, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });

            int initialLives = gameManager.PlayerLives;

            // Start Queue 1
            bool started1 = board.TryStartMove(q1);
            Assert.IsTrue(started1, "Queue 1 must start moving legally.");
            Assert.IsTrue(q1.IsMoving);

            // Start Queue 2 while Queue 1 is actively moving along the exact same corridor
            bool started2 = board.TryStartMove(q2);
            Assert.IsTrue(started2, "Queue 2 must be accepted and start moving along the same path while Queue 1 is moving.");
            Assert.IsTrue(q2.IsMoving);

            // Verify no life was deducted
            Assert.AreEqual(initialLives, gameManager.PlayerLives, "Moving along the same path must not lose lives.");

            // Both queues enter and complete their rescues
            board.OnQueueCompleted(q1);
            board.OnQueueCompleted(q2);

            Assert.AreEqual(0, hole.InboundReservations);
            Assert.AreEqual(0, hole.RemainingCount, "Both queues must be counted, consuming hole capacity.");
        }

        [Test]
        public void HoleCapacity_CannotBeOverbooked_IndependentOfCollisionHandling()
        {
            // Hole has capacity 1
            var hole = CreateHole("Hole_Single", 0, new Vector2Int(5, 10), capacity: 1);

            var q1 = CreateQueue("Q1", 0, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });
            var q2 = CreateQueue("Q2", 0, new List<Vector2Int> { new Vector2Int(8, 10), new Vector2Int(9, 10) }); // Different corridor!

            // Queue 1 reserves the only capacity slot
            bool started1 = board.TryStartMove(q1);
            Assert.IsTrue(started1);
            Assert.AreEqual(1, hole.InboundReservations);
            Assert.AreEqual(0, hole.AvailableCapacity);

            // Queue 2 attempts to move into the same hole - must be rejected due to capacity limit
            bool started2 = board.TryStartMove(q2);
            Assert.IsFalse(started2, "Queue 2 must be rejected because hole capacity (1) is fully booked.");
            Assert.IsFalse(q2.IsMoving);
            Assert.AreEqual(1, hole.InboundReservations, "Inbound reservations must remain 1.");
        }

        [Test]
        public void StationaryBlockingQueue_StillBlocksMovement()
        {
            var hole = CreateHole("Hole_Red", 0, new Vector2Int(5, 15), capacity: 2);

            // Stationary blocking obstacle at (5, 10)
            var blockingQueue = CreateQueue("Blocker", 1, new List<Vector2Int> { new Vector2Int(5, 10), new Vector2Int(5, 9) });

            // Moving candidate at (5, 5) heading North through (5, 10)
            var candidate = CreateQueue("Candidate", 0, new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 4) });

            bool started = board.TryStartMove(candidate);
            Assert.IsFalse(started, "Stationary queue must still act as an obstacle blocking the path.");
            Assert.IsFalse(candidate.IsMoving);
        }

        [Test]
        public void Level14_Queue_Record_42_10_CanMoveAndCompleteLegally()
        {
            // Reproduce Level 14 Queue_Record_42_10:
            // Cells: [(25, 19), (26, 19), (27, 19), (27, 20), (26, 20), (25, 20), (24, 20), (24, 19)]
            // Head is at (25, 19), heading left (-1, 0) into (24, 19).
            // Tail is at (24, 19).
            // Because trailing cells vacate as the queue advances, (24, 19) is clear at step 1!
            var cells = new List<Vector2Int>
            {
                new Vector2Int(25, 19),
                new Vector2Int(26, 19),
                new Vector2Int(27, 19),
                new Vector2Int(27, 20),
                new Vector2Int(26, 20),
                new Vector2Int(25, 20),
                new Vector2Int(24, 20),
                new Vector2Int(24, 19)
            };

            // Hole 3 in Level 14 is at (17, 19) to (19, 19) with entrance at (19, 19)
            var holeCells = new List<Vector2Int>
            {
                new Vector2Int(17, 19), new Vector2Int(18, 19), new Vector2Int(19, 19),
                new Vector2Int(17, 18), new Vector2Int(18, 18), new Vector2Int(19, 18),
                new Vector2Int(17, 17), new Vector2Int(18, 17), new Vector2Int(19, 17)
            };
            var holeGo = new GameObject("Hole_10");
            spawnedObjs.Add(holeGo);
            var hole = holeGo.AddComponent<HoleController>();
            hole.Initialize(10, new Vector2Int(19, 19), 5);
            board.RegisterActiveHole(hole);

            var q = CreateQueue("Queue_Record_42_10", 10, cells);

            // Path must be clear and move must be accepted
            bool isClear = board.IsPathClearForQueue(q);
            Assert.IsTrue(isClear, "Level 14 Queue_Record_42_10 must have a clear path to its hole accounting for vacated tail cells.");

            bool moveStarted = board.TryStartMove(q);
            Assert.IsTrue(moveStarted, "TryStartMove must succeed for Queue_Record_42_10.");
            Assert.IsTrue(q.IsMoving, "Queue_Record_42_10 must transition to moving state.");

            // Complete rescue
            board.OnQueueCompleted(q);
            Assert.AreEqual(0, board.ActiveQueues.Count, "Queue_Record_42_10 must be successfully rescued and cleared from board.");
        }

        [Test]
        public void PipeReservations_AllowMultipleTraversals_UpToAvailableUses()
        {
            // Pipe with 2 uses
            var pipeCells = new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 6), new Vector2Int(5, 7) };
            var pipeGo = new GameObject("Pipe");
            spawnedObjs.Add(pipeGo);
            var pipe = pipeGo.AddComponent<PipeController>();
            pipe.Initialize(pipeCells, rawPipeCount: 2);
            board.RegisterActivePipe(pipe);

            var hole = CreateHole("Hole_Cyan", 0, new Vector2Int(5, 10), capacity: 2);

            var q1 = CreateQueue("Q1", 0, new List<Vector2Int> { new Vector2Int(5, 4), new Vector2Int(5, 3) });
            var q2 = CreateQueue("Q2", 0, new List<Vector2Int> { new Vector2Int(5, 2), new Vector2Int(5, 1) });

            // Queue 1 starts through pipe
            bool started1 = board.TryStartMove(q1);
            Assert.IsTrue(started1);
            Assert.AreEqual(1, pipe.ActiveReservations);

            // Queue 2 starts through pipe concurrently
            bool started2 = board.TryStartMove(q2);
            Assert.IsTrue(started2, "Queue 2 must be allowed to reserve second use of pipe concurrently.");
            Assert.AreEqual(2, pipe.ActiveReservations);

            // Complete queue 1
            board.OnQueueCompleted(q1);
            pipe.ConsumeUse();
            Assert.AreEqual(1, pipe.RemainingUses);
            Assert.AreEqual(1, pipe.ActiveReservations);
            Assert.IsFalse(pipe.IsExhausted);

            // Complete queue 2
            board.OnQueueCompleted(q2);
            pipe.ConsumeUse();
            Assert.AreEqual(0, pipe.RemainingUses);
            Assert.AreEqual(0, pipe.ActiveReservations);
            Assert.IsTrue(pipe.IsExhausted);
        }
    }
}
