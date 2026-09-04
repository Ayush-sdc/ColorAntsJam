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

    [Header("Settings Modal")]
    [SerializeField] private GameObject settingsModal;
    [SerializeField] private Transform settingsCard;
    [SerializeField] private Button soundToggleButton;
    [SerializeField] private Text soundToggleText;
    [SerializeField] private Image soundToggleBg;
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private Text musicToggleText;
    [SerializeField] private Image musicToggleBg;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsRestartButton;
    [SerializeField] private Button backdropCloseButton;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button gearSettingsButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

    private readonly Color colorToggleOn = new Color(0.18f, 0.72f, 0.32f);
    private readonly Color colorToggleOff = new Color(0.55f, 0.58f, 0.62f);
    private bool isSettingsAnimating = false;

    private void Awake()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnTimerUpdated += UpdateTimerUI;
        GameManager.OnLivesUpdated += UpdateLivesUI;
        LevelManager.OnLevelLoaded += HandleLevelLoaded;

        // Top bar buttons
        if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        if (gearSettingsButton != null) gearSettingsButton.onClick.AddListener(OpenSettingsModal);

        // Overlay panel buttons
        if (retryButton != null) retryButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(() => GameManager.Instance.NextLevel());

        // Settings modal buttons
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettingsModal);
        if (resumeButton != null) resumeButton.onClick.AddListener(CloseSettingsModal);
        if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(CloseSettingsModal);
        if (soundToggleButton != null) soundToggleButton.onClick.AddListener(ToggleSound);
        if (musicToggleButton != null) musicToggleButton.onClick.AddListener(ToggleMusic);
        if (settingsRestartButton != null)
        {
            settingsRestartButton.onClick.AddListener(() =>
            {
                CloseSettingsModal();
                GameManager.Instance.RestartLevel();
            });
        }

        if (settingsModal != null) settingsModal.SetActive(false);
        UpdateSettingsButtonsUI();
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnTimerUpdated -= UpdateTimerUI;
        GameManager.OnLivesUpdated -= UpdateLivesUI;
        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    public void OpenSettingsModal()
    {
        if (settingsModal == null || isSettingsAnimating) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTap();
        }

        UpdateSettingsButtonsUI();

        settingsModal.SetActive(true);
        if (settingsCard != null)
        {
            isSettingsAnimating = true;
            settingsCard.localScale = Vector3.one * 0.65f;
            settingsCard.DOScale(Vector3.one, 0.28f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() => isSettingsAnimating = false);
        }
    }

    public void CloseSettingsModal()
    {
        if (settingsModal == null || !settingsModal.activeSelf || isSettingsAnimating) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTap();
        }

        if (settingsCard != null)
        {
            isSettingsAnimating = true;
            settingsCard.DOScale(Vector3.one * 0.65f, 0.18f)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    settingsModal.SetActive(false);
                    isSettingsAnimating = false;
                });
        }
        else
        {
            settingsModal.SetActive(false);
        }
    }

    private void ToggleSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ToggleMute();
            if (!SoundManager.Instance.IsMuted)
            {
                SoundManager.Instance.PlayTap();
            }
        }
        UpdateSettingsButtonsUI();
    }

    private void ToggleMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ToggleMusic();
            if (SoundManager.Instance != null && !SoundManager.Instance.IsMuted)
            {
                SoundManager.Instance.PlayTap();
            }
        }
        UpdateSettingsButtonsUI();
    }

    private void UpdateSettingsButtonsUI()
    {
        bool soundOn = SoundManager.Instance == null || !SoundManager.Instance.IsMuted;
        bool musicOn = SoundManager.Instance == null || !SoundManager.Instance.IsMusicMuted;

        if (soundToggleText != null) soundToggleText.text = soundOn ? "ON" : "OFF";
        if (soundToggleBg != null) soundToggleBg.color = soundOn ? colorToggleOn : colorToggleOff;

        if (musicToggleText != null) musicToggleText.text = musicOn ? "ON" : "OFF";
        if (musicToggleBg != null) musicToggleBg.color = musicOn ? colorToggleOn : colorToggleOff;
    }

    private void HandleLevelLoaded(Level level)
    {
        if (levelNumberText != null) levelNumberText.text = level.LevelNumber.ToString();

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (settingsModal != null) settingsModal.SetActive(false);
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
