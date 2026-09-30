# Technical Context Report: Color Ants Jam (Unity Android)

**Document Type:** Authoritative Technical Context & System Specification  
**Target Audience:** Autonomous Coding Agents & Core Engineering Team  
**Game Title:** Color Ants Jam (inspired by *Color Slither Jam*)  
**Report Generated:** September 30, 2026  
**Project Workspace:** `c:\Users\ayush\Desktop\Unity Projects\Slither Ants Jam`  

---

## 1. Project Information & Metadata

| Attribute | Project Configuration | Verification Source |
| :--- | :--- | :--- |
| **Unity Version** | `6000.3.23f1` (Unity 6, revision `09d2ecc7fb28`) | [ProjectVersion.txt](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectVersion.txt#L1-L2) |
| **Render Pipeline** | Universal Render Pipeline (URP `17.3.0`) | [manifest.json](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Packages/manifest.json#L12), [GraphicsSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/GraphicsSettings.asset#L40) |
| **Active URP Asset** | `Assets/Settings/PC_RPAsset.asset` (GUID `4b83569d67af61e458304325a23e5dfd`) | [QualitySettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/QualitySettings.asset) |
| **Target Platform** | Android (API Level Min: 25, Target: Automatic/0) | [ProjectSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectSettings.asset#L183-L184) |
| **Application Identifier** | `com.UnityTechnologies.com.unity.template.urpblank` *(Default template ID - requires update)* | [ProjectSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectSettings.asset#L173) |
| **Screen Orientation** | Auto-Rotation (Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight) | [ProjectSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectSettings.asset#L11-L66) |
| **Color Space** | Linear (`m_ActiveColorSpace: 1`) | [ProjectSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/ProjectSettings.asset#L50) |
| **Primary Scene** | `Assets/Scenes/main.unity` (GUID `99c9720ab356a0642a771bea13969a05`) | [EditorBuildSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/EditorBuildSettings.asset#L8-L10) |
| **Build Scenes** | Index 0: `Assets/Scenes/main.unity` (Only scene in build) | [EditorBuildSettings.asset](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/ProjectSettings/EditorBuildSettings.asset#L8-L10) |

### Key Packages & Plugins
- **`com.unity.render-pipelines.universal` (17.3.0):** Universal Render Pipeline core shaders, post-processing, and renderer.
- **`com.unity.inputsystem` (1.20.0):** New Input System package (though runtime code primarily processes touch/mouse through `Input.GetTouch` / `Input.GetMouseButtonDown` in [PlayerInputHandler.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs#L127-L144)).
- **`com.unity.cinemachine` (3.1.7):** Cinemachine v3 package for virtual camera control ([BoardCameraController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Camera/BoardCameraController.cs#L4)).
- **`com.unity.ugui` (2.0.0):** Standard Unity UI system.
- **Demigiant DOTween:** Installed in `Assets/Plugins/Demigiant/DOTween` (used for ant walk cycle bobbing, blocked shakes, hole swallowing, UI transitions).
- **Toony Colors Pro 2:** Installed in `Assets/JMO Assets/Toony Colors Pro` (hybrid toon shaders, shader generator).

---

## 2. Inventory of Relevant Scripts and Prefabs

### A. Core Gameplay Scripts
- [GameManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/GameManager.cs): Central game state machine (`NotStarted`, `Playing`, `LevelWon`, `LevelLost`), countdown timer, player lives (3 hearts), win/loss evaluation, deadlock detection.
- [BoardManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/BoardManager.cs): Grid lifecycle, board construction, multi-cell hole tracking (`holesMap`), active queue tracking, forward movement validation, atomic linked group movement, concurrent move limiting (max 4), completed hole unmapping.
- [AntQueueController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueController.cs): Ant queue chain movement, trail history recording (`trailHistory`), follower unit distance sampling, hole entry descent, individual ant swallow triggering, queue completion event dispatch.
- [AntUnit.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntUnit.cs): Individual ant segment representation. Leader (index 0) uses `ChubbyAntColors`, followers (index >= 1) use `WorkerAnt`. Controls procedural walk wobbling (`StartWalkAnimation`).
- [HoleController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/HoleController.cs): Procedural stadium mesh generation (`SlotRim`, `SlotInnerBevel`, `SlotPit`), queue capacity badge text, inbound slot reservations, ant swallow VFX (`ant_dis`), queue-level completion, disappearance animation, footprint release.
- [AntPathfinder.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Board/AntPathfinder.cs): 4-directional BFS pathfinding algorithm (currently bypassed by default because `useAutomaticPathfinding = false`).
- [PlayerInputHandler.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/PlayerInputHandler.cs): Touch and mouse raycasting against ant colliders or ground plane, drag gesture detection for camera panning, tap trigger for queue movement.
- [ReferenceColorMapper.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/ReferenceColorMapper.cs): Authoritative 0..11 raw integer ColorID mapper to distinct `AntColor` enum values.
- [AntColor.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/AntColor.cs): Calibrated sRGB color palette, shared material caches for ant bodies, holder rims, and bevels.
- [AntVisualConfig.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntVisualConfig.cs): ScriptableObject defining leader/worker prefabs, scales (0.37 leader, 0.28 worker), visual lengths, and bob heights.
- [LinkedAntsRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/LinkedAntsRopeVisual.cs): Instantiates and stretches horizontal `GoldenRope` mesh between linked ant queue leaders.
- [AntQueueRopeVisual.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Ants/AntQueueRopeVisual.cs): Parallel rope visual controller.
- [BoardCameraController.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Camera/BoardCameraController.cs): Fixed baseline camera framing (78° FOV, 88° pitch, 13.04m height) with bounded manual drag-panning.
- [GardenBackgroundManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Environment/GardenBackgroundManager.cs): Procedural garden environment surrounding the board (lawn, stone patio slab, curb, bushes, flowers, pebbles).
- [GardenEnvironmentPalette.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Environment/GardenEnvironmentPalette.cs): Garden material factory and color tokens.
- [SoundManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Core/SoundManager.cs): Procedural audio synthesizer creating 6 SFX clips (`tap`, `hop`, `swallow`, `blocked`, `win`, `lose`) via mathematical waveforms.

### B. UI Scripts
- [GameUIManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/UI/GameUIManager.cs): Manages HUD timer, level number, 3 heart icons, settings modal (sound/music toggles), win popup, fail popup animation, and Level 1 tutorial overlay.
- [FailScreenSetup.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/FailScreenSetup.cs): Editor setup utility constructing the polished fail screen popup hierarchy.

### C. Level Data & Progression Scripts
- [Level.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/Level.cs): ScriptableObject schema representing a level (metadata, grid dimensions, disabled cells, cell info, holes, ant queues).
- [AntQueueData.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/AntQueueData.cs): Serialized data classes `AntQueueData`, `HoleData`, and `CellInfo`.
- [LevelDatabase.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/LevelDatabase.cs): ScriptableObject containing ordered array of `Level` assets.
- [LevelManager.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/LevelManager.cs): Progression manager reading/writing `PlayerPrefs` key `"CurrentLevel"`, loading level assets, handling next/previous level.
- [ReferenceLevelRuntimeAdapter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/LevelSystem/ReferenceLevelRuntimeAdapter.cs): Runtime translator converting raw immutable reference levels (`HolderType 0` holes, `HolderType 1` queues) into playable board objects with color-grouped hole capacity.
- [ReferenceLevelImporter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/ReferenceLevelImporter.cs): Editor menu tool importing `Level_001.json` through `Level_045.json` into ScriptableObject assets while preserving difficulty and metadata.

### D. Key Prefab Locations
- Ant Queues & Units:
  - `Assets/Prefabs/AntQueue.prefab` (Root queue container)
  - `Assets/Prefabs/AntUnit.prefab` (Logical unit segment container)
  - `Assets/WorkerAnt/Prefabs/WorkerAnt_*.prefab` (Worker follower models across 8 colors + rope variants)
  - `Assets/Resources/Ants/Prefabs/WorkerAnt.prefab`, `ArcherAnt.prefab`, `FireAnt.prefab`
- Holes & Holders:
  - `Assets/Prefabs/Hole_Stadium.prefab`, `Hole.prefab`, `Hole_new.prefab`, `HOLEE.prefab`
- Rope Connectors:
  - `Assets/GoldenRope/Prefabs/GoldenRope_2Cells.prefab`, `3Cells.prefab`, `4Cells.prefab`, `5Cells.prefab`
  - `Assets/Resources/GoldenRope/GoldenRope_*.prefab`
- VFX:
  - `Assets/Prefabs/VFX/ant_dis.prefab` (Particle burst when ant enters hole)
  - `Assets/Prefabs/VFX/antarrow.prefab` (Directional indicator arrow)
- UI:
  - `Assets/UI/FailScreen/LevelFailed_Title.png`, `LevelFailed_Heart.png`, `LevelFailed_RestartButton.png`
  - Root texture assets: `Hard_level.png`, `Hardleveloverlay.png`, `level failed.png`, `Linked_ants.png`

---

## 3. Strict Evidence & Confidence Standard

Every finding in this report is categorized into one of five standard classifications:
1. **[IMPLEMENTED]**: C# scripts, game systems, and visual pipelines created and functional in the project.
2. **[VERIFIED BY AUTOMATED TESTS]**: Validated by unit/integration tests running in Unity Test Framework (`TestResults.xml`, 23/23 passing) or Python validation suite (`validate_all_levels.py`, 45/45 passing).
3. **[VERIFIED MANUALLY]**: Audited via Editor play-mode, scene inspection, and frame profiling.
4. **[ASSUMED REFERENCE BEHAVIOR]**: Mechanics assumptions (e.g., PipeCount conversion, permitted entrance directions) documented as hypotheses awaiting reference video confirmation.
5. **[UNVERIFIED / FAILING]**: Areas without hardware validation (e.g., physical Android target performance) or lacking end-to-end full play-through proof.

---

## 4. Current Feature Implementation & Verification Matrix (Sept 2026)

| Feature | Implementation Status | Verification Classification | Verification Detail |
| :--- | :--- | :--- | :--- |
| **Locked Ants** | Implemented | **VERIFIED BY AUTOMATED TESTS** | 5 tests: Tap gating, decrements on completed rescue, +2 on linked pair rescue, rejection of in-progress/canceled moves, unlock at 0 and restore on restart. |
| **Linked Groups** | Implemented | **VERIFIED BY AUTOMATED TESTS** | 7 tests: Group sizes 2, 5, 6, 7; atomic pre-validation (all-or-none movement); no reservations on block; simultaneous movement; unequal queue & route lengths; interruption cleanup. |
| **Stacked Ants** | Implemented | **VERIFIED BY AUTOMATED TESTS** | 4 tests: Blocked tap does not reveal; accepted upper move reveals lower layer at original footprint; lower cannot enter departing upper's tile; independent rescue with lock decrements & hole capacity. |
| **Directional Pipes** | Implemented | **VERIFIED BY AUTOMATED TESTS** & **ASSUMED REFERENCE BEHAVIOR** | 4 tests: Queue traversal through straight/bent pipes, entry rule gating, concurrent reservation rejection, exhaustion at 0. *Note: PipeCount conversion and entrance direction are documented reference assumptions.* |
| **Hole Capacity** | Implemented | **VERIFIED BY AUTOMATED TESTS** | Color-grouped capacity sufficiency across all 45 levels (0 deficits); automated JSON round-trip test; hard-level overlay metadata test. |
| **JSON Pipeline** | Implemented | **VERIFIED BY AUTOMATED TESTS** | 45-level import & export with 0 errors via `ReferenceLevelImporter.cs` and `validate_all_levels.py`. Preserves `snakes` compatibility key and ordered cell arrays. |
| **Scene Sanitization** | Implemented | **VERIFIED MANUALLY** | Sanitized `main.unity` from 66.7 MB to 5.8 MB, purged 546 baked runtime clones. |
| **Level Loading Matrix**| Implemented | **VERIFIED BY AUTOMATED TESTS** | Levels 1, 7, 15, 21, 28, 30–33, 35, 37, 40, and 45 audited and loaded through `BoardManager` with initial valid moves recorded. |
| **Performance Profiling**| Implemented | **VERIFIED MANUALLY** (Windows Editor) | Level 45 loaded in 832.2 ms (72.4 MB heap delta); 5 restart cycles averaged 1,093.5 ms (+124 KB net heap post-GC). |
| **Android Performance** | Pending Device | **UNVERIFIED** | Android performance unverified (tested on Windows Editor; physical Android device unavailable). Unsupported "60 FPS" claims removed. |
