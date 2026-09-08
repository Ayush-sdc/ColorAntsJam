using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BoardManager : MonoBehaviour
{
    private static BoardManager instance;
    public static BoardManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindFirstObjectByType<BoardManager>();
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("Board Configuration")]
    [SerializeField] private Transform boardParent;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject antQueuePrefab;

    [Header("Hierarchy Sync Options")]
    [Tooltip("If true, BoardManager will preserve manually placed scene Hole and Queue GameObjects instead of wiping and respawning them on Play.")]
    [SerializeField] private bool preserveSceneObjects = true;

    [SerializeField] private int width = 9;
    [SerializeField] private int height = 6;
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
        if (instance != null && instance != this && Application.isPlaying)
        {
            SafeDestroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    /// <summary>
    /// Converts a grid coordinate (x, y) into a 3D world position centered around (0, 0, 0).
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos, float yOffset = 0f)
    {
        int w = width > 0 ? width : 9;
        int h = height > 0 ? height : 6;
        float cs = cellSize > 0 ? cellSize : 1.0f;
        float x = (gridPos.x - (w - 1) * 0.5f) * cs;
        float z = (gridPos.y - (h - 1) * 0.5f) * cs;
        return new Vector3(x, yOffset, z);
    }

    /// <summary>
    /// Checks if a world position hits a grid cell coordinate.
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int w = width > 0 ? width : 9;
        int h = height > 0 ? height : 6;
        float cs = cellSize > 0 ? cellSize : 1.0f;
        int x = Mathf.RoundToInt(worldPos.x / cs + (w - 1) * 0.5f);
        int y = Mathf.RoundToInt(worldPos.z / cs + (h - 1) * 0.5f);
        return new Vector2Int(x, y);
    }

    public void SetupBoard(Level level)
    {
        instance = this;

        width = level.GridWidth;
        height = level.GridHeight;
        cellSize = level.CellSize;

        Transform parentTransform = boardParent != null ? boardParent : transform;

        // 1. Update/Frame Background Environment if present
        if (GardenBackgroundManager.Instance != null)
        {
            GardenBackgroundManager.Instance.BuildEnvironment(width, height, cellSize);
        }

        // Check if scene already contains pre-placed holes & queues (Edit Mode customization)
        HoleController[] existingHoles = parentTransform.GetComponentsInChildren<HoleController>(true);
        AntQueueController[] existingQueues = parentTransform.GetComponentsInChildren<AntQueueController>(true);

        if (preserveSceneObjects && existingHoles != null && existingHoles.Length > 0)
        {
            holesMap.Clear();
            activeQueues.Clear();

            foreach (var hole in existingHoles)
            {
                RegisterHoleInMap(hole);
                hole.EnsureVisuals();
            }

            foreach (var q in existingQueues)
            {
                if (!activeQueues.Contains(q))
                {
                    activeQueues.Add(q);
                }
            }

            // Ensure grid visuals if missing
            if (parentTransform.Find("TilesHolder") == null)
            {
                SpawnGridVisuals(level);
            }

            OnBoardStateChanged?.Invoke();
            return;
        }

        ClearBoard();

        // 2. Spawn Grid Visual Tiles & Static Ground Dots ONLY at ant coordinates
        SpawnGridVisuals(level);

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

    private void SpawnGridVisuals(Level level)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;
        Transform gridHolder = new GameObject("TilesHolder").transform;
        gridHolder.SetParent(parentTransform);
        gridHolder.localPosition = Vector3.zero;

        // 1. Clean, elegant studio floor backdrop
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

        // 2. Static Circular Ground Dots ONLY at exact coordinates where ants exist (dots = ants)
        Material dotMat = AntColorPalette.GetGroundDotMaterial();
        GameObject dotsHolder = new GameObject("GroundDotsHolder");
        dotsHolder.transform.SetParent(gridHolder);
        dotsHolder.transform.localPosition = Vector3.zero;

        HashSet<Vector2Int> antCoordinates = new HashSet<Vector2Int>();
        if (level != null && level.AntQueues != null)
        {
            foreach (var q in level.AntQueues)
            {
                if (q.InitialBodyCoordinates != null)
                {
                    foreach (var coord in q.InitialBodyCoordinates)
                    {
                        antCoordinates.Add(coord);
                    }
                }
            }
        }

        foreach (var coord in antCoordinates)
        {
            Vector3 dotWorldPos = GridToWorldPosition(coord, 0.005f);

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dot.name = $"TileDot_{coord.x}_{coord.y}";
            dot.transform.SetParent(dotsHolder.transform);
            dot.transform.position = dotWorldPos;
            dot.transform.localScale = new Vector3(0.36f * cellSize, 0.005f, 0.36f * cellSize);
            SafeDestroy(dot.GetComponent<Collider>());

            var dotRenderer = dot.GetComponent<MeshRenderer>();
            if (dotRenderer != null)
            {
                dotRenderer.material = dotMat;
                dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dotRenderer.receiveShadows = true;
            }
        }
    }

    private void SpawnHole(HoleData holeData, int initialCount)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;

        // Multiple parallel rows check (e.g. 2 parallel rows double the width)
        int rowCount = holeData.DisplayCount >= 2 ? 2 : 1;

        Vector3 worldPos;
        bool isRightSide = holeData.GridCoordinate.x >= 4;

        if (rowCount >= 2)
        {
            // For 2 parallel rows, center over the 2x2 grid cell area
            float xOffset = (holeData.GridCoordinate.x >= 4 ? 0.5f : 0.5f) * cellSize;
            // For top-left blue box (x=1, y=9), red box (x=1, y=0), green box (x=10, y=9), orange (x=5, y=0), cyan (x=10, y=2)
            if (holeData.GridCoordinate.x == 1 && holeData.GridCoordinate.y >= 8)
            {
                // Top-left blue box spans x=0..1, y=9..10 -> center at x=0.5, y=9.5
                xOffset = -0.5f * cellSize;
            }
            float zOffset = 0.5f * cellSize;
            worldPos = GridToWorldPosition(holeData.GridCoordinate, 0.01f) + new Vector3(xOffset, 0f, zOffset);
        }
        else
        {
            float xOffset = holeData.GridCoordinate.x >= 4 ? 0.5f : -0.5f;
            worldPos = GridToWorldPosition(holeData.GridCoordinate, 0.01f) + new Vector3(xOffset * cellSize, 0f, 0f);
        }

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

        controller.Initialize(holeData.HoleColor, holeData.GridCoordinate, initialCount, isRightSide, rowCount);
        RegisterHoleInMap(controller);
    }

    private void RegisterHoleInMap(HoleController controller)
    {
        if (controller == null) return;

        Vector2Int primaryCoord = controller.GridCoordinate;
        holesMap[primaryCoord] = controller;

        int rowCount = controller.ParallelRows;
        int xDir = (primaryCoord.x == 1 && primaryCoord.y >= 8) ? -1 : (primaryCoord.x >= 4 ? 1 : 1);
        Vector2Int secondaryCoord = primaryCoord + new Vector2Int(xDir, 0);
        holesMap[secondaryCoord] = controller;

        if (rowCount >= 2)
        {
            Vector2Int upperPrimary = primaryCoord + new Vector2Int(0, 1);
            Vector2Int upperSecondary = secondaryCoord + new Vector2Int(0, 1);
            holesMap[upperPrimary] = controller;
            holesMap[upperSecondary] = controller;
        }
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
