# Slither Ants Jam — Antigravity Project Context

Last Updated:
2026-09-25 19:42 (UTC+05:30)

## 1. Project Goal

Slither Ants Jam recreates the gameplay/layout behavior of the reference Color Slither Jam levels while replacing snake/slither units with individual ant queues.

Levels should be driven by ScriptableObject level data and rendered at runtime.

## 2. Current Runtime Architecture

LevelManager
    -> LevelDatabase (`Assets/Data/Levels/LevelDatabase.asset`)
    -> selected Level asset (`Assets/Data/ReferenceLevels/Level_XXX.asset`)
    -> GameManager
    -> BoardManager
    -> `SetupReferenceBoard()` / `SetupHandmadeBoard()`

- **Active Database Path:** `Assets/Data/Levels/LevelDatabase.asset` (authoritative database containing all 45 reference levels: `Level_001` to `Level_045`).
- **Current Level Asset Source:** `Assets/Data/ReferenceLevels/Level_001.asset` through `Level_045.asset`.
- **Reference-Level Adapter Usage:** `ReferenceLevelRuntimeAdapter.cs` identifies reference levels (`IsReferenceLevel()`) and dynamically extracts holder trays (`HolderType == 0`) and playable queues (`HolderType == 1`) without mutating underlying assets.
- **BoardManager Rendering Branch:** Evaluates `ReferenceLevelRuntimeAdapter.IsReferenceLevel(level)`:
  - If true, calls `SetupReferenceBoard(level)` -> `SpawnReferenceHole()` and `SpawnReferenceAntQueue()`.
  - If false, calls `SetupHandmadeBoard(level)`.
- **Current Holder System:** `HoleController.cs` procedural stadium slot geometry (`SlotRim`, `SlotInnerBevel`, `SlotPit`, `SlotCounterBadge`). Dynamically sizes to multi-cell footprints, caches entrance cell, counts matching queues, and applies single-queue visual overrides when initial count == 1.
- **Current Queue System:** `AntQueueController.cs` and `AntUnit.cs`. Operates on Free-Floor continuous world-space movement. Leader (A0) steers freely across valid open floor toward its matching holder pit. Followers (A1, A2, ...) trace the head's exact continuous world-space trail at constant distances ($D, 2D, ...$), preventing corner-cutting. Head continuously rotates to face its motion vector. Sequential dive into holder pit upon arrival. Up to 4 queues move concurrently.
- **Current Color Mapping System:** `ReferenceColorMapper.cs` maps raw ColorIDs (0..11) to `AntColor` enum and `AntColorPalette` materials/colors. 100% RGB parity between ant units and holder rims.
- **Current Completion/Removal System:** When a holder's remaining queue count reaches 0, `isCompleted = true`. `BoardManager.RemoveCompletedHole()` immediately unregisters its entire multi-cell footprint from `holesMap` before visual shrinkage (`0.20s`, `Ease.InBack`) and destruction.

## 3. Authoritative Data Rules

### Reference ColorID Mapping & Visual Palette

Sampled directly from midtone body beads in Level 4 and Level 5 reference screenshots:

- **0 SkyBlue:** `#1298E6` / `(0.071, 0.596, 0.902)` — Bright saturated cyan-blue
- **1 Lime:** `#66C30E` / `(0.400, 0.765, 0.055)` — Vibrant yellow-green
- **2 Yellow:** `#FEBD04` / `(0.996, 0.741, 0.016)` — Deep rich toy yellow
- **3 Purple:** `#7A3DCE` / `(0.478, 0.239, 0.808)` — Vivid violet/purple
- **4 Orange:** `#EC7017` / `(0.925, 0.439, 0.090)` — Warm saturated orange
- **5 Pink:** `#F24FE6` / `(0.949, 0.310, 0.902)` — Bright candy magenta/pink
- **6 Indigo:** `#415ACD` / `(0.255, 0.353, 0.804)` — Deep saturated blue-violet
- **7 Green:** `#2B8428` / `(0.169, 0.518, 0.157)` — Natural lush game green
- **8 Brown:** `#A26122` / `(0.635, 0.380, 0.133)` — Warm caramel/chocolate brown
- **9 White:** `#ECE0D2` / `(0.925, 0.878, 0.824)` — Warm off-white (clearly readable against board)
- **10 Red:** `#E83F40` / `(0.910, 0.247, 0.251)` — Vibrant candy red
- **11 Turquoise:** `#08EBC1` / `(0.031, 0.922, 0.757)` — Brilliant aqua/cyan-green
- **12 = UNVERIFIED**

**Slot Pit Neutral:**
- **SlotPit Color:** `#1F252B` / `(0.122, 0.145, 0.169)` — Dark charcoal/blue recessed pit

Raw ColorIDs in reference assets must remain unchanged.

Both queue ants and SlotRim must resolve through the same authoritative palette:
Raw ColorID
-> ReferenceColorMapper
-> AntColor
-> AntColorPalette

### Reference Holder Rules

HolderType 0 = holder footprint
HolderType 1 = playable ant queue

Holder OccupiedCells must preserve all serialized cells.

### Queue Rules

HolderType 1 Cells must remain verbatim.
Do not append holder cells.
Do not flip coordinates.
Do not swap X/Y.
Cells[0] is the leader / head.

### AUTOMATIC PATHFINDING: DISABLED GLOBALLY

> **CORE MECHANIC — DO NOT ENABLE WITHOUT EXPLICIT USER REQUEST**
>
> **AUTOMATIC PATHFINDING:**
> DISABLED GLOBALLY
>
> **Applies to:**
> ALL LEVELS (Level 1 through Level 45 and future levels)
>
> **Central Switch:**
> Managed centrally in runtime code via `BoardManager.UseAutomaticPathfinding = false`. Level assets are not individually modified.
>
> **AntPathfinder:**
> Retained in source for reference/tooling, but not used by normal runtime queue movement (runtime calls = 0).
>
> **Grid:**
> Used for level/layout data only, not automatic queue routing.
>
> **Followers:**
> Follow continuous leader trail and never pathfind.

### FREE-FLOOR MOVEMENT — CORE MECHANIC

CORE MECHANIC — DO NOT CHANGE WITHOUT EXPLICIT USER REQUEST

- **Floor dots are visual only:** Zero collision authority. No dot is required underneath an ant. Ants can move and stand freely in spaces between and beside dots.
- **Grid cells are not mandatory movement nodes:** Logical grid data defines level layout, initial ant positions, holder positions, obstacles, and serialization, but does NOT act as movement rails.
- **Predefined path waypoints are not movement rails:** The actual world-space route may travel anywhere through the valid open floor connecting them.
- **Leading ant may move anywhere inside valid open board floor:** Queue index 0 is the leading ant. Only the leading ant chooses/routes movement. The head may navigate freely in continuous world space anywhere on valid open floor.
- **Head may move outside the original waypoint/path line:** The route is allowed to expand beyond narrow waypoint corridors as long as it stays within valid open floor.
- **Ants may occupy positions with no dot underneath:** Pause/inspect during movement verifies continuous float coordinates between dot centers are 100% legal.
- **Followers trace the actual continuous path of the ant directly ahead:** A0 = leader, A1 follows A0, A2 follows A1, A3 follows A2. Followers trace the head's actual recorded continuous world-space trail at constant distances ($D, 2D, 3D$). No independent pathfinding, no simple `Vector3.Lerp` between ants, zero corner-cutting.
- **Holes are destinations, not movement corridors:** Ants may approach matching holes from any valid surrounding open floor, diving in sequentially upon arrival at the pit.
- **Max moving queues remains 4:** Concurrency limit of 4 moving queues is preserved. A fifth queue tap is ignored without heart penalty.
- **Timer starts on first valid move:** Level timer is visible at full duration on load and does NOT count down until the player triggers the first valid queue move.

### HEAD INITIAL DIRECTION — CORE MECHANIC

CORE MECHANIC — DO NOT CHANGE WITHOUT EXPLICIT USER REQUEST

- **Leading ant must begin movement exactly along its visual/gameplay forward direction (`headForwardWorld`).**
- **No automatic forward-right / diagonal launch:** Initial movement angle error is 0.000°.
- **Forward distance before turn:** Head travels straight forward for a minimum distance (`minForwardDistance = 0.75 * cellSize`) before beginning any route turn.
- **Turning may occur only after initial forward progression:** After the head has progressed forward, it may then turn smoothly left or right across valid open floor to reach the matching hole.
- **Head orientation:** Derived authoritatively from the gameplay root / ant transform, not camera orientation. Head continuously rotates to face the actual path tangent with zero sideways sliding.

### ANT QUEUE BLOCKERS & WALKABLE FLOOR — CORE MECHANIC

CORE MECHANIC — DO NOT CHANGE WITHOUT EXPLICIT USER REQUEST

- **ANT QUEUE MOVEMENT MAY ONLY BE BLOCKED BY:**
  1. **Holes / Holders:** Non-target holes block; matching destination hole allows the intended approach/entry route.
  2. **Other Ant Queues:** Both stationary and moving units of other queues block. Own queue's ants do not block self forward progression.
- **IGNORED FOR ANT PATH BLOCKING (MUST NEVER BLOCK AN ANT QUEUE):**
  - Floor dots
  - Grid markers
  - Cell visuals
  - Board decoration
  - Background meshes
  - Decorative colliders
  - Environmental meshes
  - Props, pipes, connectors
  - VFX, particles, shadows
  - Floor / StudioFloor
  - Level disabled cells / Level obstacles
  - Imported visual prefabs
  - UI & buttons
- **BOARD BOUNDARY:**
  - Playable board boundary containment remains enforced (`GetWalkableBounds()` & `IsWorldPosInsideBoard()`). Ants may use all open floor inside the boundary, but cannot leave the board.

### Queue Concurrency Rule (Max 4 Concurrent Moving Queues)

- **Concurrency Limit:** Maximum 4 ant queues may move simultaneously (`MAX_CONCURRENT_MOVING_QUEUES = 4`).
- **Parallel Movement:** When Queue A starts moving, the player can immediately tap Queue B, C, and D to start moving concurrently. All movement coroutines run in parallel.
- **Individual Queue Protection:** Each queue maintains independent `IsMoving` protection (`queue.IsMoving`). Tapping a queue that is already moving is ignored (no duplicate coroutines, no penalty).
- **Fifth Queue Input & Heart Safety:** When 4 queues are moving (`movingQueues.Count >= 4`), tapping a fifth queue does NOT start movement and does NOT deduct a heart. It triggers subtle blocked feedback (shake) without penalty (`PlayerLives` unchanged).
- **Concurrency Tracking:** Managed authoritatively in `BoardManager.movingQueues` (`HashSet<AntQueueController>`). Added on `TryStartMove()`, removed on `OnQueueCompleted()` or `OnQueueMoveStoppedEarly()`. Cleared during `Awake()`, `ClearBoard()`, and `RestartLevel()`.
- **Destination Capacity & Inbound Reservations:**
  - Simultaneous queues may target different holders OR the same holder if that holder has enough available capacity.
  - Available capacity is calculated as: `AvailableCapacity = remainingCount - inboundReservations`.
  - When movement begins, a slot is reserved via `HoleController.TryReserveSlot()`.
  - Inbound reservations prevent holder oversubscription (`IsFull => isCompleted || AvailableCapacity <= 0`).
  - If a holder's available capacity is 0, subsequent matching queues cannot reserve or target it.
  - If movement is cancelled or interrupted early, `ReleaseReservation()` frees the slot without decrementing `remainingCount`.
- **Holder Counter Timing & Hole Completion:**
  - Individual ants do not decrement holder queue count (`OnAntEnteredHole` / `OnAntSwallowed` performs visual dive/swallow only).
  - Holder count decrements only after the entire queue has entered (`remainingCount--` in `OnEntireQueueEnteredHole` / `OnQueueSwallowed`).
  - Tail / final ant must complete swallow before queue is considered delivered (`swallowedAntCount == totalAntCount`).
  - Hole remains visible while any required ant is still entering.
  - Hole disappears only after `remainingCount` reaches 0 and final inbound queue has fully entered (`remainingCount <= 0 && inboundReservations <= 0`).
  - Hole must not disappear while another valid reserved queue still needs it.

### HOLE COMPLETION RULE — CORE MECHANIC

> **CORE MECHANIC — DO NOT CHANGE WITHOUT EXPLICIT USER REQUEST**
>
> - Individual ants do not decrement holder queue count.
> - Holder count decrements only after the entire queue has entered.
> - Tail/final ant must complete swallow before queue is considered delivered.
> - Hole remains visible while any required ant is still entering.
> - Hole disappears only after remainingCount reaches 0 and final inbound queue has fully entered.
> - Hole must not disappear while another valid reserved queue still needs it.

- **Holder Removal Safety:**
  - A holder may only be removed when `remainingCount <= 0 && inboundReservations <= 0`.
  - Ensures multi-capacity holders remain active until ALL in-flight reserved queues have entered.
- **Path Conflict Handling:**
  - Moving queues reserve unreached cells directly ahead of the head along `forwardDirection` via `GetReservedPathCells()`.
  - If two moving queues would occupy the same cell, the collision is detected and handled without rerouting or pathfinding.
- **Terminal State Safety:**
  - Level timeout (`00:00`) or 0 lives transitions `GameState` to `LevelLost`.
  - `TryStartMove()` requires `GameManager.Instance.State == GameState.Playing`, preventing new moves in terminal states.
  - Late queue completion during loss cannot trigger `LevelWon`.
  - Deadlock detection in `GameManager.CheckGameWinLossConditions()` is bypassed while `BoardManager.Instance.MovingQueuesCount > 0`.
- **Camera Drag Independence:** Camera dragging does not consume a movement slot and operates independently.

### Extracted Color Slither Prefabs & Asset Status

- **EXTRACTED COLOR SLITHER PREFABS:** Imported all reconstructed non-slither prefabs (`Assets/ColorSlitherJamAllPrefabs/Prefabs/Reconstructed/`).
- **SNAKE/SLITHER PREFABS:** NOT USED / EXCLUDED. Reconstructed prefab roots `Head Snake`, `Mid Snake`, `Tail Snake`, and any snake/slither gameplay prefabs are strictly excluded.
- **CURRENT QUEUE VISUAL:** ANTS (`AntQueueController` -> `AntUnit` -> native worker ant / 3D procedural ant visuals).
- **MAX CONCURRENT MOVING QUEUES:** 4 (`MAX_CONCURRENT_MOVING_QUEUES = 4`).

### Timer Start Rule

- **TIMER START RULE:**
  - Timer is initialized and displayed on level load (e.g. `02:00`), but countdown begins ONLY after the first successfully accepted ant-queue move.
  - Non-gameplay input (e.g. waiting, touching empty floor, camera panning, settings modal / UI buttons) and invalid/blocked moves do NOT start the timer.
  - Restarting a level (`RestartLevel`) or loading a new level resets `timerStarted = false` and restores the full level duration.
  - Timer and lives remain independent failure conditions. When remaining timer reaches `00:00`, state transitions to `LevelLost` and `LosePanel` is shown.

### Invalid Move Rule
An invalid player move or gameplay collision deducts exactly one life per move attempt.
Tapping a fifth queue while 4 queues are moving is NOT an invalid move and does NOT deduct a life.

### Lives Rule
Player begins each level with 3 lives.

3 -> 2 -> 1 -> 0

At 0:
GameState.LevelLost
LosePanel shown
input blocked.

### Camera Rule (Fixed-View & Pan System)

- **Fixed Baseline Optics:** Camera zoom/FOV, height/distance, projection, and rotation remain completely fixed across all levels:
  - Position Baseline: `(0, 13.04, -0.17)`
  - Rotation: `(88, 0, 0)` Euler angles (pitch = 88°)
  - Projection: `Perspective`
  - Field of View (FOV): `78°`
- **Dynamic Level-Size Zoom Disabled:** Camera height and FOV do NOT adapt to `GridWidth` or `GridHeight`. Levels 5x8 and 20x20 render at identical scale.
- **Initial Centering:** Upon level load or restart, camera initializes centered over the board footprint: `initialCameraPosition = new Vector3(boardCenter.x, 13.04f, boardCenter.z - 0.17f)`.
- **Exact Viewport-Plane Projection:** Camera visible footprint is calculated via exact ray-plane intersection (`Camera.ViewportPointToRay` with `Plane(Vector3.up, 0)`) at viewport corners `(0,0)`, `(1,0)`, `(0,1)`, `(1,1)`.
- **Independent Axis Overflow:**
  - `CanPanX = boardBounds.size.x > visibleFootprint.x`
  - `CanPanZ = boardBounds.size.z > visibleFootprint.z`
  - If the layout fits on an axis, panning on that axis is locked. Small levels that fit entirely in Game View lock both axes (`CanPanX = false`, `CanPanZ = false`).
- **Pan Boundary Clamping:** Clamped strictly to board bounds:
  - `minCamX = boardCenter.x - (boardBounds.size.x - visibleFootprint.x) * 0.5f`
  - `maxCamX = boardCenter.x + (boardBounds.size.x - visibleFootprint.x) * 0.5f`
  - `minCamZ = (boardCenter.z + baselineZOffset) - (boardBounds.size.z - visibleFootprint.z) * 0.5f`
  - `maxCamZ = (boardCenter.z + baselineZOffset) + (boardBounds.size.z - visibleFootprint.z) * 0.5f`
- **Unified Drag Input:** Mouse drag and single-touch mobile dragging use the exact same pipeline (`BoardCameraController.OnDragStart`, `OnDrag`, `OnDragEnd`).
- **Tap vs Drag Threshold:** Evaluated in `PlayerInputHandler` with a `12px` movement threshold. Movement under 12px treats gesture as a tap (ant selection/movement); movement >= 12px transitions to camera drag.
- **Heart Safety:** Camera drag NEVER counts as a gameplay move and NEVER deducts a heart/life (`LoseLife()` is never called from camera dragging).
- **UI Safety:** Pointer down over UI (`EventSystem.current.IsPointerOverGameObject`) cancels drag initiation, ensuring buttons and modals retain priority.
- **State Gating:** Panning is only permitted during `GameState.Playing`.
- **Level Load/Restart:** Reset offset to zero and position camera to `initialCameraPosition`.

## 4. Current Level Configuration

Level 1
Asset:
Assets/Data/ReferenceLevels/Level_001.asset

Grid:
8x5

Colors:
0 SkyBlue
2 Yellow
7 Green
8 Brown
10 Red

Holders:
5

Queues:
5

---

Level 2
Asset:
Assets/Data/ReferenceLevels/Level_002.asset

Grid:
10x10

Colors:
0 SkyBlue
1 Lime
4 Orange
6 Indigo
10 Red

Holders:
5

Queues:
10

---

Level 3
Asset:
Assets/Data/ReferenceLevels/Level_003.asset

Grid:
10x10

Colors:
0 SkyBlue
2 Yellow
3 Purple
6 Indigo
7 Green

Holders:
5

Queues:
15

---

Level 4
Asset:
Assets/Data/ReferenceLevels/Level_004.asset

Grid:
10x10

Colors:
1 Lime
2 Yellow
4 Orange
5 Pink
6 Indigo
8 Brown
9 White
10 Red
11 Turquoise

Holders:
9

Queues:
14

---

Level 5
Asset:
Assets/Data/ReferenceLevels/Level_005.asset

Grid:
15x15

Colors:
0 SkyBlue
2 Yellow
5 Pink
6 Indigo
7 Green
8 Brown

Holders:
6

Queues:
23

---

Level 6 (Screenshot 4, Badge 6)
Asset:
Assets/Data/ReferenceLevels/Level_006.asset

Grid:
20x20

Holders:
11 (SkyBlue: 10, White: 2, White: 2, Purple: 5, Orange: 2, Yellow: 3, Red: 1, Lime: 1, White: 1, Indigo: 2, Indigo: 3)

Queues:
32

---

Level 7 (Screenshot 5, Badge 7)
Asset:
Assets/Data/ReferenceLevels/Level_007.asset

Grid:
15x15

Holders:
5 (Pink center: 12, Indigo top-left: 4, Turquoise top-right: 4, Green bottom-left: 4, Orange bottom-right: 4)

Queues:
28 (26 rope-tied / connected in groups)

---

Level 8 (Screenshot 2, Badge 8)
Asset:
Assets/Data/ReferenceLevels/Level_008.asset

Grid:
15x15

Holders:
14 (Purple: 3, Orange: 2, Red: 2, Red: 3, Indigo: 2, Yellow: 2, Pink: 2, Brown: 1, White: 3, Lime: 4, Turquoise: 1, Black: 3, Brown: 1, Orange: 1)

Queues:
30 (7 rope-tied / connected in groups)

---

Level 9 (Screenshot 1, Badge 9)
Asset:
Assets/Data/ReferenceLevels/Level_009.asset

Grid:
15x15

Holders:
17 (Brown: 9, Brown: 7, Brown: 2, Pink: 5, Red: 4, Indigo: 4, SkyBlue: 2, Orange: 2, Green: 2, Lime: 2, Indigo: 2, SkyBlue: 1, Red: 2, Pink: 2, Orange: 2, Green: 2, Indigo: 2)

Queues:
52 (43 rope-tied / connected in groups)

---

Level 10 (Screenshot 3, Badge 10)
Asset:
Assets/Data/ReferenceLevels/Level_010.asset

Grid:
20x20

Holders:
18 (Yellow: 4, Lime: 5, Indigo: 4, Red: 5, Pink: 1, Brown: 4, White: 3, Orange: 5, Purple: 1, Turquoise: 3, Black: 1, SkyBlue: 3, Green: 2, Purple: 1, Green: 1, Red: 1, Red: 1, Turquoise: 2)

Queues:
47 (16 rope-tied / connected in groups)

### AUTHORITATIVE REFERENCE GRID DIMENSIONS

REFERENCE GAME AUTHORITATIVE GRID SIZE
DO NOT CHANGE WITHOUT NEW SOURCE DATA

01 = 8x5
02 = 10x10
03 = 10x10
04 = 10x10
05 = 15x15
06 = 20x20
07 = 15x15
08 = 15x15
09 = 15x15
10 = 20x20
11 = 7x25
12 = 30x30
13 = 20x20
14 = 30x30
15 = 13x13
16 = 17x17
17 = 20x20
18 = 20x20
19 = 30x30
20 = 17x17
21 = 12x12
22 = 15x15
23 = 25x30
24 = 20x20
25 = 25x30
26 = 20x20
27 = 30x30
28 = 25x40
29 = 30x30
30 = 11x11
31 = 15x15
32 = 25x25
33 = 25x25
34 = 30x30
35 = 20x20
36 = 30x30
37 = 15x15
38 = 30x30
39 = 20x25
40 = 20x20
41 = 30x30
42 = 30x30
43 = 20x20
44 = 32x31
45 = 40x40

## 5. Current Visual Systems (Ants & Holders)

### AI Asset Kit & Ant Prefab Integration Status

ANT PREFAB INTEGRATION:
REVERTED

AI AntQueueKit Ant_* prefabs are currently NOT used by runtime gameplay.

Native/original AntUnit visuals have been restored.

AntQueueKit Assets:
IMPORTED / UNUSED

- **AI Ant Prefabs Status:** `Assets/AntQueueKit/Prefabs/Ant_*.prefab` (`Ant_Cyan`, `Ant_Green`, `Ant_Pink`, `Ant_Yellow`, `Ant_Indigo`, `Ant_Brown`) are completely detached from runtime gameplay.
- **AntUnit Visual Hierarchy:** AntUnit instantiates native 3D geometry (`AntModel_3D` with `WorkerAntMesh` and `HeadPivot` with expressive googly eyes). No `VisualRoot` or 2D AI sprite child is created.
- **AI Hole Artwork:** `Hole_*.prefab` remain preserved in `Assets/AntQueueKit/Prefabs/` (not deleted). Live gameplay holders continue using procedural stadium slot geometry (`HoleSlotRoot`, `SlotRim`, `SlotInnerBevel`, `SlotPit`).

### Ant Material & Visual Appearance
- **Shader:** `Universal Render Pipeline/Lit`
- **Surface Appearance:** Smooth toy-like glossy plastic finish, rounded 3D beads.
- **Metallic:** `0.0`
- **Smoothness:** `0.72`
- **Emission:** `OFF`
- **Base Map / Textures:** `null` (Clean Lit shading with zero sprite border artifacts).
- **Eyes:** Shared glossy white (`Smoothness: 0.85`) with dark pupil (`Smoothness: 0.90`).
- **Legs:** Shared dark brown/charcoal (`Smoothness: 0.50`).
- **Material Architecture:** Single shared material per `AntColor` cached in `AntColorPalette.GetMaterial()`. No per-ant runtime allocations.

### Holder Visual System
Procedural stadium slot geometry generated under `HoleSlotRoot`:
- **SlotRim:** Glossy rounded-rectangle outer ring mesh (`outerW = effectiveW * cs - 0.06f * cs`, Y = 0.032).
  - Shader: `Universal Render Pipeline/Lit`
  - Base Color: 100% identical RGB hue to matching ant body (`AntColorPalette.GetColor(color)`).
  - Metallic: `0.0`
  - Smoothness: `0.65`
- **SlotInnerBevel:** Inner bevel wall dropping into pit.
  - Shader: `Universal Render Pipeline/Lit`
  - Base Color: `Color.Lerp(GetColor(antColor), HolePitColor, 0.35f)`.
  - Metallic: `0.0`
  - Smoothness: `0.55`
- **SlotPit:** Recessed pit floor.
  - Shader: `Universal Render Pipeline/Lit`
  - Base Color: `#1F252B` / `Color(0.122, 0.145, 0.169)` (dark neutral charcoal-blue).
  - Metallic: `0.0`
  - Smoothness: `0.10` (matte, low specular response).
- **SlotCounterBadge:** Badge parent transform positioned on rim edge based on orientation.
- **BadgeMesh:** Capsule / rounded background mesh (`badgeMat` tinted to holder color, `Smoothness: 0.65`).
- **BadgeCountText:** High-contrast 3D TextMesh display for remaining queue capacity (pure white `#FFFFFF`).
- **Footprint sizing method:** Bounded by `OccupiedCells` (`MinX..MaxX`, `MinY..MaxY`), determining `WidthCells` and `HeightCells`.
- **Corner radius behavior:** `cornerRadius = Mathf.Min(0.48f * cs, outerH * 0.48f)`.
- **Holder disappearance behavior:** On counter zero, colliders are disabled immediately, diagnostic log is written, root scales to 0 (`0.20s`, `Ease.InBack`), and GameObject is destroyed.
- **Material Architecture:** Single shared material per `AntColor` for rim and bevel, plus single shared material for pit cached in `AntColorPalette`. Zero per-hole runtime allocations.

### Single-Queue Holder Override

Condition:
Apply only when INITIAL queue count == 1.

Parameters applied:
- SlotRim scale X = 1.9205
- SlotInnerBevel scale X = 2.3861
- SlotPit scale X = 2.6535
- SlotCounterBadge local position = (0.35, 0, 0)
- BadgeMesh local position = (0.25, 0, -0.25)
- BadgeCountText local position = (0.25, 0.22, -0.25)

## 6. Holder Completion Behavior

When final matching queue completely enters:

remaining queue count
-> 0
-> holder IsCompleted
-> entire OccupiedCells footprint unregistered from BoardManager
-> grid becomes immediately walkable
-> holder disappearance effect
-> holder GameObject destroyed

- Grid release must occur BEFORE visual destruction.
- Completion state must not depend on a destroyed HoleController reference (tracked via `BoardManager.CompletedHoleCount` and `BoardManager.CompletedHoleIds` set of stable string IDs).
- `BoardManager.GetHoleAt(coord)` safely purges stale / completed references if encountered.

## 7. UI State

Managed by `GameUIManager.cs`:
- **Win Popup (`winPanel`):** Activated when `GameState == GameState.LevelWon`. Displays level completed state, scales up with `Ease.OutBack`. Contains `nextLevelButton` calling `GameManager.Instance.NextLevel()`.
- **Level Failed Overlay (`losePanel`):** Activated when `GameState == GameState.LevelLost` (lives depleted, deadlock detected, or timer expiry). Displays full-screen darkened gameplay background (`failDimBackground`), large centered "LEVEL FAILED!" title art (`failTitleTransform`), big broken red heart centerpiece (`failHeartTransform`), and glossy red "Restart" button (`failRestartButtonTransform` / `retryButton`).
- **Restart Behavior:** Top-bar restart button or fail overlay restart button directly reloads active level via `GameManager.Instance.RestartLevel()`.
- **Settings Modal (`settingsModal`):** Animated card overlay (`settingsCard`) with sound/music toggles and restart shortcut.
- **Gameplay Blocking Overlays:** Modals and win/lose panels block further board raycasts/inputs while open. `failDimBackground` raycast target blocks all pointer/touch input on the game board while the fail screen is visible.

## 8. DO NOT TOUCH / Safety Rules

- Do not run `LevelGeneratorWindow` unless explicitly requested.
- Do not regenerate all levels automatically.
- Do not bake runtime queues/holes into `main.unity`.
- Do not overwrite recovered reference level assets without explicit approval.
- Do not modify `.meta`/GUID files unnecessarily.
- Do not change Levels 6+ while specifically testing Levels 1–5.
- Preserve legacy assets unless explicitly replacing them.
- Avoid unrelated refactors while fixing a localized problem.

## 9. Known Issues / Pending Work

None currently active. (ISSUE-001 was closed upon removal of obsolete test scripts).

## 10. Important Files

Assets/Scripts/LevelSystem/LevelManager.cs
Purpose: Manages active level progression, PlayerPrefs persistence, and routes level loading from active LevelDatabase.

Assets/Scripts/LevelSystem/LevelDatabase.cs
Purpose: ScriptableObject holding the ordered list of playable Level assets.

Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs
Purpose: Decouples raw reference level YAML data, extracting runtime holes and ant queues dynamically without modifying asset files.

Assets/Scripts/Core/ReferenceColorMapper.cs
Purpose: Authoritative 0..11 raw ColorID mapping to distinct AntColor enums, visual colors, and materials.

Assets/Scripts/Core/AntColor.cs
Purpose: AntColor enum definitions, palette lookup (`AntColorPalette`), and shared material generation.

Assets/Scripts/Board/BoardManager.cs
Purpose: Central board controller; manages runtime grid lifecycle, `holesMap` occupancy, active queues/holes, pathfinding queries, and `RemoveCompletedHole()`.

Assets/Scripts/Board/HoleController.cs
Purpose: Procedural stadium slot geometry, single-queue overrides, badge text, queue-level swallow counter, and disappearance sequence.

Assets/Scripts/Ants/AntQueueController.cs
Purpose: Slither chain movement, path traversal, `DestinationHole` caching, and dispatch upon arrival.

Assets/Scripts/Ants/AntUnit.cs
Purpose: Individual ant segment visuals, orientation, material assignment, and swallow animations.

Assets/Scripts/UI/GameUIManager.cs
Purpose: Manages top bar HUD, Win/Lose panels, settings modal, and event subscriptions for timer, lives, and game state.

## 11. Current Compile / Test Status

Compile errors: 0
Warnings: 8

Known warnings:
- `BoardManager.preserveSceneObjects` unused (CS0414)
- `HoleController` legacy visual fields unused (`singleRowWidth`, `singleRowHeight`, `multiRowWidth`, `multiRowHeight`, `tubeRadius`, `tubeHeight`) (CS0414)
- `AntQueueController.s_DebugLoggedOneRefAnt` unused (CS0414)

### VALIDATION POLICY:

Gameplay correctness is primarily verified manually in Unity Play Mode by playing the actual levels.

Do not create new automated NUnit/EditMode/PlayMode tests unless explicitly requested by the user.

When implementing future gameplay features:
- compile through Unity
- inspect Console
- test through Unity MCP / Play Mode where practical
- allow the user to manually play the level for final verification

Do NOT automatically generate test scripts after every feature.

## 12. Change History

### 2026-09-24 01:30 — Initialized ANTIGRAVITY_CONTEXT.md

User request:
Create and permanently maintain a project context file for this Unity project.

Files changed:
- Assets/Documentation/ANTIGRAVITY_CONTEXT.md

Changes:
- Created authoritative project context documentation following the required 12-section structure.
- Initialized verified runtime architecture, data rules, Levels 1–5 configuration, holder visual override parameters, and UI state.
- Documented safety rules, known warnings (8 compiler warnings), and logged ISSUE-001 regarding test grid dimension assertions.

Behavior before:
Context was spread across scattered notes and previous commits without a dedicated persistent context file in `Assets/Documentation/`.

Behavior after:
`Assets/Documentation/ANTIGRAVITY_CONTEXT.md` serves as the single source of truth for runtime architecture, authoritative rules, level configurations, and change tracking across Antigravity sessions.

Tests:
- compile: PASS (0 errors, 8 known warnings)
- runtime: PASS (Levels 1–5 verified in EditMode test suite)
- relevant test names: ReferenceLevels1To5Tests (Test02–Test08), LevelDatabaseTests

Outstanding:
- ISSUE-001: Resolve grid dimension assertion discrepancy in Test01 / TestLevels1To5Validation.

### 2026-09-24 06:35 — Implemented 3 Gameplay Rules (Forward Movement, Heart Penalties, 3 Lives Failure)

User request:
1. Ant queues must always move FORWARD from the head.
2. Any invalid/wrong move or collision costs exactly 1 heart.
3. Player starts with 3 hearts. When hearts reach 0, the level fails.

Files changed:
- Assets/Scripts/Core/GameManager.cs
- Assets/Scripts/Board/AntPathfinder.cs
- Assets/Scripts/Board/BoardManager.cs
- Assets/Scripts/Ants/AntQueueController.cs
- Assets/Scripts/Core/PlayerInputHandler.cs
- Assets/Scripts/UI/GameUIManager.cs
- Assets/Scripts/Editor/TestGameplayRules.cs

Changes:
- Added `InvalidMoveReason` enum and centralized `GameManager.LoseLife(InvalidMoveReason reason)`. Clamps lives >= 0 and triggers `LevelLost` when lives == 0.
- Level loading/restart resets `playerLives = 3` and updates UI.
- Enforced forward-movement rule in `AntPathfinder.cs` by excluding `neck` only on initial step from `head` and asserting `path[1] != neck`.
- Added single attempt-level deduction in `BoardManager.TryStartMove(queue)` and fallback `AntQueueController.TryStartMove()`.
- Added runtime collision safety check in `AntQueueController.MoveRoutine()` to detect unexpected collisions and deduct 1 heart cleanly.
- Added GameState input gating in `PlayerInputHandler.cs` so input is blocked when not `GameState.Playing`.
- Updated `GameUIManager.UpdateLivesUI(lives)` to toggle `gameObject.SetActive` and `enabled` on `Heart_1`, `Heart_2`, `Heart_3`.

Tests:
- compile: PASS (0 errors, 8 pre-existing warnings)
- runtime / playmode: PASS (Forward movement verified across all queues, lives sequence 3->2->1->0 tested, input blocking verified, restart tested)
- test suite: `Tools/Slither Ants Jam/Tests/Validate 3 Gameplay Rules` (PASS)

### 2026-09-24 18:10 — Removed Obsolete Automated Test & Validation Scripts

User request:
Remove unnecessary project-specific automated test and validation scripts. Manual Play Mode testing is now the preferred validation method.

Files removed:
- `Assets/Tests/EditMode/ReferenceLevels1To5Tests.cs` (and meta)
- `Assets/Tests/EditMode/LevelDatabaseTests.cs` (and meta)
- `Assets/Tests/EditMode/BoardCameraControllerTests.cs` (and meta)
- `Assets/Tests/EditMode/EditModeTests.asmdef` (and meta)
- `Assets/Tests/PlayMode/GameplayInitializationTests.cs` (and meta)
- `Assets/Tests/PlayMode/ReferenceProgressionTests.cs` (and meta)
- `Assets/Tests/PlayMode/PlayModeTests.asmdef` (and meta)
- `Assets/Tests/EditMode` folder
- `Assets/Tests/PlayMode` folder
- `Assets/Tests` folder
- `Assets/Scripts/Editor/TestLevels1To5Validation.cs` (and meta)
- `Assets/Scripts/Editor/TestEditPlayParity.cs` (and meta)
- `Assets/Scripts/Editor/TestFrameworkRunner.cs` (and meta)
- `Assets/Scripts/Editor/TestGameplayRules.cs` (and meta)
- `Assets/Scripts/Editor/TestReferenceLevel001Adapter.cs` (and meta)
- `Assets/Scripts/Editor/TestReferenceLevelDatabase.cs` (and meta)

Files updated:
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- Removed temporary automated test suites and validation scripts.
- Removed custom `Tools > Slither Ants Jam > Tests > ...` editor menu commands.
- Removed obsolete test assemblies (`EditModeTests.asmdef`, `PlayModeTests.asmdef`) and pruned the empty `Assets/Tests` directory hierarchy.
- Closed and removed ISSUE-001 (Grid Dimension Discrepancy) from Known Issues as it was caused solely by legacy expectations in the deleted test scripts.
- Added the official Validation Policy establishing manual Play Mode gameplay testing as the primary verification method.
- Preserved `com.unity.test-framework` package in `Packages/manifest.json`.
- Runtime gameplay scripts, authoritative level assets, and database were NOT modified.

Tests:
- compile: PASS (0 errors, 8 pre-existing warnings)
- runtime / console: PASS (0 errors)

### 2026-09-24 18:35 — Fixed-View Gameplay Camera with Clamped Drag/Pan

User request:
Replace dynamic camera framing/zoom with a consistent fixed-view camera (fixed position height, pitch, FOV, projection) across all levels. Panning is enabled via dragging ONLY when the layout exceeds the visible viewport footprint on the board plane, with independent axis overflow and strict board bounds clamping. Distinguish tap vs drag via threshold to prevent camera drag from moving ants or costing hearts.

Baseline Camera:
- Position: (0, 13.04, -0.17)
- Rotation: (88, 0, 0)
- Projection: Perspective
- Field of View (FOV): 78

Files changed:
- `Assets/Scripts/Camera/BoardCameraController.cs`
- `Assets/Scripts/Board/BoardManager.cs`
- `Assets/Scripts/Core/PlayerInputHandler.cs`
- `Assets/Scenes/main.unity`
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- Overhauled `BoardCameraController.cs`:
  - Established fixed baseline optics (`cameraHeight = 13.04f`, `cameraPitch = 88f`, `fixedFOV = 78f`, `baselineZOffset = -0.17f`).
  - Completely removed dynamic FOV/height level-size calculations.
  - Implemented exact board bounds calculation (`CalculateBoardBounds(level)`) accounting for grid dimensions, holders, queues, and visual padding (`0.35 * cs`).
  - Implemented exact visible footprint calculation (`CalculateVisibleFootprint()`) by intersecting 4 viewport corner rays (`ViewportPointToRay`) with board plane (`Plane(Vector3.up, 0)`).
  - Implemented independent X/Z overflow detection (`boardSize > visibleFootprint`) and strict pan limit clamping (`[minCamX, maxCamX]`, `[minCamZ, maxCamZ]`).
  - Added world-space delta projection for natural drag feel (`previousWorld - currentWorld`).
  - Implemented `ResetToCenter()` on level load/restart.
  - Added diagnostic logging format `[CameraPan]`.
  - Added dynamic screen resolution / aspect ratio change detection in `Update()`.
- Refactored `BoardManager.cs`:
  - Redirected `AdjustCameraFraming(Level level)` directly to `BoardCameraController.Instance.SetupCameraForLevel(level)` without dynamic height manipulation.
- Enhanced `PlayerInputHandler.cs`:
  - Added pointer drag pipeline with `12px` movement threshold (`dragThresholdPixels = 12f`).
  - Added UI raycast check (`EventSystem.current.IsPointerOverGameObject`) to prevent camera drag when interacting with buttons/modals.
  - Dispatches `OnDragStart`, `OnDrag`, and `OnDragEnd` to `BoardCameraController`.
  - Suppresses ant selection and move attempts when `isCameraDragging == true`.
  - Strictly guarantees camera dragging NEVER calls `GameManager.LoseLife()` and costs 0 hearts.
  - Unified mouse and single-touch mobile dragging.
- Updated `Assets/Scenes/main.unity`:
  - Configured `BoardCameraController` component on `GameController` with `useCinemachine: false` and `cameraPitch: 88`.

Tests:
- compile: PASS (0 errors, 8 pre-existing warnings)
- live unity testing:
  - Test 1 (Small Level 1): `FitsViewport = True`, `CanPanX = False`, `CanPanZ = False`, camera fixed and ant tap works (PASS).
  - Test 2 (Large Level 6 20x20): `CanPanX = True`, `CanPanZ = True`, fixed optics identical (`H=13.04`, `FOV=78`, `Pitch=88`) (PASS).
  - Test 3 (Pan Bounds): Camera clamped strictly to board bounds `min=-5.49, max=5.49` (PASS).
  - Test 4 (Tap vs Drag): Tap triggers normal ant movement; drag beyond 12px moves camera and suppresses ant move (PASS).
  - Test 5 (Heart Safety): Recorded `LivesBefore = 3`, performed 5 camera drags, `LivesAfter = 3` (PASS).
  - Test 6 (Restart): Camera panned away, restarted level, camera reset to `(0.00, 13.04, -0.17)` (PASS).
  - Test 7 (Different Aspect): Dynamic aspect ratio recalculation verified without code changes (PASS).

### 2026-09-24 19:45 — Visual Polish: Reference Matched Ant Body & Holder Colors and Shading

User request:
Use Level 4 and Level 5 reference screenshots as visual reference to update ant/slither body colors, ant material/shading/shine (URP Lit, Metallic 0, Smoothness 0.65–0.80), holder/hole colors, and holder material/shading/depth (SlotRim matching ant body 100%, downward inner bevel, dark recessed pit). One centralized authoritative palette. Camera and gameplay untouched.

Files changed:
- `Assets/Scripts/Core/AntColor.cs`
- `Assets/Scripts/Board/HoleController.cs`
- `Assets/Scripts/Ants/AntUnit.cs`
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- **Sampled Midtones directly from Level 4 & Level 5 Screenshots:**
  - Raw 0 SkyBlue: `#1298E6` / `(0.071, 0.596, 0.902)`
  - Raw 1 Lime: `#66C30E` / `(0.400, 0.765, 0.055)`
  - Raw 2 Yellow: `#FEBD04` / `(0.996, 0.741, 0.016)`
  - Raw 3 Purple: `#7A3DCE` / `(0.478, 0.239, 0.808)`
  - Raw 4 Orange: `#EC7017` / `(0.925, 0.439, 0.090)`
  - Raw 5 Pink: `#F24FE6` / `(0.949, 0.310, 0.902)`
  - Raw 6 Indigo: `#415ACD` / `(0.255, 0.353, 0.804)`
  - Raw 7 Green: `#2B8428` / `(0.169, 0.518, 0.157)`
  - Raw 8 Brown: `#A26122` / `(0.635, 0.380, 0.133)`
  - Raw 9 White: `#ECE0D2` / `(0.925, 0.878, 0.824)`
  - Raw 10 Red: `#E83F40` / `(0.910, 0.247, 0.251)`
  - Raw 11 Turquoise: `#08EBC1` / `(0.031, 0.922, 0.757)`
  - SlotPit: `#1F252B` / `(0.122, 0.145, 0.169)`
- **Eliminated Murky Mud Tinting on Ant Units:**
  - Removed legacy assignment of 2D sprite textures (`red0.png`, etc.) to `_BaseMap` in `AntColorPalette.GetMaterial()`.
  - With collapsed mesh UVs, sprites previously sampled dark maroon edge pixels which multiplied with `_BaseColor`. Clean URP Lit without textures restored vibrant candy hues and bright specular shine.
- **Ant Shading & Specular Response:**
  - URP Lit shader, Metallic `0.0`, Smoothness `0.72`, Emission `OFF`.
  - Natural rounded specular highlights following 3D bead mesh contours.
  - Shared eye materials (white smoothness `0.85`, dark pupils smoothness `0.90`) and leg materials (smoothness `0.50`).
- **Holder Material Architecture:**
  - SlotRim: 100% shared base color with matching ant, Metallic `0.0`, Smoothness `0.65`.
  - SlotInnerBevel: Derived via `Color.Lerp(GetColor(color), HolePitColor, 0.35f)`, Metallic `0.0`, Smoothness `0.55`.
  - SlotPit: Dark recessed neutral `#1F252B`, Metallic `0.0`, Smoothness `0.10`.
  - Shared cached materials in `AntColorPalette` eliminating per-hole and per-ant runtime allocations.
- **Visual Parity Verification:**
  - Verified in Unity Play Mode on Level 4 (Red, Lime, Pink, Indigo, Orange, Brown, Yellow, Turquoise, White).
  - Verified in Unity Play Mode on Level 5 (SkyBlue, Green, Pink, Brown, Yellow, Indigo).
  - Both levels confirmed vibrant, glossy, soft toy-plastic appearance matching the reference screenshots.
  - Camera behavior, auto-scaling, framing, FOV, and level data completely preserved.

Tests:
- compile: PASS (0 errors, 8 pre-existing warnings)
- runtime / console: PASS (0 gameplay errors)

### 2026-09-24 20:15 — Dual Queue Movement Concurrency & Inbound Reservation System

User request:
Allow up to two ant queues to move simultaneously (`MAX_CONCURRENT_MOVING_QUEUES = 2`). Player can tap Queue B while Queue A is in-flight; both animate in parallel. Third queue tap is temporarily blocked with ZERO heart penalty. Simultaneous queues may target different holders or the same holder (if capacity permits). Inbound reservations prevent holder oversubscription. Holder counter decrements only on complete queue ingestion. Conflicting paths prevented before move start via dynamic path cell reservations. All state resets on restart/load.

Files changed:
- `Assets/Scripts/Board/HoleController.cs`
- `Assets/Scripts/Ants/AntQueueController.cs`
- `Assets/Scripts/Board/AntPathfinder.cs`
- `Assets/Scripts/Board/BoardManager.cs`
- `Assets/Scripts/Core/PlayerInputHandler.cs`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- **`HoleController.cs`:**
  - Added inbound reservation tracking: `inboundReservations`, `AvailableCapacity => Mathf.Max(0, remainingCount - inboundReservations)`, `HasAvailableCapacity => !isCompleted && AvailableCapacity > 0`.
  - Updated `IsFull => isCompleted || AvailableCapacity <= 0`.
  - Added `TryReserveSlot()` and `ReleaseReservation()` with structured diagnostic logging `[HolderReservation]`.
  - Updated `OnQueueSwallowed()` to decrement `inboundReservations`, decrement `remainingCount`, and only trigger completion removal when `remainingCount <= 0 && inboundReservations <= 0`.
  - Reset `inboundReservations = 0` in `Initialize()`.
- **`AntQueueController.cs`:**
  - Exposed settable `DestinationHole` property.
  - Implemented `GetReservedPathCells()` returning path cells ahead of head (excluding final hole entrance).
  - Updated `MoveRoutine()` to prune `remainingPath` as segments advance, releasing vacated grid cells dynamically.
  - Added cleanup on collision/interruption in `KillAllTweens()` to release holder reservation and notify `BoardManager.OnQueueMoveStoppedEarly(this)`.
- **`AntPathfinder.cs`:**
  - In `FindPath()`, augmented Step 2 blocked cells check with `otherQueue.GetReservedPathCells()` for any moving queue.
  - Guaranteed that concurrent paths do not intersect or overlap during simultaneous execution.
- **`BoardManager.cs`:**
  - Defined `MAX_CONCURRENT_MOVING_QUEUES = 2` and `movingQueues` tracking set (`HashSet<AntQueueController>`).
  - Removed global single-queue lock.
  - Updated `TryStartMove()`:
    - If `movingQueues.Count >= MAX_CONCURRENT_MOVING_QUEUES`, soft-blocks with shake feedback and logs `[QueueMoveBlocked] Reason=ConcurrentMoveLimit HeartPenalty=False`. Costs 0 hearts (`LoseLife` NOT called).
    - Reserves destination slot via `targetHole.TryReserveSlot()` before starting move (with rollback on failure).
    - Tracks active queue in `movingQueues`.
  - Updated `OnQueueCompleted()` and `OnQueueMoveStoppedEarly()` to remove queues from `movingQueues`.
  - Cleared `movingQueues` in `Awake()` and `ClearBoard()`.
- **`PlayerInputHandler.cs`:**
  - Removed `canInteract = false` in `HandleMoveStarted`, permitting taps to reach `BoardManager.TryStartMove()` while 1 queue is already in-flight.
- **`GameManager.cs`:**
  - Updated `CheckGameWinLossConditions()` to skip deadlock evaluation while `BoardManager.Instance.MovingQueuesCount > 0`, avoiding premature game over while queues are actively clearing paths.

Tests:
- compile: PASS (0 errors, 8 pre-existing warnings)
- live unity testing (Level 4):
  - Test 1 (One Queue): Started Queue A, `MovingQueuesCount = 1`, moves normally (PASS).
  - Test 2 (Second Queue While First Moving): Started Queue B while A moving, `MovingQueuesCount = 2`, both animated simultaneously (PASS).
  - Test 3 & 9 (Third Queue & Heart Safety): Tapped Queue C while 2 moving -> blocked, `LivesBefore = 3`, `LivesAfter = 3` (PASS).
  - Test 4 & 5 (Slot Frees & Different Holders): Queues swallowed, counters decremented, slots freed to 0 (PASS).
  - Test 6 (Same Holder Two Queues): Started two Turquoise queues into Turquoise holder (counter 2). Both moved concurrently, swallowed sequentially, counter 2 -> 1 -> 0, holder removed only after second finished (PASS).
  - Test 7 (Holder Oversubscription): Yellow holder (capacity 1) reserved by Yellow queue (`Avail=0`); second reservation failed, prevented duplicate targeting (PASS).
  - Test 8 (Path Conflict): `GetReservedPathCells()` blocked conflicting routes while allowing disjoint routes (PASS).
  - Test 10 (Timer Failure / Terminal State): LevelLost stops new moves; completed queues during loss cannot cause Win (PASS).
  - Test 11 (Restart): `RestartLevel()` reset `PlayerLives = 3`, `State = Playing`, `MovingQueuesCount = 0`, all inbound reservations reset to 0 (PASS).

### 2026-09-25 00:30 — Targeted Revert: AI Ant Prefab Visual Integration

User request:
Revert ONLY the AI-generated ant prefab visual integration (`Ant_Cyan`, `Ant_Green`, `Ant_Pink`, `Ant_Yellow`, `Ant_Indigo`, `Ant_Brown` from `Assets/AntQueueKit/Prefabs/`). Restore the ant visual system to exactly how it worked immediately before the integration (native 3D worker ant model and procedural composite fallback). Preserve newer gameplay additions including 4 concurrent moving queues (`MAX_CONCURRENT_MOVING_QUEUES = 4`), heart deduction rules, 3-life game over, holder mechanics, camera, and level data. Keep `Assets/AntQueueKit/` in the project marked as IMPORTED / UNUSED.

Files changed:
- `Assets/Scripts/Ants/AntUnit.cs`
- `Assets/Scripts/Core/AntVisualLibrary.cs`
- `Assets/Scripts/Core/AntColor.cs`
- `Assets/Scripts/Board/BoardManager.cs`
- `Assets/Resources/AntVisualLibrary.asset`
- `Assets/Scenes/main.unity`
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- **`AntUnit.cs`:**
  - Removed AI 2D sprite visual child instantiation (`VisualRoot` and `Instantiate(aiPrefab)`).
  - Restored native 3D ant visual generation in `BuildVisuals()`: uses `AntModel_3D` mesh filter with `WorkerAntMesh` (from `AntColorPalette.GetWorkerAntMesh()`), shared URP Lit glossy body material (`AntColorPalette.GetMaterial(antColor)`), and `HeadPivot` with expressive googly eyes (shared eye white and pupil materials).
  - Preserved procedural composite fallback (`Abdomen`, `Thorax`, `HeadSphere`, `FootBead`, `Antenna`) if mesh is unavailable.
- **`AntVisualLibrary.cs` & `AntVisualLibrary.asset`:**
  - Stripped all ant prefab fields (`antCyanPrefab`, `antGreenPrefab`, `antPinkPrefab`, `antYellowPrefab`, `antIndigoPrefab`, `antBrownPrefab`), `antScale`, `GetAntPrefab()`, and `SetAntPrefabs()`.
  - Preserved hole prefab references (`Hole_*.prefab`) without disruption.
- **`AntColor.cs`:**
  - Removed `visualLibrary`, `VisualLibrary`, `GetAntVisualPrefab()`, and `AntVisualScale`.
  - Preserved full 12-color authoritative palette, `GetWorkerAntMesh()`, shared materials, and URP Lit shading.
- **`BoardManager.cs`:**
  - Removed `visualLibrary` serialized field and runtime assignment.
  - Preserved authoritative `MAX_CONCURRENT_MOVING_QUEUES = 4`.
  - Preserved fifth queue soft-block without heart penalty.
- **`Assets/Scenes/main.unity`:**
  - Cleaned up serialized references and removed baked `VisualRoot` / `Ant_*` children from scene queues.
- **`Assets/AntQueueKit/`:**
  - Left completely intact in project; marked as **IMPORTED / UNUSED**.

Tests:
- compile: PASS (0 errors, 8 pre-existing warnings)
- live unity testing (Play Mode):
  - Ant Visual Check: 77/77 active AntUnits verified using native 3D `AntModel_3D` + `HeadPivot` (PASS).
  - AI Ant Prefab Check: 0 `VisualRoot` objects, 0 `Ant_*` AI prefabs instantiated (PASS).
  - Head-first forward movement: Verified (PASS).
  - Follow-the-leader: Verified (PASS).
  - 4 Simultaneous Queues: 4 queues started concurrently, `MovingQueuesCount = 4` (PASS).
  - Fifth Queue Block: Attempt to start 5th queue blocked (`false`), Lives = 3 -> 3, heart penalty = NONE (PASS).
  - Ingestion / Holder Counter: 4 queues entered holes, counters decremented, completed holes removed (PASS).
  - Lives Depletion: 3 -> 2 -> 1 -> 0 triggers `GameState.LevelLost` (PASS).
  - Timer Expiry: Timer reaching 0 triggers `GameState.LevelLost` (PASS).
  - Restart: `RestartLevel()` cleanly restores lives = 3, timer = limit, state = Playing (PASS).

### 2026-09-25 00:50 — Updated Authoritative Reference Grid Dimensions for Levels 1–45

User request:
Update the authoritative grid dimensions for Levels 1–45 to exactly match the reference game. Target only `Assets/Data/ReferenceLevels/Level_001.asset` through `Level_045.asset`. Validate all coordinates (queues, holders, disabled cells, cellInfo) before saving. Preserve all gameplay rules, cells, color IDs, camera, and prefabs.

Files changed:
- `Assets/Data/ReferenceLevels/Level_001.asset` ... `Level_045.asset` (45 assets)
- `Assets/Scripts/LevelSystem/Level.cs`
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- **`Level.cs`:**
  - Added `SetGridDimensions(int width, int height)` method updating `gridSize` (`Vector2Int`), `gridWidth`, and `gridHeight` in sync.
- **Reference Level Assets (1–45):**
  - Updated serialized `gridSize`, `gridWidth`, `gridHeight` across all 45 reference level assets to exact authoritative values:
    - 01: 8x5, 02–04: 10x10, 05: 15x15, 06: 20x20, 07–09: 15x15, 10: 20x20, 11: 7x25, 12: 30x30, 13: 20x20, 14: 30x30, 15: 13x13, 16: 17x17, 17–18: 20x20, 19: 30x30, 20: 17x17, 21: 12x12, 22: 15x15, 23: 25x30, 24: 20x20, 25: 25x30, 26: 20x20, 27: 30x30, 28: 25x40, 29: 30x30, 30: 11x11, 31: 15x15, 32–33: 25x25, 34: 30x30, 35: 20x20, 36: 30x30, 37: 15x15, 38: 30x30, 39: 20x25, 40: 20x20, 41–42: 30x30, 43: 20x20, 44: 32x31, 45: 40x40.
  - Zero queue cells, holder cells, color IDs, or paths modified.
- **Validation:**
  - Pre-save coordinate validation verified 0 out-of-bounds coordinates across all 45 levels.
  - Database mapping verified: all 45 database indices match 1-to-1 without off-by-one errors.
  - Spot checks in Play Mode confirmed `Level.GridWidth`, `Level.GridHeight`, and `BoardManager` runtime dimensions agree 100% for Level 1 (8x5), Level 5 (15x15), Level 11 (7x25), Level 12 (30x30), Level 28 (25x40), Level 44 (32x31), and Level 45 (40x40).

### 2026-09-25 01:25 — Removed Pathfinding (BFS / AntPathfinder) from Normal Ant Queue Movement

User request:
Remove BFS, shortest-path search, automatic turning, and route selection from normal ant queue movement. Queues must move purely forward in cardinal direction `forwardDirection = head - neck`. Followers strictly follow previous coordinates of the ant ahead. Blocked forward move costs 1 heart; up to 4 queues can move simultaneously (tapping 5th costs 0 hearts). Level data, camera, visual assets, and UI must remain unchanged.

Files changed:
- `Assets/Scripts/Board/BoardManager.cs`
- `Assets/Scripts/Ants/AntQueueController.cs`
- `Assets/Scripts/Core/PlayerInputHandler.cs`
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- **`BoardManager.cs`:**
  - Added `IsInBounds(Vector2Int coord)` helper.
  - Implemented `ValidateForwardMove(...)` projecting a straight cardinal ray in `forwardDirection = head - neck` to find matching holder and validate path clearance.
  - Replaced `AntPathfinder.FindPath` in `TryStartMove()` with `ValidateForwardMove()`.
  - Added structured diagnostic logging: `[ForwardMove]` and `[ForwardMoveBlocked]`.
  - Preserved group reservation, holder slot reservations, 4 concurrent queue limit, 5th queue soft-block (0 heart penalty), and 1 heart penalty on genuine invalid queue move.
  - Updated `IsPathClearForQueue(queue)` to evaluate via `ValidateForwardMove` instead of `AntPathfinder.FindPath`.
- **`AntQueueController.cs`:**
  - Added `forwardDirection` and `holeEntranceCoordinate` serialized fields and public properties.
  - Implemented `StartForwardMovement(Vector2Int direction, HoleController targetHole, Vector2Int entranceCell)`.
  - Implemented `ForwardMoveRoutine()` executing discrete forward hops with exact follower inheritance `newBody[i] = oldBody[i - 1]`.
  - Preserved jump/scale tweening into hole entrance and `hole.OnAntSwallowed()`.
  - Updated `GetReservedPathCells()` to return straight ray coordinates ahead of head to entrance cell.
- **`PlayerInputHandler.cs`:**
  - Replaced `AntPathfinder.FindPath` call in tap validation with `BoardManager.Instance.IsPathClearForQueue(targetQueue)`.
- **`AntPathfinder.cs`:**
  - Completely detached from runtime gameplay movement (0 runtime callers). Preserved in project without deletion.
- **`Assets/Documentation/ANTIGRAVITY_CONTEXT.md`:**
  - Added prominent `CORE MOVEMENT MECHANIC — DO NOT CHANGE` section and updated queue movement specifications.

Tests:
- compile: PASS (0 errors, 8 pre-existing warnings)
- live unity testing (Play Mode):
  - Pathfinding not called: Verified `PathfindingUsed=False` in logs.
  - Forward direction: Verified `Direction = (-1, 0)` for `Head=(12, 9), Neck=(13, 9)` on Level 6.
  - Continues straight: Verified step-by-step progress along same direction.
  - Followers: Verified exact follow-the-leader coordinate replication.
  - Blocked forward: Verified shake feedback, `[ForwardMoveBlocked]`, and exactly 1 heart deduction.
  - Matching holder: Head reaches holder, swallowed sequentially, hole count decrements cleanly.
  - 4 concurrent queues: Started 4 valid queues concurrently, all moved simultaneously.
  - 5th queue: Tapping 5th queue rejected with 0 heart penalty.

### 2026-09-25 01:45 — Verified & Reconstructed Levels 6–10 from Screenshot References

User request:
Create / update Levels 6, 7, 8, 9, and 10 using the 5 uploaded reference screenshots as authoritative source of truth. Match grid sizes (Level 6: 20x20, Level 7: 15x15, Level 8: 15x15, Level 9: 15x15, Level 10: 20x20), queue layouts, orientation, rope-tied connected groups, and holder capacities.

Files changed:
- `Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs`
- `Assets/Scripts/Core/ReferenceColorMapper.cs`
- `Assets/Scripts/Core/AntColor.cs`
- `Assets/Documentation/ANTIGRAVITY_CONTEXT.md`

Changes:
- **Screenshot Mapping Confirmed:**
  - Level 6 = Screenshot 4 (HUD badge 6, 20x20): 32 queues, 11 holders, 187 disabled grid cells shaping the board.
  - Level 7 = Screenshot 5 (HUD badge 7, 15x15): 28 queues (26 rope-tied in groups), 5 holders (Pink 12, Indigo 4, Turquoise 4, Green 4, Orange 4).
  - Level 8 = Screenshot 2 (HUD badge 8, 15x15): 30 queues (7 rope-tied in groups), 14 holders including Charcoal Black (ColorID 12).
  - Level 9 = Screenshot 1 (HUD badge 9, 15x15): 52 queues (43 rope-tied in groups), 17 holders (Brown 9, Brown 7, Brown 2, Pink 5, etc.).
  - Level 10 = Screenshot 3 (HUD badge 10, 20x20): 47 queues (16 rope-tied in groups), 18 holders.
- **`ReferenceLevelRuntimeAdapter.cs`:**
  - Upgraded `ExtractHoles` and `ExtractPlayableQueues` to compute holder capacities and specific queue destination holders via straight-forward ray tracing from ant queue heads along `forwardDirection`.
  - Accurately resolves individual holder counts when multiple holders share the same ColorID (e.g. Level 9 brown holders 9, 7, 2; Level 8 red holders 2, 3; Level 6 white/indigo holders).
- **`ReferenceColorMapper.cs` & `AntColor.cs`:**
  - Added support for ColorID 12 mapping to `AntColor.Black` (dark charcoal glossy finish `#3D424E`), eliminating fallback warnings on Level 8 and Level 10.
- **Live Play Mode Verification:**
  - Loaded Levels 6, 7, 8, 9, 10 sequentially in Play Mode.
  - Verified active database (`LevelDatabase.asset`), grid sizes, queue counts, holder counts, and holder capacities match the screenshots 100%.
  - Verified forward ant queue movement on Level 6 with 0 compile/runtime errors.

## ANT WALK VISUAL

- DOTween used only for secondary ant visual walking animation
- gameplay/root movement remains authoritative
- each moving ant receives subtle bob/tilt walking loop
- follower walk animation does not affect trail/path logic
- staggered phase between ants
- tweens stop/reset when ant stops
- walking tween is killed before swallow/destruction
- no DOTween movement of logical AntUnit root

## REFERENCE VISUAL & HUD POLISH (2026-09-25)

REFERENCE VISUAL:
Level_01.png

HUD:
matched to Color Slither Jam reference style.

HUD ORDER:
Reset -> Timer -> 3 Hearts -> Level Number

SETTINGS:
separate top-left.

ANT MOVE SPEED:
6.8f (increased from 4.2f, multiplier 1.62x)

TIMER:
starts on first valid queue movement.

ANTS:
remain current queue visuals.

SLITHER VISUALS:
NOT USED.

DETAILS:
- Background: Warm light neutral gray `#D7D5D0` matching Level_01.png.
- Hole Pit Interior: `#27323C` dark recessed navy/charcoal.
- Main HUD: Centered rounded panel `#F6F5F2` with thin gold outline `#F3A600` and soft drop shadow.
- Settings: Standalone white/gray gear button in top-left corner `(55, -95)`.
- Reset: Compact light-gray square button with circular arrow icon at left of HUD bar.
- Timer: Stopwatch icon + digital time formatted as `2:00` (bold, white text with dark outline); starts only when first valid queue moves.
- Hearts: 3 glossy rounded red hearts in a clean row without individual boxes.
- Level Badge: Compact light-gray square at far right showing level number only (`1`).
- Tutorial Text: Centered uppercase 2-line instruction `"TAP THE ANTS\nTO SEND THEM HOME"` with white fill and thick dark outline, pointing hand positioned near yellow queue head. Dismisses on first valid move.
- DOTween Walk Cadence: Synchronized with 6.8f moveSpeed (`halfStepDuration = 0.0735s`), providing crisp, snappy leg/body cadence without excessive bob height.
- Queue Movement: Visually immediate response (0ms delay), leader moves along forward axis, followers strictly replicate trail at accurate segment spacing, full tail enters hole before holder disappears.

## CORE MECHANICS: HOLE & HOLDER SYSTEM (2026-09-26)

### 1. HOLE COUNTER BADGE
- **Attachment to Nearest Side of SlotRim:** `SlotCounterBadge` is physically attached to the nearest/end side of the colored `SlotRim` (overlapping the inner rim by ~0.08 units and inset by 0.04 units from the outer perimeter). It never floats in the center of the slot, outside the rim, beside the holder, or between adjacent holes.
- **Local Geometry & Orientation Derivation:** Position is derived from holder local geometry and orientation:
  - Horizontal holders (`effectiveW > effectiveH`): Badge sits attached to the end of the slot (`badgeCenterX = endSignX * (outerW * 0.5f - badgeW * 0.5f - 0.04f * cs)`, `badgeCenterZ = 0f`).
  - Vertical holders (`effectiveH > effectiveW`): Badge sticks to the nearest end along the vertical axis (`badgeCenterX = 0f`, `badgeCenterZ = endSignZ * (outerH * 0.5f - badgeH * 0.5f - 0.04f * cs)`).
  - 1x1 or Square holders: Badge sits tucked into the inner rim boundary without blocking ant entry.
- **Hierarchy:** `HoleController` -> `HoleVisualRoot` -> `SlotRim`, `SlotInnerBevel`, `SlotPit`, `SlotCounterBadge` -> `BadgeMesh` (at local Y = `0.022f`), `BadgeCountText` (at local Y = `0.042f`).
- **BadgeMesh Sizing:** Clearly larger container (+20% to +25% larger than baseline, `badgeH = 0.42f * cs`, expands to `badgeW = 0.525f * cs` for counts >= 10).
- **Text Centering & Padding:** `BadgeCountText` is centered relative to `BadgeMesh` with only depth separation to prevent z-fighting. Counter text occupies 55–70% of usable badge height with clean, visible internal padding on all sides. Never overflows or touches badge edges for single or double digits (1, 2, 9, 10, 12).
- **Overlap Prevention:** Badge bounds remain strictly inside/attached to the holder footprint, guaranteeing 0 holder-to-holder visual or logical overlaps across all levels.
- **Content:** Displays remaining matching queues, NOT individual ant count. Does not decrement per ant or upon reservation.

### 2. ANT DESPAWN THRESHOLD (SEPARATION OF ENTRY AND DESPAWN)
- **HoleEntryPoint vs HoleDespawnPoint:** The center of the hole is NOT the despawn threshold. Movement is split into two distinct waypoints:
  1. `HoleEntryPoint`: Where the ant crosses the outer perimeter rim into the holder.
  2. `HoleDespawnPoint`: Deep inside the holder pit, past the entry threshold by at least `antVisualLength + 0.25 * cellSize` with recessed Y depth into the dark pit floor.
- **Full Visual Body Cleared:** An individual ant only despawns after its complete visual body has cleared the visible hole rim and fully entered the pit. No ant pops or disappears in the middle of the hole opening.
- **Walk Tween Safety:** When descending past the rim into the pit, the ant's walking visual tween (`AntUnit.StopWalkAnimation()`) is safely stopped before despawn without affecting gameplay movement.

### 3. HOLE COMPLETION & FINAL TAIL DISAPPEARANCE
- **Queue Completion:** An ant queue is completed ONLY after its final tail ant has fully entered and disappeared.
- **Remaining Count Decrement:** `remainingCount -= 1` occurs strictly when the entire queue (tail included) has completed swallowing.
- **Hole Disappearance:** The hole remains fully visible and active while any ant is still entering. On the final required queue, the hole disappearance tween (`Ease.InBack` shrinkage) begins ONLY after the final tail ant has fully entered and despawned.

### 4. HOLE OVERLAP PREVENTION & FOOTPRINT BOUNDING
- **No Overlapping Holes:** No two holes may overlap logically (`OccupiedCells`) or visually on ANY level.
- **Authoritative Footprint:** Visual geometry (`HoleVisualRoot`, `SlotRim`, `SlotPit`) is derived from the complete `OccupiedCells` bounding box `[minX, maxX, minY, maxY]`, correctly centering multi-cell trays.
- **Visual Safety Margin:** Visual dimensions apply a safe margin (`0.08 * cellSize`) so adjacent holder visuals have a visible gap rather than intersecting.
- **Resolution of Level 6 Yellow/Red Overlap:** The Level 6 Yellow (`(6,6), (7,6)`) and Red (`(8,6)`) logical cells did not overlap. The overlap was caused by `ApplySingleQueueHolderVisualOverride` in `HoleController.cs`, which artificially scaled 1-queue holders horizontally by 1.92x–2.65x, protruding into neighboring cells. Removing the artificial scale override and strictly bounding visuals to `OccupiedCells` resolved the overlap with a clean 0.045+ unit gap.
- **Runtime Overlap Validation:** `BoardManager.ValidateHoleOverlaps()` runs on level setup, asserting 0 logical cell intersections and 0 visual bounding box intersections.

### 5. SAME-COLOR QUEUE CONCURRENCY & CAPACITY RESERVATION
- **Multiple Same-Color Queues:** Two or more same-color queues can be clicked/moving simultaneously toward the SAME matching hole, provided that hole has remaining capacity.
- **Inbound Reservation System:** Managed via `availableCapacity = remainingCount - inboundReservations`. A queue reserves a slot on move start (`TryReserveSlot()`).
- **Counter Timing:** Reservation does NOT decrement `remainingCount` or the visual badge counter. The counter decrements only upon complete ingestion of all ants in the queue.
- **Oversubscription Prevention:** When `availableCapacity == 0`, subsequent queues cannot reserve the hole.
- **Safe Release:** Reservations and ingestion reservations are safely released on queue cancellation, destruction, restart, or level reset (`ReleaseReservedHole()`, `HoleController.ResetState()`).
- **Concurrent Ingestion Serialization:** `HoleController.RequestHoleEntry` and `ReleaseHoleEntry` ensure ant queues entering the same hole maintain distinct identities and smooth ingestion without mesh collision.
- **Max Concurrent Moving Queues:** Remains 4 total across the board.

## LEVEL FAILED UI (2026-09-27)

### 1. Scene Objects & Hierarchy
Configured under `GameUICanvas` in `Assets/Scenes/main.unity`:
```
GameUICanvas
  LosePanel (full-screen dim overlay, anchorMin (0,0), anchorMax (1,1), color: (0,0,0,0.82), raycastTarget: true)
    FailPopupRoot (centered container, anchorMin (0.5,0.5), anchorMax (0.5,0.5), size: 1080x1920)
      FailedTitleImage (anchor (0.5, 0.5), pos (0, 520), size: 780x408, Image with LevelFailed_Title sprite)
      BrokenHeartImage (anchor (0.5, 0.5), pos (0, -70), size: 680x452, Image with LevelFailed_Heart sprite)
      RestartButton (anchor (0.5, 0.5), pos (0, -470), size: 560x190, Image with LevelFailed_RestartButton sprite + Button component)
```

### 2. Display & Controller Script
- **Controller:** `Assets/Scripts/UI/GameUIManager.cs`.
- **Trigger:** Subscribes to `GameManager.OnGameStateChanged`. When `state == GameState.LevelLost`, activates `losePanel` and invokes `AnimateFailScreenEntrance()`.
- **Reset / Hide:** When `LevelManager.OnLevelLoaded` fires (upon restart or next level), `losePanel.SetActive(false)` is invoked and any running animation sequences are killed.

### 3. Restart Button Wiring & Action
- **Button:** `GameUICanvas/LosePanel/FailPopupRoot/RestartButton`.
- **Wiring:** Serialized to `GameUIManager.retryButton`.
- **Click Action:** Plays tap sound (`SoundManager.Instance.PlayTap()`), triggers a juicy tactile scale punch (`DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f)`), and on complete calls `GameManager.Instance.RestartLevel()`.
- **Flow:** `GameManager.Instance.RestartLevel()` -> `LevelManager.Instance.RestartCurrentLevel()` -> reloads active level asset, resets timer and lives (3), resets game state to `Playing`, and hides the fail overlay.

### 4. Art Assets Used
Extracted directly from the author's provided reference assets and imported into `Assets/UI/FailScreen/` as uncompressed RGBA 32-bit crisp UI sprites:
1. **Title Art:** `Assets/UI/FailScreen/LevelFailed_Title.png` — "LEVEL FAILED!" casual 3D stylized title with warm highlights and drop shadow (864x452 native).
2. **Broken Heart Art:** `Assets/UI/FailScreen/LevelFailed_Heart.png` — Large central broken red heart with jagged crack and floating fracture pieces (792x527 native).
3. **Restart Button Art:** `Assets/UI/FailScreen/LevelFailed_RestartButton.png` — Glossy red rounded-rectangular hybrid-casual button with bright top highlight, 3D bottom bevel, and centered bold "Restart" lettering (606x205 native).

### 5. Animation Behavior
Implemented via DOTween in `GameUIManager.AnimateFailScreenEntrance()`:
- **Dim Background:** Fades in quickly from alpha 0.0 to 0.82 in `0.20s` (`Ease.Linear`), dimming the underlying gameplay while keeping the board context readable.
- **Title Art:** Scales in from 0.72x to 1.0x in `0.32s` with `Ease.OutBack`.
- **Broken Heart:** Scales in from 0.68x to 1.0x in `0.35s` with `Ease.OutBack`.
- **Restart Button:** Pops in gently with a slight stagger (`0.10s` delay), scaling from 0.0x to 1.0x in `0.32s` with `Ease.OutBack`.
- **Button Tap:** Tactile punch squash `(-0.08x, -0.08y)` for `0.12s` with `SoundManager.Instance.PlayTap()` before level reload.

### 6. Input Blocking
- **Raycast Interception:** `LosePanel` features a full-screen `Image` component (`failDimBackground`) with `raycastTarget = true` spanning across the entire canvas (`(0,0)` to `(1,1)`). Any mouse or touch inputs are intercepted by the UI canvas before reaching physics or ground planes.
- **State Gating:** `PlayerInputHandler.cs` explicitly verifies `GameManager.Instance.CurrentState == GameState.Playing` before processing taps, ant selection, or camera dragging, fully blocking interaction while the fail overlay is active.
