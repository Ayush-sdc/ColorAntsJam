using System;
using UnityEngine;

public enum GameState
{
    NotStarted,
    Playing,
    LevelWon,
    LevelLost
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.NotStarted;
    [SerializeField] private float remainingTimer;
    [SerializeField] private int playerLives = 3;

    public GameState CurrentState => currentState;
    public float RemainingTimer => remainingTimer;
    public int PlayerLives => playerLives;

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

        LevelManager.OnLevelLoaded += HandleLevelLoaded;
        BoardManager.OnBoardStateChanged += CheckGameWinLossConditions;
        AntQueueController.OnQueueBlockedFeedback += HandleBlockedFeedback;
    }

    private void OnDestroy()
    {
        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
        BoardManager.OnBoardStateChanged -= CheckGameWinLossConditions;
        AntQueueController.OnQueueBlockedFeedback -= HandleBlockedFeedback;
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
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
        remainingTimer = level.TimeLimitInSeconds;
        currentState = GameState.Playing;

        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.SetupBoard(level);
        }

        OnGameStateChanged?.Invoke(currentState);
        OnTimerUpdated?.Invoke(remainingTimer);
        OnLivesUpdated?.Invoke(playerLives);
    }

    private void HandleBlockedFeedback(AntQueueController queue)
    {
        // When player clicks a blocked queue, decrement a life / heart if desired, or provide feedback
        // Here we notify UI of current lives
        OnLivesUpdated?.Invoke(playerLives);
    }

    /// <summary>
    /// Evaluates win (all queues cleared) and loss (deadlock: queues remain, but none can move).
    /// </summary>
    public void CheckGameWinLossConditions()
    {
        if (currentState != GameState.Playing) return;

        var activeQueues = BoardManager.Instance.ActiveQueues;

        // 1. Win condition: No ant queues remain on board
        if (activeQueues.Count == 0)
        {
            TriggerWin();
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

    public void RestartLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }
    }
}
