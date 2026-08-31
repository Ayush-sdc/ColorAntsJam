using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Top Bar UI Elements")]
    [SerializeField] private Text levelTitleText;
    [SerializeField] private Text difficultyBadgeText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text livesText;

    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Text loseReasonText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

    private void Awake()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnTimerUpdated += UpdateTimerUI;
        GameManager.OnLivesUpdated += UpdateLivesUI;
        LevelManager.OnLevelLoaded += HandleLevelLoaded;

        if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        if (retryButton != null) retryButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(() => GameManager.Instance.NextLevel());
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnTimerUpdated -= UpdateTimerUI;
        GameManager.OnLivesUpdated -= UpdateLivesUI;
        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void HandleLevelLoaded(Level level)
    {
        if (levelTitleText != null) levelTitleText.text = $"Level {level.LevelNumber}";
        if (difficultyBadgeText != null) difficultyBadgeText.text = level.Difficulty;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void UpdateTimerUI(float seconds)
    {
        if (timerText == null) return;
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{mins:00}:{secs:00}";
    }

    private void UpdateLivesUI(int lives)
    {
        if (livesText != null)
        {
            string heartString = "";
            for (int i = 0; i < Mathf.Max(0, lives); i++)
            {
                heartString += "<3 ";
            }
            livesText.text = heartString;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (winPanel != null) winPanel.SetActive(state == GameState.LevelWon);
        if (losePanel != null) losePanel.SetActive(state == GameState.LevelLost);
    }
}
