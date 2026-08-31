using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_01", menuName = "SlitherAnts/Level Data", order = 1)]
public class Level : ScriptableObject
{
    [Header("Level Meta")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelName = "Level 1";
    [SerializeField] private string difficulty = "Easy";
    [SerializeField] private float timeLimitInSeconds = 120f;

    [Header("Grid Board Settings")]
    [SerializeField] private int gridWidth = 11;
    [SerializeField] private int gridHeight = 15;
    [SerializeField] private float cellSize = 1.0f;

    [Header("Level Content")]
    [Tooltip("Target holes placed on the grid.")]
    [SerializeField] private List<HoleData> holes = new List<HoleData>();

    [Tooltip("All ant queues positioned on the grid for this level.")]
    [SerializeField] private List<AntQueueData> antQueues = new List<AntQueueData>();

    public int LevelNumber => levelNumber;
    public string LevelName => levelName;
    public string Difficulty => difficulty;
    public float TimeLimitInSeconds => timeLimitInSeconds;
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float CellSize => cellSize;
    public IReadOnlyList<HoleData> Holes => holes;
    public IReadOnlyList<AntQueueData> AntQueues => antQueues;

    public void SetLevelData(int number, string name, string diff, int width, int height, float cell, List<HoleData> holeList, List<AntQueueData> queueList)
    {
        levelNumber = number;
        levelName = name;
        difficulty = diff;
        gridWidth = width;
        gridHeight = height;
        cellSize = cell;
        holes = holeList;
        antQueues = queueList;
    }
}
