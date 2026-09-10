using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Level_001", menuName = "SlitherAnts/Level Data", order = 1)]
public class Level : ScriptableObject
{
    [Header("Level Meta")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelName = "Level 1";
    [SerializeField] private string difficulty = "Easy";
    [FormerlySerializedAs("timeLimitInSeconds")]
    [SerializeField] private float duration = 120f;

    [Header("Grid Board Settings")]
    [Tooltip("Authoritative grid dimensions (Width, Height) for the board.")]
    [SerializeField] private Vector2Int gridSize = Vector2Int.zero;

    [Tooltip("Legacy serialized width fallback for older Level 1-3 assets.")]
    [FormerlySerializedAs("gridWidth")]
    [SerializeField] private int gridWidth = 0;

    [Tooltip("Legacy serialized height fallback for older Level 1-3 assets.")]
    [FormerlySerializedAs("gridHeight")]
    [SerializeField] private int gridHeight = 0;

    [SerializeField] private float cellSize = 1.0f;
    [SerializeField] private int capacity = 5;

    [Header("Grid Obstacles & Inactive Cells")]
    [SerializeField] private List<Vector2Int> disabledGridCells = new List<Vector2Int>();
    [SerializeField] private List<CellInfo> cellInfo = new List<CellInfo>();

    [Header("Level Content")]
    [Tooltip("Target holes placed on the grid.")]
    [SerializeField] private List<HoleData> holes = new List<HoleData>();

    [Tooltip("All ant queues positioned on the grid for this level.")]
    [SerializeField] private List<AntQueueData> antQueues = new List<AntQueueData>();

    // Public Properties
    public int LevelNumber => levelNumber;
    public string LevelName => levelName;
    public string Difficulty => difficulty;
    public float Duration => duration > 0f ? duration : 120f;
    public float TimeLimitInSeconds => Duration;

    /// <summary>
    /// Authoritative grid dimensions (X = Width, Y = Height).
    /// If gridSize is set (gridSize.x > 0 && gridSize.y > 0), it is authoritative.
    /// Otherwise falls back to legacy gridWidth/gridHeight.
    /// </summary>
    public Vector2Int GridSize
    {
        get
        {
            if (gridSize.x > 0 && gridSize.y > 0)
            {
                return gridSize;
            }
            int w = gridWidth > 0 ? gridWidth : 9;
            int h = gridHeight > 0 ? gridHeight : 6;
            return new Vector2Int(w, h);
        }
    }

    /// <summary>
    /// Runtime GridWidth returns authoritative GridSize.x.
    /// </summary>
    public int GridWidth => GridSize.x;

    /// <summary>
    /// Runtime GridHeight returns authoritative GridSize.y.
    /// </summary>
    public int GridHeight => GridSize.y;

    public float CellSize => cellSize > 0f ? cellSize : 1.0f;
    public int Capacity => capacity;
    public int MaxCapacity => capacity;
    public IReadOnlyList<Vector2Int> DisabledGridCells => disabledGridCells;
    public IReadOnlyList<CellInfo> CellInfo => cellInfo;
    public IReadOnlyList<HoleData> Holes => holes;
    public IReadOnlyList<AntQueueData> AntQueues => antQueues;
    public IReadOnlyList<AntQueueData> Snakes => antQueues;

    /// <summary>
    /// Returns the combined set of all disabled / inactive grid coordinates from DisabledGridCells and CellInfo.
    /// </summary>
    public HashSet<Vector2Int> GetCombinedDisabledCells()
    {
        HashSet<Vector2Int> set = new HashSet<Vector2Int>();
        if (disabledGridCells != null)
        {
            foreach (var c in disabledGridCells)
            {
                set.Add(c);
            }
        }
        if (cellInfo != null)
        {
            foreach (var info in cellInfo)
            {
                if (info != null && (info.IsForcedDisabledCell || info.IsBlocked))
                {
                    set.Add(info.Position);
                }
            }
        }
        return set;
    }

    public void SetLevelData(int number, string name, string diff, Vector2Int size, float cell, List<HoleData> holeList, List<AntQueueData> queueList)
    {
        levelNumber = number;
        levelName = name;
        difficulty = diff;
        gridSize = size;
        gridWidth = size.x;
        gridHeight = size.y;
        cellSize = cell;
        holes = holeList != null ? new List<HoleData>(holeList) : new List<HoleData>();
        antQueues = queueList != null ? new List<AntQueueData>(queueList) : new List<AntQueueData>();
    }

    public void SetLevelData(int number, string name, string diff, int width, int height, float cell, List<HoleData> holeList, List<AntQueueData> queueList)
    {
        SetLevelData(number, name, diff, new Vector2Int(width, height), cell, holeList, queueList);
    }

    public void SetReferenceLevelData(
        int number,
        string name,
        string diff,
        float durationSeconds,
        Vector2Int size,
        int maxCapacity,
        List<Vector2Int> disabledCells,
        List<CellInfo> cellsInfoList,
        List<AntQueueData> queueList,
        List<HoleData> holeList = null,
        float cell = 1.0f)
    {
        levelNumber = number;
        levelName = name;
        difficulty = diff;
        duration = durationSeconds;
        gridSize = size;
        gridWidth = size.x;
        gridHeight = size.y;
        capacity = maxCapacity;
        disabledGridCells = disabledCells != null ? new List<Vector2Int>(disabledCells) : new List<Vector2Int>();
        cellInfo = cellsInfoList != null ? new List<CellInfo>(cellsInfoList) : new List<CellInfo>();
        antQueues = queueList != null ? new List<AntQueueData>(queueList) : new List<AntQueueData>();
        holes = holeList != null ? new List<HoleData>(holeList) : new List<HoleData>();
        cellSize = cell > 0f ? cell : 1.0f;
    }
}
