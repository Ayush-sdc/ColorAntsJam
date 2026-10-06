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
7. **Slithering & Hole Entry**: Ants move segment-by-segment along the smooth route ([AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs)), recording trail history. Followers trace the recorded trail at constant segment distances. Each individual ant unit visibly reaches and enters the actual hole opening, plunging steeply into the pit interior (`y <= -0.34f` or `pitDescent >= 0.95f`) with progressive scale reduction. Ants do not prematurely pop out of existence at the rim while outside, nor do they traverse across the pit or emerge beyond the far rim on small $1\times 1$ holes. Followers enter sequentially in order. Logical queue completion and hole capacity decrements occur strictly when the tail unit finishes entering. Visual state is fully restored on pool reuse or level restart.
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
| **Normal Queues** | `AntQueueController`, `BoardManager`, `HoleController` | `Cells`, `ColorID`, `HolderType=1` | **WORKING** | Lengthwise queue rope removed; clean 3D ants, eyes, and materials preserved; progressive plunge & concealment |
| **Ant Queue Visual Settings** | `AntQueueVisualSettings.cs`, `AntQueueVisualSettingsEditor.cs` | `leaderVisualScale`, `workerVisualScale`, `movementSpacingMultiplier` | **COMPLETE** | Shared ScriptableObject config; tight continuous group reading; preserves logical grid cells, ant count, and footprint; Editor Apply & Rebuild |
| **Linked Ants** | `BoardManager:638`, `BoardManager:1776`, `LinkedAntsRopeVisual` | `IsConnected`, `ConnectedIndex` | **WORKING** | Leader connector preserved; dismisses into hole; all-or-none launch preserved; connector cleans up immediately when leaders enter hole |
| **Locked Ants** | `AntLockBadge.cs`, `AntQueueController.cs` | `IsLocked`, `LockAmount` | **COMPLETE** | Padlock badge, move gating, counter decrement on rescues, unlock at 0 |
| **Stacked Ants** | `AntQueueController.cs`, `BoardManager.cs` | `IsSecondColor`, `SecondColor` | **COMPLETE** | Dominant moves first; lower layer revealed on accepted move as independent queue |
| **Pipes & Multi-Pipe Routes** | `PipeController.cs`, `BoardManager.cs` | `PipeCount` (in HolderType 2) | **COMPLETE** | Sequential multi-pipe routes supported, loop/cycle detection (`PipeCycleDetected`), atomic reservations with rollback, usage consumed once per completed queue |
| **Level Completed Overlay** | `LevelCompletedOverlay.cs`, `GameUIManager.cs` | `HighestCompletedLevel`, `FeatureProgressionConfig` | **COMPLETE** | Progress bar initializes to 0% and animates upward to saved target using unscaled time, text stays synchronized, prefab defaults 0 / 0%, upcoming feature icon with blue glow, NEXT button |
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
| **Board Manager** | `Assets/Scripts/Board/BoardManager.cs` | [L30-L2357](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs#L30-L2357) | Board lifecycle, `ValidateForwardMove` (multi-pipe routing), atomic reservations, footprint release, moving queue walkability |
| **Ant Queue Controller** | `Assets/Scripts/Ants/AntQueueController.cs` | [L20-L1115](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs#L20-L1115) | Slither traversal, `trailHistory`, progressive hole plunge & concealment, `TraversedPipes` tracking |
| **Ant Queue Visual Settings** | `Assets/Scripts/Ants/AntQueueVisualSettings.cs` | [L1-L76](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueVisualSettings.cs#L1-L76) | Shared ScriptableObject config for visual scales and spacing without modifying logical grid cells |
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
11. **Level Completed Progress Bar Animating Downward / Prefab Flash (ERR-20)** - **[RESOLVED]**:
    - `Show()` previously activated the GameObject before resetting fill, and used `prevProgress` which was `1.0f` when the previous milestone was just completed, causing downward animation. Prefab defaults (`fillAmount: 0.5`, `text: 77%`) also flashed on frame 0. Fixed by calling `SetBarFill(0f)` before `gameObject.SetActive(true)`, animating strictly from `0f` to `newProgress` with unscaled time (`.SetUpdate(true)`), keeping text synchronized, updating prefab serialized defaults to `0` and `"0%"`, and resetting presentation on dismiss/reuse without resetting saved progress.
12. **Progressive Hole Entry Plunge & Concealment (ERR-21)** - **[RESOLVED]**:
    - Overrode previous behavior where ants vanished instantly upon leading edge contact with the outer rim. Implemented progressive steep downward plunge into the hole pit opening (`0.32f * cs` forward, `y` drops to `-0.35f`) with proportional scale reduction (`(1.0f - pitDescent)`). The ant unit remains visible as it enters the actual opening, submerging/concealing only once fully inside the pit (`pitDescent >= 0.95f` or `y <= initialAntY - 0.34f`). Does not traverse across the pit or emerge beyond the opposite rim on small $1\times 1$ holes. Followers enter sequentially in order. All visual state (visibility, scale, position) is cleanly restored on restart or pool reuse.
13. **Level 23 Multi-Pipe Route Traversal Blocked by Single-Pipe Assumption (ERR-22)** - **[RESOLVED]**:
    - In `BoardManager.cs:ValidateForwardMove`, the loop checked `if (traversedPipe != null || !p.CanEnterFrom(curr, currentDir) ...)` which unconditionally treated any second pipe along a route as an obstacle. On Level 23, `Queue_Record_75_3` must traverse Pipe #6 and then Pipe #7 into Hole #72. Fixed by generalizing route tracing to `List<PipeController> traversedPipes`, adding loop detection (`"PipeCycleDetected"`), reserving all required pipes atomically with full rollback on failure, generating composite spline world paths across sequential pipes, and consuming exactly one use per completed queue passage.
14. **Ant Queue Visual Spacing Configuration & Preservation (ERR-23)** - **[RESOLVED]**:
    - Large lengthwise gaps between ants within queues made queues appear disjointed rather than a cohesive group. Resolved by introducing `AntQueueVisualSettings` ScriptableObject with custom Editor preview and "Apply & Rebuild Scene Queues" button. Adjusted worker scale to `0.35f` and leader scale to `0.38f`, bringing visual unit length to ~1.0 grid unit while preserving 0.44 grid unit lateral clearance between parallel rows. Logical grid cell spacing, ant count, and tail coordinates are strictly preserved with zero jump, spread, or bunching when motion starts.
15. **Android Package & SDK Settings**:
    - [ProjectSettings.asset:173](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectSettings.asset#L173) still uses template package name `com.UnityTechnologies.com.unity.template.urpblank` and Target SDK 0.
    - Must be updated to a custom bundle identifier and Target API Level 34+ before Google Play release.
16. **Configurable Queue Movement Speed Multiplier (1.5x Default)**:
    - Exposed `movementSpeedMultiplier` (Range `0.5` to `4.0`, default `1.5`) in the Unity Inspector across [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs), [AntQueueVisualSettings.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueVisualSettings.cs), and [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs).
    - Calculated as `EffectiveMoveSpeed = MoveSpeed * multiplier`. Applied frame-rate-independently to leaders, followers, linked groups, pipe traversal, and hole entry without snapping, stretching, or skipping pipe/hole triggers.
17. **Feature Progress Display Capped at Level 30**:
    - For levels 31 onward, completion screens automatically hide the feature progress bar (`ProgressBarContainer`), percentage text, `"Next Feature"` label, and feature icon/glow/reveal animations.
    - Title (`TitleImage`) and Next button (`NextButton`) are dynamically repositioned to $Y = 120$ and $Y = -120$ respectively, cleanly eliminating the empty vertical gap.
    - Level 30 remains unaffected and continues displaying the final feature milestone progress.
18. **Dedicated Campaign Complete Screen (Level 45 End-of-Game)**:
    - Upon finishing the final campaign level (Level 45), [GameUIManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs) shows [CampaignCompletedOverlay.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/CampaignCompletedOverlay.cs) instead of the normal NEXT screen, preventing any attempt to load Level 46.
    - Screen features `"ALL LEVELS COMPLETED!"` title, celebratory cute ants illustration (`Illustration_LinkedAnts.png`) with rotating radial rays, `"You rescued every ant queue!"` message, and glossy `"PLAY AGAIN"` button.
    - Tapping `"PLAY AGAIN"` resets active level to 1 for replay without wiping saved `HighestCompletedLevel`, settings, or tutorial acknowledgements. Protected with click-debounce guards against duplicate launches.
19. **Global Simultaneous Queue Movement (Concurrency Fix)**:
    - Eliminated the arbitrary limit `MAX_CONCURRENT_MOVING_QUEUES = 4` in [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) and enabled multiple queues to move at once across different holes or the same hole.
    - Replaced the flawed multi-touch abort branch (`Input.touchCount >= 2 => abort`) in [PlayerInputHandler.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs) with finger-specific tracking. Taps on different queues trigger independently, while pinch gestures are isolated for camera zoom and cannot be misinterpreted as queue selections.
    - Repeated taps on an active moving queue are rejected without duplicate moves or lost lives. Moving queues freely overlap, cross, and share trajectories without collision. Hole capacities are atomically reserved (1 slot per queue, not per ant), preventing overbooking.
20. **Tutorial Feature Overlays, Legal Terms & Welcome Architecture (Oct 2026)**:
    - **Flattened Tutorial Compositions**: Integrated 4 supplied tutorial PNGs (`Feature_Linked_Ants.png`, `Feature_Stacked_Ants.png`, `Feature_Pipe.png`, `Feature_Locked_Ants.png` ~1201×1309 px) in `Assets/UI/Sprites/Tutorials/FeatureOverlays/`. Imported as Sprites with alpha preserved, mipmaps disabled, clamp mode, and max size 2048. Dedicated editable prefabs in `Assets/Prefabs/UI/Tutorials/` use `FeatureOverlayView` with `AspectRatioFitter` (1201/1309) and an invisible `Button` hit area anchored precisely over the illustrated OK button (`anchorMin = (0.31, 0.031)`, `anchorMax = (0.69, 0.115)`). Duplicate titles, descriptions, and second buttons are suppressed.
    - **Progression Integration**: Configured in `FeatureProgressionConfig.asset` at levels 7 (Linked Ants), 15 (Stacked Ants), 21 (Pipes), and 30 (Locked Ants). Acknowledged only upon tapping OK; persists via `SeenTutorial_<id>` in `PlayerPrefs`. Pauses timer and blocks board/camera input during display.
    - **Legal Terms Modal in Settings**: Recreated from `Privacy_policies.png` with periwinkle-blue rounded frame, cream inset, red circular close button, blue gradient buttons, and scrollable document modal (`LegalDocumentModal.cs`).
      - *Privacy Policy & Terms of Service*: Configured in [LegalConfig.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Legal/LegalConfig.cs) (`LegalConfig.asset`), viewable in-game via `LegalDocumentModal` or opened via browser URL.
      - *Account Deletion & Data Request*: Opens clear request instructions; never erases local or remote data directly on tap.
      - *Personalized Ads*: Since no advertising SDK is currently installed, the button is visibly disabled (`interactable = false`) with clear informational feedback ("Personalized advertising is disabled. No ad SDK installed.") and no decorative fake switch.
      - *Player ID Row*: Omitted by default until a real backend authentication or analytics account service is configured.
    - **Welcome Overlay**: Recreated from `Welcome.png` in `Assets/Prefabs/UI/Legal/WelcomeOverlay.prefab` with deep-purple backdrop, cream panel, thick blue backing, purple body copy, clickable ToS / Privacy Policy links, blue footer, and green ACCEPT button.
      - Displays on first launch when acceptance record is absent, and on a configured terms version update requiring renewed acceptance.
      - Acceptance recorded via [LegalManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Legal/LegalManager.cs) (`Legal_AcceptedTermsVersion`, `Legal_AcceptedTermsTimestamp`, `Legal_AcceptedPlayerId`) strictly upon tapping ACCEPT.
      - Gameplay timer and board interaction paused until accepted, then smoothly continues into introductory level sequence.
    - **Android Back Stack Hierarchy**: Escape key / Android Back dismisses modals strictly topmost-first (`LegalDocumentModal` -> `LegalTermsModal` -> `SettingsModal` -> Board). Board input is restored only when all modals are closed.
    - **Editor Tooling & Automation**: Menu items provided under `Tools/Slither Ants Jam/Tutorials/` and `Tools/Slither Ants Jam/Legal/` for zero-progression previewing and testing.

21. **Overlay Visibility, Entry Effects Removal & Audio Routing Fixes (Oct 2026)**:
    - **Issue 1 Root Cause & Resolution (Missing Overlay Artwork)**:
      - *Root Cause*: In [FeatureTutorialModal.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/FeatureTutorialModal.cs), `customPrefabContainer` was serialized as null (`{fileID: 0}`) in `FeatureTutorialModal.prefab`. When displaying features with `TutorialPrefab`, the modal suppressed the default UI elements but failed to instantiate the prefab into any container, leaving only `backdropDim` visible. Furthermore, `UIOverlaysSetup.cs` had previously wiped custom prefab references on setup passes.
      - *Fix & Architecture*: Added `EnsureCustomPrefabContainer()` to dynamically locate or create `CustomPrefabContainer` under the modal transform with full-screen stretch anchors (`anchorMin=0, anchorMax=1`, scale=1) set as the last sibling (in front of the backdrop dim). In `DisplayFeature()`, prefabs are instantiated with `worldPositionStays: false`, anchors and scales normalized, child `BackdropDim` disabled on the prefab to avoid double dimming, and `FeatureOverlayView` configured with `Color.white` tint, preserved aspect ratio, and active hit area.
      - *Fallback Protection*: If both prefab and artwork are missing, logs `Debug.LogError` with the exact feature ID and level, displaying a clean, dismissible fallback modal instead of an opaque blank screen.
      - *Shared Lifecycle Audit*: Confirmed Welcome, Legal Terms, Hard Level Intro, Level Completed, and Campaign Complete screens properly manage their canvas groups, dimming layers, and board/timer pause states without leaving invisible raycast blockers.
    - **Issue 2 Root Cause & Resolution (Ant-Entry Smoke and Glow Effects Removed)**:
      - *Root Cause*: In [HoleController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs), `SpawnSwallowVFX()` dynamically instantiated `Resources.Load<GameObject>("Ants/Prefabs/ant_dis")` (containing `Nest FX` smoke, dust, and glow particle systems) on every ant swallow event (`OnAntSwallowed`).
      - *Fix*: Disabled `SpawnSwallowVFX()` at the source. Created `CleanupActiveEntryEffects()` static method to find and destroy active or pooled `ant_dis` / `Nest FX` instances on board initialization and level reset in [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs) (`ClearBoard`).
      - *Preservation*: The progressive steep pit plunge math (`y <= initialAntY - 0.34f` or `pitDescent >= 0.95f`), scale reduction, hole geometry, capacity, and rescue timing remain fully intact.
    - **Issue 3 Root Cause & Resolution (Move Hole-Entry Sound to Queue Click)**:
      - *Root Cause*: `swallowClip` played in `HoleController.OnAntSwallowed()` and `OnQueueSwallowed()`, while queue movement start played a generic `PlayTap()` via `SoundManager.HandleQueueMoveStarted`.
      - *Fix*: Added `SoundManager.PlayQueueSelect()`, reusing the exact `swallowClip`. Triggered immediately in [PlayerInputHandler.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs) (`ProcessClickOrTap`) when a stationary queue receives a valid tap. Suppressed playback for moving queues, dragging, pinching, UI pointer taps, and empty-board taps. Linked groups play the clip once per user tap.
      - *Removal & Stacking Prevention*: Completely removed audio playback from `HoleController.OnAntSwallowed` and `OnQueueSwallowed`. Cleared `SoundManager.PlaySwallow()` and suppressed generic `PlayTap()` from `HandleQueueMoveStarted` to avoid stacking. Mute setting in `SoundManager` silences queue selection cleanly.22. **Dedicated Welcome Startup Scene & Refined Feature Progression Architecture (Oct 2026)**:
    - **Dedicated Welcome Startup Scene (`Assets/Scenes/Welcome.unity`)**:
      - Configured as Scene index 0 in [EditorBuildSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/EditorBuildSettings.asset) (`[0] Welcome.unity`, `[1] main.unity`).
      - Built using full editable Unity UI: deep-purple background (`#0E0818`), cream rounded panel (`#F6EFE6`), blue badge with outlined `WELCOME` heading, configured legal body copy, interactive Terms of Service & Privacy Policy link buttons (opening [LegalDocumentModal.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/LegalDocumentModal.cs)), and green `ACCEPT` button.
      - **Flow**:
        - Controlled by [WelcomeSceneController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/WelcomeSceneController.cs).
        - If terms already accepted and not in editor preview mode: immediately skips to saved gameplay scene (`main.unity`) at the player's saved level.
        - On explicit ACCEPT: persists acceptance with ISO timestamp via [LegalManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Legal/LegalManager.cs) and loads `main.unity`. Guarded against repeated taps and duplicate scene loads. Never accepts missing or unconfigured legal documents.
        - Zero duplicate managers: `Welcome.unity` does not use `DontDestroyOnLoad`; scene replacement destroys cameras, canvases, and EventSystems, leaving zero duplicate listeners in `main.unity`.
        - Editor preview provided via `Tools/Slither Ants Jam/Legal/Preview Welcome Scene (No Data Reset)` or `SlitherAntsTestingWindow.cs` without erasing player data.
    - **Shared Feature Progression Milestones**:
      - Configured in [FeatureProgressionConfig.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Data/Progression/FeatureProgressionConfig.asset):
        - **Linked Ants**: Intro Level = 7, Start Milestone = 1 (completed levels 1–6 / 6).
        - **Stacked Ants**: Intro Level = 15, Start Milestone = 7 (completed levels 7–14 / 8).
        - **Pipes**: Intro Level = 21, Start Milestone = 15 (completed levels 15–20 / 6).
        - **Locked Ants**: Intro Level = 30, Start Milestone = 21 (completed levels 21–29 / 9).
      - **Level Data Mechanics Appearance Audit**:
        - First Linked Ants in JSON data: Level 7 (matches milestone).
        - First Stacked Ants in JSON data: Level 15 (matches milestone).
        - First Pipes in JSON data: Level 23 (presentation milestone is 21; puzzle layout strictly preserved).
        - First Locked Ants in JSON data: Level 30 (matches milestone).
        - Confirmed: No mechanics are introduced earlier than their presentation milestones.
    - **Consistent Range Boundaries & Replay Progression**:
      - Completed levels 1–6: Linked Ants preview; reaches 100% after level 6 completion.
      - Completed levels 7–14: Stacked Ants preview; reaches 100% after level 14 completion.
      - Completed levels 15–20: Pipes preview; reaches 100% after level 20 completion.
      - Completed levels 21–29: Locked Ants preview; reaches 100% after level 29 completion.
      - Level 30 onward: Next-feature progress section is completely hidden (`effectiveLevel < 30`). Title and Next button dynamically reposition to $Y=120$ and $Y=-120$ to close vertical gap.
      - **Replay Protection**: Progression calculated from `effectiveLevel = Mathf.Max(justCompletedLevel, savedHighest)`. Replays cannot add progress, and display progress toward the player's next still-locked feature (or stay hidden if all features are unlocked).
      - **Presentation Reset**: Displayed progress bar always initializes at 0% and animates smoothly upward to saved target percentage via unscaled DOTween sequence (`.SetUpdate(true)`).
    - **Preview Sprites Extracted from Central Compositions**:
      - Separate preview sprites extracted from the central illustrations in `AntFeatureOverlays`:
        - `Preview_LinkedAnts.png` (Blue/red ants connected by rope)
        - `Preview_StackedAnts.png` (Blue ant with yellow stacked markings)
        - `Preview_Pipes.png` (Cyan pipe)
        - `Preview_LockedAnts.png` (Ant with chain and gold padlock)
      - Cropped with transparent alpha, square aspect ratio, and assigned to `PreviewIcon` in `FeatureProgressionConfig.asset`.
    - **Tutorial Display Sequence at Level Start**:
      - Level start sequence: Completion Screen -> NEXT -> Load Level -> Hard Intro -> Unacknowledged Feature Tutorial Modal -> Gameplay.
      - Board interaction, timer, and camera gestures paused until player presses OK.
      - Acknowledgement persisted only on OK. Quitting before OK redisplays the tutorial on return. Scoped testing-window reset controls preserve campaign progress.

---

## 20. Wrong-Move Feedback Architecture & Physical Precision Fix (Oct 2026)

- **Mechanic Overview**:
  - Replaced immediate vibration and tap-time life loss with physical queue approach, visible impact bump into stationary blockers, recoil pause, single "ouch" sound, exactly one life deducted at impact, and path retracing back to the exact initial unit coordinates and idle state.
  - All valid-move mechanics, pathfinding, and level 14 self-collision rules are strictly preserved.

- **Root Causes of Previously Observed Bugs**:
  1. **Visual Overlap / Mesh Penetration**:
     - *Issue*: A fixed scalar offset (`0.72f`) was used to offset the leader from the blocker's grid cell center.
     - *Root Cause*: Renderers on `ChubbyAntColors` and `WorkerAnt` have real visual extents: leader forward extent $\approx 0.52$, worker rear extent $\approx 0.48$, side half-width $\approx 0.29\text{--}0.31$. For a head-to-head collision, the centers must be separated by at least $1.04$ units. A fixed $0.72$ offset allowed up to $0.32$ units of mesh interpenetration (over 30% of ant body length).
  2. **Lateral Drift / Steering Towards Blocker**:
     - *Issue*: The leader queue drifted sideways instead of advancing strictly straight.
     - *Root Cause*: `GenerateApproachRouteToContact` previously interpolated between `headWorldPos` and `contactPos` (`Vector3.Lerp`). If the blocker ant's pivot had any minute lateral offset or if the contact cell had a slight diagonal component, the route tilted sideways off the cardinal heading.
  3. **Blocker Queue Displacement**:
     - *Issue*: The stationary blocker shifted during impact.
     - *Root Cause*: `PlayCosmeticBump` invoked `transform.DOPunchPosition(push, 0.18f, 6, 0.5f)` on the blocker queue, shoving it through the moving queue.

- **Verified Physical Simulation Implementation**:
  - **Dynamic Collision Proxy Helper (`AntCollisionHelper.cs`)**:
    - Derives 2D Oriented Bounding Boxes (OBBs) from authoritative ant visual bounds (`AntLeaderVisualExtents = [0.31, 0.52]`, `AntWorkerVisualExtents = [0.29, 0.415]`) scaled by runtime `visualScale`.
    - Implements Separating Axis Theorem (SAT) with continuous swept distance calculation: calculates the exact distance $d$ along movement heading where the moving leader's OBB touches any segment OBB of the blocking queue, minus configurable safety clearance ($0.04$ units).
  - **Strict Straight Approach**:
    - `GenerateApproachRouteToContact` projects strictly along the cardinal heading forward unit vector: `headWorldPos + fwd * (dist * t)`. The perpendicular coordinate is 100% constant ($0.0000$ lateral drift outside pipes).
  - **Follower Curve Retracing & Forward-Facing Backup**:
    - Followers slither forward along existing queue bends and onto the leader's extension; initially bent queues do not translate as a rigid block.
    - During return, all ants retrace their traveled polyline backward. Ants face forward as they back up (zero $180^\circ$ rotation flips, shortcuts, or teleports).
  - **Stationary Blocker & Backward-Only Recoil**:
    - Removed `DOPunchPosition` on blocker; blocker remains strictly stationary ($0.0000$ displacement).
    - Leader recoil only reduces traveled distance along the trail (backward only).
  - **Single Life Loss & Audio Sync**:
    - Exactly one heart deducted and one "ouch" sound played per failed action (including linked groups).
    - If the blocker starts moving before contact, the collision penalty is canceled (safe return, 0 hearts lost).
  - **Exact Pose Restoration**:
    - Restores exact original position, rotation, and idle spacing ($0.0000$ error).

---

## 21. Wrong-Color Hole Failed-Move Animation & Outer Rim Boundary System (Oct 2026)

- **Mechanic Overview**:
  - Extends the wrong-move physical feedback system to **wrong-color holes** while preserving existing stationary-queue collisions and static feedback rules.
  - When tapped, a queue heading directly toward a wrong-color hole executes a physical advance straight forward along its cardinal heading, stops its leading edge cleanly at the hole's outer rim without entering the pit, plays the impact "ouch" and deducts 1 heart, reverses along the traveled path, and restores its exact initial pose.

- **Key Technical Implementations**:
  1. **Failure Classification (`MoveValidationResult`)**:
     - Classifies `WrongColorHole` failure separately from queue collisions, full holes, or missing destinations.
     - Sets `IsBlockedByWrongColorHole = true` and `IsAnimatedWrongMove = true`.
     - Preserves static non-animated feedback (shake and immediate heart loss) for `MatchingHolderFull` and `IsLocked` queues.
  2. **Outer Rim Boundary Geometry (`HoleController.GetOuterRimBounds`)**:
     - Dynamically extracts the floor-level outer rim bounding box from `SlotRim` renderer bounds or cell footprint minus margin ($W = w \cdot cs - 0.08 \cdot cs, H = h \cdot cs - 0.08 \cdot cs$).
     - Scales accurately across $1\times 1$, $1\times 2$, and $2\times 2$ hole geometries.
  3. **Continuous Swept Contact Detection (`AntCollisionHelper.ComputeSweptContactDistanceWithHole`)**:
     - Uses 2D Oriented Bounding Box (OBB) Separating Axis Theorem (SAT) swept testing and binary bisection along the leader's cardinal heading.
     - Ensures the ant's leading tip stops at the outer rim with a 0.04m safety clearance.
     - The ant's head pivot ($z = 0$) stays strictly outside the hole at all times (distance from rim $> 0.56$m).
     - Continuous swept testing prevents high speeds ($20\,\text{units/s}$) or large frame deltas from tunneling through the rim.
  4. **Strict Cardinal Approach (Zero Lateral Steering)**:
     - The approach route extends strictly along the queue's original cardinal heading ($0^\circ, 90^\circ, 180^\circ, 270^\circ$).
     - Zero lateral alignment or steering toward the center of the hole occurs; the queue maintains its approach lane.
  5. **Full Visibility & Rescue Effect Suppression**:
     - 100% of ant units remain fully visible and rendered throughout the failed attempt.
     - Swallowing, pit descent, scale reduction, fading, clipping, and rescue completion are completely bypassed.
  6. **Zero Resource Consumption**:
     - Failed wrong-color contacts never reserve or consume hole capacity (`RemainingCount` and `InboundReservations` unchanged).
     - Padlock counters are never decremented.
     - Stacked ant lower layers are never revealed or activated.
  7. **Atomic Linked Group Synchronization**:
     - Linked queues advance in parallel lockstep.
     - If any member contacts an obstacle (wrong-color hole or stationary queue), all members stop at the earliest contact distance.
     - All members recoil and pause together, retrace backward in unison, and apply exactly **one heart deduction** per failed group action.
  8. **Existing Rule Preservation**:
     - Full same-color holes preserve static non-animated shake and immediate life loss.
     - Locked queues preserve static padlock feedback.
     - Valid same-color simultaneous hole entry remains 100% unchanged.


