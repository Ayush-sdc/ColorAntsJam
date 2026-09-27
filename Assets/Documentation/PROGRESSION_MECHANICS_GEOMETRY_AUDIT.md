# Slither Ants Jam Audit

**Audit Date:** 2026-09-28  
**Scope:** Read-Only Audit of Levels 1–45, Runtime Architecture, Gameplay Mechanics, and Visual Assets  
**Authorities Inspected:** Live Unity Editor (Process 36200 via Unity MCP Relay), `Assets/Data/Levels/LevelDatabase.asset`, `Assets/Data/ReferenceLevels/Level_*.asset` (1–45), `Assets/Data/ReferenceLevelJSON/Level_*.json` (1–45), active project C# source code.  
**Modifications Made:** NONE (Read-Only Audit)

---

## 1. Progression Blockers

### PB-001: Holder Capacity Deficit (Queue Demand Exceeds Assigned Capacity)

- **Level(s):** 22, 23, 24, 25, 32, 33, 37, 40, 45 (10 levels, 23 color deficits)
- **Problem:** Total holder capacity for a color is strictly less than the number of queues of that color on the board. Once the available holder capacity reaches zero, remaining matching queues cannot reserve or enter any holder, making the level mathematically impossible to complete.
- **Exact evidence:**  
  [ReferenceLevelRuntimeAdapter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs#L111-L159)  
  In `ReferenceLevelRuntimeAdapter.ExtractHoles(Level level)`:
  ```csharp
  // Assign capacity: if forward-facing queues were found, use that count; otherwise fallback to total color count
  foreach (var h in holes)
  {
      if (forwardCounts.TryGetValue(h, out int count) && count > 0)
          h.Capacity = count;
      else if (matchingQueueCounts.TryGetValue(h.ColorID, out int totalCount) && totalCount > 0)
          h.Capacity = totalCount;
      else
          h.Capacity = 1;
  }
  ```
  `forwardCounts` only counts queues whose initial cardinal `head - neck` ray directly intersects the hole footprint. In layouts where multiple holes share a ColorID and some queues require turning or are angled away, only the queues directly facing each hole are credited. Queues that do not face any hole directly are credited to zero holes. Consequently, `Sum(h.Capacity for color C) < TotalQueues(color C)`.
- **Measured Deficits:**
  - **Level 22:** Pink (ID 5) Demand=2, Capacity=1 (Deficit: 1); Brown (ID 8) Demand=2, Capacity=1 (Deficit: 1)
  - **Level 23:** Purple (ID 3) Demand=11, Capacity=6 (Deficit: 5); Yellow (ID 2) Demand=7, Capacity=4 (Deficit: 3); Indigo (ID 6) Demand=5, Capacity=4 (Deficit: 1); Pink (ID 5) Demand=10, Capacity=8 (Deficit: 2); Turquoise (ID 11) Demand=4, Capacity=3 (Deficit: 1)
  - **Level 24:** Pink (ID 5) Demand=13, Capacity=12 (Deficit: 1)
  - **Level 25:** SkyBlue (ID 0) Demand=4, Capacity=3 (Deficit: 1); Lime (ID 1) Demand=7, Capacity=6 (Deficit: 1)
  - **Level 32:** Red (ID 10) Demand=6, Capacity=4 (Deficit: 2); Yellow (ID 2) Demand=4, Capacity=2 (Deficit: 2)
  - **Level 33:** Red (ID 10) Demand=8, Capacity=4 (Deficit: 4); Turquoise (ID 11) Demand=12, Capacity=7 (Deficit: 5); Purple (ID 3) Demand=4, Capacity=3 (Deficit: 1)
  - **Level 37:** Lime (ID 1) Demand=6, Capacity=4 (Deficit: 2); SkyBlue (ID 0) Demand=6, Capacity=4 (Deficit: 2)
  - **Level 40:** Red (ID 10) Demand=13, Capacity=12 (Deficit: 1); Indigo (ID 6) Demand=8, Capacity=5 (Deficit: 3); Pink (ID 5) Demand=5, Capacity=4 (Deficit: 1)
  - **Level 45:** SkyBlue (ID 0) Demand=17, Capacity=14 (Deficit: 3); Pink (ID 5) Demand=19, Capacity=15 (Deficit: 4); Indigo (ID 6) Demand=20, Capacity=19 (Deficit: 1)
- **Why it blocks progression:** When matching queues enter up to the assigned capacity, `HoleController.remainingCount` reaches 0, the hole disappears, and subsequent valid matching queues find `HasAvailableCapacity == false`. `TryReserveSlot()` fails, blocking all remaining queues of that color from ever finishing.
- **Reproduction:** Load Level 22 in Play Mode. Play the first Pink queue into the Pink hole. The Pink hole disappears. The second Pink queue (`Record_14_5`) now has no available Pink hole and cannot move.
- **Confidence:** HIGH

---

### PB-002: Oversized Linked Groups Exceeding Concurrency Cap (`group.Count > MAX_CONCURRENT_MOVING_QUEUES`)

- **Level(s):** 28, 35
- **Problem:** Linked groups with more than 4 member queues can never be moved under the hard-coded `MAX_CONCURRENT_MOVING_QUEUES = 4` rule. Attempting to start the group always fails the concurrency check even when 0 other queues are moving.
- **Exact evidence:**  
  [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1176-L1184)  
  In `BoardManager.TryStartMove(AntQueueController queue)`:
  ```csharp
  var group = GetConnectedGroup(queue);

  if (movingQueues.Count + group.Count > MAX_CONCURRENT_MOVING_QUEUES)
  {
      Debug.Log($"[QueueMoveBlocked] Queue={queue.QueueId} Reason=ConcurrentMoveLimit GroupSize={group.Count} MovingQueues={movingQueues.Count} HeartPenalty=False");
      foreach (var member in group)
      {
          if (member != null) member.PlayBlockedShake();
      }
      return false;
  }
  ```
  And in Level data:
  - **Level 28:** `connectedIndex = 34` contains **6 queues** (`Record_57_6`, `Record_58_6`, `Record_59_6`, `Record_60_6`, `Record_61_6`, `Record_62_6`). `group.Count = 6`.
  - **Level 35:** `connectedIndex = 2` contains **7 queues** (`Record_26_6`, `Record_27_6`, `Record_28_6`, `Record_29_6`, `Record_30_6`, `Record_31_6`, `Record_32_6`). `group.Count = 7`.
  - **Level 35:** `connectedIndex = 3` contains **5 queues** (`Record_34_2`, `Record_35_2`, `Record_36_2`, `Record_37_2`, `Record_38_2`). `group.Count = 5`.
- **Why it blocks progression:** When `group.Count > 4`, `movingQueues.Count + group.Count` is at minimum `0 + 5 = 5 > 4` (or `6 > 4`, `7 > 4`). The condition triggers unconditionally on every tap, plays `PlayBlockedShake()`, and returns `false`. These linked groups cannot ever start, leaving the level permanently incomplete.
- **Reproduction:** Load Level 28. Tap any queue belonging to group 34 (e.g. `Record_57_6`). Notice the shake animation and log `[QueueMoveBlocked] ... GroupSize=6 MovingQueues=0 HeartPenalty=False`. The queue never starts.
- **Confidence:** HIGH

---

### PB-003: Unreachable Non-Facing Queues under Direct-Forward Movement Model (Pathfinding OFF)

- **Level(s):** 22, 23, 24, 25, 32, 33, 37, 40, 45 (42 queues across 9 levels)
- **Problem:** Automatic pathfinding and turning were globally disabled (`BoardManager.useAutomaticPathfinding = false`). Queues are restricted to moving purely straight along their initial `forwardDirection = head - neck`. Across 9 levels, 42 queues are positioned such that their straight-line raycast hits NO matching hole (either exits the board boundary into void or permanently intersects a wrong-color hole).
- **Exact evidence:**  
  [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L961-L989)  
  In `BoardManager.ValidateForwardMove(...)`:
  ```csharp
  // Scan forward along forwardDir
  while (IsInBounds(scan))
  {
      HoleController h = GetHoleAt(scan);
      if (h != null) { destinationHolder = h; entrance = scan; break; }
      scan += forwardDir;
  }
  if (destinationHolder == null) { blockedReason = "OutOfBoundsNoHolder"; return false; }
  if (destinationHolder.ColorID != queue.ColorID) { blockedReason = "WrongColorHolder"; return false; }
  ```
- **Unreachable Examples:**
  - **Level 22:**
    - `Record_6_1` (Lime, Head: `(12, 1)`, Fwd: `(-1, 0)`). Travels horizontally along Y = 1 to X = 0 (board edge). The Lime hole is at `(14, 13), (13, 13), (12, 13)` (Y = 13). It never intersects the Lime hole.
    - `Record_8_3` (Purple, Head: `(8, 1)`, Fwd: `(-1, 0)`). Travels along Y = 1 to X = 0. Purple holes are at Y = 3 and Y = 13.
    - `Record_9_4` (Orange, Head: `(3, 1)`, Fwd: `(-1, 0)`). Travels along Y = 1 to X = 0. Orange holes are at Y = 4 and Y = 13.
    - In total, 13 out of 17 queues in Level 22 cannot reach their hole without turning!
  - **Level 23:** 14 queues have no matching hole along their straight ray (e.g. `Record_0_3`, `Record_1_3`, `Record_2_2`).
  - **Level 33:** 10 queues have no matching hole along their straight ray (e.g. `Record_6_10`, `Record_7_10`, `Record_8_11`).
- **Why it blocks progression:** Tapping any non-facing queue triggers `[ForwardMoveBlocked] Reason=OutOfBoundsNoHolder` or `Reason=WrongColorHolder`, deducts 1 player life, and never moves. These queues can never reach a hole under pure straight movement.
- **Reproduction:** Load Level 22. Tap `Record_6_1` (Lime queue at bottom right). The ant shakes, log records `[ForwardMoveBlocked] Queue=Record_6_1 Reason=OutOfBoundsNoHolder`, and 1 heart is lost.
- **Confidence:** HIGH

---

### PB-004: Premature Deadlock Game Over Triggered by Straight-Line Checker

- **Level(s):** 16, 22, 23, 24, 25, 29, 32, 33, 37, 40, 41, 45
- **Problem:** `GameManager.CheckGameWinLossConditions()` evaluates whether any remaining queue can make a valid move via `BoardManager.IsPathClearForQueue(q)`. When `useAutomaticPathfinding` is false, this calls `ValidateForwardMove`. If all currently available queues are blocked by other queues or require turning, `anyCanMove` evaluates to `false`, immediately triggering `GameState.LevelLost` ("Deadlock!").
- **Exact evidence:**  
  [GameManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/GameManager.cs#L176-L190)  
  ```csharp
  bool anyCanMove = false;
  foreach (var q in activeQueues)
  {
      if (BoardManager.Instance.IsPathClearForQueue(q))
      {
          anyCanMove = true;
          break;
      }
  }
  if (!anyCanMove)
  {
      TriggerGameOver("Deadlock! No remaining ant queues have an unblocked path to their matching hole.");
  }
  ```
  And [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1485-L1505):
  `IsPathClearForQueue` requires `ValidateForwardMove` to succeed immediately.
- **Why it blocks progression:** In levels where queues are interlocked or where queues must wait for other queues/holes to clear, if at any point after board setup or queue ingestion no queue has an immediately clear straight shot, the player is instantly failed via Game Over without having made an invalid move.
- **Reproduction:** In Level 22, if initial queues are ingested such that remaining queues are non-facing (PB-003), the game immediately triggers `TriggerGameOver("Deadlock! ...")`.
- **Confidence:** HIGH

---

## 2. Missing / Partial Special Mechanics

### SM-001: HolderType 2 (Static Obstacles / Blocker Snakes)

- **Mechanic:** In the reference data, `HolderType 2` denotes static obstacle snakes/walls placed across the board that serve as collision barriers.
- **Level(s):** 21, 22, 23, 24, 25, 32, 33, 37, 38, 40, 45 (11 levels, 45 records)
- **Source data indicating mechanic:**  
  `Assets/Data/ReferenceLevelJSON/Level_*.json` and `Assets/Data/ReferenceLevels/Level_*.asset` contain records with `holderType: 2` (e.g. Level 22 `Record_0_0` with 19 cells forming a perimeter wall, Level 23 with 13 obstacle records).
- **Current runtime support:**  
  [ReferenceLevelRuntimeAdapter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs#L58-L180):  
  Only checks `if (record.HolderType == 0)` (Holes) and `if (record.HolderType == 1)` (Queues). `HolderType 2` is completely ignored and dropped.
- **Status:** MISSING
- **Gameplay consequence:** The obstacle structures do not spawn at all. Boards appear open and empty where complex walls/channels are supposed to exist.

---

### SM-002: Numbered Padlocks / Locked Queues (`isLocked`, `lockAmount`)

- **Mechanic:** Queues that are locked with a numeric counter (`lockAmount`). In the reference game, the lock prevents movement until a specified number of queues (or specific matching keys) have been cleared, decrementing the lock counter.
- **Level(s):** 7, 30, 31, 32, 33, 37 (6 levels, 43 records; lock amounts range from 2 to 30)
- **Source data indicating mechanic:**  
  `isLocked: 1` and `lockAmount: [2..30]` serialized in `AntQueueData`.
- **Current runtime support:**  
  [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs#L107-L120):  
  `Initialize(AntQueueData data)` copies `queueId`, `colorId`, `antCount`, `isConnected`, and `connectedIndex`. `data.IsLocked` and `data.LockAmount` are not stored, checked, or rendered.
- **Status:** MISSING
- **Gameplay consequence:** Locked queues are treated as normal unlocked queues and can be moved immediately from turn 1, bypassing puzzle gating.

---

### SM-003: Dual-Color Ant Queues (`isSecondColor`, `secondColor`)

- **Mechanic:** Queues possessing two distinct color segments (head segment of Color A, tail segment of Color B), requiring dispatch or split swallow into two different holes.
- **Level(s):** 15, 16, 20, 25, 29, 32, 37, 40, 41, 44 (10 levels, 19 records)
- **Source data indicating mechanic:**  
  `isSecondColor: 1` and `secondColor: [0..11]` serialized in `AntQueueData`.
- **Current runtime support:**  
  [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs#L107-L120):  
  `Initialize(AntQueueData data)` ignores `isSecondColor` and `secondColor`. All ant segments are colored uniformly using `data.ColorID`.
- **Status:** MISSING
- **Gameplay consequence:** Dual-color queues spawn as single-color queues. Their secondary color logic and dual-destination entry behavior do not exist.

---

### SM-004: Linked Ants System (`isConnected`, `connectedIndex`)

- **Mechanic:** Two or more queues physically connected by a horizontal rope spanning their leaders. Clicking either queue validates and launches all connected queues simultaneously in parallel lanes.
- **Level(s):** 7, 8, 9, 10, 17, 18, 20, 24, 25, 28, 29, 32, 33, 35, 37, 40, 41, 43, 44, 45 (20 levels)
- **Source data indicating mechanic:**  
  `isConnected: 1` and `connectedIndex: [0..N]` serialized in `AntQueueData`.
- **Current runtime support:**  
  - [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L638-L664) (`GetConnectedGroup` groups queues by `connectedIndex`).
  - [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L1776-L1832) (`SetupLinkedRopes` creates `LinkedAntsRopeVisual`).
  - [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs) connects leaders with a horizontal `GoldenRope_2Cells` visual.
- **Status:** PARTIAL
- **Gameplay consequence:** Fully functional on Levels 7–10 for pairs of 2–4 queues. However, on Level 28 (group of 6) and Level 35 (groups of 5 and 7), the mechanic hard-locks progression (PB-002) because the concurrency limit (4) prevents groups larger than 4 from ever starting. Furthermore, linked queues only support parallel straight lines; if one member encounters an obstacle or turn, the pair cannot navigate jointly.

---

### SM-005: Pipe Obstacles (`pipeCount`)

- **Mechanic:** Pipe tunnel / transit obstacle through which ant queues must pass or navigate around.
- **Level(s):** 45 (`Record_100_10` with `pipeCount: 8`)
- **Source data indicating mechanic:**  
  `pipeCount: 8` serialized in `AntQueueData` for Level 45.
- **Current runtime support:**  
  None. Field is passed into `AntQueueData` constructor but discarded in `AntQueueController.Initialize`.
- **Status:** MISSING
- **Gameplay consequence:** Silently ignored; treated as a normal queue.

---

## 3. Ant Geometry / Runtime Cost

*Measurements acquired directly via live Unity Editor asset analysis of authoritative prefabs.*

### ChubbyAntColors (`Assets/ChubbyAntColors/Prefabs/ChubbyAnt_Red.prefab`)
- **GameObjects:** 9 (Root + 8 child mesh GameObjects)
- **Renderer count:** 8 MeshRenderers
- **SkinnedMeshRenderer count:** 0
- **Vertices:** 39,064
- **Triangles:** 72,112
- **Material Slots:** 8
- **Unique Materials:** 8
- **Colliders:** 1 (`BoxCollider` on prefab root; stripped at runtime by `AntVisualConfig.StripConflictingComponents`; replaced by 1 `SphereCollider` on `AntUnit` root)
- **Rigidbodies:** 0
- **Animators:** 0
- **Scripts:** 0
- **Child Mesh Breakdown:**
  - `body`: 13,860 verts / 25,600 tris
  - `accent`: 8,407 verts / 15,792 tris
  - `eyes`: 1,386 verts / 2,560 tris
  - `iris`: 1,386 verts / 2,560 tris
  - `pupils`: 1,386 verts / 2,560 tris
  - `glints`: 2,772 verts / 5,120 tris
  - `blush`: 1,386 verts / 2,560 tris
  - `rope`: 8,481 verts / 15,360 tris

### WorkerAnt (`Assets/WorkerAnt/Prefabs/WorkerAnt_Red.prefab`)
- **GameObjects:** 9 (Root + 8 child mesh GameObjects)
- **Renderer count:** 8 MeshRenderers
- **SkinnedMeshRenderer count:** 0
- **Vertices:** 35,604
- **Triangles:** 65,632
- **Material Slots:** 8
- **Unique Materials:** 8
- **Colliders:** 1 (`BoxCollider` on prefab root; stripped at runtime; replaced by 1 `SphereCollider` on `AntUnit` root)
- **Rigidbodies:** 0
- **Animators:** 0
- **Scripts:** 0
- **Child Mesh Breakdown:**
  - `body`: 11,088 verts / 20,480 tris
  - `accent`: 7,719 verts / 14,432 tris
  - `eyes`: 1,386 verts / 2,560 tris
  - `iris`: 1,386 verts / 2,560 tris
  - `pupils`: 1,386 verts / 2,560 tris
  - `glints`: 2,772 verts / 5,120 tris
  - `blush`: 1,386 verts / 2,560 tris
  - `rope`: 8,481 verts / 15,360 tris

---

### Estimated Queue Cost (1 Leader + N-1 Workers)

| Queue Size | Leader + Workers | Estimated Vertices | Estimated Triangles | MeshRenderers | SphereColliders |
|---|---|---|---|---|---|
| **1 Ant** | 1 Chubby | 39,064 | 72,112 | 8 | 1 |
| **5 Ants** | 1 Chubby + 4 Workers | 181,480 | 334,640 | 40 | 5 |
| **10 Ants** | 1 Chubby + 9 Workers | 359,500 | 662,798 | 80 | 10 |
| **20 Ants** | 1 Chubby + 19 Workers | 715,540 | 1,319,120 | 160 | 20 |

---

### Worst Observed Levels (Scene Total Ant Geometry)

| Rank | Level | Total Queues | Total Ants | Total Triangles | Total Vertices | Total MeshRenderers |
|---|---|---|---|---|---|---|
| **1** | **Level 45** | 150 | 1,409 | **93,447,488** | 50,685,036 | 11,272 |
| **2** | **Level 42** | 87 | 801 | **53,134,992** | 28,819,824 | 6,408 |
| **3** | **Level 34** | 113 | 655 | **43,721,200** | 23,711,600 | 5,240 |
| **4** | **Level 25** | 93 | 642 | **42,738,384** | 23,179,548 | 5,136 |
| **5** | **Level 14** | 70 | 566 | **37,601,312** | 20,394,064 | 4,528 |

- **Worst Level:** Level 45
- **Ant count:** 1,409 ants across 150 queues
- **Estimated scene triangles:** **93,447,488 triangles**
- **Estimated scene vertices:** **50,685,036 vertices**
- **Estimated renderers:** **11,272 MeshRenderers**
- **Estimated material instances:** 0 (Shared materials cached in `AntColorPalette` + `MaterialPropertyBlock` for per-instance color)
- **Worst 4 Moving Queues Concurrency Cost (e.g. 4 queues of 15 ants = 60 ants):**  
  ~4.0 million triangles, 2.1 million vertices, 480 MeshRenderers, and 480 active DOTween animation tweens executing simultaneously.

---

## 4. Excessive Geometry Findings

### AG-001: Unused `rope` Mesh Child Baked into Every Single Ant Prefab
- **Prefab/system:** `ChubbyAntColors` and `WorkerAnt` prefabs (`Assets/ChubbyAntColors/Prefabs/` and `Assets/WorkerAnt/Prefabs/`)
- **Issue:** Every single ant prefab contains an 8th child GameObject named `rope` with an 8,481-vertex, 15,360-triangle mesh. `AntVisualConfig.StripConflictingComponents` disables this GameObject at runtime (`rope.gameObject.SetActive(false)`), but the asset is still loaded into memory and bundled into every prefab instance.
- **Measured cost:**  
  - 15,360 triangles / 8,481 vertices wasted per ant.
  - On Level 45 (1,409 ants), the inactive `rope` meshes alone account for **21,642,240 triangles** and **11,949,729 vertices** of wasted memory.
- **Engineering severity:** HIGH
- **Potential optimization direction:** Remove the inactive `rope` child GameObject and mesh from the source prefabs.

---

### AG-002: Hyper-Detailed Multi-Mesh Facial Features on Top-Down Distant Models
- **Prefab/system:** `ChubbyAntColors` and `WorkerAnt` (`eyes`, `iris`, `pupils`, `glints`, `blush`)
- **Issue:** Facial details are broken into 5 separate GameObjects with individual MeshRenderers and high-polygon meshes (2,560 to 5,120 triangles each). From the fixed top-down perspective (Camera height = 13.04, FOV = 78°), ants occupy approximately 30–60 pixels on a mobile screen. The glints, pupils, irises, and blush are virtually sub-pixel and produce 5 separate draw calls per ant.
- **Measured cost:**  
  - 5 MeshRenderers and 15,360 triangles per ant.
  - On Level 45, facial features alone consume **21,642,240 triangles** and **7,045 MeshRenderers**.
- **Engineering severity:** HIGH
- **Potential optimization direction:** Consolidate facial features into the base head texture or combine them into a single low-poly planar decal/mesh with 1 renderer.

---

### AG-003: Extreme Polycount on Base Body Geometry for Mobile
- **Prefab/system:** `ChubbyAntColors` (`body` + `accent` = 41,392 tris) and `WorkerAnt` (`body` + `accent` = 34,912 tris)
- **Issue:** A single ant unit contains 65k–72k triangles. Standard mobile character polycounts for top-down strategy/puzzle games typically range from 500 to 2,500 triangles per character. Rendering 1,409 ants at 70k triangles pushes scene geometry to 93.4 million triangles, which exceeds mobile GPU budgets by 50x–100x.
- **Measured cost:** 93.4 million triangles on Level 45; 10.6 million triangles on Level 6.
- **Engineering severity:** CRITICAL
- **Potential optimization direction:** Decimate/re-topologize the ant body and accent meshes to mobile-appropriate LOD budgets (e.g. 800–1,500 triangles per ant).

---

### AG-004: Excessive MeshRenderer Draw Call Fragmentation
- **Prefab/system:** Visual ant instantiation (`AntUnit` -> `VisualRoot` -> 8 MeshRenderers)
- **Issue:** Each ant contains 8 separate `MeshRenderer` components. 1,409 ants create 11,272 active MeshRenderers in the scene graph. Even though `MaterialPropertyBlock` avoids material instance duplication, Unity's render pipeline must still cull, sort, and batch 11,272 individual render nodes each frame.
- **Measured cost:** 11,272 renderers on Level 45; 1,280 renderers on Level 6.
- **Engineering severity:** CRITICAL
- **Potential optimization direction:** Combine the static child meshes of the ant prefab into a single unified mesh with 1 MeshRenderer.

---

### AG-005: Total Absence of LODs or Distance Culling
- **Prefab/system:** Entire ant visual pipeline
- **Issue:** There are 0 `LODGroup` components in the entire project. High-polygon meshes are rendered at 100% resolution regardless of distance, level grid dimension (Level 45 is 40x40 where ants appear tiny), or whether the camera is panned away.
- **Measured cost:** 100% geometry rendered across all visible ants.
- **Engineering severity:** HIGH
- **Potential optimization direction:** Implement LOD groups or simple low-poly representation for small/zoomed-out scales.

---

### AG-006: High DOTWEEN Sequence Allocation During Movement
- **Prefab/system:** `AntUnit.StartWalkAnimation()`
- **Issue:** When a queue starts moving, every ant in the queue instantiates a new 8-tween DOTween `Sequence` (`Append`, `Join` x 4) looping indefinitely on `visualRoot`. With 4 queues moving simultaneously, up to 480 active tweens run concurrently.
- **Measured cost:** Forced expanding global DOTween capacity via `DOTween.SetTweensCapacity(750, 150)`.
- **Engineering severity:** MEDIUM
- **Potential optimization direction:** Use procedural vertex shader animation or evaluate bob/tilt using a single lightweight mathematical sine wave in `Update()`.

---

## 5. Cross-Cutting Issues

1. **PB-001 + SM-001 (Holder Capacity Deficit & Missing Obstacle Snakes):**  
   `ReferenceLevelRuntimeAdapter` drops `HolderType 2` entirely and calculates holder capacities using straight raycasts. Because `HolderType 2` obstacles are not spawned, the visual board is missing its puzzle boundaries; and because non-facing queues are not counted, the holders that DO spawn lack sufficient capacity, causing guaranteed level failure on Levels 22, 23, 24, 25, 32, 33, 37, 40, 45.

2. **PB-002 + SM-004 (Linked Ants Oversized Groups & Concurrency Lock):**  
   The linked ant mechanic groups all queues sharing a `connectedIndex`. However, the movement system enforces a rigid `MAX_CONCURRENT_MOVING_QUEUES = 4` limit. On Levels 28 and 35, reference data defines linked groups with 5, 6, and 7 queues. The mechanic collides directly with the concurrency check, resulting in a permanent input block where the groups can never move.

3. **PB-003 + SM-001 (Unreachable Queues & Missing Reference Obstacle/Pathfinding Integration):**  
   The reference game levels were authored with specific movement corridors formed by `HolderType 2` obstacle snakes and turning paths. Disabling automatic turning/pathfinding in favor of strict `forwardDirection` raycasting breaks 42 queues across 9 levels, making them physically unable to aim at their matching holes.

4. **AG-003 + AG-004 + High Queue Count (Level 45 Performance Crash Risk):**  
   Level 45 combines 150 queues (1,409 ants) with 93.4 million triangles and 11,272 MeshRenderers. On mobile hardware, this volume of draw calls and vertex processing is likely to produce severe frame drops or Out-Of-Memory crashes.

---

## 6. Level Coverage

- **Levels inspected:** Levels 1–45 (45/45 reference level assets and source JSONs verified)
- **Levels with progression blockers:** Levels 16, 22, 23, 24, 25, 28, 29, 32, 33, 35, 37, 40, 41, 45 (14 levels)
- **Levels with special mechanics:** Levels 7, 8, 9, 10, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 28, 29, 30, 31, 32, 33, 35, 37, 38, 40, 41, 43, 44, 45 (28 levels)
- **Levels with unsupported / missing mechanics:** Levels 7, 15, 16, 20, 21, 22, 23, 24, 25, 28, 29, 30, 31, 32, 33, 35, 37, 38, 40, 41, 44, 45 (22 levels)
- **Levels with hole overlaps:** 0 levels (All 45 levels have 0 logical cell overlaps and 0 visual bounding box overlaps)
- **Levels with invalid / corrupt level data:** 0 levels (All queue cells, holder cells, and grid sizes are within valid bounds; 0 duplicate cells; 0 queues inside holes)

---

## 7. Final Diagnostic Summary

- **PROGRESSION BLOCKERS FOUND:** 4 (PB-001 Capacity Deficit [10 levels], PB-002 Oversized Linked Groups [2 levels], PB-003 Unreachable Non-Facing Queues [9 levels], PB-004 Deadlock Checker False-Failure [12 levels])
- **MISSING SPECIAL MECHANICS:** 4 (SM-001 HolderType 2 Obstacles, SM-002 Locked Queues, SM-003 Dual-Color Queues, SM-005 Pipe Obstacles)
- **PARTIAL SPECIAL MECHANICS:** 1 (SM-004 Linked Ants — operational on Levels 7–10, blocked on Levels 28 & 35)
- **UNSUPPORTED HOLDER TYPES:** `HolderType 2` (Obstacle / Wall Snakes, present in 11 levels)
- **LEVELS AFFECTED:** Levels 7, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 28, 29, 30, 31, 32, 33, 35, 37, 38, 40, 41, 43, 44, 45 (25 levels)
- **CHUBBY TRIANGLES:** 72,112 triangles (39,064 vertices, 8 MeshRenderers)
- **WORKER TRIANGLES:** 65,632 triangles (35,604 vertices, 8 MeshRenderers)
- **WORST ANT GEOMETRY LEVEL:** Level 45 (1,409 ants, 93,447,488 triangles, 50,685,036 vertices, 11,272 MeshRenderers)
- **PER-ANT MATERIAL INSTANCING:** NO (Shared materials in `AntColorPalette` + `MaterialPropertyBlock` used for color tinting)
- **MOVEMENT-RELATED PHYSICS COMPONENTS ON ANT VISUALS:** 1 `SphereCollider` on each `AntUnit` root (used for pointer raycasting; visual prefab `BoxCollider` and `Rigidbody` are stripped at runtime)
- **FILES MODIFIED:** NONE (Audit report created only)
- **PROJECT BEHAVIOR MODIFIED:** NO
