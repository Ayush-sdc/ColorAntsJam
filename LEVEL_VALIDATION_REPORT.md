# Level Validation Report: All 45 Reference Levels

**Project:** Color Ants Jam  
**Engine:** Unity `6000.3.23f1` (URP `17.3.0`, Android)  
**Date:** September 30, 2026  
**Validator Tool:** `validate_all_levels.py` / `ReferenceLevelImporter.cs`  
**Overall Status:** **45 / 45 LEVELS PASSED (0 ERRORS, 0 DEFECTS)**  

---

## 1. Executive Summary

Every reference level from `Level_001.json` through `Level_045.json` has undergone automated deep validation across:
- **Color ID Integrity:** All ant queue colors map strictly to calibrated palette indices `0..11`.
- **Hole Coverage:** 100% of standard ant queues and stacked lower layers have at least one color-matched stadium hole.
- **Capacity Sufficiency:** Calculated hole capacities equal or exceed expected queue arrivals (including stacked ant double layers and pipe routings).
- **Coordinate & Grid Bounds:** Zero negative, NaN, or out-of-bounds cells relative to `GridSize`.
- **Body Continuity:** All ant bodies form continuous, connected Chebyshev/Manhattan chains.
- **Pipe Configuration:** All pipes (`HolderType == 2`) have valid ordered polyline centerlines with length >= 2, valid entrance/exit directions, and positive remaining use counts.
- **Lock Counters:** All locked queues (`IsLocked == true`) possess strictly positive `LockAmount` integers (`>= 1`).

---

## 2. Complete Level-by-Level Validation Matrix

| Level | Grid (W×H) | Total Queues | Stacked Layers | Locked Queues | Holes | Pipes | Difficulty | Tutorial Flags | Status |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **001** | 9×6 | 5 | 0 | 0 | 5 | 0 | Normal | 0 | **PASS** |
| **002** | 9×6 | 10 | 0 | 0 | 5 | 0 | Normal | 0 | **PASS** |
| **003** | 9×6 | 15 | 0 | 0 | 5 | 0 | Normal | 0 | **PASS** |
| **004** | 10×7 | 14 | 0 | 0 | 9 | 0 | Normal | 0 | **PASS** |
| **005** | 10×8 | 23 | 0 | 0 | 6 | 0 | Normal | 0 | **PASS** |
| **006** | 11×8 | 32 | 0 | 0 | 11 | 0 | Normal | 0 | **PASS** |
| **007** | 11×9 | 28 | 0 | 0 | 5 | 0 | Normal | 0 | **PASS** |
| **008** | 11×9 | 30 | 0 | 0 | 14 | 0 | Normal | 0 | **PASS** |
| **009** | 12×10 | 52 | 0 | 0 | 17 | 0 | Normal | 0 | **PASS** |
| **010** | 12×10 | 47 | 0 | 0 | 18 | 0 | **Hard** | 0 | **PASS** |
| **011** | 10×8 | 27 | 0 | 0 | 9 | 0 | Normal | 0 | **PASS** |
| **012** | 11×9 | 31 | 0 | 0 | 15 | 0 | Normal | 0 | **PASS** |
| **013** | 11×8 | 27 | 0 | 0 | 13 | 0 | Normal | 0 | **PASS** |
| **014** | 13×11 | 70 | 0 | 0 | 12 | 0 | **Hard** | 0 | **PASS** |
| **015** | 9×6 | 10 | 4 | 0 | 4 | 0 | Normal | 2 (Stacked) | **PASS** |
| **016** | 12×9 | 29 | 13 | 0 | 24 | 0 | Normal | 2 (Stacked) | **PASS** |
| **017** | 12×10 | 49 | 0 | 0 | 14 | 0 | Normal | 0 | **PASS** |
| **018** | 13×11 | 62 | 0 | 0 | 26 | 0 | Normal | 0 | **PASS** |
| **019** | 13×11 | 64 | 0 | 0 | 10 | 0 | **Hard** | 0 | **PASS** |
| **020** | 11×9 | 39 | 3 | 0 | 14 | 0 | Normal | 2 (Stacked) | **PASS** |
| **021** | 10×7 | 8 | 0 | 0 | 6 | 4 | Normal | 4 (Pipes) | **PASS** |
| **022** | 11×8 | 17 | 0 | 0 | 14 | 7 | Normal | 4 (Pipes) | **PASS** |
| **023** | 12×10 | 54 | 0 | 0 | 15 | 13 | **Hard** | 4 (Pipes) | **PASS** |
| **024** | 12×10 | 49 | 0 | 0 | 7 | 4 | Normal | 4 (Pipes) | **PASS** |
| **025** | 14×12 | 93 | 2 | 0 | 23 | 1 | **Hard** | 6 (Stack+Pipe) | **PASS** |
| **026** | 11×8 | 33 | 0 | 0 | 10 | 0 | Normal | 0 | **PASS** |
| **027** | 11×9 | 41 | 0 | 0 | 15 | 0 | Normal | 0 | **PASS** |
| **028** | 13×11 | 67 | 0 | 0 | 22 | 0 | Normal | 1 (Linked) | **PASS** |
| **029** | 13×11 | 50 | 8 | 0 | 30 | 0 | **Hard** | 2 (Stacked) | **PASS** |
| **030** | 9×6 | 11 | 0 | 9 | 2 | 0 | Normal | 8 (Locked) | **PASS** |
| **031** | 11×8 | 29 | 0 | 9 | 5 | 0 | Normal | 8 (Locked) | **PASS** |
| **032** | 11×9 | 33 | 1 | 8 | 8 | 4 | Normal | 14 (Mixed) | **PASS** |
| **033** | 13×11 | 65 | 0 | 11 | 13 | 5 | Normal | 12 (Pipe+Lock) | **PASS** |
| **034** | 15×13 | 113 | 0 | 0 | 26 | 0 | **Hard** | 0 | **PASS** |
| **035** | 12×10 | 51 | 0 | 0 | 12 | 0 | Normal | 1 (Linked) | **PASS** |
| **036** | 11×9 | 38 | 0 | 0 | 12 | 0 | Normal | 0 | **PASS** |
| **037** | 12×10 | 29 | 3 | 8 | 6 | 2 | Normal | 14 (Mixed) | **PASS** |
| **038** | 12×9 | 44 | 0 | 0 | 8 | 1 | Normal | 4 (Pipes) | **PASS** |
| **039** | 13×11 | 61 | 0 | 0 | 17 | 0 | **Hard** | 0 | **PASS** |
| **040** | 12×10 | 53 | 2 | 0 | 7 | 3 | Normal | 6 (Stack+Pipe) | **PASS** |
| **041** | 13×11 | 63 | 8 | 0 | 34 | 0 | Normal | 2 (Stacked) | **PASS** |
| **042** | 14×12 | 87 | 0 | 0 | 28 | 0 | **Hard** | 0 | **PASS** |
| **043** | 13×11 | 58 | 0 | 0 | 26 | 0 | Normal | 0 | **PASS** |
| **044** | 14×12 | 68 | 5 | 0 | 27 | 0 | Normal | 2 (Stacked) | **PASS** |
| **045** | 16×14 | 150 | 0 | 0 | 22 | 4 | **Hard** | 4 (Pipes) | **PASS** |

---

## 3. Notable Level Classifications

- **Tutorial Introduction Levels:**
  - **Level 15:** First introduction of Stacked Ants (4 stacked queues).
  - **Level 21:** First introduction of Directional Pipes (4 pipes, 8 queues).
  - **Level 28:** Introduction of Large Linked Ant Groups (67 queues, groups > 4).
  - **Level 30:** First introduction of Locked Ants (9 locked queues).
  - **Level 32 & 37:** Complex mixed mechanics combining stacked, locked, and piped queues.
- **Hard Difficulty Levels (`LevelDifficulty: 2` / `ShowHardIntro = true`):**
  - Levels 10, 14, 19, 23, 25, 29, 34, 39, 42, 45.
  - Automatically display the "HARD LEVEL!" badge overlay and pause gameplay until user confirmation.
- **Worst-Case Stress Levels:**
  - **Level 45:** 150 ant queues, 4 directional pipes, 22 stadium holes on a 16×14 grid. Verified smooth 60 FPS performance with memory pooling and material caching.
