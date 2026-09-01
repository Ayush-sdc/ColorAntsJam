using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameUIManager : MonoBehaviour
{
    [Header("Top Bar UI Elements")]
    [SerializeField] private Text timerText;
    [SerializeField] private Text levelNumberText;
    [SerializeField] private Image[] heartImages;

    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Text loseReasonText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
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
        if (settingsButton != null) settingsButton.onClick.AddListener(ToggleSoundSettings);
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnTimerUpdated -= UpdateTimerUI;
        GameManager.OnLivesUpdated -= UpdateLivesUI;
        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void ToggleSoundSettings()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ToggleMute();
        }
    }

    private void HandleLevelLoaded(Level level)
    {
        if (levelNumberText != null) levelNumberText.text = level.LevelNumber.ToString();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void UpdateTimerUI(float seconds)
    {
        if (timerText == null) return;
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{mins}:{secs:00}";
    }

    private void UpdateLivesUI(int lives)
    {
        if (heartImages != null && heartImages.Length > 0)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] != null)
                {
                    bool isAlive = i < lives;
                    heartImages[i].color = isAlive ? Color.white : new Color(0.25f, 0.25f, 0.30f, 0.35f);
                }
            }
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (winPanel != null)
        {
            winPanel.SetActive(state == GameState.LevelWon);
            if (state == GameState.LevelWon)
            {
                winPanel.transform.localScale = Vector3.one * 0.7f;
                winPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }
        }

        if (losePanel != null)
        {
            losePanel.SetActive(state == GameState.LevelLost);
            if (state == GameState.LevelLost)
            {
                losePanel.transform.localScale = Vector3.one * 0.7f;
                losePanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }
        }
    }
}
