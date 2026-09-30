using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameUIManager : MonoBehaviour
{
    [Header("Top Bar UI Elements")]
    [SerializeField] private Text timerText;
    [SerializeField] private Text levelNumberText;
    [SerializeField] private Image[] heartImages;

    [Header("Tutorial Elements")]
    [SerializeField] private GameObject tutorialContainer;
    [SerializeField] private Text tutorialText;
    [SerializeField] private GameObject tutorialHand;

    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Text loseReasonText;

    [Header("Level Failed UI")]
    [SerializeField] private Image failDimBackground;
    [SerializeField] private Transform failPopupRoot;
    [SerializeField] private Transform failTitleTransform;
    [SerializeField] private Transform failHeartTransform;
    [SerializeField] private Transform failRestartButtonTransform;

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
    private Sequence failSequence;

    private void Awake()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        GameManager.OnTimerUpdated += UpdateTimerUI;
        GameManager.OnLivesUpdated += UpdateLivesUI;
        LevelManager.OnLevelLoaded += HandleLevelLoaded;
        AntQueueController.OnQueueMoveStarted += HandleQueueMoveStarted;

        EnsureFailUIReferences();

        // Top bar buttons
        if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());
        if (gearSettingsButton != null) gearSettingsButton.onClick.AddListener(OpenSettingsModal);

        // Overlay panel buttons
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayTap();
                }
                if (failRestartButtonTransform != null)
                {
                    failRestartButtonTransform.DOKill();
                    failRestartButtonTransform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f, 1, 0f)
                        .SetUpdate(true)
                        .OnComplete(() => GameManager.Instance.RestartLevel());
                }
                else
                {
                    GameManager.Instance.RestartLevel();
                }
            });
        }
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
        if (losePanel != null) losePanel.SetActive(false);
        UpdateSettingsButtonsUI();
    }

    public void EnsureFailUIReferences()
    {
        if (losePanel != null)
        {
            if (failDimBackground == null)
            {
                failDimBackground = losePanel.GetComponent<Image>();
            }
            if (failPopupRoot == null)
            {
                var root = losePanel.transform.Find("FailPopupRoot");
                failPopupRoot = root != null ? root : losePanel.transform;
            }
            if (failPopupRoot != null)
            {
                if (failTitleTransform == null)
                    failTitleTransform = failPopupRoot.Find("FailedTitleImage");
                if (failHeartTransform == null)
                    failHeartTransform = failPopupRoot.Find("BrokenHeartImage");
                if (failRestartButtonTransform == null)
                {
                    var btnTrans = failPopupRoot.Find("RestartButton") ?? failPopupRoot.Find("ActionButton");
                    failRestartButtonTransform = btnTrans;
                }
            }
            if (retryButton == null && failRestartButtonTransform != null)
            {
                retryButton = failRestartButtonTransform.GetComponent<Button>();
            }
        }
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        GameManager.OnTimerUpdated -= UpdateTimerUI;
        GameManager.OnLivesUpdated -= UpdateLivesUI;
        LevelManager.OnLevelLoaded -= HandleLevelLoaded;
        AntQueueController.OnQueueMoveStarted -= HandleQueueMoveStarted;
    }

    private void HandleQueueMoveStarted(AntQueueController queue)
    {
        if (tutorialContainer != null && tutorialContainer.activeSelf)
        {
            tutorialContainer.SetActive(false);
        }
    }

    public void OpenSettingsModal()
    {
        if (settingsModal == null || isSettingsAnimating) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTap();
        }

        UpdateSettingsButtonsUI();
        GameManager.Instance?.PauseGame();

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

        GameManager.Instance?.ResumeGame();

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

    [Header("Hard Level Overlay")]
    [SerializeField] private GameObject hardLevelOverlay;
    [SerializeField] private Transform hardLevelCard;
    [SerializeField] private Button hardLevelStartButton;
    private bool isHardOverlayShowing = false;

    private void HandleLevelLoaded(Level level)
    {
        if (levelNumberText != null) levelNumberText.text = level != null ? level.LevelNumber.ToString() : "1";

        // Data-driven tutorial presentation
        UpdateTutorialForLevel(level);

        failSequence?.Kill();
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (settingsModal != null) settingsModal.SetActive(false);

        // Hard level overlay check (LevelDifficulty >= 2 or ShowHardIntro)
        bool isHard = level != null && (level.LevelDifficulty >= 2 || level.ShowHardIntro);
        if (isHard)
        {
            ShowHardLevelOverlay();
        }
        else if (hardLevelOverlay != null)
        {
            hardLevelOverlay.SetActive(false);
            isHardOverlayShowing = false;
        }
    }

    private void UpdateTutorialForLevel(Level level)
    {
        if (tutorialContainer == null || level == null) return;

        string tutorialMsg = null;
        int flags = level.TutorialFlags;

        if ((flags & 1) != 0)
        {
            tutorialMsg = "Linked Ants: These ant queues are roped together and move at the same time!";
        }
        else if ((flags & 2) != 0)
        {
            tutorialMsg = "Stacked Ants: Guide the top ant to free the lower ant underneath!";
        }
        else if ((flags & 4) != 0)
        {
            tutorialMsg = "Pipes: Enter the pipe entrance to guide ants to their matching hole!";
        }
        else if ((flags & 8) != 0)
        {
            tutorialMsg = "Locked Ants: Rescue other ant queues to decrement lock counters and free locked ants!";
        }
        else if (level.LevelNumber == 1)
        {
            tutorialMsg = "Tap an ant to guide it to its matching hole!";
        }

        if (!string.IsNullOrEmpty(tutorialMsg))
        {
            if (tutorialText != null) tutorialText.text = tutorialMsg;
            tutorialContainer.SetActive(true);
        }
        else
        {
            tutorialContainer.SetActive(false);
        }
    }

    public void ShowHardLevelOverlay()
    {
        EnsureHardLevelOverlay();
        if (hardLevelOverlay == null) return;

        isHardOverlayShowing = true;
        hardLevelOverlay.SetActive(true);
        GameManager.Instance?.PauseGame();

        if (hardLevelCard != null)
        {
            hardLevelCard.DOKill();
            hardLevelCard.localScale = Vector3.one * 0.65f;
            hardLevelCard.DOScale(Vector3.one, 0.32f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void DismissHardLevelOverlay()
    {
        if (hardLevelOverlay == null || !isHardOverlayShowing) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTap();
        }

        if (hardLevelCard != null)
        {
            hardLevelCard.DOKill();
            hardLevelCard.DOScale(Vector3.one * 0.65f, 0.20f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                hardLevelOverlay.SetActive(false);
                isHardOverlayShowing = false;
                GameManager.Instance?.ResumeGame();
            });
        }
        else
        {
            hardLevelOverlay.SetActive(false);
            isHardOverlayShowing = false;
            GameManager.Instance?.ResumeGame();
        }
    }

    private void EnsureHardLevelOverlay()
    {
        if (hardLevelOverlay != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        if (parentCanvas == null) return;

        GameObject overlayObj = new GameObject("HardLevelOverlay");
        overlayObj.transform.SetParent(parentCanvas.transform, false);
        var overlayRect = overlayObj.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        var bgImg = overlayObj.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.05f, 0.88f);

        GameObject cardObj = new GameObject("HardCard");
        cardObj.transform.SetParent(overlayObj.transform, false);
        var cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(380, 260);
        hardLevelCard = cardObj.transform;

        var cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.18f, 0.08f, 0.12f, 0.95f);

        // Title
        GameObject titleObj = new GameObject("HardTitle");
        titleObj.transform.SetParent(cardObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 60);
        titleRect.sizeDelta = new Vector2(360, 60);
        var titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 38;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.28f, 0.28f);
        titleText.text = "HARD LEVEL!";

        // Subtitle
        GameObject subObj = new GameObject("HardSub");
        subObj.transform.SetParent(cardObj.transform, false);
        var subRect = subObj.AddComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 10);
        subRect.sizeDelta = new Vector2(340, 40);
        var subText = subObj.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = 18;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.9f, 0.9f, 0.95f);
        subText.text = "Guide all ant queues to their holes!";

        // Start Button
        GameObject btnObj = new GameObject("StartButton");
        btnObj.transform.SetParent(cardObj.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -65);
        btnRect.sizeDelta = new Vector2(220, 56);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.20f, 0.72f, 0.35f);

        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        var btnText = btnTextObj.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 22;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = "START";

        hardLevelStartButton = btnObj.AddComponent<Button>();
        hardLevelStartButton.onClick.AddListener(DismissHardLevelOverlay);

        hardLevelOverlay = overlayObj;
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
                    heartImages[i].gameObject.SetActive(isAlive);
                    heartImages[i].enabled = isAlive;
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
            bool isLost = state == GameState.LevelLost;
            losePanel.SetActive(isLost);
            if (isLost)
            {
                AnimateFailScreenEntrance();
            }
            else
            {
                failSequence?.Kill();
            }
        }
    }

    private void AnimateFailScreenEntrance()
    {
        EnsureFailUIReferences();
        failSequence?.Kill();
        failSequence = DOTween.Sequence();

        // 1. Dark dim overlay fades in quickly
        if (failDimBackground != null)
        {
            Color targetColor = new Color(0f, 0f, 0f, 0.82f);
            failDimBackground.color = new Color(0f, 0f, 0f, 0f);
            failSequence.Join(failDimBackground.DOColor(targetColor, 0.20f).SetEase(Ease.Linear));
        }

        // 2. Title art scales in
        if (failTitleTransform != null)
        {
            failTitleTransform.localScale = Vector3.one * 0.72f;
            failSequence.Join(failTitleTransform.DOScale(Vector3.one, 0.32f).SetEase(Ease.OutBack));
        }

        // 3. Broken heart scales in
        if (failHeartTransform != null)
        {
            failHeartTransform.localScale = Vector3.one * 0.68f;
            failSequence.Join(failHeartTransform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
        }

        // 4. Glossy Restart button pops in gently with short delay
        if (failRestartButtonTransform != null)
        {
            failRestartButtonTransform.localScale = Vector3.zero;
            failSequence.Insert(0.10f, failRestartButtonTransform.DOScale(Vector3.one, 0.32f).SetEase(Ease.OutBack));
        }

        failSequence.SetUpdate(true);
    }
}
