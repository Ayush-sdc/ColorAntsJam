using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace AntsJam.UI
{
    public class LegalDocumentModal : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text documentBodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openUrlButton;
        [SerializeField] private Text openUrlButtonText;

        private Action onCloseCallback;
        private string currentUrl;
        private Sequence modalSequence;
        private bool isAnimating = false;

        public bool IsShowing => gameObject.activeSelf;

        private bool listenersInitialized = false;

        private void Awake()
        {
            InitializeListeners();
        }

        public void InitializeListeners()
        {
            if (listenersInitialized) return;
            listenersInitialized = true;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseModal);
            }
            if (openUrlButton != null)
            {
                openUrlButton.onClick.AddListener(HandleOpenUrlClicked);
            }
        }

        private void Update()
        {
            if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseModal();
            }
        }

        private void OnDestroy()
        {
            modalSequence?.Kill();
        }

        public void Show(string title, string content, string url, Action onClose)
        {
            this.onCloseCallback = onClose;
            this.currentUrl = url;
            isAnimating = false;

            InitializeListeners();

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (documentBodyText != null)
            {
                documentBodyText.text = content;
            }

            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = new Vector2(0f, 1f); // Reset scroll to top
            }

            if (openUrlButton != null)
            {
                bool hasUrl = !string.IsNullOrEmpty(url);
                openUrlButton.gameObject.SetActive(hasUrl);
                if (hasUrl && openUrlButtonText != null)
                {
                    openUrlButtonText.text = "Open in Browser";
                }
            }

            modalSequence?.Kill();
            modalSequence = null;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (Application.isPlaying)
            {
                modalSequence = DOTween.Sequence();
                modalSequence.SetUpdate(true);

                if (backdropDim != null)
                {
                    Color targetDim = new Color(0f, 0f, 0f, 0.70f);
                    backdropDim.color = new Color(0f, 0f, 0f, 0f);
                    modalSequence.Join(backdropDim.DOColor(targetDim, 0.20f).SetEase(Ease.Linear));
                }

                if (cardRoot != null)
                {
                    cardRoot.localScale = Vector3.one * 0.70f;
                    modalSequence.Join(cardRoot.DOScale(Vector3.one, 0.30f).SetEase(Ease.OutBack));
                }
            }
            else
            {
                if (backdropDim != null) backdropDim.color = new Color(0f, 0f, 0f, 0.70f);
                if (cardRoot != null) cardRoot.localScale = Vector3.one;
            }
        }

        public void CloseModal()
        {
            if (isAnimating) return;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTap();
            }

            if (Application.isPlaying && cardRoot != null)
            {
                isAnimating = true;
                cardRoot.DOScale(Vector3.one * 0.70f, 0.16f)
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

        private void DismissImmediately()
        {
            modalSequence?.Kill();
            modalSequence = null;
            gameObject.SetActive(false);
            isAnimating = false;

            onCloseCallback?.Invoke();
            onCloseCallback = null;
        }

        private void HandleOpenUrlClicked()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTap();
            }

            if (!string.IsNullOrEmpty(currentUrl))
            {
                Application.OpenURL(currentUrl);
            }
        }
    }
}
