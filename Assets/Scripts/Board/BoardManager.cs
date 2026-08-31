using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Board Configuration")]
    [SerializeField] private Transform boardParent;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject antQueuePrefab;

    private int width;
    private int height;
    private float cellSize;

    private readonly Dictionary<Vector2Int, HoleController> holesMap = new Dictionary<Vector2Int, HoleController>();
    private readonly List<AntQueueController> activeQueues = new List<AntQueueController>();

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public IReadOnlyList<AntQueueController> ActiveQueues => activeQueues;

    public static event Action OnBoardStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Converts a grid coordinate (x, y) into a 3D world position centered around (0, 0, 0).
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos, float yOffset = 0f)
    {
        float x = (gridPos.x - (width - 1) * 0.5f) * cellSize;
        float z = (gridPos.y - (height - 1) * 0.5f) * cellSize;
        return new Vector3(x, yOffset, z);
    }

    /// <summary>
    /// Checks if a world position hits a grid cell coordinate.
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize + (width - 1) * 0.5f);
        int y = Mathf.RoundToInt(worldPos.z / cellSize + (height - 1) * 0.5f);
        return new Vector2Int(x, y);
    }

    public void SetupBoard(Level level)
    {
        ClearBoard();

        width = level.GridWidth;
        height = level.GridHeight;
        cellSize = level.CellSize;

        // 1. Update/Frame Background Environment
        if (GardenBackgroundManager.Instance != null)
        {
            GardenBackgroundManager.Instance.BuildEnvironment(width, height, cellSize);
        }

        // 2. Spawn Grid Visual Tiles
        SpawnGridVisuals();

        // 3. Spawn Holes
        foreach (var holeData in level.Holes)
        {
            SpawnHole(holeData);
        }

        // 4. Spawn Ant Queues
        foreach (var queueData in level.AntQueues)
        {
            SpawnAntQueue(queueData);
        }

        OnBoardStateChanged?.Invoke();
    }

    private void SpawnGridVisuals()
    {
        Transform gridHolder = new GameObject("TilesHolder").transform;
        gridHolder.SetParent(boardParent != null ? boardParent : transform);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = GridToWorldPosition(new Vector2Int(x, y), -0.05f);
                GameObject tileObj;
                if (tilePrefab != null)
                {
                    tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, gridHolder);
                }
                else
                {
                    tileObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tileObj.name = $"Tile_{x}_{y}";
                    tileObj.transform.SetParent(gridHolder);
                    tileObj.transform.position = worldPos;
                    tileObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    tileObj.transform.localScale = Vector3.one * (cellSize * 0.95f);
                    
                    var renderer = tileObj.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                        bool isChecker = (x + y) % 2 == 0;
                        // Polished garden flagstone tones: creamy warm white & soft sand
                        mat.color = isChecker ? new Color(0.96f, 0.95f, 0.93f) : new Color(0.88f, 0.86f, 0.82f);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
                        renderer.material = mat;
                    }
                }
            }
        }
    }

    private void SpawnHole(HoleData holeData)
    {
        Vector3 worldPos = GridToWorldPosition(holeData.GridCoordinate, 0.01f);
        GameObject holeObj;
        HoleController controller;

        if (holePrefab != null)
        {
            holeObj = Instantiate(holePrefab, worldPos, Quaternion.identity, transform);
            controller = holeObj.GetComponent<HoleController>();
        }
        else
        {
            holeObj = new GameObject($"Hole_{holeData.HoleColor}_{holeData.GridCoordinate.x}_{holeData.GridCoordinate.y}");
            holeObj.transform.SetParent(transform);
            holeObj.transform.position = worldPos;
            controller = holeObj.AddComponent<HoleController>();
        }

        controller.Initialize(holeData.HoleColor, holeData.GridCoordinate);
        holesMap[holeData.GridCoordinate] = controller;
    }

    private void SpawnAntQueue(AntQueueData data)
    {
        GameObject queueObj;
        AntQueueController queueController;

        if (antQueuePrefab != null)
        {
            queueObj = Instantiate(antQueuePrefab, Vector3.zero, Quaternion.identity, transform);
            queueController = queueObj.GetComponent<AntQueueController>();
        }
        else
        {
            queueObj = new GameObject($"AntQueue_{data.QueueId}_{data.AntColor}");
            queueObj.transform.SetParent(transform);
            queueController = queueObj.AddComponent<AntQueueController>();
        }

        queueController.Initialize(data);
        activeQueues.Add(queueController);
    }

    public HoleController GetHoleAt(Vector2Int coord)
    {
        holesMap.TryGetValue(coord, out var hole);
        return hole;
    }

    public AntQueueController GetQueueOccupying(Vector2Int coordinate, AntQueueController excludeQueue = null)
    {
        for (int i = 0; i < activeQueues.Count; i++)
        {
            var queue = activeQueues[i];
            if (queue == excludeQueue) continue;

            if (queue.Occupies(coordinate))
            {
                return queue;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks whether the entire remaining path for the queue is completely clear of ANY other queue's current body.
    /// </summary>
    public bool IsPathClearForQueue(AntQueueController queue)
    {
        var remainingPath = queue.GetRemainingPath();
        if (remainingPath == null || remainingPath.Count == 0) return false;

        // Check if destination matches correct color hole
        Vector2Int destination = remainingPath[remainingPath.Count - 1];
        var hole = GetHoleAt(destination);
        if (hole == null || hole.Color != queue.Color)
        {
            return false;
        }

        // Check each step on the path ahead (from head forward)
        // Note: we start from index 1 because index 0 is the queue's own current head position
        for (int i = 1; i < remainingPath.Count; i++)
        {
            Vector2Int step = remainingPath[i];
            AntQueueController blocker = GetQueueOccupying(step, queue);
            if (blocker != null)
            {
                return false; // Path blocked by another queue!
            }
        }

        return true;
    }

    public void OnQueueCompleted(AntQueueController queue)
    {
        if (activeQueues.Contains(queue))
        {
            activeQueues.Remove(queue);
            Destroy(queue.gameObject);
        }

        OnBoardStateChanged?.Invoke();
    }

    public void ClearBoard()
    {
        holesMap.Clear();
        foreach (var q in activeQueues)
        {
            if (q != null) Destroy(q.gameObject);
        }
        activeQueues.Clear();

        // Destroy any leftover children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        if (boardParent != null)
        {
            for (int i = boardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(boardParent.GetChild(i).gameObject);
            }
        }
    }
}
