using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the brief head-look reaction for idle leader ants (queue index 0).
/// When an adjacent queue begins moving, the leader smoothly turns its HeadLookPivot
/// toward the moving ants, briefly tracks them, and returns to its neutral authored pose.
/// Workers and moving leaders do not react. Logical facing and collisions remain unaffected.
/// </summary>
public class LeaderHeadLook : MonoBehaviour
{
    public enum HeadLookState
    {
        Neutral,
        TurningToward,
        Watching,
        Returning
    }

    [Header("Head Look Pivot")]
    [Tooltip("Dedicated visual pivot transform located at the head/neck connection.")]
    [SerializeField] private Transform headLookPivot;

    [Header("Detection & Rotation Limits")]
    [Tooltip("Board-space detection radius in grid cells (default: 1.5 cells).")]
    [SerializeField] private float detectionRadius = 1.50f;

    [Tooltip("Maximum allowed head turn angle in degrees (default: 25 degrees).")]
    [SerializeField] private float maxHeadTurnAngle = 25.0f;

    [Header("Animation Durations (Seconds)")]
    [Tooltip("Time to smoothly rotate head toward the moving queue (default: 0.15s).")]
    [SerializeField] private float turnTowardDuration = 0.15f;

    [Tooltip("Time spent tracking/watching the moving queue (default: 0.40s).")]
    [SerializeField] private float watchDuration = 0.40f;

    [Tooltip("Time to smoothly return head to neutral authored rotation (default: 0.25s).")]
    [SerializeField] private float returnDuration = 0.25f;

    [Tooltip("Cooldown period after completing a reaction before another can trigger (default: 0.60s).")]
    [SerializeField] private float reactionCooldown = 0.60f;

    [Tooltip("Angular speed in degrees/sec for continuous tracking during Watch phase.")]
    [SerializeField] private float trackingAngularSpeed = 180f;

    [Header("Runtime State & Diagnostics")]
    [SerializeField] private HeadLookState currentState = HeadLookState.Neutral;
    [SerializeField] private float currentTurnAngle = 0f;
    [SerializeField] private string targetQueueId = "";

    public Transform HeadLookPivot => headLookPivot;
    public HeadLookState CurrentState => currentState;
    public float CurrentTurnAngle => currentTurnAngle;
    public string TargetQueueId => targetQueueId;
    public float DetectionRadius { get => detectionRadius; set => detectionRadius = value; }
    public float EffectiveDetectionRadius
    {
        get
        {
            float cs = 1.0f;
            if (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0f)
            {
                cs = BoardManager.Instance.CellSize;
            }
            return detectionRadius * cs;
        }
    }
    public float MaxHeadTurnAngle { get => maxHeadTurnAngle; set => maxHeadTurnAngle = value; }
    public float TurnTowardDuration { get => turnTowardDuration; set => turnTowardDuration = value; }
    public float WatchDuration { get => watchDuration; set => watchDuration = value; }
    public float ReturnDuration { get => returnDuration; set => returnDuration = value; }
    public float ReactionCooldown { get => reactionCooldown; set => reactionCooldown = value; }

    private AntUnit antUnit;
    private AntQueueController myQueue;
    private Quaternion initialLocalRotation = Quaternion.identity;
    private bool hasCapturedInitialRotation = false;

    private AntQueueController currentTargetQueue;
    private float stateTimer = 0f;
    private float nextAllowedReactionTime = 0f;
    private int lastTriggerFrame = -1;
    private float currentTargetDistance = float.MaxValue;
    private Quaternion startTurnRotation = Quaternion.identity;
    private Quaternion startReturnRotation = Quaternion.identity;

    private void Awake()
    {
        CacheComponents();
        EnsurePivot();
        CaptureInitialRotation();
    }

    private void Start()
    {
        CacheComponents();
        EnsurePivot();
        CaptureInitialRotation();
    }

    private void OnEnable()
    {
        CacheComponents();
        EnsurePivot();
        CaptureInitialRotation();

        AntQueueController.OnQueueMoveStarted += HandleGlobalQueueMoveStarted;
        if (BoardManager.Instance != null)
        {
            BoardManager.OnBoardStateChanged += HandleBoardStateChanged;
        }
    }

    private void OnDisable()
    {
        AntQueueController.OnQueueMoveStarted -= HandleGlobalQueueMoveStarted;
        BoardManager.OnBoardStateChanged -= HandleBoardStateChanged;
        ResetToNeutral();
    }

    private void OnDestroy()
    {
        AntQueueController.OnQueueMoveStarted -= HandleGlobalQueueMoveStarted;
        BoardManager.OnBoardStateChanged -= HandleBoardStateChanged;
        ResetToNeutral();
    }

    public void Initialize(Transform pivot, AntUnit unit)
    {
        this.headLookPivot = pivot;
        this.antUnit = unit;
        CacheComponents();
        CaptureInitialRotation();
    }

    private void CacheComponents()
    {
        if (antUnit == null) antUnit = GetComponent<AntUnit>();
        if (antUnit == null) antUnit = GetComponentInParent<AntUnit>();
        if (myQueue == null && antUnit != null) myQueue = antUnit.GetComponentInParent<AntQueueController>();
        if (myQueue == null) myQueue = GetComponentInParent<AntQueueController>();
    }

    public void EnsurePivot()
    {
        if (headLookPivot != null) return;

        // Search in hierarchy
        Transform found = transform.Find("VisualRoot/ChubbyAntColors/HeadLookPivot");
        if (found == null) found = transform.Find("HeadLookPivot");
        if (found == null)
        {
            // Search all children recursively
            var allChildren = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                if (allChildren[i].name == "HeadLookPivot")
                {
                    found = allChildren[i];
                    break;
                }
            }
        }
        if (found != null)
        {
            headLookPivot = found;
        }
    }

    private void CaptureInitialRotation()
    {
        if (headLookPivot != null && !hasCapturedInitialRotation)
        {
            initialLocalRotation = headLookPivot.localRotation;
            hasCapturedInitialRotation = true;
        }
    }

    private void HandleBoardStateChanged()
    {
        // Reset head pose on board changes or resets
        if (currentState != HeadLookState.Neutral)
        {
            ResetToNeutral();
        }
    }

    public void HandleGlobalQueueMoveStarted(AntQueueController movingQueue)
    {
        if (movingQueue == null) return;
        CacheComponents();

        // 1. Only leaders react. Workers do not react.
        if (antUnit != null && !antUnit.IsHead) return;

        // 2. If our own queue starts moving, immediately cancel reaction and restore forward head
        if (myQueue != null && (myQueue == movingQueue || myQueue.IsMoving || myQueue.MoveState != AntQueueController.QueueMoveState.Idle))
        {
            CancelReaction();
            return;
        }

        // 3. Leader must be idle, active, and not on cooldown
        if (!gameObject.activeInHierarchy || !enabled) return;
        if (headLookPivot == null)
        {
            EnsurePivot();
            if (headLookPivot == null) return;
        }
        CaptureInitialRotation();

        // 4. Check distance to moving queue segments in board-space
        float minDistance = ComputeBoardDistanceToQueue(movingQueue, out AntUnit closestUnit);
        if (minDistance > EffectiveDetectionRadius || closestUnit == null)
        {
            return;
        }

        // 5. Multiple queues starting simultaneously (e.g. linked groups):
        // If triggered in the same frame, prefer the nearest moving queue without duplicate animations.
        if (Time.frameCount == lastTriggerFrame && currentState != HeadLookState.Neutral)
        {
            if (minDistance < currentTargetDistance)
            {
                currentTargetQueue = movingQueue;
                currentTargetDistance = minDistance;
                targetQueueId = movingQueue.QueueId;
            }
            return;
        }

        // 6. Respect reaction cooldown when idle
        if (currentState == HeadLookState.Neutral && Time.time < nextAllowedReactionTime)
        {
            return;
        }

        // 7. If currently reacting from a previous event, do not jitter or rapidly switch targets
        if (currentState != HeadLookState.Neutral)
        {
            return;
        }

        // 8. Start reaction towards this moving queue
        StartReaction(movingQueue, minDistance);
    }

    private float ComputeBoardDistanceToQueue(AntQueueController queue, out AntUnit closestUnit)
    {
        closestUnit = null;
        if (queue == null || queue.Units == null) return float.MaxValue;

        Vector3 myPos = (headLookPivot != null) ? headLookPivot.position : transform.position;
        Vector2 myPos2D = new Vector2(myPos.x, myPos.z);
        float minSqDist = float.MaxValue;

        var units = queue.Units;
        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (u == null || !u.gameObject.activeInHierarchy || u.IsVisuallyHidden) continue;

            Vector3 uPos = u.transform.position;
            Vector2 uPos2D = new Vector2(uPos.x, uPos.z);
            float sqDist = (myPos2D - uPos2D).sqrMagnitude;
            if (sqDist < minSqDist)
            {
                minSqDist = sqDist;
                closestUnit = u;
            }
        }

        return Mathf.Sqrt(minSqDist);
    }

    private void StartReaction(AntQueueController queue, float initialDistance)
    {
        currentTargetQueue = queue;
        currentTargetDistance = initialDistance;
        targetQueueId = queue != null ? queue.QueueId : "";
        lastTriggerFrame = Time.frameCount;

        startTurnRotation = headLookPivot != null ? headLookPivot.localRotation : initialLocalRotation;
        stateTimer = 0f;
        currentState = HeadLookState.TurningToward;
    }

    private void Update()
    {
        StepHeadLook(Time.deltaTime);
    }

    /// <summary>
    /// Deterministic update step. Can be called by Update or directly by verification test suites.
    /// </summary>
    public void StepHeadLook(float dt)
    {
        if (headLookPivot == null) return;

        // If our own queue starts moving, cancel immediately
        if (myQueue != null && (myQueue.IsMoving || myQueue.MoveState != AntQueueController.QueueMoveState.Idle))
        {
            if (currentState != HeadLookState.Neutral)
            {
                CancelReaction();
            }
            return;
        }

        if (currentState == HeadLookState.Neutral)
        {
            return;
        }

        switch (currentState)
        {
            case HeadLookState.TurningToward:
                UpdateTurningToward(dt);
                break;

            case HeadLookState.Watching:
                UpdateWatching(dt);
                break;

            case HeadLookState.Returning:
                UpdateReturning(dt);
                break;
        }
    }

    private void UpdateTurningToward(float dt)
    {
        stateTimer += dt;
        float duration = Mathf.Max(0.01f, turnTowardDuration);
        float t = Mathf.Clamp01(stateTimer / duration);

        Quaternion targetRot = ComputeTargetHeadRotation();
        headLookPivot.localRotation = Quaternion.Slerp(startTurnRotation, targetRot, Mathf.SmoothStep(0f, 1f, t));
        UpdateCurrentAngleMetric();

        if (stateTimer >= duration)
        {
            currentState = HeadLookState.Watching;
            stateTimer = 0f;
        }
    }

    private void UpdateWatching(float dt)
    {
        stateTimer += dt;

        // Return early if the moving queue disappeared, finished, or moved out of range
        if (currentTargetQueue == null ||
            !currentTargetQueue.IsMoving ||
            currentTargetQueue.MoveState == AntQueueController.QueueMoveState.Idle)
        {
            TransitionToReturning();
            return;
        }

        float dist = ComputeBoardDistanceToQueue(currentTargetQueue, out AntUnit closestUnit);
        float cs = (BoardManager.Instance != null && BoardManager.Instance.CellSize > 0f) ? BoardManager.Instance.CellSize : 1.0f;
        if (closestUnit == null || dist > (EffectiveDetectionRadius + 0.80f * cs))
        {
            TransitionToReturning();
            return;
        }

        // Smoothly track moving queue ants
        Quaternion targetRot = ComputeTargetHeadRotation();
        headLookPivot.localRotation = Quaternion.RotateTowards(
            headLookPivot.localRotation,
            targetRot,
            trackingAngularSpeed * dt
        );
        UpdateCurrentAngleMetric();

        if (stateTimer >= watchDuration)
        {
            TransitionToReturning();
        }
    }

    private void TransitionToReturning()
    {
        currentState = HeadLookState.Returning;
        stateTimer = 0f;
        startReturnRotation = headLookPivot.localRotation;
    }

    private void UpdateReturning(float dt)
    {
        stateTimer += dt;
        float duration = Mathf.Max(0.01f, returnDuration);
        float t = Mathf.Clamp01(stateTimer / duration);

        headLookPivot.localRotation = Quaternion.Slerp(startReturnRotation, initialLocalRotation, Mathf.SmoothStep(0f, 1f, t));
        UpdateCurrentAngleMetric();

        if (stateTimer >= duration)
        {
            headLookPivot.localRotation = initialLocalRotation;
            currentState = HeadLookState.Neutral;
            currentTurnAngle = 0f;
            targetQueueId = "";
            currentTargetQueue = null;
            nextAllowedReactionTime = Time.time + reactionCooldown;
        }
    }

    private Quaternion ComputeTargetHeadRotation()
    {
        if (currentTargetQueue == null || headLookPivot == null)
        {
            return initialLocalRotation;
        }

        ComputeBoardDistanceToQueue(currentTargetQueue, out AntUnit closestUnit);
        if (closestUnit == null)
        {
            return initialLocalRotation;
        }

        Vector3 targetPos = closestUnit.transform.position;
        Vector3 pivotPos = headLookPivot.position;
        Vector3 worldDir = targetPos - pivotPos;
        worldDir.y = 0f;

        if (worldDir.sqrMagnitude < 0.0001f)
        {
            return initialLocalRotation;
        }

        // Convert world target direction into the pivot's parent coordinate space
        Transform parent = headLookPivot.parent != null ? headLookPivot.parent : transform;
        Vector3 localDir = parent.InverseTransformDirection(worldDir);

        // Yaw angle: positive is right, negative is left
        float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        // Clamp head turn to never twist backward or detach
        float clampedYaw = Mathf.Clamp(yaw, -maxHeadTurnAngle, maxHeadTurnAngle);

        return initialLocalRotation * Quaternion.Euler(0f, clampedYaw, 0f);
    }

    private void UpdateCurrentAngleMetric()
    {
        if (headLookPivot == null)
        {
            currentTurnAngle = 0f;
            return;
        }

        float delta = Mathf.DeltaAngle(initialLocalRotation.eulerAngles.y, headLookPivot.localEulerAngles.y);
        currentTurnAngle = Mathf.Clamp(delta, -180f, 180f);
    }

    /// <summary>
    /// Cancels any active reaction and smoothly/immediately restores the neutral forward-facing head pose.
    /// </summary>
    public void CancelReaction()
    {
        if (headLookPivot != null && hasCapturedInitialRotation)
        {
            headLookPivot.localRotation = initialLocalRotation;
        }
        currentState = HeadLookState.Neutral;
        currentTurnAngle = 0f;
        targetQueueId = "";
        currentTargetQueue = null;
        stateTimer = 0f;
    }

    /// <summary>
    /// Resets the head pose cleanly to the authored neutral rotation.
    /// </summary>
    public void ResetToNeutral()
    {
        CancelReaction();
        nextAllowedReactionTime = 0f;
    }

    /// <summary>
    /// Direct programmatic trigger for verification test suites.
    /// </summary>
    public void TestTriggerReaction(AntQueueController movingQueue)
    {
        if (movingQueue == null) return;
        CacheComponents();
        EnsurePivot();
        CaptureInitialRotation();
        float dist = ComputeBoardDistanceToQueue(movingQueue, out _);
        StartReaction(movingQueue, dist);
    }
}
