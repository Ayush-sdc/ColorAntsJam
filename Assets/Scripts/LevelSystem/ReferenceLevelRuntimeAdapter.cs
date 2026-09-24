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

        // Count active HolderType 1 playable queues per ColorID
        var matchingQueueCounts = new Dictionary<int, int>();
        foreach (var q in level.AntQueues)
        {
            if (q.HolderType == 1)
            {
                int cId = q.ColorID;
                if (matchingQueueCounts.ContainsKey(cId))
                {
                    matchingQueueCounts[cId]++;
                }
                else
                {
                    matchingQueueCounts[cId] = 1;
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

                ReferenceColorMapper.LogColorMappingOnce(record.ColorID);

                // Compute bounding footprint and dimensions
                if (runtimeHole.OccupiedCells.Count > 0)
                {
                    int minX = int.MaxValue, maxX = int.MinValue;
                    int minY = int.MaxValue, maxY = int.MinValue;
                    foreach (var cell in runtimeHole.OccupiedCells)
                    {
                        if (cell.x < minX) minX = cell.x;
                        if (cell.x > maxX) maxX = cell.x;
                        if (cell.y < minY) minY = cell.y;
                        if (cell.y > maxY) maxY = cell.y;
                    }
                    runtimeHole.MinX = minX;
                    runtimeHole.MaxX = maxX;
                    runtimeHole.MinY = minY;
                    runtimeHole.MaxY = maxY;
                    runtimeHole.WidthCells = maxX - minX + 1;
                    runtimeHole.HeightCells = maxY - minY + 1;
                    runtimeHole.EntranceCell = runtimeHole.OccupiedCells[0];
                    runtimeHole.ParallelRows = (runtimeHole.HeightCells >= 2 && runtimeHole.WidthCells >= 2) ? 2 : 1;
                    runtimeHole.IsRightSide = runtimeHole.EntranceCell.x <= (minX + maxX) / 2;
                }
                else
                {
                    runtimeHole.WidthCells = 1;
                    runtimeHole.HeightCells = 1;
                }

                holes.Add(runtimeHole);
            }
        }

        // Map occupied cell -> hole
        var cellToHole = new Dictionary<Vector2Int, RuntimeReferenceHole>();
        foreach (var h in holes)
        {
            foreach (var c in h.OccupiedCells)
            {
                cellToHole[c] = h;
            }
        }

        // Count queues whose forward direction directly faces each hole
        var forwardCounts = new Dictionary<RuntimeReferenceHole, int>();
        foreach (var h in holes) forwardCounts[h] = 0;

        int gridW = level.GridWidth > 0 ? level.GridWidth : 20;
        int gridH = level.GridHeight > 0 ? level.GridHeight : 20;

        foreach (var q in level.AntQueues)
        {
            if (q.HolderType == 1 && q.Cells != null && q.Cells.Count >= 2)
            {
                Vector2Int head = q.Cells[0];
                Vector2Int neck = q.Cells[1];
                Vector2Int dir = head - neck;
                if (dir == Vector2Int.right || dir == Vector2Int.left || dir == Vector2Int.up || dir == Vector2Int.down)
                {
                    Vector2Int cur = head + dir;
                    while (cur.x >= 0 && cur.x < gridW && cur.y >= 0 && cur.y < gridH)
                    {
                        if (cellToHole.TryGetValue(cur, out var targetHole))
                        {
                            if (targetHole.ColorID == q.ColorID)
                            {
                                forwardCounts[targetHole]++;
                                break;
                            }
                        }
                        cur += dir;
                    }
                }
            }
        }

        // Assign capacity: if forward-facing queues were found, use that count; otherwise fallback to total color count
        foreach (var h in holes)
        {
            if (forwardCounts.TryGetValue(h, out int count) && count > 0)
            {
                h.Capacity = count;
            }
            else if (matchingQueueCounts.TryGetValue(h.ColorID, out int totalCount) && totalCount > 0)
            {
                h.Capacity = totalCount;
            }
            else
            {
                h.Capacity = 1;
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

        int gridW = level.GridWidth > 0 ? level.GridWidth : 20;
        int gridH = level.GridHeight > 0 ? level.GridHeight : 20;

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
                    RuntimeReferenceHole matchingHole = null;

                    // Trace forward along cardinal direction to find matching hole
                    if (runtimeQueue.BodyCoordinates.Count >= 2)
                    {
                        Vector2Int neck = runtimeQueue.BodyCoordinates[1];
                        Vector2Int dir = head - neck;
                        if (dir == Vector2Int.right || dir == Vector2Int.left || dir == Vector2Int.up || dir == Vector2Int.down)
                        {
                            Vector2Int cur = head + dir;
                            while (cur.x >= 0 && cur.x < gridW && cur.y >= 0 && cur.y < gridH)
                            {
                                var hit = holes.Find(h => h.ColorID == record.ColorID && h.OccupiedCells.Contains(cur));
                                if (hit != null)
                                {
                                    matchingHole = hit;
                                    break;
                                }
                                cur += dir;
                            }
                        }
                    }

                    if (matchingHole == null)
                    {
                        matchingHole = holes.Find(h => h.ColorID == record.ColorID);
                    }

                    if (matchingHole != null && matchingHole.OccupiedCells.Count > 0)
                    {
                        Vector2Int holeEntrance = matchingHole.EntranceCell;
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
    public int MinX;
    public int MaxX;
    public int MinY;
    public int MaxY;
    public int WidthCells = 1;
    public int HeightCells = 1;
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
