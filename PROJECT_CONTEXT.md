# Slither Ants Jam - Project Context & Session Handover

**Last Updated:** September 11, 2026 (Comprehensive Project State)  
**Active Branch:** `main`  
**Unity Version:** 6000.3.23f1 (Universal Render Pipeline - URP)  
**Primary Scene:** `Assets/Scenes/main.unity`  

---

## 1. Project Overview & Core Gameplay Loop

**Slither Ants Jam** is a top-down 3D grid puzzle game built in Unity. Players tap / click colored ant queues ("caterpillar" slither chains) to route them into color-matched stadium holes around obstacles and other ant bodies.

### Core Loop
1. **Board Rebuild**: `LevelManager` loads the selected `Level` asset on start or on level change. `BoardManager.SetupBoard()` clears previous runtime objects and instantiates holes, queues, and grid dots.
2. **Pathfinding & Input**: `PlayerInputHandler` detects raycasts on ant queues / grid cells. `BoardManager.TryStartMove()` uses `AntPathfinder.cs` (A* grid search) to validate that an unblocked path exists to a matching hole.
3. **Slithering & Hole Entry**: Ants move segment-by-segment along the path (`AntQueueController`), slithering toward the hole entrance.
4. **Hole Swallowing & Capacity**: As each ant unit reaches the hole, `HoleController` swallows the unit with DOTween scaling and burst VFX (`ant_dis`), decrementing its badge counter.
5. **Win / Loss Evaluation**:
   - **Win**: All ant queues on the board have been cleared into their respective holes.
   - **Loss (Deadlock / Timeout)**: Timer reaches 0 or all remaining ant queues are deadlocked with no valid path to their holes.

---

## 2. Level Architecture & Dual-Database System

The game supports two parallel level database systems without any conflicts:

```text
                                  LevelManager (GameController)
                                                │
                 ┌──────────────────────────────┴──────────────────────────────┐
                 ▼                                                             ▼
    [Handmade Production Mode]                                    [Reference Progression Mode]
  useReferenceLevelDatabase = FALSE                             useReferenceLevelDatabase = TRUE
                 │                                                             │
   Assets/Data/Levels/LevelDatabase.asset                      Assets/Data/ReferenceLevelDatabase.asset
  (3 Handmade Levels: Level 1, 2, 3)                         (45 Reference Levels: Level 001 - 045)
                 │                                                             │
                 ▼                                                             ▼
     SetupHandmadeBoard(level)                                    SetupReferenceBoard(level)
 (Direct HoleData + AntQueueData)                            (Via ReferenceLevelRuntimeAdapter)
```

### Current Active Configuration (Default)
- **Active Database**: `Assets/Data/Levels/LevelDatabase.asset`
- **`useReferenceLevelDatabase`**: `FALSE`
- **Active Level 1 Asset**: `Assets/Data/Levels/Level_01.asset` (9x6 grid, 5 holes, 5 queues, 35 total ants)
- **BoardManager Branch**: `SetupHandmadeBoard()`

---

## 3. Key Scripts & Systems

| Script | Location | Responsibility |
| :--- | :--- | :--- |
| **`LevelManager.cs`** | `Assets/Scripts/LevelSystem/` | Singleton controlling active level index, progression persistence (`PlayerPrefs`), database switching, and clamped `E`/`Q` keyboard navigation. |
| **`LevelDatabase.cs`** | `Assets/Scripts/LevelSystem/` | ScriptableObject holding an ordered list of `Level` assets with index bounds clamping/looping. |
| **`Level.cs`** | `Assets/Scripts/LevelSystem/` | Data container for level metadata (dimensions `GridWidth`/`GridHeight`, duration, disabled cells, holes, and ant queues). |
| **`ReferenceLevelRuntimeAdapter.cs`** | `Assets/Scripts/LevelSystem/` | In-memory dynamic adapter converting raw reference levels (`HolderType 0` holes, `HolderType 1` queues) into runtime board data without mutating immutable source files. |
| **`BoardManager.cs`** | `Assets/Scripts/Board/` | Grid coordinate translation, clean runtime board lifecycle (`SetupBoard`, `ClearBoard`), hole map registration (`holesMap`), connected group validation, and visual dot spawning. |
| **`AntPathfinder.cs`** | `Assets/Scripts/Board/` | A* pathfinding algorithm navigating around obstacles, other queues, disabled cells, and hole boundaries directly into color-matched hole entrances. |
| **`BoardCameraController.cs`** | `Assets/Scripts/Camera/` | Dynamic camera auto-framing adjusting orthographic/perspective bounds to fit arbitrary board sizes with UI headroom. |
| **`AntQueueController.cs`** | `Assets/Scripts/Ants/` | Slither chain movement, path execution, eye gaze tracking, blocked wobble feedback, connected group synchronization. |
| **`AntUnit.cs`** | `Assets/Scripts/Ants/` | Individual ant body mesh, material color rendering, animated eye and pupil rotation. |
| **`HoleController.cs`** | `Assets/Scripts/Board/` | Procedural 3D tubular glossy stadium rim, black cavity pit, colored badge mesh, 3D text counter, ant absorption animation, burst particles. |
| **`GameManager.cs`** | `Assets/Scripts/Core/` | Game state machine (`NotStarted`, `Playing`, `LevelWon`, `LevelLost`), countdown timer, player lives, deadlock detection. |
| **`PlayerInputHandler.cs`** | `Assets/Scripts/Core/` | Unified mouse and touch raycasting against ant colliders and board plane, keyboard shortcuts (`E` = Next, `Q` = Prev). |
| **`GameUIManager.cs`** | `Assets/Scripts/UI/` | Top HUD (timer, level number text, life hearts, settings button), win/loss modal dialogs, audio settings modal. |
| **`SoundManager.cs`** | `Assets/Scripts/Core/` | Procedural audio synthesis for taps, ant slithering, hole absorption, win/loss jingles, mute toggling. |
| **`GardenBackgroundManager.cs`** | `Assets/Scripts/Environment/` | Dynamic stone patio floor, curb borders, foliage bushes, and pebble scatters adapted to board dimensions. |

---

## 4. Progressive Mechanics Status Across All 45 Reference Levels

| Mechanic | Reference Field | Levels Present | Runtime Support Status |
| :--- | :--- | :--- | :--- |
| **Normal Queues** | `snakes[].HolderType == 1` | All 45 Levels | **Fully Supported** |
| **Normal Holes** | `snakes[].HolderType == 0` | All 45 Levels | **Fully Supported** |
| **Disabled Grid Cells** | `DisabledGridCells`, `CellInfo.IsForcedDisabledCell` | 2, 5, 6, 7, 12, 13, 14, 15, 19, 20, 21, 22, 23, 26, 27, 28, 29, 30, 31, 32, 33, 36, 38, 41, 44 | **Fully Supported** (Checked in `BoardManager.IsCellFree` & `AntPathfinder`) |
| **Connected Queues** | `snakes[].IsConnected`, `ConnectedIndex` | 7, 8, 9, 10, 17, 18, 20, 24, 25, 28, 29, 32, 33, 35, 37, 40, 41, 43, 44, 45 | **Fully Supported** (Atomic path validation & synchronized group movement) |
| **Second Color Queues** | `snakes[].IsSecondColor`, `SecondColor` | 15, 16, 20, 25, 29, 32, 37, 40, 41, 44 | **Partially Implemented** (Fields parsed; dual-color visuals & mid-absorption split pending) |
| **Holder Type 2 (Obstacles)** | `snakes[].HolderType == 2` | 21, 22, 23, 24, 25, 32, 33, 37, 38, 40, 45 | **Not Yet Wired** (Adapter ignores type 2; needs obstacle/barrier spawning) |
| **Locks** | `snakes[].IsLocked`, `LockAmount` | 30, 31, 32, 33, 37 | **Not Yet Wired** (Fields parsed; key/counter unlocking behavior pending) |
| **Pipes (Tunnels)** | `snakes[].PipeCount > 0` | 45 | **Not Yet Wired** (Underground traversal logic pending) |

---

## 5. Automated Test Suites

All tests are located in `Assets/Tests/` and organized by assembly definitions:

- **EditMode Tests** (`Assets/Tests/EditMode/EditModeTests.asmdef`):
  - `BoardCameraControllerTests.cs`: Verifies camera height calculation, FOV constraints, and top UI margin padding.
  - `LevelDatabaseTests.cs`: Verifies 45 reference levels integrity and Level 1-2 handmade ant counts (35 and 60).
- **PlayMode Tests** (`Assets/Tests/PlayMode/PlayModeTests.asmdef`):
  - `GameplayInitializationTests.cs`: Verifies manager singletons, input handling, and Level 1 handmade setup.
  - `ReferenceProgressionTests.cs`: Verifies sequential loading, `E`/`Q` clamping (Level 1 min, Level 45 max), and exact dimensions/hole/queue counts for sample reference levels (1, 2, 3, 7, 15, 21, 30, 45).

---

## 6. How to Continue Next Session

1. **To play Handmade Levels (Production)**:
   - Ensure `useReferenceLevelDatabase = false` in `LevelManager` (or toggle in Inspector on `GameController`).
   - Levels 1, 2, 3 in `Assets/Data/Levels/` will load sequentially.
2. **To switch to 45 Reference Levels**:
   - Set `useReferenceLevelDatabase = true` in `LevelManager`.
   - All 45 levels in `Assets/Data/ReferenceLevels/` will load sequentially with `E`/`Q` navigation.
3. **Next Feature Priorities**:
   - Add visual & runtime support for **Second Color Queues** (Level 15+).
   - Add **Holder Type 2 static obstacles/barriers** to `ReferenceLevelRuntimeAdapter` (Level 21+).
   - Implement **Lock & Key queue mechanics** (Level 30+).
   - Implement **Pipes/Tunnels** (Level 45).
