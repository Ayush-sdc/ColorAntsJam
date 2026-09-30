# Architecture and Runtime Flow Specification

**Project:** Color Ants Jam  
**Document:** System Architecture, Runtime Lifecycles & Data Schemas  
**Target Audience:** Autonomous Coding Agents & Core Systems Engineers  

---

## 1. High-Level Architectural Diagram

```text
                                  [ GameManager ]
                        (State Machine, Timer, Lives, Win/Loss)
                                         │
                 ┌───────────────────────┼────────────────────────┐
                 ▼                       ▼                        ▼
        [ LevelManager ]       [ PlayerInputHandler ]     [ GameUIManager ]
   (LevelDatabase, Progress)    (Touch / Click Raycasts)   (HUD, Modals, Overlays)
                 │                       │
                 ▼                       ▼
        [ BoardManager ] ◄───────────────┘ (TryStartMove)
  (Grid Occupancy, HolesMap,
   ActiveQueues, Concurrency)
                 │
       ┌─────────┴────────────────────────┐
       ▼                                  ▼
[ HoleController ]              [ AntQueueController ]
(Procedural Mesh, Badge,         (Slither Movement, TrailHistory,
 Swallowing VFX, Footprint)       AntUnit Instances, Linked Ropes)
```

---

## 2. Comprehensive Runtime Flow

### Phase 1: Game Initialization
1. **Scene Bootstrap (`Assets/Scenes/main.unity`)**:
   - Unity engine loads the scene.
   - [GameManager.cs:41](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/GameManager.cs#L41) executes `Awake()`:
     - Sets singleton `GameManager.Instance`.
     - Initializes DOTween global tween/sequence capacities: `DOTween.SetTweensCapacity(750, 150)`.
     - Subscribes to events: `LevelManager.OnLevelLoaded`, `BoardManager.OnBoardStateChanged`, `AntQueueController.OnQueueBlockedFeedback`, `AntQueueController.OnQueueMoveStarted`.
   - [SoundManager.cs:23](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/SoundManager.cs#L23) executes `Awake()`:
     - Instantiates runtime `AudioSource`.
     - Procedurally computes float sample buffers for 6 procedural audio clips (`tap`, `hop`, `swallow`, `blocked`, `win`, `lose`) via `AudioClip.Create(...)`.
   - [GameUIManager.cs:54](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs#L54) executes `Awake()`:
     - Hooks event listeners and button callbacks for restart, settings, resume, and next level.
     - Ensures fail UI references via `EnsureFailUIReferences()`.

### Phase 2: Level Loading
1. **Level Selection & Retrieval**:
   - [LevelManager.cs:76](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/LevelManager.cs#L76) runs `Start()` -> `LoadCurrentLevel()`.
   - Reads `PlayerPrefs.GetInt("CurrentLevel", 1)`.
   - Queries `ActiveDatabase` ([LevelDatabase.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Data/Levels/LevelDatabase.asset)) via `db.GetLevel(currentLevelNumber)`.
   - Triggers static event `LevelManager.OnLevelLoaded?.Invoke(activeLevel)`.
2. **State & Environment Synchronization**:
   - `GameManager.HandleLevelLoaded(level)`:
     - Sets `currentState = GameState.NotStarted`.
     - Sets `timerStarted = false` *(countdown does NOT begin until the first valid ant move)*.
     - Resets `playerLives = 3`.
     - Resets `remainingTimer = level.TimeLimitInSeconds`.
     - Calls [BoardManager.cs:302](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L302): `BoardManager.Instance.SetupBoard(level)`.
     - Transitions `currentState = GameState.Playing`.
     - Fires `OnGameStateChanged`, `OnTimerUpdated`, and `OnLivesUpdated`.
   - `GameUIManager.HandleLevelLoaded(level)`: Updates level badge text, hides win/lose panels, enables tutorial container if Level 1.
   - `GardenBackgroundManager.HandleLevelLoaded(level)`: Rebuilds stone patio slab, lawn plane, bushes, flowers, and pebbles surrounding the grid.

### Phase 3: Board & Hole Construction
1. **Board Cleanup**:
   - `BoardManager.SetupBoard()` clears previous `activeQueues`, destroys old GameObjects, purges `holesMap`, `activeHoles`, `completedHoleIds`, and `linkedRopes`.
2. **Grid Geometry & Dimension Calculation**:
   - Authoritative dimensions are read from `level.GridSize` (or `level.GridWidth`/`level.GridHeight`).
   - Grid dot indicators (`GridDot`) are instantiated for every cell.
   - Inactive/obstacle cells defined in `level.DisabledGridCells` and `level.CellInfo` are stored in `disabledCellsSet`.
3. **Reference Hole Extraction**:
   - [ReferenceLevelRuntimeAdapter.cs:33](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs#L33) executes `ExtractHoles(level)`.
   - Iterates through `level.AntQueues` records where `HolderType == 0`.
   - Computes bounding box (`MinX`, `MaxX`, `MinY`, `MaxY`, `WidthCells`, `HeightCells`).
   - Sets `EntranceCell` to `OccupiedCells[0]`.
   - Evaluates queue capacities: counts how many playable queues face directly toward each hole along cardinal forward lines. If forward queues exist, assigns that count; otherwise falls back to total matching color queues.
4. **Hole Instantiation & Registration**:
   - For each hole, creates a GameObject with `HoleController`.
   - Calls `HoleController.Initialize(...)`:
     - Builds procedural 3D rounded-rectangle rim mesh ([HoleController.cs:645](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs#L645)), inner bevel wall, dark recessed pit floor, and text badge.
     - For single-queue capacity holders, applies `ApplySingleQueueHolderVisualOverride()` (scaled rim, bevel, and offset badge).
   - In `BoardManager.cs`, every cell in `hole.OccupiedCells` is registered into `holesMap[cell] = hole`.

### Phase 4: Ant Queue Spawning
1. **Playable Queue Extraction**:
   - `ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level, holes)` extracts records where `HolderType == 1`.
   - Preserves ordered body coordinates: `Cells[0]` is the leader/head, `Cells[N-1]` is the tail.
2. **Queue Instantiation**:
   - `BoardManager.CreateReferenceQueue(...)` instantiates `AntQueue` prefab.
   - Attaches `AntQueueController` and passes `AntQueueData` into `controller.Initialize(data)`.
3. **Ant Unit Spawning (`AntQueueController.SpawnAnts()`)**:
   - Clears existing units.
   - Iterates through `currentBodyCoordinates`:
     - **Index 0 (Head/Leader)**: Spawns `ChubbyAntColors` prefab with scale `0.37`. Sets forward rotation facing `neck -> head` direction.
     - **Index >= 1 (Followers/Tail)**: Spawns `WorkerAnt` prefab with scale `0.28`.
     - Body colors applied via `MaterialPropertyBlock` or cached URP Lit materials.
4. **Linked Group Discovery & Rope Visuals**:
   - `BoardManager.SetupLinkedRopes()` ([BoardManager.cs:1776](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1776)):
     - Groups active queues by `ConnectedIndex` where `IsConnected == true`.
     - For paired/grouped queues, instantiates `GoldenRope` prefabs linking the leader transforms via [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs).
5. **Camera Setup**:
   - `BoardCameraController.SetupCameraForLevel(level)` sets up fixed perspective baseline (88° pitch, 78° FOV, 13.04m height) and computes manual pan boundaries.

### Phase 5: Input & Queue Selection
1. **Pointer Detection**:
   - [PlayerInputHandler.cs:119](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs#L119) runs `HandleUnifiedPointerInput()` each frame.
   - If pointer drags > 12 pixels, switches to camera panning mode (`isCameraDragging = true`).
   - On release without drag (Tap Gesture): calls `ProcessClickOrTap(screenPos)`.
2. **Raycasting Hierarchy**:
   - **Step 1: Physics Raycast**: Raycasts against `interactableLayers`. If hitting an `AntUnit` collider, traverses upward to find `AntQueueController`.
   - **Step 2: Ground Plane Fallback**: If physics raycast misses, raycasts against mathematical plane `Plane(Vector3.up, Vector3.zero)`. Converts world coordinate to grid coordinate via `BoardManager.WorldToGridPosition(worldHitPos)` and queries `BoardManager.GetQueueOccupying(gridCoord)`.
3. **Trigger**:
   - Calls `targetQueue.TryStartMove()`.

### Phase 6: Movement Validation (Forward-Only)
1. **Delegation**:
   - `targetQueue.TryStartMove()` invokes `BoardManager.Instance.TryStartMove(queue)`.
2. **Gate Checks**:
   - If `queue.IsMoving` or `GameManager.CurrentState != GameState.Playing` -> return `false`.
   - **Concurrency Limit**: If `movingQueues.Count >= 4`, movement is soft-blocked with a wobble shake; **no life is lost** ([BoardManager.cs:1167](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1167)).
3. **Atomic Group Evaluation**:
   - Queries `var group = GetConnectedGroup(queue)`.
   - If `movingQueues.Count + group.Count > 4` -> soft-blocked, return `false`.
   - For every queue in the linked group, executes [ValidateForwardMove(...)](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L912):
     - Derives forward cardinal direction: `forwardDir = head - neck`.
     - Scans forward cell-by-cell (`curr = head + forwardDir`) until the first hole is encountered via `GetHoleAt(curr)`.
     - Validates hole color matches queue color (`destinationHolder.ColorID == queue.ColorID`).
     - Validates hole has remaining unreserved capacity (`destinationHolder.AvailableCapacity - pendingReservations > 0`).
     - Validates all cells between head and hole entrance are unblocked (not occupied by stationary queues, not reserved by moving queues).
4. **Validation Outcome**:
   - **BLOCKED**: If *any* member of the group is blocked:
     - Group movement fails atomically.
     - Members play wobble shake (`member.TriggerBlockedFeedback()`).
     - **Exactly 1 life is deducted**: `GameManager.Instance.LoseLife(InvalidMoveReason.NoValidPath)`.
     - Returns `false`.
   - **VALID**: If *all* members are unblocked:
     - Reserves slots on destination holders (`destinationHolder.TryReserveSlot(member)`).
     - Starts countdown timer on first valid move (`GameManager.Instance.StartTimerOnFirstValidMove()`).
     - Calculates lateral lane offsets so queues entering the same holder slither side-by-side without overlapping.
     - Launches `member.StartSmoothMovement(smoothRoute, targetHole, entranceCell)`.

### Phase 7: Slithering, Swallowing & Hole Completion
1. **Head Traversal & Trail History**:
   - [AntQueueController.cs:536](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs#L536) executes `SmoothFreeMoveRoutine()`.
   - Leader ant moves forward along the route at `moveSpeed = 6.8f`.
   - Leader continuously appends world positions and headings to `trailHistory`.
2. **Follower Unit Marching**:
   - Follower ants binary-search `trailHistory` to sample their exact world position at distance `totalTrailDistance - i * segmentDistance`.
   - Each `AntUnit` plays walk cycle wobble via DOTween sequence (`DOLocalMoveY` and `DOLocalRotate`).
3. **Hole Entry & Deep Despawn**:
   - When head approaches hole entrance, calls `destinationHole.RequestHoleEntry(queue)`.
   - As each ant unit reaches `HoleDespawnPoint` (deep inside the pit floor):
     - [HoleController.cs:880](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs#L880) runs `OnAntEnteredHole()`: triggers swallow burst VFX (`ant_dis`), plays swallow gulp SFX, shrinks unit, and destroys the ant unit GameObject.
4. **Queue Completion**:
   - Once all ants in the queue (including the tail) are swallowed:
     - `AntQueueController` calls `destinationHole.OnEntireQueueEnteredHole(this)` -> `destinationHole.OnQueueSwallowed(this)`.
     - Decrements `remainingCount` by 1.
     - Updates hole badge text.
     - [BoardManager.cs:1568](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1568) runs `OnQueueCompleted(queue)`: removes queue from `movingQueues` and `activeQueues`, destroys queue GameObject.
5. **Holder Completion & Permanent Grid Release**:
   - When a hole's `remainingCount == 0`, `inboundReservations == 0`, and `enteringQueues.Count == 0`:
     - Marks `isCompleted = true`.
     - Invokes [BoardManager.cs:1511](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1511): `RemoveCompletedHole(hole)`.
     - **Grid Release**: Iterates through all cells in `hole.OccupiedCells` and purges them from `holesMap`.
     - Fires `BoardManager.OnBoardStateChanged?.Invoke()`.
     - Runs `hole.PlayCompletedEffectAndDisappear()`: disables colliders, shrinks hole (`0.20s`, `Ease.InBack`), and destroys the holder GameObject.

### Phase 8: Win / Loss Detection
1. **Trigger Points**:
   - Evaluated whenever board state changes (`BoardManager.OnBoardStateChanged`) or timer ticks.
2. **Win Condition**:
   - Evaluated in [GameManager.cs:164](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/GameManager.cs#L164):
   - `if (activeQueues.Count == 0)` -> Level Won!
   - Sets `currentState = GameState.LevelWon`.
   - Displays `winPanel` with scale bounce, plays victory arpeggio SFX.
3. **Loss Conditions**:
   - **Lives Depleted**: `playerLives <= 0` -> Triggers `LevelLost` ("Lives depleted!").
   - **Time Out**: `remainingTimer <= 0f` -> Triggers `LevelLost` ("Time ran out!").
   - **Deadlock**: If no queues are currently moving and `!anyCanMove` (no remaining active queue has an unblocked path) -> Triggers `LevelLost` ("Deadlock!").
   - `GameUIManager` executes `AnimateFailScreenEntrance()`: dark background fade-in, failed title bounce, broken heart bounce, glossy restart button pop-in.

### Phase 9: Progression, Restart & Next Level
- **Restart**:
  - `GameManager.RestartLevel()` -> `LevelManager.RestartCurrentLevel()` -> reloads active level data and resets board.
- **Next Level**:
  - `GameManager.NextLevel()` -> `LevelManager.AdvanceToNextLevel()`:
    - Increments `currentLevelNumber = Mathf.Min(45, currentLevelNumber + 1)`.
    - Persists to `PlayerPrefs.SetInt("CurrentLevel", currentLevelNumber)`.
    - Calls `LoadLevel(currentLevelNumber)`.

---

## 3. Data Structures & Serialized Schemas

### A. ScriptableObject: `Level.cs`
| Field Name | Type | Meaning | Example Value | Used at Runtime? |
| :--- | :--- | :--- | :--- | :--- |
| `levelNumber` | `int` | 1-based index of the level | `1` | **Yes** (HUD, adapters) |
| `levelName` | `string` | Display title | `"Reference Level 1"` | **Yes** (Logging/HUD) |
| `difficulty` | `string` | Difficulty descriptor | `"0"`, `"Easy"`, `"Hard"` | **Partial** (Data preserved; hard overlay missing) |
| `duration` | `float` | Countdown timer limit in seconds | `120.0f` | **Yes** (`GameManager.RemainingTimer`) |
| `gridSize` | `Vector2Int` | Authoritative grid width (x) & height (y) | `(8, 5)` | **Yes** (Primary board dimensions) |
| `gridWidth` | `int` | Legacy fallback grid width | `8` | **Yes** (Fallback if `gridSize == 0`) |
| `gridHeight` | `int` | Legacy fallback grid height | `5` | **Yes** (Fallback if `gridSize == 0`) |
| `cellSize` | `float` | Physical spacing between grid cell centers | `1.0f` | **Yes** (World pos math) |
| `capacity` | `int` | Global tray capacity fallback | `5` | **No** (Per-hole capacity used instead) |
| `disabledGridCells` | `List<Vector2Int>` | Explicit coordinates of blocked cells | `[(0, 0), (1, 0)]` | **Yes** (`GetCombinedDisabledCells()`) |
| `cellInfo` | `List<CellInfo>` | Structured metadata per grid cell | `[CellInfo, ...]` | **Yes** (Obstacle/disabled flags) |
| `holes` | `List<HoleData>` | Handmade level holes (empty in ref levels) | `[]` | **Yes** (Handmade levels only) |
| `antQueues` | `List<AntQueueData>` | All reference objects (holes, queues, obstacles) | `[AntQueueData, ...]` | **Yes** (Core source of truth) |

### B. Class: `AntQueueData.cs`
| Field Name | Type | Meaning | Example Value | Used at Runtime? |
| :--- | :--- | :--- | :--- | :--- |
| `queueId` | `string` | Unique identifier string | `"Record_4_10"` | **Yes** (GameObject naming, logs) |
| `colorId` | `int` | Raw reference color integer (0..11) | `10` (Red) | **Yes** (Authoritative color mapping) |
| `holderType` | `int` | Type identifier (0=Hole, 1=Queue, 2=Obstacle) | `1` | **Yes** (Partitioning holes vs queues) |
| `antCount` | `int` | Count of ant units | `5` | **Yes** (Fallback for body length) |
| `cells` | `List<Vector2Int>` | Ordered body coordinates (Head=0, Tail=N-1) | `[(1,0), (2,0)]` | **Yes** (Primary spawn & body coordinates) |
| `fullPathToHole` | `List<Vector2Int>` | Precalculated path to target hole | `[(1,0), (5,0)]` | **No** (Dynamic forward raycast used instead) |
| `isConnected` | `bool` | Flag indicating queue is roped to another | `true` | **Yes** (Atomic group movement & rope visual) |
| `connectedIndex` | `int` | Group ID shared by linked queues | `3` | **Yes** (Groups queues into atomic sets) |
| `isSecondColor` | `bool` | Flag indicating stacked/secondary color | `true` | **Yes** (Stacked dominant layer & dormant lower spawn) |
| `secondColor` | `int` | Raw color ID of secondary layer | `2` (Yellow) | **Yes** (Lower dormant layer color mapping) |
| `pipeCount` | `int` | Number of pipe redirection uses | `2` | **Yes** (Directional pipe remaining-use counter) |
| `isLocked` | `bool` | Flag indicating queue is locked behind counter | `true` | **Yes** (AntLockBadge padlock & movement gating) |
| `lockAmount` | `int` | Counter of queues to rescue before unlocking | `3` | **Yes** (Countdown to unlock on matching arrivals) |

### C. Class: `HoleData.cs` (Used in Handmade Levels)
| Field Name | Type | Meaning | Example Value | Used at Runtime? |
| :--- | :--- | :--- | :--- | :--- |
| `colorId` | `int` | Raw reference color ID | `4` (Orange) | **Yes** (Handmade levels) |
| `gridCoordinate` | `Vector2Int` | Entrance coordinate | `(5, 4)` | **Yes** (Handmade levels) |
| `displayCount` | `int` | Initial queue capacity count | `2` | **Yes** (Handmade levels) |
| `parallelRows` | `int` | Parallel entrance slots (1 or 2) | `1` | **Yes** (Handmade levels) |

### D. Class: `CellInfo.cs`
| Field Name | Type | Meaning | Example Value | Used at Runtime? |
| :--- | :--- | :--- | :--- | :--- |
| `position` | `Vector2Int` | Grid coordinate | `(2, 4)` | **Yes** (Board positioning) |
| `isForcedDisabledCell` | `bool` | Whether cell is blocked / unplayable | `true` | **Yes** (Blocker registration) |
| `cellType` | `int` | Cell category identifier | `0` | **No** (Unused at runtime) |
| `colorId` | `int` | Color association | `0` | **No** (Unused at runtime) |
| `capacity` | `int` | Capacity of cell | `1` | **No** (Unused at runtime) |

### E. Raw JSON Level Format (`Assets/Data/ReferenceLevelJSON/*.json`)
```json
{
  "snakes": [
    {
      "Cells": [{"x": 5, "y": 4}, {"x": 6, "y": 4}],
      "ColorID": 2,
      "HolderType": 0,
      "IsConnected": false,
      "IsSecondColor": false,
      "ConnectedIndex": 0,
      "SecondColor": 0,
      "PipeCount": 0,
      "IsLocked": false,
      "LockAmount": 0
    }
  ],
  "CellInfo": [
    {
      "IsForcedDisabledCell": true,
      "Position": {"x": 0, "y": 0}
    }
  ],
  "GridSize": {"x": 8, "y": 5},
  "Duration": 120.0,
  "LevelDifficulty": 0,
  "MaxCapacity": 5,
  "DisabledGridCells": []
}
```
*(All fields mapped losslessly by [ReferenceLevelImporter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/ReferenceLevelImporter.cs) into `Level.cs` assets).*
