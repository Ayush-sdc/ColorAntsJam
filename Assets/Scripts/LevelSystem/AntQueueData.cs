using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AntQueueData
{
    [Tooltip("Unique ID for this ant queue in the level.")]
    [SerializeField] private string queueId = "Queue_0";

    [Tooltip("Color of this ant queue.")]
    [SerializeField] private AntColor antColor = AntColor.Blue;

    [Tooltip("Number of ants in this queue.")]
    [SerializeField] private int antCount = 5;

    [Tooltip("Occupied grid coordinates at start, ordered from HEAD (index 0) to TAIL (index N-1).")]
    [SerializeField] private List<Vector2Int> initialBodyCoordinates = new List<Vector2Int>();

    [Tooltip("Predefined path coordinates that this queue traverses to reach its hole. Index 0 is the starting head position, and the last index is the target Hole coordinate.")]
    [SerializeField] private List<Vector2Int> fullPathToHole = new List<Vector2Int>();

    public string QueueId => queueId;
    public AntColor AntColor => antColor;
    public int AntCount => antCount;
    public IReadOnlyList<Vector2Int> InitialBodyCoordinates => initialBodyCoordinates;
    public IReadOnlyList<Vector2Int> FullPathToHole => fullPathToHole;

    public AntQueueData() { }

    public AntQueueData(string queueId, AntColor antColor, int antCount, List<Vector2Int> initialBody, List<Vector2Int> path)
    {
        this.queueId = queueId;
        this.antColor = antColor;
        this.antCount = antCount;
        this.initialBodyCoordinates = initialBody;
        this.fullPathToHole = path;
    }
}

[Serializable]
public class HoleData
{
    [Tooltip("Color of the hole. Only matching colored ants can enter.")]
    [SerializeField] private AntColor holeColor = AntColor.Blue;

    [Tooltip("Grid coordinate where this hole is located.")]
    [SerializeField] private Vector2Int gridCoordinate = Vector2Int.zero;

    [Tooltip("Target / capacity count displayed on the hole badge.")]
    [SerializeField] private int displayCount = 1;

    public AntColor HoleColor => holeColor;
    public Vector2Int GridCoordinate => gridCoordinate;
    public int DisplayCount => displayCount;

    public HoleData() { }

    public HoleData(AntColor color, Vector2Int coordinate, int count = 1)
    {
        this.holeColor = color;
        this.gridCoordinate = coordinate;
        this.displayCount = count;
    }
}
