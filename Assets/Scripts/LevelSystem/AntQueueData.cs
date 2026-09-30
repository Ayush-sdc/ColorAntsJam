using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class CellInfo
{
    [SerializeField] private Vector2Int position = Vector2Int.zero;
    [SerializeField] private bool isForcedDisabledCell = false;
    [SerializeField] private int cellType = 0;
    [SerializeField] private int colorId = 0;
    [SerializeField] private int capacity = 1;

    public Vector2Int Position => position;
    public Vector2Int GridPosition => position;
    public bool IsForcedDisabledCell => isForcedDisabledCell;
    public bool IsBlocked => isForcedDisabledCell;
    public int CellType => cellType;
    public int ColorID => colorId;
    public AntColor Color => ReferenceColorMapper.ToAntColor(colorId);
    public int Capacity => capacity;

    public CellInfo() { }

    public CellInfo(Vector2Int pos, bool isForcedDisabled = false, int cellType = 0, int colorId = 0, int capacity = 1)
    {
        this.position = pos;
        this.isForcedDisabledCell = isForcedDisabled;
        this.cellType = cellType;
        this.colorId = colorId;
        this.capacity = capacity;
    }
}

[Serializable]
public class AntQueueData
{
    [Header("Identification")]
    [SerializeField] private string queueId = "Queue_0";

    [Header("Color & Holder Attributes")]
    [Tooltip("Raw reference ColorID integer. Preserved exactly as in reference data.")]
    [FormerlySerializedAs("antColor")]
    [SerializeField] private int colorId = 0;

    [Tooltip("Holder type identifier (e.g. 0 = standard, etc.). Exact reference numeric value preserved.")]
    [SerializeField] private int holderType = 0;

    [Header("Ant Queue Body")]
    [Tooltip("Number of ants in this queue.")]
    [SerializeField] private int antCount = 5;

    [Tooltip("Ordered grid coordinates of the ant body, from HEAD (index 0) to TAIL (index N-1).")]
    [FormerlySerializedAs("initialBodyCoordinates")]
    [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();

    [Tooltip("Predefined path coordinates that this queue traverses to reach its hole. Index 0 is the starting head position, and the last index is the target Hole coordinate.")]
    [SerializeField] private List<Vector2Int> fullPathToHole = new List<Vector2Int>();

    [Header("Connected Queues")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int connectedIndex = -1;

    [Header("Second Color Mechanics")]
    [SerializeField] private bool isSecondColor = false;
    [SerializeField] private int secondColor = 0;

    [Header("Obstacles / Mechanics")]
    [SerializeField] private int pipeCount = 0;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private int lockAmount = 0;
    [SerializeField] private int holeCapacity = 0;

    // Public Properties
    public string QueueId => queueId;
    public int ColorID => colorId;
    public AntColor AntColor => ReferenceColorMapper.ToAntColor(colorId);
    public int HolderType => holderType;
    public int AntCount => (cells != null && cells.Count > 0) ? cells.Count : antCount;
    public IReadOnlyList<Vector2Int> Cells => cells;
    public IReadOnlyList<Vector2Int> InitialBodyCoordinates => cells;
    public IReadOnlyList<Vector2Int> FullPathToHole => fullPathToHole;

    public bool IsConnected => isConnected;
    public int ConnectedIndex => connectedIndex;
    public bool IsSecondColor => isSecondColor;
    public int SecondColor => secondColor;
    public AntColor SecondAntColor => ReferenceColorMapper.ToAntColor(secondColor);
    public int PipeCount => pipeCount;
    public bool IsLocked => isLocked;
    public int LockAmount => lockAmount;
    public int HoleCapacity => holeCapacity;

    public AntQueueData() { }

    public AntQueueData(string queueId, AntColor antColor, int antCount, List<Vector2Int> initialBody, List<Vector2Int> path)
    {
        this.queueId = queueId;
        this.colorId = ReferenceColorMapper.ToRawColorId(antColor);
        this.antCount = antCount;
        this.cells = initialBody != null ? new List<Vector2Int>(initialBody) : new List<Vector2Int>();
        this.fullPathToHole = path != null ? new List<Vector2Int>(path) : new List<Vector2Int>();
    }

    public AntQueueData(string queueId, int colorId, List<Vector2Int> cells, int holderType = 0, List<Vector2Int> path = null)
    {
        this.queueId = queueId;
        this.colorId = colorId;
        this.cells = cells != null ? new List<Vector2Int>(cells) : new List<Vector2Int>();
        this.antCount = this.cells.Count;
        this.holderType = holderType;
        this.fullPathToHole = path != null ? new List<Vector2Int>(path) : new List<Vector2Int>();
    }

    public AntQueueData(
        string queueId,
        int colorId,
        int holderType,
        List<Vector2Int> cells,
        bool isConnected,
        int connectedIndex,
        bool isSecondColor,
        int secondColor,
        int pipeCount,
        bool isLocked,
        int lockAmount,
        List<Vector2Int> fullPath = null,
        int holeCapacity = 0)
    {
        this.queueId = queueId;
        this.colorId = colorId;
        this.holderType = holderType;
        this.cells = cells != null ? new List<Vector2Int>(cells) : new List<Vector2Int>();
        this.antCount = this.cells.Count;
        this.isConnected = isConnected;
        this.connectedIndex = connectedIndex;
        this.isSecondColor = isSecondColor;
        this.secondColor = secondColor;
        this.pipeCount = pipeCount;
        this.isLocked = isLocked;
        this.lockAmount = lockAmount;
        this.fullPathToHole = fullPath != null ? new List<Vector2Int>(fullPath) : new List<Vector2Int>();
        this.holeCapacity = holeCapacity;
    }
}

[Serializable]
public class HoleData
{
    [Tooltip("Raw reference ColorID integer. Preserved exactly as in reference data.")]
    [FormerlySerializedAs("holeColor")]
    [SerializeField] private int colorId = 0;

    [Tooltip("Grid coordinate where this hole is located.")]
    [SerializeField] private Vector2Int gridCoordinate = Vector2Int.zero;

    [Tooltip("Target / capacity count displayed on the hole badge.")]
    [SerializeField] private int displayCount = 1;

    [Tooltip("Number of parallel rows (1 for standard tray, 2 for 2x2 box).")]
    [SerializeField] private int parallelRows = 1;

    public int ColorID => colorId;
    public AntColor HoleColor => ReferenceColorMapper.ToAntColor(colorId);
    public Vector2Int GridCoordinate => gridCoordinate;
    public int DisplayCount => displayCount;
    public int ParallelRows => Mathf.Max(1, parallelRows);

    public HoleData() { }

    public HoleData(AntColor color, Vector2Int coordinate, int count = 1, int rows = 1)
    {
        this.colorId = ReferenceColorMapper.ToRawColorId(color);
        this.gridCoordinate = coordinate;
        this.displayCount = count;
        this.parallelRows = rows;
    }

    public HoleData(int colorId, Vector2Int coordinate, int count = 1, int rows = 1)
    {
        this.colorId = colorId;
        this.gridCoordinate = coordinate;
        this.displayCount = count;
        this.parallelRows = rows;
    }
}
