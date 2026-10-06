using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AntsJam.Legal;

namespace AntsJam.UI
{
    public class WelcomeOverlay : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LegalConfig legalConfig;

        [Header("Visual Components")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private Text headingText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button termsLinkButton;
        [SerializeField] private Text termsLinkText;
        [SerializeField] private Button privacyLinkButton;
        [SerializeField] private Text privacyLinkText;
        [SerializeField] private Button acceptButton;

        [Header("Document Viewer")]
        [SerializeField] private LegalDocumentModal documentModal;

        private Action onAcceptedCallback;
        private Sequence entranceSequence;
        private bool isProcessingAccept = false;
        private bool listenersInitialized = false;

        public bool IsShowing => gameObject.activeSelf;
        public LegalConfig Config => legalConfig != null ? legalConfig : LegalConfig.Instance;

        private void Awake()
        {
            InitializeListeners();
        }

        public void InitializeListeners()
        {
            if (listenersInitialized) return;
            listenersInitialized = true;

            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(HandleAcceptClicked);
            }

            if (termsLinkButton != null)
            {
                termsLinkButton.onClick.AddListener(HandleTermsLinkClicked);
            }

            if (privacyLinkButton != null)
            {
                privacyLinkButton.onClick.AddListener(HandlePrivacyLinkClicked);
            }
        }

        private void OnDestroy()
        {
            entranceSequence?.Kill();
        }

        public void Setup(LegalConfig config, LegalDocumentModal docModal)
        {
            this.legalConfig = config;
            this.documentModal = docModal;
            InitializeListeners();
        }

        public void Show(Action onAccepted)
        {
            this.onAcceptedCallback = onAccepted;
            isProcessingAccept = false;

            InitializeListeners();
            RefreshCopy();

            entranceSequence?.Kill();
            entranceSequence = null;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (acceptButton != null)
            {
                acceptButton.interactable = true;
            }

            if (Application.isPlaying)
            {
                entranceSequence = DOTween.Sequence();
                entranceSequence.SetUpdate(true);

                if (backdropDim != null)
                {
                    Color targetDim = new Color(0.06f, 0.04f, 0.12f, 0.88f);
                    backdropDim.color = new Color(0f, 0f, 0f, 0f);
                    entranceSequence.Join(backdropDim.DOColor(targetDim, 0.25f).SetEase(Ease.Linear));
                }

                if (cardRoot != null)
                {
                    cardRoot.localScale = Vector3.one * 0.70f;
                    entranceSequence.Join(cardRoot.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
                }
            }
            else
            {
                if (backdropDim != null) backdropDim.color = new Color(0.06f, 0.04f, 0.12f, 0.88f);
                if (cardRoot != null) cardRoot.localScale = Vector3.one;
            }
        }

        public void RefreshCopy()
        {
            var cfg = Config;
            if (cfg == null) return;

            if (headingText != null)
            {
                headingText.text = "Please Accept the Terms";
            }

            if (bodyText != null)
            {
                string text = cfg.WelcomeMessage;
                if (cfg.IsTermsRecentlyUpdated && !string.IsNullOrEmpty(cfg.WelcomeUpdateNotice))
                {
                    text = $"{cfg.WelcomeUpdateNotice}\n\n{text}";
                }
                bodyText.text = text;
            }

            if (termsLinkText != null)
            {
                termsLinkText.text = "<u>Terms of Service</u>";
            }

            if (privacyLinkText != null)
            {
                privacyLinkText.text = "<u>Privacy Policy</u>";
            }
        }

        private void HandleAcceptClicked()
        {
            if (isProcessingAccept) return;
            isProcessingAccept = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTap();
            }

            var cfg = Config;
            if (cfg == null || (string.IsNullOrEmpty(cfg.TermsOfServiceContent) && string.IsNullOrEmpty(cfg.TermsOfServiceUrl)))
            {
                Debug.LogError("[WelcomeOverlay] Cannot record acceptance: Legal terms configuration is missing!");
                isProcessingAccept = false;
                return;
            }

            // Save acceptance record strictly on ACCEPT tap
            LegalManager.RecordTermsAcceptance(cfg);

            if (acceptButton != null && Application.isPlaying)
            {
                acceptButton.interactable = false;
                acceptButton.transform.DOKill();
                acceptButton.transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f, 1, 0f)
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
            if (Application.isPlaying && cardRoot != null)
            {
                cardRoot.DOScale(Vector3.one * 0.70f, 0.18f)
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
            entranceSequence?.Kill();
            entranceSequence = null;
            gameObject.SetActive(false);
            isProcessingAccept = false;

            onAcceptedCallback?.Invoke();
            onAcceptedCallback = null;
        }

        private void HandleTermsLinkClicked()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayTap();

            var cfg = Config;
            string title = cfg != null ? cfg.TermsOfServiceTitle : "Terms of Service";
            string content = cfg != null ? cfg.TermsOfServiceContent : string.Empty;
            string url = cfg != null ? cfg.TermsOfServiceUrl : string.Empty;

            OpenDocument(title, content, url);
        }

        private void HandlePrivacyLinkClicked()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayTap();

            var cfg = Config;
            string title = cfg != null ? cfg.PrivacyPolicyTitle : "Privacy Policy";
            string content = cfg != null ? cfg.PrivacyPolicyContent : string.Empty;
            string url = cfg != null ? cfg.PrivacyPolicyUrl : string.Empty;

            OpenDocument(title, content, url);
        }

        private void OpenDocument(string title, string content, string url)
        {
            if (documentModal != null)
            {
                documentModal.Show(title, content, url, () =>
                {
                    // Returning to WelcomeOverlay: preserve Welcome screen!
                    gameObject.SetActive(true);
                });
            }
            else if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }
    }
}
