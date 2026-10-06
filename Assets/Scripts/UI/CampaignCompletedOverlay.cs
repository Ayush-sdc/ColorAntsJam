using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace AntsJam.UI
{
    /// <summary>
    /// Dedicated campaign completion screen displayed when the player beats the final campaign level.
    /// Presents celebratory visuals with cute ants, celebration message, and a PLAY AGAIN button.
    /// Does not show NEXT or feature progression bars.
    /// </summary>
    public class CampaignCompletedOverlay : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Image radialRays;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Text messageText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Text playAgainButtonText;

        [Header("Animation Settings")]
        [SerializeField] private float raysRotationSpeed = 15f;

        private Action onPlayAgainCallback;
        private bool isProcessingPlayAgain = false;
        private Sequence entranceSequence;

        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(HandlePlayAgainClicked);
            }
        }

        private void Update()
        {
            if (gameObject.activeSelf && radialRays != null)
            {
                radialRays.transform.Rotate(0f, 0f, raysRotationSpeed * Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            entranceSequence?.Kill();
        }

        public void Setup(Action onPlayAgain)
        {
            this.onPlayAgainCallback = onPlayAgain;
        }

        /// <summary>
        /// Displays the Campaign Completed screen.
        /// </summary>
        public void Show(Action onPlayAgain)
        {
            entranceSequence?.Kill();
            entranceSequence = null;

            isProcessingPlayAgain = false;
            this.onPlayAgainCallback = onPlayAgain;

            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(HandlePlayAgainClicked);
                playAgainButton.onClick.AddListener(HandlePlayAgainClicked);
                playAgainButton.interactable = true;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            Color targetDim = new Color(0.02f, 0.03f, 0.06f, 0.95f);
            if (Application.isPlaying)
            {
                entranceSequence = DOTween.Sequence();
                entranceSequence.SetUpdate(true);

                if (backdropDim != null)
                {
                    backdropDim.color = new Color(0f, 0f, 0f, 0f);
                    entranceSequence.Join(backdropDim.DOColor(targetDim, 0.25f).SetEase(Ease.Linear));
                }

                if (contentRoot != null)
                {
                    contentRoot.localScale = Vector3.one * 0.70f;
                    entranceSequence.Join(contentRoot.DOScale(Vector3.one, 0.40f).SetEase(Ease.OutBack));
                }

                if (illustrationImage != null)
                {
                    illustrationImage.transform.localScale = Vector3.one * 0.85f;
                    entranceSequence.Join(illustrationImage.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack));
                }
            }
            else
            {
                if (backdropDim != null) backdropDim.color = targetDim;
                if (contentRoot != null) contentRoot.localScale = Vector3.one;
                if (illustrationImage != null) illustrationImage.transform.localScale = Vector3.one;
            }
        }

        private void HandlePlayAgainClicked()
        {
            if (isProcessingPlayAgain) return;
            isProcessingPlayAgain = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTap();
            }

            if (playAgainButton != null && Application.isPlaying)
            {
                playAgainButton.interactable = false;
                playAgainButton.transform.DOKill();
                playAgainButton.transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f, 1, 0f)
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
            entranceSequence?.Kill();
            entranceSequence = null;
            gameObject.SetActive(false);
            if (playAgainButton != null) playAgainButton.interactable = true;

            onPlayAgainCallback?.Invoke();
        }
    }
}
