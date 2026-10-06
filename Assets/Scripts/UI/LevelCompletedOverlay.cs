using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AntsJam.Progression;

namespace AntsJam.UI
{
    public class LevelCompletedOverlay : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Image titleImage;
        [SerializeField] private Text nextFeatureLabel;
        [SerializeField] private Image progressBarBg;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Text progressPercentText;
        [SerializeField] private Image featureIconGlow;
        [SerializeField] private Image featureIcon;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextButtonText;

        [Header("Configuration")]
        [SerializeField] private FeatureProgressionConfig progressionConfig;
        [SerializeField] private float fillAnimationDuration = 0.90f;
        [SerializeField] private float fillStartDelay = 0.35f;

        private bool isProcessingNext = false;
        private Sequence winSequence;
        private Action onNextClickedCallback;

        public Image ProgressBarFill => progressBarFill;
        public Text ProgressPercentText => progressPercentText;
        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(HandleNextButtonClicked);
            }
            SetBarFill(0f);
        }

        private void OnEnable()
        {
            SetBarFill(0f);
        }

        private void OnDisable()
        {
            winSequence?.Kill();
            winSequence = null;
        }

        private void OnDestroy()
        {
            winSequence?.Kill();
            winSequence = null;
        }

        public void Setup(FeatureProgressionConfig config, Action onNext)
        {
            this.progressionConfig = config;
            this.onNextClickedCallback = onNext;
        }

        /// <summary>
        /// Displays the Level Completed overlay with saved feature progression animation.
        /// Guaranteed to initialize displayed fill & percentage to 0% and animate upward to target.
        /// </summary>
        public void Show(int justCompletedLevel, int prevHighestCompleted)
        {
            // Cancel any previous fill tween before starting another
            winSequence?.Kill();
            winSequence = null;

            isProcessingNext = false;
            if (nextButton != null) nextButton.interactable = true;

            // Before the overlay becomes visible, initialize displayed fill and displayed percentage to zero
            SetBarFill(0f);

            // Make visible only after presentation state is zeroed out (prevents 1-frame flash)
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (progressionConfig == null)
            {
                progressionConfig = FeatureProgressionConfig.Instance;
            }

            // Presentation is determined by justCompletedLevel's position within its milestone range:
            // Levels 1–6: Linked Ants (completedLevel / 6)
            // Levels 7–14: Stacked Ants ((completedLevel - 6) / 8)
            // Levels 15–20: Pipes ((completedLevel - 14) / 6)
            // Levels 21–29: Locked Ants ((completedLevel - 20) / 9)
            // Level 30 onward: hide the feature progress section
            // Do not globally hide the bar merely because the profile previously reached level 30+.
            bool showFeatureProgress = justCompletedLevel < 30;
            float newProgress = 0f;
            FeatureDefinition upcomingFeature = null;

            if (showFeatureProgress)
            {
                string targetFeatureId;
                float rawProgress;

                if (justCompletedLevel <= 6)
                {
                    targetFeatureId = "linked_ants";
                    rawProgress = justCompletedLevel / 6f;
                }
                else if (justCompletedLevel <= 14)
                {
                    targetFeatureId = "stacked_ants";
                    rawProgress = (justCompletedLevel - 6) / 8f;
                }
                else if (justCompletedLevel <= 20)
                {
                    targetFeatureId = "pipes";
                    rawProgress = (justCompletedLevel - 14) / 6f;
                }
                else // 21..29
                {
                    targetFeatureId = "locked_ants";
                    rawProgress = (justCompletedLevel - 20) / 9f;
                }

                newProgress = Mathf.Clamp01(rawProgress);
                if (progressionConfig != null)
                {
                    upcomingFeature = progressionConfig.GetFeature(targetFeatureId);
                }
            }

            // Configure button label (check if campaign complete)
            int maxLevels = LevelManager.Instance != null && LevelManager.Instance.ActiveDatabase != null
                ? LevelManager.Instance.ActiveDatabase.LevelCount
                : 45;
            bool isCampaignFinished = justCompletedLevel >= maxLevels;

            if (nextButtonText != null)
            {
                if (isCampaignFinished)
                {
                    nextButtonText.text = "COMPLETED!";
                    nextButtonText.gameObject.SetActive(true);
                }
                else
                {
                    // Next_Button.png sprite already contains the 3D glossy embossed "NEXT"
                    nextButtonText.gameObject.SetActive(false);
                }
            }

            // Configure element visibility and layout based on level
            Transform progContainer = progressBarBg != null && progressBarBg.transform.parent != null && progressBarBg.transform.parent != contentRoot
                ? progressBarBg.transform.parent
                : (progressBarBg != null ? progressBarBg.transform : null);

            if (showFeatureProgress)
            {
                if (nextFeatureLabel != null)
                {
                    nextFeatureLabel.gameObject.SetActive(true);
                    if (upcomingFeature != null)
                    {
                        nextFeatureLabel.text = $"Next Feature: {upcomingFeature.DisplayName}";
                    }
                }

                if (progContainer != null) progContainer.gameObject.SetActive(true);
                if (progressBarBg != null) progressBarBg.gameObject.SetActive(true);
                if (progressBarFill != null) progressBarFill.gameObject.SetActive(true);
                if (progressPercentText != null) progressPercentText.gameObject.SetActive(true);
                if (featureIconGlow != null) featureIconGlow.gameObject.SetActive(true);

                if (featureIcon != null)
                {
                    if (upcomingFeature != null && upcomingFeature.PreviewIcon != null)
                    {
                        featureIcon.sprite = upcomingFeature.PreviewIcon;
                    }
                    featureIcon.gameObject.SetActive(true);
                }

                if (titleImage != null) titleImage.rectTransform.anchoredPosition = new Vector2(0f, 290f);
                if (nextButton != null) nextButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -220f);
            }
            else
            {
                // Level 30+: Hide progress bar, percent text, next feature label, icon, glow
                if (nextFeatureLabel != null) nextFeatureLabel.gameObject.SetActive(false);
                if (progContainer != null) progContainer.gameObject.SetActive(false);
                if (progressBarBg != null) progressBarBg.gameObject.SetActive(false);
                if (progressBarFill != null) progressBarFill.gameObject.SetActive(false);
                if (progressPercentText != null) progressPercentText.gameObject.SetActive(false);
                if (featureIconGlow != null) featureIconGlow.gameObject.SetActive(false);
                if (featureIcon != null) featureIcon.gameObject.SetActive(false);

                // Reposition remaining elements cleanly to close the gap
                if (titleImage != null) titleImage.rectTransform.anchoredPosition = new Vector2(0f, 120f);
                if (nextButton != null) nextButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -120f);
            }

            // Pop-in entrance animation
            winSequence = DOTween.Sequence();
            winSequence.SetUpdate(true);

            if (backdropDim != null)
            {
                Color targetDim = new Color(0.02f, 0.03f, 0.06f, 0.88f);
                backdropDim.color = new Color(0f, 0f, 0f, 0f);
                winSequence.Join(backdropDim.DOColor(targetDim, 0.22f).SetEase(Ease.Linear));
            }

            if (contentRoot != null)
            {
                contentRoot.localScale = Vector3.one * 0.70f;
                winSequence.Join(contentRoot.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
            }

            if (showFeatureProgress)
            {
                // Feature icon pulse/glow
                if (featureIconGlow != null)
                {
                    featureIconGlow.transform.localScale = Vector3.one * 0.85f;
                    winSequence.Join(featureIconGlow.transform.DOScale(Vector3.one * 1.15f, 0.6f).SetEase(Ease.InOutSine).SetLoops(4, LoopType.Yoyo));
                }

                // Animate progress fill strictly upward from 0% to newProgress (saved progression target)
                float currentAnimatedFill = 0f;
                SetBarFill(0f);

                if (newProgress > 0f)
                {
                    winSequence.Insert(fillStartDelay, DOTween.To(() => currentAnimatedFill, x =>
                    {
                        currentAnimatedFill = x;
                        SetBarFill(currentAnimatedFill);
                    }, newProgress, fillAnimationDuration).SetEase(Ease.OutCubic));
                }
                else
                {
                    SetBarFill(0f);
                }

                // If milestone reached (100%), celebrate with punch scale
                if (newProgress >= 0.999f)
                {
                    winSequence.InsertCallback(fillStartDelay + (newProgress > 0f ? fillAnimationDuration : 0f), () =>
                    {
                        if (featureIcon != null)
                        {
                            featureIcon.transform.DOPunchScale(Vector3.one * 0.25f, 0.30f, 10, 0.6f).SetUpdate(true);
                        }
                        if (progressPercentText != null)
                        {
                            progressPercentText.transform.DOPunchScale(Vector3.one * 0.20f, 0.25f, 8, 0.5f).SetUpdate(true);
                        }
                    });
                }
            }

            winSequence.SetUpdate(true);
        }

        public void SetBarFill(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);

            if (progressBarFill != null)
            {
                if (progressBarFill.type == Image.Type.Filled)
                {
                    progressBarFill.fillAmount = ratio;
                }
                else
                {
                    // Scale X or anchor size
                    var rt = progressBarFill.rectTransform;
                    rt.anchorMax = new Vector2(ratio, rt.anchorMax.y);
                }
            }

            if (progressPercentText != null)
            {
                progressPercentText.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
            }
        }

        private void HandleNextButtonClicked()
        {
            if (isProcessingNext) return;
            isProcessingNext = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTap();
            }

            if (nextButton != null)
            {
                nextButton.interactable = false;
            }

            // Button punch feedback
            if (nextButton != null)
            {
                nextButton.transform.DOKill();
                nextButton.transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f, 1, 0f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        DismissAndProceed();
                    });
            }
            else
            {
                DismissAndProceed();
            }
        }

        private void DismissAndProceed()
        {
            winSequence?.Kill();
            winSequence = null;
            SetBarFill(0f);
            gameObject.SetActive(false);
            if (nextButton != null) nextButton.interactable = true;

            onNextClickedCallback?.Invoke();
        }
    }
}
