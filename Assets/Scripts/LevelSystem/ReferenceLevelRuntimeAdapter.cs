using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ReferenceLevelRuntimeAdapter translates raw immutable reference Level assets
/// (containing HolderType 0 holes and HolderType 1 ant queues) into active runtime board objects
/// without mutating, overwriting, or converting the underlying ScriptableObjects.
/// </summary>
public static class ReferenceLevelRuntimeAdapter
{
    /// <summary>
    /// Checks whether a Level ScriptableObject is a raw reference level (e.g. contains HolderType 0 records in AntQueues without explicit Holes list).
    /// </summary>
    public static bool IsReferenceLevel(Level level)
    {
        if (level == null) return false;

        // If explicit handmade holes are configured, this is a handmade level
        if (level.Holes != null && level.Holes.Count > 0)
        {
            return false;
        }

        // If no explicit holes list, but antQueues contains reference data
        return (level.AntQueues != null && level.AntQueues.Count > 0) || (level.CellInfo != null && level.CellInfo.Count > 0);
    }

    /// <summary>
    /// Extracts runtime Hole definitions from raw reference Level data (HolderType == 0).
    /// Uses complete ordered Cells list to determine physical tray shape, entrance cell, and orientation.
    /// </summary>
    public static List<RuntimeReferenceHole> ExtractHoles(Level level)
    {
        var holes = new List<RuntimeReferenceHole>();
        if (level == null || level.AntQueues == null) return holes;

        // Count total ants per ColorID from HolderType 1 records in this level
        var colorAntCounts = new Dictionary<int, int>();
        foreach (var q in level.AntQueues)
        {
            if (q.HolderType == 1)
            {
                int cId = q.ColorID;
                int count = (q.Cells != null && q.Cells.Count > 0) ? q.Cells.Count : q.AntCount;
                if (colorAntCounts.ContainsKey(cId))
                {
                    colorAntCounts[cId] += count;
                }
                else
                {
                    colorAntCounts[cId] = count;
                }
            }
        }

        foreach (var record in level.AntQueues)
        {
            if (record.HolderType == 0)
            {
                var runtimeHole = new RuntimeReferenceHole
                {
                    ColorID = record.ColorID,
                    AntColor = ReferenceColorMapper.ToAntColor(record.ColorID),
                    OccupiedCells = record.Cells != null ? new List<Vector2Int>(record.Cells) : new List<Vector2Int>(),
                };

                // Determine capacity: matching ants count or explicit lock/capacity
                if (colorAntCounts.TryGetValue(record.ColorID, out int count) && count > 0)
                {
                    runtimeHole.Capacity = count;
                }
                else
                {
                    runtimeHole.Capacity = record.AntCount > 0 ? record.AntCount : 1;
                }

                // Determine physical orientation, entrance cell, and parallel rows from ordered Cells list
                if (runtimeHole.OccupiedCells.Count >= 2)
                {
                    Vector2Int entranceCell = runtimeHole.OccupiedCells[0];
                    Vector2Int outerCell = runtimeHole.OccupiedCells[1];

                    runtimeHole.EntranceCell = entranceCell;

                    // If entrance is to the left of outer cell (entrance.x < outer.x), entrance is on left, badge is on right -> isRightSide = true
                    // If entrance is to the right of outer cell (entrance.x > outer.x), entrance is on right, badge is on left -> isRightSide = false
                    runtimeHole.IsRightSide = entranceCell.x <= outerCell.x;
                    runtimeHole.ParallelRows = (runtimeHole.OccupiedCells.Count > 2) ? 2 : 1;
                }
                else if (runtimeHole.OccupiedCells.Count == 1)
                {
                    runtimeHole.EntranceCell = runtimeHole.OccupiedCells[0];
                    runtimeHole.IsRightSide = runtimeHole.EntranceCell.x >= (level.GridWidth / 2);
                    runtimeHole.ParallelRows = 1;
                }

                holes.Add(runtimeHole);
            }
        }

        return holes;
    }

    /// <summary>
    /// Extracts playable AntQueue definitions from raw reference Level data (HolderType == 1).
    /// Spawns one ant per Cells entry, preserves exact ordered coordinates with Cells[0] as leader/head.
    /// </summary>
    public static List<RuntimeReferenceQueue> ExtractPlayableQueues(Level level, List<RuntimeReferenceHole> holes)
    {
        var queues = new List<RuntimeReferenceQueue>();
        if (level == null || level.AntQueues == null) return queues;

        int queueIndex = 0;
        foreach (var record in level.AntQueues)
        {
            if (record.HolderType == 1)
            {
                var runtimeQueue = new RuntimeReferenceQueue
                {
                    QueueId = !string.IsNullOrEmpty(record.QueueId) ? record.QueueId : $"RefQueue_{record.ColorID}_{queueIndex++}",
                    ColorID = record.ColorID,
                    AntColor = ReferenceColorMapper.ToAntColor(record.ColorID),
                    BodyCoordinates = record.Cells != null ? new List<Vector2Int>(record.Cells) : new List<Vector2Int>(),
                    IsConnected = record.IsConnected,
                    ConnectedIndex = record.ConnectedIndex,
                    IsSecondColor = record.IsSecondColor,
                    SecondColor = record.SecondColor,
                    PipeCount = record.PipeCount,
                    IsLocked = record.IsLocked,
                    LockAmount = record.LockAmount
                };

                // Cells[0] is the leader / head
                if (runtimeQueue.BodyCoordinates.Count > 0)
                {
                    Vector2Int head = runtimeQueue.BodyCoordinates[0];

                    // Find matching hole of the same ColorID
                    RuntimeReferenceHole matchingHole = holes.Find(h => h.ColorID == record.ColorID);
                    if (matchingHole != null && matchingHole.OccupiedCells.Count > 0)
                    {
                        Vector2Int holeEntrance = matchingHole.EntranceCell;
                        // Initial step: path from head into adjacent hole entrance
                        runtimeQueue.PathToHole = new List<Vector2Int> { head, holeEntrance };
                    }
                    else
                    {
                        runtimeQueue.PathToHole = new List<Vector2Int> { head };
                    }
                }

                queues.Add(runtimeQueue);
            }
        }

        return queues;
    }
}

/// <summary>
/// Runtime representation of a reference hole / tray reconstructed dynamically from HolderType = 0 data.
/// </summary>
public class RuntimeReferenceHole
{
    public int ColorID;
    public AntColor AntColor;
    public List<Vector2Int> OccupiedCells = new List<Vector2Int>();
    public Vector2Int EntranceCell;
    public int Capacity;
    public bool IsRightSide;
    public int ParallelRows = 1;
}

/// <summary>
/// Runtime representation of a reference playable ant queue reconstructed dynamically from HolderType = 1 data.
/// </summary>
public class RuntimeReferenceQueue
{
    public string QueueId;
    public int ColorID;
    public AntColor AntColor;
    public List<Vector2Int> BodyCoordinates = new List<Vector2Int>();
    public List<Vector2Int> PathToHole = new List<Vector2Int>();
    public bool IsConnected;
    public int ConnectedIndex;
    public bool IsSecondColor;
    public int SecondColor;
    public int PipeCount;
    public bool IsLocked;
    public int LockAmount;
}
