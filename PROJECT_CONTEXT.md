# Slither Ants Jam - Project Context & Session Handover

**Last Updated:** September 12, 2026 (Authoritative Reference Pipeline, Color Consistency & Holder Removal)  
**Active Branch:** `main`  
**Unity Version:** 6000.3.23f1 (Universal Render Pipeline - URP)  
**Primary Scene:** `Assets/Scenes/main.unity`  

---

## 1. Project Overview & Core Gameplay Loop

**Slither Ants Jam** is a top-down 3D grid puzzle game built in Unity. Players tap / click colored ant queues ("caterpillar" slither chains) to route them into color-matched stadium holders around obstacles and other ant bodies.

### Core Loop
1. **Board Rebuild**: `LevelManager` loads the selected `Level` asset on start or on level change. `BoardManager.SetupBoard()` clears previous runtime objects and instantiates holders, queues, and grid dots.
2. **Pathfinding & Input**: `PlayerInputHandler` detects raycasts on ant queues / grid cells. `BoardManager.TryStartMove()` uses `AntPathfinder.cs` (BFS grid search) to validate that an unblocked path exists to a matching holder entrance.
3. **Slithering & Hole Entry**: Ants move segment-by-segment along the path (`AntQueueController`), slithering toward the holder entrance. The target `DestinationHole` is cached on the queue upon movement start.
4. **Queue Swallowing & Capacity**: As ant units reach the holder, `HoleController` swallows units with DOTween scaling and burst VFX (`ant_dis`). When the entire queue finishes entering, `BoardManager.OnQueueCompleted(queue)` notifies `targetHole.OnQueueSwallowed()`, decrementing its queue counter.
5. **Holder Completion & Permanent Grid Release**: When a holder's counter reaches 0:
   - The holder enters `IsCompleted = true`.
   - `BoardManager.RemoveCompletedHole(hole)` unregisters ALL cells across the holder's entire multi-cell footprint (`OccupiedCells`) from `holesMap`.
   - Grid blocking is released immediately so remaining queues can path through that space.
   - The holder shrinks cleanly (`0.20s`, `Ease.InBack`) and its GameObject (including `SlotRim`, `SlotInnerBevel`, `SlotPit`, and badge) is destroyed.
6. **Win / Loss Evaluation**:
   - **Win**: All ant queues on the board have been cleared (`BoardManager.ActiveQueues.Count == 0`). Win condition does not depend on destroyed `HoleController` GameObjects.
   - **Loss (Deadlock / Timeout)**: Timer reaches 0 or all remaining ant queues are deadlocked with no valid path to their holders.

---

## 2. Level Architecture & Database System

The project uses `Assets/Data/Levels/LevelDatabase.asset` as the authoritative level database containing all 45 reference levels (`Level_001` through `Level_045`).

```text
                              LevelManager (GameController)
                                            │
                                            ▼
                           Assets/Data/Levels/LevelDatabase.asset
                           (45 Reference Levels: Level_001 - Level_045)
                                            │
                                            ▼
                           ReferenceLevelRuntimeAdapter.cs
                                            │
                    ┌───────────────────────┴───────────────────────┐
                    ▼                                               ▼
      HoleController (Holders)                        AntQueueController (Queues)
   - Multi-cell footprints                         - Ordered body coordinates
   - Raw ColorID mapping                           - Matching raw ColorID
   - Initial count = matching queues               - Head-to-tail slither movement
   - Single-queue visual override                  - DestinationHole caching
```

---

## 3. Authoritative Raw Color Pipeline

Raw ColorIDs from `.asset` files are the single source of truth mapped via `ReferenceColorMapper.cs`:

| Raw ColorID | Distinct Color Name | `AntColor` Enum | Notes |
| :---: | :---: | :---: | :--- |
| **0** | Sky Blue | `AntColor.SkyBlue` | Light sky blue `#33B3FF` |
| **1** | Lime Green | `AntColor.Lime` | Bright vibrant lime `#7BD926` |
| **2** | Yellow | `AntColor.Yellow` | Warm candy yellow `#FFBF0D` |
| **3** | Purple | `AntColor.Purple` | Rich vivid purple `#9438E0` |
| **4** | Orange | `AntColor.Orange` | Crisp citrus orange `#FF800D` |
| **5** | Pink | `AntColor.Pink` | Punchy candy magenta/pink `#FA59BF` |
| **6** | Indigo | `AntColor.Indigo` | Deep royal blue `#2E47D9` |
| **7** | Green | `AntColor.Green` | Forest/leaf green `#1FA638` |
| **8** | Brown | `AntColor.Brown` | Milk chocolate brown `#8C522B` |
| **9** | White | `AntColor.White` | Clean warm white `#EBEBEB` |
| **10** | Red | `AntColor.Red` | Coral ruby red `#F23D42` |
| **11** | Turquoise | `AntColor.Turquoise` | Aqua/cyan `#1AE0C7` |
| **12+** | UNVERIFIED | White fallback | Default fallback without guessing |

Both Ants (`AntUnit` / `AntColorPalette.GetMaterial`) and Holder rims (`SlotRim` / `AntColorPalette.GetHoleMaterial`) derive their exact base RGB values from this pipeline (verified 100% RGB parity).

---

## 4. Holder Visuals & Single-Queue Overrides

For runtime holders whose initial queue count is exactly 1 (e.g. all 5 Level 1 holders), `ApplySingleQueueHolderVisualOverride()` is applied:
- `SlotRim`: LocalScale X = `1.9205`
- `SlotInnerBevel`: LocalScale X = `2.3861`, dark shaded bevel (`Color.Lerp(baseColor, black, 0.38f)`)
- `SlotPit`: LocalScale X = `2.6535`, recessed dark pit floor
- `SlotCounterBadge`: LocalPosition `(0.35, 0.0, 0.0)`
  - `BadgeMesh`: LocalPosition `(0.25, 0.0, -0.25)`
  - `BadgeCountText`: LocalPosition `(0.25, 0.22, -0.25)`

---

## 5. Holder Removal & Footprint Grid Release System

When a holder counter reaches 0 after queue completion:
1. **Execution Sequence**:
   - Final queue completely finishes entering -> `BoardManager.OnQueueCompleted(queue)`.
   - Cached `queue.DestinationHole` is notified via `hole.OnQueueSwallowed()`.
   - `remainingCount` reaches 0 -> marked `isCompleted = true`.
   - `BoardManager.RemoveCompletedHole(hole)` is invoked.
   - For every cell in `hole.OccupiedCells`, if mapped to this hole, it is removed from `holesMap`.
   - Defensive sweep clears any lingering mappings.
   - `activeHoles.Remove(hole)` updates active tracking; completion tracked via `completedHoleCount` and `completedHoleIds` (stable IDs, not destroyed references).
   - Board state refreshed (`OnBoardStateChanged?.Invoke()`).
   - `PlayCompletedEffectAndDisappear()` disables colliders, logs diagnostic entry, animates scale to 0 (`0.20s`, `Ease.InBack`), and destroys the GameObject.
2. **Pathfinding & Occupancy Safety**:
   - `AntPathfinder` evaluates dynamic occupancy from `BoardManager.HolesMap`. Because completed holders are unregistered immediately, released cells naturally become empty and walkable.
   - `BoardManager.GetHoleAt(coord)` safely purges stale or completed mappings:
     ```csharp
     if (!holesMap.TryGetValue(coord, out var hole)) return null;
     if (hole == null || hole.IsCompleted) { holesMap.Remove(coord); return null; }
     return hole;
     ```
3. **Diagnostic Log Format**:
   ```text
   [HoleCompleted]
   Color=Red
   RemainingCount=0
   OccupiedCells=[(1,0),(0,0)]
   ReleasedCells=2
   RemovedFromHoleMap=True
   GameObjectRemoval=True
   ```

---

## 6. Key Scripts & Systems

| Script | Location | Responsibility |
| :--- | :--- | :--- |
| **`ReferenceColorMapper.cs`** | `Assets/Scripts/Core/` | Authoritative 0..11 raw ColorID mapping to distinct `AntColor` values. |
| **`AntColor.cs` / `Palette`** | `Assets/Scripts/Core/` | Color enums, calibrated sRGB color palette, shared materials for ants and holder rims. |
| **`HoleController.cs`** | `Assets/Scripts/Board/` | Procedural stadium mesh geometry, single-queue overrides, badge text, queue-level swallow counter, disappearance animation. |
| **`BoardManager.cs`** | `Assets/Scripts/Board/` | Runtime grid lifecycle, `holesMap` occupancy, `activeHoles` & `completedHoleIds` tracking, `RemoveCompletedHole()`, queue completion handling. |
| **`AntQueueController.cs`** | `Assets/Scripts/Ants/` | Slither chain movement, path traversal, `DestinationHole` caching and dispatch. |
| **`AntPathfinder.cs`** | `Assets/Scripts/Board/` | Deterministic BFS pathfinding respecting active queues, matching non-full holders, and released footprint cells. |
| **`GameManager.cs`** | `Assets/Scripts/Core/` | Game state machine (`NotStarted`, `Playing`, `LevelWon`, `LevelLost`), deadlock detection, win condition evaluation (`activeQueues.Count == 0`). |
| **`TestLevels1To5Validation.cs`** | `Assets/Scripts/Editor/` | Menu tools: `Validate Levels 1 to 5`, `Validate Color Consistency Levels 1 to 5`, `Validate Holder Removal Level 1`. |

---

## 7. Automated Test Suites (`Assets/Tests/EditMode/`)

- **`ReferenceLevels1To5Tests.cs`**:
  - `Test01`: Verifies LevelDatabase contains all reference levels with exact grid dimensions.
  - `Test02`: Verifies all 12 distinct colors are preserved without collapse.
  - `Test03`: Verifies holder capacity matches playable queues count.
  - `Test04`: Verifies holder footprints and dimensions are calculated accurately.
  - `Test05`: Verifies centered holder placement and badge initialization.
  - `Test06`: Verifies single-queue holder visual overrides applied on exact transforms.
  - `Test07`: Verifies 100% RGB base color consistency between ant queues and holder rims.
  - `Test08`: Verifies Level 1 holder removal, full 2x1 footprint release, counter zeroing, unmapping from `holesMap`, `GetHoleAt` null safety, and win condition.
- **`LevelDatabaseTests.cs`**:
  - Verifies level database counts and asset validity.
