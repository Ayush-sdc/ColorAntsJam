# Color Ants Jam: JSON Schema & Conversion Specification

**Version:** 1.0.0  
**Target:** Unity `6000.3.23f1` / JSON Reference Level Pipeline  
**Storage Directory:** `Assets/Data/ReferenceLevelJSON/`  
**Associated ScriptableObjects:** `Assets/Data/ReferenceLevels/`  
**Tooling:** [ReferenceLevelImporter.cs](file:///c:/Users/ayush/Desktop/Unity%20Projects/Slither%20Ants%20Jam/Assets/Scripts/Editor/ReferenceLevelImporter.cs)  

---

## 1. Top-Level JSON Document Schema

Each level file (`Level_001.json` through `Level_045.json`) contains a single root JSON object:

```json
{
  "snakes": [
    { ... }
  ],
  "CellInfo": [
    { ... }
  ],
  "GridSize": {
    "x": 10,
    "y": 7
  },
  "Duration": 120.0,
  "LevelDifficulty": 0,
  "MaxCapacity": 5,
  "DisabledGridCells": [
    {
      "x": 2,
      "y": 3
    }
  ]
}
```

### Top-Level Properties

| Property | Type | Description | Default / Constraints |
| :--- | :--- | :--- | :--- |
| **`snakes`** | Array of Objects | Unified array of all gameplay objects: Holes, Ant Queues, and Pipes. | Minimum 1 item. |
| **`CellInfo`** | Array of Objects | Optional metadata per cell (blocked or forced disabled). | May be empty. |
| **`GridSize`** | Vector2Int (`{"x": int, "y": int}`) | Board width (`x`) and board height (`y`). | `x > 0, y > 0`. |
| **`Duration`** | Float | Level countdown timer in seconds. | Defaults to `120.0` if `<= 0`. |
| **`LevelDifficulty`** | Integer | Difficulty rating (`0` = Normal, `1` = Medium, `2` = Hard). | `0` or `2`. |
| **`MaxCapacity`** | Integer | Global default capacity for holes when not overridden. | Defaults to `5`. |
| **`DisabledGridCells`**| Array of Vector2Int | Coordinate list of holes/voids in the grid floor. | May be empty. |

---

## 2. The `snakes` Element Schema (`RawSnakeJSON`)

The `snakes` array represents three distinct board element types, differentiated by `HolderType`:

| `HolderType` | Gameplay Object | Meaning |
| :---: | :--- | :--- |
| **`0`** | **Stadium Hole (Holder)** | Target destination where colored ant queues must arrive and be swallowed. |
| **`1`** | **Ant Queue** | Controllable slithering chain of ant units (leader + workers). |
| **`2`** | **Directional Pipe** | Fixed transit tube that redirects ant queues from entrance to exit. |

### Property Reference

```json
{
  "Cells": [
    { "x": 3, "y": 4 },
    { "x": 3, "y": 5 }
  ],
  "ColorID": 2,
  "HolderType": 1,
  "IsConnected": false,
  "ConnectedIndex": 0,
  "IsSecondColor": false,
  "SecondColor": 0,
  "PipeCount": 0,
  "IsLocked": false,
  "LockAmount": 0
}
```

| Field | Type | Used By | Description |
| :--- | :--- | :--- | :--- |
| **`Cells`** | Array of Vector2Int | All | Ordered grid coordinates: for Holes = footprint; for Ants = body segments (head first); for Pipes = centerline polyline. |
| **`ColorID`** | Integer (`0..11`) | Holes, Ants | Primary color identifier mapping to `AntColor`. |
| **`HolderType`** | Integer (`0..2`) | All | Element discriminator (`0` = Hole, `1` = Ant Queue, `2` = Pipe). |
| **`IsConnected`** | Boolean | Ants | If `true`, ant queue is linked to companions by a golden rope. |
| **`ConnectedIndex`** | Integer | Ants | Unique identifier grouping companion linked ant queues together. |
| **`IsSecondColor`** | Boolean | Ants | If `true`, indicates a stacked ant queue with a dormant lower layer. |
| **`SecondColor`** | Integer (`0..11`) | Ants | Color ID of the lower dormant layer in a stacked ant queue. |
| **`PipeCount`** | Integer | Pipes | Remaining usable passages through the pipe before exhaustion. |
| **`IsLocked`** | Boolean | Ants | If `true`, ant queue cannot move until its padlock counter reaches 0. |
| **`LockAmount`** | Integer | Ants | Number of companion queue arrivals required to unlock this queue. |

---

## 3. Data Conversion & Runtime Mapping Rules

### A. Pipe Conversion
1. `snakes` entry with `HolderType == 2`:
   - `Cells` must have `Count >= 2`.
   - `EntranceCell` = `Cells[0]`.
   - `ExitCell` = `Cells[Cells.Count - 1]`.
   - `EntryDirection` = `Cells[1] - Cells[0]`.
   - `ExitDirection` = `Cells[Cells.Count - 1] - Cells[Cells.Count - 2]`.
   - `RemainingUses` = `PipeCount > 0 ? PipeCount : 1`.

### B. Stacked Ant Conversion
1. `snakes` entry with `HolderType == 1` and `IsSecondColor == true`:
   - Dominant upper queue is instantiated with `ColorID` and `SecondColor`.
   - Lower dormant queue is instantiated with `ColorID = SecondColor`, sharing identical initial `Cells`.
   - Lower queue is deactivated until dominant queue executes an accepted move.

### C. Locked Ant Conversion
1. `snakes` entry with `HolderType == 1` and `IsLocked == true`:
   - Initialized with `IsLocked = true` and `LockAmount = entry.LockAmount`.
   - An `AntLockBadge` component is attached to the queue root and displays the counter.

### D. Hard Level & Presentation Flags
1. If `LevelDifficulty >= 2` or `raw.LevelDifficulty == 2`:
   - `ShowHardIntro = true`.
   - Triggers the hard overlay on board initialization.
2. `TutorialFlags`:
   - Bit `1`: Linked Ants Tutorial.
   - Bit `2`: Stacked Ants Tutorial.
   - Bit `4`: Directional Pipes Tutorial.
   - Bit `8`: Locked Ants Tutorial.
