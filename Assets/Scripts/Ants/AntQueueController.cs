using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AntQueueController : MonoBehaviour
{
    private static bool s_DebugLoggedOneRefAnt = false;

    [Header("Queue Data")]
    [SerializeField] private string queueId;
    [SerializeField] private int colorId = 0;
    [SerializeField] private AntColor antColor;
    [SerializeField] private int antCount;

    [Header("Connected Queues")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int connectedIndex = -1;

    [Header("Runtime State")]
    [SerializeField] private bool isMoving = false;
    [SerializeField] private List<Vector2Int> currentBodyCoordinates = new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> remainingPath = new List<Vector2Int>();
    [SerializeField] private Vector2Int forwardDirection = Vector2Int.zero;
    [SerializeField] private Vector2Int holeEntranceCoordinate = Vector2Int.zero;

    [Header("Animation Settings")]
    [SerializeField] private float stepDuration = 0.16f;
    [SerializeField] private float jumpPower = 0.42f;
    [SerializeField] private float enterHoleJumpPower = 1.1f;

    [SerializeField] private List<AntUnit> antUnits = new List<AntUnit>();
    [SerializeField] private HoleController destinationHole;

    public string QueueId => queueId;
    public int ColorID => colorId;
    public AntColor Color => antColor;
    public int AntCount => antCount;
    public bool IsConnected => isConnected;
    public int ConnectedIndex => connectedIndex;
    public bool IsMoving => isMoving;
    public Vector2Int ForwardDirection => forwardDirection;
    public Vector2Int HoleEntranceCoordinate => holeEntranceCoordinate;
    public HoleController DestinationHole { get => destinationHole; set => destinationHole = value; }
    public IReadOnlyList<Vector2Int> CurrentBodyCoordinates => currentBodyCoordinates;
    public IReadOnlyList<Vector2Int> RemainingPath => remainingPath;
    public IReadOnlyList<AntUnit> Units => antUnits;

    public static event Action<AntQueueController> OnQueueMoveStarted;
    public static event Action<AntQueueController> OnQueueMoveCompleted;
    public static event Action<AntQueueController> OnQueueBlockedFeedback;

    private Vector3 GetWorldPositionForGrid(Vector2Int coord, float yOffset = 0f)
    {
        if (BoardManager.Instance != null)
        {
            return BoardManager.Instance.GridToWorldPosition(coord, yOffset);
        }
        // Standard centered board fallback (9x6, cellSize = 1)
        float x = (coord.x - 4.0f) * 1.0f;
        float z = (coord.y - 2.5f) * 1.0f;
        return new Vector3(x, yOffset, z);
    }

    private bool IsPrefabAsset()
    {
#if UNITY_EDITOR
        return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject) || !gameObject.scene.IsValid();
#else
        return false;
#endif
    }

    private void Awake()
    {
        if (IsPrefabAsset()) return;
        if (antUnits.Count == 0 || transform.childCount == 0)
        {
            if (currentBodyCoordinates != null && currentBodyCoordinates.Count > 0)
            {
                SpawnAnts();
            }
        }
    }

    public void Initialize(AntQueueData data)
    {
        queueId = data.QueueId;
        colorId = data.ColorID;
        antColor = ReferenceColorMapper.ToAntColor(data.ColorID);
        antCount = data.AntCount;
        isConnected = data.IsConnected;
        connectedIndex = data.ConnectedIndex;

        currentBodyCoordinates = new List<Vector2Int>(data.InitialBodyCoordinates);
        remainingPath = new List<Vector2Int>(data.FullPathToHole);

        SpawnAnts();
    }

    public void InitializeReference(string id, int rawColorId, AntColor color, List<Vector2Int> bodyCoords, List<Vector2Int> path, bool connected = false, int connIdx = -1)
    {
        queueId = id;
        colorId = rawColorId;
        antColor = ReferenceColorMapper.ToAntColor(rawColorId);
        isConnected = connected;
        connectedIndex = connIdx;
        currentBodyCoordinates = bodyCoords != null ? new List<Vector2Int>(bodyCoords) : new List<Vector2Int>();
        antCount = currentBodyCoordinates.Count;
        remainingPath = path != null ? new List<Vector2Int>(path) : new List<Vector2Int>();

        SpawnAnts();
    }

    public void InitializeReference(string id, AntColor color, List<Vector2Int> bodyCoords, List<Vector2Int> path)
    {
        int rawId = ReferenceColorMapper.ToRawColorId(color);
        InitializeReference(id, rawId, color, bodyCoords, path, false, -1);
    }

    [ContextMenu("Rebuild Queue Visuals")]
    public void SpawnAnts()
    {
        // Clear previous units if any
        foreach (var unit in antUnits)
        {
            if (unit != null) SafeDestroy(unit.gameObject);
        }
        antUnits.Clear();

        // Also clean any unreferenced child objects
        for (int c = transform.childCount - 1; c >= 0; c--)
        {
            SafeDestroy(transform.GetChild(c).gameObject);
        }

        for (int i = 0; i < currentBodyCoordinates.Count; i++)
        {
            Vector2Int coord = currentBodyCoordinates[i];
            Vector3 worldPos = GetWorldPositionForGrid(coord);

            GameObject antObj = new GameObject($"Ant_{queueId}_{i}");
            antObj.transform.SetParent(transform, false);
            antObj.transform.position = worldPos;

            var antUnit = antObj.AddComponent<AntUnit>();
            antUnit.Setup(antColor, (i == 0), (i == currentBodyCoordinates.Count - 1));

            antUnits.Add(antUnit);
        }

        UpdateAntOrientations();
    }

    /// <summary>
    /// Aligns each ant segment's body to naturally face the leader ahead of it.
    /// </summary>
    public void UpdateAntOrientations()
    {
        for (int i = 0; i < antUnits.Count; i++)
        {
            var unit = antUnits[i];
            if (unit == null) continue;

            Vector3 lookDir = Vector3.forward;

            if (i == 0)
            {
                // Head faces the next step on the path towards the hole
                if (remainingPath != null && remainingPath.Count > 1)
                {
                    Vector3 nextPos = GetWorldPositionForGrid(remainingPath[1]);
                    lookDir = nextPos - unit.transform.position;
                }
                else if (currentBodyCoordinates.Count > 1)
                {
                    // Fallback: face away from the segment behind it
                    Vector3 behindPos = GetWorldPositionForGrid(currentBodyCoordinates[1]);
                    lookDir = unit.transform.position - behindPos;
                }
            }
            else
            {
                // Following ant strictly faces the ant directly in front of it in the queue
                var leadAnt = antUnits[i - 1];
                if (leadAnt != null)
                {
                    lookDir = leadAnt.transform.position - unit.transform.position;
                }
            }

            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                unit.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    public bool Occupies(Vector2Int coord)
    {
        return currentBodyCoordinates.Contains(coord);
    }

    public IReadOnlyList<Vector2Int> GetRemainingPath()
    {
        return remainingPath;
    }

    public IEnumerable<Vector2Int> GetReservedPathCells()
    {
        if (!isMoving) yield break;
        if (currentBodyCoordinates == null || currentBodyCoordinates.Count == 0) yield break;
        if (forwardDirection == Vector2Int.zero) yield break;

        Vector2Int curr = currentBodyCoordinates[0] + forwardDirection;
        while (curr != holeEntranceCoordinate)
        {
            if (BoardManager.Instance != null && !BoardManager.Instance.IsInBounds(curr))
            {
                yield break;
            }
            yield return curr;
            curr += forwardDirection;
        }
    }

    /// <summary>
    /// Attempts to trigger this queue (or its connected group) to move straight forward to its matching hole.
    /// Returns true if movement started, false if blocked or unable to move.
    /// </summary>
    public bool TryStartMove()
    {
        if (isMoving) return false;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;

        if (BoardManager.Instance != null)
        {
            return BoardManager.Instance.TryStartMove(this);
        }

        // Standalone fallback without BoardManager
        if (currentBodyCoordinates == null || currentBodyCoordinates.Count < 2)
        {
            TriggerBlockedFeedback();
            return false;
        }

        Vector2Int head = currentBodyCoordinates[0];
        Vector2Int neck = currentBodyCoordinates[1];
        Vector2Int dir = head - neck;
        StartForwardMovement(dir, null, head + dir);
        return true;
    }

    public void StartForwardMovement(Vector2Int direction, HoleController targetHole, Vector2Int entranceCell)
    {
        if (isMoving) return;
        this.forwardDirection = direction;
        this.destinationHole = targetHole;
        this.holeEntranceCoordinate = entranceCell;
        StartCoroutine(ForwardMoveRoutine());
    }

    public void StartMoveAlongPath(List<Vector2Int> path)
    {
        if (isMoving || path == null || path.Count < 2) return;
        Vector2Int dir = path[1] - path[0];
        Vector2Int entrance = path[path.Count - 1];
        HoleController hole = null;
        if (BoardManager.Instance != null)
        {
            hole = BoardManager.Instance.GetHoleAt(entrance);
        }
        StartForwardMovement(dir, hole, entrance);
    }

    private void OnDisable()
    {
        KillAllTweens();
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    private void KillAllTweens()
    {
        StopAllCoroutines();
        if (isMoving)
        {
            if (destinationHole != null)
            {
                destinationHole.ReleaseReservation();
            }
            if (BoardManager.Instance != null)
            {
                BoardManager.Instance.OnQueueMoveStoppedEarly(this);
            }
            isMoving = false;
        }
        if (antUnits != null)
        {
            for (int i = 0; i < antUnits.Count; i++)
            {
                if (antUnits[i] != null)
                {
                    antUnits[i].transform.DOKill();
                }
            }
        }
        transform.DOKill();
    }

    public void TriggerBlockedFeedback()
    {
        PlayBlockedShake();
        OnQueueBlockedFeedback?.Invoke(this);
    }

    public void PlayBlockedShake()
    {
        if (!Application.isPlaying) return;
        foreach (var unit in antUnits)
        {
            if (unit == null) continue;
            unit.transform.DOKill();
            unit.transform.DOShakePosition(0.35f, new Vector3(0.18f, 0f, 0.18f), 15, 90, false, true);
        }
    }

    /// <summary>
    /// Strict straight-forward follow-the-leader chain movement:
    /// - Head moves strictly forward along forwardDirection step-by-step.
    /// - Follower [i] only steps into the exact cell vacated by segment [i-1].
    /// - Each segment only dives into the hole pit once it reaches the hole entrance cell.
    /// - No automatic turns, BFS, A*, or rerouting.
    /// </summary>
    private IEnumerator ForwardMoveRoutine()
    {
        isMoving = true;
        foreach (var u in antUnits) { if (u != null) u.IsMoving = true; }
        OnQueueMoveStarted?.Invoke(this);

        HoleController hole = destinationHole;
        Vector2Int holeCoord = holeEntranceCoordinate;
        if (hole != null && holeCoord == Vector2Int.zero)
        {
            holeCoord = hole.GridCoordinate;
        }
        Vector3 holeWorldPos = GetWorldPositionForGrid(holeCoord);

        List<AntUnit> activeUnits = new List<AntUnit>(antUnits);

        while (activeUnits.Count > 0)
        {
            // Snapshot active body coordinates before step begins
            List<Vector2Int> previousActiveCoords = new List<Vector2Int>(currentBodyCoordinates);

            // Determine target cell for each active ant:
            // Leading ant (index 0): previous body[0] + forwardDirection
            // Followers (index a >= 1): previous body[a - 1]
            List<Vector2Int> targetCoords = new List<Vector2Int>(activeUnits.Count);
            for (int a = 0; a < activeUnits.Count; a++)
            {
                if (a == 0)
                {
                    targetCoords.Add(previousActiveCoords[0] + forwardDirection);
                }
                else
                {
                    targetCoords.Add(previousActiveCoords[a - 1]);
                }
            }

            // Moving queue collision check before hop execution
            bool collisionDetected = false;
            Vector2Int collisionCoord = Vector2Int.zero;
            if (BoardManager.Instance != null)
            {
                for (int a = 0; a < activeUnits.Count; a++)
                {
                    Vector2Int target = targetCoords[a];
                    if (target != holeCoord)
                    {
                        var otherQueue = BoardManager.Instance.GetQueueOccupying(target, this);
                        if (otherQueue != null)
                        {
                            collisionDetected = true;
                            collisionCoord = target;
                            break;
                        }
                    }
                }
            }

            if (collisionDetected)
            {
                Debug.LogWarning($"[InvalidMove] Queue={queueId} Reason=BlockedCell BlockedCell={collisionCoord} LivesBefore={GameManager.Instance?.PlayerLives}");
                if (destinationHole != null)
                {
                    destinationHole.ReleaseReservation();
                }
                if (BoardManager.Instance != null)
                {
                    BoardManager.Instance.OnQueueMoveStoppedEarly(this);
                }
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoseLife(InvalidMoveReason.OccupancyCollision);
                }
                isMoving = false;
                foreach (var u in antUnits) { if (u != null) u.IsMoving = false; }
                TriggerBlockedFeedback();
                yield break;
            }

            Sequence stepSequence = DOTween.Sequence();
            List<int> toRemoveAtStepEnd = new List<int>();

            for (int a = 0; a < activeUnits.Count; a++)
            {
                var unit = activeUnits[a];
                if (unit == null) continue;

                Vector2Int target = targetCoords[a];

                if (target == holeCoord)
                {
                    // Ant reaches the hole entrance cell: high-arc dive jump into the pit!
                    Vector3 targetPit = holeWorldPos + new Vector3(0f, -0.2f, 0f);
                    stepSequence.Join(unit.transform.DOJump(targetPit, enterHoleJumpPower, 1, stepDuration * 1.2f).SetEase(Ease.InQuad));
                    stepSequence.Join(unit.transform.DOScale(Vector3.zero, stepDuration * 1.2f).SetEase(Ease.InBack));

                    if (hole != null)
                    {
                        hole.OnAntSwallowed();
                    }

                    toRemoveAtStepEnd.Add(a);
                }
                else
                {
                    // Strict 1-tile hop along the grid
                    Vector3 targetPos = GetWorldPositionForGrid(target);
                    Vector3 moveDir = targetPos - unit.transform.position;
                    moveDir.y = 0;

                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        stepSequence.Join(unit.transform.DORotateQuaternion(Quaternion.LookRotation(moveDir), stepDuration * 0.5f));
                    }
                    stepSequence.Join(unit.transform.DOJump(targetPos, jumpPower, 1, stepDuration).SetEase(Ease.Linear));

                    if (a < currentBodyCoordinates.Count)
                    {
                        currentBodyCoordinates[a] = target;
                    }
                }
            }

            if (stepSequence != null && stepSequence.IsActive() && !stepSequence.IsComplete())
            {
                yield return stepSequence.WaitForCompletion(true);
            }
            else
            {
                yield return new WaitForSeconds(stepDuration);
            }

            // Clean up swallowed ant GameObjects
            for (int r = toRemoveAtStepEnd.Count - 1; r >= 0; r--)
            {
                int remIdx = toRemoveAtStepEnd[r];
                var swallowedUnit = activeUnits[remIdx];

                if (remIdx < currentBodyCoordinates.Count)
                {
                    currentBodyCoordinates.RemoveAt(remIdx);
                }

                activeUnits.RemoveAt(remIdx);
                antUnits.Remove(swallowedUnit);

                SafeDestroy(swallowedUnit.gameObject);
            }
        }

        isMoving = false;
        OnQueueMoveCompleted?.Invoke(this);
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.OnQueueCompleted(this);
        }
        destinationHole = null;
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj)) return;
#endif
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (IsPrefabAsset()) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying && !IsPrefabAsset() && antUnits.Count > 0)
            {
                UpdateAntOrientations();
            }
        };
    }
#endif
}
