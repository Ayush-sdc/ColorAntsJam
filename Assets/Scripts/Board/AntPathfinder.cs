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
        if (board != null && !board.UseAutomaticPathfinding)
        {
            Debug.LogError("[ERROR] AntPathfinder invoked while automatic pathfinding disabled.");
            return null;
        }

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
                if (otherQueue.IsMoving)
                {
                    foreach (var c in otherQueue.GetReservedPathCells())
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

        bool hasNeck = queue.CurrentBodyCoordinates.Count >= 2;
        Vector2Int forbiddenFirstStep = hasNeck ? queue.CurrentBodyCoordinates[1] : new Vector2Int(-99999, -99999);

        // Own body segments behind the neck (indices 2 to N-1).
        // Index 1 (neck) is forbidden as the initial step from head, but not permanently blocked if vacated later.
        for (int i = 2; i < queue.CurrentBodyCoordinates.Count; i++)
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

        int gridWidth = board != null ? board.GridWidth : 9;
        int gridHeight = board != null ? board.GridHeight : 6;

        Vector2Int forwardDir = hasNeck ? (start - forbiddenFirstStep) : Directions[0];
        Vector2Int[] orderedDirections = new Vector2Int[]
        {
            forwardDir,
            new Vector2Int(-forwardDir.y, forwardDir.x),
            new Vector2Int(forwardDir.y, -forwardDir.x),
            -forwardDir
        };

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

            bool isAtStart = (current == start);

            foreach (var dir in orderedDirections)
            {
                // Initial step from head MUST be strictly forward along queue forward direction
                if (isAtStart && hasNeck && dir != forwardDir)
                {
                    continue;
                }

                Vector2Int next = current + dir;

                // Rule 1: Queue must always move forward from head and NEVER immediately reverse into its neck/body
                if (isAtStart && hasNeck && next == forbiddenFirstStep)
                {
                    continue;
                }

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

        // Enforce Rule 1 assertion: First step after head must NEVER be neck
        if (path.Count >= 2 && hasNeck)
        {
            if (path[1] == forbiddenFirstStep)
            {
                Debug.LogWarning($"[AntPathfinder] Path rejected for {queue.QueueId}: First step equals neck {forbiddenFirstStep}!");
                return null;
            }
            forwardDir = start - forbiddenFirstStep;
            Debug.Log($"[MoveAttempt] Queue={queue.QueueId} Head={start} Forward={forwardDir} FirstStep={path[1]} Result=VALID");
        }

        return path;
    }

    /// <summary>
    /// Finds path to matching holder and outputs the targeted HoleController and its entrance coordinate.
    /// </summary>
    public static List<Vector2Int> FindPath(BoardManager board, AntQueueController queue, out HoleController targetHole, out Vector2Int entranceCell)
    {
        targetHole = null;
        entranceCell = Vector2Int.zero;
        var path = FindPath(board, queue);
        if (path == null || path.Count < 2) return null;

        entranceCell = path[path.Count - 1];
        if (board != null)
        {
            targetHole = board.GetHoleAt(entranceCell);
            if (targetHole == null && board.HolesMap != null)
            {
                foreach (var h in board.HolesMap.Values)
                {
                    if (h != null && h.OccupiedCells != null)
                    {
                        for (int i = 0; i < h.OccupiedCells.Count; i++)
                        {
                            if (h.OccupiedCells[i] == entranceCell)
                            {
                                targetHole = h;
                                break;
                            }
                        }
                        if (targetHole != null) break;
                    }
                }
            }
        }
        return path;
    }
}
