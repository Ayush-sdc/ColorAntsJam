# Slither Ants Jam — Antigravity Project Context

Last Updated:
2026-09-24 20:15 (UTC+05:30)

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
- **Current Queue System:** `AntQueueController.cs` and `AntUnit.cs`. Slithers segment-by-segment along BFS path (`AntPathfinder.cs`). Caches `DestinationHole` on movement start and enters hole sequentially.
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

### Movement Rule
Ant queues move from the head and may not immediately reverse through their own neck/body.

### Queue Concurrency Rule (Max 2 Concurrent Moving Queues)

- **Concurrency Limit:** Maximum 2 ant queues may move simultaneously (`MAX_CONCURRENT_MOVING_QUEUES = 2`).
- **Parallel Movement:** When Queue A starts moving, the player can immediately tap Queue B to start moving concurrently. Both movement coroutines run in parallel.
- **Individual Queue Protection:** Each queue maintains independent `IsMoving` protection (`queue.IsMoving`). Tapping a queue that is already moving is ignored (no duplicate coroutines, no penalty).
- **Third Queue Input & Heart Safety:** When 2 queues are moving (`movingQueues.Count >= 2`), tapping a third queue does NOT start movement and does NOT deduct a heart. It triggers subtle blocked feedback (shake) without penalty (`PlayerLives` unchanged).
- **Concurrency Tracking:** Managed authoritatively in `BoardManager.movingQueues` (`HashSet<AntQueueController>`). Added on `TryStartMove()`, removed on `OnQueueCompleted()` or `OnQueueMoveStoppedEarly()`. Cleared during `Awake()`, `ClearBoard()`, and `RestartLevel()`.
- **Destination Capacity & Inbound Reservations:**
  - Simultaneous queues may target different holders OR the same holder if that holder has enough available capacity.
  - Available capacity is calculated as: `AvailableCapacity = remainingCount - inboundReservations`.
  - When movement begins, a slot is reserved via `HoleController.TryReserveSlot()`.
  - Inbound reservations prevent holder oversubscription (`IsFull => isCompleted || AvailableCapacity <= 0`).
  - If a holder's available capacity is 0, subsequent matching queues cannot reserve or pathfind to it.
  - If movement is cancelled or interrupted early, `ReleaseReservation()` frees the slot without decrementing `remainingCount`.
- **Holder Counter Timing:**
  - Holder counter does NOT decrement when movement starts or when a reservation is placed.
  - Counter decrements ONLY when an entire queue has completed entering the holder (`remainingCount--` in `OnQueueSwallowed()`).
- **Holder Removal Safety:**
  - A holder may only be removed when `remainingCount <= 0 && inboundReservations <= 0`.
  - Ensures multi-capacity holders remain active until ALL in-flight reserved queues have entered.
- **Path Conflict Handling:**
  - `AntPathfinder` incorporates `otherQueue.GetReservedPathCells()` into blocked cells for any moving queue.
  - Reserved path cells cover unreached path cells ahead of the moving queue's head (excluding final hole entrance).
  - As a queue advances, vacated cells are released immediately (`remainingPath.RemoveAt(0)`), allowing other queues to cross previously vacated tiles safely.
  - Conflicting paths are prevented before movement begins; non-conflicting paths move simultaneously with zero interference.
- **Terminal State Safety:**
  - Level timeout (`00:00`) or 0 lives transitions `GameState` to `LevelLost`.
  - `TryStartMove()` requires `GameManager.Instance.State == GameState.Playing`, preventing new moves in terminal states.
  - Late queue completion during loss cannot trigger `LevelWon`.
  - Deadlock detection in `GameManager.CheckGameWinLossConditions()` is bypassed while `BoardManager.Instance.MovingQueuesCount > 0`.
- **Camera Drag Independence:** Camera dragging does not consume a movement slot and operates independently.

### Invalid Move Rule
An invalid player move or gameplay collision deducts exactly one life per move attempt.
Tapping a third queue while 2 queues are moving is NOT an invalid move and does NOT deduct a life.

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

## 5. Current Visual Systems (Ants & Holders)

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
- **Level Failed Popup (`losePanel`):** Activated when `GameState == GameState.LevelLost` (deadlock detected or timer expiry). Scales up with `Ease.OutBack`. Contains `retryButton` calling `GameManager.Instance.RestartLevel()`.
- **Restart Behavior:** Top-bar restart button or in-modal restart button directly reloads active level via `GameManager.Instance.RestartLevel()`.
- **Settings Modal (`settingsModal`):** Animated card overlay (`settingsCard`) with sound/music toggles and restart shortcut.
- **Gameplay Blocking Overlays:** Modals and win/lose panels block further board raycasts/inputs while open.

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




