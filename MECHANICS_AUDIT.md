# Mechanics and UI Audit

**Project:** Color Ants Jam  
**Document:** Authoritative Audit of Gameplay Mechanics, Implementation Completeness & UI Systems  
**Engine:** Unity `6000.3.23f1` (URP `17.3.0`, Target: Android)  
**Date:** September 30, 2026  

---

## 1. Mechanics Completeness & Verification Status Matrix

| Mechanic | Controlling Scripts | Governing Data Fields | Status | Automated Test Coverage | Manual PlayMode Verification |
| :--- | :--- | :--- | :---: | :---: | :---: |
| **Normal Ant Queues** | [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [HoleController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs) | `Cells`, `ColorID`, `HolderType = 1` | **VERIFIED** | Free-floor pathing, vacated trailing segment clearance, swallow VFX, multi-cell release | Verified on Levels 1, 2, 4, 14 |
| **Moving Queue Overlap** | [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs) | `IsMoving`, `InboundReservations` | **VERIFIED** | Shared/crossing paths permitted without rejection, zero lateral shifts, stationary obstacle preservation | Verified across 9 simultaneous tests |
| **Linked / Roped Ants** | [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs) | `IsConnected`, `ConnectedIndex` | **VERIFIED** | Real groups of 2, 5, 6, 7; atomic block; leader-to-leader ropes restored; lengthwise body ropes removed | Visual rope stretch & leader detachment on Level 7 |
| **Locked Ants** | [AntLockBadge.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntLockBadge.cs), [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `IsLocked`, `LockAmount` | **VERIFIED** | Blocked while locked; decrements once per rescue; pair decrements by 2; zero-unlock & restart restoration | Padlock 3D badge and TMP counter |
| **Stacked / Layered Ants** | [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `IsSecondColor`, `SecondColor` | **VERIFIED** | Blocked attempt does not reveal; accepted move reveals lower layer; lower blocked by stationary obstacles; independent rescues | Dominant vs dormant layer initialization |
| **Pipes / Swept Bends** | [PipeController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/PipeController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `PipeCount`, `HolderType = 2` | **VERIFIED** | Swept toroidal circular elbows, centerline ant tracking, concurrent reservations up to uses, source-derived count | Procedural 3D tube mesh, badge counter |
| **Hole Capacity Allocation** | [ReferenceLevelRuntimeAdapter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs), [HoleController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs) | `ExplicitCapacity`, `ColorID` | **VERIFIED** | Color-grouped allocation satisfying all queues (including stacked) across all 45 levels | Hole capacity badge countdown & completion |
| **Settings UI Redesign** | [GameUIManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs), [SoundManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/SoundManager.cs), [VibrationManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/VibrationManager.cs) | `PlayerPrefs` (Sound/Vib) | **VERIFIED** | Pause ownership restoration (resumes only if no other modal owns pause), non-interfering mute toggle, Android back | Rounded purple card, green switches, dark purple privacy button |

---

## 2. In-Depth Mechanics Verification Details

### A. Confirmed Moving-Queue Collision Rules (Supersedes Earlier Crossing Restrictions)
- **Superseded Previous Rule:** Earlier interim guidelines rejected intersecting routes (`GroupRoutesCross`). This has been **explicitly superseded by confirmed gameplay**:
  1. Multiple ant queues can move simultaneously into the same matching hole as long as sufficient capacity remains.
  2. Moving queues may overlap, cross, or follow the exact same path. Once queues start moving, they **do not collide with each other**.
  3. Overlapping movement does not block queues, cancel rescues, or deduct player lives.
  4. Leaders continue straight along natural grid trajectories into holes without artificial lateral lane offsets.
  5. Stationary queues remain legitimate obstacles. Once any queue begins moving, its old footprint is vacated immediately and does not remain registered as a stationary obstacle.

### B. Level 14: Queue_Record_42_10 Analysis & Fix
- **Problem Statement:** In Level 14, green queue `Queue_Record_42_10` (head at `(25, 19)`, facing down towards hole at `(24, 19)`) was blocked from moving.
- **Root Cause:** The movement validator treated the queue's own static initial body cells (`(26, 19)`, `(27, 19)`, etc.) as permanent obstacle cells throughout the forward trajectory, instead of accounting for segments vacated as the queue advances along its path.
- **Resolution:** In [AntQueueController.cs:OccupiesAtStep](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), self-occupancy simulation dynamically computes which body segments remain occupied after `stepIndex` forward steps. As the queue moves into `(24, 19)`, trailing segments vacate cells `(27, 19)` and `(26, 19)`.
- **Regression Guard:** Regression test `Test_Level14_Queue_Record_42_10_LegalMove_CompletesSuccessfully` verifies this queue launches, traverses, and completes into Hole `(24, 19)` legally through the full movement pipeline.

### C. Locked Ants
- **Invariants Verified:**
  1. Queues with `IsLocked == true` and `LockAmount > 0` are completely rejected from movement (`TryStartMove` returns `false`).
  2. Active lock counters decrement by exactly 1 per completed rescue (`BoardManager.OnQueueCompleted`).
  3. A rescued linked pair (2 queues) decrements active lock counters by 2.
  4. Blocked taps, moves still in progress, canceled/stopped-early moves, and duplicate completion callbacks DO NOT decrement lock counters.
  5. Queues automatically unlock (`IsLocked = false`) when `LockAmount` reaches 0. Original lock amounts restore upon level restart / re-initialization.

### D. Linked / Roped Ants & Leader Ropes
- **Leader Connector Rope Restored:** The short rope connecting leaders of different linked queues from Level 7 onward has been restored ([LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs)).
- **Unwanted Permanent Ropes Purged:** Found 119 baked root GameObjects named `LinkedRope_Record_*` in `Assets/Scenes/main.unity` caused by previous `ropeTransform.SetParent(null, true);`. Purged all baked scene instances and corrected parenting to `LinkedAntsRopeVisual` transform under `boardParent`.
- **Intra-Queue Lengthwise Ropes Removed:** Lengthwise ropes connecting successive ants within a single queue remain strictly deactivated.
- **Invariants Verified:**
  1. Linked leader ropes are visible at rest and dynamically stretch between leaders during movement.
  2. Ropes dismiss and destroy cleanly when leaders enter holes ($y < -0.05$ or `IsLeaderInHole`).
  3. All-or-none atomic launch preserved across groups of 2, 5, 6, and 7 queues.
  4. Level transitions, restarts, and group removals completely sweep and destroy all stray connectors.

### E. Pipes & Swept Toroidal Elbow Geometry
- **Toroidal Elbow Swept Meshes:** Procedural pipe meshes swept around 90-degree corners with circular toroidal ring slices, eliminating pinch, stretch, gaps, and intersecting end caps. Material: Glossy Cyan with smooth normals.
- **Centerline Waypoint Traversal:** Ant leaders and follower trails strictly follow circular arc waypoints through elbows ([PipeController.cs:GetCenterlineWaypoints](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/PipeController.cs)).
- **Capacity & Reservation Accounting:**
  1. Raw JSON `PipeCount` audit: Out of 49 total pipes across 45 levels, 47 pipes have raw `PipeCount = 0` (unspecified in JSON), while Level 45 explicitly specifies `PipeCount = 2`.
  2. Runtime derivation: In [ReferenceLevelRuntimeAdapter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs), pipes with raw count <= 0 dynamically derive capacity by counting queues whose head trajectory points into the entrance (`Mathf.Max(1, pointingQueues)`). Raw counts > 0 (e.g. Level 45) are strictly preserved.
  3. Concurrent reservations: Multiple moving queues can traverse a pipe concurrently up to available uses (`remainingUses - activeReservations > 0`). Exhaustion only destroys the pipe after all active traversals complete (`remainingUses <= 0 && activeReservations <= 0`).

### F. Settings Modal & Pause Ownership
- **Pause Ownership Invariant:** Closing the Settings modal inspects `!IsAnyModalActive(excludeSettings: true)` before invoking `GameManager.Instance.ResumeGame()`. If a tutorial or hard level intro is active, gameplay stays paused.
- **State Transition Fix:** Muting sounds only modifies audio settings; it does not pause gameplay or freeze touch input.
- **Android Back & Outside Tap:** Pressing Android Back (`KeyCode.Escape`) or tapping the dimmed backdrop closes the modal without passing touch clicks to underlying ants.
- **PlayerPrefs Persistence:** Audio mute (`SoundMuted`, `MusicMuted`) and vibration (`VibrationEnabled`) settings persist across sessions.

### G. Simultaneous Hole Entry Globally
- **Global Implementation:** Enabled across shared systems in `HoleController.cs` and `BoardManager.cs`, eliminating single active queue serialization, moving-queue collision restrictions, and duplicate completion leaks.
- **Verified on Level 7 `Hole_Pink_6_6`:** Multiple moving queues (tested with 2 and 4 concurrent queues) share the exact same corridor, overlap, and enter `Hole_Pink_6_6` simultaneously without collision or life loss.
- **Atomic Capacity Accounting:** Capacity is reserved atomically per-queue via `TryReserveSlot(queue)`. When a queue finishes entering, `OnQueueSwallowed(queue)` transitions reserved capacity to consumed capacity (`remainingCount = Mathf.Max(0, remainingCount - 1)`), preventing overbooking and capacity leaks.
- **Duplicate Completion Guard:** `completedQueues` hash set guarantees each queue triggers completion logic exactly once.

### H. Hole & Badge Scale Stability
- **Root Cause:** In earlier versions, `countBadgeObj.transform.DOPunchScale(...)` was invoked per ant unit swallowed. When multiple queues (16-24 ants) entered rapidly, compounding relative punch tweens permanently expanded the badge and outer slot rim.
- **Permanent Fix:** All scale tweens and relative punch effects were removed from `PlaySwallowEffect()` and `PlayCompletedEffect()`.
- **Scale Invariant:** `slotRoot.localScale` and `countBadgeObj.transform.localScale` remain locked to `Vector3.one` throughout rapid entries on Level 8 (`Hole_Orange_12_18`) and Level 11 (`Hole_Pink_3_11`).
- **2-Digit Counter Sizing:** Badge mesh dynamically sizes when `Mathf.Max(initialCapacity, remainingCount) >= 10` to fit two-digit numbers with clear padding without obstructing entry lanes.

### I. Camera Framing & Viewport Below HUD
- **Baseline Z World Offset:** Scene component `BoardCameraController` in `main.unity` configured with authoritative `baselineZOffset = 2.63f`, framing the board below the top HUD on standard levels.
- **Dynamic Bounds Fitting:** `CalculateVisibleFootprint` calculates `targetOrtho = Mathf.Max(requiredOrthoW, requiredOrthoH)` ensuring all queues, stadium holders, pipes, and ropes fit within usable viewport $Y \in [0.04, 0.83]$ and $X \in [0.04, 0.96]$.
- **Cinemachine Sync:** Synchronizes `gameplayCamera.Lens.OrthographicSize = targetOrtho` whenever Cinemachine is active.
- **Editor Framing Control:** Provided "Fit Board Below HUD" context menu and editor window button in `SlitherAntsTestingWindow.cs`.

### J. Last Accepted Move Finishing Grace Period
- **Rule:** If the final required move or linked group is launched before the level timer reaches 0:00, the game does not trigger immediate timeout defeat.
- **Behavior:** Timer clamps at 0:00 (`isFinishingAfterTimeout = true`), allowing in-flight rescues to finish animations and hole entry.
- **Outcome:** If all remaining queues enter holes, game triggers `TriggerWin()`. If any stationary or dormant queues remain on the board once moving queues halt, game triggers `TriggerGameOver("Time ran out!")`.
- **Single-Frame Mutex:** `TriggerWin()` and `TriggerGameOver()` enforce `if (currentState != GameState.Playing) return;`, ensuring win and loss can never trigger in the same frame.

### K. Unlimited-Time Testing Mode
- **Implementation:** Added `IsTimerFrozen` property in `GameManager.cs` and toggle in `SlitherAntsTestingWindow.cs`.
- **Mechanic:** Freezes countdown without using `Time.timeScale = 0`. Ant slithering, movement paths, and tap input operate at full speed.
- **HUD Indicator:** `GameUIManager.cs` displays cyan `[TEST]` label next to the level timer when frozen.

### L. Locked-Ants Tutorial Overlay
- **Trigger Condition:** Evaluated on mechanic presence (`queue.IsLocked || queue.LockAmount > 0`) on board setup rather than hard-coded level numbers.
- **Visuals:** Ant illustration with golden padlock, chain, radial sparkles, blue description panel, and green OK button.
- **Preview:** Accessible via `SlitherAntsTestingWindow.cs` "Preview Locked Ants Tutorial" button.

### M. Authoritative Reference Levels 23, 25, 29
- **Durations:** Level 23 = 180s, Level 25 = 150s, Level 29 = 120s.
- **Difficulty:** Difficulty 2 (Hard) across all three levels.
- **Level 23 Pipe Count:** Authoritative 13 pipes confirmed and re-imported into `Level_023.json` and `Level_023.asset`.
- **Visual Parity:** Sanitized scene board ensures clean butterfly layout (Level 23) and heart layout (Level 29) without ghost objects.

### N. Closer Camera & Dynamic Pan Freedom
- **Superseded Previous Rule:** Previous behavior forced full-board bounds fitting on every level load. Superseded by a comfortable closer default view (`defaultZoom = 10.0f`).
- **Dynamic Pan Limits:** Calculated mathematically from board bounds, pitch ($88^\circ$), height ($13.04$), usable viewport ($Y \in [0.04, 0.83]$), and current zoom level, plus configurable margins (`panMarginX = 2.0f`, `panMarginZ = 2.5f`). Guarantees every queue, pipe, and hole in all 4 corners is reachable without upper pan restriction.
- **Mobile Pinch & Editor Mouse-Wheel Zoom:** Supports smooth zoom adjustments between `minZoom = 5.5f` and `maxZoom = 26.0f`, dynamically expanding/contracting pan limits in real time.
- **Modal Input Protection:** Gestures and taps are blocked while modals (settings, tutorials, level complete) are open.
- **View Preservation:** Camera position and zoom level are preserved across moves, rescues, and modal closures (does not snap back or zoom out).

---

## 3. Categorized Verification Status

### Implemented & Verified by Automated Tests
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
- Win/loss single-frame mutual exclusion and finishing grace period (PASS).
- Moving queue overlap and non-collision rules (9/9 Simultaneous Tests PASS).
- Level 14 `Queue_Record_42_10` legal move execution (5/5 Moving Queue & Level 14 Tests PASS).
- Settings pause ownership, modal animations, and PlayerPrefs persistence (4/4 UI Verification Tests PASS).
- Swept toroidal pipe bends and source-derived multi-use badge counters.
- EditMode integration test suite (23/23 PASS in `SlitherAnts.Tests.Editor.dll`).
- Camera Overhaul and Rope Visual Lifecycle verification suite (7/7 PASS in `FeatureUIPlayModeTests.cs`).

### Verified Visual Assets (Game View 1080x1920 in Assets/TestEvidence/Screenshots/)
- `Evidence_NormalLevel1_ZeroRopes.png`: Normal Level 1 showing 0 ropes and comfortable closer view.
- `Evidence_Level7_LinkedConnectors.png`: Level 7 showing exactly 14 leader-to-leader connectors for 12 linked groups.
- `Evidence_Level7_RopeDismissedOnHoleEntry.png`: Rope visual dismissed upon hole entry without lingering objects.
- `Evidence_Level6_Transition_ZeroRopes.png`: Clean transition from linked Level 7 to normal Level 6 with 0 ropes.
- `Evidence_Level23_CloserView_Centered.png`: Level 23 butterfly board in comfortable closer view.
- `Evidence_Level23_CloserView_PanBottomLeft.png`: Level 23 panned to bottom-left corner with full visibility.
- `Evidence_Level23_CloserView_PanTopRight.png`: Level 23 panned to top-right corner with full visibility.
- `Evidence_Level23_ZoomedIn.png`: Level 23 zoomed in (`zoom = 7.0f`) showing expanded pan limits.
- `Evidence_Level23_ZoomedOut.png`: Level 23 zoomed out (`zoom = 17.0f`) showing contracted pan limits.
- `Evidence_SettingsModal_Open_GesturesBlocked.png`: Modal open with gestures and taps blocked.
- `Evidence_SettingsModal_Closed_ViewPreserved.png`: Modal closed with camera position and zoom preserved.
- `Tutorial_LockedAnts.png`: Locked-Ants tutorial overlay modal.
- `Settings_Modal.png`: Purple rounded panel, yellow header, green switches.
- `LevelCompleted_NextFeatureProgress.png`: Full "LEVEL COMPLETED!" title with progress bar.
