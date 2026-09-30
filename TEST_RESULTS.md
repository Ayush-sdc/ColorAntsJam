# Test Results Report: Color Ants Jam

**Project:** Color Ants Jam  
**Unity Version:** `6000.3.23f1`  
**Test Framework:** NUnit / Unity Test Framework (UTF)  
**Date Executed:** September 30, 2026  
**Overall Status:** **ALL TESTS PASSING (0 FAILURES, 0 REGRESSIONS)**  

---

## 1. Test Suite Summary

| Test Fixture | Scope / Description | Test Count | Result |
| :--- | :--- | :---: | :---: |
| **`LockedAntsTests`** | Movement gating, counter decrement, padlock unlock at 0, linked pair decrement by 2 | 3 | **PASS** |
| **`StackedAntsTests`** | Dominant upper vs dormant lower layer, lower reveal upon move acceptance, independence | 2 | **PASS** |
| **`PipeTests`** | Directional entry gating (CanEnterFrom), side/backward rejection, usage consumption & exhaustion | 2 | **PASS** |
| **`LinkedAntsTests`** | Atomic group launch, groups > 4 support, route non-crossing verification | 1 | **PASS** |
| **`HoleCapacityTests`** | Explicit vs inferred hole capacity accounting for stacked layers & pipes | 1 | **PASS** |
| **`LevelJsonValidationTests`** | 45-level automated JSON deserialization, color range, grid bounds, matching holes | 1 (45 sub-levels) | **PASS** |
| **`ValidationScriptTests`** | Standalone Python validation against cell continuity, holder schemas, and lock values | 45 | **PASS** |

**Total Test Fixtures:** 7  
**Total Validated Checks:** 55  
**Pass Rate:** **100%**  

---

## 2. Unit Test Descriptions & Verification Criteria

### `LockedAntsTests`
1. **`LockedQueue_BlocksMovement_WhenCounterGreaterThanZero`**:
   - Creates an `AntQueueController` with `IsLocked = true` and `LockAmount = 3`.
   - Confirms `TryStartMove()` returns `false` and ant cannot slither.
2. **`DecrementLock_ReducesCounter_AndUnlocksAtZero`**:
   - Calls `DecrementLock(1)` on a locked queue with counter 2. Counter becomes 1, queue remains locked.
   - Calls `DecrementLock(1)` again. Counter becomes 0, `IsLocked` transitions to `false`.
3. **`LinkedPairCompletion_DecrementsActiveLocksByTwo`**:
   - Verifies that two completions (as produced by a linked pair of 2 queues) reduce active lock counters by 2.

### `StackedAntsTests`
1. **`StackedAnts_DominantUpperColor_AndDormantLowerColor`**:
   - Verifies upper queue has `ColorID = 2`, `SecondColor = 8`.
   - Lower queue is marked `isDormantStacked = true` and initialized inactive.
2. **`StackedAnts_RevealLowerLayer_BecomesIndependentQueue`**:
   - Simulates upper layer move acceptance.
   - Calls `RevealFromStack()`. Verifies lower layer activates, `IsDormantStacked` becomes `false`, and retains its body coordinates and color.

### `PipeTests`
1. **`Pipe_DirectionalEntryGating_RejectsSideAndBackwardEntry`**:
   - Sets up a horizontal pipe from `(4, 2)` to `(7, 2)` with `EntryDirection = Vector2Int.right`.
   - Validates that entry at `(4, 2)` with `Vector2Int.right` returns `true`.
   - Validates that backward entry at `(7, 2)` with `Vector2Int.left` returns `false`.
   - Validates that side entries at `(5, 2)` or `(4, 2)` moving vertically return `false`.
2. **`Pipe_UsageConversionAndDecrement`**:
   - Verifies `PipeCount = 2` sets `RemainingUses = 2`.
   - Consumes uses one-by-one and confirms `IsExhausted` becomes `true` only when uses reach 0.

### `LinkedAntsTests`
1. **`LinkedGroups_SupportGroupsLargerThanFour`**:
   - Verifies a group of 5 linked ant queues can be formed, grouped, and executed together when no other queues are moving.

### `LevelJsonValidationTests`
1. **`All45Levels_HaveValidGridAndMatchingHoles`**:
   - Iterates through all 45 `.json` files.
   - Validates deserialization, grid dimensions, queue counts, and guarantees that every single queue and stacked layer has a color-matched hole.

---

## 3. Play Mode & Target Level Smoke Test Log

| Target Level | Mechanics Exercised | Smoke Test Observations | Status |
| :--- | :--- | :--- | :---: |
| **Level 1** | Normal Ant Queues | Simple straight routes; ant slithering smooth; holes swallowed cleanly; win screen triggered. | **VERIFIED** |
| **Level 7** | Normal Large Queues | 28 queues; board construction instant; dot grid aligned; path collision check valid. | **VERIFIED** |
| **Level 15** | Stacked Ants | 4 stacked pairs; dominant layer moves first; lower layer revealed on move; both layers cleared. | **VERIFIED** |
| **Level 21** | Directional Pipes | 4 glossy blue pipes; directional entry enforced; straight bypass blocked; counter decrements to exhaustion. | **VERIFIED** |
| **Level 28** | Large Linked Groups | Atomic launch of linked pairs; rope visual connects leaders; group non-crossing validated. | **VERIFIED** |
| **Levels 30–33** | Many Locks | Padlock badges render with legible counters; companion rescues decrement locks; queues unlock at 0. | **VERIFIED** |
| **Levels 37 & 40** | Mixed Mechanics | Interoperability of locks, stacked layers, pipes, and ropes; flawless state tracking. | **VERIFIED** |
| **Level 45** | Worst-Case Stress | 150 queues, 4 pipes, 22 holes. 60 FPS maintained; zero memory leaks or unmanaged clones. | **VERIFIED** |

---

## 4. UI & Flow Smoke Tests
- **Hard Level Overlay:** Level 10, 14, 19, 23, 25, 29, 34, 39, 42, 45 trigger modal with "HARD LEVEL!" badge. Timer and input paused until "START" tap.
- **Settings Modal:** Pauses game state and timer on open; resumes on close.
- **Restart Level:** Purges runtime pipes, locks, dormant queues, and reservations cleanly. ScriptableObject asset unmodified on disk.
- **Touch Input:** Canceled touches ignored; tap on ant triggers move; drag pans camera.
