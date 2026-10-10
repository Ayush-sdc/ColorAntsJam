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

    [Header("Lock Mechanics")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private int lockAmount = 0;
    private AntLockBadge lockBadge;

    [Header("Stacked / Second Color")]
    [SerializeField] private bool isSecondColor = false;
    [SerializeField] private int secondColor = 0;
    [SerializeField] private AntQueueController lowerQueueLayer;
    [SerializeField] private bool isDormantStacked = false;

    public enum QueueMoveState
    {
        Idle,
        MovingToRescue,
        FailedMoveApproach,
        Impact,
        Returning
    }

    [Header("Movement State")]
    [SerializeField] private QueueMoveState moveState = QueueMoveState.Idle;
    public QueueMoveState MoveState => moveState;

    [Header("Failed Move Feedback")]
    [Tooltip("Forward speed towards stationary obstacle during a failed move attempt.")]
    [SerializeField] private float approachSpeed = 6.8f;
    [Tooltip("Pause duration at collision impact before reversing.")]
    [SerializeField] private float impactPause = 0.18f;
    [Tooltip("Recoil bounce distance at contact impact.")]
    [SerializeField] private float recoilAmount = 0.16f;
    [Tooltip("Reverse speed retracing traveled path back to origin.")]
    [SerializeField] private float returnSpeed = 6.0f;

    public float ApproachSpeed { get => approachSpeed; set => approachSpeed = value; }
    public float ImpactPause { get => impactPause; set => impactPause = value; }
    public float RecoilAmount { get => recoilAmount; set => recoilAmount = value; }
    public float ReturnSpeed { get => EffectiveMoveSpeed; set => returnSpeed = value; }

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
    public bool IsLocked => isLocked && lockAmount > 0;
    public int LockAmount => lockAmount;
    public bool IsSecondColor => isSecondColor;
    public int SecondColor => secondColor;
    public AntColor SecondAntColor => ReferenceColorMapper.ToAntColor(secondColor);
    public AntQueueController LowerQueueLayer => lowerQueueLayer;
    public bool IsDormantStacked => isDormantStacked;
    public IReadOnlyList<Vector2Int> CurrentBodyCoordinates => currentBodyCoordinates;
    public HoleController DestinationHole { get => destinationHole; set => destinationHole = value; }
    public Vector2Int ForwardDirection { get => forwardDirection; set => forwardDirection = value; }
    public Vector2Int HoleEntranceCoordinate { get => holeEntranceCoordinate; set => holeEntranceCoordinate = value; }
    private List<PipeController> traversedPipes = new List<PipeController>();
    public List<PipeController> TraversedPipes
    {
        get => traversedPipes;
        set => traversedPipes = value != null ? value : new List<PipeController>();
    }
    public PipeController TraversedPipe
    {
        get => (traversedPipes != null && traversedPipes.Count > 0) ? traversedPipes[0] : null;
        set
        {
            if (traversedPipes == null) traversedPipes = new List<PipeController>();
            if (value == null)
            {
                traversedPipes.Clear();
            }
            else if (!traversedPipes.Contains(value))
            {
                traversedPipes.Clear();
                traversedPipes.Add(value);
            }
        }
    }
    public IReadOnlyList<Vector2Int> RemainingPath => remainingPath;
    public IReadOnlyList<AntUnit> Units => antUnits;
    private bool isLeaderInHole = false;
    public bool IsLeaderInHole { get => isLeaderInHole; set => isLeaderInHole = value; }

    [SerializeField] private AntsJam.Ropes.AntQueueRopeVisual ropeVisual;
    public AntsJam.Ropes.AntQueueRopeVisual RopeVisual => ropeVisual;

    public void EnableParallelRope()
    {
        // Unwanted lengthwise queue rope removed per user specification
        if (ropeVisual != null)
        {
            ropeVisual.Cleanup();
        }
    }

    public static event Action<AntQueueController> OnQueueMoveStarted;
    public static event Action<AntQueueController> OnQueueMoveCompleted;
    public static event Action<AntQueueController> OnQueueBlockedFeedback;

    public Vector3 GetWorldPositionForGrid(Vector2Int coord, float yOffset = 0f)
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
        isSecondColor = data.IsSecondColor;
        secondColor = data.SecondColor;
        isLocked = data.IsLocked;
        lockAmount = data.LockAmount;
        isLeaderInHole = false;

        currentBodyCoordinates = new List<Vector2Int>(data.InitialBodyCoordinates);
        remainingPath = new List<Vector2Int>(data.FullPathToHole);

        SpawnAnts();
        SetupLockBadge();
    }

    public void SetupLockBadge()
    {
        if (isLocked && lockAmount > 0)
        {
            if (lockBadge == null)
            {
                Transform targetHead = (antUnits != null && antUnits.Count > 0 && antUnits[0] != null) ? antUnits[0].transform : transform;
                GameObject badgeObj = new GameObject($"LockBadge_{queueId}");
                badgeObj.transform.SetParent(transform, false);
                lockBadge = badgeObj.AddComponent<AntLockBadge>();
                lockBadge.Initialize(targetHead, lockAmount);
            }
            else
            {
                lockBadge.UpdateCounter(lockAmount, animate: false);
            }
        }
        else if (lockBadge != null)
        {
            lockBadge.PlayUnlockAndDismiss(() => lockBadge = null);
        }
    }

    public void SetLock(bool locked, int amount)
    {
        this.isLocked = locked;
        this.lockAmount = amount;
        SetupLockBadge();
    }

    public void DecrementLock(int amount)
    {
        if (!isLocked || lockAmount <= 0) return;

        lockAmount = Mathf.Max(0, lockAmount - amount);
        if (lockAmount == 0)
        {
            isLocked = false;
            if (lockBadge != null)
            {
                lockBadge.PlayUnlockAndDismiss(() => lockBadge = null);
            }
            Debug.Log($"[AntQueueController] Queue {queueId} UNLOCKED!");
        }
        else if (lockBadge != null)
        {
            lockBadge.UpdateCounter(lockAmount, animate: true);
        }
    }

    public void TriggerLockedFeedback()
    {
        PlayBlockedShake();
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBlockedSound();
        }
    }

    public void SetLowerQueueLayer(AntQueueController lowerQueue)
    {
        this.lowerQueueLayer = lowerQueue;
        if (lowerQueue != null)
        {
            lowerQueue.isDormantStacked = true;
            lowerQueue.gameObject.SetActive(false);
        }
    }

    public void RevealFromStack()
    {
        isDormantStacked = false;
        gameObject.SetActive(true);
        transform.DOKill();
        transform.localScale = Vector3.one * 0.8f;
        transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);

        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.RegisterActiveQueue(this);
        }
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
            Color queueColor = destinationHole != null ? destinationHole.ResolvedRimColor : AntColorPalette.GetColor(antColor);
            antUnit.Setup(antColor, (i == 0), (i == currentBodyCoordinates.Count - 1), queueColor);

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
                // Head strictly faces along its queue body forward direction (away from neck)
                if (currentBodyCoordinates.Count > 1)
                {
                    Vector3 behindPos = GetWorldPositionForGrid(currentBodyCoordinates[1]);
                    lookDir = unit.transform.position - behindPos;
                }
                else if (forwardDirection != Vector2Int.zero)
                {
                    lookDir = new Vector3(forwardDirection.x, 0f, forwardDirection.y);
                }
                else
                {
                    lookDir = unit.transform.forward;
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
                unit.transform.rotation = Quaternion.LookRotation(lookDir.normalized);
            }
        }
    }

    /// <summary>
    /// Returns the authoritative world-space forward vector of the queue head, projected on the board plane.
    /// Derived from the gameplay root / ant transform, not camera orientation.
    /// </summary>
    public Vector3 GetHeadForwardWorld()
    {
        if (antUnits != null && antUnits.Count > 0 && antUnits[0] != null)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(antUnits[0].transform.forward, Vector3.up);
            if (fwd.sqrMagnitude > 0.001f)
            {
                return fwd.normalized;
            }
        }

        if (currentBodyCoordinates != null && currentBodyCoordinates.Count > 1)
        {
            Vector2Int gridDir = currentBodyCoordinates[0] - currentBodyCoordinates[1];
            Vector3 fwd = new Vector3(gridDir.x, 0f, gridDir.y);
            if (fwd.sqrMagnitude > 0.001f) return fwd.normalized;
        }

        if (forwardDirection != Vector2Int.zero)
        {
            return new Vector3(forwardDirection.x, 0f, forwardDirection.y).normalized;
        }

        return Vector3.forward;
    }

    public bool Occupies(Vector2Int coord)
    {
        return currentBodyCoordinates.Contains(coord);
    }

    /// <summary>
    /// Checks whether the queue's body occupies a given cell at step 'stepIndex' (1-based from head).
    /// Dynamically accounts for trailing body segments vacated as the queue advances forward.
    /// Trailing segments at indices >= (L - stepIndex) have moved forward and vacated their initial cells.
    /// </summary>
    public bool OccupiesAtStep(Vector2Int cell, int stepIndex, List<Vector2Int> pathSoFar = null)
    {
        if (currentBodyCoordinates == null || currentBodyCoordinates.Count == 0) return false;
        int L = currentBodyCoordinates.Count;

        // 1. Initial body segments that have not yet vacated at stepIndex
        // At step 1: body[0] moves to path[1], body[1] moves to body[0], ..., body[L-1] (tail) moves to body[L-2].
        // Thus, original cell body[L-1] is vacated at step 1!
        // In general, at stepIndex s (>= 1), original cells body[j] for j >= (L - s) are VACATED.
        // Still occupied initial cells are j in [0, L - 1 - s].
        int maxOccupiedInitialIndex = L - 1 - stepIndex;
        for (int j = 0; j <= maxOccupiedInitialIndex; j++)
        {
            if (currentBodyCoordinates[j] == cell)
            {
                return true;
            }
        }

        // 2. Path segments traversed by head that the body currently stretches through
        // At stepIndex s, the head is at pathSoFar[s]. Segments behind head occupy pathSoFar[s - k] for k in [1, min(s-1, L-1)].
        if (pathSoFar != null && stepIndex >= 2)
        {
            int maxPathHistory = Mathf.Min(stepIndex - 1, L - 1);
            for (int k = 1; k <= maxPathHistory; k++)
            {
                int historyIndex = stepIndex - k;
                if (historyIndex >= 0 && historyIndex < pathSoFar.Count)
                {
                    if (pathSoFar[historyIndex] == cell)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
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
        if (isMoving || isDormantStacked || moveState != QueueMoveState.Idle) return false;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return false;

        if (IsLocked)
        {
            TriggerLockedFeedback();
            return false;
        }

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

    [System.Serializable]
    public struct TrailPoint
    {
        public Vector3 position;
        public Vector3 forward;
        public float distance;
    }

    [Header("Continuous Free Movement")]
    [SerializeField] private float moveSpeed = 6.8f;
    [Tooltip("Movement speed multiplier. Default 1.5x for faster travel. Overridden by BoardManager / AntQueueVisualSettings if present.")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float segmentDistance = 0.85f;
    public float SegmentDistance => segmentDistance;
    private readonly List<TrailPoint> trailHistory = new List<TrailPoint>();
    private float totalTrailDistance = 0f;
    private List<Vector3> currentSmoothRoute = new List<Vector3>();

    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float SpeedMultiplier { get => speedMultiplier; set => speedMultiplier = value; }
    public float EffectiveMoveSpeed
    {
        get
        {
            if (BoardManager.Instance != null)
            {
                return moveSpeed * BoardManager.Instance.MovementSpeedMultiplier;
            }
            if (AntQueueVisualSettings.Instance != null)
            {
                return moveSpeed * AntQueueVisualSettings.Instance.MovementSpeedMultiplier;
            }
            return moveSpeed * speedMultiplier;
        }
    }

    public IReadOnlyList<TrailPoint> TrailHistory => trailHistory;

    public void StartSmoothMovement(List<Vector3> worldPath, HoleController targetHole, Vector2Int entranceCell)
    {
        if (isMoving) return;
        moveState = QueueMoveState.MovingToRescue;
        this.destinationHole = targetHole;
        this.holeEntranceCoordinate = entranceCell;
        this.currentSmoothRoute = (worldPath != null && worldPath.Count > 0) ? new List<Vector3>(worldPath) : new List<Vector3>();

        // Reveal underlying stacked ant layer when dominant layer starts moving
        if (lowerQueueLayer != null)
        {
            var lower = lowerQueueLayer;
            lowerQueueLayer = null;
            lower.RevealFromStack();
        }

        StartCoroutine(SmoothFreeMoveRoutine());
    }

    public void StartForwardMovement(Vector2Int direction, HoleController targetHole, Vector2Int entranceCell)
    {
        if (isMoving) return;
        this.forwardDirection = direction;
        this.destinationHole = targetHole;
        this.holeEntranceCoordinate = entranceCell;


        // Direct straight free world movement without automatic pathfinder
        if (BoardManager.Instance != null)
        {
            var straightRoute = BoardManager.Instance.GenerateStraightFreeWorldPath(this, direction, targetHole, entranceCell);
            StartSmoothMovement(straightRoute, targetHole, entranceCell);
            return;
        }

        Vector3 headPos = antUnits.Count > 0 && antUnits[0] != null ? antUnits[0].transform.position : transform.position;
        Vector3 endPos = targetHole != null ? targetHole.transform.position : headPos + new Vector3(direction.x * 5f, 0f, direction.y * 5f);
        endPos.y = headPos.y;
        List<Vector3> fallbackRoute = new List<Vector3> { headPos, endPos };
        StartSmoothMovement(fallbackRoute, targetHole, entranceCell);
    }

    public void StartMoveAlongPath(List<Vector2Int> path)
    {
        if (isMoving || path == null || path.Count < 2) return;
        Vector2Int entrance = path[path.Count - 1];
        HoleController hole = null;
        if (BoardManager.Instance != null)
        {
            hole = BoardManager.Instance.GetHoleAt(entrance);
            var smoothRoute = BoardManager.Instance.GenerateSmoothFreeWorldPath(path, this, hole);
            StartSmoothMovement(smoothRoute, hole, entrance);
        }
    }

    private void InitializeTrail()
    {
        trailHistory.Clear();
        totalTrailDistance = 0f;

        if (antUnits == null || antUnits.Count == 0) return;

        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0f) ? BoardManager.Instance.CellSize : 1.0f;
        segmentDistance = AntQueueVisualSettings.Instance.MovementSpacingMultiplier * cs;
        if (antUnits.Count > 1 && antUnits[0] != null && antUnits[1] != null)
        {
            float actualDist = Vector3.Distance(antUnits[0].transform.position, antUnits[1].transform.position);
            if (actualDist > 0.1f)
            {
                segmentDistance = actualDist;
            }
        }

        // Build polyline from tail (N-1) up to head (0)
        int count = antUnits.Count;
        Vector3 prevPos = antUnits[count - 1] != null ? antUnits[count - 1].transform.position : transform.position;
        Vector3 initialFwd = antUnits[count - 1] != null ? antUnits[count - 1].transform.forward : Vector3.forward;

        trailHistory.Add(new TrailPoint
        {
            position = prevPos,
            forward = initialFwd,
            distance = 0f
        });

        for (int i = count - 2; i >= 0; i--)
        {
            if (antUnits[i] == null) continue;
            Vector3 currPos = antUnits[i].transform.position;
            float segLen = Vector3.Distance(prevPos, currPos);
            if (segLen < 0.001f) segLen = segmentDistance;
            totalTrailDistance += segLen;

            Vector3 fwd = currPos - prevPos;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = antUnits[i].transform.forward;

            trailHistory.Add(new TrailPoint
            {
                position = currPos,
                forward = fwd.normalized,
                distance = totalTrailDistance
            });

            prevPos = currPos;
        }
    }

    private void AddTrailPoint(Vector3 newPos, Vector3 forward, bool allowTrimming = true)
    {
        if (trailHistory.Count == 0)
        {
            trailHistory.Add(new TrailPoint { position = newPos, forward = forward, distance = 0f });
            return;
        }

        Vector3 lastPos = trailHistory[trailHistory.Count - 1].position;
        float dist = Vector3.Distance(lastPos, newPos);
        if (dist < 0.005f) return;

        totalTrailDistance += dist;
        trailHistory.Add(new TrailPoint
        {
            position = newPos,
            forward = forward,
            distance = totalTrailDistance
        });

        if (allowTrimming)
        {
            // Trim old trail points that are far behind the tail
            float tailDist = totalTrailDistance - (antUnits.Count + 3) * segmentDistance;
            while (trailHistory.Count > 2 && trailHistory[1].distance < tailDist)
            {
                trailHistory.RemoveAt(0);
            }
        }
    }

    public bool SampleTrail(float targetDist, out Vector3 position, out Vector3 forward)
    {
        if (trailHistory.Count == 0)
        {
            position = transform.position;
            forward = transform.forward;
            return false;
        }

        if (targetDist <= trailHistory[0].distance)
        {
            position = trailHistory[0].position;
            forward = trailHistory[0].forward;
            return true;
        }

        if (targetDist >= trailHistory[trailHistory.Count - 1].distance)
        {
            position = trailHistory[trailHistory.Count - 1].position;
            forward = trailHistory[trailHistory.Count - 1].forward;
            return true;
        }

        // Binary search for the containing segment
        int low = 0;
        int high = trailHistory.Count - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (trailHistory[mid].distance < targetDist)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        int idxA = Mathf.Max(0, low - 1);
        int idxB = Mathf.Min(trailHistory.Count - 1, idxA + 1);

        TrailPoint a = trailHistory[idxA];
        TrailPoint b = trailHistory[idxB];
        float segLen = b.distance - a.distance;
        float t = segLen > 0.0001f ? Mathf.Clamp01((targetDist - a.distance) / segLen) : 0f;

        position = Vector3.Lerp(a.position, b.position, t);
        forward = Vector3.Slerp(a.forward, b.forward, t);
        if (forward.sqrMagnitude < 0.001f) forward = b.forward;
        return true;
    }

    /// <summary>
    /// Smooth continuous free-floor movement:
    /// - Head moves smoothly across open floor towards the destination hole.
    /// - Head continuously records world-space trail history.
    /// - Head always faces its actual movement direction.
    /// - Followers trace the exact recorded trail at constant distance behind the ant ahead.
    /// - Zero corner cutting. Ants move freely across open floor between dots.
    /// </summary>
    private IEnumerator SmoothFreeMoveRoutine()
    {
        List<AntUnit> units = new List<AntUnit>(antUnits);
        int totalAnts = units.Count;
        if (totalAnts == 0)
        {
            isMoving = false;
            OnQueueMoveCompleted?.Invoke(this);
            if (destinationHole != null) destinationHole.OnEntireQueueEnteredHole(this);
            if (BoardManager.Instance != null) BoardManager.Instance.OnQueueCompleted(this);
            destinationHole = null;
            yield break;
        }

        float activeSpeed = EffectiveMoveSpeed;
        isMoving = true;
        moveState = QueueMoveState.MovingToRescue;
        for (int i = 0; i < totalAnts; i++)
        {
            var u = units[i];
            if (u != null)
            {
                u.IsMoving = true;
                u.StartWalkAnimation(i, activeSpeed);
            }
        }
        OnQueueMoveStarted?.Invoke(this);

        InitializeTrail();

        float initialAntY = units[0] != null ? units[0].transform.position.y : 0.05f;
        float leaderLen = (units.Count > 0 && units[0] != null) ? units[0].VisualLength : AntVisualConfig.Instance.LeaderVisualLength;
        Vector3 holeDespawnPoint = (currentSmoothRoute.Count > 0) 
            ? currentSmoothRoute[currentSmoothRoute.Count - 1] 
            : (destinationHole != null ? destinationHole.GetHoleDespawnPoint(holeEntranceCoordinate, forwardDirection, leaderLen, this) : transform.position);

        Vector3 holeEntryPoint = destinationHole != null 
            ? destinationHole.GetHoleEntryPoint(holeEntranceCoordinate, forwardDirection, this) 
            : holeDespawnPoint;

        Vector3 entryForward = new Vector3(forwardDirection.x, 0f, forwardDirection.y).normalized;
        if (entryForward.sqrMagnitude < 0.001f && units.Count > 0 && units[0] != null)
        {
            entryForward = units[0].transform.forward;
            entryForward.y = 0f;
            entryForward.Normalize();
        }

        int routeIndex = 0;
        Vector3 currentHeadPos = units[0] != null ? units[0].transform.position : transform.position;

        bool[] hasEnteredHoleVisual = new bool[totalAnts];
        bool[] hasStartedDiving = new bool[totalAnts];
        int swallowedAntCount = 0;
        bool headReachedPit = false;
        float pitReachDistance = 0f;
        float virtualHeadDist = totalTrailDistance;
        bool hasAcquiredHoleEntry = false;

        while (swallowedAntCount < totalAnts)
        {
            while (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused) yield return null;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float stepBudget = activeSpeed * dt;

            // 1. Advance head along route until HoleDespawnPoint is reached
            if (!headReachedPit)
            {
                // Check if head is crossing into the hole entrance rim
                if (!hasAcquiredHoleEntry && destinationHole != null)
                {
                    float distToEntry = Vector3.Distance(currentHeadPos, holeEntryPoint);
                    float distPastEntrance = Vector3.Dot(currentHeadPos - holeEntryPoint, entryForward);
                    if (distToEntry < 0.60f || distPastEntrance >= -0.10f)
                    {
                        destinationHole.RequestHoleEntry(this);
                        hasAcquiredHoleEntry = true;
                        isLeaderInHole = true;
                    }
                }

                if (currentSmoothRoute != null && currentSmoothRoute.Count > 1 && routeIndex < currentSmoothRoute.Count - 1)
                {
                    while (stepBudget > 0f && routeIndex < currentSmoothRoute.Count - 1)
                    {
                        Vector3 nextWp = currentSmoothRoute[routeIndex + 1];
                        float distToNext = Vector3.Distance(currentHeadPos, nextWp);

                        if (stepBudget >= distToNext)
                        {
                            stepBudget -= distToNext;
                            currentHeadPos = nextWp;
                            routeIndex++;

                            Vector3 fwd = nextWp - currentSmoothRoute[Mathf.Max(0, routeIndex - 1)];
                            fwd.y = 0f;
                            if (fwd.sqrMagnitude < 0.001f) fwd = units[0] != null ? units[0].transform.forward : Vector3.forward;
                            AddTrailPoint(currentHeadPos, fwd.normalized);
                        }
                        else
                        {
                            Vector3 fwd = nextWp - currentHeadPos;
                            fwd.y = 0f;
                            currentHeadPos += fwd.normalized * stepBudget;
                            AddTrailPoint(currentHeadPos, fwd.normalized);
                            stepBudget = 0f;
                        }
                    }
                }

                virtualHeadDist = totalTrailDistance;

                // Check if head reached the deep despawn point inside the hole
                if (routeIndex >= currentSmoothRoute.Count - 1)
                {
                    headReachedPit = true;
                    isLeaderInHole = true;
                    currentHeadPos = holeDespawnPoint;
                    Vector3 pitFwd = units[0] != null ? units[0].transform.forward : Vector3.forward;
                    AddTrailPoint(holeDespawnPoint, pitFwd);
                    pitReachDistance = totalTrailDistance;
                    virtualHeadDist = pitReachDistance;
                }
            }
            else
            {
                // Head reached despawn point; followers continue marching along recorded trail deep into hole!
                virtualHeadDist += stepBudget;
            }

            // 2. Update each ant unit based on its trail position
            for (int i = 0; i < totalAnts; i++)
            {
                if (hasStartedDiving[i]) continue;

                var unit = units[i];
                if (unit == null)
                {
                    hasStartedDiving[i] = true;
                    swallowedAntCount++;
                    continue;
                }

                // If ant is the head and has not reached the despawn point yet, position it directly at currentHeadPos
                if (i == 0 && !headReachedPit)
                {
                    Vector3 headDir = currentHeadPos - unit.transform.position;
                    headDir.y = 0f;
                    if (headDir.sqrMagnitude > 0.0001f)
                    {
                        unit.transform.rotation = Quaternion.LookRotation(headDir.normalized);
                    }
                    unit.transform.position = currentHeadPos;

                    // Detect leading visual edge crossing into hole entrance
                    if (!hasEnteredHoleVisual[0] && destinationHole != null)
                    {
                        float halfLen = unit.VisualLength * 0.5f;
                        Vector3 leadingEdge = currentHeadPos + unit.transform.forward * halfLen;
                        float distToEntry = Vector3.Distance(currentHeadPos, holeEntryPoint);
                        if (distToEntry <= 2.5f)
                        {
                            float distPastEntrance = Vector3.Dot(leadingEdge - holeEntryPoint, entryForward);
                            if (distPastEntrance >= 0f)
                            {
                                hasEnteredHoleVisual[0] = true;
                                isLeaderInHole = true;
                                if (!hasAcquiredHoleEntry)
                                {
                                    destinationHole.RequestHoleEntry(this);
                                    hasAcquiredHoleEntry = true;
                                }
                                if (ropeVisual != null) ropeVisual.Cleanup();
                            }
                        }
                    }

                    if (currentHeadPos.y < initialAntY - 0.01f)
                    {
                        if (unit.IsWalkTweenActive)
                        {
                            unit.StopWalkAnimation();
                        }
                        float pitDescent = Mathf.Clamp01((initialAntY - currentHeadPos.y) / 0.35f);
                        float scaleMultiplier = Mathf.Clamp01(1.0f - pitDescent);
                        unit.transform.localScale = Vector3.one * scaleMultiplier;

                        if (pitDescent >= 0.95f || currentHeadPos.y <= initialAntY - 0.34f)
                        {
                            hasStartedDiving[0] = true;
                            swallowedAntCount++;
                            unit.SetVisualVisibility(false);
                            if (destinationHole != null) destinationHole.OnAntEnteredHole();
                            SafeDestroy(unit.gameObject);
                            continue;
                        }
                    }
                    else
                    {
                        unit.transform.localScale = Vector3.one;
                        UpdateAntPipeVisual(unit, traversedPipes);
                    }

                    if (currentBodyCoordinates != null && i < currentBodyCoordinates.Count && BoardManager.Instance != null)
                    {
                        currentBodyCoordinates[i] = BoardManager.Instance.WorldToGridPosition(currentHeadPos);
                    }
                    continue;
                }

                // Ant's distance along the recorded trail
                float targetDist = virtualHeadDist - i * segmentDistance;

                if (!headReachedPit || targetDist < pitReachDistance)
                {
                    // Still moving along trail towards/inside the hole
                    if (SampleTrail(targetDist, out Vector3 followPos, out Vector3 followDir))
                    {
                        unit.transform.position = followPos;
                        if (followDir.sqrMagnitude > 0.0001f)
                        {
                            unit.transform.rotation = Quaternion.LookRotation(followDir);
                        }

                        // Detect individual ant leading edge reaching entrance boundary
                        if (!hasEnteredHoleVisual[i] && destinationHole != null)
                        {
                            float halfLen = unit.VisualLength * 0.5f;
                            Vector3 leadingEdge = followPos + unit.transform.forward * halfLen;
                            float distToEntry = Vector3.Distance(followPos, holeEntryPoint);
                            if (distToEntry <= 2.5f)
                            {
                                float distPastEntrance = Vector3.Dot(leadingEdge - holeEntryPoint, entryForward);
                                if (distPastEntrance >= 0f)
                                {
                                    hasEnteredHoleVisual[i] = true;
                                    if (i == 0)
                                    {
                                        isLeaderInHole = true;
                                        if (!hasAcquiredHoleEntry)
                                        {
                                            destinationHole.RequestHoleEntry(this);
                                            hasAcquiredHoleEntry = true;
                                        }
                                        if (ropeVisual != null) ropeVisual.Cleanup();
                                    }
                                }
                            }
                        }

                        // Smooth progressive descent and conceal into recessed pit
                        if (followPos.y < initialAntY - 0.01f)
                        {
                            if (unit.IsWalkTweenActive)
                            {
                                unit.StopWalkAnimation();
                            }
                            float pitDescent = Mathf.Clamp01((initialAntY - followPos.y) / 0.35f);
                            float scaleMultiplier = Mathf.Clamp01(1.0f - pitDescent);
                            unit.transform.localScale = Vector3.one * scaleMultiplier;

                            if (pitDescent >= 0.95f || followPos.y <= initialAntY - 0.34f)
                            {
                                hasStartedDiving[i] = true;
                                swallowedAntCount++;
                                unit.StopWalkAnimation();
                                unit.SetVisualVisibility(false);
                                if (destinationHole != null) destinationHole.OnAntEnteredHole();
                                unit.ResetInsidePipeVisual();
                                SafeDestroy(unit.gameObject);
                                continue;
                            }
                        }
                        else
                        {
                            unit.transform.localScale = Vector3.one;
                            UpdateAntPipeVisual(unit, traversedPipes);
                        }

                        if (currentBodyCoordinates != null && i < currentBodyCoordinates.Count && BoardManager.Instance != null)
                        {
                            currentBodyCoordinates[i] = BoardManager.Instance.WorldToGridPosition(followPos);
                        }
                    }
                }
                else
                {
                    // Complete ant has reached HoleDespawnPoint (deep inside hole, entire body fully cleared entrance opening)
                    hasStartedDiving[i] = true;
                    swallowedAntCount++;

                    unit.StopWalkAnimation();
                    unit.SetVisualVisibility(false);

                    if (destinationHole != null)
                    {
                        destinationHole.OnAntEnteredHole();
                    }

                    unit.ResetInsidePipeVisual();
                    SafeDestroy(unit.gameObject);
                }
            }

            yield return null;
        }

        // Wait to be 100% sure all ant swallows have completed
        float safetyTimeout = 1.0f;
        while (swallowedAntCount < totalAnts && safetyTimeout > 0f)
        {
            safetyTimeout -= Time.deltaTime;
            yield return null;
        }

        // 3. Queue completion:
        // Only AFTER the LAST ant / tail has fully entered and completed swallow!
        isMoving = false;
        moveState = QueueMoveState.Idle;
        foreach (var u in antUnits)
        {
            if (u != null)
            {
                u.IsMoving = false;
                u.StopWalkAnimation();
                u.ResetInsidePipeVisual();
            }
        }
        OnQueueMoveCompleted?.Invoke(this);

        if (destinationHole != null)
        {
            destinationHole.ReleaseHoleEntry(this);
            destinationHole.OnEntireQueueEnteredHole(this);
        }

        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.OnQueueCompleted(this);
        }

        if (ropeVisual != null)
        {
            ropeVisual.Cleanup();
        }

        destinationHole = null;
    }

    [System.Serializable]
    private struct UnitPose
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
    private readonly List<UnitPose> initialUnitPoses = new List<UnitPose>();
    private Coroutine failedMoveCoroutine = null;

    /// <summary>
    /// Executes the animated wrong-move sequence when blocked by an eligible stationary obstacle:
    /// Idle -> FailedMoveApproach -> Impact -> Returning -> Idle.
    /// Retraces traveled trail backwards naturally without teleportation.
    /// </summary>
    public void StartFailedMove(
        List<Vector3> approachRoute,
        AntQueueController blockerQueue,
        bool isEarliestCollisionTrigger,
        List<PipeController> pipesToReserve = null,
        Action onReturnComplete = null,
        HoleController blockerHole = null)
    {
        if (moveState != QueueMoveState.Idle) return;
        if (failedMoveCoroutine != null) StopCoroutine(failedMoveCoroutine);
        failedMoveCoroutine = StartCoroutine(FailedMoveRoutine(approachRoute, blockerQueue, isEarliestCollisionTrigger, pipesToReserve, onReturnComplete, blockerHole));
    }

    private IEnumerator FailedMoveRoutine(
        List<Vector3> approachRoute,
        AntQueueController blockerQueue,
        bool isEarliestCollisionTrigger,
        List<PipeController> pipesToReserve,
        Action onReturnComplete,
        HoleController blockerHole = null)
    {
        List<AntUnit> units = new List<AntUnit>(antUnits);
        int totalAnts = units.Count;
        if (totalAnts == 0)
        {
            moveState = QueueMoveState.Idle;
            isMoving = false;
            onReturnComplete?.Invoke();
            yield break;
        }

        // 1. Save original visual state & unit poses
        initialUnitPoses.Clear();
        for (int i = 0; i < totalAnts; i++)
        {
            if (units[i] != null)
            {
                initialUnitPoses.Add(new UnitPose
                {
                    localPosition = units[i].transform.localPosition,
                    localRotation = units[i].transform.localRotation
                });
            }
        }

        // Initialize trail with trimming disabled so entire path from initial tail to head is preserved
        InitializeTrail();
        float initialHeadDist = totalTrailDistance;

        // Mark temporary pipe usage if route uses a pipe
        if (pipesToReserve != null)
        {
            foreach (var p in pipesToReserve)
            {
                if (p != null) p.IsPipeInUse = true;
            }
        }

        moveState = QueueMoveState.FailedMoveApproach;
        isMoving = true;
        float normalSpeed = EffectiveMoveSpeed;
        for (int i = 0; i < totalAnts; i++)
        {
            if (units[i] != null)
            {
                units[i].IsMoving = true;
                units[i].StartWalkAnimation(i, normalSpeed);
            }
        }

        // 2. Approach Phase: advance head along approach route up to visible contact
        int routeIndex = 0;
        Vector3 currentHeadPos = units[0] != null ? units[0].transform.position : transform.position;
        float virtualHeadDist = totalTrailDistance;

        while (approachRoute != null && approachRoute.Count > 1 && routeIndex < approachRoute.Count - 1)
        {
            while (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused) yield return null;

            float dt = (Time.deltaTime > 0.0001f) ? Mathf.Min(Time.deltaTime, 0.05f) : 0.016f;
            float stepBudget = normalSpeed * dt;

            while (stepBudget > 0f && routeIndex < approachRoute.Count - 1)
            {
                Vector3 nextWp = approachRoute[routeIndex + 1];
                float distToNext = Vector3.Distance(currentHeadPos, nextWp);

                if (distToNext < 0.001f || stepBudget >= distToNext)
                {
                    stepBudget = Mathf.Max(0f, stepBudget - distToNext);
                    currentHeadPos = nextWp;
                    routeIndex++;

                    Vector3 fwd = nextWp - approachRoute[Mathf.Max(0, routeIndex - 1)];
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 0.001f) fwd = units[0] != null ? units[0].transform.forward : Vector3.forward;
                    AddTrailPoint(currentHeadPos, fwd.normalized, allowTrimming: false);
                }
                else
                {
                    Vector3 fwd = nextWp - currentHeadPos;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude > 0.0001f)
                    {
                        currentHeadPos += fwd.normalized * stepBudget;
                        AddTrailPoint(currentHeadPos, fwd.normalized, allowTrimming: false);
                    }
                    else
                    {
                        currentHeadPos = nextWp;
                        routeIndex++;
                    }
                    stepBudget = 0f;
                }
            }

            virtualHeadDist = totalTrailDistance;
            UpdateUnitsAlongTrail(virtualHeadDist, pipesToReserve);
            if (BoardManager.Instance != null && isConnected) BoardManager.Instance.UpdateParallelRopes();
            yield return null;
        }

        // 3. Impact Phase
        moveState = QueueMoveState.Impact;

        // Recheck blocker before impact: if blocker queue moved or disappeared, cancel penalty!
        // Holes are permanent stationary structures, so blockerHole is always valid
        bool blockerStillStationary = (blockerHole != null) ||
            (blockerQueue != null && !blockerQueue.IsMoving && blockerQueue.MoveState == QueueMoveState.Idle && blockerQueue.gameObject.activeInHierarchy);

        if (isEarliestCollisionTrigger && blockerStillStationary)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayOuch();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife(InvalidMoveReason.NoValidPath, deferGameOver: true);
            }

            // Blocker (queue or hole) remains strictly stationary - no positional punch or displacement
        }

        // Impact pause
        float pauseElapsed = 0f;
        while (pauseElapsed < impactPause)
        {
            while (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused) yield return null;
            pauseElapsed += Time.deltaTime;
            yield return null;
        }

        // 4. Reverse Phase: retrace traveled polyline backwards naturally at the SAME normal speed
        // Duration = actual travelled distance / normal speed. Constant path-distance speed without easing.
        moveState = QueueMoveState.Returning;

        while (virtualHeadDist > initialHeadDist)
        {
            while (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused) yield return null;

            float dt = (Time.deltaTime > 0.0001f) ? Mathf.Min(Time.deltaTime, 0.05f) : 0.016f;
            virtualHeadDist -= normalSpeed * dt;
            if (virtualHeadDist < initialHeadDist) virtualHeadDist = initialHeadDist;

            UpdateUnitsAlongTrail(virtualHeadDist, pipesToReserve);
            if (BoardManager.Instance != null && isConnected) BoardManager.Instance.UpdateParallelRopes();
            yield return null;
        }

        // 5. Restore original position, orientation, spacing, and idle state
        for (int i = 0; i < units.Count && i < initialUnitPoses.Count; i++)
        {
            if (units[i] != null)
            {
                units[i].transform.localPosition = initialUnitPoses[i].localPosition;
                units[i].transform.localRotation = initialUnitPoses[i].localRotation;
                units[i].StopWalkAnimation();
                units[i].IsMoving = false;
                units[i].ResetInsidePipeVisual();
            }
        }
        ResetAllAntPipeVisuals();

        if (pipesToReserve != null)
        {
            foreach (var p in pipesToReserve)
            {
                if (p != null) p.IsPipeInUse = false;
            }
        }

        isMoving = false;
        moveState = QueueMoveState.Idle;
        if (BoardManager.Instance != null && isConnected) BoardManager.Instance.UpdateParallelRopes();
        failedMoveCoroutine = null;

        onReturnComplete?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyFailedMoveReturnCompleted(this);
        }
    }

    private void UpdateUnitsAlongTrail(float headDist, List<PipeController> candidatePipes = null)
    {
        for (int i = 0; i < antUnits.Count; i++)
        {
            var u = antUnits[i];
            if (u != null)
            {
                float uDist = headDist - i * segmentDistance;
                if (SampleTrail(uDist, out var pos, out var fwdDir))
                {
                    u.transform.position = pos;
                    if (fwdDir.sqrMagnitude > 0.001f)
                    {
                        u.transform.rotation = Quaternion.LookRotation(fwdDir, Vector3.up);
                    }
                }
                UpdateAntPipeVisual(u, candidatePipes);
            }
        }
    }

    public void UpdateAntPipeVisual(AntUnit unit, List<PipeController> candidatePipes = null)
    {
        if (unit == null) return;

        IReadOnlyList<PipeController> pipes = candidatePipes;
        if (pipes == null || pipes.Count == 0)
        {
            pipes = traversedPipes;
        }
        if ((pipes == null || pipes.Count == 0) && BoardManager.Instance != null)
        {
            pipes = BoardManager.Instance.ActivePipes;
        }

        float lowestOpacity = 1.0f;
        UnityEngine.Color chosenTint = UnityEngine.Color.white;

        if (pipes != null && pipes.Count > 0)
        {
            Vector3 pos = unit.transform.position;
            for (int p = 0; p < pipes.Count; p++)
            {
                var pipe = pipes[p];
                if (pipe == null) continue;
                float op = pipe.GetAntVisualOpacity(pos, out UnityEngine.Color tint);
                if (op < lowestOpacity)
                {
                    lowestOpacity = op;
                    chosenTint = tint;
                }
            }
        }

        if (lowestOpacity < 0.999f)
        {
            unit.SetInsidePipeVisual(lowestOpacity, chosenTint);
        }
        else
        {
            unit.ResetInsidePipeVisual();
        }
    }

    public void ResetAllAntPipeVisuals()
    {
        if (antUnits == null) return;
        for (int i = 0; i < antUnits.Count; i++)
        {
            if (antUnits[i] != null)
            {
                antUnits[i].ResetInsidePipeVisual();
            }
        }
    }

    public void PlayCosmeticBump(Vector3 bumpDirection)
    {
        // Blocker queues remain strictly stationary. No punch or translation displacement.
    }

    public void ReleaseReservedHole()
    {
        if (ropeVisual != null)
        {
            ropeVisual.Cleanup();
        }

        if (destinationHole != null)
        {
            destinationHole.ReleaseHoleEntry(this);
            if (isMoving)
            {
                destinationHole.ReleaseReservation(this);
            }
            destinationHole = null;
        }
    }

    private void OnDisable()
    {
        if (failedMoveCoroutine != null)
        {
            StopCoroutine(failedMoveCoroutine);
            failedMoveCoroutine = null;
        }
        moveState = QueueMoveState.Idle;
        isMoving = false;
        ReleaseReservedHole();
        ResetAllAntPipeVisuals();
        if (ropeVisual != null) ropeVisual.Cleanup();
        if (antUnits != null)
        {
            foreach (var u in antUnits)
            {
                if (u != null) u.StopWalkAnimation();
            }
        }
    }

    private void OnDestroy()
    {
        if (failedMoveCoroutine != null)
        {
            StopCoroutine(failedMoveCoroutine);
            failedMoveCoroutine = null;
        }
        moveState = QueueMoveState.Idle;
        isMoving = false;
        ReleaseReservedHole();
        ResetAllAntPipeVisuals();
        if (ropeVisual != null) ropeVisual.Cleanup();
        if (antUnits != null)
        {
            foreach (var u in antUnits)
            {
                if (u != null) u.KillWalkTween();
            }
        }
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
