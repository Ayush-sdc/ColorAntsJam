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

    public GameState CurrentState => currentState;
    public float RemainingTimer => remainingTimer;
    public int PlayerLives => playerLives;
    public bool IsTimerStarted => timerStarted;

    public static event Action<GameState> OnGameStateChanged;
    public static event Action<float> OnTimerUpdated;
    public static event Action<int> OnLivesUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
            if (remainingTimer > 0f)
            {
                remainingTimer -= Time.deltaTime;
                OnTimerUpdated?.Invoke(remainingTimer);

                if (remainingTimer <= 0f)
                {
                    remainingTimer = 0f;
                    TriggerGameOver("Time ran out!");
                }
            }
        }
    }

    private void HandleLevelLoaded(Level level)
    {
        currentState = GameState.NotStarted;
        timerStarted = false;
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
    /// If lives reach 0, immediately triggers LevelLost.
    /// </summary>
    public void LoseLife(InvalidMoveReason reason = InvalidMoveReason.NoValidPath)
    {
        if (currentState != GameState.Playing) return;

        int livesBefore = playerLives;
        playerLives = Mathf.Max(0, playerLives - 1);
        Debug.Log($"[InvalidMove] Reason={reason} LivesBefore={livesBefore} LivesAfter={playerLives}");

        OnLivesUpdated?.Invoke(playerLives);

        if (playerLives <= 0)
        {
            playerLives = 0;
            Debug.Log("[LevelFailed] Reason=LivesDepleted");
            TriggerGameOver("Lives depleted!");
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
        currentState = GameState.LevelWon;
        Debug.Log("[GameManager] Level Won! All ant queues entered their holes.");
        OnGameStateChanged?.Invoke(currentState);
    }

    private void TriggerGameOver(string reason)
    {
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
