# Level Validation Report: All 45 Reference Levels

**Project:** Color Ants Jam  
**Engine:** Unity `6000.3.23f1` (URP `17.3.0`, Target: Android)  
**Date:** September 30, 2026  
**Validator Tool:** `validate_all_levels.py` / `MechanicsUnitTests.cs`  
**Overall Status:** **45 / 45 LEVELS STRUCTURALLY VALIDATED & CAPACITY SUFFICIENT**  

> [!IMPORTANT]
> **Distinction Between Structural Validation and Level Solvability**:
> Passing structural validation and hole-capacity checks confirms that level assets deserialize cleanly, cells are continuous and within bounds, and hole capacities meet or exceed total required queues per color.
> **Structural validity does NOT prove gameplay solvability.** End-to-end solvability requires full PlayMode playthrough without deadlocks. Only Levels 1, 2, and 4 have verified end-to-end win completions.

---

## 1. Executive Summary

Every reference level from `Level_001.json` through `Level_045.json` has undergone automated validation:
- **Color ID Integrity:** All ant queue colors map strictly to calibrated palette indices `0..14`.
- **Hole Coverage:** 100% of standard ant queues and stacked lower layers have at least one color-matched stadium hole.
- **Capacity Sufficiency:** Calculated hole capacities equal or exceed expected queue arrivals (including stacked ant double layers and pipe routings).
- **Coordinate & Grid Bounds:** Zero negative, NaN, or out-of-bounds cells relative to `GridSize`.
- **Body Continuity:** All ant bodies form continuous, connected Manhattan chains.
- **Pipe Configuration:** All pipes (`HolderType == 2`) have valid ordered polyline centerlines with length >= 2, valid entrance/exit directions, and positive remaining use counts.
- **Lock Counters:** All locked queues (`IsLocked == true`) possess strictly positive `LockAmount` integers (`>= 1`).
- **Level Difficulty Metadata:** All 45 reference JSON files specify `LevelDifficulty: 0` (Normal). Hard-level intro overlays do NOT appear on normal levels.

---

## 2. Complete Level-by-Level Validation Matrix

| Level | Grid (W×H) | Total Queues | Stacked Layers | Locked Queues | Holes | Pipes | Difficulty in JSON | Verification Status |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **001** | 9×6 | 5 | 0 | 0 | 5 | 0 | Normal (0) | **Fully Completed (Manual PlayMode & Automated)** |
| **002** | 9×6 | 10 | 0 | 0 | 5 | 0 | Normal (0) | **Fully Completed (Manual PlayMode)** |
| **003** | 9×6 | 15 | 0 | 0 | 5 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **004** | 10×7 | 14 | 0 | 0 | 9 | 0 | Normal (0) | **Fully Completed (Manual PlayMode)** |
| **005** | 10×8 | 23 | 0 | 0 | 6 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **006** | 11×8 | 32 | 0 | 0 | 11 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **007** | 11×9 | 28 | 0 | 0 | 5 | 0 | Normal (0) | **Mechanics Exercised (Linked Groups, 20 Moves Validated)** |
| **008** | 11×9 | 30 | 0 | 0 | 14 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **009** | 12×10 | 52 | 0 | 0 | 17 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **010** | 12×10 | 47 | 0 | 0 | 18 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **011** | 10×8 | 27 | 0 | 0 | 9 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **012** | 11×9 | 31 | 0 | 0 | 15 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **013** | 11×8 | 27 | 0 | 0 | 13 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **014** | 13×11 | 70 | 0 | 0 | 12 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **015** | 9×6 | 10 | 4 | 0 | 4 | 0 | Normal (0) | **Mechanics Exercised (Stacked Reveal, Capacity Allocation)** |
| **016** | 12×9 | 29 | 13 | 0 | 24 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **017** | 12×10 | 49 | 0 | 0 | 14 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **018** | 13×11 | 62 | 0 | 0 | 26 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **019** | 13×11 | 64 | 0 | 0 | 10 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **020** | 11×9 | 39 | 3 | 0 | 14 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **021** | 10×7 | 8 | 0 | 0 | 6 | 4 | Normal (0) | **Mechanics Exercised (Straight Pipes, Direction Gating)** |
| **022** | 11×8 | 17 | 0 | 0 | 14 | 7 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **023** | 12×10 | 54 | 0 | 0 | 15 | 13 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **024** | 12×10 | 49 | 0 | 0 | 7 | 4 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **025** | 14×12 | 93 | 2 | 0 | 23 | 1 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **026** | 11×8 | 33 | 0 | 0 | 10 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **027** | 11×9 | 41 | 0 | 0 | 15 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **028** | 13×11 | 67 | 0 | 0 | 22 | 0 | Normal (0) | **Mechanics Exercised (Linked Groups, 17 Moves Validated)** |
| **029** | 13×11 | 50 | 8 | 0 | 30 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **030** | 9×6 | 11 | 0 | 9 | 2 | 0 | Normal (0) | **Mechanics Exercised (9 Locks, Decrement on Rescue)** |
| **031** | 11×8 | 29 | 0 | 9 | 5 | 0 | Normal (0) | **Mechanics Exercised (9 Locks, 13 Moves Validated)** |
| **032** | 11×9 | 33 | 1 | 8 | 8 | 4 | Normal (0) | **Mechanics Exercised (Stacked + Locked + Pipes + Linked)** |
| **033** | 13×11 | 65 | 0 | 11 | 13 | 5 | Normal (0) | **Mechanics Exercised (11 Locks + 5 Pipes + 9 Linked)** |
| **034** | 14×12 | 113 | 0 | 0 | 26 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **035** | 12×10 | 51 | 0 | 0 | 12 | 0 | Normal (0) | **Mechanics Exercised (13 Linked, 25 Moves Validated)** |
| **036** | 11×8 | 38 | 0 | 0 | 12 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **037** | 11×9 | 29 | 3 | 8 | 6 | 2 | Normal (0) | **Mechanics Exercised (3 Stacked + 8 Locked + 2 Pipes)** |
| **038** | 11×9 | 44 | 0 | 0 | 8 | 1 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **039** | 13×10 | 61 | 0 | 0 | 17 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **040** | 12×10 | 53 | 2 | 0 | 7 | 3 | Normal (0) | **Mechanics Exercised (2 Stacked + 3 Pipes + 5 Linked)** |
| **041** | 13×11 | 63 | 8 | 0 | 34 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **042** | 14×12 | 87 | 0 | 0 | 28 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **043** | 13×10 | 58 | 0 | 0 | 26 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **044** | 14×11 | 68 | 5 | 0 | 27 | 0 | Normal (0) | Structural & Capacity PASS (Loaded) |
| **045** | 15×13 | 150 | 0 | 0 | 22 | 4 | Normal (0) | **Profiled Benchmark (150 Queues, 22 Holes, 4 Pipes, 35 Linked)** |

---

## 3. Specifically Tested Levels Record

| Level | Specific Mechanics Tested | Test Method | Outcome / Notes |
| :---: | :--- | :---: | :--- |
| **Level 1** | Normal ant slithering, trail tracking, hole swallowing, win condition | Manual PlayMode & Automated | Fully completed; verified win panel |
| **Level 2** | Multi-queue sequence, camera framing | Manual PlayMode | Fully completed |
| **Level 4** | 14 queues, 9 holes, hole completion & multi-cell release | Manual PlayMode | Fully completed |
| **Level 7** | 26 linked queues (groups up to 5) | Automated Move Validation | 20 initial valid moves; simultaneous launch |
| **Level 15** | 4 stacked pairs (8 queues) | Automated & Integration Tests | Color-grouped capacity satisfied; lower revealed |
| **Level 21** | 4 straight pipes | Automated & Integration Tests | Directional gating and pipe trajectory verified |
| **Level 28** | 67 queues, 22 holes, 19 linked queues | Automated Setup & Audit | 17 initial valid moves; multi-hole distribution |
| **Level 30** | 9 locked queues | Automated & Integration Tests | 11 initial valid moves; unlock at 0 |
| **Level 31** | 9 locked queues, 5 holes | Automated Setup & Audit | 13 initial valid moves |
| **Level 32** | Stacked (1) + Locked (8) + Pipes (4) + Linked (3) | Automated Setup & Audit | 8 initial valid moves; multi-mechanic coexistence |
| **Level 33** | Locked (11) + Pipes (5) + Linked (9) | Automated Setup & Audit | 13 initial valid moves |
| **Level 35** | 51 queues, 13 linked | Automated Setup & Audit | 25 initial valid moves |
| **Level 37** | Stacked (3) + Locked (8) + Pipes (2) + Linked (5) | Automated Setup & Audit | 4 initial valid moves |
| **Level 40** | Stacked (2) + Pipes (3) + Linked (5) | Automated Setup & Audit | 8 initial valid moves |
| **Level 45** | 150 queues, 22 holes, 4 pipes, 35 linked queues | Profiler & Setup Audit | 33 initial valid moves; profiled 5 restart cycles |
