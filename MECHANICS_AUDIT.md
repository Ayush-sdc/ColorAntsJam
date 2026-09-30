# Mechanics and UI Audit

**Project:** Color Ants Jam  
**Document:** Authoritative Audit of Gameplay Mechanics, Implementation Completeness & UI Systems  
**Engine:** Unity `6000.3.23f1` (URP `17.3.0`, Target: Android)  
**Date:** September 30, 2026  

---

## 1. Mechanics Completeness & Verification Status Matrix

| Mechanic | Controlling Scripts | Governing Data Fields | Status | Automated Test Coverage | Manual PlayMode Verification |
| :--- | :--- | :--- | :---: | :---: | :---: |
| **Normal Ant Queues** | [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [HoleController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs) | `Cells`, `ColorID`, `HolderType = 1` | **VERIFIED** | Free-floor pathing, swallow VFX, multi-cell release | Verified on Levels 1, 2, 4 |
| **Linked / Roped Ants** | [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs) | `IsConnected`, `ConnectedIndex` | **VERIFIED** | Real groups of 2, 5, 6, 7; atomic block; unequal lengths | Visual rope stretch & leader detachment |
| **Locked Ants** | [AntLockBadge.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntLockBadge.cs), [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `IsLocked`, `LockAmount` | **VERIFIED** | Blocked while locked; decrements once per rescue; pair decrements by 2; zero-unlock & restart restoration | Padlock 3D badge and TMP counter |
| **Stacked / Layered Ants** | [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `IsSecondColor`, `SecondColor` | **VERIFIED** | Blocked attempt does not reveal; accepted move reveals lower layer; lower blocked by departing upper; independent rescues | Dominant vs dormant layer initialization |
| **Pipes / Direction Gating** | [PipeController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/PipeController.cs), [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) | `PipeCount`, `HolderType = 2` | **VERIFIED** | Straight & bent trajectories; entrance/direction gating; concurrent reservation rejection; exhaustion | Procedural 3D tube mesh, badge counter |
| **Hole Capacity Allocation** | [ReferenceLevelRuntimeAdapter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs), [HoleController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs) | `ExplicitCapacity`, `ColorID` | **VERIFIED** | Color-grouped allocation satisfying all queues (including stacked) across all 45 levels | Hole capacity badge countdown & completion |
| **Hard Level Overlay** | [GameUIManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs), [Level.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/Level.cs) | `LevelDifficulty`, `ShowHardIntro` | **VERIFIED** | Normal levels (`LevelDifficulty = 0`) suppress overlay; Hard levels (`LevelDifficulty >= 2`) display overlay | Overlay card UI and start button |

---

## 2. In-Depth Mechanics Verification Details

### A. Locked Ants
- **Invariants Verified:**
  1. Queues with `IsLocked == true` and `LockAmount > 0` are completely rejected from movement (`TryStartMove` returns `false`).
  2. Active lock counters decrement by exactly 1 per completed rescue (`BoardManager.OnQueueCompleted`).
  3. A rescued linked pair (2 queues) decrements active lock counters by 2.
  4. Blocked taps, moves still in progress, canceled/stopped-early moves, and duplicate completion callbacks DO NOT decrement lock counters.
  5. Queues automatically unlock (`IsLocked = false`) when `LockAmount` reaches 0. Original lock amounts restore upon level restart / re-initialization.
- **Visuals:** [AntLockBadge.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntLockBadge.cs) procedural golden padlock mesh with shackle, body, and world-space TextMeshPro counter positioned above the ant leader.

### B. Linked / Roped Ants
- **Invariants Verified:**
  1. Tested and verified on actual groups of sizes 2, 5, 6, and 7 queues.
  2. Upfront atomic validation: all routes and hole capacities for all members must be valid before any member begins moving.
  3. If any member is blocked, the entire group stays stationary with 0 route cells reserved and 0 hole reservations consumed.
  4. Simultaneous movement across all members, including unequal queue lengths (e.g. 2 vs 4 units) and unequal route lengths.
  5. Clean teardown and interruption cleanup via `BoardManager.ClearBoard()`.
- **Visuals:** [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs) connects leaders with `GoldenRope_2Cells.prefab`, scaling down smoothly as leaders enter the hole pit.

### C. Stacked / Layered Ants
- **Invariants Verified:**
  1. A blocked upper queue attempt does NOT reveal the lower layer; lower layer remains dormant and inactive.
  2. An accepted upper queue move reveals the lower layer at its exact original footprint.
  3. The revealed lower layer cannot move through the departing upper layer while the upper layer occupies the corridor or reserved route cells.
  4. Each layer is rescued independently into its own matching color hole, consuming capacity and updating active locks.
- **Level Interaction:** Verified in Level 15 (4 stacked pairs), Level 32 (stacked + locked + pipes), and Level 37 (stacked + locked).

### D. Pipes (Direction Redirection)
- **Invariants Verified:**
  1. Traversal verified through straight pipes and bent (L-shaped) pipes; ant units and followers enter entrance, follow interior waypoints, and emerge at exit heading in `ExitDirection`.
  2. Directional gating: entry permitted only at `EntranceCell` in `EntryDirection`. Side entry and backward entry are strictly rejected.
  3. Concurrent reservation gating: when a pipe has 1 use remaining, a linked group cannot route 2 queues through it simultaneously.
  4. Usage consumption decrements `RemainingUses`; exhaustion at 0 destroys/deactivates the pipe.
- **Reference Assumptions Explicitly Documented:**
  - `PipeCount` in reference JSON is assumed to represent remaining usage count.
  - Entrance is assumed to be permitted only at `Cells[0]` in direction `Cells[1] - Cells[0]`.

### E. Hole Capacity Allocation
- **Invariants Verified:**
  1. Color-grouped allocation in `ReferenceLevelRuntimeAdapter.cs`: total capacity for each color satisfies total expected queues (including stacked second colors) across all 45 reference levels (0 deficits).
  2. In multi-hole scenarios, initial forward ray hits determine proportional allocation, with remaining deficits distributed across non-explicit holes.
  3. Mismatches in explicit capacity are reported via warning rather than silently altering level configuration.

---

## 3. Categorized Verification Status

### Implemented
- All core gameplay mechanics: normal ant queues, linked ant groups, locked ant queues, stacked ant layers, directional pipes, procedural hole meshes, and hard-level metadata overlays.

### Verified by Automated Tests
- 23/23 tests passing in `SlitherAnts.Tests.Editor.dll` (NUnit / Unity Test Framework).
- 45/45 levels passing structural and capacity sufficiency checks in `validate_all_levels.py`.
- JSON round-trip serialization preserving all mechanics fields and ordered cell sequences.

### Verified Manually (PlayMode in Editor)
- Levels 1, 2, and 4 tested with interactive click/tap, camera framing, hole animations, win sequence, and audio effects.

### Assumed Reference Behavior
- Pipe usage count interpretation from `PipeCount`.
- Pipe single-entrance gating from `Cells[0]`.
- Single rescue = 1 lock decrement; linked pair rescue = 2 lock decrements.

### Unverified
- Physical Android device performance (FPS, GPU fill, battery, thermals).
- Full end-to-end win solvability for Levels 5–45 (verified structurally and capacity-sufficient, but end-to-end playthrough remains unverified).
