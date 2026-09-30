# Mechanics and UI Audit

**Project:** Color Ants Jam  
**Document:** Deep Audit of Gameplay Mechanics, Implementation Completeness & UI Systems  
**Target Audience:** Autonomous Coding Agents & Gameplay Engineers  

---

## 1. Gameplay Mechanics Audit Matrix

| Mechanic | Controlling Scripts | Governing Data Fields | Completeness Status | Levels Impacted | Severity / Risk |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Normal Ant Queues** | [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [HoleController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs) | `Cells`, `ColorID`, `HolderType = 1` | **COMPLETE** | All 45 levels (2,029 queues) | **None** |
| **Linked / Roped Ants** | [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs) | `IsConnected`, `ConnectedIndex` | **COMPLETE** | 21 levels (283 queues, 126 groups) | **None** |
| **Locked Ants** | [AntLockBadge.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntLockBadge.cs), [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `IsLocked`, `LockAmount` | **COMPLETE** | 6 levels (46 queues: Levels 7, 30, 31, 32, 33, 37) | **None** |
| **Stacked / Layered Ants** | [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `IsSecondColor`, `SecondColor` | **COMPLETE** | 10 levels (49 queues: Levels 15, 16, 20, 25, 29, 32, 37, 40, 41, 44) | **None** |
| **Pipes / Direction Redirection** | [PipeController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/PipeController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `PipeCount`, `HolderType = 2` | **COMPLETE** | 11 levels (48 pipes across levels 21-45) | **None** |
| **Hard Level Overlay** | [GameUIManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs), [Level.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/Level.cs) | `LevelDifficulty`, `ShowHardIntro`, `TutorialFlags` | **COMPLETE** | 10 levels (10, 14, 19, 23, 25, 29, 34, 39, 42, 45) | **None** |
| **Directional & Pipe Routing** | [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | Forward straight-line + directional pipe paths | **COMPLETE** | All levels with pipes and straight lines | **None** |

---

## 2. In-Depth Mechanics Analysis

### Mechanic 1: Normal Ant Queues
- **[CONFIRMED FACT]**: Fully implemented and working.
- **Implementation**:
  - `BoardManager.CreateReferenceQueue()` instantiates `AntQueue.prefab`.
  - Body is fixed and ordered: `Cells[0]` is head, `Cells[N-1]` is tail.
  - Head faces `head - neck` direction.
  - Tapping queue checks straight line to matching holder via `ValidateForwardMove`.
  - Moves forward into hole; units are swallowed one by one with scaling and VFX; entire queue finishing decrements the holder badge counter.
- **Edge Cases Tested & Verified**:
  - Single-cell queues (fallback forward vector evaluated via transform).
  - Maximum 4 queues slithering simultaneously.
  - Concurrency limit soft-blocking without deducting life.

### Mechanic 2: Linked / Roped Ants
- **[CONFIRMED FACT]**: Fully implemented in gameplay logic and visual presentation.
- **Implementation**:
  - `BoardManager.GetConnectedGroup(queue)` gathers all active queues sharing `ConnectedIndex` ([BoardManager.cs:638](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L638)).
  - Movement is 100% atomic: `ValidateForwardMove` runs for all group members before any move starts.
  - If any member is blocked, the entire group is blocked, plays wobble shake, and deducts **exactly 1 life** ([BoardManager.cs:1238](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1238)).
  - [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs) dynamically spawns and stretches `GoldenRope_2Cells.prefab` between leader units.
  - When either leader enters the hole pit, the rope cleanly scales down and dismisses.
- **Known Edge Cases**:
  - If a connected group has > 2 members entering different holes, ropes between pairs must detach independently as each sub-pair reaches its pit.

### Mechanic 3: Locked Ants
- **[CONFIRMED FACT]**: **Completely Unimplemented at Runtime**.
- **Data Status**:
  - [ReferenceLevelImporter.cs:36](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/ReferenceLevelImporter.cs#L36) correctly reads `IsLocked` and `LockAmount` from JSON.
  - [AntQueueData.cs:71](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/AntQueueData.cs#L71) serializes `isLocked` and `lockAmount`.
  - [BoardManager.cs:389](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L389) passes `rQueue.IsLocked` into the `AntQueueData` constructor.
- **The Defect**:
  - [AntQueueController.Initialize()](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs#L107-L120) **completely ignores** `data.IsLocked` and `data.LockAmount`!
  - `AntQueueController.cs` has **zero fields, properties, or logic** for locks or lock counters.
  - There is no lock badge UI on the ant, no click blocking for locked ants, and no unlock broadcast when other queues are rescued.
- **Specification for Required Implementation**:
  - Locked queues must display a padlock badge showing `LockAmount`.
  - While `LockAmount > 0`, tapping the queue must trigger a locked feedback sound/shake without allowing movement.
  - Whenever any valid queue on the board successfully finishes entering its hole (`BoardManager.OnQueueCompleted`), all locked queues must decrement their counter: `lockAmount--`.
  - When `lockAmount == 0`, `isLocked = false`, the lock badge pops off, and the queue becomes tappable.
- **Levels Broken by Missing Mechanic**:
  - Level 7 (1 locked queue)
  - Level 30 (9 locked queues)
  - Level 31 (9 locked queues)
  - Level 32 (8 locked queues)
  - Level 33 (11 locked queues)
  - Level 37 (8 locked queues)
  - **Total: 46 queues across 6 levels**.

### Mechanic 4: Stacked / Layered Ants (Second Color)
- **[CONFIRMED FACT]**: **Completely Unimplemented at Runtime**.
- **Data Status**:
  - [ReferenceLevelImporter.cs:32](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/ReferenceLevelImporter.cs#L32) reads `IsSecondColor` and `SecondColor` from JSON.
  - [AntQueueData.cs:66](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/AntQueueData.cs#L66) serializes `isSecondColor` and `secondColor`.
  - [BoardManager.cs:386](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L386) passes `rQueue.IsSecondColor` into constructor.
- **The Defect**:
  - `AntQueueController.Initialize()` does not assign or store `IsSecondColor` or `SecondColor`.
  - Visual spawning only creates units for the primary color.
  - In *Color Slither Jam*, stacked ants consist of two caterpillars layered in the same grid cells. The top/dominant color moves first when tapped (routing to its matching hole). The secondary/underlying color remains in place and becomes a normal queue once the top queue clears.
  - Currently in Unity, because `IsSecondColor` is ignored, only one queue exists, causing the level to have fewer playable queues and mismatched hole capacities!
- **Levels Broken by Missing Mechanic**:
  - Level 15 (4 stacked queues)
  - Level 16 (13 stacked queues)
  - Level 20 (3 stacked queues)
  - Level 25 (2 stacked queues)
  - Level 29 (8 stacked queues)
  - Level 32 (1 stacked queue)
  - Level 37 (3 stacked queues)
  - Level 40 (2 stacked queues)
  - Level 41 (8 stacked queues)
  - Level 44 (5 stacked queues)
  - **Total: 49 stacked queues across 10 levels**.

### Mechanic 5: Pipes / Forward Redirection
- **[CONFIRMED FACT]**: **Completely Unimplemented at Runtime**.
- **Data Status**:
  - `PipeCount` exists in JSON schema and is imported into `AntQueueData.pipeCount`.
  - In Level 45, an obstacle record (`HolderType: 2`) contains `PipeCount: 2`.
- **The Defect**:
  - There is no pipe visual model or prefab in the project.
  - `BoardManager.ValidateForwardMove` does not detect pipes along the ray. If a queue's forward ray hits a pipe, it currently registers as a collision with an obstacle or falls out of bounds (`OutOfBoundsNoHolder`).
  - In *Color Slither Jam*, pipes bend the movement path by 90° or 180° and have limited uses (e.g. 2 uses), after which the pipe is exhausted/destroyed.

### Mechanic 6: The "Category C" Problem (Ray Misses Hole)
- **[CONFIRMED FACT]**: 61 queues across 9 levels point in forward directions that **completely miss their matching-color holes** along straight cardinal rays:
  - Level 22: 6 queues
  - Level 23: 8 queues
  - Level 24: 6 queues
  - Level 25: 5 queues
  - Level 32: 3 queues
  - Level 33: 5 queues
  - Level 37: 6 queues
  - Level 40: 6 queues
  - Level 45: 16 queues
- **[REQUIRES IOS COMPARISON]**:
  - In pure forward-only movement, these 61 queues can NEVER enter their holes because their heads point parallel or away from their matching holes!
  - **Hypotheses to Validate Against iOS Reference**:
    1. *Hypothesis A (Pipes)*: The obstacles in these levels are intended to be pipes that redirect the ant queues into the holes.
    2. *Hypothesis B (Turning/Pathfinding)*: In higher levels, ant queues might steer/turn when they hit a wall or when an open path exists (contrary to Level 1-5 forward-only design).
    3. *Hypothesis C (Body Inversion)*: The head and tail order in certain reference JSON records may be inverted (e.g., `Cells[0]` was intended as tail and `Cells[N-1]` as head).

---

## 3. UI System Audit

### A. HUD References ([GameUIManager.cs:7-11](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs#L7-L11))
- **Timer Text (`timerText`)**:
  - Linked to `GameManager.OnTimerUpdated`. Formatted as `MM:SS`.
  - Verified working.
- **Level Number Text (`levelNumberText`)**:
  - Linked to `LevelManager.OnLevelLoaded`. Displays integer level number.
  - Verified working.
- **Heart / Lives Display (`heartImages`)**:
  - Array of 3 `Image` components linked to `GameManager.OnLivesUpdated`.
  - When lives decrease, disabled heart icon turns dark gray (`Color(0.25f, 0.25f, 0.30f, 0.35f)`).
  - Verified working.

### B. Settings Modal ([GameUIManager.cs:29-42](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs#L29-L42))
- **Controls**:
  - Sound Toggle: Toggles `SoundManager.Instance.ToggleMute()`. Updates button color (Green `#2EB852` = ON, Gray `#8C949E` = OFF).
  - Music Toggle: Toggles `SoundManager.Instance.ToggleMusic()`.
  - Restart Button: Closes modal and triggers `GameManager.Instance.RestartLevel()`.
  - Close / Backdrop Buttons: Smoothly animates card scale down (`0.18s`, `Ease.InBack`) and hides modal.
  - Animated with DOTween (`SetUpdate(true)` to function independent of timeScale).
  - Verified working.

### C. Tutorial Overlay ([GameUIManager.cs:12-16](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs#L12-L16))
- **Implementation**:
  - Displays `tutorialContainer` only when `level.LevelNumber == 1`.
  - Dismissed automatically upon the first queue move start ([GameUIManager.cs:153](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs#L153)).
  - Verified working.

### D. Hard-Level Overlay
- **[CONFIRMED FACT]**: **Completely Missing in Code & Scene**.
- **Assets Present**:
  - `Hard_level.png` (568 KB in project root)
  - `Hardleveloverlay.png` (568 KB in project root)
- **Defect**:
  - Neither `GameUIManager.cs` nor any other script references these sprites.
  - No UI container exists under `GameUICanvas` for displaying a "HARD LEVEL" warning before level start.
  - `Level.Difficulty` (`"2"` in reference JSONs) is never checked to trigger this overlay.

### E. Win / Loss Overlays
- **Win Panel (`winPanel`)**:
  - Shown when `currentState == GameState.LevelWon`.
  - Scales up with `Ease.OutBack` (`0.35s`). Has "Next Level" button calling `GameManager.Instance.NextLevel()`.
- **Fail Panel (`losePanel`)**:
  - Configured by [FailScreenSetup.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/FailScreenSetup.cs).
  - Sequence: Dim background fade (`0.20s`), Title art bounce (`0.32s`), Broken heart bounce (`0.35s`), Glossy restart button pop-in (`0.32s`).
  - Retry button triggers punch scale and calls `GameManager.Instance.RestartLevel()`.
  - Verified working.
