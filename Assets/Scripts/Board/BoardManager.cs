using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    public int CompletedHoleCount => completedHoleCount;
    public IReadOnlyCollection<string> CompletedHoleIds => completedHoleIds;
    public IReadOnlyDictionary<Vector2Int, HoleController> HolesMap => holesMap;
    public bool EnableDiagnostics { get => enableDiagnostics; set => enableDiagnostics = value; }
    public bool IsTestInstance => isTestInstance;

    private bool isSettingUpBoard = false;
    public bool IsSettingUpBoard => isSettingUpBoard;

    public const int MAX_CONCURRENT_MOVING_QUEUES = 4;
    private readonly HashSet<AntQueueController> movingQueues = new HashSet<AntQueueController>();

    public int MovingQueuesCount => movingQueues.Count;
    public bool CanStartQueueMove => movingQueues.Count < MAX_CONCURRENT_MOVING_QUEUES;
    public bool IsQueueMoving(AntQueueController queue) => queue != null && movingQueues.Contains(queue);
    public IEnumerable<AntQueueController> MovingQueues => movingQueues;

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
    }

    /// <summary>
    /// Reconstructs the runtime board for a raw reference Level using ReferenceLevelRuntimeAdapter.
    /// </summary>
    private void SetupReferenceBoard(Level level)
    {
        var refHoles = ReferenceLevelRuntimeAdapter.ExtractHoles(level);
        var refQueues = ReferenceLevelRuntimeAdapter.ExtractPlayableQueues(level, refHoles);

        foreach (var rHole in refHoles)
        {
            SpawnReferenceHole(rHole);
        }

        foreach (var rQueue in refQueues)
        {
            SpawnReferenceAntQueue(rQueue);
        }
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
        forwardDir = Vector2Int.zero;
        targetHole = null;
        entranceCell = Vector2Int.zero;
        blockedReason = "None";

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

        // Trace straight ray forward from head
        Vector2Int curr = head + forwardDir;
        while (IsInBounds(curr))
        {
            // 1. Check stationary queues (ignore queues in the same connected moving group)
            var otherQueue = GetQueueOccupying(curr, queue);
            if (otherQueue != null && (group == null || !group.Contains(otherQueue)))
            {
                blockedReason = "OccupiedByStationaryQueue";
                return false;
            }

            // 2. Check moving queues and their path reservations
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
                        if (mq.GetReservedPathCells() != null)
                        {
                            foreach (var rCell in mq.GetReservedPathCells())
                            {
                                if (rCell == curr)
                                {
                                    blockedReason = "ReservedByMovingQueue";
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // 3. Check if cell has a holder
            HoleController hole = GetHoleAt(curr);
            if (hole != null)
            {
                // Holder encountered!
                if (hole.ColorID == queue.ColorID)
                {
                    int alreadyReserved = 0;
                    if (pendingReservations != null && pendingReservations.TryGetValue(hole, out int count))
                    {
                        alreadyReserved = count;
                    }

                    int available = hole.AvailableCapacity - alreadyReserved;
                    if (available > 0)
                    {
                        targetHole = hole;
                        entranceCell = curr;
                        blockedReason = "None";
                        return true;
                    }
                    else
                    {
                        blockedReason = "MatchingHolderFull";
                        return false;
                    }
                }
                else
                {
                    // Reached holder of wrong color!
                    blockedReason = "WrongColorHolder";
                    return false;
                }
            }

            curr += forwardDir;
        }

        // Exited board boundaries without reaching matching holder
        blockedReason = "OutOfBoundsNoHolder";
        return false;
    }

    /// <summary>
    /// Generates a straight continuous world-space trajectory across open floor from the queue's head to the destination hole.
    /// Strictly moves in forwardDir without any waypoint navigation or A* route generation.
    /// </summary>
    public List<Vector3> GenerateStraightFreeWorldPath(AntQueueController queue, Vector2Int forwardDir, HoleController targetHole, Vector2Int entranceCell)
    {
        Vector3 headWorldPos = queue.Units != null && queue.Units.Count > 0 && queue.Units[0] != null 
            ? queue.Units[0].transform.position 
            : GridToWorldPosition(queue.CurrentBodyCoordinates[0]);

        Vector3 entrancePos = GridToWorldPosition(entranceCell);
        entrancePos.y = headWorldPos.y;

        Vector3 pitPos = targetHole != null ? targetHole.transform.position : entrancePos;
        pitPos.y = headWorldPos.y;

        List<Vector3> denseRoute = new List<Vector3>();
        float stepSize = 0.05f;

        // March straight from headWorldPos to entrancePos
        float dist1 = Vector3.Distance(headWorldPos, entrancePos);
        int steps1 = Mathf.Max(1, Mathf.RoundToInt(dist1 / stepSize));
        for (int s = 0; s < steps1; s++)
        {
            denseRoute.Add(Vector3.Lerp(headWorldPos, entrancePos, (float)s / steps1));
        }

        // March from entrancePos into hole pitPos
        float dist2 = Vector3.Distance(entrancePos, pitPos);
        int steps2 = Mathf.Max(1, Mathf.RoundToInt(dist2 / stepSize));
        for (int s = 0; s <= steps2; s++)
        {
            denseRoute.Add(Vector3.Lerp(entrancePos, pitPos, (float)s / steps2));
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

        // Concurrency check: Maximum 4 queues can move simultaneously.
        // Fifth queue tap is temporarily ignored/soft-blocked WITHOUT deducting a life/heart!
        if (movingQueues.Count >= MAX_CONCURRENT_MOVING_QUEUES)
        {
            Debug.Log($"[QueueMoveBlocked] Queue={queue.QueueId} Reason=ConcurrentMoveLimit MovingQueues={movingQueues.Count} HeartPenalty=False");
            queue.PlayBlockedShake();
            return false;
        }

        var group = GetConnectedGroup(queue);

        if (movingQueues.Count + group.Count > MAX_CONCURRENT_MOVING_QUEUES)
        {
            Debug.Log($"[QueueMoveBlocked] Queue={queue.QueueId} Reason=ConcurrentMoveLimit GroupSize={group.Count} MovingQueues={movingQueues.Count} HeartPenalty=False");
            queue.PlayBlockedShake();
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
            var pendingReservations = new Dictionary<HoleController, int>();
            bool allValid = true;
            AntQueueController firstBlockedMember = null;
            string failureReason = "";

            foreach (var member in group)
            {
                if (ValidateForwardMove(member, group, pendingReservations, out var dir, out var hole, out var entrance, out var blockedReason))
                {
                    groupDirections[member] = dir;
                    groupTargetHoles[member] = hole;
                    groupEntranceCells[member] = entrance;

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
            foreach (var kvp in groupTargetHoles)
            {
                HoleController targetHole = kvp.Value;
                if (targetHole == null || !targetHole.TryReserveSlot())
                {
                    allReserved = false;
                    break;
                }
                reservedHoles.Add(targetHole);
            }

            if (!allReserved)
            {
                foreach (var hole in reservedHoles)
                {
                    hole.ReleaseReservation();
                }
                queue.PlayBlockedShake();
                return false;
            }

            // Launch straight continuous movement for all members
            foreach (var member in group)
            {
                if (member != null)
                {
                    movingQueues.Add(member);
                    var dir = groupDirections[member];
                    var hole = groupTargetHoles[member];
                    var entrance = groupEntranceCells[member];
                    member.DestinationHole = hole;
                    member.ForwardDirection = dir;
                    member.HoleEntranceCoordinate = entrance;

                    var straightRoute = GenerateStraightFreeWorldPath(member, dir, hole, entrance);
                    member.StartSmoothMovement(straightRoute, hole, entrance);
                }
            }

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
        foreach (var kvp in legacyTargetHoles)
        {
            HoleController targetHole = kvp.Value;
            if (targetHole == null || !targetHole.TryReserveSlot())
            {
                legacyAllReserved = false;
                break;
            }
            legacyReservedList.Add(targetHole);
        }

        if (!legacyAllReserved)
        {
            foreach (var hole in legacyReservedList)
            {
                hole.ReleaseReservation();
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

        GameManager.Instance?.StartTimerOnFirstValidMove(queue);
        return true;
    }

    /// <summary>
    /// Checks whether an ant queue has a valid unblocked path across open floor to a matching non-full holder.
    /// When automatic pathfinding is OFF, evaluates straight forward movement without calling AntPathfinder.
    /// </summary>
    public bool IsPathClearForQueue(AntQueueController queue)
    {
        if (queue == null) return false;

        if (!useAutomaticPathfinding)
        {
            var group = GetConnectedGroup(queue);
            var pendingReservations = new Dictionary<HoleController, int>();
            foreach (var member in group)
            {
                if (member == null) return false;
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
                        targetHole.OnEntireQueueEnteredHole();
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
            movingQueues.Remove(queue);
            Debug.Log($"[QueueMoveStoppedEarly] Queue={queue.QueueId} MovingQueues={movingQueues.Count}");
        }
    }

    public void UnregisterQueue(AntQueueController queue)
    {
        OnQueueCompleted(queue);
    }

    public void ClearBoard()
    {
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

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
