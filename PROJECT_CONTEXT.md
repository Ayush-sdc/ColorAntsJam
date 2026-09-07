# Slither Ants Jam - Project Context & Session Handover

**Last Updated:** September 8, 2026 (Session Handover)  
**Active Branch:** `main`  
**Unity Version:** 2022.3 LTS (Universal Render Pipeline - URP)  
**Primary Scene:** `Assets/Scenes/main.unity`  

---

## 1. Project Overview & Core Mechanics

**Slither Ants Jam** is a 3D puzzle game where colored ant snakes ("caterpillar" queues) navigate grid paths to match and enter designated target stadium holes.

### Key Gameplay Loop
1. Ants queue up on the board with specific body segments and colors.
2. Player guides / directs ants along valid paths across the grid.
3. Ants slither into color-matched holes (`HoleController`), decrementing the hole's required count badge until full (`remainingCount == 0`), triggering completion juice and particle VFX.

---

## 2. Core Architecture & Key Scripts

| Script Path | Purpose |
| :--- | :--- |
| [`Assets/Scripts/Board/HoleController.cs`](file:///Assets/Scripts/Board/HoleController.cs) | Procedural 3D stadium hole generator (`SlotRim`, `SlotPit`, `Capsule`, `SlotCounterBadge`, `BadgeMesh`, `BadgeCountText`), ant swallowing animation (`DOTween`), particle VFX (`ant_dis`), audio cues, dynamic capacity display. |
| [`Assets/Scripts/Ants/AntQueueController.cs`](file:///Assets/Scripts/Ants/AntQueueController.cs) | Multi-segment snake movement controller, path following, slithering wave animation, queue state management. |
| [`Assets/Scripts/Ants/AntUnit.cs`](file:///Assets/Scripts/Ants/AntUnit.cs) | Individual ant segment rendering, pupil/eye orientation, color assignments. |
| [`Assets/Scripts/Board/BoardManager.cs`](file:///Assets/Scripts/Board/BoardManager.cs) | Grid cell-to-world positioning, hole registration and mapping (`holesMap`), ant spawning. |
| [`Assets/Scripts/LevelSystem/LevelManager.cs`](file:///Assets/Scripts/LevelSystem/LevelManager.cs) | Level loading, progression, database integration. |
| [`Assets/Scripts/LevelSystem/Level.cs`](file:///Assets/Scripts/LevelSystem/Level.cs) | Level data container (grid width/height, cell size, hole positions, queue layouts). |
| [`Assets/Scripts/Environment/GardenBackgroundManager.cs`](file:///Assets/Scripts/Environment/GardenBackgroundManager.cs) | Procedural garden environment (stone patio slab, curb rim, bushes, pebbles). |
| [`Assets/Scripts/Core/AntColorPalette.cs`](file:///Assets/Scripts/Core/AntColorPalette.cs) | Standardized color palette mappings for all ant and hole types. |
| [`Assets/Scripts/Editor/ModelCreationUtility.cs`](file:///Assets/Scripts/Editor/ModelCreationUtility.cs) | Editor utilities to refresh and rebuild scene visuals and hole models. |

---

## 3. Level 1 Active Holes Configuration

The 5 holes in Level 1 (`Assets/Scenes/main.unity`) are configured with standardized transform values:

| Hole Name | Color | Grid Coord | World Position (X, Y, Z) | Capacity | `isRightSide` | Ant Entry Side | Badge Side | `BadgeCountText` Local Pos (X, Y, Z) | `BadgeCountText` Scale |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **`Hole_Red_1_0`** | **Red** | `(1, 0)` | `(-3.5, 0.01, -2.5)` | **11** | `false` | **Right** | **Left** | `(-0.66, 0.17, 0.0)` | `(1.0, 1.0, 1.0)` |
| **`Hole_Brown_6_1`** | **Brown** | `(6, 1)` | `(2.5, 0.01, -1.5)` | **3** | `true` | **Left** | **Right** | `(0.66, 0.17, 0.0)` | `(1.0, 1.0, 1.0)` |
| **`Hole_Blue_6_2`** | **Blue** | `(6, 2)` | `(2.5, 0.01, -0.5)` | **5** | `true` | **Left** | **Right** | `(0.66, 0.17, 0.0)` | `(1.0, 1.0, 1.0)` |
| **`Hole_Green_6_3`** | **Green** | `(6, 3)` | `(2.5, 0.01, 0.5)` | **7** | `true` | **Left** | **Right** | `(0.66, 0.17, 0.0)` | `(1.0, 1.0, 1.0)` |
| **`Hole_Yellow_6_4`** | **Yellow** | `(6, 4)` | `(2.5, 0.01, 1.5)` | **9** | `true` | **Left** | **Right** | `(0.66, 0.17, 0.0)` | `(1.0, 1.0, 1.0)` |

### Hierarchy Structure for Each Hole:
```text
Hole_<Color>_<X>_<Y>
  └── HoleSlotRoot
        ├── SlotRim            (Procedural URP Lit 3D tubular glossy stadium rim)
        ├── SlotPit            (Recessed pure black cavity floor)
        │     └── Capsule      (Inner black pit shadow sprite)
        └── SlotCounterBadge   (Counter tab container)
              ├── BadgeMesh    (Glossy colored rounded tab background)
              └── BadgeCountText (Bold white 3D TextMesh counter)
```

---

## 4. Key Work Completed in Recent Sessions

1. **Procedural Stadium Visuals**:
   - Fully continuous 3D tubular capsule rim with glossiness and URP Lit materials.
   - Recessed pure black pit cavity floor (`SlotPit`).
   - Integrated `Capsule` shadow depth sprite.
   - Glossy colored badge background mesh with rounded fillets (`BadgeMesh`).
2. **BadgeCountText Normalization**:
   - Elevated text Y to `0.17` and Rotation to `Quaternion.Euler(90, 0, 0)` (Rotation Z = 0) facing top-down camera cleanly.
   - Normalized all scale values to `(1, 1, 1)`.
   - Cleaned up `HoleController.cs -> UpdateBadgeDisplay()` so it updates text count and character size without clobbering custom transform coordinates.
3. **Safety & Stability**:
   - Added `IsPrefabAsset()` guards to prevent accidental serialization leaks in prefab stage.
   - Verified 0 compile errors in `Editor.log`.
   - All changes committed to Git.

---

## 5. Next Steps & Recommended Backlog

1. **Ant Queuing & Swallowing Flow**:
   - Continue testing `AntQueueController` spline motion and head-first entry into holes.
2. **Additional Levels & Testing**:
   - Test level generation for multi-row holes (`parallelRows >= 2`) and vertical orientations.
3. **UI & Audio Juice**:
   - Win/loss dialogs and audio triggers in `SoundManager`.
