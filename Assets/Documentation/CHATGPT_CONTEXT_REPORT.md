# Slither Ants Jam — Complete Current-State Context Report

Generated: 2026-09-24 02:15 (UTC+05:30)
Unity project: Slither Ants Jam
Git commit/hash: 037a1c0d9d4c70f8e878d2d5f63fa724425a9693 (main branch)
Active scene: Assets/Scenes/main.unity
Report mode: READ-ONLY PROJECT AUDIT

---

## 0. Project Context Alignment & Authority Rules

This report was produced after a complete read-only audit of the project source code, serialized scenes, and asset databases.

- **Primary Project Memory:** `Assets/Documentation/ANTIGRAVITY_CONTEXT.md` was read in full prior to this audit.
- **Rule of Authority:** Current source files (`Assets/Scripts/...`), serialized scene files (`Assets/Scenes/main.unity`), and ScriptableObject `.asset` files are the final authority. Where discrepancies were detected against earlier test expectations or draft notes, the actual project assets were verified directly.
- **Scope:** Zero modifications were made to gameplay code, scenes, prefabs, ScriptableObjects, databases, or project settings during this audit.

---

## 1. Project Identification

- **Project Name:** Slither Ants Jam
- **Unity Version:** `6000.3.23f1 (09d2ecc7fb28)`
- **Current Project Path:** `C:\Users\ayush\Desktop\Unity Projects\Slither Ants Jam`
- **Active Scene:** `Assets/Scenes/main.unity`
- **Scene 0 in Build Settings:** `Assets/Scenes/main.unity` (GUID: `99c9720ab356a0642a771bea13969a05`)
- **Other Enabled Build Scenes:** None (Only Scene 0 is enabled).
- **Render Pipeline:** Universal Render Pipeline (URP) — `UniversalRenderPipelineAsset` (GUID: `4b83569d67af61e458304325a23e5dfd`), URP Global Settings (GUID: `18dc0cd2c080841dea60987a38ce93fa`).
- **Path Parity Confirmation:** Verified. Antigravity and the running Unity Editor process are confirmed to be operating on the exact same project path: `C:\Users\ayush\Desktop\Unity Projects\Slither Ants Jam`.

---

## 2. High-Level Game Description

Slither Ants Jam is a 3D top-down grid-based puzzle game inspired by Color Slither Jam. The player guides colored ant chains into matching stadium-shaped holders while navigating around obstacles and other ant bodies.

- **Game Objective:** Route every ant queue into its matching color-coded holder until the board is completely cleared.
- **Ant Units & Queues:** Each 'queue' is a slither chain of individual 3D ant models (`AntUnit.cs`) with expressive googly eyes and colored carapaces. The leader segment is at index 0 (`head`).
- **Holders / Holes:** Stadium-shaped recessed slots (`HoleController.cs`) with colored outer rims (`SlotRim`), inner drop-bevel walls (`SlotInnerBevel`), recessed dark floors (`SlotPit`), and counter badges (`SlotCounterBadge`). Each holder occupies a multi-cell rectangular footprint on the grid.
- **Player Interaction:** Players tap or click on an ant queue or any grid cell occupied by a queue.
- **Movement Trigger:** When clicked, `BoardManager.TryStartMove()` uses deterministic BFS pathfinding (`AntPathfinder.cs`) to find the shortest 4-directional unblocked path from the queue's head to a reachable entrance of a non-full matching holder. If a path exists, the queue slithers segment-by-segment into the holder.
- **Level Completion (Win):** Triggered when `BoardManager.ActiveQueues.Count == 0`. All ant queues have been cleared.
- **Level Failure (Loss):**
  1. **Timer Expiry:** The level timer reaches `0:00`.
  2. **Deadlock:** There are remaining queues on the board, but none can find an unblocked path to their matching holder (`!anyCanMove`).

---

## 3. Current Runtime Architecture

### Runtime Flow Trace

```text
1. Play Mode Entered -> Assets/Scenes/main.unity
2. GameController GameObject initializes:
   a. LevelManager.Start() [Assets/Scripts/LevelSystem/LevelManager.cs]
      -> Reads PlayerPrefs 'CurrentLevel' (default: 1)
      -> Evaluates ActiveDatabase: useReferenceLevelDatabase == false
         => Selects levelDatabase: Assets/Data/Levels/LevelDatabase.asset
      -> Retrieves Level asset: Assets/Data/ReferenceLevels/Level_001.asset
      -> Fires OnLevelLoaded(Level)
   b. GameManager.HandleLevelLoaded(Level) [Assets/Scripts/Core/GameManager.cs]
      -> Sets GameState = Playing, resets timer
      -> Calls BoardManager.Instance.SetupBoard(Level)
   c. BoardManager.SetupBoard(Level) [Assets/Scripts/Board/BoardManager.cs]
      -> ClearBoard(): Destroys any lingering objects under boardParent
      -> Evaluates ReferenceLevelRuntimeAdapter.IsReferenceLevel(level)
         => Returns TRUE (level.Holes is empty, antQueues contains HolderType 0/1)
      -> Executes Branch: SetupReferenceBoard(level)
         * ExtractHoles(level): Builds RuntimeReferenceHole list (HolderType == 0)
         * ExtractPlayableQueues(level, holes): Builds RuntimeReferenceQueue list (HolderType == 1)
         * Spawns HoleController instances into holesMap and activeHoles
         * Spawns AntQueueController instances into activeQueues
      -> SpawnGridVisuals(level): Instantiates floor dot markers
      -> BoardCameraController.AdjustCameraFraming(level): Dual-mode dynamic framing
3. Live Gameplay Loop:
   PlayerInputHandler.Update() [Assets/Scripts/Core/PlayerInputHandler.cs]
   -> Raycasts on click/touch to detect AntUnit / Grid coordinate
   -> Calls targetQueue.TryStartMove()
   -> BoardManager.TryStartMove(queue)
      -> AntPathfinder.FindPath(board, queue) [BFS shortest path to matching holder]
      -> If path found:
         * queue.StartMoveAlongPath(path)
         * Queue slithers into DestinationHole
         * HoleController swallows ants with DOTween scaling and ant_dis VFX
         * Queue finishes entry -> BoardManager.OnQueueCompleted(queue)
         * DestinationHole.OnQueueSwallowed() decrements remainingCount
         * When remainingCount == 0:
           - hole.IsCompleted = true
           - BoardManager.RemoveCompletedHole(hole):
             * All occupied footprint cells immediately freed in holesMap
             * ActiveHoles.Remove(hole)
             * CompletedHoleCount++ & completedHoleIds.Add(holeId)
             * OnBoardStateChanged triggered
             * HoleController.PlayCompletedEffectAndDisappear() shrinks and destroys GameObject
4. Evaluation:
   GameManager.CheckGameWinLossConditions()
   -> If activeQueues.Count == 0 => TriggerWin() -> GameUIManager displays WinPanel
   -> If activeQueues.Count > 0 and no queue can move => TriggerGameOver() -> GameUIManager displays LosePanel
```

---

## 4. Active Database / Level Source

### Serialized LevelManager State (`Assets/Scenes/main.unity`)
- **GameObject:** `GameController` (FileID: `35470740`)
- **Component:** `LevelManager` (Script GUID: `fa1e7687a8b04ad438141b9fbe3fd207`, FileID: `51246`)
- **`levelDatabase`:** `{fileID: 11400000, guid: a2be64d3f89a2704ab3e83255f1a2807, type: 2}` -> `Assets/Data/Levels/LevelDatabase.asset`
- **`referenceLevelDatabase`:** `{fileID: 11400000, guid: 8328801cd2d64f1aa309c900ba32f417, type: 2}` -> `Assets/Data/ReferenceLevelDatabase.asset`
- **`useReferenceLevelDatabase`:** `0` (False)
- **`overrideLevelForTesting`:** `0` (False)
- **`testLevelNumber`:** `1`
- **ACTUALLY ACTIVE DATABASE:** `Assets/Data/Levels/LevelDatabase.asset` (evaluates `ActiveDatabase => (useReferenceLevelDatabase && referenceLevelDatabase != null) ? referenceLevelDatabase : levelDatabase`).

### Active LevelDatabase Levels 1–10 Mapping

| Level # | DB Index | Referenced Asset Path | Asset Name | Grid Size | Style | HT 0 (Holders) | HT 1 (Queues) | Explicit Holes | Total AntQueues Records |
| :---: | :---: | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | 0 | `Assets/Data/ReferenceLevels/Level_001.asset` | Level_001 | 8x5 | Reference-style | 5 | 5 | 0 | 10 |
| **2** | 1 | `Assets/Data/ReferenceLevels/Level_002.asset` | Level_002 | 10x10 | Reference-style | 5 | 10 | 0 | 15 |
| **3** | 2 | `Assets/Data/ReferenceLevels/Level_003.asset` | Level_003 | 10x10 | Reference-style | 5 | 15 | 0 | 20 |
| **4** | 3 | `Assets/Data/ReferenceLevels/Level_004.asset` | Level_004 | 10x10 | Reference-style | 9 | 14 | 0 | 23 |
| **5** | 4 | `Assets/Data/ReferenceLevels/Level_005.asset` | Level_005 | 15x15 | Reference-style | 6 | 23 | 0 | 29 |
| **6** | 5 | `Assets/Data/ReferenceLevels/Level_006.asset` | Level_006 | 20x20 | Reference-style | 11 | 32 | 0 | 43 |
| **7** | 6 | `Assets/Data/ReferenceLevels/Level_007.asset` | Level_007 | 15x15 | Reference-style | 5 | 28 | 0 | 33 |
| **8** | 7 | `Assets/Data/ReferenceLevels/Level_008.asset` | Level_008 | 15x15 | Reference-style | 14 | 30 | 0 | 44 |
| **9** | 8 | `Assets/Data/ReferenceLevels/Level_009.asset` | Level_009 | 15x15 | Reference-style | 17 | 52 | 0 | 69 |
| **10** | 9 | `Assets/Data/ReferenceLevels/Level_010.asset` | Level_010 | 20x20 | Reference-style | 18 | 47 | 0 | 65 |

**Critical Distinction:** The active database `Assets/Data/Levels/LevelDatabase.asset` does NOT point to handmade legacy levels (`Assets/Data/Levels/Level_XX.asset`). It points directly to the 45 authoritative reference levels in `Assets/Data/ReferenceLevels/`!

---

## 5. Level Data Model

### `Level.cs` (`Assets/Scripts/LevelSystem/Level.cs`)
ScriptableObject asset representing level configuration:
- `levelNumber` (`int`): 1-based level index.
- `levelName` (`string`): Display name (e.g. 'Reference Level 1').
- `difficulty` (`int`): Difficulty rating integer.
- `duration` (`float`): Level time limit (0.0 = unlimited, default fallback in `TimeLimitInSeconds` is 120s if duration <= 0).
- `gridSize` (`Vector2Int`): Authoritative grid dimensions (X = width, Y = height).
- `gridWidth` / `gridHeight` (`int`): Serialized legacy dimension fields. `GridWidth` returns `GridSize.x`, `GridHeight` returns `GridSize.y`.
- `cellSize` (`float`): World spacing between cells (typically 1.0f).
- `capacity` (`int`): Legacy capacity field.
- `disabledGridCells` (`List<Vector2Int>`): Coordinates permanently blocked.
- `holes` (`List<HoleData>`): Explicit handmade hole definitions (empty in reference levels).
- `antQueues` (`List<AntQueueData>`): Serialized records containing both HolderType 0 (holders) and HolderType 1 (queues).
- `cellInfo` (`List<CellInfo>`): Per-cell metadata list.

### `AntQueueData.cs` (`Assets/Scripts/LevelSystem/AntQueueData.cs`)
- `queueId` (`string`): Unique string identifier (e.g. `Record_0_2`).
- `colorId` (`int`): Raw reference ColorID integer (0..11).
- `color` (`AntColor`): Resolved internal enum (via `ReferenceColorMapper.ToAntColor(colorId)`).
- `holderType` (`int`):
  - `0`: Reconstructed as a static multi-cell holder tray (`HoleController`).
  - `1`: Reconstructed as an active playable ant queue (`AntQueueController`).
  - `2`: Obstacle / blocker.
- `antCount` (`int`): Count of units in queue, or initial capacity of holder.
- `cells` (`List<Vector2Int>`): Complete ordered coordinate array. For queues: index 0 is head. For holders: complete physical multi-cell footprint.
- `fullPathToHole` (`List<Vector2Int>`): Serialized path if precomputed.
- `isConnected` (`bool`) / `connectedIndex` (`int`): Queue linking metadata.
- `isSecondColor` (`bool`) / `secondColor` (`int`): Multi-color queue metadata.
- `pipeCount` (`int`): Tube/tunnel traversal metadata.
- `isLocked` (`bool`) / `lockAmount` (`int`): Queue lock metadata.

### `HoleData.cs` (`Assets/Scripts/LevelSystem/HoleData.cs`)
Handmade hole representation (used only if `level.Holes.Count > 0`):
- `colorId` (`int`) / `color` (`AntColor`)
- `gridCoordinate` (`Vector2Int`)
- `capacity` (`int`)
- `isRightSideSlot` (`bool`)
- `parallelRows` (`int`)

### `ReferenceLevelRuntimeAdapter.cs` (`Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs`)
Dynamic translation adapter:
- `IsReferenceLevel(Level level)`: Returns true if `level.Holes == null || level.Holes.Count == 0`, and `level.AntQueues.Count > 0`.
- `ExtractHoles(Level level)`: Filters `AntQueues` where `holderType == 0`. Determines matching playable queue count for initial capacity, computes bounding box (`MinX..MaxX`, `MinY..MaxY`), sets `EntranceCell = OccupiedCells[0]`.
- `ExtractPlayableQueues(Level level, List<RuntimeReferenceHole> holes)`: Filters `AntQueues` where `holderType == 1`. Sets `BodyCoordinates = Cells`, `Cells[0]` as leader head.

---

## 6. Reference Level Detection

### Current Code Condition (`ReferenceLevelRuntimeAdapter.cs:14`)
```csharp
public static bool IsReferenceLevel(Level level)
{
    if (level == null) return false;

    // If explicit handmade holes are configured, this is a handmade level
    if (level.Holes != null && level.Holes.Count > 0)
    {
        return false;
    }

    // If no explicit holes list, but antQueues contains reference data
    return (level.AntQueues != null && level.AntQueues.Count > 0) || (level.CellInfo != null && level.CellInfo.Count > 0);
}
```

### Renderer Branching in `BoardManager.SetupBoard()`
```csharp
bool isRefLevel = ReferenceLevelRuntimeAdapter.IsReferenceLevel(level);
if (isRefLevel)
{
    SetupReferenceBoard(level);
}
else
{
    SetupHandmadeBoard(level);
}
```
**Levels 1–5 Verification:**
For every Level 1 to 5 in the active database, `level.Holes` has 0 entries and `level.AntQueues` contains reference records. Therefore, `IsReferenceLevel` returns **TRUE** for all of them, and **`SetupReferenceBoard()`** is executed.

---

## 7. Levels 1–5 — Complete Current State

All coordinates below are **unabbreviated** and extracted directly from the authoritative `.asset` files.
Format for coordinates: `[x, y]`.
Coordinate system: (0,0) is bottom-left. X increases rightward, Y increases upward.

### LEVEL 1
- **Asset Path:** `Assets/Data/ReferenceLevels/Level_001.asset`
- **Grid Size:** `8x5`
- **Duration:** `0.0s` (unlimited, falls back to default 120s timer)
- **Difficulty:** `0`
- **HolderType 0 (Holders) Count:** `5`
- **HolderType 1 (Playable Queues) Count:** `5`
- **Distinct Raw ColorIDs Used:** `0 (SkyBlue), 2 (Yellow), 7 (Green), 8 (Brown), 10 (Red)`

#### Holders (5 total)
- **Raw ColorID 2 (Yellow):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(5, 4), (6, 4)]`
  - Entrance Cell: `(5, 4)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 7 (Green):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(5, 3), (6, 3)]`
  - Entrance Cell: `(5, 3)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 0 (SkyBlue):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(5, 2), (6, 2)]`
  - Entrance Cell: `(5, 2)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 8 (Brown):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(5, 1), (6, 1)]`
  - Entrance Cell: `(5, 1)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 10 (Red):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(1, 0), (0, 0)]`
  - Entrance Cell: `(1, 0)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `LeftSide` (isRightSide = `False`)

#### Playable Queues (5 total)
- **Queue `Record_4_10`:**
  - Raw ColorID: `10` (Red)
  - Ant Count: `10` units
  - Head Coordinate: `(2, 0)`
  - Target Destination: `Red Holder at entrance (1, 0)`
  - Complete Body Cells: `[(2, 0), (3, 0), (4, 0), (5, 0), (6, 0), (7, 0), (7, 1), (7, 2), (7, 3), (7, 4)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_6_2`:**
  - Raw ColorID: `2` (Yellow)
  - Ant Count: `8` units
  - Head Coordinate: `(4, 4)`
  - Target Destination: `Yellow Holder at entrance (5, 4)`
  - Complete Body Cells: `[(4, 4), (3, 4), (2, 4), (1, 4), (0, 4), (0, 3), (0, 2), (0, 1)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_7_7`:**
  - Raw ColorID: `7` (Green)
  - Ant Count: `6` units
  - Head Coordinate: `(4, 3)`
  - Target Destination: `Green Holder at entrance (5, 3)`
  - Complete Body Cells: `[(4, 3), (3, 3), (2, 3), (1, 3), (1, 2), (1, 1)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_8_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `4` units
  - Head Coordinate: `(4, 2)`
  - Target Destination: `SkyBlue Holder at entrance (5, 2)`
  - Complete Body Cells: `[(4, 2), (3, 2), (2, 2), (2, 1)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_9_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `2` units
  - Head Coordinate: `(4, 1)`
  - Target Destination: `Brown Holder at entrance (5, 1)`
  - Complete Body Cells: `[(4, 1), (3, 1)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`

---

### LEVEL 2
- **Asset Path:** `Assets/Data/ReferenceLevels/Level_002.asset`
- **Grid Size:** `10x10`
- **Duration:** `0.0s` (unlimited, falls back to default 120s timer)
- **Difficulty:** `0`
- **HolderType 0 (Holders) Count:** `5`
- **HolderType 1 (Playable Queues) Count:** `10`
- **Distinct Raw ColorIDs Used:** `0 (SkyBlue), 1 (Lime), 4 (Orange), 6 (Indigo), 10 (Red)`

#### Holders (5 total)
- **Raw ColorID 10 (Red):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(0, 1), (1, 1), (1, 0), (0, 0)]`
  - Entrance Cell: `(0, 1)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 0 (SkyBlue):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(8, 3), (8, 2), (9, 2), (9, 3)]`
  - Entrance Cell: `(8, 3)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 1 (Lime):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(7, 9), (7, 8), (8, 8), (8, 9)]`
  - Entrance Cell: `(7, 9)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 6 (Indigo):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(1, 7), (0, 7), (0, 8), (1, 8)]`
  - Entrance Cell: `(1, 7)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `LeftSide` (isRightSide = `False`)
- **Raw ColorID 4 (Orange):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(4, 1), (4, 0), (5, 0), (5, 1)]`
  - Entrance Cell: `(4, 1)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)

#### Playable Queues (10 total)
- **Queue `Record_4_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `5` units
  - Head Coordinate: `(2, 8)`
  - Target Destination: `Indigo Holder at entrance (1, 7)`
  - Complete Body Cells: `[(2, 8), (3, 8), (4, 8), (5, 8), (6, 8)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_5_4`:**
  - Raw ColorID: `4` (Orange)
  - Ant Count: `3` units
  - Head Coordinate: `(4, 4)`
  - Target Destination: `Orange Holder at entrance (4, 1)`
  - Complete Body Cells: `[(4, 4), (4, 5), (4, 6)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_6_4`:**
  - Raw ColorID: `4` (Orange)
  - Ant Count: `3` units
  - Head Coordinate: `(5, 4)`
  - Target Destination: `Orange Holder at entrance (4, 1)`
  - Complete Body Cells: `[(5, 4), (5, 5), (5, 6)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_8_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `5` units
  - Head Coordinate: `(2, 7)`
  - Target Destination: `Indigo Holder at entrance (1, 7)`
  - Complete Body Cells: `[(2, 7), (3, 7), (4, 7), (5, 7), (6, 7)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_9_1`:**
  - Raw ColorID: `1` (Lime)
  - Ant Count: `4` units
  - Head Coordinate: `(7, 7)`
  - Target Destination: `Lime Holder at entrance (7, 9)`
  - Complete Body Cells: `[(7, 7), (7, 6), (7, 5), (7, 4)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_10_1`:**
  - Raw ColorID: `1` (Lime)
  - Ant Count: `4` units
  - Head Coordinate: `(8, 7)`
  - Target Destination: `Lime Holder at entrance (7, 9)`
  - Complete Body Cells: `[(8, 7), (8, 6), (8, 5), (8, 4)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_11_10`:**
  - Raw ColorID: `10` (Red)
  - Ant Count: `5` units
  - Head Coordinate: `(0, 2)`
  - Target Destination: `Red Holder at entrance (0, 1)`
  - Complete Body Cells: `[(0, 2), (0, 3), (0, 4), (0, 5), (0, 6)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_12_10`:**
  - Raw ColorID: `10` (Red)
  - Ant Count: `5` units
  - Head Coordinate: `(1, 2)`
  - Target Destination: `Red Holder at entrance (0, 1)`
  - Complete Body Cells: `[(1, 2), (1, 3), (1, 4), (1, 5), (1, 6)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_13_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `6` units
  - Head Coordinate: `(7, 3)`
  - Target Destination: `SkyBlue Holder at entrance (8, 3)`
  - Complete Body Cells: `[(7, 3), (6, 3), (5, 3), (4, 3), (3, 3), (2, 3)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_14_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `6` units
  - Head Coordinate: `(7, 2)`
  - Target Destination: `SkyBlue Holder at entrance (8, 3)`
  - Complete Body Cells: `[(7, 2), (6, 2), (5, 2), (4, 2), (3, 2), (2, 2)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`

---

### LEVEL 3
- **Asset Path:** `Assets/Data/ReferenceLevels/Level_003.asset`
- **Grid Size:** `10x10`
- **Duration:** `0.0s` (unlimited, falls back to default 120s timer)
- **Difficulty:** `0`
- **HolderType 0 (Holders) Count:** `5`
- **HolderType 1 (Playable Queues) Count:** `15`
- **Distinct Raw ColorIDs Used:** `0 (SkyBlue), 2 (Yellow), 3 (Purple), 6 (Indigo), 7 (Green)`

#### Holders (5 total)
- **Raw ColorID 3 (Purple):**
  - Footprint Dimensions: `1x2`
  - Complete OccupiedCells: `[(1, 8), (1, 7)]`
  - Entrance Cell: `(1, 8)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 0 (SkyBlue):**
  - Footprint Dimensions: `1x3`
  - Complete OccupiedCells: `[(0, 2), (0, 1), (0, 0)]`
  - Entrance Cell: `(0, 2)`
  - Initial Queue Counter: `6` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 6 (Indigo):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(8, 9), (9, 9), (9, 8), (8, 8)]`
  - Entrance Cell: `(8, 9)`
  - Initial Queue Counter: `4` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 7 (Green):**
  - Footprint Dimensions: `1x2`
  - Complete OccupiedCells: `[(2, 5), (2, 4)]`
  - Entrance Cell: `(2, 5)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 2 (Yellow):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(9, 4), (8, 4)]`
  - Entrance Cell: `(9, 4)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `LeftSide` (isRightSide = `False`)

#### Playable Queues (15 total)
- **Queue `Record_0_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `7` units
  - Head Coordinate: `(0, 7)`
  - Target Destination: `SkyBlue Holder at entrance (0, 2)`
  - Complete Body Cells: `[(0, 7), (0, 8), (0, 9), (1, 9), (2, 9), (2, 8), (2, 7)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_3_3`:**
  - Raw ColorID: `3` (Purple)
  - Ant Count: `16` units
  - Head Coordinate: `(1, 5)`
  - Target Destination: `Purple Holder at entrance (1, 8)`
  - Complete Body Cells: `[(1, 5), (1, 4), (0, 4), (0, 3), (1, 3), (2, 3), (3, 3), (4, 3), (5, 3), (5, 2), (5, 1), (5, 0), (6, 0), (7, 0), (8, 0), (9, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_7_2`:**
  - Raw ColorID: `2` (Yellow)
  - Ant Count: `5` units
  - Head Coordinate: `(6, 4)`
  - Target Destination: `Yellow Holder at entrance (9, 4)`
  - Complete Body Cells: `[(6, 4), (5, 4), (4, 4), (3, 4), (3, 5)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_8_7`:**
  - Raw ColorID: `7` (Green)
  - Ant Count: `3` units
  - Head Coordinate: `(2, 2)`
  - Target Destination: `Green Holder at entrance (2, 5)`
  - Complete Body Cells: `[(2, 2), (2, 1), (2, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_9_3`:**
  - Raw ColorID: `3` (Purple)
  - Ant Count: `3` units
  - Head Coordinate: `(1, 2)`
  - Target Destination: `Purple Holder at entrance (1, 8)`
  - Complete Body Cells: `[(1, 2), (1, 1), (1, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_10_7`:**
  - Raw ColorID: `7` (Green)
  - Ant Count: `8` units
  - Head Coordinate: `(4, 5)`
  - Target Destination: `Green Holder at entrance (2, 5)`
  - Complete Body Cells: `[(4, 5), (5, 5), (5, 6), (4, 6), (4, 7), (4, 8), (5, 8), (5, 7)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_11_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `5` units
  - Head Coordinate: `(6, 2)`
  - Target Destination: `SkyBlue Holder at entrance (0, 2)`
  - Complete Body Cells: `[(6, 2), (7, 2), (8, 2), (8, 3), (9, 3)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_12_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `5` units
  - Head Coordinate: `(6, 1)`
  - Target Destination: `SkyBlue Holder at entrance (0, 2)`
  - Complete Body Cells: `[(6, 1), (7, 1), (8, 1), (9, 1), (9, 2)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_13_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `8` units
  - Head Coordinate: `(9, 7)`
  - Target Destination: `Indigo Holder at entrance (8, 9)`
  - Complete Body Cells: `[(9, 7), (9, 6), (9, 5), (8, 5), (7, 5), (7, 4), (7, 3), (6, 3)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_14_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `12` units
  - Head Coordinate: `(7, 9)`
  - Target Destination: `Indigo Holder at entrance (8, 9)`
  - Complete Body Cells: `[(7, 9), (6, 9), (5, 9), (4, 9), (3, 9), (3, 8), (3, 7), (3, 6), (2, 6), (1, 6), (0, 6), (0, 5)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_15_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `5` units
  - Head Coordinate: `(7, 8)`
  - Target Destination: `Indigo Holder at entrance (8, 9)`
  - Complete Body Cells: `[(7, 8), (6, 8), (6, 7), (6, 6), (6, 5)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_16_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `4` units
  - Head Coordinate: `(8, 7)`
  - Target Destination: `Indigo Holder at entrance (8, 9)`
  - Complete Body Cells: `[(8, 7), (8, 6), (7, 6), (7, 7)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_17_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `2` units
  - Head Coordinate: `(3, 2)`
  - Target Destination: `SkyBlue Holder at entrance (0, 2)`
  - Complete Body Cells: `[(3, 2), (4, 2)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_18_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `2` units
  - Head Coordinate: `(3, 1)`
  - Target Destination: `SkyBlue Holder at entrance (0, 2)`
  - Complete Body Cells: `[(3, 1), (4, 1)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_19_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `2` units
  - Head Coordinate: `(3, 0)`
  - Target Destination: `SkyBlue Holder at entrance (0, 2)`
  - Complete Body Cells: `[(3, 0), (4, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`

---

### LEVEL 4
- **Asset Path:** `Assets/Data/ReferenceLevels/Level_004.asset`
- **Grid Size:** `10x10`
- **Duration:** `0.0s` (unlimited, falls back to default 120s timer)
- **Difficulty:** `0`
- **HolderType 0 (Holders) Count:** `9`
- **HolderType 1 (Playable Queues) Count:** `14`
- **Distinct Raw ColorIDs Used:** `1 (Lime), 2 (Yellow), 4 (Orange), 5 (Pink), 6 (Indigo), 8 (Brown), 9 (White), 10 (Red), 11 (Turquoise)`

#### Holders (9 total)
- **Raw ColorID 10 (Red):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(0, 1), (1, 1)]`
  - Entrance Cell: `(0, 1)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 1 (Lime):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(0, 0), (1, 0)]`
  - Entrance Cell: `(0, 0)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 5 (Pink):**
  - Footprint Dimensions: `1x2`
  - Complete OccupiedCells: `[(8, 3), (8, 2)]`
  - Entrance Cell: `(8, 3)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 6 (Indigo):**
  - Footprint Dimensions: `1x2`
  - Complete OccupiedCells: `[(9, 3), (9, 2)]`
  - Entrance Cell: `(9, 3)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 4 (Orange):**
  - Footprint Dimensions: `1x2`
  - Complete OccupiedCells: `[(2, 1), (2, 0)]`
  - Entrance Cell: `(2, 1)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 8 (Brown):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(2, 9), (2, 8), (3, 8), (3, 9)]`
  - Entrance Cell: `(2, 9)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 2 (Yellow):**
  - Footprint Dimensions: `2x2`
  - Complete OccupiedCells: `[(4, 1), (4, 0), (5, 0), (5, 1)]`
  - Entrance Cell: `(4, 1)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 11 (Turquoise):**
  - Footprint Dimensions: `1x3`
  - Complete OccupiedCells: `[(8, 7), (8, 6), (8, 5)]`
  - Entrance Cell: `(8, 7)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 9 (White):**
  - Footprint Dimensions: `2x1`
  - Complete OccupiedCells: `[(7, 4), (8, 4)]`
  - Entrance Cell: `(7, 4)`
  - Initial Queue Counter: `1` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)

#### Playable Queues (14 total)
- **Queue `Record_0_1`:**
  - Raw ColorID: `1` (Lime)
  - Ant Count: `6` units
  - Head Coordinate: `(0, 7)`
  - Target Destination: `Lime Holder at entrance (0, 0)`
  - Complete Body Cells: `[(0, 7), (0, 8), (0, 9), (1, 9), (1, 8), (1, 7)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_1_10`:**
  - Raw ColorID: `10` (Red)
  - Ant Count: `6` units
  - Head Coordinate: `(0, 4)`
  - Target Destination: `Red Holder at entrance (0, 1)`
  - Complete Body Cells: `[(0, 4), (0, 5), (0, 6), (1, 6), (1, 5), (1, 4)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_2_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `3` units
  - Head Coordinate: `(2, 3)`
  - Target Destination: `Indigo Holder at entrance (9, 3)`
  - Complete Body Cells: `[(2, 3), (1, 3), (0, 3)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_3_5`:**
  - Raw ColorID: `5` (Pink)
  - Ant Count: `3` units
  - Head Coordinate: `(2, 2)`
  - Target Destination: `Pink Holder at entrance (8, 3)`
  - Complete Body Cells: `[(2, 2), (1, 2), (0, 2)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_9_4`:**
  - Raw ColorID: `4` (Orange)
  - Ant Count: `4` units
  - Head Coordinate: `(6, 1)`
  - Target Destination: `Orange Holder at entrance (2, 1)`
  - Complete Body Cells: `[(6, 1), (7, 1), (7, 0), (6, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_10_10`:**
  - Raw ColorID: `10` (Red)
  - Ant Count: `4` units
  - Head Coordinate: `(8, 1)`
  - Target Destination: `Red Holder at entrance (0, 1)`
  - Complete Body Cells: `[(8, 1), (9, 1), (9, 0), (8, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_12_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `9` units
  - Head Coordinate: `(2, 7)`
  - Target Destination: `Brown Holder at entrance (2, 9)`
  - Complete Body Cells: `[(2, 7), (2, 6), (2, 5), (2, 4), (3, 4), (3, 3), (3, 2), (3, 1), (3, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_13_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `6` units
  - Head Coordinate: `(4, 9)`
  - Target Destination: `Brown Holder at entrance (2, 9)`
  - Complete Body Cells: `[(4, 9), (5, 9), (6, 9), (7, 9), (8, 9), (9, 9)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_14_2`:**
  - Raw ColorID: `2` (Yellow)
  - Ant Count: `9` units
  - Head Coordinate: `(4, 2)`
  - Target Destination: `Yellow Holder at entrance (4, 1)`
  - Complete Body Cells: `[(4, 2), (4, 3), (4, 4), (4, 5), (3, 5), (3, 6), (3, 7), (4, 7), (4, 8)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_16_5`:**
  - Raw ColorID: `5` (Pink)
  - Ant Count: `6` units
  - Head Coordinate: `(7, 3)`
  - Target Destination: `Pink Holder at entrance (8, 3)`
  - Complete Body Cells: `[(7, 3), (6, 3), (5, 3), (5, 2), (6, 2), (7, 2)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_17_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `5` units
  - Head Coordinate: `(9, 4)`
  - Target Destination: `Indigo Holder at entrance (9, 3)`
  - Complete Body Cells: `[(9, 4), (9, 5), (9, 6), (9, 7), (9, 8)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_19_11`:**
  - Raw ColorID: `11` (Turquoise)
  - Ant Count: `4` units
  - Head Coordinate: `(7, 6)`
  - Target Destination: `Turquoise Holder at entrance (8, 7)`
  - Complete Body Cells: `[(7, 6), (6, 6), (5, 6), (4, 6)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_20_11`:**
  - Raw ColorID: `11` (Turquoise)
  - Ant Count: `7` units
  - Head Coordinate: `(7, 7)`
  - Target Destination: `Turquoise Holder at entrance (8, 7)`
  - Complete Body Cells: `[(7, 7), (6, 7), (5, 7), (5, 8), (6, 8), (7, 8), (8, 8)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_22_9`:**
  - Raw ColorID: `9` (White)
  - Ant Count: `5` units
  - Head Coordinate: `(6, 4)`
  - Target Destination: `White Holder at entrance (7, 4)`
  - Complete Body Cells: `[(6, 4), (5, 4), (5, 5), (6, 5), (7, 5)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`

---

### LEVEL 5
- **Asset Path:** `Assets/Data/ReferenceLevels/Level_005.asset`
- **Grid Size:** `15x15`
- **Duration:** `0.0s` (unlimited, falls back to default 120s timer)
- **Difficulty:** `0`
- **HolderType 0 (Holders) Count:** `6`
- **HolderType 1 (Playable Queues) Count:** `23`
- **Distinct Raw ColorIDs Used:** `0 (SkyBlue), 2 (Yellow), 5 (Pink), 6 (Indigo), 7 (Green), 8 (Brown)`

#### Holders (6 total)
- **Raw ColorID 0 (SkyBlue):**
  - Footprint Dimensions: `3x4`
  - Complete OccupiedCells: `[(0, 14), (0, 13), (0, 12), (0, 11), (1, 11), (2, 11), (2, 12), (2, 13), (2, 14), (1, 14), (1, 13), (1, 12)]`
  - Entrance Cell: `(0, 14)`
  - Initial Queue Counter: `4` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 7 (Green):**
  - Footprint Dimensions: `3x4`
  - Complete OccupiedCells: `[(14, 14), (13, 14), (12, 14), (12, 13), (12, 12), (12, 11), (13, 11), (14, 11), (14, 12), (14, 13), (13, 13), (13, 12)]`
  - Entrance Cell: `(14, 14)`
  - Initial Queue Counter: `4` (matches playable queues count)
  - Visual Orientation: `LeftSide` (isRightSide = `False`)
- **Raw ColorID 5 (Pink):**
  - Footprint Dimensions: `3x2`
  - Complete OccupiedCells: `[(6, 11), (7, 11), (8, 11), (8, 10), (7, 10), (6, 10)]`
  - Entrance Cell: `(6, 11)`
  - Initial Queue Counter: `4` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 8 (Brown):**
  - Footprint Dimensions: `3x2`
  - Complete OccupiedCells: `[(6, 3), (6, 4), (7, 4), (8, 4), (8, 3), (7, 3)]`
  - Entrance Cell: `(6, 3)`
  - Initial Queue Counter: `7` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 2 (Yellow):**
  - Footprint Dimensions: `3x4`
  - Complete OccupiedCells: `[(0, 0), (0, 1), (0, 2), (0, 3), (1, 3), (2, 3), (2, 2), (2, 1), (2, 0), (1, 0), (1, 1), (1, 2)]`
  - Entrance Cell: `(0, 0)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `RightSide` (isRightSide = `True`)
- **Raw ColorID 6 (Indigo):**
  - Footprint Dimensions: `3x4`
  - Complete OccupiedCells: `[(14, 3), (13, 3), (12, 3), (12, 2), (12, 1), (12, 0), (13, 0), (14, 0), (14, 1), (14, 2), (13, 2), (13, 1)]`
  - Entrance Cell: `(14, 3)`
  - Initial Queue Counter: `2` (matches playable queues count)
  - Visual Orientation: `LeftSide` (isRightSide = `False`)

#### Playable Queues (23 total)
- **Queue `Record_6_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `3` units
  - Head Coordinate: `(1, 10)`
  - Target Destination: `SkyBlue Holder at entrance (0, 14)`
  - Complete Body Cells: `[(1, 10), (1, 9), (1, 8)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_7_5`:**
  - Raw ColorID: `5` (Pink)
  - Ant Count: `3` units
  - Head Coordinate: `(8, 12)`
  - Target Destination: `Pink Holder at entrance (6, 11)`
  - Complete Body Cells: `[(8, 12), (8, 13), (8, 14)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_8_7`:**
  - Raw ColorID: `7` (Green)
  - Ant Count: `6` units
  - Head Coordinate: `(11, 14)`
  - Target Destination: `Green Holder at entrance (14, 14)`
  - Complete Body Cells: `[(11, 14), (10, 14), (9, 14), (9, 13), (10, 13), (11, 13)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_9_7`:**
  - Raw ColorID: `7` (Green)
  - Ant Count: `3` units
  - Head Coordinate: `(11, 12)`
  - Target Destination: `Green Holder at entrance (14, 14)`
  - Complete Body Cells: `[(11, 12), (10, 12), (9, 12)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_10_5`:**
  - Raw ColorID: `5` (Pink)
  - Ant Count: `6` units
  - Head Coordinate: `(9, 11)`
  - Target Destination: `Pink Holder at entrance (6, 11)`
  - Complete Body Cells: `[(9, 11), (10, 11), (11, 11), (11, 10), (10, 10), (9, 10)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_11_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `5` units
  - Head Coordinate: `(6, 7)`
  - Target Destination: `Brown Holder at entrance (6, 3)`
  - Complete Body Cells: `[(6, 7), (6, 8), (6, 9), (7, 9), (8, 9)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_12_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `6` units
  - Head Coordinate: `(8, 5)`
  - Target Destination: `Brown Holder at entrance (6, 3)`
  - Complete Body Cells: `[(8, 5), (8, 6), (7, 6), (6, 6), (6, 5), (7, 5)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_13_5`:**
  - Raw ColorID: `5` (Pink)
  - Ant Count: `4` units
  - Head Coordinate: `(7, 8)`
  - Target Destination: `Pink Holder at entrance (6, 11)`
  - Complete Body Cells: `[(7, 8), (7, 7), (8, 7), (8, 8)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_14_7`:**
  - Raw ColorID: `7` (Green)
  - Ant Count: `7` units
  - Head Coordinate: `(14, 10)`
  - Target Destination: `Green Holder at entrance (14, 14)`
  - Complete Body Cells: `[(14, 10), (14, 9), (14, 8), (13, 8), (12, 8), (11, 8), (11, 9)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_15_5`:**
  - Raw ColorID: `5` (Pink)
  - Ant Count: `4` units
  - Head Coordinate: `(12, 10)`
  - Target Destination: `Pink Holder at entrance (6, 11)`
  - Complete Body Cells: `[(12, 10), (13, 10), (13, 9), (12, 9)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_16_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `3` units
  - Head Coordinate: `(6, 1)`
  - Target Destination: `Brown Holder at entrance (6, 3)`
  - Complete Body Cells: `[(6, 1), (6, 0), (5, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_17_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `3` units
  - Head Coordinate: `(8, 1)`
  - Target Destination: `Brown Holder at entrance (6, 3)`
  - Complete Body Cells: `[(8, 1), (8, 0), (9, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_18_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `3` units
  - Head Coordinate: `(7, 2)`
  - Target Destination: `Brown Holder at entrance (6, 3)`
  - Complete Body Cells: `[(7, 2), (7, 1), (7, 0)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_19_2`:**
  - Raw ColorID: `2` (Yellow)
  - Ant Count: `3` units
  - Head Coordinate: `(4, 2)`
  - Target Destination: `Yellow Holder at entrance (0, 0)`
  - Complete Body Cells: `[(4, 2), (5, 2), (6, 2)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_20_2`:**
  - Raw ColorID: `2` (Yellow)
  - Ant Count: `17` units
  - Head Coordinate: `(0, 4)`
  - Target Destination: `Yellow Holder at entrance (0, 0)`
  - Complete Body Cells: `[(0, 4), (0, 5), (1, 5), (2, 5), (3, 5), (4, 5), (5, 5), (5, 4), (5, 3), (4, 3), (3, 3), (3, 2), (3, 1), (3, 0), (4, 0), (4, 1), (5, 1)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_21_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `4` units
  - Head Coordinate: `(4, 4)`
  - Target Destination: `Brown Holder at entrance (6, 3)`
  - Complete Body Cells: `[(4, 4), (3, 4), (2, 4), (1, 4)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_22_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `3` units
  - Head Coordinate: `(10, 2)`
  - Target Destination: `Indigo Holder at entrance (14, 3)`
  - Complete Body Cells: `[(10, 2), (9, 2), (8, 2)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_23_6`:**
  - Raw ColorID: `6` (Indigo)
  - Ant Count: `17` units
  - Head Coordinate: `(14, 4)`
  - Target Destination: `Indigo Holder at entrance (14, 3)`
  - Complete Body Cells: `[(14, 4), (14, 5), (13, 5), (12, 5), (11, 5), (10, 5), (9, 5), (9, 4), (9, 3), (10, 3), (11, 3), (11, 2), (11, 1), (11, 0), (10, 0), (10, 1), (9, 1)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_24_8`:**
  - Raw ColorID: `8` (Brown)
  - Ant Count: `4` units
  - Head Coordinate: `(10, 4)`
  - Target Destination: `Brown Holder at entrance (6, 3)`
  - Complete Body Cells: `[(10, 4), (11, 4), (12, 4), (13, 4)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_25_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `3` units
  - Head Coordinate: `(0, 10)`
  - Target Destination: `SkyBlue Holder at entrance (0, 14)`
  - Complete Body Cells: `[(0, 10), (0, 9), (0, 8)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_26_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `5` units
  - Head Coordinate: `(3, 12)`
  - Target Destination: `SkyBlue Holder at entrance (0, 14)`
  - Complete Body Cells: `[(3, 12), (4, 12), (5, 12), (6, 12), (7, 12)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_27_7`:**
  - Raw ColorID: `7` (Green)
  - Ant Count: `10` units
  - Head Coordinate: `(7, 14)`
  - Target Destination: `Green Holder at entrance (14, 14)`
  - Complete Body Cells: `[(7, 14), (6, 14), (5, 14), (4, 14), (3, 14), (3, 13), (4, 13), (5, 13), (6, 13), (7, 13)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`
- **Queue `Record_28_0`:**
  - Raw ColorID: `0` (SkyBlue)
  - Ant Count: `11` units
  - Head Coordinate: `(3, 11)`
  - Target Destination: `SkyBlue Holder at entrance (0, 14)`
  - Complete Body Cells: `[(3, 11), (4, 11), (5, 11), (5, 10), (4, 10), (3, 10), (2, 10), (2, 9), (2, 8), (3, 8), (3, 9)]`
  - Metadata: Connected = `(0, index 0)`, Locked = `(0, amt 0)`, PipeCount = `0`

---

## 8. Current Verified Color Mapping

### A. `AntColor` Enum Integer Values (`Assets/Scripts/Core/AntColor.cs`)
```csharp
public enum AntColor
{
    // Legacy serialized values
    Blue = 0,
    Green = 1,
    Purple = 2,
    Yellow = 3,
    Red = 4,
    Orange = 5,
    Cyan = 6,
    Pink = 7,
    White = 8,
    Black = 9,
    Brown = 10,

    // Extended distinct reference colors
    SkyBlue = 11,
    Lime = 12,
    Indigo = 13,
    Turquoise = 14
}
```

### B. Authoritative Raw Reference ColorID Mapping (`Assets/Scripts/Core/ReferenceColorMapper.cs`)
Raw reference ColorIDs are translated via `ReferenceColorMapper.ToAntColor(rawColorId)`:

| Raw ColorID | Distinct Color Name | Runtime `AntColor` Enum | Enum Int | Ant Material Name | Holder Rim Material | Base sRGB Color |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **0** | Sky Blue | `AntColor.SkyBlue` | 11 | `AntMat_SkyBlue` | `HoleRimMat_SkyBlue` | `RGBA(0.20, 0.70, 1.00, 1.00)` (`#33B3FF`) |
| **1** | Lime Green | `AntColor.Lime` | 12 | `AntMat_Lime` | `HoleRimMat_Lime` | `RGBA(0.48, 0.85, 0.15, 1.00)` (`#7BD926`) |
| **2** | Yellow | `AntColor.Yellow` | 3 | `AntMat_Yellow` | `HoleRimMat_Yellow` | `RGBA(1.00, 0.75, 0.05, 1.00)` (`#FFBF0D`) |
| **3** | Purple | `AntColor.Purple` | 2 | `AntMat_Purple` | `HoleRimMat_Purple` | `RGBA(0.58, 0.22, 0.88, 1.00)` (`#9438E0`) |
| **4** | Orange | `AntColor.Orange` | 5 | `AntMat_Orange` | `HoleRimMat_Orange` | `RGBA(1.00, 0.50, 0.05, 1.00)` (`#FF800D`) |
| **5** | Pink | `AntColor.Pink` | 7 | `AntMat_Pink` | `HoleRimMat_Pink` | `RGBA(0.98, 0.35, 0.75, 1.00)` (`#FA59BF`) |
| **6** | Indigo | `AntColor.Indigo` | 13 | `AntMat_Indigo` | `HoleRimMat_Indigo` | `RGBA(0.18, 0.28, 0.85, 1.00)` (`#2E47D9`) |
| **7** | Green | `AntColor.Green` | 1 | `AntMat_Green` | `HoleRimMat_Green` | `RGBA(0.12, 0.65, 0.22, 1.00)` (`#1FA638`) |
| **8** | Brown | `AntColor.Brown` | 10 | `AntMat_Brown` | `HoleRimMat_Brown` | `RGBA(0.55, 0.32, 0.17, 1.00)` (`#8C522B`) |
| **9** | White | `AntColor.White` | 8 | `AntMat_White` | `HoleRimMat_White` | `RGBA(0.92, 0.92, 0.92, 1.00)` (`#EBEBEB`) |
| **10** | Red | `AntColor.Red` | 4 | `AntMat_Red` | `HoleRimMat_Red` | `RGBA(0.95, 0.24, 0.26, 1.00)` (`#F23D42`) |
| **11** | Turquoise | `AntColor.Turquoise` | 14 | `AntMat_Turquoise` | `HoleRimMat_Turquoise` | `RGBA(0.10, 0.88, 0.78, 1.00)` (`#1AE0C7`) |
| **12+** | UNVERIFIED | `AntColor.White` | 8 | `AntMat_White` | `HoleRimMat_White` | `RGBA(1.00, 1.00, 1.00, 1.00)` (Fallback) |

**RGB Parity Confirmation:**
Verified 100% base RGB match between ant carapace materials and holder rims. Both resolve through `AntColorPalette.GetColor(ToAntColor(rawColorId))`.

---

## 9. Ant Visual System

- **Instantiation:**
  `BoardManager.SpawnReferenceAntQueue` instantiates an empty GameObject with `AntQueueController.cs`. For each coordinate in `record.Cells`, an `AntUnit.cs` child GameObject is spawned.
- **Worker Ant Mesh:** Loaded via `AntColorPalette.GetWorkerAntMesh()` from `Resources.Load<GameObject>('Ants/Prefabs/WorkerAnt')` or `Resources.Load<Mesh>('Ants/Meshes/WorkerAntMesh')`.
- **Model Transform:**
  - Child GameObject: `AntModel_3D`
  - LocalPosition: `(0, 0.05, 0)`
  - LocalRotation: `Quaternion.Euler(0, 180, 0)`
  - LocalScale: `(0.95, 0.95, 0.82)`
- **Head / Tail Determination:** `isHead = (index == 0)`, `isTail = (index == count - 1)`.
- **Expressive Googly Eyes:**
  - Parent `HeadPivot` at `(0, 0.25, 0.20)`.
  - Googly Eye scale: `0.22` for leader head, `0.18` for body units.
  - Sclera sphere + dark pupil disc. Pupils dynamically track `PlayerInputHandler.MouseWorldPosition` within eye sockets.
- **Orientation Logic:** Segments rotate to face their traversal direction during slither movement (`Quaternion.Slerp` toward movement delta).
- **Material Assignment:** `bodyRenderer.material = AntColorPalette.GetMaterial(antColor)` with URP Lit shader, base color tint, and texture from `Resources/Ants/Textures/`.
- **Lifecycle Protections:** `AntUnit` includes `BuildVisuals()` guarded against redundant creation when already initialized, and safe destruction helpers (`Destroy` during play mode, `DestroyImmediate` in edit mode).

---

## 10. Holder / Hole Visual System

### Hierarchy under a Spawned Holder
```text
Hole_<ColorID>_<X>_<Y> (HoleController.cs)
  └── HoleSlotRoot (Transform)
        ├── SlotRim (MeshFilter + MeshRenderer)
        ├── SlotInnerBevel (MeshFilter + MeshRenderer)
        ├── SlotPit (MeshFilter + MeshRenderer)
        └── SlotCounterBadge (GameObject)
              ├── BadgeMesh (MeshFilter + MeshRenderer)
              └── BadgeCountText (TextMesh)
```

- **Footprint Determination:** Bounding box of `OccupiedCells`: `WidthCells = MaxX - MinX + 1`, `HeightCells = MaxY - MinY + 1`.
- **World Center:** Centered on the grid footprint: `((minX + maxX) * 0.5f * cs, 0, (minY + maxY) * 0.5f * cs)`.
- **Dimensions & Margins:**
  - `outerW = effectiveW * cs - 0.06f * cs`
  - `outerH = effectiveH * cs - 0.06f * cs`
  - `cornerRadius = Mathf.Min(0.48f * cs, outerH * 0.48f)`
  - Rim thickness: `innerW = outerW - 0.20f * cs`, `innerH = outerH - 0.20f * cs`
- **Visual Layering (Y Offsets):**
  - Outer Rim (`SlotRim`): Y = `0.032` (highest layer, gloss 0.90)
  - Inner Bevel (`SlotInnerBevel`): Drops from Y = `0.030` to Y = `0.016`, shaded darker (`Color.Lerp(baseColor, Color.black, 0.38f)`)
  - Slot Pit Floor (`SlotPit`): Y = `0.015`, recessed black floor
- **Badge Positioning:** Positioned along the rim perimeter based on slot orientation:
  - Horizontal (`W > H`): Right edge `(outerW * 0.5f - 0.20f * cs, 0, 0)`
  - Vertical (`H > W`): Bottom edge `(0, 0, -(outerH * 0.5f - 0.20f * cs))`
  - Square / Box: Bottom-right `(outerW * 0.5f - 0.22f * cs, 0, -(outerH * 0.5f - 0.22f * cs))`

---

## 11. Single-Queue Holder Visual Override

Implemented in `HoleController.ApplySingleQueueHolderVisualOverride()`:
- **Condition:** Applied when `this.initialQueueCount == 1`.
- **Exact Serialized Scale & Position Overrides:**
  - `SlotRim` localScale X = `1.9205`
  - `SlotInnerBevel` localScale X = `2.3861`
  - `SlotPit` localScale X = `2.6535`
  - `SlotCounterBadge` localPosition = `(0.35, 0.00, 0.00)`
  - `BadgeMesh` localPosition = `(0.25, 0.00, -0.25)`
  - `BadgeCountText` localPosition = `(0.25, 0.22, -0.25)`

---

## 12. Holder Counter System

- **What Numbers Represent:** The remaining number of matching playable queues that must enter this holder to complete it.
- **Initial Calculation:** `ReferenceLevelRuntimeAdapter.ExtractHoles()` counts all `HolderType == 1` queues sharing the same `colorId`.
- **Decrement Logic:**
  - Occurs in `HoleController.OnQueueSwallowed()`.
  - Individual ants DO NOT decrement the counter.
  - Decrement occurs ONLY when an entire queue finishes entering the holder (`isQueueBasedCount = true`).
- **Badge Text Update:** Displayed via 3D TextMesh `countTextMesh.text = remainingCount.ToString()`.
- **Zero Counter Event:** When `remainingCount <= 0`, `isCompleted = true`, triggers `BoardManager.RemoveCompletedHole(this)`.
- **Expected Initial Counters for Levels 1–5:**
  - **Level 1:** All 5 holders have initial counter = `1` (Yellow=1, Green=1, SkyBlue=1, Brown=1, Red=1).
  - **Level 2:** All 5 holders have initial counter = `2` (Red=2, SkyBlue=2, Lime=2, Indigo=2, Orange=2).
  - **Level 3:** Purple=`2`, SkyBlue=`6`, Indigo=`4`, Green=`2`, Yellow=`1`.
  - **Level 4:** Lime=`1`, Pink=`2`, Indigo=`2`, Orange=`1`, Brown=`2`, Turquoise=`2`, White=`1`, Red=`2`, Yellow=`1`.
  - **Level 5:** SkyBlue=`4`, Green=`4`, Pink=`4`, Brown=`7`, Yellow=`2`, Indigo=`2`.

---

## 13. Holder Completion / Disappearance

### Exact Completion Sequence (`HoleController.cs` & `BoardManager.cs`)
1. An ant queue finishes entering its target holder -> `BoardManager.OnQueueCompleted(queue)`.
2. Cached target holder receives `hole.OnQueueSwallowed()`.
3. `remainingCount` decrements to `0` -> marked `isCompleted = true`.
4. `BoardManager.RemoveCompletedHole(hole)` is invoked immediately:
   - `activeHoles.Remove(hole)`.
   - `completedHoleCount++`.
   - `completedHoleIds.Add(hole.HoleId)` records stable string ID.
   - For every coordinate in `hole.OccupiedCells`: `holesMap.Remove(cell)`.
   - Defensive sweep purges any lingering mappings pointing to this hole.
   - `OnBoardStateChanged?.Invoke()` triggers game state check and releases walkable cells.
5. `PlayCompletedEffectAndDisappear()` executes:
   - Disables all colliders immediately so no clicks hit dying holders.
   - Emits diagnostic log `[HoleCompleted]`.
   - Animates root transform scale to 0 (`0.20s`, `Ease.InBack`).
   - Destroys GameObject (`SafeDestroy(gameObject)`).

### Safety Verifications
- **Grid Release Timing:** Grid cells become walkable **BEFORE** visual destruction. `holesMap` is cleared synchronously inside `RemoveCompletedHole()` prior to starting the tween or calling `Destroy()`.
- **Survival of Completion State:** `BoardManager` tracks completion using `completedHoleCount` and `completedHoleIds` (stable string IDs, not GameObject references). Win condition evaluation relies on `activeQueues.Count == 0`, ensuring zero dependency on destroyed `HoleController` objects.
- **Null Safety:** `BoardManager.GetHoleAt(coord)` safely purges stale or completed mappings:
  ```csharp
  if (!holesMap.TryGetValue(coord, out var hole)) return null;
  if (hole == null || hole.IsCompleted) { holesMap.Remove(coord); return null; }
  return hole;
  ```

---

## 14. Grid Occupancy System

### Key Collections in `BoardManager.cs`
- `activeQueues` (`List<AntQueueController>`): Currently active queues on the board.
- `activeHoles` (`List<HoleController>`): Currently active holders.
- `holesMap` (`Dictionary<Vector2Int, HoleController>`): Spatial grid coordinate to holder mapping.
- `completedHoleIds` (`HashSet<string>`): Set of completed holder IDs.
- `completedHoleCount` (`int`): Counter of completed holders.

### Cell State Determination (`IsCellFree(Vector2Int coord)`)
A cell `coord` is considered **free and walkable** if:
1. `coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height` (within grid bounds)
2. `!CurrentLevel.GetCombinedDisabledCells().Contains(coord)` (not disabled by level data)
3. `GetHoleAt(coord) == null` (not occupied by an active holder)
4. `GetQueueOccupying(coord) == null` (not occupied by any active ant queue body segment)

---

## 15. Pathfinding System

### Implementation (`Assets/Scripts/Board/AntPathfinder.cs`)
- **Algorithm:** Deterministic 4-directional Breadth-First-Search (BFS).
- **Start Coordinate:** Queue leader head `queue.CurrentBodyCoordinates[0]`.
- **Target Goal:** Footprint cells of all matching holders where `hole.ColorID == queue.ColorID && !hole.IsFull`.
- **Blocked Cells (Obstacles):**
  - Other active ant queues (`otherQueue.CurrentBodyCoordinates`).
  - Non-matching or full holes in `board.HolesMap`.
  - Disabled grid cells from `level.GetCombinedDisabledCells()`.
  - HolderType 2 obstacles.
  - Own queue body segments behind the head (indices 1 to N-1).
- **Dynamic Calculation:** Paths are calculated dynamically at the instant of player click. If no path exists, returns `null` and triggers `OnQueueBlockedFeedback` (wiggle shake).

---

## 16. Queue Movement System

### Implementation (`Assets/Scripts/Ants/AntQueueController.cs`)
- **Movement Routine:** `StartMoveAlongPath(List<Vector2Int> path)` launches `MoveRoutine()`.
- **Precomputed Ant Tracks:** Each ant segment follows the vacated track of the ant ahead of it in a follow-the-leader chain.
- **Hole Ingestion:** When reaching the holder entrance, segments perform a smooth dive into the tray with scale reduction and burst VFX (`ant_dis.prefab`).
- **Destination Caching:** `destinationHole` is cached on the queue at move start.
- **Completion Event:** At the end of `MoveRoutine()`, `OnQueueMoveCompleted?.Invoke(this)` fires and `BoardManager.Instance.OnQueueCompleted(this)` is called.

---

## 17. Input System

### Implementation (`Assets/Scripts/Core/PlayerInputHandler.cs`)
- **Pointer Tracking:** Screen touches or mouse clicks converted to rays via `mainCamera.ScreenPointToRay`.
- **Raycast Layer 1 (Direct Physics):** `Physics.Raycast` against `interactableLayers` targeting `AntUnit` colliders.
- **Raycast Layer 2 (Ground Plane):** Raycast against ground mathematical plane `Plane(Vector3.up, Vector3.zero)` converting world hit to grid coordinate `WorldToGridPosition` to eliminate gaps between segments.
- **Input Blocking:** Interaction is blocked (`canInteract = false`) while any queue is moving (`movingQueuesCount > 0`) or when modals/panels are open.

---

## 18. Game State & GameManager

### Implementation (`Assets/Scripts/Core/GameManager.cs`)
- **GameState Enum:**
  ```csharp
  public enum GameState { NotStarted, Playing, LevelWon, LevelLost }
  ```
- **Timer:** Decrements every frame during `GameState.Playing`. Fires `OnTimerUpdated`.
- **Exact Win Condition:**
  `BoardManager.Instance.ActiveQueues.Count == 0`
  (All ant queues cleared. Completely independent of destroyed HoleController objects).
- **Exact Loss Conditions:**
  1. `remainingTimer <= 0f` ('Time ran out!').
  2. Deadlock: `activeQueues.Count > 0` and no queue has an unblocked path (`!anyCanMove`).

---

## 19. UI System

### Implementation (`Assets/Scripts/UI/GameUIManager.cs`) & Scene Canvas
- **Top Card HUD (`TopCardHUD`):**
  - Level number text (`levelNumberText`).
  - Timer display (`timerText` formatted as `M:SS`).
  - Heart lives (`heartImages[0..2]`).
  - Restart button (`restartButton` calling `GameManager.Instance.RestartLevel()`).
  - Settings button (`gearSettingsButton` opening modal).
- **Win Popup (`WinPanel`):**
  - Activated when `GameState == GameState.LevelWon`.
  - Scaled from 0.7 to 1.0 with `Ease.OutBack`.
  - Next Level button calling `GameManager.Instance.NextLevel()`.
- **Level Failed Popup (`LosePanel`):**
  - **STATUS: IMPLEMENTED** in `GameUIManager.cs` and present in `Assets/Scenes/main.unity` (GameObject ID: `7623599`).
  - Hierarchy:
    ```text
    LosePanel (Image with dark overlay RGBA(0.08, 0.1, 0.14, 0.88))
      ├── Title (Text: 'LEVEL FAILED' / lose reason)
      └── ActionButton (Button with ButtonText: 'RETRY')
    ```
  - Showing mechanism: On `GameState.LevelLost`, scales up from 0.7 to 1.0 (`0.35s`, `Ease.OutBack`).
  - Retry button is wired to `GameManager.Instance.RestartLevel()`.
  - Raycast blocking is active.

---

## 20. Camera System

### Implementation (`Assets/Scripts/Camera/BoardCameraController.cs`)
- **Projection Mode:** Dual-mode; default `Orthographic` with reference-accurate pitch of `89°`.
- **Framing Algorithm:**
  - Computes content bounding box combining board grid dimensions and outer holder footprints.
  - Accounts for UI margins (`targetHorizontalOccupancy = 0.78f`, `topViewportMargin = 0.18f`, `bottomViewportMargin = 0.08f`).
  - Operates dynamically per level without inheriting zoom state.
- **Engine Switch:** Supports Cinemachine 3.x (`CinemachineCamera`, `CinemachineBrain`) with direct fallback to `Camera.main`.

---

## 21. Scene Hierarchy (`Assets/Scenes/main.unity`)

### Real Root GameObjects (9 total)
1. `GameController` (ID: `35470740`): Holds `LevelManager`, `GameManager`, `BoardManager`, `PlayerInputHandler`.
2. `GameUICanvas` (ID: `1955814692`): Holds `GameUIManager`, `TopCardHUD`, `WinPanel`, `LosePanel`, `SettingsModal`.
3. `Board` (ID: `1986304593`): Transform parent for runtime grid objects.
4. `Ants` (ID: `260101787`): Legacy queue container.
5. `Main Camera` (ID: `330585543`): Unity Camera component.
6. `Camera` (ID: `865263599`): Cinemachine camera rig.
7. `Directional Light` (ID: `410087039`)
8. `EventSystem` (ID: `2048140430`)
9. `FoodBoxes` (ID: `884646043`): Environment props.

### Baked Runtime Objects Audit
- **Findings:** The serialized scene `main.unity` currently contains **626 baked runtime objects** from earlier edit-mode testing or play-mode saves:
  - 546 `ant_dis(Clone)` VFX objects at root level.
  - 11 children under `Board`: `Hole_Green_6_3`, `AntQueue_Yellow_Slither_Yellow`, `Hole_Red_1_0`, `Hole_Yellow_6_4`, `AntQueue_Blue_Slither_Blue`, `TilesHolder`, `Hole_Blue_6_2`, `AntQueue_Brown_Slither_Brown`, `AntQueue_Red_Slither_Red`, `AntQueue_Green_Slither_Green`.
- **Runtime Impact:** Handled cleanly at runtime startup: `BoardManager.SetupBoard()` executes `ClearBoard()` which destroys all children under `boardParent` before spawning fresh objects. However, the scene file itself contains these remnants.

---

## 22. Prefabs / Resources

### Gameplay Prefabs (`Assets/Prefabs/`)
- `Assets/Prefabs/AntQueue.prefab`: Runtime queue controller template.
- `Assets/Prefabs/AntUnit.prefab`: Ant unit segment template.
- `Assets/Prefabs/Hole_Stadium.prefab` / `Hole.prefab`: Holder template.
- `Assets/Prefabs/VFX/ant_dis.prefab`: Swallowing burst particle effect.
- `Assets/Prefabs/VFX/antarrow.prefab`: Directional indicator effect.

### Resources (`Assets/Resources/`)
- Meshes: `Assets/Resources/Ants/Meshes/WorkerAntMesh.asset`
- Prefabs: `Assets/Resources/Ants/Prefabs/WorkerAnt.prefab`
- Textures: `Assets/Resources/Ants/Textures/` (`blue0.png`, `green0.png`, `yellow0.png`, `red0.png`, `orange0.png`, `pink0.png`, `purple0.png`, `teal0.png`, `white0.png`, `brown0.png`, `ant_shadowblack.png`, etc.)

---

## 23. Editor Tools & Dangerous Utilities

| Script | Location | Type | Capabilities / Danger Level | Safety Advice |
| :--- | :--- | :--- | :--- | :--- |
| **`AutoAssetInitializer.cs`** | `Assets/Scripts/Editor/` | Manual (Disabled) | Auto-initialization is disabled. Can call `PrefabCreationUtility` and `LevelGeneratorWindow`. | Safe/Inert. Do not re-enable auto-hooks. |
| **`LevelGeneratorWindow.cs`** | `Assets/Scripts/Editor/` | Manual (Disabled) | Can generate synthetic sample levels and overwrite `LevelDatabase.asset` or bake objects. MenuItems are commented out. | **DANGEROUS**. Do NOT run casually. |
| **`ReferenceLevelImporter.cs`** | `Assets/Scripts/Editor/` | Menu Tool | Has `Import Reference Levels` and `Validate Reference Levels`. Import overwrites reference assets. | **DANGEROUS**. Run validation only; do NOT run import without approval. |
| **`SceneSetupUtility.cs`** | `Assets/Scripts/Editor/` | Manual (Disabled) | Legacy scene setup. MenuItem disabled. | Inert. Do not run. |
| **`TestLevels1To5Validation.cs`** | `Assets/Scripts/Editor/` | Menu Tool | Validates Levels 1–5, color consistency, and holder removal. Completely read-only. | Safe to run. |

---

## 24. Automatic Execution Hooks

Audit of edit-mode / execution hooks across `Assets/Scripts`:
1. `Assets/Scripts/Camera/BoardCameraController.cs:18`: `[ExecuteAlways]` — Used for dynamic camera framing in scene view and edit mode.
2. `Assets/Scripts/Environment/GardenBackgroundManager.cs:4`: `[ExecuteAlways]` — Used for background framing in scene view.
3. `OnValidate()` hooks in `AntQueueController.cs`, `AntUnit.cs`, `HoleController.cs`: Guarded with `EditorApplication.delayCall` and `!Application.isPlaying`.

---

## 25. Test System

### EditMode Test Suites (`Assets/Tests/EditMode/`)
- **`ReferenceLevels1To5Tests.cs` (8 Tests):**
  - `Test01`: Asserts level asset loading from LevelDatabase (see Issue-001 below).
  - `Test02`: Verifies all 12 distinct reference colors without collapsing.
  - `Test03`: Verifies total holder capacity equals playable queues count for Levels 1–5.
  - `Test04`: Verifies multi-cell holder footprints and dimensions calculation.
  - `Test05`: Verifies centered holder placement and badge initialization.
  - `Test06`: Verifies single-queue holder visual overrides on exact transforms.
  - `Test07`: Verifies 100% RGB base color parity between ant units and holder rims.
  - `Test08`: Verifies Level 1 holder removal, full 2x1 footprint release, counter zeroing, unmapping from `holesMap`, and win condition.
- **`LevelDatabaseTests.cs`:** Asserts LevelDatabase asset validity and count.
- **`BoardCameraControllerTests.cs`:** Validates camera math and bounds.

**Test Safety:** All tests instantiate temporary in-memory GameObjects and clean them up with `Object.DestroyImmediate()` in `finally` blocks. They are 100% read-only with respect to disk assets and scenes.

---

## 26. Current Compile Status

- **Compile Errors:** `0`
- **Compiler Warnings:** `8`

### Warning List
1. `Assets/Scripts/Board/BoardManager.cs(18,35)`: `CS0414` — Field `preserveSceneObjects` is assigned but its value is never used.
2. `Assets/Scripts/Board/HoleController.cs(19,36)`: `CS0414` — Field `singleRowWidth` is assigned but its value is never used.
3. `Assets/Scripts/Board/HoleController.cs(20,36)`: `CS0414` — Field `singleRowHeight` is assigned but its value is never used.
4. `Assets/Scripts/Board/HoleController.cs(21,36)`: `CS0414` — Field `multiRowWidth` is assigned but its value is never used.
5. `Assets/Scripts/Board/HoleController.cs(22,36)`: `CS0414` — Field `multiRowHeight` is assigned but its value is never used.
6. `Assets/Scripts/Board/HoleController.cs(23,36)`: `CS0414` — Field `tubeRadius` is assigned but its value is never used.
7. `Assets/Scripts/Board/HoleController.cs(24,36)`: `CS0414` — Field `tubeHeight` is assigned but its value is never used.
8. `Assets/Scripts/Ants/AntQueueController.cs(9,25)`: `CS0414` — Field `s_DebugLoggedOneRefAnt` is assigned but its value is never used.

---

## 27. Current Runtime Errors

- **Unity Editor Logs Audit (`Editor.log`):**
  - Gameplay exceptions / crashes: `0`
  - NullReferenceExceptions: `0`
  - Unhandled assertion failures: `0`
  - Standard Unity engine licensing 404 queries (normal client licensing probe) and ADB device scans.
  - Verified clean execution of game loop diagnostics: `[ColorMap]`, `[ColorConsistency] MATCH=True`, and `[HoleCompleted]`.

---

## 28. Known Working Features

- **Reference level loading from `Assets/Data/Levels/LevelDatabase.asset`:** VERIFIED WORKING
- **Dynamic parsing via `ReferenceLevelRuntimeAdapter.cs`:** VERIFIED WORKING
- **12 distinct raw reference ColorIDs via `ReferenceColorMapper.cs`:** VERIFIED WORKING
- **100% RGB base color parity between ant carapaces and holder rims:** VERIFIED WORKING
- **Procedural stadium slot mesh generation (`SlotRim`, `SlotInnerBevel`, `SlotPit`, `SlotCounterBadge`):** VERIFIED WORKING
- **Single-queue visual override (`ApplySingleQueueHolderVisualOverride`):** VERIFIED WORKING
- **Deterministic 4-directional BFS pathfinding (`AntPathfinder.cs`):** VERIFIED WORKING
- **Queue slithering movement along path:** VERIFIED WORKING
- **Queue swallowing into holder with DOTween & burst VFX:** VERIFIED WORKING
- **Holder queue-based counter decrement on queue ingestion:** VERIFIED WORKING
- **Holder removal & multi-cell footprint grid release on counter reaching 0:** VERIFIED WORKING
- **Win condition evaluation (`activeQueues.Count == 0`) independent of destroyed holders:** VERIFIED WORKING
- **Deadlock detection & level failure on blocked queues:** VERIFIED WORKING
- **Level restart via `GameManager.Instance.RestartLevel()`:** VERIFIED WORKING
- **Dynamic camera auto-framing (`BoardCameraController.cs`):** VERIFIED WORKING
- **Level Failed popup (`LosePanel`):** IMPLEMENTED BUT NOT VERIFIED in live play mode.

---

## 29. Known Issues

### ISSUE-001: Grid Dimension Test Discrepancy
- **Problem:** `ReferenceLevels1To5Tests.cs` (Test01) and `TestLevels1To5Validation.cs` assert expected grid dimensions `{ 8x10, 12x10, 14x10, 14x12, 14x12 }`, whereas the actual authoritative reference level assets in `Assets/Data/ReferenceLevels/` contain `{ 8x5, 10x10, 10x10, 10x10, 15x15 }`.
- **Evidence:** `Level_001.asset` has `gridWidth: 8, gridHeight: 5`; `Level_002` to `Level_004` have `10x10`; `Level_005` has `15x15`.
- **Affected Files:** `Assets/Tests/EditMode/ReferenceLevels1To5Tests.cs`, `Assets/Scripts/Editor/TestLevels1To5Validation.cs`.
- **Severity:** Medium (Causes Test01 to fail against genuine reference assets).
- **Suggested Next Inspection:** Update `Test01` and `TestLevels1To5Validation.cs` to assert the verified reference asset dimensions.

### ISSUE-002: Serialized Scene Contains 626 Baked Remnant Objects
- **Problem:** `Assets/Scenes/main.unity` contains 546 `ant_dis(Clone)` instances at root level and 11 baked children under `Board` (`Hole_*`, `AntQueue_*`, `TilesHolder`).
- **Evidence:** Verified by direct scene parsing.
- **Affected Files:** `Assets/Scenes/main.unity`.
- **Severity:** Low (Cleaned dynamically at runtime by `BoardManager.ClearBoard()`, but adds bloat to the scene file).
- **Suggested Next Inspection:** Perform a dedicated clean scene save without runtime objects baked in.

---

## 30. Current Safety Rules

1. **DO NOT run `LevelGeneratorWindow`** — it can generate synthetic sample levels and overwrite `LevelDatabase.asset`.
2. **DO NOT run `Import Reference Levels`** in `ReferenceLevelImporter.cs` casually — it can overwrite recovered assets.
3. **DO NOT bake runtime objects into `main.unity`** — board objects must be spawned purely at runtime.
4. **DO NOT mutate raw reference `.asset` files** — `ReferenceLevelRuntimeAdapter.cs` must translate them dynamically in memory.
5. **DO NOT alter `.meta` files or GUIDs** — doing so breaks database references.
6. **DO NOT change Levels 6–45** while testing Levels 1–5.
7. **Source code and serialized data beat documentation** — verify against actual files before assuming context notes are current.

---

## 31. Important File Map

| FILE | ROLE | RUNTIME / EDITOR | IMPORTANT NOTES |
| :--- | :--- | :---: | :--- |
| **`Assets/Scripts/LevelSystem/LevelManager.cs`** | Level progression & loading | Runtime | Evaluates `ActiveDatabase`, manages PlayerPrefs progression. |
| **`Assets/Scripts/LevelSystem/LevelDatabase.cs`** | Level collection ScriptableObject | Runtime | Contains ordered list of all 45 playable level assets. |
| **`Assets/Scripts/LevelSystem/Level.cs`** | Level data ScriptableObject | Runtime | Holds grid dimensions, queues, cell info, duration, difficulty. |
| **`Assets/Scripts/LevelSystem/AntQueueData.cs`** | Serialized record structure | Runtime | Used for both HolderType 0 (holders) and HolderType 1 (queues). |
| **`Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs`** | Dynamic reference data translator | Runtime | Extracts holes and queues dynamically without mutating assets. |
| **`Assets/Scripts/Core/ReferenceColorMapper.cs`** | ColorID to AntColor mapper | Runtime | Authoritative 0..11 translation layer; preserves raw IDs. |
| **`Assets/Scripts/Core/AntColor.cs`** | Color enum & `AntColorPalette` | Runtime | Calibrated sRGB colors, material generation with URP Lit shaders. |
| **`Assets/Scripts/Board/BoardManager.cs`** | Core board manager | Runtime | Manages grid occupancy, `holesMap`, active lists, `RemoveCompletedHole()`. |
| **`Assets/Scripts/Board/HoleController.cs`** | Stadium slot controller | Runtime | Procedural meshes, single-queue overrides, badge text, queue swallowing. |
| **`Assets/Scripts/Board/AntPathfinder.cs`** | BFS pathfinder | Runtime | Deterministic 4-directional pathfinding respecting dynamic occupancy. |
| **`Assets/Scripts/Ants/AntQueueController.cs`** | Queue movement & slither chain | Runtime | Follow-the-leader coordinate traversal, destination caching. |
| **`Assets/Scripts/Ants/AntUnit.cs`** | Ant segment visual & model | Runtime | Worker ant mesh, googly eyes tracking pointer, swallow animations. |
| **`Assets/Scripts/Core/PlayerInputHandler.cs`** | Input detection & dispatch | Runtime | Dual-layer physics and ground plane raycasting; handles click input. |
| **`Assets/Scripts/Core/GameManager.cs`** | Game state machine | Runtime | State transitions (`Playing`, `LevelWon`, `LevelLost`), deadlock detection. |
| **`Assets/Scripts/UI/GameUIManager.cs`** | UI manager | Runtime | Manages top HUD, WinPanel, LosePanel, Settings modal. |
| **`Assets/Scripts/Camera/BoardCameraController.cs`** | Camera framing controller | Runtime (`[ExecuteAlways]`) | Dual-mode Orthographic/Perspective framing; dynamic bounds math. |
| **`Assets/Scripts/Editor/TestLevels1To5Validation.cs`** | Editor validation tools | Editor | Menu tools for validating levels 1–5, colors, and holder removal. |
| **`Assets/Tests/EditMode/ReferenceLevels1To5Tests.cs`** | EditMode test suite | Editor / Tests | 8 comprehensive tests covering database, colors, capacity, footprints, release. |

---

## 32. Recent Implementation State

- **Git Status:** Clean working tree on `main` branch.
- **Recent Commit:** `037a1c0d9d4c70f8e878d2d5f63fa724425a9693`
- **Recent Work Accomplished:**
  - Implemented holder removal and full multi-cell footprint grid release upon counter reaching 0.
  - Resolved 100% RGB parity between ant carapaces and holder rims via `ReferenceColorMapper`.
  - Added comprehensive test suite `ReferenceLevels1To5Tests.cs` (Tests 1–8).
  - Cleaned up obsolete legacy files.

---

## 33. Source of Truth / Conflict Report

### CONFLICT 1: Test Grid Dimensions vs Actual Reference Assets
- **Context / Tests Say:** `ReferenceLevels1To5Tests.cs` (Test01) and `TestLevels1To5Validation.cs` expect grid sizes `{ 8x10, 12x10, 14x10, 14x12, 14x12 }`.
- **Source / Assets Say:** `Level_001.asset` has `8x5`, `Level_002` has `10x10`, `Level_003` has `10x10`, `Level_004` has `10x10`, `Level_005` has `15x15`.
- **Current Truth:** The actual `.asset` files in `Assets/Data/ReferenceLevels/` are the authoritative data.
- **Recommended Correction:** Update `Test01` and `TestLevels1To5Validation.cs` to assert `{ 8x5, 10x10, 10x10, 10x10, 15x15 }`.

### CONFLICT 2: Clean Scene Expectation vs Baked Scene Content
- **Context Says:** Scene should not have runtime objects baked in.
- **Source / Assets Say:** `Assets/Scenes/main.unity` contains 546 `ant_dis(Clone)` instances and 11 baked children under `Board`.
- **Current Truth:** Leftover baked objects exist in the serialized scene file, but are purged dynamically at runtime startup by `BoardManager.ClearBoard()`.
- **Recommended Correction:** Re-save `main.unity` cleanly in edit mode without baked children.

---

# HANDOFF SUMMARY FOR CHATGPT

- **PROJECT:** Slither Ants Jam — Top-down 3D Unity puzzle game (Unity `6000.3.23f1`, URP). Players click caterpillar-style ant queues to guide them into matching color stadium holders on a grid.
- **CURRENT LEVEL PIPELINE:** ScriptableObject driven. `LevelManager` loads from the active database; `ReferenceLevelRuntimeAdapter` detects reference levels (`IsReferenceLevel()`) and extracts holders (`HolderType 0`) and playable queues (`HolderType 1`) dynamically into `BoardManager` without mutating asset files.
- **ACTIVE DATABASE:** `Assets/Data/Levels/LevelDatabase.asset` containing all 45 reference levels (`Assets/Data/ReferenceLevels/Level_001.asset` through `Level_045.asset`). `useReferenceLevelDatabase` is `false` in scene, so normal database path is active.
- **LEVELS 1–5:**
  - Level 1: 8x5 grid, 5 holders, 5 queues. Colors: SkyBlue (0), Yellow (2), Green (7), Brown (8), Red (10).
  - Level 2: 10x10 grid, 5 holders, 10 queues. Colors: SkyBlue (0), Lime (1), Orange (4), Indigo (6), Red (10).
  - Level 3: 10x10 grid, 5 holders, 15 queues. Colors: SkyBlue (0), Yellow (2), Purple (3), Indigo (6), Green (7).
  - Level 4: 10x10 grid, 9 holders, 14 queues. Colors: Lime (1), Yellow (2), Orange (4), Pink (5), Indigo (6), Brown (8), White (9), Red (10), Turquoise (11).
  - Level 5: 15x15 grid, 6 holders, 23 queues. Colors: SkyBlue (0), Yellow (2), Pink (5), Indigo (6), Green (7), Brown (8).
- **RAW COLOR MAPPING:** Authoritative 0..11 mapping via `ReferenceColorMapper.cs`: 0=SkyBlue, 1=Lime, 2=Yellow, 3=Purple, 4=Orange, 5=Pink, 6=Indigo, 7=Green, 8=Brown, 9=White, 10=Red, 11=Turquoise, 12+=White (Fallback). 100% RGB base color parity between ant carapaces and holder rims.
- **HOLDER SYSTEM:** Procedural stadium slot mesh (`SlotRim`, `SlotInnerBevel`, `SlotPit`, `SlotCounterBadge`). For initial count == 1, single-queue visual overrides are applied: Rim scale X = 1.9205, InnerBevel scale X = 2.3861, Pit scale X = 2.6535, Badge pos = (0.35, 0, 0).
- **QUEUE SYSTEM:** Segment-by-segment follow-the-leader chain movement (`AntQueueController.cs`). Uses 3D ant models (`AntUnit.cs`) with googly eyes tracking cursor. Destination holder cached upon move start.
- **PATHFINDING:** Deterministic 4-directional BFS (`AntPathfinder.cs`) finding shortest path from head to matching non-full holder entrance. Evaluates dynamic queue occupancy, disabled cells, and active holders.
- **HOLDER ZERO BEHAVIOR:** When remaining count reaches 0, `isCompleted = true`. `BoardManager.RemoveCompletedHole()` immediately unregisters all multi-cell footprint coordinates from `holesMap` before visual shrink animation (`0.20s`, `Ease.InBack`) and GameObject destruction. Grid cells are immediately walkable for remaining queues.
- **GAME STATE:** Managed by `GameManager.cs`. Win triggered when `activeQueues.Count == 0` (independent of destroyed holder objects). Loss triggered on timer expiry or deadlock (remaining queues cannot find unblocked path).
- **UI:** `GameUIManager.cs` controls HUD (timer, level #, hearts, restart), `WinPanel` (Next Level button), `LosePanel` (Retry button, dark overlay), and `SettingsModal`.
- **KNOWN WORKING:** Level loading, reference adaptation, 12 distinct colors, RGB parity, BFS pathfinding, slithering entry, queue swallowing, queue-based counter decrements, holder removal & footprint grid release, win/deadlock conditions, dynamic camera.
- **KNOWN ISSUES:**
  1. `Test01` in `ReferenceLevels1To5Tests.cs` and `TestLevels1To5Validation.cs` assert old draft grid dimensions instead of actual reference asset dimensions `{ 8x5, 10x10, 10x10, 10x10, 15x15 }`.
  2. `main.unity` contains 626 leftover baked objects (546 `ant_dis(Clone)` instances and 11 baked children under `Board`), though runtime `ClearBoard()` handles them safely.
  3. 8 compiler warnings for unused fields (`CS0414`).
- **CURRENT COMPILE STATUS:** 0 Errors, 8 Warnings.
- **DO NOT TOUCH:** Do NOT run `LevelGeneratorWindow` or `ReferenceLevelImporter.ImportReferenceLevels`. Do NOT mutate raw `.asset` reference files on disk. Do NOT bake objects into `main.unity`.
- **NEXT RECOMMENDED TASK:** Align `Test01` and `TestLevels1To5Validation.cs` with the verified reference asset grid dimensions, and optionally clean baked clones from `main.unity`.