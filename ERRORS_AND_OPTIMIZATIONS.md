# Errors, Performance and Optimizations Audit

**Project:** Color Ants Jam  
**Document:** Deep Technical Error Analysis, Profiling Audit & Mobile Optimization Plan  
**Target Platform:** Unity Android (Universal Render Pipeline)  
**Hardware Profiling Environment:** Windows 11 (10.0.26200), Lenovo 83DV, NVIDIA GeForce RTX 4060 Laptop GPU, Unity `6000.3.23f1` Editor  
**Mobile Device Status:** **Android physical hardware performance is UNVERIFIED** (no physical Android device connected). Unsupported claims of "60 FPS maintained" and "zero memory leaks" have been removed.

---

## 1. Measured Profiling Evidence: Level 45 & Restart Cycles

Level 45 represents the largest stress scenario in the game, comprising:
- **150 Ant Queues**
- **22 Multi-Cell Stadium Holes**
- **4 Pipes**
- **35 Linked Queues**
- **15×13 Grid Dimensions**

### Exact Profiler Measurements

| Metric / Event | Measured Value | Profiler Notes |
| :--- | :---: | :--- |
| **Initial Memory Baseline** | `1175.35 MB` | Unity Editor heap baseline prior to board instantiation |
| **Level 45 Initial Load Duration** | **`832.2 ms`** | Complete board setup (`SetupBoard` in test mode) |
| **Level 45 Load Heap Allocation** | **`74,184.00 KB` (~72.4 MB)** | Heap delta for 150 queues, body meshes, hole procedural meshes |
| **Restart Cycle 1 Duration** | `1208.3 ms` | Full `ClearBoard()` teardown + `SetupBoard()` rebuild (Heap: 1340.50 MB) |
| **Restart Cycle 2 Duration** | `1188.9 ms` | Full teardown + rebuild (Heap: 1235.99 MB) |
| **Restart Cycle 3 Duration** | `932.6 ms` | Full teardown + rebuild (Heap: 1366.41 MB) |
| **Restart Cycle 4 Duration** | `1037.6 ms` | Full teardown + rebuild (Heap: 1298.28 MB) |
| **Restart Cycle 5 Duration** | `1100.0 ms` | Full teardown + rebuild (Heap: 1247.98 MB) |
| **Average Restart Time (5 cycles)** | **`1,093.5 ms` (~1.09 s)** | Stable rebuild cycle across consecutive restarts |
| **Final Memory After GC** | `1175.47 MB` | Forced GC post-teardown |
| **Net Heap Delta Post-GC** | **`+124.00 KB`** | Minimal residual memory retention across 5 full teardown/setup cycles |

> [!NOTE]
> **Mobile Hardware Caveat**:
> While Editor memory retention is low (+124 KB net delta across 5 cycles), Android performance is unverified on physical hardware. On low-end ARM devices (e.g. Mali-G52 or Adreno 610), instantiating 150 GameObjects within a single frame may induce a perceptible frame hitch (~300–600 ms). An object pool for ant units and hole segments remains recommended for production Android release.

---

## 2. Summary of Performance & Stability Findings

| ID | Issue Description | Location | Severity | Resolution Status |
| :--- | :--- | :--- | :--- | :--- |
| **ERR-01** | Massive Scene File Bloat (66.7 MB) with 546 Baked Particle Systems | `Assets/Scenes/main.unity` | **CRITICAL** | **RESOLVED** (Sanitized to 5.8 MB; purged 546 duplicate clones in commit `906d14d`) |
| **ERR-02** | Runtime Material Allocation in Garden Background | `GardenEnvironmentPalette.cs` | **HIGH** | **RESOLVED** (Implemented material caching dictionary by color) |
| **ERR-03** | DOTween Sequence Overhead on Ant Wobble | `AntUnit.cs` | **HIGH** | **RESOLVED** (Replaced with mathematical sine wobble in Update) |
| **ERR-04** | Destroy / Instantiate Cycle for Ant Queues & Holes | `AntQueueController.cs`, `BoardManager.cs` | **MEDIUM** | **MITIGATED** (Clean lifecycle & GC collection; pooling recommended for low-end Android) |
| **ERR-05** | Android PlayerSettings: Template Application Identifier | `ProjectSettings.asset:173` | **MEDIUM** | Pending production bundle ID assignment prior to Google Play upload |
| **ERR-06** | Expensive Raycasting for Ant Eye Gaze | `PlayerInputHandler.cs` | **LOW** | Optimized; eye gaze disabled during camera pan |
| **ERR-07** | Duplicate Queue Completion Processing | `BoardManager.cs` | **HIGH** | **RESOLVED** (`completedQueues` guard prevents duplicate lock decrements or route clearing) |
| **ERR-08** | Concurrent Pipe Over-Reservation in Linked Groups | `PipeController.cs`, `BoardManager.cs` | **HIGH** | **RESOLVED** (Active reservations track concurrent queues; reject moves exceeding remaining uses) |
| **ERR-09** | Settings Modal Pause Lockout & Input Freeze on Close | `GameUIManager.cs` | **CRITICAL** | **RESOLVED** (Corrected pause ownership evaluation; fixed timeScale resumption, Android Back handling, and PlayerPrefs audio persistence) |
| **ERR-10** | Level 14 False Self-Collision on Vacated Trailing Cells | `AntQueueController.cs` | **HIGH** | **RESOLVED** (Simulated step-by-step body vacancy in `OccupiesAtStep` so trailing body segments do not block valid advancing bent moves) |
| **ERR-11** | Distorted Pipe Elbow Bends & Normal Stretching | `PipeController.cs`, `BoardManager.cs` | **MEDIUM** | **RESOLVED** (Constructed procedural toroidal circular elbows with smooth normals and centerline arc waypoint traversal) |
| **ERR-12** | Reference JSON Pipe Count Discrepancies (All Starting at 1) | `ReferenceLevelRuntimeAdapter.cs` | **MEDIUM** | **RESOLVED** (Identified 47/49 pipes have raw JSON count 0; dynamically derive from incoming queue head alignment while preserving raw counts > 0) |
| **ERR-13** | Compounding Scale Punch Tweens Inflating Holes & Badges | `HoleController.cs` | **HIGH** | **RESOLVED** (Removed `DOPunchScale` on swallow; locked `slotRoot` and `countBadgeObj` scale to `Vector3.one`; cached stable dimensions; dynamically sized 2-digit badges) |
| **ERR-14** | Camera Baseline Z Overwrite & HUD Element Occlusion | `BoardCameraController.cs`, `main.unity` | **HIGH** | **RESOLVED** (Updated scene serialized `baselineZOffset` to 2.63; implemented dynamic orthographic bounds calculation to fit board below top HUD [$Y \in 0.04, 0.83$]) |
| **ERR-15** | Premature Level Defeat on Final In-Flight Accepted Move | `GameManager.cs` | **HIGH** | **RESOLVED** (Implemented finishing grace period clamping timer at 0:00 until in-flight rescues resolve; added win/loss frame mutex) |
| **ERR-16** | Simultaneous Hole Entry Serialization & Capacity Leaks | `HoleController.cs`, `BoardManager.cs` | **CRITICAL** | **RESOLVED** (Multi-queue entry enabled across shared systems; atomic reservation-to-swallow transition in `OnQueueSwallowed`; duplicate callback guard) |
| **ERR-17** | Reference JSON Level Inaccuracies (Levels 23, 25, 29) | `Level_023.json`, `Level_025.json`, `Level_029.json` | **MEDIUM** | **RESOLVED** (Authoritative durations [180s, 150s, 120s], Difficulty 2, and 13 pipes for Level 23 codified in JSON and re-imported to ScriptableObjects) |
| **ERR-18** | Permanent Unwanted Linked Rope Visuals Baked into Scene | `Assets/Scenes/main.unity`, `LinkedAntsRopeVisual.cs` | **CRITICAL** | **RESOLVED** (Identified and purged 119 root `LinkedRope_Record_*` GameObjects serialized into `main.unity`. Fixed parenting to `LinkedAntsRopeVisual` transform under `boardParent`. Ropes dismiss immediately when leaders enter holes [$y < -0.05$ or `IsLeaderInHole`]. ClearBoard sweeps destroy all old connectors) |
| **ERR-19** | Overly Distant Starting Camera & Restrained Panning Freedom | `BoardCameraController.cs`, `PlayerInputHandler.cs` | **HIGH** | **RESOLVED** (Replaced mandatory full-board-fit on level load with closer starting view [`defaultZoom = 10.0f`]. Derived dynamic pan limits from board bounds, zoom, and margins [`panMarginX = 2.0f`, `panMarginZ = 2.5f`] ensuring reachability of all board edges/corners. Added mouse-wheel and pinch zoom. Gestures blocked during open modals; view preserved on close) |

---

## 3. Categorized Status

### Implemented & Verified (Automated Tests)
- Permanent unwanted rope elimination: 0 ropes on normal levels (Level 1 PASS, Level 6 PASS).
- Exact leader-to-leader connectors on linked levels (Level 7 PASS, 14 connectors for 12 groups).
- Linked rope dismissal into hole entry and level transition sweeps (PASS).
- Closer camera default view (`defaultZoom = 10.0f`) on large levels 23, 25, 29 with full corner reachability (PASS).
- Dynamic zoom and dynamic pan bounds expansion/contraction (PASS).
- Modal gesture blocking and camera view preservation (PASS).
- Simultaneous hole entry on Level 7 `Hole_Pink_6_6` (PASS).
- Scale stability on Level 8 and Level 11 holes/badges (PASS).
- Unlimited-time testing mode timer freeze and HUD indicator (PASS).
- Authoritative level data for Levels 23, 25, 29 (PASS).
- Camera framing with baseline Z = 2.63 and dynamic ortho bounds fitting (PASS).
- Win/loss single-frame mutual exclusion and finishing grace period (PASS).
- `completedQueues` guard in `BoardManager.cs:1846` and `HoleController.cs:894` preventing duplicate callbacks from decrementing locks or double-releasing slots.
- `activeReservations` in `PipeController.cs` and `pendingPipeReservations` in `BoardManager.cs` allowing concurrent traversals up to available uses without premature destruction.
- Moving queue non-collision and shared/crossing path traversal (9/9 Simultaneous Tests PASS).
- Level 14 `Queue_Record_42_10` legal movement through real movement pipeline (5/5 Moving Queue & Level 14 Tests PASS).
- Color-grouped hole capacity allocation in `ReferenceLevelRuntimeAdapter.cs`.
- 23/23 EditMode integration tests passing in `SlitherAnts.Tests.Editor.dll`.
- 4/4 UI and feature verification tests passing in `FeatureUIPlayModeTests.cs`.
- 7/7 Camera Overhaul and Rope Visual Lifecycle verification tests passing in `FeatureUIPlayModeTests.cs`.

### Implemented & Verified (Manual Editor Testing)
- Settings modal redesign matching reference `Settings.png`: purple card, yellow header, white labels, green switches, dark purple privacy button.
- Title restoration in Level Completed overlay: full 713x295 "LEVEL COMPLETED!" visible without cropping.
- Clean toroidal elbow geometry on Level 22 without pinch or stretch.
- Linked leader ropes restored on Level 7 without lengthwise queue ropes.
- Locked-Ants intro tutorial overlay modal preview in testing window.
- Clean closer board view for butterfly layout (Level 23), rectangular layout (Level 25), and heart layout (Level 29) with smooth drag panning across all corners.

### Unverified on Mobile
- Physical Android device frame rates, GPU fill rates, thermal throttling, and battery drain.
