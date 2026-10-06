using System;
using UnityEngine;
using DG.Tweening;

public enum GameState
{
    NotStarted,
    Playing,
    Paused,
    LevelWon,
    LevelLost
}

public enum InvalidMoveReason
{
    NoValidPath,
    NoValidForwardPath,
    BackwardMoveForbidden,
    OccupancyCollision,
    BlockedCell
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.NotStarted;
    [SerializeField] private float remainingTimer;
    [SerializeField] private int playerLives = 3;
    [SerializeField] private bool timerStarted = false;
    private bool isGameOverDeferred = false;
    private string deferredGameOverReason = "";

    public GameState CurrentState => currentState;
    public float RemainingTimer => remainingTimer;
    public int PlayerLives => playerLives;
    public bool IsTimerStarted => timerStarted;
    public bool IsGameOverDeferred => isGameOverDeferred;

    public static event Action<GameState> OnGameStateChanged;
    public static event Action<float> OnTimerUpdated;
    public static event Action<int> OnLivesUpdated;

    [Header("Testing Mode")]
    [SerializeField] private bool isTimerFrozen = false;
    public bool IsTimerFrozen
    {
        get => isTimerFrozen;
        set
        {
            isTimerFrozen = value;
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool("SlitherAnts_FreezeTimer", isTimerFrozen);
#endif
            OnTimerFrozenChanged?.Invoke(isTimerFrozen);
        }
    }
    public static event Action<bool> OnTimerFrozenChanged;

    private bool isFinishingAfterTimeout = false;
    public bool IsFinishingAfterTimeout => isFinishingAfterTimeout;

    public void SetTestingState(GameState state, int lives)
    {
        currentState = state;
        playerLives = lives;
        isGameOverDeferred = false;
        deferredGameOverReason = "";
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
#if UNITY_EDITOR
        isTimerFrozen = UnityEditor.EditorPrefs.GetBool("SlitherAnts_FreezeTimer", false);
#else
        isTimerFrozen = false;
#endif
        DOTween.SetTweensCapacity(750, 150);

        LevelManager.OnLevelLoaded += HandleLevelLoaded;
        BoardManager.OnBoardStateChanged += CheckGameWinLossConditions;
        AntQueueController.OnQueueBlockedFeedback += HandleBlockedFeedback;
        AntQueueController.OnQueueMoveStarted += HandleQueueMoveStarted;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
        BoardManager.OnBoardStateChanged -= CheckGameWinLossConditions;
        AntQueueController.OnQueueBlockedFeedback -= HandleBlockedFeedback;
        AntQueueController.OnQueueMoveStarted -= HandleQueueMoveStarted;
    }

    private void Update()
    {
        if (currentState == GameState.Playing && timerStarted)
        {
#if UNITY_EDITOR
            if (isTimerFrozen)
            {
                // In testing mode, timer is frozen, timeout failure disabled
                return;
            }
#endif
            if (remainingTimer > 0f)
            {
                remainingTimer -= Time.deltaTime;
                if (remainingTimer < 0f) remainingTimer = 0f;
                OnTimerUpdated?.Invoke(remainingTimer);

                if (remainingTimer <= 0f)
                {
                    remainingTimer = 0f;
                    EvaluateTimeoutOrFinishingGrace();
                }
            }
            else if (isFinishingAfterTimeout)
            {
                EvaluateFinishingGraceProgress();
            }
        }
    }

    private void EvaluateTimeoutOrFinishingGrace()
    {
        if (currentState != GameState.Playing) return;
        if (isGameOverDeferred) return; // Failure sequence is finishing; do not show duplicate fail overlay

        // Check if all remaining work is already moving on accepted rescue routes:
        // Must satisfy:
        // 1. At least 1 queue is moving.
        // 2. ALL remaining active queues are moving (no stationary queues left).
        // 3. No dormant/unrevealed stacked layers remain.
        // 4. No remaining queues are locked.
        var bm = BoardManager.Instance;
        if (bm != null && bm.MovingQueuesCount > 0)
        {
            bool allActiveAreMoving = true;
            foreach (var q in bm.ActiveQueues)
            {
                if (q == null || !q.IsMoving || q.IsLocked || q.MoveState != AntQueueController.QueueMoveState.MovingToRescue)
                {
                    allActiveAreMoving = false;
                    break;
                }
            }

            bool hasDormantStacked = bm.DormantStackedQueues != null && bm.DormantStackedQueues.Count > 0;

            if (allActiveAreMoving && !hasDormantStacked)
            {
                // All remaining required rescues were accepted before expiry and are currently traveling!
                // Enter finishing state: clamp timer at 0:00, allow accepted rescues to finish.
                isFinishingAfterTimeout = true;
                Debug.Log("[GameManager] Timeout reached, but final accepted rescue(s) are moving. Entering finishing state at 0:00.");
                return;
            }
        }

        // Stationary, locked, or dormant queue remains when time expires -> normal timeout failure
        isFinishingAfterTimeout = false;
        TriggerGameOver("Time ran out!");
    }

    private void EvaluateFinishingGraceProgress()
    {
        if (currentState != GameState.Playing) return;

        var bm = BoardManager.Instance;
        if (bm == null)
        {
            TriggerGameOver("Time ran out!");
            return;
        }

        // Once moving queues reached 0, evaluate whether all required queues finished
        if (bm.MovingQueuesCount == 0)
        {
            isFinishingAfterTimeout = false;
            if (bm.ActiveQueues.Count == 0 && (bm.DormantStackedQueues == null || bm.DormantStackedQueues.Count == 0))
            {
                TriggerWin();
            }
            else
            {
                TriggerGameOver("Time ran out!");
            }
        }
    }

    private void HandleLevelLoaded(Level level)
    {
        currentState = GameState.NotStarted;
        timerStarted = false;
        isFinishingAfterTimeout = false;
        isGameOverDeferred = false;
        deferredGameOverReason = "";
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.SetInteractable(true);
        }
        remainingTimer = level.TimeLimitInSeconds;
        playerLives = 3;

        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.SetupBoard(level);
        }

        currentState = GameState.Playing;

        OnGameStateChanged?.Invoke(currentState);
        OnTimerUpdated?.Invoke(remainingTimer);
        OnLivesUpdated?.Invoke(playerLives);
    }

    private void HandleQueueMoveStarted(AntQueueController queue)
    {
        StartTimerOnFirstValidMove(queue);
    }

    /// <summary>
    /// Starts the level countdown timer when the first valid ant queue move is successfully accepted.
    /// Subsequent calls do nothing.
    /// </summary>
    public void StartTimerOnFirstValidMove(AntQueueController queue = null)
    {
        if (currentState == GameState.Playing && !timerStarted)
        {
            timerStarted = true;
            Debug.Log($"[Timer] First valid ant queue move accepted (Queue={(queue != null ? queue.QueueId : "Manual")}). Level timer started!");
        }
    }

    private void HandleBlockedFeedback(AntQueueController queue)
    {
        // Visual/audio feedback hook
    }

    /// <summary>
    /// Deducts exactly one life per invalid move attempt or gameplay collision.
    /// If deferGameOver is true and lives reach 0, allows in-flight recoil/return animation to finish before showing fail screen.
    /// </summary>
    public void LoseLife(InvalidMoveReason reason = InvalidMoveReason.NoValidPath, bool deferGameOver = false)
    {
        if (currentState != GameState.Playing) return;

        int livesBefore = playerLives;
        playerLives = Mathf.Max(0, playerLives - 1);
        Debug.Log($"[InvalidMove] Reason={reason} LivesBefore={livesBefore} LivesAfter={playerLives} DeferGameOver={deferGameOver}");

        OnLivesUpdated?.Invoke(playerLives);

        if (playerLives <= 0)
        {
            playerLives = 0;
            if (deferGameOver)
            {
                isGameOverDeferred = true;
                deferredGameOverReason = "Lives depleted!";
                Debug.Log("[LevelFailed] Reason=LivesDepleted (Deferred until failed return finishes)");
                if (PlayerInputHandler.Instance != null)
                {
                    PlayerInputHandler.Instance.SetInteractable(false);
                }
            }
            else
            {
                Debug.Log("[LevelFailed] Reason=LivesDepleted");
                TriggerGameOver("Lives depleted!");
            }
        }
    }

    /// <summary>
    /// Called when an ant queue completes its return motion after a failed move attempt.
    /// If the last heart was lost during impact, triggers LevelLost now that return has cleanly finished.
    /// </summary>
    public void NotifyFailedMoveReturnCompleted(AntQueueController queue)
    {
        if (isGameOverDeferred && currentState == GameState.Playing)
        {
            isGameOverDeferred = false;
            TriggerGameOver(deferredGameOverReason);
        }
    }

    /// <summary>
    /// Evaluates win (all queues cleared) and loss (deadlock: queues remain, but none can move).
    /// </summary>
    public void CheckGameWinLossConditions()
    {
        if (currentState != GameState.Playing) return;
        if (BoardManager.Instance == null || BoardManager.Instance.IsSettingUpBoard) return;

        var activeQueues = BoardManager.Instance.ActiveQueues;
        if (activeQueues == null) return;

        // 1. Win condition: No ant queues remain on board (including revealed and dormant stacked layers)
        if (activeQueues.Count == 0 && (BoardManager.Instance.DormantStackedQueues == null || BoardManager.Instance.DormantStackedQueues.Count == 0))
        {
            TriggerWin();
            return;
        }

        // If another queue is currently moving, board occupancy is still in flux; do not evaluate deadlock yet
        if (BoardManager.Instance != null && BoardManager.Instance.MovingQueuesCount > 0)
        {
            return;
        }

        // 2. Deadlock condition: There are remaining queues, but none can make a valid move
        bool anyCanMove = false;
        foreach (var q in activeQueues)
        {
            if (BoardManager.Instance.IsPathClearForQueue(q))
            {
                anyCanMove = true;
                break;
            }
        }

        if (!anyCanMove)
        {
            TriggerGameOver("Deadlock! No remaining ant queues have an unblocked path to their matching hole.");
        }
    }

    private void TriggerWin()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.LevelWon;
        Debug.Log("[GameManager] Level Won! All ant queues entered their holes.");
        OnGameStateChanged?.Invoke(currentState);
    }

    private void TriggerGameOver(string reason)
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.LevelLost;
        Debug.Log($"[GameManager] Level Lost! Reason: {reason}");
        OnGameStateChanged?.Invoke(currentState);
    }

    public void NextLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.AdvanceToNextLevel();
        }
    }

    public void PreviousLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadPreviousLevel();
        }
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Debug.Log("[GameManager] Game Paused.");
            OnGameStateChanged?.Invoke(currentState);
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Debug.Log("[GameManager] Game Resumed.");
            OnGameStateChanged?.Invoke(currentState);
        }
    }

    public void SetPaused(bool paused)
    {
        if (paused) PauseGame();
        else ResumeGame();
    }

    public void RestartLevel()
    {
        timerStarted = false;
        isGameOverDeferred = false;
        deferredGameOverReason = "";
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.SetInteractable(true);
        }
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }
    }

    public void SetGameState(GameState state)
    {
        currentState = state;
        OnGameStateChanged?.Invoke(currentState);
    }
}
