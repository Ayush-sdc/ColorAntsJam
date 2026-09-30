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
    /// Accurately counts stacked layers (second color) and queues routed through pipes.
    /// </summary>
    public static List<RuntimeReferenceHole> ExtractHoles(Level level)
    {
        var holes = new List<RuntimeReferenceHole>();
        if (level == null || level.AntQueues == null) return holes;

        // Count active playable queues per ColorID (including second color of stacked ants)
        var matchingQueueCounts = new Dictionary<int, int>();
        foreach (var q in level.AntQueues)
        {
            if (q.HolderType == 1)
            {
                int cId = q.ColorID;
                if (!matchingQueueCounts.ContainsKey(cId)) matchingQueueCounts[cId] = 0;
                matchingQueueCounts[cId]++;

                // Stacked layer also requires hole capacity!
                if (q.IsSecondColor)
                {
                    int sId = q.SecondColor;
                    if (!matchingQueueCounts.ContainsKey(sId)) matchingQueueCounts[sId] = 0;
                    matchingQueueCounts[sId]++;
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
                    ExplicitCapacity = record.HoleCapacity
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

        // Extract pipes for routing calculations
        var rawPipes = ExtractPipes(level);

        // Count queues whose forward direction (or pipe route) reaches each hole
        var forwardCounts = new Dictionary<RuntimeReferenceHole, int>();
        foreach (var h in holes) forwardCounts[h] = 0;

        int gridW = level.GridWidth > 0 ? level.GridWidth : 20;
        int gridH = level.GridHeight > 0 ? level.GridHeight : 20;

        foreach (var q in level.AntQueues)
        {
            if (q.HolderType == 1 && q.Cells != null && q.Cells.Count >= 2)
            {
                // Trace primary color
                TraceQueueToHole(q.Cells[0], q.Cells[1], q.ColorID, rawPipes, cellToHole, forwardCounts, gridW, gridH);

                // Trace stacked second color
                if (q.IsSecondColor)
                {
                    TraceQueueToHole(q.Cells[0], q.Cells[1], q.SecondColor, rawPipes, cellToHole, forwardCounts, gridW, gridH);
                }
            }
        }

        // Assign capacity: group holes by ColorID so total color capacity satisfies all required queues (including stacked/piped).
        // If explicit capacity exists in JSON, respect it and report mismatches. If inferred, distribute across holes.
        var holesByColor = new Dictionary<int, List<RuntimeReferenceHole>>();
        foreach (var h in holes)
        {
            if (!holesByColor.TryGetValue(h.ColorID, out var list))
            {
                list = new List<RuntimeReferenceHole>();
                holesByColor[h.ColorID] = list;
            }
            list.Add(h);
        }

        foreach (var kvp in holesByColor)
        {
            int colorId = kvp.Key;
            var colorHoles = kvp.Value;
            int totalNeeded = matchingQueueCounts.TryGetValue(colorId, out int t) ? t : 0;

            int explicitSum = 0;
            int holesWithoutExplicit = 0;
            foreach (var h in colorHoles)
            {
                if (h.ExplicitCapacity > 0) explicitSum += h.ExplicitCapacity;
                else holesWithoutExplicit++;
            }

            if (holesWithoutExplicit == 0)
            {
                // All holes have explicit capacity
                foreach (var h in colorHoles) h.Capacity = h.ExplicitCapacity;
                if (explicitSum < totalNeeded)
                {
                    Debug.LogWarning($"[HoleCapacityMismatch] Level {level.LevelNumber} ColorID {colorId}: Explicit capacity sum {explicitSum} is less than required queues {totalNeeded}!");
                }
                continue;
            }

            if (colorHoles.Count == 1)
            {
                var h = colorHoles[0];
                int assigned = h.ExplicitCapacity > 0 ? h.ExplicitCapacity : Mathf.Max(1, totalNeeded);
                h.Capacity = assigned;
                if (h.ExplicitCapacity <= 0)
                {
                    Debug.LogWarning($"[HoleCapacity] Level {level.LevelNumber} Hole ColorID {h.ColorID}: Inferred capacity {assigned} from {totalNeeded} expected queues.");
                }
            }
            else
            {
                // Multi-hole distribution: start with forwardCounts or 1
                int currentAllocated = 0;
                foreach (var h in colorHoles)
                {
                    if (h.ExplicitCapacity > 0)
                    {
                        h.Capacity = h.ExplicitCapacity;
                    }
                    else
                    {
                        int fCount = forwardCounts.TryGetValue(h, out int fc) ? fc : 0;
                        h.Capacity = Mathf.Max(1, fCount);
                    }
                    currentAllocated += h.Capacity;
                }

                // If sum of capacities is still less than totalNeeded, distribute remaining across non-explicit holes
                while (currentAllocated < totalNeeded)
                {
                    RuntimeReferenceHole bestHole = null;
                    int minCap = int.MaxValue;
                    foreach (var h in colorHoles)
                    {
                        if (h.ExplicitCapacity <= 0 && h.Capacity < minCap)
                        {
                            minCap = h.Capacity;
                            bestHole = h;
                        }
                    }
                    if (bestHole == null) break;
                    bestHole.Capacity++;
                    currentAllocated++;
                }

                // If sum of capacities exceeds totalNeeded (e.g. because of max(1, 0)), trim holes with 0 forward count
                while (currentAllocated > totalNeeded)
                {
                    RuntimeReferenceHole holeToTrim = null;
                    int maxCap = 1;
                    foreach (var h in colorHoles)
                    {
                        int fCount = forwardCounts.TryGetValue(h, out int fc) ? fc : 0;
                        if (h.ExplicitCapacity <= 0 && fCount == 0 && h.Capacity > 0 && h.Capacity >= maxCap)
                        {
                            maxCap = h.Capacity;
                            holeToTrim = h;
                        }
                    }
                    if (holeToTrim == null) break;
                    holeToTrim.Capacity--;
                    currentAllocated--;
                }

                foreach (var h in colorHoles)
                {
                    if (h.ExplicitCapacity <= 0)
                    {
                        Debug.LogWarning($"[HoleCapacity] Level {level.LevelNumber} Multi-hole ColorID {h.ColorID}: Inferred capacity {h.Capacity} (total color needed: {totalNeeded}).");
                    }
                }
            }
        }

        return holes;
    }

    private static void TraceQueueToHole(
        Vector2Int head,
        Vector2Int neck,
        int targetColorId,
        List<RuntimeReferencePipe> pipes,
        Dictionary<Vector2Int, RuntimeReferenceHole> cellToHole,
        Dictionary<RuntimeReferenceHole, int> forwardCounts,
        int gridW,
        int gridH)
    {
        Vector2Int dir = head - neck;
        if (dir != Vector2Int.right && dir != Vector2Int.left && dir != Vector2Int.up && dir != Vector2Int.down) return;

        Vector2Int cur = head + dir;
        int maxSteps = 100;
        int steps = 0;

        while (cur.x >= 0 && cur.x < gridW && cur.y >= 0 && cur.y < gridH && steps++ < maxSteps)
        {
            // Direct hole hit
            if (cellToHole.TryGetValue(cur, out var targetHole))
            {
                if (targetHole.ColorID == targetColorId)
                {
                    forwardCounts[targetHole]++;
                    break;
                }
                // If a hole of a different color is encountered:
                // During gameplay, that hole will be cleared and unmapped by its own matching queues,
                // freeing this corridor. Do NOT break - continue scanning along the ray!
            }

            // Pipe entrance hit
            var pipe = pipes.Find(p => p.Cells.Count >= 2 && p.Cells[0] == cur && (p.Cells[1] - p.Cells[0]) == dir);
            if (pipe != null)
            {
                // Follow pipe to exit
                Vector2Int exitCell = pipe.Cells[pipe.Cells.Count - 1];
                Vector2Int exitDir = pipe.Cells[pipe.Cells.Count - 1] - pipe.Cells[pipe.Cells.Count - 2];
                cur = exitCell + exitDir;
                dir = exitDir;
                continue;
            }

            cur += dir;
        }
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

    /// <summary>
    /// Extracts runtime Pipe definitions from raw reference Level data (HolderType == 2).
    /// Resolves required uses: if raw PipeCount > 0 (e.g. Level 45 has PipeCount 2), preserves it.
    /// If raw PipeCount == 0, dynamically counts ant queues routed into the pipe entrance, ensuring all queues can pass.
    /// </summary>
    public static List<RuntimeReferencePipe> ExtractPipes(Level level)
    {
        var pipes = new List<RuntimeReferencePipe>();
        if (level == null || level.AntQueues == null) return pipes;

        foreach (var record in level.AntQueues)
        {
            if (record.HolderType == 2 && record.Cells != null && record.Cells.Count >= 2)
            {
                int effectiveCount = record.PipeCount;
                if (effectiveCount <= 0)
                {
                    // Dynamically resolve required uses from queues pointing into pipe entrance
                    Vector2Int ent = record.Cells[0];
                    Vector2Int entDir = record.Cells[1] - record.Cells[0];
                    int pointingCount = 0;
                    foreach (var q in level.AntQueues)
                    {
                        if (q.HolderType == 1 && q.Cells != null && q.Cells.Count >= 2)
                        {
                            Vector2Int head = q.Cells[0];
                            Vector2Int neck = q.Cells[1];
                            Vector2Int fwd = head - neck;
                            if (fwd == entDir)
                            {
                                int steps = (fwd.x != 0) ? (ent.x - head.x) * fwd.x : (ent.y - head.y) * fwd.y;
                                if (steps > 0)
                                {
                                    if ((fwd.x != 0 && head.y == ent.y) || (fwd.y != 0 && head.x == ent.x))
                                    {
                                        pointingCount++;
                                    }
                                }
                            }
                        }
                    }
                    effectiveCount = Mathf.Max(1, pointingCount);
                }

                pipes.Add(new RuntimeReferencePipe
                {
                    Cells = new List<Vector2Int>(record.Cells),
                    PipeCount = effectiveCount
                });
            }
        }

        return pipes;
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
    public int ExplicitCapacity = 0;
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
/// Runtime representation of a reference pipe reconstructed dynamically from HolderType = 2 data.
/// </summary>
public class RuntimeReferencePipe
{
    public List<Vector2Int> Cells = new List<Vector2Int>();
    public int PipeCount;
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
