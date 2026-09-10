using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic 4-directional Breadth-First-Search (BFS) pathfinder for ant queues.
/// Evaluates dynamic grid occupancy, matching destination holders by raw ColorID, and holder entrance cells.
/// </summary>
public static class AntPathfinder
{
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0)   // Left
    };

    /// <summary>
    /// Finds the shortest 4-directional path from queue's head (CurrentBodyCoordinates[0])
    /// to a reachable entrance cell of a matching non-full destination holder.
    /// Returns null if no valid unblocked path exists.
    /// </summary>
    public static List<Vector2Int> FindPath(BoardManager board, AntQueueController queue)
    {
        if (board == null || queue == null) return null;
        if (queue.CurrentBodyCoordinates == null || queue.CurrentBodyCoordinates.Count == 0) return null;

        Vector2Int start = queue.CurrentBodyCoordinates[0];
        int queueColorId = queue.ColorID;

        // 1. Identify matching destination holders by raw ColorID that are not full
        List<HoleController> matchingHoles = new List<HoleController>();
        foreach (var hole in board.HolesMap.Values)
        {
            if (hole == null) continue;
            if (hole.ColorID == queueColorId && !hole.IsFull)
            {
                if (!matchingHoles.Contains(hole))
                {
                    matchingHoles.Add(hole);
                }
            }
        }

        if (matchingHoles.Count == 0)
        {
            return null;
        }

        // Collect all valid goal footprint cells across all matching holders
        HashSet<Vector2Int> goalCells = new HashSet<Vector2Int>();
        foreach (var hole in matchingHoles)
        {
            if (hole.OccupiedCells != null && hole.OccupiedCells.Count > 0)
            {
                foreach (var cell in hole.OccupiedCells)
                {
                    goalCells.Add(cell);
                }
            }
            else
            {
                goalCells.Add(hole.GridCoordinate);
            }
        }

        // 2. Identify all currently blocked cells:
        // - Cells occupied by other active ant queues
        // - Cells occupied by non-matching or full holes
        // - Disabled / inactive cells from Level data
        // - Cells occupied by HolderType 2 obstacles (if any)
        // - Own queue's body segments behind the head (indices 1 to N-1)
        HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();

        // Other active ant queues
        if (board.ActiveQueues != null)
        {
            foreach (var otherQueue in board.ActiveQueues)
            {
                if (otherQueue == null || otherQueue == queue) continue;
                if (otherQueue.CurrentBodyCoordinates != null)
                {
                    foreach (var c in otherQueue.CurrentBodyCoordinates)
                    {
                        blockedCells.Add(c);
                    }
                }
            }
        }

        // Non-matching or full holes
        if (board.HolesMap != null)
        {
            foreach (var kvp in board.HolesMap)
            {
                var h = kvp.Value;
                if (h == null || !matchingHoles.Contains(h))
                {
                    blockedCells.Add(kvp.Key);
                }
            }
        }

        // Disabled grid cells
        Level level = board.CurrentLevel;
        if (level != null)
        {
            foreach (var c in level.GetCombinedDisabledCells())
            {
                blockedCells.Add(c);
            }

            // HolderType 2 blockers
            if (level.AntQueues != null)
            {
                foreach (var qData in level.AntQueues)
                {
                    if (qData.HolderType == 2 && qData.Cells != null)
                    {
                        foreach (var c in qData.Cells)
                        {
                            blockedCells.Add(c);
                        }
                    }
                }
            }
        }

        // Own body segments behind the head (indices 1 to N-1)
        for (int i = 1; i < queue.CurrentBodyCoordinates.Count; i++)
        {
            blockedCells.Add(queue.CurrentBodyCoordinates[i]);
        }

        // 3. Perform 4-direction BFS traversal
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        frontier.Enqueue(start);
        visited.Add(start);

        Vector2Int targetReached = new Vector2Int(-9999, -9999);
        bool reached = false;

        int gridWidth = board.GridWidth;
        int gridHeight = board.GridHeight;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            // Reached one of the matching holder goal entrance cells
            if (goalCells.Contains(current))
            {
                targetReached = current;
                reached = true;
                break;
            }

            foreach (var dir in Directions)
            {
                Vector2Int next = current + dir;

                // Check grid boundaries (unless it is a valid goal cell)
                if (!goalCells.Contains(next))
                {
                    if (next.x < 0 || next.x >= gridWidth || next.y < 0 || next.y >= gridHeight)
                    {
                        continue;
                    }
                }

                // Check if cell is blocked
                if (blockedCells.Contains(next))
                {
                    continue;
                }

                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }
        }

        if (!reached)
        {
            return null;
        }

        // 4. Reconstruct path from start to targetReached
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int step = targetReached;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }
        path.Add(start);
        path.Reverse();

        return path;
    }
}
