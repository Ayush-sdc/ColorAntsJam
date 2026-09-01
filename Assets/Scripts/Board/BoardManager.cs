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

    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float cellSize = 1.0f;

    private readonly Dictionary<Vector2Int, HoleController> holesMap = new Dictionary<Vector2Int, HoleController>();
    private readonly List<AntQueueController> activeQueues = new List<AntQueueController>();

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public IReadOnlyList<AntQueueController> ActiveQueues => activeQueues;
    public Transform BoardParent => boardParent;

    public static event Action OnBoardStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            SafeDestroy(gameObject);
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

        Transform parentTransform = boardParent != null ? boardParent : transform;

        // 1. Update/Frame Background Environment if present
        if (GardenBackgroundManager.Instance != null)
        {
            GardenBackgroundManager.Instance.BuildEnvironment(width, height, cellSize);
        }

        // 2. Spawn Grid Visual Tiles
        SpawnGridVisuals();

        // 3. Spawn Holes with initial capacity equal to matching queue ants
        foreach (var holeData in level.Holes)
        {
            int totalAntsForColor = 0;
            if (level.AntQueues != null)
            {
                foreach (var q in level.AntQueues)
                {
                    if (q.AntColor == holeData.HoleColor)
                    {
                        totalAntsForColor += q.InitialBodyCoordinates.Count;
                    }
                }
            }

            int initialCount = totalAntsForColor > 0 ? totalAntsForColor : holeData.DisplayCount;
            SpawnHole(holeData, initialCount);
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
        Transform parentTransform = boardParent != null ? boardParent : transform;
        Transform gridHolder = new GameObject("TilesHolder").transform;
        gridHolder.SetParent(parentTransform);
        gridHolder.localPosition = Vector3.zero;

        // Clean, elegant studio floor backdrop
        GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorObj.name = "StudioFloor";
        floorObj.transform.SetParent(gridHolder);
        floorObj.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        floorObj.transform.localScale = new Vector3((width * cellSize + 20f) * 0.1f, 1f, (height * cellSize + 20f) * 0.1f);
        SafeDestroy(floorObj.GetComponent<Collider>());

        var floorRenderer = floorObj.GetComponent<MeshRenderer>();
        if (floorRenderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = AntColorPalette.StudioBackground;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
            floorRenderer.material = mat;
        }
    }

    private void SpawnHole(HoleData holeData, int initialCount)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;

        // Capsule slots span 2 cells. For right-side slots (x >= 4), offset center +0.5 cellSize; for left-side (x <= 2), offset -0.5 cellSize
        float xOffset = holeData.GridCoordinate.x >= 4 ? 0.5f : -0.5f;
        Vector3 worldPos = GridToWorldPosition(holeData.GridCoordinate, 0.01f) + new Vector3(xOffset * cellSize, 0f, 0f);
        GameObject holeObj;
        HoleController controller;

        if (holePrefab != null)
        {
            holeObj = Instantiate(holePrefab, worldPos, Quaternion.identity, parentTransform);
            controller = holeObj.GetComponent<HoleController>();
        }
        else
        {
            holeObj = new GameObject($"Hole_{holeData.HoleColor}_{holeData.GridCoordinate.x}_{holeData.GridCoordinate.y}");
            holeObj.transform.SetParent(parentTransform);
            holeObj.transform.position = worldPos;
            controller = holeObj.AddComponent<HoleController>();
        }

        controller.Initialize(holeData.HoleColor, holeData.GridCoordinate, initialCount);
        holesMap[holeData.GridCoordinate] = controller;
        
        // Register secondary cell of the 2-cell wide slot
        Vector2Int secondaryCoord = holeData.GridCoordinate + new Vector2Int(holeData.GridCoordinate.x >= 4 ? 1 : -1, 0);
        holesMap[secondaryCoord] = controller;
    }

    private void SpawnAntQueue(AntQueueData data)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;
        GameObject queueObj;
        AntQueueController queueController;

        if (antQueuePrefab != null)
        {
            queueObj = Instantiate(antQueuePrefab, Vector3.zero, Quaternion.identity, parentTransform);
            queueController = queueObj.GetComponent<AntQueueController>();
        }
        else
        {
            queueObj = new GameObject($"AntQueue_{data.QueueId}_{data.AntColor}");
            queueObj.transform.SetParent(parentTransform);
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

        // Check if destination matches correct color hole and is not full
        Vector2Int destination = remainingPath[remainingPath.Count - 1];
        var hole = GetHoleAt(destination);
        if (hole == null || hole.Color != queue.Color || hole.IsFull)
        {
            return false;
        }

        // Check each step on the path ahead (from head forward)
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
            SafeDestroy(queue.gameObject);
        }

        OnBoardStateChanged?.Invoke();
    }

    public void ClearBoard()
    {
        holesMap.Clear();
        foreach (var q in activeQueues)
        {
            if (q != null) SafeDestroy(q.gameObject);
        }
        activeQueues.Clear();

        // Destroy any leftover children in transform
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }

        // Destroy children in boardParent if separate
        if (boardParent != null && boardParent != transform)
        {
            for (int i = boardParent.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(boardParent.GetChild(i).gameObject);
            }
        }
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }
}
