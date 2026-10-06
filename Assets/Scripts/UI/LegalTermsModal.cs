using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AntsJam.Legal;

namespace AntsJam.UI
{
    public class LegalTermsModal : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LegalConfig legalConfig;

        [Header("Modal Framework")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private Button closeButton;

        [Header("Buttons")]
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button termsOfServiceButton;
        [SerializeField] private Button accountDeletionButton;
        [SerializeField] private Button personalizedAdsButton;
        [SerializeField] private Image personalizedAdsButtonImage;
        [SerializeField] private Text personalizedAdsButtonText;

        [Header("Player ID Row")]
        [SerializeField] private GameObject playerIdRow;
        [SerializeField] private Text playerIdLabel;
        [SerializeField] private Text playerIdValueText;
        [SerializeField] private Button copyPlayerIdButton;
        [SerializeField] private GameObject toastContainer;
        [SerializeField] private Text toastText;

        [Header("Document Viewer")]
        [SerializeField] private LegalDocumentModal documentModal;

        private Action onCloseCallback;
        private Sequence modalSequence;
        private Coroutine toastCoroutine;
        private bool isAnimating = false;

        public bool IsShowing => gameObject.activeSelf;
        public LegalConfig Config => legalConfig != null ? legalConfig : LegalConfig.Instance;

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

            if (privacyPolicyButton != null)
            {
                privacyPolicyButton.onClick.AddListener(HandlePrivacyPolicyClicked);
            }

            if (termsOfServiceButton != null)
            {
                termsOfServiceButton.onClick.AddListener(HandleTermsOfServiceClicked);
            }

            if (accountDeletionButton != null)
            {
                accountDeletionButton.onClick.AddListener(HandleAccountDeletionClicked);
            }

            if (personalizedAdsButton != null)
            {
                personalizedAdsButton.onClick.AddListener(HandlePersonalizedAdsClicked);
            }

            if (copyPlayerIdButton != null)
            {
                copyPlayerIdButton.onClick.AddListener(HandleCopyPlayerIdClicked);
            }
        }

        private void Update()
        {
            // Android Back closes only topmost modal
            if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                if (documentModal != null && documentModal.IsShowing)
                {
                    // Document modal handles its own escape
                    return;
                }
                CloseModal();
            }
        }

        private void OnDestroy()
        {
            modalSequence?.Kill();
        }

        public void Setup(LegalConfig config, LegalDocumentModal docModal)
        {
            this.legalConfig = config;
            this.documentModal = docModal;
            InitializeListeners();
        }

        public void Show(Action onClose)
        {
            this.onCloseCallback = onClose;
            isAnimating = false;

            InitializeListeners();
            RefreshUIState();

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

        public void RefreshUIState()
        {
            var cfg = Config;

            // 1. Personalized Ads Button availability
            if (personalizedAdsButton != null)
            {
                bool adsAvailable = cfg != null && cfg.IsPersonalizedAdsAvailable;
                personalizedAdsButton.interactable = adsAvailable;
                if (personalizedAdsButtonImage != null)
                {
                    personalizedAdsButtonImage.color = adsAvailable ? Color.white : new Color(0.70f, 0.75f, 0.85f, 0.65f);
                }
                if (personalizedAdsButtonText != null)
                {
                    personalizedAdsButtonText.color = adsAvailable ? Color.white : new Color(0.85f, 0.85f, 0.90f, 0.65f);
                }
            }

            // 2. Player ID row: ONLY shown if real ID is available from backend/service!
            if (playerIdRow != null)
            {
                bool showId = cfg != null && cfg.IsPlayerIdAvailable;
                playerIdRow.SetActive(showId);

                if (showId && playerIdValueText != null)
                {
                    playerIdValueText.text = cfg.ConfiguredPlayerId;
                }
            }

            if (toastContainer != null)
            {
                toastContainer.SetActive(false);
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

        private void HandlePrivacyPolicyClicked()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayTap();

            var cfg = Config;
            string title = cfg != null ? cfg.PrivacyPolicyTitle : "Privacy Policy";
            string content = cfg != null ? cfg.PrivacyPolicyContent : string.Empty;
            string url = cfg != null ? cfg.PrivacyPolicyUrl : string.Empty;

            OpenDocument(title, content, url);
        }

        private void HandleTermsOfServiceClicked()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayTap();

            var cfg = Config;
            string title = cfg != null ? cfg.TermsOfServiceTitle : "Terms of Service";
            string content = cfg != null ? cfg.TermsOfServiceContent : string.Empty;
            string url = cfg != null ? cfg.TermsOfServiceUrl : string.Empty;

            OpenDocument(title, content, url);
        }

        private void HandleAccountDeletionClicked()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayTap();

            var cfg = Config;
            string title = cfg != null ? cfg.AccountDeletionTitle : "Account Deletion and Data Request";
            string content = cfg != null ? cfg.AccountDeletionContent : string.Empty;
            string url = cfg != null ? cfg.AccountDeletionUrl : string.Empty;

            OpenDocument(title, content, url);
        }

        private void HandlePersonalizedAdsClicked()
        {
            var cfg = Config;
            if (cfg != null && cfg.IsPersonalizedAdsAvailable)
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayTap();
                // Trigger real consent management flow here when installed
                ShowToast("Advertising preferences updated.");
            }
            else
            {
                string reason = cfg != null ? cfg.PersonalizedAdsUnavailableReason : "Personalized advertising is disabled.";
                ShowToast(reason);
            }
        }

        private void HandleCopyPlayerIdClicked()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayTap();

            var cfg = Config;
            if (cfg != null && cfg.IsPlayerIdAvailable)
            {
                GUIUtility.systemCopyBuffer = cfg.ConfiguredPlayerId;
                ShowToast("Player ID copied to clipboard!");
            }
        }

        private void OpenDocument(string title, string content, string url)
        {
            if (documentModal != null)
            {
                documentModal.Show(title, content, url, () =>
                {
                    // Returning to LegalTermsModal
                    gameObject.SetActive(true);
                });
            }
            else if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        public void ShowToast(string message)
        {
            if (toastContainer == null || toastText == null) return;

            toastText.text = message;
            toastContainer.SetActive(true);

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(ToastTimerRoutine());
        }

        private IEnumerator ToastTimerRoutine()
        {
            yield return new WaitForSecondsRealtime(2.2f);
            if (toastContainer != null) toastContainer.SetActive(false);
            toastCoroutine = null;
        }
    }
}
