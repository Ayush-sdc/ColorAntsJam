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

---

## 22. Pipe Visual Pass (Level 22 Reference Conduits, Oct 2026)

- **Visual Target**:
  - Replaced cylindrical/hose-like pipe visual meshes with **glossy cyan conduits** featuring straight sides, soft beveled cross-sections, square 90-degree corner silhouettes, diagonal miter seams, pale-cyan end collars, and squircle usage badges matching the reference layout of Level 22 from *Color Slither Jam*.
  - Preserved all characters as **3D Ants** (head, thorax, abdomen, 6 legs, antennae, eyes; zero caterpillars, slithers, or snake bodies).
  - Preserved all level layouts, routing, cell footprints, simultaneous queue movement, pathfinding, and usage counters.

- **Key Technical Implementations**:
  1. **Authoritative Configuration (`PipeVisualConfig.cs`)**:
     - Created [PipeVisualConfig.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/PipeVisualConfig.cs) ScriptableObject asset at `Assets/Resources/PipeVisualConfig.asset`, loaded via `PipeVisualConfig.Instance`.
     - Exposes fully editable Inspector parameters:
       - *Conduit Dimensions*: `pipeWidth = 0.84`, `pipeHeight = 0.34`, `bevelWidth = 0.13`, `bevelHeight = 0.10`.
       - *End Collars*: `collarWidth = 0.92`, `collarThickness = 0.08`, `collarColor = #D8F8FF`.
       - *Badges*: `badgeSize = 42`, `badgeForwardOffset = 0.35`, `badgeLateralOffset = 0.18`, `badgeSortingOrder = 5`, `outerBorderColor = #51D0FE`, `innerFaceColor = #004B87`.
       - *Contact Shadows*: `shadowWidth = 1.05`, `shadowAlpha = 0.40`, `shadowYOffset = 0.002`.
       - *Materials & Shading*: `customPipeMaterial`, `pipeBaseColor`, `smoothness = 0.88`, `metallic = 0.05`.
  2. **Procedural Square Mitered Conduit Geometry (`PipeController.cs`)**:
     - `GenerateSquareMiteredPipeMesh` in [PipeController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/PipeController.cs) constructs a 5-facet longitudinal conduit: top flat face, left/right chamfer bevels, and left/right vertical side walls.
     - At 90-degree turns, calculates exact angle-bisector miter vectors ($M = \nu_u + \nu_v$), creating clean 45-degree diagonal seams with zero pinching, bulging, gaps, or overlapping faces. Constant visible width is maintained across all straight, L-shaped, and U-shaped runs.
     - Maps continuous longitudinal $V$ distance and exact cross-sectional $U$ facet coordinates to `PipeConduit_Gradient.png` (upper turquoise highlight, peak specular line, saturated cyan body, deep blue side shadow), ensuring identical shading on both horizontal and vertical runs.
  3. **Pale-Cyan End Collars**:
     - `GenerateCollarMesh` constructs a collar perpendicular to the pipe entrance/exit mouths, extending slightly wider than the conduit (`0.92` units) with rounded pill caps.
     - Automatically aligns with endpoint direction and remains unaffected by length scaling.
  4. **Squircle Counter Badges & Modal Layering**:
     - Built using 9-sliced `Badge_Squircle.png` double-plate (cyan border and deep blue face) with bold white numeral and dark drop outline.
     - Docked laterally along the outer rim near the entrance mouth without obstructing the traversal path.
     - Rendered on a World Space Canvas with `sortingOrder = 5` (strictly $\le 10$), guaranteeing that modal overlays (settings, tutorials, level complete/lost at `sortingOrder = 50+`) render cleanly on top.
  5. **Ant Traversal & Centerline Preservation**:
     - Retains exact logical grid centerlines via `GetCenterlineWaypoints()`.
     - Ant units enter and exit precisely at collar mouths with smooth continuous motion.
     - Simultaneous queue movement, pipe reservations, and exhaustion destruction remain 100% intact.

- **Verification Results**:
  - Executed via `Tools/Slither Ants Jam/Tests/Run Complete Pipe Visual Verification Suite` ([PipeVisualVerificationSuite.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/PipeVisualVerificationSuite.cs)):
    - **Test 1: Comparison Gallery** (`Evidence_PipeComparison_Overview.png`, `Evidence_PipeComparison_CloseUp_Corners.png`, `Evidence_PipeComparison_CloseUp_Collars.png`):
      - Verifies straight pipe (counter 1), 90-degree bend (counter 2), long U-pipe (counter 3), and adjacent nested U-pipes (counters 2 & 1).
      - Confirms square outer silhouette, crisp diagonal miter seams, constant conduit width, pale-cyan end collars, and squircle counter badges.
    - **Test 2: Level 22 Board Verification** (`Evidence_Level22_FullBoard.png`, `Evidence_Level22_LeftNestedPipes.png`, `Evidence_Level22_MiddlePipeAndMouth.png`):
      - Recreated Level 22 layout matching `media_1791406165950.png` with nested U-pipes, middle straight conduit with yellow ant lined up at entrance mouth, and independent counter badges.
    - **Test 3: Level 23 Multi-Pipe Traversal (`Queue_Record_75_3`)** (`Evidence_Level23_Queue75_3_Traversal.png`):
      - Verified multi-pipe pathfinding through Pipe 6 and Pipe 7 into target hole, atomic pipe reservation, and use counter decrement.
    - **Test 4: Modal Layering Verification** (`Evidence_PipeModalLayering.png`):
      - Verified pipe counter badge canvas (`sortingOrder = 5`) renders strictly beneath simulated modal backdrop (`sortingOrder = 50`).
  - Executed all project test suites:
    - `FeatureUIPlayModeTests` (Level 23 Multi-Pipe): **3 / 3 PASS**
    - `PreciseFailedMoveVerificationSuite`: **14 / 14 PASS**

---

## 23. Translucent Pipe Traversal, Return Speed Parity & Entrance Counters (Oct 2026)

### 1. Translucent Ants Inside Pipes
- **Rendering Pipeline Architecture**:
  - Worker & Chubby Ant shaders ([WorkerColors.shader](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/WorkerAnt/Shaders/WorkerColors.shader), [ChubbyColors.shader](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/ChubbyAntColors/Shaders/ChubbyColors.shader)) updated with `_Opacity` (default `1.0`) and `_PipeTint` (default `(0.85, 0.96, 1.0, 1.0)`).
  - Shaders assigned `Tags { "Queue"="Geometry+50" "RenderType"="Transparent" }`, `Blend SrcAlpha OneMinusSrcAlpha`, and `ZWrite On`.
  - Pipe conduit surface rendered in Transparent queue (`Queue = 3000`) with semi-transparent alpha (`pipeConduitOpacity = 0.78f`) and specular cyan gloss.
  - Ant geometry renders into the scene depth buffer first, and the conduit surface renders directly over it. This guarantees ants visually appear **inside/beneath** the glossy pipe shell, without X-ray bleed through unrelated obstacles, borders, or UI overlays.
- **Per-Ant Smooth Entry & Exit Transition**:
  - Ant opacity transitions per ant unit ([AntUnit.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntUnit.cs), [AntVisualConfig.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntVisualConfig.cs)) via `MaterialPropertyBlock` (`ApplyInsidePipeOpacity` / `ResetInsidePipeOpacity`), completely avoiding shared material mutations that could affect other queues.
  - Ants outside the pipe remain 100% opaque.
  - When an individual ant unit crosses the logical entrance collar into the conduit, its opacity interpolates smoothly to 40% (`insidePipeAntOpacity = 0.40f`) with soft cyan tinting (`insidePipeAntTint`).
  - As each ant exits the pipe collar, opacity smoothly restores to 1.0.
  - Full appearance cleanup is guaranteed on movement completion, queue swallowing, cancel, disable, destroy, and object-pool reuse.
- **Multi-Queue Concurrency & Complex Routing**:
  - Centerline waypoints strictly followed through straight runs, 90-degree square elbows, and U-bends.
  - Multiple queues traversing the same or adjacent pipes simultaneously maintain independent opacity states without flickering or popping.

### 2. Wrong-Move Return Speed Parity
- **Exact 1:1 Speed Matching**:
  - The return movement strictly shares the identical world-units-per-second speed as normal forward movement: `normalSpeed = EffectiveMoveSpeed` (`ReturnSpeed` property in [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs)).
  - Return duration is computed directly as `actualTravelledDistance / normalSpeed`.
  - Removed fixed recoil Lerp hop loops (previously 0.08s fixed hop) and arbitrary recoil multipliers.
  - Return movement proceeds at a constant path-distance speed without easing, acceleration, or deceleration.
- **Exact Path Retracing & Pose Preservation**:
  - Queue followers retrace their recorded trail distance in reverse (`virtualHeadDist -= normalSpeed * dt`).
  - Units back away naturally along the path without flipping the entire queue 180 degrees.
  - Final poses snap to original starting coordinates and rotations with zero drift.
- **Fail State Rules**:
  - Exactly 1 impact sound (`PlayOuch()`) and 1 life deduction per failed action.
  - Zero hole capacity, slot reservation, or pipe uses consumed.
  - Linked queues advance, impact, pause, and return in parallel lockstep.

### 3. Larger Pipe Counters at Entrance
- **Visual Design Reference (`Feature_three_pipe.png`)**:
  - Squircle badge plate with solid dark blue inner face (`#004B87`) and vibrant cyan border (`#51D0FE`).
  - Crisp, large white numeral with a strong dark outline for readability on mobile screens.
- **Anchoring & Orientation**:
  - Anchored to the pipe's logical `EntranceCell` beside the entrance collar without covering entering ants or blocking the mouth.
  - Rotation locked to camera-facing upright angle `Quaternion.Euler(60f, 0f, 0f)`, ensuring upright readability across horizontal, vertical, and bent pipes.
  - World scale is uniform and completely decoupled from pipe length and parent hierarchy transforms.
  - Actual remaining use value is dynamically preserved and decremented on entry.
- **Layering & Non-Blocking**:
  - Graphic raycasts disabled (`raycastTarget = false`) on canvas, image, and text.
  - Canvas `sortingOrder = 5`, ensuring badges remain beneath modal overlays (`sortingOrder >= 50`).

### 4. Inspector Controls & Configuration
- **`PipeVisualConfig.cs` (`Assets/Resources/PipeVisualConfig.asset`)**:
  - `insidePipeAntOpacity` (float, default `0.40f`): Translucent opacity for ants inside conduit.
  - `insidePipeAntTint` (Color, default `#D9F5FF`): Soft cyan tint applied to ant body.
  - `pipeConduitOpacity` (float, default `0.78f`): Conduit transparency to balance shell gloss with ant visibility.
  - `badgeScale` (float, default `0.012f`): Global badge canvas scale.
  - `badgeSize` (float, default `52f`): Width and height of badge plate.
  - `numeralSize` (int, default `34`): Font size of the counter numeral.
  - `badgeForwardOffset` (float, default `0.20f`): Longitudinal offset relative to entrance mouth.
  - `badgeLateralOffset` (float, default `0.48f`): Lateral offset docking badge beside the collar.
  - `badgeHeightOffset` (float, default `0.42f`): Vertical elevation above ground.
- **`PipeController.cs`**:
  - Exposes local override Inspector fields for `badgeScale`, `numeralSize`, `entranceForwardOffset`, `entranceLateralOffset`, and `entranceHeightOffset`.

### 5. Verification & Test Evidence
- **Automated Verification Suite** (`TranslucentPipeAndReturnSpeedVerificationSuite.cs`):
  - Executed via menu `Tools/Slither Ants Jam/Tests/Run Complete Translucent Pipe & Return Speed Suite`:
    - `Test 1: Partial Queue Entry Translucency (Per-Ant Opacity & Tint)`: **PASS**
    - `Test 2: Square 90-Degree Bend Centerline Traversal & Exit Restoration`: **PASS**
    - `Test 3: Multiple Queues Inside Pipe Simultaneously (No Shared Material Mutation)`: **PASS**
    - `Test 4: Reset & Appearance Restoration on Cleanup / Disable / Restart`: **PASS**
    - `Test 5: Wrong-Move Return Speed Parity (1:1 with Normal Movement Speed)`: **PASS**
    - `Test 6: Larger Entrance Badge Anchor, Upright Text & Modal Layering`: **PASS**
    - `Test 7: Gameplay Traversal & Failed-Move Frame Sequences Generation`: **PASS**
    - **Total: 7 PASSED, 0 FAILED**
- **Captured Test Artifacts & Gameplay Animations** (`Assets/TestEvidence/TraversalAndReturnVerification/`):
  - `Evidence_PartialEntry_InsideTranslucent_OutsideOpaque.png`: Head ant inside at 40% opacity, tail ant outside fully opaque.
  - `Evidence_FullTraversal_TranslucentBeneathShell.png`: Full queue moving visibly beneath glossy conduit.
  - `Evidence_ExitRestoration_OpaqueRestored.png`: Clean 100% opacity restored upon exiting collar.
  - `PipeTraversal_Demo.gif` & `PipeTraversal_Demo.webp` (24 frames): Smooth traversal through pipe entrance, conduit, and exit.
  - `FailedMoveReturn_Demo.gif` & `FailedMoveReturn_Demo.webp` (29 frames): Forward movement at normal speed, contact without penetration, impact pause, and exact 1:1 reverse return to starting pose.
- **Status of Tests**:
  - Automated unit and integration tests passed with zero errors.
  - Physical multi-touch device gestures remain for human player validation on target mobile hardware.

---

## 24. Sound Effects System & Audio Event Integration (Oct 2026)

### 1. Source Audio Asset Mapping & Import Specifications
- **Source Folder**: Root project directory `sounds/` preserved untouched.
- **Import Destination**: `Assets/Audio/SFX/` with Android short-SFX settings:
  | Event | Source File | Imported Asset Path | Asset GUID | Duration | Android Importer Settings |
  | :--- | :--- | :--- | :--- | :--- | :--- |
  | **1. Ant Queue Tap** | `sounds/Tap sound.mp3` | `Assets/Audio/SFX/Tap sound.mp3` | `4de165e7a259ebb45a90bb4f7e4858fe` | ~1.02s (sharp pop) | Force to Mono, Preload Audio Data, Decompress On Load |
  | **2. Wrong-Move Impact** | `sounds/wrong move.mp3` | `Assets/Audio/SFX/wrong move.mp3` | `b7069c1e7f74be04596c3242bf2b335b` | ~1.18s (impact boing) | Force to Mono, Preload Audio Data, Decompress On Load |
  | **3. Level Completed** | `sounds/winning sound.mp3` | `Assets/Audio/SFX/winning sound.mp3` | `a486ed4c63c8ce14397f867ff46e84d5` | ~3.24s (victory fanfare) | Force to Mono, Preload Audio Data, Compressed In Memory |
- **Audio Spatialization**: Strictly non-positional 2D playback (`sfxSource.spatialBlend = 0f`).

### 2. Inspector Controls & Volume Configuration
- **Script**: [SoundManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/SoundManager.cs)
- **Scene Location**:
  - `Assets/Scenes/main.unity` -> `GameController` GameObject -> `SoundManager` component.
  - `Assets/Scenes/main.unity` -> `SoundManager` GameObject -> `SoundManager` component.
- **Serialized Clip Fields**:
  - `Tap Clip`: Serialized `AudioClip` field (`tapClip`), wired to `Tap sound.mp3`.
  - `Ouch Clip`: Serialized `AudioClip` field (`ouchClip`), wired to `wrong move.mp3`.
  - `Win Clip`: Serialized `AudioClip` field (`winClip`), wired to `winning sound.mp3`.
- **Separate Volume Controls**:
  - `Tap Volume` (Slider `0.0` - `1.0`, default `0.80f`): Controls queue select tap volume.
  - `Impact Volume` (Slider `0.0` - `1.0`, default `0.95f`): Controls wrong-move impact volume.
  - `Completion Volume` (Slider `0.0` - `1.0`, default `1.00f`): Controls level completed fanfare volume.
  - `Master Volume` (Slider `0.0` - `1.0`, default `0.80f`): Global SFX scaling factor.

### 3. Event Handling, Timing & Guard Rules
1. **Ant Queue Tap**:
   - Triggered strictly once in `PlayerInputHandler.ProcessClickOrTap` when an interactable queue is selected.
   - Linked groups selected with one tap produce exactly **one** tap sound.
   - Throttled with `TapThrottleDuration = 0.04s` to prevent duplicate playback or volume spikes from multi-touch events while allowing legitimate rapid distinct selections.
   - Suppressed for camera dragging, pinch zooming, UI clicks, ignored taps on moving queues, and automated callbacks (`HandleQueueMoveStarted` intentionally silences movement initiation sound).
   - Tapping that initiates a wrong move still plays the tap sound; the impact sound occurs later upon actual collision.
2. **Wrong-Move Impact**:
   - Plays strictly at actual contact with a blocking stationary queue or wrong-color hole rim (`AntQueueController.ExecuteImpactSequence` -> `SoundManager.PlayOuch()`).
   - Does NOT play on initial selection while approaching the obstacle.
   - Plays once per failed action; linked groups are guarded by `isEarliestCollisionTrigger` so only the earliest contact triggers audio and life deduction.
   - Does NOT repeat during the reverse return animation.
   - Fully replaces the legacy fallback procedural boing without duplicate playback.
3. **Level Completed**:
   - Triggered once when the game confirms level completion via `GameManager.OnGameStateChanged` (`GameState.LevelWon` -> `SoundManager.PlayWin()`), coordinated with `LevelCompletedOverlay` and `CampaignCompletedOverlay` (final level 45).
   - Occurs strictly after all required queues finish entering their holes.
   - Guarded by `hasPlayedWinSoundForCurrentLevel`: ignores redundant overlay re-enables and does not play per-queue or on NEXT.
   - Reset cleanly on level restarts, scene loads, and level transitions (`ResetCompletionGuard`).
   - Preserves the last accepted move grace period when the timer reaches 0:00.
4. **Hole Entry Silence Preserved**:
   - Zero audio on ant hole entry (`HoleController.OnAntSwallowed` and `OnQueueSwallowed` have zero audio triggers; `SoundManager.PlaySwallow()` is a no-op).
   - Hole-entry smoke and glow particle effects remain completely removed.
5. **Settings Mute Functionality**:
   - Muting via Settings -> Sounds immediately silences currently playing effects (`sfxSource.Stop()`).
   - Muted state persists across app launches via `PlayerPrefs` key `"SoundMuted"` (`0` = unmuted, `1` = muted).
   - Closing Settings restores gameplay smoothly.

### 4. Verification Suite & Results
- **Automated Verification Suite**: [AudioEffectsSetupAndVerificationSuite.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/AudioEffectsSetupAndVerificationSuite.cs)
- **Menu Items**:
  - `Tools/Slither Ants Jam/Audio/Configure SoundManager In Scene`
  - `Tools/Slither Ants Jam/Audio/Run Audio Effects Verification Suite`
- **Results**:
  - `Test 1: Audio Asset Integrity & Android Import Settings (2D Mono Preload)`: **PASS**
  - `Test 2: SoundManager Inspector Fields & Separate Volume Controls`: **PASS**
  - `Test 3: Normal Queue Selection Tap Sound (Single Play, Throttling & PlayOneShot)`: **PASS**
  - `Test 4: Linked Groups Tap (Single Tap Sound for Linked Group Selection)`: **PASS**
  - `Test 5: Tap Sound Constraints (Camera Drag, UI Click, Busy Queue, Auto Callback)`: **PASS**
  - `Test 6: Wrong-Queue Impact Audio (At Contact, Single Play, No Repeat on Return)`: **PASS**
  - `Test 7: Wrong-Hole Rim Impact Audio (At Outer Rim, Single Play, No Repeat on Return)`: **PASS**
  - `Test 8: Level Completion Fanfare (Waits for Queues, Coordinated, Guarded, Reset on Restart)`: **PASS**
  - `Test 9: Final Campaign Completion (Level 45 Fanfare with CampaignCompletedOverlay)`: **PASS**
  - `Test 10: Timer Zero Grace Period Level Completion Preserved`: **PASS**
  - `Test 11: Settings Mute Immediate Silence, Persistence & Reload With Sounds Off`: **PASS**
  - `Test 12: Hole Entry Silence Preserved (Zero Audio on Swallow, No Smoke/Glow Particles)`: **PASS**
  - `Test 13: Main Scene Configuration & Duplicate Cleanup`: **PASS**
  - **Total: 13 PASSED, 0 FAILED**

---

## 25. Android App Icon Configuration & Adaptive Layer Specification

### 1. Source Asset & Import Location
- **Source Image**: `C:\Users\ayush\Downloads\colorantsjam.png` (preserved untouched).
- **Asset Directory**: `Assets/Art/AppIcon/`
- **Generated Assets & Roles**:
  | Asset Name | Resolution | Format | Role & Android Usage |
  | :--- | :--- | :--- | :--- |
  | `colorantsjam.png` | 1254x1254 | RGBA32 | Original unmodified source with baked squircle bezel. |
  | `AppIcon_Legacy.png` | 1024x1024 | RGBA32 | Default app icon, Android Legacy icon slots, Android Round icon slots. |
  | `AppIcon_Adaptive_Background.png` | 1024x1024 | RGB24/RGBA | Adaptive Icon Layer 0 (Background): Full-bleed vibrant blue radial gradient, 100% opaque, NO baked rounded corners. |
  | `AppIcon_Adaptive_Foreground.png` | 1024x1024 | RGBA32 | Adaptive Icon Layer 1 (Foreground): Isolated purple ant & stadium hole centered within safe area (inner 66% circle), transparent background, NO outer bezel. |
- **Texture Importer Settings**:
  - `textureType`: `Default`
  - `isReadable`: `true`
  - `mipmapEnabled`: `false`
  - `alphaIsTransparency`: `true`
  - `wrapMode`: `Clamp`
  - `npotScale`: `None`
  - `maxTextureSize`: `2048`

### 2. Flattened Image Analysis & Adaptive Layer Requirements
- **Why `colorantsjam.png` is Flattened**:
  - The source PNG contains the 3D purple ant, stadium hole, light streaks, blue background, and purple squircle bezel all merged onto a single flat raster plane with transparent outer corners.
- **Why Flattened Images Cannot Serve Directly as Adaptive Foreground**:
  1. Android launchers apply dynamic device-specific masks (circles on Google Pixel, squircles on Samsung One UI, rounded squares on Xiaomi MIUI, teardrops on OnePlus). If a pre-baked squircle frame is placed in the foreground, circular launcher masks slice through the rectangular bezel, producing jagged double-border cutouts.
  2. Adaptive icons support parallax and dynamic physics effects on modern Android launchers. If the background and bezel remain baked into the foreground, the entire card translates over the background layer as an awkward floating tile instead of producing natural 3D parallax depth.
  3. The Android Adaptive Icon specification mandates that all critical foreground artwork reside strictly within the central safe area (66dp / 72dp diameter out of 108dp canvas, or the inner ~66% circle). The outer bezel of `colorantsjam.png` extends to 95% of the canvas, which is immediately cropped by round masks.
- **Separate Artwork Needed for Production**:
  - **Foreground (`ic_launcher_foreground.png`)**: Unflattened artwork or separate 3D render containing ONLY the purple ant, hole rim, interior pit, and glow streaks on a 100% transparent alpha channel, centered within the inner 66% safe circle with zero outer bezel.
  - **Background (`ic_launcher_background.png`)**: Full-bleed 108dp square texture without transparency or rounded corners.

### 3. Unity 6 Android Player Settings Configuration
- **Default App Icons**:
  - `BuildTargetGroup.Unknown`: `AppIcon_Legacy.png`
  - `BuildTargetGroup.Android`: `AppIcon_Legacy.png`
- **Adaptive Icons (API 26)**:
  - 6 slots configured: `432x432`, `324x324`, `216x216`, `162x162`, `108x108`, `81x81`
  - Layer 0 (Background): `AppIcon_Adaptive_Background.png`
  - Layer 1 (Foreground): `AppIcon_Adaptive_Foreground.png`
- **Round Icons (API 25)**:
  - 6 slots configured: `192x192`, `144x144`, `96x96`, `72x72`, `48x48`, `36x36` -> `AppIcon_Legacy.png`
- **Legacy Icons**:
  - 6 slots configured: `192x192`, `144x144`, `96x96`, `72x72`, `48x48`, `36x36` -> `AppIcon_Legacy.png`
- **Build Settings Preservation**:
  - `applicationIdentifier`: Preserved as `com.UnityTechnologies.com.unity.template.urpblank`.
  - Android Min SDK: `25`, Target SDK: `Automatic` (`0`).
  - Scripting Backend & Architectures intact.

### 4. Verification & Menu Utilities
- **Configurator & Verifier**: [AppIconConfigurator.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/AppIconConfigurator.cs)
- **Editor Menus**:
  - `Tools/Slither Ants Jam/Icons/Configure Android App Icons`
  - `Tools/Slither Ants Jam/Icons/Verify Android App Icons`
- **Automated Verification**: 18/18 slots verified, 12 adaptive layers verified, Application Identifier verified — **PASSED**.
- **Physical Device Verification**: Marked **untested** (requires physical Android device / launcher).

---

## 26. LEADER ANT HEAD-LOOK REACTION SYSTEM (COLOR ANTS JAM)

### 1. Trigger and Behavioral Architecture
- **Trigger Source**: Strictly hooked to `AntQueueController.OnQueueMoveStarted` when a queue begins moving to its destination rescue hole.
- **Ignored / Invalid Clicks**: Blocked shakes, locked queue taps, and invalid clicks do not fire `OnQueueMoveStarted`, guaranteeing that ignored clicks never trigger reactions.
- **Target Filtering**:
  - Only adjacent **idle leaders** (`isHead == true`, queue index 0) react.
  - Workers never react (workers do not receive the `LeaderHeadLook` component).
  - Distance check is calculated in **board-space** to the closest ant/segment of the moving queue, not just its leader, ensuring long multi-segment queues are detected accurately.
  - Configurable detection radius defaults to `1.5` grid cells (`EffectiveDetectionRadius = detectionRadius * BoardManager.CellSize`).
- **Autonomous Lifecycle State Machine**:
  - `Neutral`: Idle resting forward pose.
  - `TurningToward` (default `0.15s`): Smoothly slerps head yaw toward moving target in local pivot space using `Mathf.SmoothStep`.
  - `Watching` (default `0.40s`): Tracks moving ants using `Quaternion.RotateTowards` at `180°/sec`. Returns early if queue leaves range (`> 2.3 cells`) or stops.
  - `Returning` (default `0.25s`): Smoothly slerps back from current turn rotation directly to authored `initialLocalRotation`.
  - Cooldown (`0.60s`): Enforced after returning before another reaction can trigger.

### 2. Mesh Hierarchy & Dedicated Visual HeadLookPivot
- **Non-Destructive Mesh Splitting**:
  - The unrigged multi-part mesh assets `body.asset` and `accent.asset` were split along $Z = 0.30$:
    - `Chubby_Head_Body.asset`: Head sphere + mandibles (3,465 vertices).
    - `Chubby_Torso_Body.asset`: Thorax + abdomen (10,395 vertices).
    - `Chubby_Head_Accent.asset`: Antennae + eyebrows + mouth (2,212 vertices).
    - `Chubby_Torso_Accent.asset`: Legs + waist band (6,195 vertices).
  - Clean separation: 0 overlapping vertices, identical vertex positions, normals, and UVs.
- **Hierarchy Organization Across All 18 ChubbyAnt Prefabs**:
  - Dedicated visual pivot `HeadLookPivot` created at neck connection: `(0, 0.50, 0.15)`.
  - Children under `HeadLookPivot`: `head`, `head_accent`, `eyes`, `pupils`, `iris`, `glints`, `blush` with local offset `(0, -0.50, -0.15)` preserving 100.00% exact alignment with torso.
  - Children under root: `body` (torso), `accent` (legs), `rope`.
  - Only `HeadLookPivot` rotates. Whole ant root, queue root, abdomen, legs, and colliders never rotate.
  - Clamping: Head rotation strictly clamped to $\pm 25^\circ$, completely preventing backward twisting or visual detachment from the thorax.

### 3. Inspector Settings & Script Configuration
- **Script**: [LeaderHeadLook.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LeaderHeadLook.cs)
- **Parameters**:
  - `Detection Radius`: `1.50` grid cells.
  - `Max Head Turn Angle`: `25.0` degrees.
  - `Turn Toward Duration`: `0.15` seconds.
  - `Watch Duration`: `0.40` seconds.
  - `Return Duration`: `0.25` seconds.
  - `Reaction Cooldown`: `0.60` seconds.
  - `Tracking Angular Speed`: `180` degrees/second.
- **Interruption Handling**:
  - If the observing leader's own queue starts moving, `AntUnit.StartWalkAnimation` cancels the reaction immediately, instantly restoring authored forward orientation.
  - Simultaneous/linked queue starts in the same frame pick the closest moving queue with zero duplicate animations or jitter.
  - Clean reset on level restart, visual rebuild, and board state changes via `ResetToNeutral()`.

### 4. Verification Suite Results & Recordings
- **Verification Suite**: [LeaderHeadLookVerificationSuite.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/LeaderHeadLookVerificationSuite.cs)
- **Menu Item**: `Tools/Slither Ants Jam/Head Look/Run Head Look Verification Suite`
- **Automated Verification Results**: **9 / 9 TESTS PASSED (100%)**:
  1. `[PASS] Test 1`: Prefab Hierarchy Verification across all 18 ChubbyAnt prefabs (`HeadLookPivot` at neck with 7 head parts).
  2. `[PASS] Test 2`: Leader vs Worker setup binding (only leaders get `LeaderHeadLook`).
  3. `[PASS] Test 3`: Detection Radius & Long Queue Segment Detection (1.5 cells).
  4. `[PASS] Test 4`: Turn Direction (Left vs Right) and 25° Angular Clamping.
  5. `[PASS] Test 5`: Full Reaction State Lifecycle and Return to Neutral Pose ($0.0000^\circ$ residual offset).
  6. `[PASS] Test 6`: Reaction Cooldown Enforcement (0.60s).
  7. `[PASS] Test 7`: Interruption on Own Queue Movement (immediate cancel & restore).
  8. `[PASS] Test 8`: Simultaneous / Linked Queue Multi-Start Disambiguation (prefers nearest).
  9. `[PASS] Test 9`: Level Simulation & Gameplay Frame Recording (rendered 30 frames).
- **Gameplay Recordings**:
  - Animated WebP: `Assets/TestEvidence/HeadLookReaction/leader_head_look_reaction.webp`
  - Animated GIF: `Assets/TestEvidence/HeadLookReaction/leader_head_look_reaction.gif`
  - Frame sequence: `Assets/TestEvidence/HeadLookReaction/Frames/head_look_frame_000.png` through `029.png`



