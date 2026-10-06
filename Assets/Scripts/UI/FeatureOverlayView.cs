using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace AntsJam.UI
{
    /// <summary>
    /// Component representing a complete flattened feature tutorial composition (approx 1201 x 1309 px)
    /// with an invisible hit-area Button mapped precisely over the illustrated OK button.
    /// </summary>
    public class FeatureOverlayView : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Image tutorialImage;
        [SerializeField] private Button okButton;

        [Header("Feature Identity")]
        [SerializeField] private string featureId = "linked_ants";

        private Action onDismissCallback;
        private Sequence entranceSequence;
        private bool isProcessingDismiss = false;

        public Image BackdropDim => backdropDim;
        public RectTransform ContentRoot => contentRoot;
        public Image TutorialImage => tutorialImage;
        public Button OkButton => okButton;
        public string FeatureId => featureId;
        public bool IsShowing => gameObject.activeSelf;

        private void OnDestroy()
        {
            entranceSequence?.Kill();
        }

        public void Initialize(string id, Sprite artwork, Action onDismiss)
        {
            this.featureId = id;
            this.onDismissCallback = onDismiss;
            isProcessingDismiss = false;

            if (tutorialImage != null)
            {
                if (artwork != null)
                {
                    tutorialImage.sprite = artwork;
                }
                tutorialImage.preserveAspect = true;
                tutorialImage.raycastTarget = false;
                tutorialImage.color = Color.white;
                tutorialImage.gameObject.SetActive(true);
            }

            if (okButton != null)
            {
                okButton.onClick.RemoveAllListeners();
                okButton.onClick.AddListener(HandleOkClicked);
                okButton.interactable = true;
                okButton.gameObject.SetActive(true);
            }
        }

        public void Show(string id, Sprite artwork, Action onDismiss)
        {
            Initialize(id, artwork, onDismiss);

            entranceSequence?.Kill();
            entranceSequence = null;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (Application.isPlaying)
            {
                entranceSequence = DOTween.Sequence();
                entranceSequence.SetUpdate(true);

                if (backdropDim != null)
                {
                    Color targetDim = new Color(0f, 0f, 0f, 0.78f);
                    backdropDim.color = new Color(0f, 0f, 0f, 0f);
                    entranceSequence.Join(backdropDim.DOColor(targetDim, 0.22f).SetEase(Ease.Linear));
                }

                if (contentRoot != null)
                {
                    contentRoot.localScale = Vector3.one * 0.70f;
                    entranceSequence.Join(contentRoot.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
                }
            }
            else
            {
                if (backdropDim != null) backdropDim.color = new Color(0f, 0f, 0f, 0.78f);
                if (contentRoot != null) contentRoot.localScale = Vector3.one;
            }
        }

        public void HandleOkClicked()
        {
            if (isProcessingDismiss) return;
            isProcessingDismiss = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTap();
            }

            if (okButton != null && Application.isPlaying)
            {
                okButton.interactable = false;
                okButton.transform.DOKill();
                okButton.transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f, 1, 0f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        AnimateDismiss();
                    });
            }
            else
            {
                AnimateDismiss();
            }
        }

        private void AnimateDismiss()
        {
            if (Application.isPlaying && contentRoot != null)
            {
                contentRoot.DOScale(Vector3.one * 0.70f, 0.16f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        DismissImmediately();
                    });
            }
            else
            {
                DismissImmediately();
            }
        }

        public void DismissImmediately()
        {
            entranceSequence?.Kill();
            entranceSequence = null;
            gameObject.SetActive(false);
            isProcessingDismiss = false;

            onDismissCallback?.Invoke();
            onDismissCallback = null;
        }
    }
}
