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
    public Vector2Int ForwardDirection { get => forwardDirection; set => forwardDirection = value; }
    public Vector2Int HoleEntranceCoordinate { get => holeEntranceCoordinate; set => holeEntranceCoordinate = value; }
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
    [SerializeField] private float segmentDistance = 0.85f;
    private readonly List<TrailPoint> trailHistory = new List<TrailPoint>();
    private float totalTrailDistance = 0f;
    private List<Vector3> currentSmoothRoute = new List<Vector3>();

    public IReadOnlyList<TrailPoint> TrailHistory => trailHistory;

    public void StartSmoothMovement(List<Vector3> worldPath, HoleController targetHole, Vector2Int entranceCell)
    {
        if (isMoving) return;
        this.destinationHole = targetHole;
        this.holeEntranceCoordinate = entranceCell;
        this.currentSmoothRoute = (worldPath != null && worldPath.Count > 0) ? new List<Vector3>(worldPath) : new List<Vector3>();
        StartCoroutine(SmoothFreeMoveRoutine());
    }

    public void StartForwardMovement(Vector2Int direction, HoleController targetHole, Vector2Int entranceCell)
    {
        if (isMoving) return;
        this.forwardDirection = direction;
        this.destinationHole = targetHole;
        this.holeEntranceCoordinate = entranceCell;

        if (BoardManager.Instance != null && BoardManager.Instance.UseAutomaticPathfinding)
        {
            var cellPath = AntPathfinder.FindPath(BoardManager.Instance, this, out var hole, out var entrance);
            if (cellPath != null && cellPath.Count >= 2)
            {
                var smoothRoute = BoardManager.Instance.GenerateSmoothFreeWorldPath(cellPath, this, hole ?? targetHole);
                StartSmoothMovement(smoothRoute, hole ?? targetHole, entrance);
                return;
            }
        }

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

        if (antUnits.Count > 1 && antUnits[0] != null && antUnits[1] != null)
        {
            segmentDistance = Vector3.Distance(antUnits[0].transform.position, antUnits[1].transform.position);
            if (segmentDistance < 0.1f) segmentDistance = 0.85f;
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

    private void AddTrailPoint(Vector3 newPos, Vector3 forward)
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

        // Trim old trail points that are far behind the tail
        float tailDist = totalTrailDistance - (antUnits.Count + 3) * segmentDistance;
        while (trailHistory.Count > 2 && trailHistory[1].distance < tailDist)
        {
            trailHistory.RemoveAt(0);
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

        isMoving = true;
        for (int i = 0; i < totalAnts; i++)
        {
            var u = units[i];
            if (u != null)
            {
                u.IsMoving = true;
                u.StartWalkAnimation(i, moveSpeed);
            }
        }
        OnQueueMoveStarted?.Invoke(this);

        InitializeTrail();

        float initialAntY = units[0] != null ? units[0].transform.position.y : 0.05f;
        Vector3 holeDespawnPoint = (currentSmoothRoute.Count > 0) 
            ? currentSmoothRoute[currentSmoothRoute.Count - 1] 
            : (destinationHole != null ? destinationHole.GetHoleDespawnPoint(holeEntranceCoordinate, forwardDirection, 0.85f, this) : transform.position);

        Vector3 holeEntryPoint = destinationHole != null 
            ? destinationHole.GetHoleEntryPoint(holeEntranceCoordinate, forwardDirection, this) 
            : holeDespawnPoint;

        int routeIndex = 0;
        Vector3 currentHeadPos = units[0] != null ? units[0].transform.position : transform.position;

        bool[] hasStartedDiving = new bool[totalAnts];
        int swallowedAntCount = 0;
        bool headReachedPit = false;
        float pitReachDistance = 0f;
        float virtualHeadDist = totalTrailDistance;
        bool hasAcquiredHoleEntry = false;

        while (swallowedAntCount < totalAnts)
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float stepBudget = moveSpeed * dt;

            // 1. Advance head along route until HoleDespawnPoint is reached
            if (!headReachedPit)
            {
                // Check if head is crossing into the hole entrance rim
                if (!hasAcquiredHoleEntry && destinationHole != null)
                {
                    float distToEntry = Vector3.Distance(currentHeadPos, holeEntryPoint);
                    if (distToEntry < 0.35f)
                    {
                        destinationHole.RequestHoleEntry(this);
                        hasAcquiredHoleEntry = true;
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

                    if (currentHeadPos.y < initialAntY - 0.03f)
                    {
                        if (unit.IsWalkTweenActive)
                        {
                            unit.StopWalkAnimation();
                        }
                        float pitDescent = Mathf.Clamp01((initialAntY - currentHeadPos.y) / 0.20f);
                        unit.transform.localScale = Vector3.one * (1.0f - pitDescent * 0.70f);
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

                        // Smoothly descend into recessed dark pit and stop walk tween safely
                        if (followPos.y < initialAntY - 0.03f)
                        {
                            if (unit.IsWalkTweenActive)
                            {
                                unit.StopWalkAnimation();
                            }
                            float pitDescent = Mathf.Clamp01((initialAntY - followPos.y) / 0.20f);
                            unit.transform.localScale = Vector3.one * (1.0f - pitDescent * 0.70f);
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

                    if (destinationHole != null)
                    {
                        destinationHole.OnAntEnteredHole();
                    }

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
        foreach (var u in antUnits)
        {
            if (u != null)
            {
                u.IsMoving = false;
                u.StopWalkAnimation();
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

        destinationHole = null;
    }

    public void ReleaseReservedHole()
    {
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
        ReleaseReservedHole();
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
        ReleaseReservedHole();
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
