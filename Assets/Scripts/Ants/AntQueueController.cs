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
    [SerializeField] private float jumpPower = 0.42f;
    [SerializeField] private float enterHoleJumpPower = 1.1f;

    [SerializeField] private List<AntUnit> antUnits = new List<AntUnit>();

    public string QueueId => queueId;
    public AntColor Color => antColor;
    public int AntCount => antCount;
    public bool IsMoving => isMoving;
    public IReadOnlyList<Vector2Int> CurrentBodyCoordinates => currentBodyCoordinates;
    public IReadOnlyList<Vector2Int> RemainingPath => remainingPath;
    public IReadOnlyList<AntUnit> Units => antUnits;

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
            Vector3 worldPos = BoardManager.Instance != null ? BoardManager.Instance.GridToWorldPosition(coord) : new Vector3(coord.x, 0, coord.y);

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
    /// Strictly aligns every individual AntUnit to face the segment directly ahead of it (or the next path step).
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
                // Lead ant faces the next step on the path towards the goal tray
                if (remainingPath != null && remainingPath.Count > 1)
                {
                    Vector3 nextPos = BoardManager.Instance != null ? BoardManager.Instance.GridToWorldPosition(remainingPath[1]) : new Vector3(remainingPath[1].x, 0, remainingPath[1].y);
                    lookDir = nextPos - unit.transform.position;
                }
                else if (currentBodyCoordinates.Count > 1)
                {
                    // Fallback: face away from the segment behind it
                    Vector3 behindPos = BoardManager.Instance != null ? BoardManager.Instance.GridToWorldPosition(currentBodyCoordinates[1]) : new Vector3(currentBodyCoordinates[1].x, 0, currentBodyCoordinates[1].y);
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

    /// <summary>
    /// Attempts to trigger this queue to move along its predefined path to its hole tray.
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

    public void PlayBlockedShake()
    {
        foreach (var unit in antUnits)
        {
            if (unit == null) continue;
            unit.transform.DOKill();
            unit.transform.DOShakePosition(0.35f, new Vector3(0.18f, 0f, 0.18f), 15, 90, false, true);
        }
    }

    /// <summary>
    /// Strict follow-the-leader chain movement:
    /// - Segment [i] only steps into the exact cell vacated by segment [i-1].
    /// - No segment skips any grid cell.
    /// - Each segment only dives into the tray once it reaches the cell directly adjacent to the tray.
    /// </summary>
    private IEnumerator MoveRoutine()
    {
        isMoving = true;
        OnQueueMoveStarted?.Invoke(this);

        Vector2Int holeCoord = remainingPath[remainingPath.Count - 1];
        HoleController hole = BoardManager.Instance.GetHoleAt(holeCoord);
        Vector3 holeWorldPos = BoardManager.Instance.GridToWorldPosition(holeCoord);

        // Precompute the complete unbroken path of coordinates that each ant must traverse
        // For Ant 0: [ remainingPath[1], remainingPath[2], ..., holeCoord ]
        // For Ant i: [ bodyCoord[i-1], bodyCoord[i-2], ..., bodyCoord[0], remainingPath[1], ..., holeCoord ]
        int initialAntCount = antUnits.Count;
        List<List<Vector2Int>> antTracks = new List<List<Vector2Int>>(initialAntCount);

        for (int i = 0; i < initialAntCount; i++)
        {
            List<Vector2Int> track = new List<Vector2Int>();
            // Historical body coordinates ahead of ant i
            for (int h = i - 1; h >= 0; h--)
            {
                track.Add(currentBodyCoordinates[h]);
            }
            // Path steps ahead of the head
            for (int p = 1; p < remainingPath.Count; p++)
            {
                track.Add(remainingPath[p]);
            }
            antTracks.Add(track);
        }

        // Total number of discrete steps to ingest the entire queue
        int totalSteps = (remainingPath.Count - 1) + initialAntCount;

        // Active tracking lists
        List<AntUnit> activeUnits = new List<AntUnit>(antUnits);
        List<int> originalIndices = new List<int>();
        for (int i = 0; i < initialAntCount; i++) originalIndices.Add(i);

        for (int step = 0; step < totalSteps; step++)
        {
            Sequence stepSequence = DOTween.Sequence();
            List<int> toRemoveAtStepEnd = new List<int>();

            for (int a = 0; a < activeUnits.Count; a++)
            {
                var unit = activeUnits[a];
                if (unit == null) continue;

                int origIndex = originalIndices[a];
                var track = antTracks[origIndex];

                if (step < track.Count)
                {
                    Vector2Int targetCoord = track[step];

                    if (targetCoord == holeCoord)
                    {
                        // High-arc dive jump into hole pit from the entrance cell
                        Vector3 targetPit = holeWorldPos + new Vector3(0f, -0.2f, 0f);
                        stepSequence.Join(unit.transform.DOJump(targetPit, enterHoleJumpPower, 1, stepDuration * 1.2f).SetEase(Ease.InQuad));
                        stepSequence.Join(unit.transform.DOScale(Vector3.zero, stepDuration * 1.2f).SetEase(Ease.InBack));

                        // Decrement hole count immediately as soon as the ant enters the hole!
                        if (hole != null)
                        {
                            hole.OnAntSwallowed();
                        }

                        toRemoveAtStepEnd.Add(a);
                    }
                    else
                    {
                        // Strict 1-tile hop along the grid
                        Vector3 targetPos = BoardManager.Instance.GridToWorldPosition(targetCoord);
                        Vector3 moveDir = targetPos - unit.transform.position;
                        moveDir.y = 0;

                        if (moveDir.sqrMagnitude > 0.001f)
                        {
                            stepSequence.Join(unit.transform.DORotateQuaternion(Quaternion.LookRotation(moveDir), stepDuration * 0.5f));
                        }
                        stepSequence.Join(unit.transform.DOJump(targetPos, jumpPower, 1, stepDuration).SetEase(Ease.Linear));

                        // Update current coordinates of active body
                        if (a < currentBodyCoordinates.Count)
                        {
                            currentBodyCoordinates[a] = targetCoord;
                        }
                    }
                }
            }

            yield return stepSequence.WaitForCompletion();

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
                originalIndices.RemoveAt(remIdx);
                antUnits.Remove(swallowedUnit);

                SafeDestroy(swallowedUnit.gameObject);
            }
        }

        isMoving = false;
        OnQueueMoveCompleted?.Invoke(this);
        BoardManager.Instance.OnQueueCompleted(this);
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && antUnits.Count > 0)
        {
            UpdateAntOrientations();
        }
    }
#endif
}
