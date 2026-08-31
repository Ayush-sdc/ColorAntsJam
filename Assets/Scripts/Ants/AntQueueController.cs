using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AntQueueController : MonoBehaviour
{
    [Header("Queue Data")]
    [SerializeField] private string queueId;
    [SerializeField] private AntColor antColor;
    [SerializeField] private int antCount;

    [Header("Runtime State")]
    [SerializeField] private bool isMoving = false;
    [SerializeField] private List<Vector2Int> currentBodyCoordinates = new List<Vector2Int>();
    [SerializeField] private List<Vector2Int> remainingPath = new List<Vector2Int>();

    [Header("Animation Settings")]
    [SerializeField] private float stepDuration = 0.16f;
    [SerializeField] private float jumpPower = 0.45f;
    [SerializeField] private float enterHoleJumpPower = 1.1f;

    private readonly List<AntUnit> antUnits = new List<AntUnit>();

    public string QueueId => queueId;
    public AntColor Color => antColor;
    public int AntCount => antCount;
    public bool IsMoving => isMoving;
    public IReadOnlyList<Vector2Int> CurrentBodyCoordinates => currentBodyCoordinates;
    public IReadOnlyList<Vector2Int> RemainingPath => remainingPath;

    public static event Action<AntQueueController> OnQueueMoveStarted;
    public static event Action<AntQueueController> OnQueueMoveCompleted;
    public static event Action<AntQueueController> OnQueueBlockedFeedback;

    public void Initialize(AntQueueData data)
    {
        queueId = data.QueueId;
        antColor = data.AntColor;
        antCount = data.AntCount;

        currentBodyCoordinates = new List<Vector2Int>(data.InitialBodyCoordinates);
        remainingPath = new List<Vector2Int>(data.FullPathToHole);

        SpawnAnts();
    }

    private void SpawnAnts()
    {
        // Clear previous units if any
        foreach (var unit in antUnits)
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        antUnits.Clear();

        for (int i = 0; i < currentBodyCoordinates.Count; i++)
        {
            Vector2Int coord = currentBodyCoordinates[i];
            Vector3 worldPos = BoardManager.Instance.GridToWorldPosition(coord);

            GameObject antObj = new GameObject($"Ant_{queueId}_{i}");
            antObj.transform.SetParent(transform);
            antObj.transform.position = worldPos;

            // Orient towards the previous segment or along the path
            if (i == 0 && remainingPath.Count > 1)
            {
                Vector3 lookDir = BoardManager.Instance.GridToWorldPosition(remainingPath[1]) - worldPos;
                lookDir.y = 0;
                if (lookDir != Vector3.zero) antObj.transform.rotation = Quaternion.LookRotation(lookDir);
            }
            else if (i > 0)
            {
                Vector3 lookDir = BoardManager.Instance.GridToWorldPosition(currentBodyCoordinates[i - 1]) - worldPos;
                lookDir.y = 0;
                if (lookDir != Vector3.zero) antObj.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            var antUnit = antObj.AddComponent<AntUnit>();
            antUnit.Setup(antColor, (i == 0));
            antUnits.Add(antUnit);
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

    /// <summary>
    /// Attempts to trigger this queue to move along its predefined path to its hole.
    /// Returns true if the move started, false if blocked or unable to move.
    /// </summary>
    public bool TryStartMove()
    {
        if (isMoving) return false;

        // Check path validity against BoardManager
        if (!BoardManager.Instance.IsPathClearForQueue(this))
        {
            PlayBlockedShake();
            OnQueueBlockedFeedback?.Invoke(this);
            return false;
        }

        StartCoroutine(MoveRoutine());
        return true;
    }

    /// <summary>
    /// Bounces/shakes the queue to provide visual feedback that it is currently blocked.
    /// </summary>
    public void PlayBlockedShake()
    {
        foreach (var unit in antUnits)
        {
            if (unit == null) continue;
            unit.transform.DOKill();
            unit.transform.DOShakePosition(0.35f, new Vector3(0.18f, 0f, 0.18f), 15, 90, false, true);
        }
    }

    private IEnumerator MoveRoutine()
    {
        isMoving = true;
        OnQueueMoveStarted?.Invoke(this);

        // Path steps to traverse from current head position (index 0) to hole (last index)
        // remainingPath[0] is current head
        int pathSteps = remainingPath.Count - 1;

        Vector2Int holeCoord = remainingPath[remainingPath.Count - 1];
        HoleController hole = BoardManager.Instance.GetHoleAt(holeCoord);
        Vector3 holeWorldPos = BoardManager.Instance.GridToWorldPosition(holeCoord);

        for (int step = 1; step <= pathSteps; step++)
        {
            Vector2Int nextHeadCoord = remainingPath[step];

            // 1. Shift body coordinates: Head moves to nextHeadCoord, each segment takes position of segment in front
            Vector2Int prevHead = currentBodyCoordinates[0];
            for (int b = currentBodyCoordinates.Count - 1; b > 0; b--)
            {
                currentBodyCoordinates[b] = currentBodyCoordinates[b - 1];
            }
            currentBodyCoordinates[0] = nextHeadCoord;

            // 2. Animate all active visual units along the shift simultaneously
            Sequence stepSequence = DOTween.Sequence();

            for (int i = 0; i < antUnits.Count; i++)
            {
                var unit = antUnits[i];
                if (unit == null) continue;

                Vector3 targetPos = BoardManager.Instance.GridToWorldPosition(currentBodyCoordinates[i]);
                Vector3 moveDir = targetPos - unit.transform.position;
                moveDir.y = 0;

                // Rotate towards movement direction
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    stepSequence.Join(unit.transform.DORotateQuaternion(Quaternion.LookRotation(moveDir), stepDuration * 0.5f));
                }

                // Smooth jump hop to the next cell
                stepSequence.Join(unit.transform.DOJump(targetPos, jumpPower, 1, stepDuration).SetEase(Ease.Linear));
            }

            yield return stepSequence.WaitForCompletion();
        }

        // Now head is at the hole! Ants enter the hole one by one in queue order with a satisfying jump/swallow animation
        yield return StartCoroutine(EnterHoleRoutine(hole, holeWorldPos));

        isMoving = false;
        OnQueueMoveCompleted?.Invoke(this);
        BoardManager.Instance.OnQueueCompleted(this);
    }

    private IEnumerator EnterHoleRoutine(HoleController hole, Vector3 holeWorldPos)
    {
        for (int i = 0; i < antUnits.Count; i++)
        {
            var unit = antUnits[i];
            if (unit == null) continue;

            Sequence enterSeq = DOTween.Sequence();

            // High arc jump into hole pit
            Vector3 targetPit = holeWorldPos + new Vector3(0f, -0.2f, 0f);
            enterSeq.Append(unit.transform.DOJump(targetPit, enterHoleJumpPower, 1, stepDuration * 1.5f).SetEase(Ease.InQuad));
            enterSeq.Join(unit.transform.DOScale(Vector3.zero, stepDuration * 1.5f).SetEase(Ease.InBack));

            enterSeq.OnComplete(() =>
            {
                if (hole != null) hole.PlaySwallowEffect();
                Destroy(unit.gameObject);
            });

            // Delay between consecutive ants jumping in
            yield return new WaitForSeconds(stepDuration * 0.9f);
        }

        // Small pause to let the final swallow effect conclude
        yield return new WaitForSeconds(0.2f);
    }
}
