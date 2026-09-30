# Implemented Mechanics Specification: Color Ants Jam

**Game Title:** Color Ants Jam  
**Engine & Version:** Unity `6000.3.23f1` (URP `17.3.0`, Android Target)  
**Date:** September 30, 2026  
**Status:** All 6 Core Mechanics Implemented and Verified  

---

## 1. Locked Ants

### Overview & Data Representation
Locked ant queues are serialized with `IsLocked = true` and an integer `LockAmount > 0` within `AntQueueData`. They represent captured ant queues that cannot move until a specific number of companion ant queues have successfully reached their color-matched holes.

### Implementation Details
- **Component:** [AntLockBadge.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntLockBadge.cs) & [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs).
- **Badge Visual:** A procedural golden padlock mesh/billboard with an overlaid WorldSpace canvas showing the current lock counter value in bold text.
- **Badge Placement:** Anchored directly above the ant leader unit (`local y + 0.65m`) so it remains visible above the board plane.
- **Movement Blocking:** When tapped, `TryStartMove()` checks `IsLocked`. If `IsLocked == true` and `LockAmount > 0`:
  - Movement is aborted immediately.
  - An audible locked rattle feedback is triggered along with `SoundManager.PlayBlockedSound()`.
  - The padlock badge vibrates (`ShakePosition`) to communicate that the ant is captive.
  - Invalid taps do not consume lives or decrement the counter.
- **Counter Decrement Rules:**
  - When an unblocked ant queue reaches its matching hole and is swallowed, [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) invokes `DecrementAllActiveLocks(1)`.
  - Every active locked ant queue decrements its `LockAmount` by `1` and updates its counter text badge.
  - When a **linked pair** of ant queues completes, each individual queue reaching its matching hole calls `DecrementAllActiveLocks(1)`, resulting in an atomic decrement of `2` for linked pairs.
- **Unlocking:** When `LockAmount` reaches `0`:
  - `IsLocked` is set to `false`.
  - The padlock badge scales down and is cleanly destroyed (`badge.UnlockAndDismiss()`).
  - The ant queue becomes fully active and selectable for standard movement.
- **State Integrity & Restart:**
  - ScriptableObjects on disk are never modified at runtime.
  - On level restart or reload, `BoardManager.ClearBoard()` destroys all runtime badge instances and restores runtime queues from immutable level data.

---

## 2. Stacked Ants

### Overview & Data Representation
Stacked ant queues occupy two vertical layers on the exact same grid cells. In `AntQueueData`, the dominant upper queue has `IsSecondColor = true` and `SecondColor` set to the color ID of the lower layer.

### Implementation Details
- **Component:** [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs) & [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs).
- **Color Layering:**
  - The dominant upper ant queue displays `ColorID`.
  - The lower ant queue displays `SecondColor`.
  - Both queues share identical initial body cell coordinates, direction, and length.
- **Dormancy & Reveal Lifecycle:**
  - During board construction (`SetupReferenceBoard`), the dominant upper queue is spawned active on the board.
  - A companion dormant queue is instantiated for `SecondColor` (`isDormantStacked = true`) and deactivated (`gameObject.SetActive(false)`), tracked in `dormantStackedQueues`.
  - The dominant queue maintains a direct reference via `SetLowerQueueLayer(lowerQueue)`.
- **Atomic Move Acceptance:**
  - Blocked moves or invalid taps do not affect or reveal the lower layer.
  - When the dominant upper ant queue executes an accepted forward move into its matching hole, it immediately calls `RevealFromStack()`.
  - The lower ant queue becomes active (`gameObject.SetActive(true)`), unregisters its dormant state, and registers into `activeQueues` and `boardOccupancy`.
  - The lower layer becomes an independent ant queue with its original cells, direction, length, and lower color preserved.
- **Full Mechanic Interoperability:**
  - Stacked layers work seamlessly with locks, ropes, pipes, restart, win, and loss conditions.
  - Win condition in `GameManager` explicitly requires `dormantStackedQueues.Count == 0 && activeQueues.Count == 0`.

---

## 3. Directional Pipes

### Overview & Data Representation
In reference levels, pipes are serialized as `HolderType == 2` with an ordered `Cells` coordinate array and an integer `PipeCount` (remaining uses).

### Implementation Details
- **Component:** [PipeController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/PipeController.cs).
- **Procedural 3D Mesh:**
  - Procedurally generated glossy blue cylindrical tube mesh following the polyline centerline of `Cells`.
  - Rounded toroidal end caps at entrance and exit.
  - Specular gloss finish using `GardenEnvironmentPalette.GetOrCreateCachedMaterial(AntColorPalette.RoyalBlue, 0.90f)`.
- **Directional Gating (`CanEnterFrom`):**
  - **Entrance Cell:** `Cells[0]`.
  - **Exit Cell:** `Cells[Cells.Count - 1]`.
  - **Entry Direction:** Cardinal vector from `Cells[0]` to `Cells[1]`.
  - **Exit Direction:** Cardinal vector from `Cells[N-2]` to `Cells[N-1]`.
  - **Rule:** An ant queue can ONLY enter through `Cells[0]` moving strictly in `EntryDirection`.
  - Side entry (intermediate cells) and backward entry (`Cells[N-1]` into `Cells[N-2]`) are strictly rejected.
- **Path Traversal & Routing:**
  - [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) evaluates whether an ant head reaches a pipe entrance with an unobstructed path.
  - If a pipe is traversed, `GeneratePipeFreeWorldPath` constructs a continuous polyline: `Ant Head -> Entrance -> Along Centerline -> Exit -> Unobstructed Path to Matching Hole -> Hole Pit`.
  - Queues cannot bypass pipes via straight-line rays through the pipe body.
- **Usage Counting & Exhaustion:**
  - **Usage Conversion Formula:**
    - If `PipeCount > 0`, Displayed Uses = `PipeCount`.
    - If `PipeCount <= 0`, Displayed Uses = `1` (single-pass default).
  - Floating 3D counter badge displays remaining uses above the pipe midpoint.
  - A use is decremented **only after** an ant queue successfully traverses and arrives at its hole.
  - When remaining uses reach 0, the pipe is marked exhausted (`IsExhausted = true`), plays a collapse fade, and is destroyed.
- **Concurrency Protection:**
  - Active traversal marks `IsPipeInUse = true` until completion, preventing simultaneous incompatible overlaps.

---

## 4. Linked and Roped Ants

### Overview & Data Representation
Ant queues with `IsConnected = true` share a common `ConnectedIndex`. They are physically bound by a horizontal golden rope connector between their leaders.

### Implementation Details
- **Component:** [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs).
- **Atomic Group Movement:**
  - Tapping any member of a linked group initiates validation for **all** group members simultaneously.
  - If any member is blocked by obstacles, stationary queues, locked state, or lacks a matching hole, the **entire group is rejected atomically**.
  - All group members launch together in the same frame.
- **Large Group Support (> 4 members):**
  - While individual simultaneous movement is normally capped at 4 queues to maintain visual clarity on mobile, a linked group with > 4 members (e.g. Level 28 or Level 35) is explicitly permitted to move atomically if no other unrelated queues are moving (`movingQueues.Count == 0`).
  - The concurrency limiter will never prevent a valid linked group from moving.
- **Route Crossing Prevention:**
  - All projected cell paths across the linked group are cross-checked before launch. If any two paths intersect or collide, movement is blocked.
- **Unequal Queue Length Synchronization:**
  - Ant queues with different lengths travel at the configured `moveSpeed` (`10.0 units/sec`).
  - Leaders and followers update positions along their individual world paths independently while remaining synchronized in simulation time.
- **Lock Decrement by 2:**
  - Each individual queue arriving at its hole decrements active locks by 1. Because a linked pair contains 2 queues, completing a linked pair inherently decrements all active locks by 2.

---

## 5. Movement and Reservation Integrity

### Collision & Reservation Invariants
1. **Self-Body Collision:**
   - [BoardManager.cs:ValidateForwardMove](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) checks every prospective path step against `queue.CurrentBodyCoordinates`. Moving through own body is strictly impossible.
2. **Stationary Queue Collision:**
   - Every prospective step checks `boardOccupancy` and `stationaryQueues`.
3. **Dynamic Route Cell Reservation:**
   - When an ant queue begins moving, all grid cells along its future path are immediately registered into `reservedRouteCells`.
   - Other queues evaluating moves in subsequent frames treat reserved cells as fully occupied.
   - Reservations are released when the queue is swallowed or despawned.
4. **Hole Entry Lane Isolation:**
   - Inbound approach lanes into hole stadium slots are reserved exclusively, preventing overlapping entries into adjacent or identical slots.
5. **Android Touch Cancellation Filter:**
   - [PlayerInputHandler.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs) explicitly ignores `TouchPhase.Canceled`. A canceled gesture never triggers an inadvertent ant launch.
6. **State Transitions & Pause Gating:**
   - During `GameState.Paused`, `GameState.LevelWon`, `GameState.LevelLost`, and while the `HardLevelOverlay` is active, user input raycasting and countdown timers are completely frozen.
7. **Framerate Independence:**
   - Head movement utilizes `Mathf.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime)`. Variable frame rates on mobile do not decelerate movement.

---

## 6. Hole Capacity & Level Validation

### Capacity Specification
- `Level.cs` provides `ExplicitHoleCapacity` and `MaxCapacity`.
- When not explicitly specified, capacity is dynamically computed:
  - Each standard ant queue requires 1 capacity unit.
  - Each stacked ant queue requires 2 capacity units (1 for dominant upper, 1 for revealed lower layer).
  - Queues routed through pipes require capacity in their destination hole.
- All 45 levels in `Assets/Data/ReferenceLevelJSON` have been verified to have 100% capacity and matching hole coverage for every queue and stacked layer.
