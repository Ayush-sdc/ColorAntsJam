# Color Ants Jam - Complete Project Context & Technical Specification

**Game Title:** Color Ants Jam (inspired by *Color Slither Jam*)  
**Unity Engine Version:** `6000.3.23f1` (Unity 6, revision `09d2ecc7fb28`)  
**Active Render Pipeline:** Universal Render Pipeline (URP `17.3.0`)  
**Target Platform:** Unity Android (Universal Render Pipeline)  
**Main Scene:** `Assets/Scenes/main.unity` (Build Scene Index: `0`)  
**Git Repository:** `https://github.com/Ayush-sdc/ColorAntsJam.git`  
**Last Updated:** September 30, 2026  

---

## 1. Project Overview & Core Gameplay Loop

**Color Ants Jam** is a top-down 3D grid puzzle game built in Unity for Android. Players tap / click colored ant queues to route them into color-matched stadium holders around obstacles and other ant bodies.

### The Gameplay Loop
1. **Board Rebuild**: [LevelManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/LevelManager.cs) loads the selected `Level` asset on start or on level change. [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) clears previous runtime objects, resets grid dots, instantiates stadium holders, spawns ant queues, and configures the camera framing.
2. **Pathfinding & Input**: [PlayerInputHandler.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs) detects touch/mouse taps on ant units or cells. [BoardManager.cs:912](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L912) (`ValidateForwardMove`) validates that an unblocked straight-line cardinal path exists from the ant head to a matching-color holder with available capacity.
3. **Slithering & Hole Entry**: Ants move segment-by-segment along the smooth route ([AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs)), recording trail history. Followers trace the recorded trail at constant segment distances.
4. **Queue Swallowing & Capacity**: As ant units reach the deep pit inside the holder, [HoleController.cs:880](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs#L880) swallows units with scaling, swallow SFX, and burst VFX (`ant_dis`). When the entire queue finishes entering, `targetHole.OnQueueSwallowed()` decrements the holder's queue counter badge.
5. **Holder Completion & Permanent Grid Release**: When a holder's counter reaches 0 and all entering ants have despawned:
   - The holder enters `IsCompleted = true`.
   - `BoardManager.RemoveCompletedHole(hole)` unregisters **ALL cells across the holder's entire multi-cell footprint (`OccupiedCells`)** from `holesMap`.
   - Grid blocking is released immediately so remaining queues can path through that space.
   - The holder shrinks cleanly (`0.20s`, `Ease.InBack`) and its GameObject is destroyed.
6. **Win / Loss Evaluation**:
   - **Win**: All ant queues on the board have been cleared ([GameManager.cs:164](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/GameManager.cs#L164): `activeQueues.Count == 0`).
   - **Loss (Deadlock)**: If queues remain on the board, none are currently moving, and no remaining queue has an unblocked path to its matching hole, the game triggers `LevelLost` ("Deadlock!").
   - **Loss (Lives/Timer)**: Player loses all 3 hearts (deducted on invalid moves) or the countdown timer expires.

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
| **Normal Queues** | `AntQueueController`, `BoardManager`, `HoleController` | `Cells`, `ColorID`, `HolderType=1` | **WORKING** | 45 levels, 2,029 queues fully functional |
| **Linked Ants** | `BoardManager:638`, `BoardManager:1776`, `LinkedAntsRopeVisual` | `IsConnected`, `ConnectedIndex` | **WORKING** | 21 levels, 283 queues, 126 groups move atomically with GoldenRope visuals |
| **Locked Ants** | `AntLockBadge.cs`, `AntQueueController.cs` | `IsLocked`, `LockAmount` | **COMPLETE** | Padlock badge, move gating, counter decrement on rescues, unlock at 0 |
| **Stacked Ants** | `AntQueueController.cs`, `BoardManager.cs` | `IsSecondColor`, `SecondColor` | **COMPLETE** | Dominant moves first; lower layer revealed on accepted move as independent queue |
| **Pipes** | `PipeController.cs`, `BoardManager.cs` | `PipeCount` (in HolderType 2) | **COMPLETE** | 3D glossy tube mesh, directional entry gating, remaining-use badge |
| **Hard Overlay** | `GameUIManager.cs`, `Level.cs` | `LevelDifficulty` | **COMPLETE** | Pause timer/input during overlay, confirm to start, ant-only terminology |
| **Forward & Pipe Routing** | `BoardManager.cs` | Forward raycast + directional pipe polyline | **COMPLETE** | Direct straight ray + pipe transit paths; zero missing hole routes |

---

## 4. Key Systems & Exact File Paths

| System | File Path | Line Range | Responsibilities |
| :--- | :--- | :--- | :--- |
| **Game State Machine** | `Assets/Scripts/Core/GameManager.cs` | [L23-L232](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/GameManager.cs#L23-L232) | State flow, timer, lives, deadlock, win/loss triggers |
| **Board Manager** | `Assets/Scripts/Board/BoardManager.cs` | [L30-L1855](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L30-L1855) | Board lifecycle, `holesMap`, `ValidateForwardMove`, footprint release |
| **Ant Queue Controller** | `Assets/Scripts/Ants/AntQueueController.cs` | [L20-L851](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs#L20-L851) | Slither traversal, `trailHistory`, swallow triggers, completion |
| **Hole Controller** | `Assets/Scripts/Board/HoleController.cs` | [L5-L1096](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs#L5-L1096) | Procedural stadium mesh, badge counter, swallow VFX, release |
| **Linked Rope Visual** | `Assets/Scripts/Ants/LinkedAntsRopeVisual.cs` | [L12-L232](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs#L12-L232) | GoldenRope mesh stretching between linked leader units |
| **Level Progression** | `Assets/Scripts/LevelSystem/LevelManager.cs` | [L4-L200](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/LevelManager.cs#L4-L200) | `PlayerPrefs` progression, level database loading, advance/restart |
| **UI Management** | `Assets/Scripts/UI/GameUIManager.cs` | [L5-L354](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs#L5-L354) | Top bar HUD, settings modal, fail screen sequence, tutorial |
| **Camera Controller** | `Assets/Scripts/Camera/BoardCameraController.cs` | [L18-L511](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Camera/BoardCameraController.cs#L18-L511) | Fixed baseline framing (88° pitch, 78° FOV), drag-panning |
| **Audio Synthesizer** | `Assets/Scripts/Core/SoundManager.cs` | [L3-L197](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/SoundManager.cs#L3-L197) | Procedural byte-buffer audio synthesis for all SFX |

---

## 5. Critical Errors and Android Optimization Concerns

1. **Scene File Bloat (`Assets/Scenes/main.unity`)** - **[RESOLVED]**:
   - `main.unity` was sanitized from 66.7 MB down to 5.8 MB, eliminating 546 baked runtime clones and optimizing deserialization time.
2. **Material Memory Leak in Garden Background** - **[RESOLVED]**:
   - Implemented `materialCache` dictionary in `GardenEnvironmentPalette.cs` caching materials by color and smoothness tokens.
3. **Per-Ant DOTween Sequence Pressure** - **[RESOLVED]**:
   - Replaced looping DOTween sequences in `AntUnit.cs` with direct mathematical sine oscillation in `Update()` (`Mathf.Sin`), eliminating hundreds of active tween allocations on Android.
4. **Android Package & SDK Settings**:
   - [ProjectSettings.asset:173](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectSettings.asset#L173) still uses template package name `com.UnityTechnologies.com.unity.template.urpblank` and Target SDK 0.
   - Must be updated to a custom bundle identifier and Target API Level 34+ before Google Play release.
