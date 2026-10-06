using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using AntsJam.Legal;

namespace AntsJam.UI
{
    public class WelcomeSceneController : MonoBehaviour
    {
        public static bool ForcePreviewMode = false;

        [Header("Configuration")]
        [SerializeField] private LegalConfig legalConfig;
        [SerializeField] private string gameplaySceneName = "main";

        [Header("UI Visual Components")]
        [SerializeField] private Image backgroundFill;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private Text headingText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button termsLinkButton;
        [SerializeField] private Text termsLinkText;
        [SerializeField] private Button privacyLinkButton;
        [SerializeField] private Text privacyLinkText;
        [SerializeField] private Button acceptButton;

        [Header("Document Viewer Modal")]
        [SerializeField] private LegalDocumentModal documentModal;

        private bool isProcessingAccept = false;
        private bool isLoadingScene = false;
        private Sequence entranceSequence;

        public LegalConfig Config => legalConfig != null ? legalConfig : LegalConfig.Instance;

        private void Awake()
        {
            InitializeUI();
        }

        private void Start()
        {
            if (isLoadingScene) return;

            PlayEntranceAnimation();
        }

        private void OnDestroy()
        {
            entranceSequence?.Kill();
        }

        private void InitializeUI()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(HandleAcceptClicked);
            }

            if (termsLinkButton != null)
            {
                termsLinkButton.onClick.RemoveAllListeners();
                termsLinkButton.onClick.AddListener(HandleTermsClicked);
            }

            if (privacyLinkButton != null)
            {
                privacyLinkButton.onClick.RemoveAllListeners();
                privacyLinkButton.onClick.AddListener(HandlePrivacyClicked);
            }

            RefreshCopy();
        }

        public void RefreshCopy()
        {
            var cfg = Config;
            if (cfg == null) return;

            if (headingText != null)
            {
                headingText.text = "WELCOME";
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

        private void PlayEntranceAnimation()
        {
            if (!Application.isPlaying) return;

            entranceSequence?.Kill();
            entranceSequence = DOTween.Sequence();
            entranceSequence.SetUpdate(true);

            if (backgroundFill != null)
            {
                Color targetBg = new Color(0.08f, 0.05f, 0.15f, 1.0f);
                backgroundFill.color = targetBg;
            }

            if (cardRoot != null)
            {
                cardRoot.localScale = Vector3.one * 0.75f;
                entranceSequence.Join(cardRoot.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
            }
        }

        private void HandleAcceptClicked()
        {
            if (isProcessingAccept || isLoadingScene) return;
            isProcessingAccept = true;

            var cfg = Config;
            if (cfg == null || (string.IsNullOrEmpty(cfg.TermsOfServiceContent) && string.IsNullOrEmpty(cfg.TermsOfServiceUrl)))
            {
                Debug.LogError("[WelcomeSceneController] Cannot record acceptance: Legal terms configuration is missing!");
                isProcessingAccept = false;
                return;
            }

            // Save acceptance record strictly on explicit ACCEPT tap
            LegalManager.RecordTermsAcceptance(cfg);
            ForcePreviewMode = false;

            if (acceptButton != null)
            {
                acceptButton.interactable = false;
                acceptButton.transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f, 1, 0f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        LoadGameplayScene();
                    });
            }
            else
            {
                LoadGameplayScene();
            }
        }

        private void LoadGameplayScene()
        {
            if (isLoadingScene) return;
            isLoadingScene = true;

            Debug.Log($"[WelcomeSceneController] Loading gameplay scene: {gameplaySceneName}");
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void HandleTermsClicked()
        {
            var cfg = Config;
            string title = cfg != null ? cfg.TermsOfServiceTitle : "Terms of Service";
            string content = cfg != null ? cfg.TermsOfServiceContent : string.Empty;
            string url = cfg != null ? cfg.TermsOfServiceUrl : string.Empty;

            OpenDocument(title, content, url);
        }

        private void HandlePrivacyClicked()
        {
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
                    if (cardRoot != null) cardRoot.gameObject.SetActive(true);
                });
            }
            else if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }
    }
}
