# Test Results Report: Color Ants Jam

**Project:** Color Ants Jam  
**Unity Version:** `6000.3.23f1` (URP `17.3.0`, Target: Android)  
**Test Framework:** NUnit / Unity Test Framework (UTF EditMode/PlayMode)  
**Test Results XML Path:** `C:/Users/ayush/AppData/LocalLow/DefaultCompany/Slither Ants Jam/TestResults.xml` (copied to [TestResults.xml](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/TestResults.xml))  
**Console Log Output:** `Assets/Tests/Editor/MechanicsUnitTests.cs` executed via `TestRunnerApi`  
**Execution Date:** September 30, 2026  
**Overall Status:** **23 / 23 TESTS PASSED (0 FAILURES, 0 REGRESSIONS)**  

---

## 1. Test Suite Execution Summary

| Test Fixture | Scope / Description | Tests | Passed | Failed | Result |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **`LockedAntsIntegrationTests`** | Movement gating while locked, decrements exactly once per rescue, linked pair decrements by 2, non-rescue event rejection, zero-unlock & restart restoration | 5 | 5 | 0 | **PASS** |
| **`LinkedAntsIntegrationTests`** | Groups of 2, 5, 6, 7 real movement execution, atomic block with zero reservations, unequal queue/route lengths, restart/interruption cleanup | 7 | 7 | 0 | **PASS** |
| **`StackedAntsIntegrationTests`** | Blocked tap does not reveal lower layer, accepted move reveals lower layer at original footprint, lower layer blocked by departing upper layer, independent rescue with capacity and lock counter updates | 4 | 4 | 0 | **PASS** |
| **`PipeIntegrationTests`** | Straight and bent pipe trajectory generation, directional entry gating (entrance & direction only), concurrent queue reservation rejection (exceeding uses), consumption & exhaustion | 4 | 4 | 0 | **PASS** |
| **`HoleCapacityAndLevelDataTests`**| Multi-hole color-grouped capacity allocation satisfying all queues including stacked, JSON round-trip preserving compatibility keys (`snakes`) and ordered cells, hard-level overlay metadata | 3 | 3 | 0 | **PASS** |
| **`validate_all_levels.py`** | Standalone validator for structural integrity, cell continuity, valid colors, and capacity sufficiency across all 45 levels | 45 | 45 | 0 | **PASS** |

**Total Automated Integration Tests:** 23  
**Total Passed:** 23  
**Total Failed:** 0  
**Inconclusive / Skipped:** 0  

---

## 2. Detailed Test Breakdown

### A. Locked Ants Integration Tests (`LockedAntsIntegrationTests`)
- **`LockedQueue_CannotMove_WhileLocked`** [PASS]:
  Queue initialized with `IsLocked = true` and `LockAmount = 2`. Calling `BoardManager.TryStartMove(lockedQueue)` returns `false`. Queue stays stationary (`IsMoving == false`).
- **`LockCounter_DecrementsExactlyOncePerCompletedRescue`** [PASS]:
  Active locked queue initialized with counter 3. Completing a single unlocked queue via `BoardManager.OnQueueCompleted(queue)` decrements counter to exactly 2.
- **`RescuedLinkedPair_DecrementsCountersByTwo`** [PASS]:
  Active locked queue initialized with counter 4. Completing both queues of a linked pair via `BoardManager.OnQueueCompleted` decrements counter to exactly 2 (decrement of 2).
- **`NonRescueEvents_DoNotDecrementLockCounter`** [PASS]:
  Asserts that blocked taps, moves in progress, stopped early / canceled moves (`OnQueueMoveStoppedEarly`), and duplicate `OnQueueCompleted` callbacks DO NOT decrement active lock counters.
- **`Lock_UnlocksAtZero_AndRestoresOnRestart`** [PASS]:
  Queue unlocks (`IsLocked == false`) immediately when counter reaches 0. Re-initializing data upon level restart restores original configured counter and locked state.

### B. Linked Ants Integration Tests (`LinkedAntsIntegrationTests`)
- **`LinkedGroups_ExecuteRealSimultaneousMovement_Groups2_5_6_7`** [PASS]:
  Parameterized test exercising real groups of 2, 5, 6, and 7 queues on `BoardManager`. Verifies all members start simultaneously (`IsMoving == true`), follow their trajectories, and complete cleanly.
- **`LinkedGroup_AtomicBlock_StaysStationaryWithoutReservations`** [PASS]:
  5-queue linked group where member 3 has an obstacle queue blocking its path. Move attempt is rejected (`false`); all 5 queues stay stationary; 0 route cells are reserved; 0 hole slots are reserved.
- **`LinkedGroup_SimultaneousMovement_UnequalQueueAndRouteLengths`** [PASS]:
  Linked group with unequal queue lengths (length 2 vs length 4) and unequal route lengths (distance 4 vs distance 8). Validated and launched; both move simultaneously towards their destinations.
- **`LinkedGroup_CleanupOnRestartAndInterruption`** [PASS]:
  Verifies that `BoardManager.ClearBoard()` during active linked group movement cleanly removes moving queues, clears reserved route cells, and releases all reservations.

### C. Stacked Ants Integration Tests (`StackedAntsIntegrationTests`)
- **`StackedQueues_BlockedAttempt_DoesNotRevealLowerLayer`** [PASS]:
  Upper layer attempt blocked by missing hole. Upper queue stays stationary; lower layer remains dormant and inactive (`isDormantStacked == true`, `gameObject.activeSelf == false`).
- **`StackedQueues_AcceptedUpperMove_RevealsLowerLayerAtOriginalFootprint`** [PASS]:
  Upper layer move accepted. Upper queue starts moving; lower layer is immediately revealed (`isDormantStacked == false`, `gameObject.activeSelf == true`) at its exact original footprint cells.
- **`StackedQueues_LowerLayer_CannotMoveThroughDepartingUpperLayer`** [PASS]:
  While upper layer is departing along corridor, revealed lower layer attempts to move through the same corridor. Movement rejected (`ReservedByMovingRoute` / `OccupiedByMovingQueue`).
- **`StackedQueues_RescueEachLayerIndependently_ColorCapacityAndLocks`** [PASS]:
  Upper layer (Color A) enters Hole A, consuming Hole A capacity and decrementing active lock by 1. Lower layer (Color B) enters Hole B, consuming Hole B capacity and decrementing active lock to 0.

### D. Pipe Integration Tests (`PipeIntegrationTests`)
- **`Pipe_StraightAndBentPipe_PathGenerationAndFollowers`** [PASS]:
  Verifies entrance, interior cells, and exit for both straight pipes and bent (L-shaped) pipes.
- **`Pipe_DirectionalEntryRules_RejectsSideAndBackwardEntry`** [PASS]:
  Verifies entry is permitted only at `EntranceCell` in `EntryDirection`. Rejects backward entry at exit and side entry at intermediate cells.
- **`Pipe_PreventsConcurrentQueues_ExceedingRemainingUses`** [PASS]:
  Pipe with 1 remaining use. Two queues in a linked group both attempt to route through the pipe. First queue validates; second queue is rejected with `BlockedByPipeObstacle`.
- **`Pipe_ReservationsConsumptionAndExhaustion`** [PASS]:
  Pipe with 2 uses. Consuming 2 uses exhausts pipe (`IsExhausted == true`, `RemainingUses == 0`).

### E. Hole Capacity & Level Data Tests (`HoleCapacityAndLevelDataTests`)
- **`HoleCapacity_ColorGroupedAllocation_SatisfiesAllQueuesIncludingStacked`** [PASS]:
  Tests multi-hole distribution on Level 15 (10 queues + 4 stacked = 14 rescues, 4 holes). Confirms total color capacity satisfies all required queues.
- **`JsonRoundTrip_PreservesAllMechanicsFieldsAndCellOrder`** [PASS]:
  Round-trips level data through JSON serialization/deserialization. Confirms exact preservation of compatibility key `snakes`, ordered `Cells`, `IsConnected`, `ConnectedIndex`, `IsSecondColor`, `SecondColor`, `PipeCount`, `IsLocked`, `LockAmount`.
- **`LevelMetadata_HardLevelOverlays_DoNotAppearOnNormalLevels`** [PASS]:
  Normal levels (`LevelDifficulty = 0`, `showHardIntro = false`) do NOT show hard overlay. Hard levels (`LevelDifficulty >= 2`) show hard overlay.

---

## 3. Categorized Verification Status

### Implemented
- Ant lock badges with 3D padlock mesh and dynamic TextMeshPro counter.
- Procedural 3D tube pipe controller with entrance gating, bent/straight trajectories, usage reservations, and exhaustion.
- Stacked queue second color support with dormant layer reveal upon accepted move.
- Atomic linked group validation, simultaneous launch, and parallel rope visuals.
- Color-grouped hole capacity allocation algorithm in `ReferenceLevelRuntimeAdapter.cs`.
- Duplicate rescue guard and pipe reservation tracking in `BoardManager.cs`.
- Hard-level overlay gating based on `LevelDifficulty >= 2` or `ShowHardIntro`.
- Complete JSON importer and exporter with reference schema compatibility.

### Verified by Automated Tests
- All 23 integration and unit tests in `Assets/Tests/Editor/MechanicsUnitTests.cs`.
- Structural integrity, cell continuity, valid colors, and capacity sufficiency across all 45 levels via `validate_all_levels.py`.
- JSON round-trip serialization preserving all mechanics fields and ordered cell sequences.

### Verified Manually (In Editor PlayMode)
- Interactive touch/click controls, camera drag panning, and zoom on Levels 1, 2, and 4.
- Normal ant queue free-floor slithering, follower trail tracing, and swallow pit animation.
- Hole completion effect, sound effects, and multi-cell footprint release.
- Win panel appearance upon board clearing.

### Assumed Reference Behavior (Documented Assumptions)
- **Pipe Usage Count:** The raw field `PipeCount` in reference JSON is treated as the number of available traversals (`RemainingUses`).
- **Pipe Entrance Direction:** Ants are permitted to enter pipes only at `Cells[0]` traveling in direction `Cells[1] - Cells[0]`. Reference game reverse-entry or multi-directional entry is unconfirmed from footage.
- **Lock Decrement Rule:** Rescuing a single queue decrements all active locked queues by 1. Rescuing a linked pair decrements by 2.

### Unverified or Failing
- **Android Physical Hardware Performance:** Unverified. No physical Android device connected. Profiling conducted in Unity 6 Windows Editor.
- **End-to-End Solvability of All 45 Levels:** Structural validation and capacity sufficiency pass, but full win-state playthrough has only been completed on Levels 1, 2, and 4. Levels 5–45 are not claimed fully solvable until verified by PlayMode simulation or manual playthrough.
