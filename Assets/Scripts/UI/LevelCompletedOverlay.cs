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

        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(HandleNextButtonClicked);
            }
        }

        private void OnDestroy()
        {
            winSequence?.Kill();
        }

        public void Setup(FeatureProgressionConfig config, Action onNext)
        {
            this.progressionConfig = config;
            this.onNextClickedCallback = onNext;
        }

        /// <summary>
        /// Displays the Level Completed overlay with saved feature progression animation.
        /// </summary>
        public void Show(int justCompletedLevel, int prevHighestCompleted)
        {
            isProcessingNext = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (progressionConfig == null)
            {
                progressionConfig = FeatureProgressionConfig.Instance;
            }

            // Calculate progression before and after this level completion
            float prevProgress = 0f;
            float newProgress = 0f;
            FeatureDefinition upcomingFeature = null;
            bool isAllUnlocked = false;

            if (progressionConfig != null)
            {
                prevProgress = progressionConfig.CalculateProgress(prevHighestCompleted, out _, out _);
                newProgress = progressionConfig.CalculateProgress(justCompletedLevel, out upcomingFeature, out isAllUnlocked);
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

            // Configure Feature preview icon
            if (isAllUnlocked || upcomingFeature == null)
            {
                if (nextFeatureLabel != null) nextFeatureLabel.text = "Features Mastered!";
                if (featureIcon != null)
                {
                    featureIcon.sprite = progressionConfig != null ? progressionConfig.AllFeaturesCompletedIcon : null;
                    featureIcon.gameObject.SetActive(featureIcon.sprite != null);
                }
            }
            else
            {
                if (nextFeatureLabel != null) nextFeatureLabel.text = $"Next Feature: {upcomingFeature.DisplayName}";
                if (featureIcon != null)
                {
                    featureIcon.sprite = upcomingFeature.PreviewIcon;
                    featureIcon.gameObject.SetActive(featureIcon.sprite != null);
                }
            }

            // Set initial fill to prevProgress
            SetBarFill(prevProgress);

            // Pop-in entrance animation
            winSequence?.Kill();
            winSequence = DOTween.Sequence();

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

            // Feature icon pulse/glow
            if (featureIconGlow != null)
            {
                featureIconGlow.transform.localScale = Vector3.one * 0.85f;
                winSequence.Join(featureIconGlow.transform.DOScale(Vector3.one * 1.15f, 0.6f).SetEase(Ease.InOutSine).SetLoops(4, LoopType.Yoyo));
            }

            // Animate progress fill from prevProgress to newProgress
            float currentAnimatedFill = prevProgress;
            winSequence.Insert(fillStartDelay, DOTween.To(() => currentAnimatedFill, x =>
            {
                currentAnimatedFill = x;
                SetBarFill(currentAnimatedFill);
            }, newProgress, fillAnimationDuration).SetEase(Ease.OutCubic));

            // If milestone reached (100%), celebrate with punch scale
            if (newProgress >= 0.999f && prevProgress < 0.999f)
            {
                winSequence.InsertCallback(fillStartDelay + fillAnimationDuration, () =>
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

            winSequence.SetUpdate(true);
        }

        private void SetBarFill(float ratio)
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
            gameObject.SetActive(false);
            if (nextButton != null) nextButton.interactable = true;

            onNextClickedCallback?.Invoke();
        }
    }
}
