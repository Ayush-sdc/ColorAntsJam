using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using AntsJam.Ropes;

public class BoardManager : MonoBehaviour
{
    private static BoardManager instance;
    public static BoardManager Instance => instance;

    [Header("Board References")]
    [SerializeField] private Transform boardParent;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject antQueuePrefab;

    [Header("Debug Options")]
    [Tooltip("Debug only: If true, BoardManager will preserve manually placed scene Hole and Queue GameObjects instead of spawning from Level assets. Keep false for normal gameplay.")]
    [SerializeField] private bool preserveSceneObjects = false;
    [Tooltip("When false, routine board diagnostics are suppressed to keep Console clean.")]
    [SerializeField] private bool enableDiagnostics = false;
    private bool isTestInstance = false;
    [Header("Movement & Pathfinding Configuration")]
    [Tooltip("Global authoritative rule: When false, AntPathfinder / BFS / grid waypoint routing is completely disabled for all levels.")]
    [SerializeField] private bool useAutomaticPathfinding = false;
    public bool UseAutomaticPathfinding { get => useAutomaticPathfinding; set => useAutomaticPathfinding = value; }

    [Header("Runtime Grid State")]
    [SerializeField] private int width = 9;
    [SerializeField] private int height = 6;
    [SerializeField] private float cellSize = 1.0f;

    // Grid occupancy maps
    private readonly Dictionary<Vector2Int, HoleController> holesMap = new Dictionary<Vector2Int, HoleController>();
    private readonly List<AntQueueController> activeQueues = new List<AntQueueController>();
    private readonly List<HoleController> activeHoles = new List<HoleController>();
    private readonly List<PipeController> activePipes = new List<PipeController>();
    private readonly List<AntQueueController> dormantStackedQueues = new List<AntQueueController>();
    private readonly HashSet<Vector2Int> reservedRouteCells = new HashSet<Vector2Int>();
    private readonly Dictionary<AntQueueController, List<Vector2Int>> queueReservedRouteCells = new Dictionary<AntQueueController, List<Vector2Int>>();
    private readonly HashSet<string> completedHoleIds = new HashSet<string>();
    private int completedHoleCount = 0;

    [SerializeField] private Level currentLevel;

    public int Width => width;
    public int Height => height;
    public int GridWidth => width;
    public int GridHeight => height;
    public float CellSize => cellSize;
    public Level CurrentLevel => currentLevel;
    public IReadOnlyList<AntQueueController> ActiveQueues => activeQueues;
    public IReadOnlyList<HoleController> ActiveHoles => activeHoles;
    public IReadOnlyList<PipeController> ActivePipes => activePipes;
    public IReadOnlyList<AntQueueController> DormantStackedQueues => dormantStackedQueues;
    public IReadOnlyCollection<Vector2Int> ReservedRouteCells => reservedRouteCells;
    public int CompletedHoleCount => completedHoleCount;
    public IReadOnlyCollection<string> CompletedHoleIds => completedHoleIds;
    public IReadOnlyDictionary<Vector2Int, HoleController> HolesMap => holesMap;
    public bool EnableDiagnostics { get => enableDiagnostics; set => enableDiagnostics = value; }
    public bool IsTestInstance => isTestInstance;

    public void RegisterActiveQueue(AntQueueController queue)
    {
        if (queue != null)
        {
            dormantStackedQueues.Remove(queue);
            if (!activeQueues.Contains(queue))
            {
                activeQueues.Add(queue);
            }
        }
    }

    public void RemovePipe(PipeController pipe)
    {
        if (pipe != null)
        {
            activePipes.Remove(pipe);
        }
    }

    public PipeController GetPipeAt(Vector2Int cell)
    {
        for (int i = 0; i < activePipes.Count; i++)
        {
            var p = activePipes[i];
            if (p != null && p.Occupies(cell))
                return p;
        }
        return null;
    }

    private bool isSettingUpBoard = false;
    public bool IsSettingUpBoard => isSettingUpBoard;

    public const int MAX_CONCURRENT_MOVING_QUEUES = 4;
    private readonly HashSet<AntQueueController> movingQueues = new HashSet<AntQueueController>();

    public int MovingQueuesCount => movingQueues.Count;
    public bool CanStartQueueMove => movingQueues.Count < MAX_CONCURRENT_MOVING_QUEUES;
    public bool IsQueueMoving(AntQueueController queue) => queue != null && movingQueues.Contains(queue);
    public IEnumerable<AntQueueController> MovingQueues => movingQueues;
    public void AddMovingQueueForTesting(AntQueueController queue) { if (queue != null) movingQueues.Add(queue); }
    public void RemoveMovingQueueForTesting(AntQueueController queue) { if (queue != null) movingQueues.Remove(queue); }

    private readonly List<LinkedAntsRopeVisual> activeLinkedRopes = new List<LinkedAntsRopeVisual>();
    public IReadOnlyList<LinkedAntsRopeVisual> ActiveLinkedRopes => activeLinkedRopes;

    public static event Action OnBoardStateChanged;

    private void Awake()
    {
        if (instance != null && instance != this && Application.isPlaying)
        {
            SafeDestroy(gameObject);
            return;
        }
        instance = this;
        movingQueues.Clear();
    }

    private void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Converts a grid coordinate (x, y) into a 3D world position centered around (0, 0, 0).
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos, float yOffset = 0f)
    {
        return GridPointToWorldPosition(gridPos.x, gridPos.y, yOffset);
    }

    /// <summary>
    /// Converts continuous grid coordinates (gridX, gridY) into a 3D world position centered around (0, 0, 0).
    /// </summary>
    public Vector3 GridPointToWorldPosition(float gridX, float gridY, float yOffset = 0f)
    {
        int w = width > 0 ? width : 9;
        int h = height > 0 ? height : 6;
        float cs = cellSize > 0 ? cellSize : 1.0f;
        float x = (gridX - (w - 1) * 0.5f) * cs;
        float z = (gridY - (h - 1) * 0.5f) * cs;
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

    public void SetupBoard(Level level, bool isTestInstance = false)
    {
        this.isTestInstance = isTestInstance;
        this.isSettingUpBoard = true;
        if (!isTestInstance)
        {
            instance = this;
        }

        if (level == null)
        {
            Debug.LogError("[BoardManager] SetupBoard called with null Level asset!");
            this.isSettingUpBoard = false;
            return;
        }

        currentLevel = level;
        width = level.GridWidth;
        height = level.GridHeight;
        cellSize = level.CellSize > 0f ? level.CellSize : 1.0f;

        // 1. Clear all previously spawned gameplay objects
        ClearBoard();

        Transform parentTransform = boardParent != null ? boardParent : transform;

        // 2. Update/Frame Background Environment (live gameplay only)
        if (!isTestInstance && GardenBackgroundManager.Instance != null)
        {
            GardenBackgroundManager.Instance.BuildEnvironment(width, height, cellSize);
        }

        #if UNITY_EDITOR
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(level);
        #else
        string assetPath = "Runtime Build";
        #endif
        bool isRefLevel = ReferenceLevelRuntimeAdapter.IsReferenceLevel(level);

        if (enableDiagnostics)
        {
            Debug.Log($"[BoardManager Diagnostics]\n" +
                      $"  Asset Name: {level.name}\n" +
                      $"  Asset Path: {assetPath}\n" +
                      $"  GridSize: {level.GridSize} ({width}x{height})\n" +
                      $"  AntQueues Count: {level.AntQueues?.Count ?? 0}\n" +
                      $"  Holes Count: {level.Holes?.Count ?? 0}\n" +
                      $"  IsReferenceLevel: {isRefLevel}");
        }

        // 3. Spawn Holes and Queues
        if (isRefLevel)
        {
            if (enableDiagnostics) Debug.Log("[BoardManager Diagnostics] Executing Branch: SetupReferenceBoard()");
            SetupReferenceBoard(level);
        }
        else
        {
            if (enableDiagnostics) Debug.Log("[BoardManager Diagnostics] Executing Branch: SetupHandmadeBoard()");
            SetupHandmadeBoard(level);
        }

        // 4. Spawn clean neutral grid visual dots (skipping all hole cells)
        SpawnGridVisuals(level);

        // 5. Dynamic camera auto-framing for entire finalized board layout (live gameplay only)
        if (!isTestInstance)
        {
            AdjustCameraFraming(level);
        }

        // 6. Log active runtime hierarchy state only if diagnostics enabled
        if (enableDiagnostics)
        {
            Debug.Log($"[BoardManager Diagnostics] activeQueues.Count = {activeQueues.Count}");
            foreach (var q in activeQueues)
            {
                if (q == null) continue;
                var units = q.Units;
                string unitsLog = "";
                for (int i = 0; i < units.Count; i++)
                {
                    var u = units[i];
                    if (u == null) continue;
                    var renderers = u.GetComponentsInChildren<MeshRenderer>(true);
                    var mf = u.GetComponentInChildren<MeshFilter>(true);
                    string meshName = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.name : "null";
                    string matName = (renderers.Length > 0 && renderers[0].sharedMaterial != null) ? renderers[0].sharedMaterial.name : "null";
                    bool rEnabled = renderers.Length > 0 && renderers[0].enabled;
                    unitsLog += $"\n    Ant #{i} at {u.transform.position}: MeshRenderer count={renderers.Length}, enabled={rEnabled}, sharedMesh='{meshName}', sharedMaterial='{matName}'";
                }
                Debug.Log($"[BoardManager Diagnostics] Queue '{q.QueueId}': Cells={q.CurrentBodyCoordinates?.Count ?? 0}, GameObject.activeInHierarchy={q.gameObject.activeInHierarchy}, AntUnits={units?.Count ?? 0}{unitsLog}");
            }

            Debug.Log($"[BoardManager Diagnostics] holesMap.Count = {holesMap.Count}");
            foreach (var kvp in holesMap)
            {
                var h = kvp.Value;
                if (h == null) continue;
                Debug.Log($"[BoardManager Diagnostics] Hole at cell {kvp.Key}: Color={h.Color}, GridCoord={h.GridCoordinate}, WorldPos={h.transform.position}, IsRightSide={h.IsRightSide}, Rows={h.ParallelRows}, RemainingCount={h.RemainingCount}");
            }
        }

        this.isSettingUpBoard = false;
    }

    private void SetupHandmadeBoard(Level level)
    {
        if (level.Holes != null)
        {
            foreach (var holeData in level.Holes)
            {
                int matchingAnts = 0;
                if (level.AntQueues != null)
                {
                    foreach (var q in level.AntQueues)
                    {
                        if (q.AntColor == holeData.HoleColor)
                        {
                            matchingAnts += q.AntCount;
                        }
                    }
                }
                int initialCount = matchingAnts > 0 ? matchingAnts : holeData.DisplayCount;
                SpawnHole(holeData, initialCount);
            }
        }

        if (level.AntQueues != null)
        {
            foreach (var queueData in level.AntQueues)
            {
                SpawnAntQueue(queueData);
            }
        }

        ValidateHoleOverlaps();
        SetupLinkedRopes();
    }

    /// <summary>
    /// Reconstructs the runtime board for a raw reference Level using ReferenceLevelRuntimeAdapter.
    /// </summary>
    private void SetupReferenceBoard(Level level)
    {
        var refHoles = ReferenceLevelRuntimeAdapter.ExtractHoles(level);
        var refQueues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level, refHoles);
        var refPipes = ReferenceLevelRuntimeAdapter.ExtractPipes(level);

        foreach (var rHole in refHoles)
        {
            SpawnReferenceHole(rHole);
        }

        foreach (var rPipe in refPipes)
        {
            SpawnReferencePipe(rPipe);
        }

        foreach (var rQueue in refQueues)
        {
            SpawnReferenceAntQueue(rQueue);
        }

        ValidateHoleOverlaps();
        SetupLinkedRopes();
    }

    private void SpawnReferencePipe(RuntimeReferencePipe rPipe)
    {
        if (rPipe == null || rPipe.Cells == null || rPipe.Cells.Count < 2) return;

        Transform parentTransform = boardParent != null ? boardParent : transform;
        GameObject pipeObj = new GameObject($"Pipe_{rPipe.Cells[0].x}_{rPipe.Cells[0].y}");
        pipeObj.transform.SetParent(parentTransform, false);
        pipeObj.transform.localPosition = Vector3.zero;

        var pipeController = pipeObj.AddComponent<PipeController>();
        pipeController.Initialize(rPipe.Cells, rPipe.PipeCount);
        activePipes.Add(pipeController);
    }

    private void SpawnReferenceHole(RuntimeReferenceHole rHole)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;

        Vector2Int entrance = rHole.EntranceCell;
        bool isRightSide = rHole.IsRightSide;
        int rowCount = rHole.ParallelRows;

        // Position the hole at the exact geometric center of its occupied footprint cells
        float centerX = (rHole.MinX + rHole.MaxX) * 0.5f;
        float centerY = (rHole.MinY + rHole.MaxY) * 0.5f;
        Vector3 worldPos = GridPointToWorldPosition(centerX, centerY, 0.01f);

        GameObject holeObj;
        if (holePrefab != null)
        {
            holeObj = Instantiate(holePrefab, worldPos, Quaternion.identity, parentTransform);
            holeObj.name = $"Hole_{rHole.AntColor}_{rHole.MinX}_{rHole.MinY}";
        }
        else
        {
            holeObj = new GameObject($"Hole_{rHole.AntColor}_{rHole.MinX}_{rHole.MinY}");
            holeObj.transform.SetParent(parentTransform, false);
            holeObj.transform.position = worldPos;
        }

        var controller = holeObj.GetComponent<HoleController>();
        if (controller == null)
        {
            controller = holeObj.AddComponent<HoleController>();
        }

        controller.Initialize(
            rHole.ColorID,
            entrance,
            rHole.OccupiedCells,
            rHole.Capacity,
            isRightSide,
            rowCount,
            widthInCells: rHole.WidthCells,
            heightInCells: rHole.HeightCells,
            queueBased: true
        );

        // Register all occupied footprint cells in holesMap
        if (rHole.OccupiedCells != null && rHole.OccupiedCells.Count > 0)
        {
            foreach (var cell in rHole.OccupiedCells)
            {
                holesMap[cell] = controller;
            }
        }
        else
        {
            RegisterHoleInMap(controller);
        }

        activeHoles.Add(controller);
    }

    private void SpawnReferenceAntQueue(RuntimeReferenceQueue rQueue)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;

        GameObject queueObj;
        if (antQueuePrefab != null)
        {
            queueObj = Instantiate(antQueuePrefab, Vector3.zero, Quaternion.identity, parentTransform);
        }
        else
        {
            queueObj = new GameObject($"Queue_{rQueue.QueueId}");
            queueObj.transform.SetParent(parentTransform, false);
            queueObj.transform.localPosition = Vector3.zero;
        }

        var controller = queueObj.GetComponent<AntQueueController>();
        if (controller == null)
        {
            controller = queueObj.AddComponent<AntQueueController>();
        }

        AntQueueData queueData = new AntQueueData(
            rQueue.QueueId,
            rQueue.ColorID,
            1,
            rQueue.BodyCoordinates,
            rQueue.IsConnected,
            rQueue.ConnectedIndex,
            rQueue.IsSecondColor,
            rQueue.SecondColor,
            rQueue.PipeCount,
            rQueue.IsLocked,
            rQueue.LockAmount,
            rQueue.PathToHole
        );

        controller.Initialize(queueData);
        activeQueues.Add(controller);

        // Stacked ant queue support (IsSecondColor & SecondColor)
        if (rQueue.IsSecondColor)
        {
            GameObject lowerObj;
            if (antQueuePrefab != null)
            {
                lowerObj = Instantiate(antQueuePrefab, Vector3.zero, Quaternion.identity, parentTransform);
            }
            else
            {
                lowerObj = new GameObject($"Queue_{rQueue.QueueId}_layer2");
                lowerObj.transform.SetParent(parentTransform, false);
                lowerObj.transform.localPosition = Vector3.zero;
            }

            var lowerController = lowerObj.GetComponent<AntQueueController>();
            if (lowerController == null)
            {
                lowerController = lowerObj.AddComponent<AntQueueController>();
            }

            AntQueueData lowerData = new AntQueueData(
                $"{rQueue.QueueId}_layer2",
                rQueue.SecondColor,
                1,
                new List<Vector2Int>(rQueue.BodyCoordinates),
                false,
                -1,
                false,
                0,
                0,
                false,
                0,
                null
            );

            lowerController.Initialize(lowerData);
            controller.SetLowerQueueLayer(lowerController);
            dormantStackedQueues.Add(lowerController);
        }
    }

    /// <summary>
    /// Configures the fixed-view camera and calculates pan limits for the level.
    /// Dynamic level-size zoom is removed in favor of a constant baseline view.
    /// </summary>
    private void AdjustCameraFraming(Level level)
    {
        if (BoardCameraController.Instance != null)
        {
            BoardCameraController.Instance.SetupCameraForLevel(level);
            return;
        }

        var ctrl = FindFirstObjectByType<BoardCameraController>();
        if (ctrl == null && Camera.main != null)
        {
            ctrl = Camera.main.gameObject.AddComponent<BoardCameraController>();
        }
        if (ctrl != null)
        {
            ctrl.SetupCameraForLevel(level);
            return;
        }

        // Fallback: fixed camera baseline (no dynamic zoom)
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = false;
            cam.fieldOfView = 78f;
            cam.transform.position = new Vector3(0f, 13.04f, -0.17f);
            cam.transform.rotation = Quaternion.Euler(88f, 0f, 0f);
        }
    }

    private void SpawnGridVisuals(Level level)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;

        GameObject tilesHolder = new GameObject("TilesHolder");
        tilesHolder.transform.SetParent(parentTransform, false);

        Material dotMat = AntColorPalette.GetGroundDotMaterial();
        HashSet<Vector2Int> disabledSet = level != null ? level.GetCombinedDisabledCells() : new HashSet<Vector2Int>();

        // Collect initial ant body coordinates across all queues (skipping holes and disabled cells)
        HashSet<Vector2Int> antCells = new HashSet<Vector2Int>();
        if (level != null && level.AntQueues != null)
        {
            foreach (var q in level.AntQueues)
            {
                if (q.InitialBodyCoordinates != null)
                {
                    foreach (var cell in q.InitialBodyCoordinates)
                    {
                        if (!holesMap.ContainsKey(cell) && !disabledSet.Contains(cell))
                        {
                            antCells.Add(cell);
                        }
                    }
                }
            }
        }

        foreach (var pos in antCells)
        {
            Vector3 worldPos = GridToWorldPosition(pos, 0.004f);
            CreateProceduralGridDot(tilesHolder.transform, worldPos, dotMat);
        }
    }

    private void CreateProceduralGridDot(Transform parent, Vector3 worldPos, Material dotMat)
    {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dot.name = "GridDot";
        dot.transform.SetParent(parent, false);
        dot.transform.position = worldPos;
        dot.transform.localScale = new Vector3(0.28f, 0.001f, 0.28f);

        SafeDestroy(dot.GetComponent<Collider>());

        var dotRenderer = dot.GetComponent<MeshRenderer>();
        if (dotRenderer != null && dotMat != null)
        {
            dotRenderer.material = dotMat;
            dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dotRenderer.receiveShadows = false;
        }
    }

    private void SpawnHole(HoleData holeData, int initialCount)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;

        int rowCount = holeData.ParallelRows;
        bool isRightSide = (rowCount >= 2) ? (holeData.GridCoordinate.x >= width / 2) : (holeData.GridCoordinate.x >= 4);

        Vector3 worldPos;
        if (rowCount >= 2)
        {
            float xOffset = 0.5f * cellSize;
            float zOffset = 0.5f * cellSize;
            worldPos = GridToWorldPosition(holeData.GridCoordinate, 0.01f) + new Vector3(xOffset, 0f, zOffset);
        }
        else
        {
            float xOffset = isRightSide ? 0.5f : -0.5f;
            worldPos = GridToWorldPosition(holeData.GridCoordinate, 0.01f) + new Vector3(xOffset * cellSize, 0f, 0f);
        }

        GameObject holeObj;
        if (holePrefab != null)
        {
            holeObj = Instantiate(holePrefab, worldPos, Quaternion.identity, parentTransform);
        }
        else
        {
            holeObj = new GameObject($"Hole_{holeData.HoleColor}_{holeData.GridCoordinate.x}_{holeData.GridCoordinate.y}");
            holeObj.transform.SetParent(parentTransform, false);
            holeObj.transform.position = worldPos;
        }

        var controller = holeObj.GetComponent<HoleController>();
        if (controller == null)
        {
            controller = holeObj.AddComponent<HoleController>();
        }

        controller.Initialize(holeData.ColorID, holeData.GridCoordinate, initialCount, isRightSide, rowCount);
        activeHoles.Add(controller);
        RegisterHoleInMap(controller);
    }

    private void RegisterHoleInMap(HoleController controller)
    {
        if (controller == null) return;

        if (controller.OccupiedCells != null && controller.OccupiedCells.Count > 0)
        {
            foreach (var cell in controller.OccupiedCells)
            {
                holesMap[cell] = controller;
            }
            return;
        }

        Vector2Int primaryCoord = controller.GridCoordinate;
        holesMap[primaryCoord] = controller;

        int rowCount = controller.ParallelRows;
        if (rowCount >= 2)
        {
            holesMap[primaryCoord + new Vector2Int(1, 0)] = controller;
            holesMap[primaryCoord + new Vector2Int(0, 1)] = controller;
            holesMap[primaryCoord + new Vector2Int(1, 1)] = controller;
        }
        else
        {
            int xDir = controller.IsRightSide ? 1 : -1;
            Vector2Int secondaryCoord = primaryCoord + new Vector2Int(xDir, 0);
            holesMap[secondaryCoord] = controller;
        }
    }

    private void SpawnAntQueue(AntQueueData queueData)
    {
        Transform parentTransform = boardParent != null ? boardParent : transform;

        GameObject queueObj;
        if (antQueuePrefab != null)
        {
            queueObj = Instantiate(antQueuePrefab, Vector3.zero, Quaternion.identity, parentTransform);
        }
        else
        {
            queueObj = new GameObject($"Queue_{queueData.QueueId}");
            queueObj.transform.SetParent(parentTransform, false);
            queueObj.transform.localPosition = Vector3.zero;
        }

        var controller = queueObj.GetComponent<AntQueueController>();
        if (controller == null)
        {
            controller = queueObj.AddComponent<AntQueueController>();
        }

        controller.Initialize(queueData);
        activeQueues.Add(controller);
    }

    public HoleController GetHoleAt(Vector2Int coord)
    {
        if (!holesMap.TryGetValue(coord, out var hole))
        {
            return null;
        }

        if (hole == null || hole.IsCompleted)
        {
            holesMap.Remove(coord);
            return null;
        }

        return hole;
    }

    public AntQueueController GetQueueOccupying(Vector2Int coord, AntQueueController ignoreQueue = null)
    {
        foreach (var q in activeQueues)
        {
            if (q == null || q == ignoreQueue) continue;
            if (q.Occupies(coord))
            {
                return q;
            }
        }
        return null;
    }

    public bool IsCellFree(Vector2Int coord)
    {
        if (coord.x < 0 || coord.x >= width || coord.y < 0 || coord.y >= height)
        {
            return false;
        }

        if (currentLevel != null && currentLevel.GetCombinedDisabledCells().Contains(coord))
        {
            return false;
        }

        if (GetHoleAt(coord) != null) return false;
        if (GetQueueOccupying(coord) != null) return false;

        return true;
    }

    /// <summary>
    /// Returns all active ant queues that belong to the same connected group as the given queue.
    /// If the queue is not connected, returns a list containing only the queue itself.
    /// </summary>
    public List<AntQueueController> GetConnectedGroup(AntQueueController queue)
    {
        var group = new List<AntQueueController>();
        if (queue == null) return group;

        if (!queue.IsConnected || queue.ConnectedIndex < 0)
        {
            group.Add(queue);
            return group;
        }

        int targetIndex = queue.ConnectedIndex;
        foreach (var q in activeQueues)
        {
            if (q != null && q.IsConnected && q.ConnectedIndex == targetIndex)
            {
                group.Add(q);
            }
        }

        if (group.Count == 0)
        {
            group.Add(queue);
        }

        return group;
    }

    public bool IsInBounds(Vector2Int coord)
    {
        return coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }

    // =========================================================================
    // WALKABLE BOARD BOUNDARY & FREE OPEN-FLOOR MOVEMENT SYSTEM
    // =========================================================================

    /// <summary>
    /// Returns the authoritative walkable world bounds of the board floor.
    /// Ants may navigate freely anywhere inside this boundary.
    /// </summary>
    public Bounds GetWalkableBounds(float margin = 0.15f)
    {
        int w = width > 0 ? width : 9;
        int h = height > 0 ? height : 6;
        float cs = cellSize > 0 ? cellSize : 1.0f;
        float totalWidth = w * cs;
        float totalHeight = h * cs;
        Vector3 size = new Vector3(Mathf.Max(0.1f, totalWidth - margin * 2f), 2f, Mathf.Max(0.1f, totalHeight - margin * 2f));
        return new Bounds(Vector3.zero, size);
    }

    /// <summary>
    /// Checks whether a continuous 3D world position is inside the playable board floor boundary.
    /// </summary>
    public bool IsWorldPosInsideBoard(Vector3 worldPos, float margin = 0.15f)
    {
        Bounds b = GetWalkableBounds(margin);
        return worldPos.x >= b.min.x && worldPos.x <= b.max.x &&
               worldPos.z >= b.min.z && worldPos.z <= b.max.z;
    }

    /// <summary>
    /// Authoritative blocker filter: ONLY Holes and other Ant Queues can block ant path.
    /// Everything else (dots, props, environment, decorations, floor, VFX, UI) returns false.
    /// </summary>
    public bool IsBlockingForAntQueue(GameObject obj, AntQueueController currentQueue, HoleController targetHole)
    {
        if (obj == null) return false;

        var hole = obj.GetComponentInParent<HoleController>();
        if (hole != null)
        {
            return hole != targetHole; // Non-target holes block; target hole allows approach/entry
        }

        var queue = obj.GetComponentInParent<AntQueueController>();
        if (queue != null)
        {
            return queue != currentQueue; // Other queues block; own queue does not block
        }

        var unit = obj.GetComponentInParent<AntUnit>();
        if (unit != null)
        {
            var parentQueue = unit.GetComponentInParent<AntQueueController>();
            return parentQueue != null && parentQueue != currentQueue;
        }

        return false;
    }

    /// <summary>
    /// Evaluates if a continuous 3D world position on the floor is traversable.
    /// Core Rule: ONLY Holes/Holders and other Ant Queues can block ant movement.
    /// Floor dots, grid markers, decorative objects, and environment meshes are 100% ignored.
    /// </summary>
    public bool IsWorldPosWalkable(Vector3 worldPos, AntQueueController currentQueue, HoleController targetHole, float antRadius = 0.28f)
    {
        // 1. Playable Board Boundary (containment on playable floor)
        if (!IsWorldPosInsideBoard(worldPos, antRadius))
        {
            return false;
        }

        float cs = cellSize > 0 ? cellSize : 1.0f;
        float blockDistSqr = (antRadius + cs * 0.38f) * (antRadius + cs * 0.38f);

        // 2. Only Holes/Holders block (non-target or wrong color holders)
        if (holesMap != null)
        {
            foreach (var kvp in holesMap)
            {
                var h = kvp.Value;
                if (h != null && h != targetHole)
                {
                    Vector3 cellPos = GridToWorldPosition(kvp.Key);
                    cellPos.y = worldPos.y;
                    if ((worldPos - cellPos).sqrMagnitude < blockDistSqr)
                    {
                        return false;
                    }
                }
            }
        }

        // 3. Only Other Ant Queues block (stationary and moving units of other queues)
        if (activeQueues != null)
        {
            float antDistSqr = (antRadius + antRadius) * (antRadius + antRadius);
            foreach (var q in activeQueues)
            {
                if (q == null || q == currentQueue) continue;
                if (q.Units != null)
                {
                    foreach (var u in q.Units)
                    {
                        if (u != null)
                        {
                            Vector3 uPos = u.transform.position;
                            uPos.y = worldPos.y;
                            if ((worldPos - uPos).sqrMagnitude < antDistSqr)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
        }

        // Everything else (dots, props, environment, decorations, floor, VFX, UI) is WALKABLE
        return true;
    }

    /// <summary>
    /// Checks whether a straight line segment across open floor between two world positions is free of genuine blockers (holes and other queues).
    /// </summary>
    public bool IsLineOfSightClear(Vector3 from, Vector3 to, AntQueueController currentQueue, HoleController targetHole, float antRadius = 0.28f)
    {
        float dist = Vector3.Distance(from, to);
        if (dist < 0.01f) return true;

        float step = 0.20f;
        int checks = Mathf.Max(2, Mathf.CeilToInt(dist / step));
        for (int i = 1; i < checks; i++)
        {
            float t = (float)i / checks;
            Vector3 pt = Vector3.Lerp(from, to, t);
            if (!IsWorldPosWalkable(pt, currentQueue, targetHole, antRadius))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Converts a topological cell path into a smooth, continuous world-space trajectory across open floor.
    /// Strictly guarantees that the first segment travels directly along headForwardWorld before any turning.
    /// </summary>
    public List<Vector3> GenerateSmoothFreeWorldPath(List<Vector2Int> cellPath, AntQueueController queue, HoleController targetHole)
    {
        if (cellPath == null || cellPath.Count == 0) return new List<Vector3>();

        Vector3 headWorldPos = queue.Units != null && queue.Units.Count > 0 && queue.Units[0] != null 
            ? queue.Units[0].transform.position 
            : GridToWorldPosition(cellPath[0]);

        // 1. Authoritative world-space forward vector derived from gameplay root / head unit
        Vector3 headForward = queue.GetHeadForwardWorld();
        headForward.y = 0f;
        if (headForward.sqrMagnitude < 0.001f)
        {
            headForward = Vector3.forward;
        }
        else
        {
            headForward.Normalize();
        }

        // 2. Minimum straight forward distance before beginning any turns (0.75 cell width)
        float cs = cellSize > 0 ? cellSize : 1.0f;
        float minForwardDistance = cs * 0.75f;
        Vector3 initialForwardPoint = headWorldPos + headForward * minForwardDistance;
        initialForwardPoint.y = headWorldPos.y;

        List<Vector3> waypoints = new List<Vector3>();
        waypoints.Add(headWorldPos);          // Point 0: Head origin
        waypoints.Add(initialForwardPoint);   // Point 1: Guaranteed straight-forward progression

        for (int i = 1; i < cellPath.Count; i++)
        {
            waypoints.Add(GridToWorldPosition(cellPath[i]));
        }

        if (targetHole != null)
        {
            Vector3 pitPos = targetHole.transform.position;
            pitPos.y = headWorldPos.y;
            Vector3 entrancePos = GridToWorldPosition(cellPath[cellPath.Count - 1]);
            waypoints[waypoints.Count - 1] = entrancePos;
            waypoints.Add(pitPos);
        }

        // 3. String pulling: Keep Point 0 and Point 1 fixed (straight-forward launch).
        // Line-of-sight shortcutting operates strictly from Point 1 onwards across open floor.
        List<Vector3> pulledWaypoints = new List<Vector3>();
        pulledWaypoints.Add(waypoints[0]);
        pulledWaypoints.Add(waypoints[1]);

        int currIdx = 1;
        while (currIdx < waypoints.Count - 1)
        {
            int nextIdx = waypoints.Count - 1;
            for (; nextIdx > currIdx + 1; nextIdx--)
            {
                if (IsLineOfSightClear(pulledWaypoints[pulledWaypoints.Count - 1], waypoints[nextIdx], queue, targetHole))
                {
                    break;
                }
            }
            pulledWaypoints.Add(waypoints[nextIdx]);
            currIdx = nextIdx;
        }

        // 4. Subdivide into dense continuous world positions (step size ~ 0.05 units)
        List<Vector3> denseRoute = new List<Vector3>();
        float stepSize = 0.05f;

        for (int i = 0; i < pulledWaypoints.Count - 1; i++)
        {
            Vector3 p0 = pulledWaypoints[i];
            Vector3 p1 = pulledWaypoints[i + 1];
            float dist = Vector3.Distance(p0, p1);
            int steps = Mathf.Max(1, Mathf.RoundToInt(dist / stepSize));

            for (int s = 0; s < steps; s++)
            {
                float t = (float)s / steps;
                Vector3 pt = Vector3.Lerp(p0, p1, t);
                denseRoute.Add(pt);
            }
        }
        denseRoute.Add(pulledWaypoints[pulledWaypoints.Count - 1]);

        return denseRoute;
    }

    /// <summary>
    /// Checks whether the given queue can legally move straight forward into its matching holder.
    /// Does NOT use BFS, A*, or any pathfinding.
    /// Only holes/holders and other ant queues may block movement.
    /// </summary>
    public bool ValidateForwardMove(
        AntQueueController queue, 
        List<AntQueueController> group,
        Dictionary<HoleController, int> pendingReservations,
        out Vector2Int forwardDir, 
        out HoleController targetHole, 
        out Vector2Int entranceCell, 
        out string blockedReason)
    {
        return ValidateForwardMove(queue, group, pendingReservations, out forwardDir, out targetHole, out entranceCell, out blockedReason, out _, out _);
    }

    public bool ValidateForwardMove(
        AntQueueController queue, 
        List<AntQueueController> group,
        Dictionary<HoleController, int> pendingReservations,
        out Vector2Int forwardDir, 
        out HoleController targetHole, 
        out Vector2Int entranceCell, 
        out string blockedReason,
        out PipeController traversedPipe,
        out List<Vector2Int> routeCells)
    {
        forwardDir = Vector2Int.zero;
        targetHole = null;
        entranceCell = Vector2Int.zero;
        blockedReason = "None";
        traversedPipe = null;
        routeCells = new List<Vector2Int>();

        if (queue == null || queue.CurrentBodyCoordinates == null || queue.CurrentBodyCoordinates.Count == 0)
        {
            blockedReason = "NullOrEmptyCoordinates";
            return false;
        }

        Vector2Int head = queue.CurrentBodyCoordinates[0];

        // Derive forward direction: head - neck or forwardDirection property
        if (queue.CurrentBodyCoordinates.Count >= 2)
        {
            Vector2Int neck = queue.CurrentBodyCoordinates[1];
            forwardDir = head - neck;
        }
        else if (queue.ForwardDirection != Vector2Int.zero)
        {
            forwardDir = queue.ForwardDirection;
        }
        else
        {
            Vector3 fwdWorld = queue.GetHeadForwardWorld();
            if (Mathf.Abs(fwdWorld.x) > Mathf.Abs(fwdWorld.z))
                forwardDir = fwdWorld.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                forwardDir = fwdWorld.z > 0 ? Vector2Int.up : Vector2Int.down;
        }

        // Must be one of the four cardinal directions
        if (!((forwardDir.x == 0 && Mathf.Abs(forwardDir.y) == 1) || (forwardDir.y == 0 && Mathf.Abs(forwardDir.x) == 1)))
        {
            Debug.LogError($"[ForwardMove] Non-cardinal forward direction {forwardDir} for Queue={queue.QueueId}");
            blockedReason = "NonCardinalDirection";
            return false;
        }

        HoleController destinationHolder = null;
        Vector2Int entrance = Vector2Int.zero;
        Vector2Int curr = head + forwardDir;
        Vector2Int currentDir = forwardDir;
        int maxSteps = (width * height * 2) + 100;
        int steps = 0;

        while (IsInBounds(curr) && steps++ < maxSteps)
        {
            // 1. Moving into own body?
            if (queue.Occupies(curr))
            {
                blockedReason = "BlockedByOwnBody";
                return false;
            }

            // 2. Moving into stationary queue?
            var otherQueue = GetQueueOccupying(curr, queue);
            if (otherQueue != null && (group == null || !group.Contains(otherQueue)))
            {
                blockedReason = "OccupiedByStationaryQueue";
                return false;
            }

            // 3. Moving into reserved route cell of another moving queue?
            if (reservedRouteCells.Contains(curr))
            {
                blockedReason = "ReservedByMovingRoute";
                return false;
            }

            // Check if cell is occupied by any active moving queue
            if (movingQueues != null)
            {
                foreach (var mq in movingQueues)
                {
                    if (mq != null && mq != queue && (group == null || !group.Contains(mq)))
                    {
                        if (mq.Occupies(curr))
                        {
                            blockedReason = "OccupiedByMovingQueue";
                            return false;
                        }
                    }
                }
            }

            // 4. Hit a Hole?
            HoleController h = GetHoleAt(curr);
            if (h != null)
            {
                destinationHolder = h;
                entrance = curr;
                routeCells.Add(curr);
                break;
            }

            // 5. Hit a Pipe?
            PipeController p = GetPipeAt(curr);
            if (p != null)
            {
                // Can only enter if entrance matches and direction matches
                if (traversedPipe != null || !p.CanEnterFrom(curr, currentDir))
                {
                    blockedReason = "BlockedByPipeObstacle";
                    return false;
                }

                // Legally enters pipe!
                traversedPipe = p;
                if (p.Cells != null)
                {
                    foreach (var pCell in p.Cells)
                    {
                        routeCells.Add(pCell);
                    }
                }

                Vector2Int exitCell = p.ExitCell;
                Vector2Int exitDir = p.ExitDirection;
                curr = exitCell + exitDir;
                currentDir = exitDir;
                continue;
            }

            routeCells.Add(curr);
            curr += currentDir;
        }

        if (destinationHolder == null)
        {
            blockedReason = "OutOfBoundsNoHolder";
            return false;
        }

        if (destinationHolder.ColorID != queue.ColorID)
        {
            blockedReason = "WrongColorHolder";
            return false;
        }

        int alreadyReserved = 0;
        if (pendingReservations != null && pendingReservations.TryGetValue(destinationHolder, out int count))
        {
            alreadyReserved = count;
        }

        int available = destinationHolder.AvailableCapacity - alreadyReserved;
        if (available <= 0)
        {
            blockedReason = "MatchingHolderFull";
            return false;
        }

        targetHole = destinationHolder;
        entranceCell = entrance;
        blockedReason = "None";
        return true;
    }

    /// <summary>
    /// Generates a straight continuous world-space trajectory across open floor from the queue's head to the destination hole.
    /// Strictly moves in forwardDir without any waypoint navigation or A* route generation.
    /// Supports side-by-side parallel approach and parallel hole entry for queues sharing the same hole.
    /// </summary>
    public List<Vector3> GenerateStraightFreeWorldPath(AntQueueController queue, Vector2Int forwardDir, HoleController targetHole, Vector2Int entranceCell)
    {
        Vector3 headWorldPos = queue.Units != null && queue.Units.Count > 0 && queue.Units[0] != null 
            ? queue.Units[0].transform.position 
            : (queue.CurrentBodyCoordinates != null && queue.CurrentBodyCoordinates.Count > 0 ? GridToWorldPosition(queue.CurrentBodyCoordinates[0]) : queue.transform.position);

        float cs = cellSize > 0 ? cellSize : 1.0f;
        Vector3 entryForward = new Vector3(forwardDir.x, 0f, forwardDir.y).normalized;
        Vector3 entryRight = Vector3.Cross(Vector3.up, entryForward).normalized;

        Vector3 entrancePos = GridToWorldPosition(entranceCell);
        entrancePos.y = headWorldPos.y;

        Vector3 entryPoint = targetHole != null 
            ? targetHole.GetHoleEntryPoint(entranceCell, forwardDir, queue) 
            : (entrancePos - entryForward * (0.50f * cs));
        entryPoint.y = headWorldPos.y;

        float leaderVisualLength = (queue != null && queue.Units.Count > 0 && queue.Units[0] != null) ? queue.Units[0].VisualLength : AntVisualConfig.Instance.LeaderVisualLength;
        Vector3 despawnPoint = targetHole != null 
            ? targetHole.GetHoleDespawnPoint(entranceCell, forwardDir, leaderVisualLength * cs, queue) 
            : (entryPoint + entryForward * (1.10f * cs));

        List<Vector3> denseRoute = new List<Vector3>();
        float stepSize = 0.04f;

        // 1. Calculate approach geometry
        // Parallel entry zone: A dedicated straight channel of length `approachZoneLength` directly in front of the hole rim.
        float totalForwardDist = Vector3.Dot(entryPoint - headWorldPos, entryForward);
        float approachZoneLength = 1.35f * cs;

        Vector3 approachStartPos;
        float alignForwardDist;

        if (totalForwardDist > approachZoneLength + 0.1f * cs)
        {
            approachStartPos = entryPoint - entryForward * approachZoneLength;
            alignForwardDist = totalForwardDist - approachZoneLength;
        }
        else
        {
            // If already within approach zone, dedicate first 35% to smooth lateral alignment and the rest to parallel approach
            alignForwardDist = Mathf.Max(0.05f * cs, totalForwardDist * 0.35f);
            approachStartPos = entryPoint - entryForward * (totalForwardDist - alignForwardDist);
        }

        // 2. Smooth lane alignment phase: from headWorldPos to approachStartPos
        // Uses smooth cubic Hermite easing (3t^2 - 2t^3) so start and end tangents are entryForward (0 sideways jerk, 0 snap)
        int alignSteps = Mathf.Max(1, Mathf.RoundToInt(alignForwardDist / stepSize));
        Vector3 lateralDeltaVec = Vector3.Project(headWorldPos - approachStartPos, entryRight);

        for (int s = 0; s < alignSteps; s++)
        {
            float t = (float)s / alignSteps;
            float smoothT = t * t * (3f - 2f * t); // S-curve easing
            Vector3 forwardProgression = Vector3.Lerp(headWorldPos, headWorldPos + entryForward * alignForwardDist, t);
            Vector3 pt = forwardProgression - lateralDeltaVec * smoothT;
            pt.y = headWorldPos.y;
            denseRoute.Add(pt);
        }

        // 3. Parallel approach phase: from approachStartPos to entryPoint
        // 100% strictly parallel to entryForward, perfectly aligned with assigned lane, 0 degree angle between queues
        float parallelDist = Vector3.Distance(approachStartPos, entryPoint);
        int parallelSteps = Mathf.Max(1, Mathf.RoundToInt(parallelDist / stepSize));
        for (int s = 0; s < parallelSteps; s++)
        {
            float t = (float)s / parallelSteps;
            Vector3 pt = Vector3.Lerp(approachStartPos, entryPoint, t);
            pt.y = headWorldPos.y;
            denseRoute.Add(pt);
        }

        // 4. Parallel entry and swallowing phase: from entryPoint into deep despawnPoint
        // Continues strictly on the same lane inward into the recessed pit
        float despawnDist = Vector3.Distance(entryPoint, despawnPoint);
        int despawnSteps = Mathf.Max(1, Mathf.RoundToInt(despawnDist / stepSize));
        for (int s = 0; s <= despawnSteps; s++)
        {
            float t = (float)s / despawnSteps;
            denseRoute.Add(Vector3.Lerp(entryPoint, despawnPoint, t));
        }

        return denseRoute;
    }

    /// <summary>
    /// Generates a continuous world-space trajectory guiding an ant queue through a pipe to its destination hole.
    /// </summary>
    public List<Vector3> GeneratePipeFreeWorldPath(AntQueueController queue, Vector2Int forwardDir, PipeController pipe, HoleController targetHole, Vector2Int entranceCell)
    {
        Vector3 headWorldPos = queue.Units != null && queue.Units.Count > 0 && queue.Units[0] != null 
            ? queue.Units[0].transform.position 
            : (queue.CurrentBodyCoordinates != null && queue.CurrentBodyCoordinates.Count > 0 ? GridToWorldPosition(queue.CurrentBodyCoordinates[0]) : queue.transform.position);

        float cs = cellSize > 0 ? cellSize : 1.0f;
        float stepSize = 0.04f;
        List<Vector3> denseRoute = new List<Vector3>();

        // 1. From headWorldPos to pipe entrance
        Vector3 pipeEntrancePos = GridToWorldPosition(pipe.EntranceCell);
        pipeEntrancePos.y = headWorldPos.y;
        float distToPipe = Vector3.Distance(headWorldPos, pipeEntrancePos);
        int stepsToPipe = Mathf.Max(1, Mathf.RoundToInt(distToPipe / stepSize));
        for (int s = 0; s < stepsToPipe; s++)
        {
            float t = (float)s / stepsToPipe;
            Vector3 pt = Vector3.Lerp(headWorldPos, pipeEntrancePos, t);
            pt.y = headWorldPos.y;
            denseRoute.Add(pt);
        }

        // 2. Along pipe centerline cells
        if (pipe.Cells != null && pipe.Cells.Count >= 2)
        {
            for (int i = 0; i < pipe.Cells.Count - 1; i++)
            {
                Vector3 p0 = GridToWorldPosition(pipe.Cells[i]);
                Vector3 p1 = GridToWorldPosition(pipe.Cells[i + 1]);
                p0.y = headWorldPos.y;
                p1.y = headWorldPos.y;
                float segDist = Vector3.Distance(p0, p1);
                int segSteps = Mathf.Max(1, Mathf.RoundToInt(segDist / stepSize));
                for (int s = 0; s < segSteps; s++)
                {
                    float t = (float)s / segSteps;
                    Vector3 pt = Vector3.Lerp(p0, p1, t);
                    pt.y = headWorldPos.y;
                    denseRoute.Add(pt);
                }
            }
        }

        // 3. From pipe exit to hole
        Vector2Int exitDir = pipe.ExitDirection;
        Vector3 exitForward = new Vector3(exitDir.x, 0f, exitDir.y).normalized;
        Vector3 pipeExitPos = GridToWorldPosition(pipe.ExitCell);
        pipeExitPos.y = headWorldPos.y;

        Vector3 entrancePos = GridToWorldPosition(entranceCell);
        entrancePos.y = headWorldPos.y;

        Vector3 entryPoint = targetHole != null 
            ? targetHole.GetHoleEntryPoint(entranceCell, exitDir, queue) 
            : (entrancePos - exitForward * (0.50f * cs));
        entryPoint.y = headWorldPos.y;

        float leaderVisualLength = (queue != null && queue.Units.Count > 0 && queue.Units[0] != null) ? queue.Units[0].VisualLength : AntVisualConfig.Instance.LeaderVisualLength;
        Vector3 despawnPoint = targetHole != null 
            ? targetHole.GetHoleDespawnPoint(entranceCell, exitDir, leaderVisualLength * cs, queue) 
            : (entryPoint + exitForward * (1.10f * cs));

        float distToHole = Vector3.Distance(pipeExitPos, entryPoint);
        int stepsToHole = Mathf.Max(1, Mathf.RoundToInt(distToHole / stepSize));
        for (int s = 0; s < stepsToHole; s++)
        {
            float t = (float)s / stepsToHole;
            Vector3 pt = Vector3.Lerp(pipeExitPos, entryPoint, t);
            pt.y = headWorldPos.y;
            denseRoute.Add(pt);
        }

        // 4. Hole swallowing into pit
        float despawnDist = Vector3.Distance(entryPoint, despawnPoint);
        int despawnSteps = Mathf.Max(1, Mathf.RoundToInt(despawnDist / stepSize));
        for (int s = 0; s <= despawnSteps; s++)
        {
            float t = (float)s / despawnSteps;
            denseRoute.Add(Vector3.Lerp(entryPoint, despawnPoint, t));
        }

        return denseRoute;
    }

    /// <summary>
    /// Attempts to launch an ant queue (or all members of its connected group) along a straight free-floor world route to its matching holder.
    /// If valid, reserves destination capacity, launches movement, and starts the timer.
    /// If invalid, plays blocked feedback and deducts 1 life.
    /// </summary>
    public bool TryStartMove(AntQueueController queue)
    {
        if (queue == null || queue.IsMoving || movingQueues.Contains(queue)) return false;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;

        var group = GetConnectedGroup(queue);

        // Check if any member of the group is locked
        foreach (var member in group)
        {
            if (member != null && member.IsLocked)
            {
                foreach (var m in group)
                {
                    if (m != null)
                    {
                        if (m.IsLocked) m.TriggerLockedFeedback();
                        else m.PlayBlockedShake();
                    }
                }
                return false;
            }
        }

        // Concurrency check: Maximum 4 queues can move simultaneously.
        // A single linked group of ANY size (> 4) is allowed to move when movingQueues.Count == 0!
        if (movingQueues.Count > 0 && movingQueues.Count + group.Count > MAX_CONCURRENT_MOVING_QUEUES)
        {
            Debug.Log($"[QueueMoveBlocked] Queue={queue.QueueId} Reason=ConcurrentMoveLimit GroupSize={group.Count} MovingQueues={movingQueues.Count} HeartPenalty=False");
            foreach (var member in group)
            {
                if (member != null) member.PlayBlockedShake();
            }
            return false;
        }

        // Check if any member in the group is already moving
        foreach (var member in group)
        {
            if (member == null || member.IsMoving || movingQueues.Contains(member)) return false;
        }

        // Central global rule: AUTOMATIC PATHFINDING = OFF
        if (!useAutomaticPathfinding)
        {
            var groupDirections = new Dictionary<AntQueueController, Vector2Int>();
            var groupTargetHoles = new Dictionary<AntQueueController, HoleController>();
            var groupEntranceCells = new Dictionary<AntQueueController, Vector2Int>();
            var groupPipes = new Dictionary<AntQueueController, PipeController>();
            var groupRouteCells = new Dictionary<AntQueueController, List<Vector2Int>>();
            var pendingReservations = new Dictionary<HoleController, int>();
            bool allValid = true;
            AntQueueController firstBlockedMember = null;
            string failureReason = "";

            foreach (var member in group)
            {
                if (ValidateForwardMove(member, group, pendingReservations, out var dir, out var hole, out var entrance, out var blockedReason, out var pipe, out var rCells))
                {
                    groupDirections[member] = dir;
                    groupTargetHoles[member] = hole;
                    groupEntranceCells[member] = entrance;
                    groupPipes[member] = pipe;
                    groupRouteCells[member] = rCells;

                    if (!pendingReservations.ContainsKey(hole))
                    {
                        pendingReservations[hole] = 0;
                    }
                    pendingReservations[hole]++;
                }
                else
                {
                    allValid = false;
                    firstBlockedMember = member;
                    failureReason = blockedReason;
                    break;
                }
            }

            // Validate that routes in the group do not cross each other
            if (allValid && group.Count > 1)
            {
                HashSet<Vector2Int> claimedCells = new HashSet<Vector2Int>();
                foreach (var member in group)
                {
                    var rCells = groupRouteCells[member];
                    var entrance = groupEntranceCells[member];
                    foreach (var c in rCells)
                    {
                        if (c != entrance && claimedCells.Contains(c))
                        {
                            allValid = false;
                            firstBlockedMember = member;
                            failureReason = "GroupRoutesCross";
                            break;
                        }
                        claimedCells.Add(c);
                    }
                    if (!allValid) break;
                }
            }

            if (!allValid)
            {
                if (firstBlockedMember != null)
                {
                    Debug.Log($"[ForwardMoveBlocked] Queue={firstBlockedMember.QueueId} Reason={failureReason} PathfindingFallback=False");
                }

                foreach (var member in group)
                {
                    if (member != null) member.TriggerBlockedFeedback();
                }

                // Deduct exactly ONE life for this player move attempt (invalid move)
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoseLife(InvalidMoveReason.NoValidPath);
                }
                return false;
            }

            // Reserve destination slots
            bool allReserved = true;
            List<HoleController> reservedHoles = new List<HoleController>();
            List<AntQueueController> reservedMembers = new List<AntQueueController>();

            // Group members by target hole so same-hole members preserve lateral left-to-right order without crossing
            var membersByHole = new Dictionary<HoleController, List<AntQueueController>>();
            foreach (var kvp in groupTargetHoles)
            {
                var member = kvp.Key;
                var hole = kvp.Value;
                if (!membersByHole.TryGetValue(hole, out var mList))
                {
                    mList = new List<AntQueueController>();
                    membersByHole[hole] = mList;
                }
                mList.Add(member);
            }

            foreach (var kvp in membersByHole)
            {
                var mList = kvp.Value;
                if (mList.Count >= 2)
                {
                    Vector3 fwd = new Vector3(groupDirections[mList[0]].x, 0f, groupDirections[mList[0]].y).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                    mList.Sort((a, b) =>
                    {
                        float dotA = Vector3.Dot(a.transform.position, right);
                        float dotB = Vector3.Dot(b.transform.position, right);
                        return dotA.CompareTo(dotB);
                    });
                }

                foreach (var member in mList)
                {
                    HoleController targetHole = kvp.Key;
                    if (targetHole == null || !targetHole.TryReserveSlot(member))
                    {
                        allReserved = false;
                        break;
                    }
                    reservedHoles.Add(targetHole);
                    reservedMembers.Add(member);
                }
                if (!allReserved) break;
            }

            if (!allReserved)
            {
                for (int i = 0; i < reservedHoles.Count; i++)
                {
                    reservedHoles[i].ReleaseReservation(reservedMembers[i]);
                }
                foreach (var member in group)
                {
                    if (member != null) member.PlayBlockedShake();
                }
                return false;
            }

            // Launch continuous movement for all members
            foreach (var member in group)
            {
                if (member != null)
                {
                    movingQueues.Add(member);
                    var dir = groupDirections[member];
                    var hole = groupTargetHoles[member];
                    var entrance = groupEntranceCells[member];
                    var pipe = groupPipes[member];
                    var rCells = groupRouteCells[member];

                    member.DestinationHole = hole;
                    member.ForwardDirection = dir;
                    member.HoleEntranceCoordinate = entrance;
                    member.TraversedPipe = pipe;
                    if (pipe != null) pipe.IsPipeInUse = true;

                    // Reserve transit cells
                    foreach (var c in rCells)
                    {
                        reservedRouteCells.Add(c);
                    }
                    queueReservedRouteCells[member] = new List<Vector2Int>(rCells);

                    List<Vector3> route;
                    if (pipe != null)
                    {
                        route = GeneratePipeFreeWorldPath(member, dir, pipe, hole, entrance);
                    }
                    else
                    {
                        route = GenerateStraightFreeWorldPath(member, dir, hole, entrance);
                    }

                    member.StartSmoothMovement(route, hole, entrance);
                }
            }

            UpdateParallelRopes();

            GameManager.Instance?.StartTimerOnFirstValidMove(queue);
            return true;
        }

        // Validate path for every member in the connected group (pathfinding fallback if enabled)
        var groupPaths = new Dictionary<AntQueueController, List<Vector2Int>>();
        var legacyTargetHoles = new Dictionary<AntQueueController, HoleController>();
        var legacyEntranceCells = new Dictionary<AntQueueController, Vector2Int>();
        var legacyReservations = new Dictionary<HoleController, int>();
        bool legacyAllValid = true;
        AntQueueController legacyBlockedMember = null;

        foreach (var member in group)
        {
            var path = AntPathfinder.FindPath(this, member, out HoleController hole, out Vector2Int entrance);
            if (path != null && path.Count >= 2 && hole != null && !hole.IsFull)
            {
                int alreadyReserved = 0;
                if (legacyReservations.TryGetValue(hole, out int count))
                {
                    alreadyReserved = count;
                }

                if (hole.AvailableCapacity - alreadyReserved > 0)
                {
                    groupPaths[member] = path;
                    legacyTargetHoles[member] = hole;
                    legacyEntranceCells[member] = entrance;
                    legacyReservations[hole] = alreadyReserved + 1;
                }
                else
                {
                    legacyAllValid = false;
                    legacyBlockedMember = member;
                    break;
                }
            }
            else
            {
                legacyAllValid = false;
                legacyBlockedMember = member;
                break;
            }
        }

        if (!legacyAllValid)
        {
            if (legacyBlockedMember != null)
            {
                Debug.Log($"[MoveBlocked] Queue={legacyBlockedMember.QueueId} Reason=NoPathOrHolderFull");
            }

            foreach (var member in group)
            {
                if (member != null)
                {
                    member.TriggerBlockedFeedback();
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife(InvalidMoveReason.NoValidPath);
            }
            return false;
        }

        // Reserve destination slots
        bool legacyAllReserved = true;
        List<HoleController> legacyReservedList = new List<HoleController>();
        List<AntQueueController> legacyReservedMembers = new List<AntQueueController>();
        foreach (var kvp in legacyTargetHoles)
        {
            AntQueueController member = kvp.Key;
            HoleController targetHole = kvp.Value;
            if (targetHole == null || !targetHole.TryReserveSlot(member))
            {
                legacyAllReserved = false;
                break;
            }
            legacyReservedList.Add(targetHole);
            legacyReservedMembers.Add(member);
        }

        if (!legacyAllReserved)
        {
            for (int i = 0; i < legacyReservedList.Count; i++)
            {
                legacyReservedList[i].ReleaseReservation(legacyReservedMembers[i]);
            }
            queue.PlayBlockedShake();
            return false;
        }

        // Launch smooth free-floor world movement for all members
        foreach (var member in group)
        {
            if (member != null)
            {
                movingQueues.Add(member);
                var cellPath = groupPaths[member];
                var hole = legacyTargetHoles[member];
                var entrance = legacyEntranceCells[member];

                var smoothWorldRoute = GenerateSmoothFreeWorldPath(cellPath, member, hole);
                member.StartSmoothMovement(smoothWorldRoute, hole, entrance);
            }
        }

        UpdateParallelRopes();

        GameManager.Instance?.StartTimerOnFirstValidMove(queue);
        return true;
    }

    /// <summary>
    /// Checks moving queues and activates GoldenRope visuals when 2 or more queues target the same hole.
    /// Strictly visual only; does not affect movement or collision.
    /// </summary>
    public void UpdateParallelRopes()
    {
        Dictionary<HoleController, List<AntQueueController>> queuesByHole = new Dictionary<HoleController, List<AntQueueController>>();
        foreach (var q in movingQueues)
        {
            if (q != null && q.DestinationHole != null)
            {
                if (!queuesByHole.TryGetValue(q.DestinationHole, out var list))
                {
                    list = new List<AntQueueController>();
                    queuesByHole[q.DestinationHole] = list;
                }
                list.Add(q);
            }
        }

        foreach (var kvp in queuesByHole)
        {
            if (kvp.Value.Count >= 2)
            {
                foreach (var q in kvp.Value)
                {
                    if (!q.IsConnected)
                    {
                        q.EnableParallelRope();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks whether an ant queue has a valid unblocked path across open floor to a matching non-full holder.
    /// When automatic pathfinding is OFF, evaluates straight forward movement without calling AntPathfinder.
    /// </summary>
    public bool IsPathClearForQueue(AntQueueController queue)
    {
        if (queue == null || queue.IsDormantStacked || dormantStackedQueues.Contains(queue)) return false;

        if (!useAutomaticPathfinding)
        {
            var group = GetConnectedGroup(queue);
            var pendingReservations = new Dictionary<HoleController, int>();
            foreach (var member in group)
            {
                if (member == null || member.IsLocked || member.IsDormantStacked) return false;
                if (!ValidateForwardMove(member, group, pendingReservations, out _, out var targetHole, out _, out _))
                {
                    return false;
                }
                if (targetHole == null) return false;

                if (!pendingReservations.ContainsKey(targetHole))
                {
                    pendingReservations[targetHole] = 0;
                }
                pendingReservations[targetHole]++;
            }
            return true;
        }

        var path = AntPathfinder.FindPath(this, queue, out HoleController legacyTarget, out _);
        return path != null && path.Count >= 2 && legacyTarget != null && !legacyTarget.IsFull;
    }

    public void RemoveCompletedHole(HoleController hole)
    {
        if (hole == null) return;

        string hId = hole.HoleId;
        if (!string.IsNullOrEmpty(hId) && completedHoleIds.Contains(hId))
        {
            return; // Idempotent: already completed
        }

        if (!string.IsNullOrEmpty(hId))
        {
            completedHoleIds.Add(hId);
        }
        completedHoleCount++;
        activeHoles.Remove(hole);

        int releasedCount = 0;
        var occupied = hole.OccupiedCells;
        if (occupied != null)
        {
            foreach (var cell in occupied)
            {
                if (holesMap.TryGetValue(cell, out var mapped) && mapped == hole)
                {
                    holesMap.Remove(cell);
                    releasedCount++;
                }
            }
        }

        // Defensive sweep for any stale mappings pointing to this exact hole
        var lingeringKeys = new List<Vector2Int>();
        foreach (var kvp in holesMap)
        {
            if (kvp.Value == hole)
            {
                lingeringKeys.Add(kvp.Key);
            }
        }
        foreach (var key in lingeringKeys)
        {
            holesMap.Remove(key);
            releasedCount++;
        }

        bool removedFromHoleMap = !holesMap.ContainsValue(hole);

        // Notify board state changed so pathfinding / win-loss conditions update immediately
        if (!isTestInstance && !isSettingUpBoard)
        {
            OnBoardStateChanged?.Invoke();
        }

        hole.PlayCompletedEffectAndDisappear(releasedCount, removedFromHoleMap);
    }

    public void OnQueueCompleted(AntQueueController queue)
    {
        if (queue != null)
        {
            movingQueues.Remove(queue);
            Debug.Log($"[QueueMoveCompleted] Queue={queue.QueueId} MovingQueues={movingQueues.Count}");

            // Release reserved route cells
            if (queueReservedRouteCells.TryGetValue(queue, out var rCells))
            {
                foreach (var c in rCells)
                {
                    reservedRouteCells.Remove(c);
                }
                queueReservedRouteCells.Remove(queue);
            }

            // Pipe usage decrement
            if (queue.TraversedPipe != null)
            {
                queue.TraversedPipe.ConsumeUse();
                queue.TraversedPipe.IsPipeInUse = false;
                queue.TraversedPipe = null;
            }

            // Lock decrement:
            // A counter decreases only after a complete queue reaches its matching hole.
            // (A linked pair has 2 queues, so completing both queues decrements active locks by 2).
            foreach (var q in activeQueues)
            {
                if (q != null && q.IsLocked)
                {
                    q.DecrementLock(1);
                }
            }

            bool wasActive = activeQueues.Contains(queue);
            if (wasActive)
            {
                activeQueues.Remove(queue);

                // Destination hole is notified by AntQueueController.OnEntireQueueEnteredHole()
                // when the tail finishes its swallow animation. Fallback here only if queue.DestinationHole was null:
                if (queue.DestinationHole == null)
                {
                    HoleController targetHole = null;
                    if (queue.CurrentBodyCoordinates != null && queue.CurrentBodyCoordinates.Count > 0)
                    {
                        targetHole = GetHoleAt(queue.CurrentBodyCoordinates[0]);
                    }
                    if (targetHole == null)
                    {
                        foreach (var h in activeHoles)
                        {
                            if (h != null && !h.IsCompleted && h.ColorID == queue.ColorID)
                            {
                                targetHole = h;
                                break;
                            }
                        }
                    }

                    if (targetHole != null)
                    {
                        targetHole.OnEntireQueueEnteredHole(queue);
                    }
                }

                queue.transform.DOKill();
                SafeDestroy(queue.gameObject);
            }
        }

        if (!isTestInstance)
        {
            OnBoardStateChanged?.Invoke();
        }
    }

    public void OnQueueMoveStoppedEarly(AntQueueController queue)
    {
        if (queue != null)
        {
            queue.ReleaseReservedHole();
            movingQueues.Remove(queue);
            if (queueReservedRouteCells.TryGetValue(queue, out var rCells))
            {
                foreach (var c in rCells)
                {
                    reservedRouteCells.Remove(c);
                }
                queueReservedRouteCells.Remove(queue);
            }
            if (queue.TraversedPipe != null)
            {
                queue.TraversedPipe.IsPipeInUse = false;
                queue.TraversedPipe = null;
            }
            Debug.Log($"[QueueMoveStoppedEarly] Queue={queue.QueueId} MovingQueues={movingQueues.Count}");
        }
    }

    public void UnregisterQueue(AntQueueController queue)
    {
        OnQueueCompleted(queue);
    }

    /// <summary>
    /// Validates that no two active holes overlap logically (in OccupiedCells) or visually (in mesh bounds).
    /// </summary>
    public void ValidateHoleOverlaps()
    {
        int currentLvl = currentLevel != null ? currentLevel.LevelNumber : 1;
        int logicalOverlapCount = 0;
        int visualOverlapCount = 0;

        // 1. Logical OccupiedCells overlap check
        var claimedCells = new Dictionary<Vector2Int, HoleController>();
        foreach (var hole in activeHoles)
        {
            if (hole == null || hole.IsCompleted) continue;
            foreach (var cell in hole.OccupiedCells)
            {
                if (claimedCells.TryGetValue(cell, out var otherHole))
                {
                    logicalOverlapCount++;
                    Debug.LogError($"[LogicalHoleOverlapError] Level={currentLvl} HoleA={otherHole.name} ({otherHole.Color}) HoleB={hole.name} ({hole.Color}) OverlappingCell=({cell.x},{cell.y})");
                }
                else
                {
                    claimedCells[cell] = hole;
                }
            }
        }

        // 2. Visual bounds overlap check
        for (int i = 0; i < activeHoles.Count; i++)
        {
            var holeA = activeHoles[i];
            if (holeA == null || holeA.IsCompleted) continue;
            Bounds boundsA = holeA.CalculateVisualBounds();

            for (int j = i + 1; j < activeHoles.Count; j++)
            {
                var holeB = activeHoles[j];
                if (holeB == null || holeB.IsCompleted) continue;
                Bounds boundsB = holeB.CalculateVisualBounds();

                if (boundsA.Intersects(boundsB))
                {
                    visualOverlapCount++;
                    float overlapX = Mathf.Min(boundsA.max.x, boundsB.max.x) - Mathf.Max(boundsA.min.x, boundsB.min.x);
                    float overlapZ = Mathf.Min(boundsA.max.z, boundsB.max.z) - Mathf.Max(boundsA.min.z, boundsB.min.z);
                    Debug.LogError($"[VisualHoleOverlapError] Level={currentLvl} HoleA={holeA.name} ({holeA.Color}) HoleB={holeB.name} ({holeB.Color}) OverlapX={overlapX:F3} OverlapZ={overlapZ:F3}");
                }
            }
        }

        if (logicalOverlapCount == 0 && visualOverlapCount == 0)
        {
            Debug.Log($"[HoleOverlapValidation] Level={currentLvl} PASS: LogicalOverlaps=0 VisualOverlaps=0 ActiveHoles={activeHoles.Count}");
        }
    }

    public void ClearBoard()
    {
        ClearLinkedRopes();

        foreach (var q in activeQueues)
        {
            if (q != null)
            {
                q.ReleaseReservedHole();
                q.RopeVisual?.Cleanup();
            }
        }
        foreach (var h in activeHoles)
        {
            if (h != null) h.ResetState();
        }
        movingQueues.Clear();

        Transform parentTransform = boardParent != null ? boardParent : transform;

        Transform tiles = parentTransform.Find("TilesHolder");
        if (tiles != null)
        {
            tiles.DOKill();
            SafeDestroy(tiles.gameObject);
        }

        foreach (var hole in activeHoles)
        {
            if (hole != null)
            {
                hole.transform.DOKill();
                SafeDestroy(hole.gameObject);
            }
        }
        activeHoles.Clear();
        completedHoleIds.Clear();
        completedHoleCount = 0;

        foreach (var hole in holesMap.Values)
        {
            if (hole != null)
            {
                hole.transform.DOKill();
                SafeDestroy(hole.gameObject);
            }
        }
        holesMap.Clear();

        foreach (var q in activeQueues)
        {
            if (q != null)
            {
                q.transform.DOKill();
                SafeDestroy(q.gameObject);
            }
        }
        activeQueues.Clear();

        foreach (var p in activePipes)
        {
            if (p != null)
            {
                p.transform.DOKill();
                SafeDestroy(p.gameObject);
            }
        }
        activePipes.Clear();

        foreach (var dq in dormantStackedQueues)
        {
            if (dq != null)
            {
                dq.transform.DOKill();
                SafeDestroy(dq.gameObject);
            }
        }
        dormantStackedQueues.Clear();

        reservedRouteCells.Clear();
        queueReservedRouteCells.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null)
            {
                child.DOKill();
                SafeDestroy(child.gameObject);
            }
        }
        if (boardParent != null && boardParent != transform)
        {
            for (int i = boardParent.childCount - 1; i >= 0; i--)
            {
                var child = boardParent.GetChild(i);
                if (child != null)
                {
                    child.DOKill();
                    SafeDestroy(child.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Spawns horizontal GoldenRope connectors between linked ant queues.
    /// Generic implementation that automatically discovers any linked groups defined in level data.
    /// </summary>
    public void SetupLinkedRopes()
    {
        ClearLinkedRopes();

        var connectedGroups = new Dictionary<int, List<AntQueueController>>();
        foreach (var q in activeQueues)
        {
            if (q != null && q.IsConnected && q.ConnectedIndex >= 0)
            {
                if (!connectedGroups.TryGetValue(q.ConnectedIndex, out var list))
                {
                    list = new List<AntQueueController>();
                    connectedGroups[q.ConnectedIndex] = list;
                }
                list.Add(q);
            }
        }

        Transform parentTransform = boardParent != null ? boardParent : transform;

        foreach (var kvp in connectedGroups)
        {
            var list = kvp.Value;
            if (list.Count < 2) continue;

            if (list.Count == 2)
            {
                CreateLinkedRope(list[0], list[1], kvp.Key, parentTransform);
            }
            else
            {
                Vector3 fwd = list[0].GetHeadForwardWorld();
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                list.Sort((a, b) =>
                {
                    float dotA = Vector3.Dot(a.transform.position, right);
                    float dotB = Vector3.Dot(b.transform.position, right);
                    return dotA.CompareTo(dotB);
                });

                for (int i = 0; i < list.Count - 1; i++)
                {
                    CreateLinkedRope(list[i], list[i + 1], kvp.Key, parentTransform);
                }
            }
        }
    }

    private void CreateLinkedRope(AntQueueController a, AntQueueController b, int groupId, Transform parent)
    {
        if (a == null || b == null) return;
        GameObject ropeObj = new GameObject($"LinkedRopeGroup_{groupId}_{a.QueueId}_{b.QueueId}");
        ropeObj.transform.SetParent(parent, false);
        var visual = ropeObj.AddComponent<LinkedAntsRopeVisual>();
        visual.Initialize(a, b);
        activeLinkedRopes.Add(visual);
    }

    public void ClearLinkedRopes()
    {
        for (int i = activeLinkedRopes.Count - 1; i >= 0; i--)
        {
            var rope = activeLinkedRopes[i];
            if (rope != null)
            {
                rope.Cleanup();
                SafeDestroy(rope.gameObject);
            }
        }
        activeLinkedRopes.Clear();
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
