# Color Ants Jam - Complete Project Context & Technical Specification

**Game Title:** Color Ants Jam (inspired by *Color Slither Jam*)  
**Unity Engine Version:** `6000.3.23f1` (Unity 6, revision `09d2ecc7fb28`)  
**Active Render Pipeline:** Universal Render Pipeline (URP `17.3.0`)  
**Target Platform:** Unity Android (Universal Render Pipeline)  
**Main Scene:** `Assets/Scenes/main.unity` (Build Scene Index: `0`)  
**Git Repository:** `https://github.com/Ayush-sdc/ColorAntsJam.git`  
**Last Updated:** October 3, 2026  

---

## 1. Project Overview & Core Gameplay Loop

**Color Ants Jam** is a top-down 3D grid puzzle game built in Unity for Android. Players tap / click colored ant queues to route them into color-matched stadium holders around obstacles and other ant bodies.

### The Gameplay Loop
1. **Board Rebuild**: [LevelManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/LevelManager.cs) loads the selected `Level` asset on start or on level change. [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) clears previous runtime objects, resets grid dots, instantiates stadium holders, spawns ant queues, and configures the camera framing.
2. **Camera Framing & Viewport Below HUD**: [BoardCameraController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Camera/BoardCameraController.cs) maintains a standard world baseline $Z = 2.63$ for standard levels and dynamically computes board bounds and orthographic size to guarantee all board elements fit completely within the usable viewport below the top HUD ($Y \in [0.04, 0.83]$).
3. **Pathfinding & Input**: [PlayerInputHandler.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs) detects touch/mouse taps on ant units or cells. [BoardManager.cs:912](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L912) (`ValidateForwardMove`) validates that an unblocked straight-line cardinal path exists from the ant head to a matching-color holder with available capacity.
4. **Confirmed Moving-Queue Collision Rules (Supersedes Earlier Crossing Restrictions)**:
   - Multiple ant queues can move simultaneously into the same matching hole if sufficient capacity remains.
   - Moving queues may overlap, cross, or follow the exact same path. Once queues start moving, they **do not collide with each other**.
   - Overlap does not block movement, cancel a rescue, or lose a life.
   - Leaders continue straight on their natural grid trajectories without artificial lateral lane offsets.
   - Stationary queues still block routes. However, once a queue starts moving, its old footprint is vacated immediately and does not remain registered as a stationary obstacle.
5. **Simultaneous Hole Entry Globally**:
   - Multiple moving queues can enter the same matching hole simultaneously without collision, life loss, capacity leak, or serialization.
   - Capacity is reserved atomically per-queue (`TryReserveSlot`), remaining valid while queues are en route.
   - When a queue finishes entering, `OnQueueSwallowed` transitions reserved capacity to consumed capacity without duplicate completion or double-counting.
6. **Hole & SlotCounterBadge Stability**:
   - Stadium holders and counter badges do not inflate or grow when multiple queues or rapid ants enter. Scale is permanently locked to `Vector3.one` with cached stable dimensions restored on reset.
   - Counter badges dynamically size to accommodate 2-digit capacities without text clipping or lane obstruction.
7. **Slithering & Hole Entry**: Ants move segment-by-segment along the smooth route ([AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs)), recording trail history. Followers trace the recorded trail at constant segment distances.
8. **Holder Completion & Permanent Grid Release**: When a holder's counter reaches 0 and all entering ants have despawned:
   - The holder enters `IsCompleted = true`.
   - `BoardManager.RemoveCompletedHole(hole)` unregisters **ALL cells across the holder's entire multi-cell footprint (`OccupiedCells`)** from `holesMap`.
   - Grid blocking is released immediately so remaining queues can path through that space.
   - The holder shrinks cleanly (`0.20s`, `Ease.InBack`) and its GameObject is destroyed.
9. **Last Accepted Move Finishing Grace Period & Win / Loss Evaluation**:
   - If the countdown timer reaches 0:00 while the final required move (or linked group) is already moving, the timer freezes at 0:00 and gameplay enters a finishing grace state.
   - If the moving queue(s) successfully enter their matching hole(s) and resolve all remaining required queues, the player **wins**.
   - If stationary or dormant queues remain on the board after moving queues finish, the game fails normally.
   - Win and loss cannot trigger in the same frame (`currentState != GameState.Playing` guard).
   - **Testing Mode**: An unlimited-time testing toggle in `SlitherAntsTestingWindow.cs` freezes the level countdown (`GameManager.IsTimerFrozen = true`) without altering `Time.timeScale`, indicated by `[TEST]` in cyan on the HUD.

---

## 2. Level Architecture & Database Workflow

The project utilizes a hybrid ScriptableObject and JSON architecture containing 45 levels:

```text
       [ Assets/Data/ReferenceLevelJSON/Level_001.json ... Level_045.json ]
                                      │
                                      ▼
                        [ ReferenceLevelImporter.cs ]
                     (Editor Menu: Import Reference Levels)
                                      │
                                      ▼
        [ Assets/Data/ReferenceLevels/Level_001.asset ... Level_045.asset ]
                                      │
                                      ▼
                   [ Assets/Data/Levels/LevelDatabase.asset ]
                                      │
                                      ▼
                         [ LevelManager.Instance ]
                                      │
                                      ▼
                    [ ReferenceLevelRuntimeAdapter.cs ]
                                      │
             ┌────────────────────────┴────────────────────────┐
             ▼                                                 ▼
[ HoleController (Holders) ]                     [ AntQueueController (Queues) ]
 - Procedural stadium meshes                      - Ordered body coordinates
 - Exact ColorID mapping                          - Matching raw ColorID
 - Multi-cell footprint release                   - Forward-only slither movement
 - Badge display counter                          - TrailHistory follower traversal
```

### JSON Data Fields and ScriptableObject Mapping
- **`Cells`**: Ordered array of `{x, y}` grid coordinates.
- **`ColorID`**: Integer from `0` to `11` mapped via [ReferenceColorMapper.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/ReferenceColorMapper.cs):
  - `0`: Sky Blue (`#1298E6`)
  - `1`: Lime Green (`#66C30E`)
  - `2`: Yellow (`#FEBD04`)
  - `3`: Purple (`#7A3DCE`)
  - `4`: Orange (`#EC7017`)
  - `5`: Pink (`#F24FE6`)
  - `6`: Indigo (`#415ACD`)
  - `7`: Green (`#2B8428`)
  - `8`: Brown (`#A26122`)
  - `9`: White/Cream (`#ECE0D2`)
  - `10`: Red (`#E83F40`)
  - `11`: Turquoise (`#08EBC1`)
- **`HolderType`**:
  - `0`: Stadium Hole / Tray (Target).
  - `1`: Playable Ant Queue.
  - `2`: Obstacle / Blocked structure.
- **`IsConnected` & `ConnectedIndex`**: Defines linked / roped ant queues that move together as an atomic unit.
- **`IsLocked` & `LockAmount`**: Defines locked queues requiring rescue counters.
- **`IsSecondColor` & `SecondColor`**: Defines stacked / double-layered ant queues.
- **`PipeCount`**: Direction-changing pipes with limited redirect uses.

---

## 3. Detailed Mechanics Audit & Implementation Status

| Mechanic | Controlling Script | Data Fields | Status | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Normal Queues** | `AntQueueController`, `BoardManager`, `HoleController` | `Cells`, `ColorID`, `HolderType=1` | **WORKING** | Lengthwise queue rope removed; clean 3D ants, eyes, and materials preserved |
| **Linked Ants** | `BoardManager:638`, `BoardManager:1776`, `LinkedAntsRopeVisual` | `IsConnected`, `ConnectedIndex` | **WORKING** | Leader connector preserved; dismisses into hole; all-or-none launch preserved |
| **Locked Ants** | `AntLockBadge.cs`, `AntQueueController.cs` | `IsLocked`, `LockAmount` | **COMPLETE** | Padlock badge, move gating, counter decrement on rescues, unlock at 0 |
| **Stacked Ants** | `AntQueueController.cs`, `BoardManager.cs` | `IsSecondColor`, `SecondColor` | **COMPLETE** | Dominant moves first; lower layer revealed on accepted move as independent queue |
| **Pipes** | `PipeController.cs`, `BoardManager.cs` | `PipeCount` (in HolderType 2) | **COMPLETE** | 3D glossy tube mesh, directional entry gating, remaining-use badge |
| **Level Completed Overlay** | `LevelCompletedOverlay.cs`, `GameUIManager.cs` | `HighestCompletedLevel`, `FeatureProgressionConfig` | **COMPLETE** | Animated progress bar, percentage, upcoming feature icon with blue glow, NEXT button |
| **Next-Feature Progression** | `FeatureProgressionConfig.cs`, `FeatureDefinition.cs` | ScriptableObject config | **COMPLETE** | Clamped 0-100%, anti-farming guards, saved campaign progress |
| **Feature Tutorials** | `FeatureTutorialModal.cs`, `GameUIManager.cs` | `SeenTutorial_<id>` | **COMPLETE** | Reusable prefab, radial rays, 4 ant-themed 3D illustrations & icons, sequential queue; Locked-Ants overlay triggers on mechanic presence |
| **Hard Level Intro** | `HardLevelIntroOverlay.cs`, `GameUIManager.cs` | `level.ShowHardIntro` (`difficulty=="2"`) | **COMPLETE** | Red horned skull emblem, pop-in, hold, fade-out; blocks board input & pauses timer |
| **Simultaneous Hole Entry** | `HoleController.cs`, `BoardManager.cs` | `InboundReservations`, `AvailableCapacity` | **COMPLETE** | Concurrent queue entry into matching hole without collision, life loss, capacity leak, or serialization |
| **Hole & Badge Scale Stability** | `HoleController.cs` | `stableSlotRootScale`, `stableBadgeScale` | **COMPLETE** | Removed scale tweens/punches; cached Vector3.one scales; 2-digit badge sizing |
| **Camera Usable Viewport** | `BoardCameraController.cs` | `baselineZOffset=2.63`, `minUsableViewportY=0.04`, `maxUsableViewportY=0.83` | **COMPLETE** | Baseline Z=2.63 preserved, dynamic bounds fitting below HUD, Cinemachine lens sync, editor framing controls |
| **Finishing Grace Period** | `GameManager.cs` | `isFinishingAfterTimeout`, `remainingTimer` | **COMPLETE** | Clamps timer at 0:00 when final accepted moves are traveling; resolves win on clear or loss if stationary queues remain; single-frame win/loss mutex |
| **Unlimited-Time Testing** | `GameManager.cs`, `SlitherAntsTestingWindow.cs` | `IsTimerFrozen` | **COMPLETE** | EditorPref/runtime toggle, freeze countdown without timeScale=0, [TEST] HUD indicator |

---

## 4. Key Systems & Exact File Paths

| System | File Path | Line Range | Responsibilities |
| :--- | :--- | :--- | :--- |
| **Game State Machine** | `Assets/Scripts/Core/GameManager.cs` | [L23-L370](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/GameManager.cs#L23-L370) | State flow, timer, finishing grace period, lives, deadlock, win/loss triggers, timer freeze |
| **Board Manager** | `Assets/Scripts/Board/BoardManager.cs` | [L30-L2191](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L30-L2191) | Board lifecycle, `holesMap`, `ValidateForwardMove`, footprint release, moving queue walkability |
| **Ant Queue Controller** | `Assets/Scripts/Ants/AntQueueController.cs` | [L20-L851](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs#L20-L851) | Slither traversal, `trailHistory`, swallow triggers, completion |
| **Hole Controller** | `Assets/Scripts/Board/HoleController.cs` | [L5-L1077](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs#L5-L1077) | Procedural stadium mesh, badge counter, concurrent entry reservations, atomic swallow transition, stable scales |
| **Linked Rope Visual** | `Assets/Scripts/Ants/LinkedAntsRopeVisual.cs` | [L12-L232](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs#L12-L232) | GoldenRope mesh stretching between linked leader units |
| **Level Progression** | `Assets/Scripts/LevelSystem/LevelManager.cs` | [L4-L244](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/LevelManager.cs#L4-L244) | `PlayerPrefs` progression, level database loading, advance/restart |
| **UI Management** | `Assets/Scripts/UI/GameUIManager.cs` | [L5-L815](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs#L5-L815) | Top bar HUD, settings modal, fail screen sequence, tutorial modals, [TEST] badge |
| **Camera Controller** | `Assets/Scripts/Camera/BoardCameraController.cs` | [L18-L620](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Camera/BoardCameraController.cs#L18-L620) | Closer default starting view (defaultZoom=10.0), dynamic pan limits derived from usable viewport ([0.04, 0.83]), pan margins (X=2.0, Z=2.5), mouse-wheel/pinch zoom, modal blocking |
| **Testing Window** | `Assets/Scripts/Editor/SlitherAntsTestingWindow.cs` | [L1-L158](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/SlitherAntsTestingWindow.cs#L1-L158) | Timer freeze toggle, camera fit button, locked tutorial preview, quick level select |
| **Audio Synthesizer** | `Assets/Scripts/Core/SoundManager.cs` | [L3-L197](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/SoundManager.cs#L3-L197) | Procedural byte-buffer audio synthesis for all SFX |

---

## 5. Critical Errors and Android Optimization Concerns

1. **Scene File Bloat (`Assets/Scenes/main.unity`)** - **[RESOLVED]**:
   - `main.unity` was sanitized from 66.7 MB down to 5.8 MB, eliminating 546 baked runtime clones and optimizing deserialization time.
2. **Material Memory Leak in Garden Background** - **[RESOLVED]**:
   - Implemented `materialCache` dictionary in `GardenEnvironmentPalette.cs` caching materials by color and smoothness tokens.
3. **Per-Ant DOTween Sequence Pressure** - **[RESOLVED]**:
   - Replaced looping DOTween sequences in `AntUnit.cs` with direct mathematical sine oscillation in `Update()` (`Mathf.Sin`), eliminating hundreds of active tween allocations on Android.
4. **Hole & Badge Scale Inflation (ERR-13)** - **[RESOLVED]**:
   - Compounding scale punch tweens during rapid queue entry permanently grew `slotRoot` and `countBadgeObj`. Removed scale tweens, locked scale to `Vector3.one`, cached stable dimensions, and properly sized 2-digit badges.
5. **Camera Baseline Overwrite & HUD Collision (ERR-14)** - **[RESOLVED]**:
   - Scene serialized `baselineZOffset` was `-0.17`, overwriting the desired $Z = 2.63$. Updated scene asset to $2.63$ and implemented dynamic orthographic sizing below top HUD ($Y \in [0.04, 0.83]$).
6. **Premature Timeout Failure on Final Moves (ERR-15)** - **[RESOLVED]**:
   - If timer reached 0:00 while the final required move was already traveling, game falsely lost. Added finishing grace state clamping timer at 0:00 until moving queues resolve.
7. **Simultaneous Hole Entry Serialization (ERR-16)** - **[RESOLVED]**:
   - Global multi-queue entry enabled for all holes; removed single active queue serialization, moving-queue collision restrictions, and duplicate completion leaks.
8. **Reference Level JSON Deserialization / Contamination (ERR-17)** - **[RESOLVED]**:
   - Corrected authoritative durations for Levels 23 (180s), 25 (150s), 29 (120s), set difficulty 2 (Hard), and restored 13 pipes for Level 23 in authoritative JSON.
9. **Permanent Unwanted Rope Visuals (ERR-18)** - **[RESOLVED]**:
   - Found 119 baked root GameObjects named `LinkedRope_Record_*` serialized into `Assets/Scenes/main.unity` caused by earlier `ropeTransform.SetParent(null, true)`. Purged all baked ropes from scene. Changed parenting in [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs) to child of `boardParent`, added immediate dismissal into holes ($y < -0.05$ or `IsLeaderInHole`), and strengthened level transition sweeps. Normal queues never display ropes.
10. **Restricted Full-Board Camera & Missing Pan Freedom (ERR-19)** - **[RESOLVED]**:
    - Replaced mandatory full-board-fit camera starting view with a comfortable closer view (`defaultZoom = 10.0f`). Added horizontal/vertical drag-panning reaching every corner with extra configurable margins (`panMarginX = 2.0f`, `panMarginZ = 2.5f`). Integrated mobile pinch-to-zoom and editor mouse-wheel zoom (`minZoom = 5.5f`, `maxZoom = 26.0f`). Gestures blocked while modals/tutorials are open, and views do not snap back or zoom out after player actions.
11. **Android Package & SDK Settings**:
    - [ProjectSettings.asset:173](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectSettings.asset#L173) still uses template package name `com.UnityTechnologies.com.unity.template.urpblank` and Target SDK 0.
    - Must be updated to a custom bundle identifier and Target API Level 34+ before Google Play release.
