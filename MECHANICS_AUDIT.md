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
- **Intra-Queue Lengthwise Ropes Removed:** Lengthwise ropes connecting successive ants within a single queue remain strictly deactivated.
- **Invariants Verified:**
  1. Linked leader ropes are visible at rest and dynamically stretch between leaders during movement.
  2. Ropes cleanly scale down and disappear as leaders submerge into the hole pit ($y < -0.10f$).
  3. All-or-none atomic launch preserved across groups of 2, 5, 6, and 7 queues.

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

---

## 3. Categorized Verification Status

### Implemented & Verified by Automated Tests
- Moving queue overlap and non-collision rules (9/9 Simultaneous Tests PASS).
- Level 14 `Queue_Record_42_10` legal move execution (5/5 Moving Queue & Level 14 Tests PASS).
- Settings pause ownership, modal animations, and PlayerPrefs persistence (4/4 UI Verification Tests PASS).
- Swept toroidal pipe bends and source-derived multi-use badge counters.
- EditMode integration test suite (23/23 PASS in `SlitherAnts.Tests.Editor.dll`).

### Verified Visual Assets (Game View 1080x1920)
- `Settings_Modal.png`: Purple rounded panel, yellow outlined header, green switches, dark purple Privacy Policy button, visible ant board behind modal.
- `LevelCompleted_NextFeatureProgress.png`: Full two-line "LEVEL COMPLETED!" title, upcoming feature glow, progress bar, NEXT button.
- `Level7_LinkedRopes.png`: Linked leader-to-leader ropes visible; lengthwise queue ropes removed.
- `Level22_PipeBends.png`: Glossy cyan toroidal pipe elbows with smooth corner shading and '2'/'1' badge counters.
