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

    /// <summary>
    /// Checks whether the given queue can legally move straight forward into its matching holder.
    /// Does NOT use BFS, A*, or any pathfinding.
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

        if (queue.CurrentBodyCoordinates.Count < 2)
        {
            Debug.LogError($"[ForwardMove] Single-ant queue {queue.QueueId} has no neck to derive direction and no explicit forward-direction data.");
            blockedReason = "SingleAntNoDirection";
            return false;
        }

        Vector2Int head = queue.CurrentBodyCoordinates[0];
        Vector2Int neck = queue.CurrentBodyCoordinates[1];
        forwardDir = head - neck;

        // Authoritative Rule: Must be one of the four cardinal directions
        if (!((forwardDir.x == 0 && Mathf.Abs(forwardDir.y) == 1) || (forwardDir.y == 0 && Mathf.Abs(forwardDir.x) == 1)))
        {
            Debug.LogError($"[ForwardMove] Non-cardinal forward direction {forwardDir} for Queue={queue.QueueId} (Head={head}, Neck={neck})");
            blockedReason = "NonCardinalDirection";
            return false;
        }

        // Trace straight ray forward from head
        Vector2Int curr = head + forwardDir;
        while (IsInBounds(curr))
        {
            // 1. Check disabled cells from level data
            if (currentLevel != null && currentLevel.GetCombinedDisabledCells().Contains(curr))
            {
                blockedReason = "DisabledCell";
                return false;
            }

            // 2. Check stationary queues (ignore queues in the same connected moving group)
            var otherQueue = GetQueueOccupying(curr, queue);
            if (otherQueue != null && (group == null || !group.Contains(otherQueue)))
            {
                blockedReason = "OccupiedByStationaryQueue";
                return false;
            }

            // 3. Check moving queues and their path reservations
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

            // 4. Check if cell has a holder
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
    /// Validates forward movement for a queue or its entire connected group atomically.
    /// If all members have clear straight paths to matching holders, starts forward movement for all members together.
    /// If any member is blocked, all members remain stationary, play blocked shake, and lose 1 life.
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

        // Validate straight forward movement for every member in the connected group
        var groupDirections = new Dictionary<AntQueueController, Vector2Int>();
        var groupTargetHoles = new Dictionary<AntQueueController, HoleController>();
        var groupEntranceCells = new Dictionary<AntQueueController, Vector2Int>();
        var pendingReservations = new Dictionary<HoleController, int>();
        bool allValid = true;
        AntQueueController firstBlockedMember = null;
        string failureReason = "";
        Vector2Int failureHead = Vector2Int.zero;
        Vector2Int failureDir = Vector2Int.zero;

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
                if (member.CurrentBodyCoordinates != null && member.CurrentBodyCoordinates.Count > 0)
                {
                    failureHead = member.CurrentBodyCoordinates[0];
                }
                failureDir = dir;
                break;
            }
        }

        if (!allValid)
        {
            // Blocked debug log as required
            if (firstBlockedMember != null)
            {
                Debug.Log($"[ForwardMoveBlocked] Queue={firstBlockedMember.QueueId} Head={failureHead} Direction={failureDir} Next={failureHead + failureDir} Reason={failureReason} PathfindingFallback=False");
            }

            // Atomic rejection: all group members stay stationary and play blocked feedback
            foreach (var member in group)
            {
                if (member != null)
                {
                    member.TriggerBlockedFeedback();
                }
            }

            // Deduct exactly ONE life for this player move attempt (invalid move)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife(InvalidMoveReason.NoValidPath);
            }
            return false;
        }

        // Reserve destination slots to prevent oversubscription
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
            // Rollback any reservations made in this attempt
            foreach (var hole in reservedHoles)
            {
                hole.ReleaseReservation();
            }
            Debug.Log($"[QueueMoveBlocked] Queue={queue.QueueId} Reason=HolderOversubscribed MovingQueues={movingQueues.Count} HeartPenalty=False");
            queue.PlayBlockedShake();
            return false;
        }

        // All valid and reserved: atomically launch synchronized forward movement
        foreach (var member in group)
        {
            if (member != null)
            {
                movingQueues.Add(member);
                var dir = groupDirections[member];
                var hole = groupTargetHoles[member];
                var entrance = groupEntranceCells[member];
                member.DestinationHole = hole;

                Vector2Int head = member.CurrentBodyCoordinates[0];
                Vector2Int neck = member.CurrentBodyCoordinates.Count > 1 ? member.CurrentBodyCoordinates[1] : head - dir;
                Debug.Log($"[ForwardMove] Queue={member.QueueId} Head={head} Neck={neck} Direction={dir} Next={head + dir} PathfindingUsed=False");

                member.StartForwardMovement(dir, hole, entrance);
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether an ant queue (or all members of its connected group) has a valid unblocked straight forward path to its holder.
    /// Does NOT use BFS, A*, or any pathfinding.
    /// </summary>
    public bool IsPathClearForQueue(AntQueueController queue)
    {
        if (queue == null) return false;

        var group = GetConnectedGroup(queue);
        var pendingReservations = new Dictionary<HoleController, int>();
        foreach (var member in group)
        {
            if (member == null) return false;
            if (!ValidateForwardMove(member, group, pendingReservations, out _, out var targetHole, out _, out _))
            {
                return false;
            }
            if (targetHole == null)
            {
                return false;
            }
            if (!pendingReservations.ContainsKey(targetHole))
            {
                pendingReservations[targetHole] = 0;
            }
            pendingReservations[targetHole]++;
        }

        return true;
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

                // Find matching hole to notify queue swallow (badge decrement)
                HoleController targetHole = queue.DestinationHole;
                if (targetHole == null && queue.CurrentBodyCoordinates != null && queue.CurrentBodyCoordinates.Count > 0)
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
                    targetHole.OnQueueSwallowed();
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
