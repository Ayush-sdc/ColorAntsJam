using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using AntsJam.Progression;

namespace AntsJam.UI
{
    public class FeatureTutorialModal : MonoBehaviour
    {
        [Header("Modal Visuals")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform modalRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Outline titleOutline;
        [SerializeField] private Image radialRaysImage;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private RectTransform customPrefabContainer;
        [SerializeField] private Image descriptionPanel;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button okButton;
        [SerializeField] private Text okButtonText;

        [Header("Animation Timings")]
        [SerializeField] private float entranceDuration = 0.32f;
        [SerializeField] private float exitDuration = 0.20f;
        [SerializeField] private float raysRotationSpeed = 12f;

        private Queue<FeatureDefinition> pendingQueue = new Queue<FeatureDefinition>();
        private FeatureDefinition currentFeature;
        private Action onAllTutorialsDismissed;
        private Sequence modalSequence;
        private bool isAnimating = false;

        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
            EnsureCustomPrefabContainer();
            if (okButton != null)
            {
                okButton.onClick.AddListener(HandleOkClicked);
            }
        }

        private void EnsureCustomPrefabContainer()
        {
            if (customPrefabContainer != null) return;

            Transform existing = transform.Find("CustomPrefabContainer");
            if (existing == null) existing = transform.Find("OverlayContainer");

            if (existing != null)
            {
                customPrefabContainer = existing.GetComponent<RectTransform>();
            }
            else
            {
                var containerObj = new GameObject("CustomPrefabContainer", typeof(RectTransform));
                containerObj.transform.SetParent(transform, false);
                customPrefabContainer = containerObj.GetComponent<RectTransform>();
            }

            if (customPrefabContainer != null)
            {
                customPrefabContainer.anchorMin = Vector2.zero;
                customPrefabContainer.anchorMax = Vector2.one;
                customPrefabContainer.offsetMin = Vector2.zero;
                customPrefabContainer.offsetMax = Vector2.zero;
                customPrefabContainer.pivot = new Vector2(0.5f, 0.5f);
                customPrefabContainer.localScale = Vector3.one;
                customPrefabContainer.SetAsLastSibling();
            }
        }

        private void Update()
        {
            if (gameObject.activeSelf && radialRaysImage != null)
            {
                radialRaysImage.transform.Rotate(0f, 0f, raysRotationSpeed * Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            modalSequence?.Kill();
        }

        /// <summary>
        /// Queues and presents required feature tutorials sequentially.
        /// </summary>
        public void ShowTutorials(List<FeatureDefinition> tutorials, Action onComplete)
        {
            if (tutorials == null || tutorials.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            pendingQueue.Clear();
            foreach (var t in tutorials)
            {
                if (t != null) pendingQueue.Enqueue(t);
            }

            onAllTutorialsDismissed = onComplete;
            ShowNextInQueue();
        }

        private void ShowNextInQueue()
        {
            if (pendingQueue.Count == 0)
            {
                DismissAll();
                return;
            }

            currentFeature = pendingQueue.Dequeue();
            DisplayFeature(currentFeature);
        }

        private void DisplayFeature(FeatureDefinition feature)
        {
            if (feature == null)
            {
                ShowNextInQueue();
                return;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            isAnimating = true;

            bool hasCustomPrefab = feature.TutorialPrefab != null;
            bool hasArtwork = feature.TutorialArtwork != null;

            if (!hasCustomPrefab && !hasArtwork)
            {
                Debug.LogError($"[FeatureTutorialModal] Missing tutorial artwork and prefab for feature '{feature.FeatureId}' (Level {feature.IntroductionLevel}). Presenting readable dismissible fallback.");
            }

            // Prepare entrance animation (unscaled time)
            modalSequence?.Kill();
            modalSequence = DOTween.Sequence();

            if (hasCustomPrefab)
            {
                // Suppress standard UI elements so they do not duplicate flattened composition
                if (titleText != null) titleText.gameObject.SetActive(false);
                if (titleOutline != null) titleOutline.enabled = false;
                if (descriptionPanel != null) descriptionPanel.gameObject.SetActive(false);
                if (descriptionText != null) descriptionText.gameObject.SetActive(false);
                if (illustrationImage != null) illustrationImage.gameObject.SetActive(false);
                if (radialRaysImage != null) radialRaysImage.gameObject.SetActive(false);
                if (okButton != null) okButton.gameObject.SetActive(false);
                if (modalRoot != null) modalRoot.gameObject.SetActive(false);

                EnsureCustomPrefabContainer();
                if (customPrefabContainer != null)
                {
                    customPrefabContainer.gameObject.SetActive(true);
                    for (int i = customPrefabContainer.childCount - 1; i >= 0; i--)
                    {
                        SafeDestroy(customPrefabContainer.GetChild(i).gameObject);
                    }

                    var instance = Instantiate(feature.TutorialPrefab, customPrefabContainer, false);
                    var instRt = instance.GetComponent<RectTransform>();
                    if (instRt != null)
                    {
                        instRt.anchorMin = Vector2.zero;
                        instRt.anchorMax = Vector2.one;
                        instRt.offsetMin = Vector2.zero;
                        instRt.offsetMax = Vector2.zero;
                        instRt.pivot = new Vector2(0.5f, 0.5f);
                        instRt.localScale = Vector3.one;
                    }

                    // Disable redundant BackdropDim child on prefab instance so only single backdropDim is active
                    Transform innerDim = instance.transform.Find("BackdropDim");
                    if (innerDim != null) innerDim.gameObject.SetActive(false);

                    var overlayView = instance.GetComponent<FeatureOverlayView>();
                    if (overlayView != null)
                    {
                        if (overlayView.BackdropDim != null) overlayView.BackdropDim.gameObject.SetActive(false);
                        overlayView.Initialize(feature.FeatureId, feature.TutorialArtwork, HandleOkClicked);
                        if (overlayView.TutorialImage != null)
                        {
                            overlayView.TutorialImage.color = Color.white;
                            overlayView.TutorialImage.preserveAspect = true;
                            overlayView.TutorialImage.raycastTarget = false;
                            overlayView.TutorialImage.gameObject.SetActive(true);
                        }
                        if (overlayView.OkButton != null)
                        {
                            overlayView.OkButton.onClick.RemoveAllListeners();
                            overlayView.OkButton.onClick.AddListener(HandleOkClicked);
                            overlayView.OkButton.interactable = true;
                        }
                    }
                    else
                    {
                        var btn = instance.GetComponentInChildren<Button>(true);
                        if (btn != null)
                        {
                            btn.onClick.RemoveAllListeners();
                            btn.onClick.AddListener(HandleOkClicked);
                            btn.interactable = true;
                        }
                    }

                    customPrefabContainer.localScale = Vector3.one * 0.65f;
                    modalSequence.Join(customPrefabContainer.DOScale(Vector3.one, entranceDuration).SetEase(Ease.OutBack));
                }
            }
            else
            {
                // Fallback standard presentation (or readable fallback when prefab is missing)
                if (customPrefabContainer != null) customPrefabContainer.gameObject.SetActive(false);
                if (modalRoot != null) modalRoot.gameObject.SetActive(true);

                if (titleText != null)
                {
                    titleText.gameObject.SetActive(true);
                    titleText.text = !string.IsNullOrEmpty(feature.TutorialTitle) ? feature.TutorialTitle : feature.DisplayName;
                }
                if (titleOutline != null) titleOutline.enabled = true;
                if (descriptionPanel != null) descriptionPanel.gameObject.SetActive(true);
                if (descriptionText != null)
                {
                    descriptionText.gameObject.SetActive(true);
                    descriptionText.text = feature.TutorialDescription;
                }
                if (okButton != null)
                {
                    okButton.gameObject.SetActive(true);
                    if (okButtonText != null) okButtonText.text = "OK";
                }
                if (illustrationImage != null)
                {
                    if (feature.TutorialArtwork != null)
                    {
                        illustrationImage.sprite = feature.TutorialArtwork;
                        illustrationImage.color = Color.white;
                        illustrationImage.preserveAspect = true;
                        illustrationImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        illustrationImage.gameObject.SetActive(false);
                    }
                }
                if (radialRaysImage != null) radialRaysImage.gameObject.SetActive(true);

                if (modalRoot != null)
                {
                    modalRoot.localScale = Vector3.one * 0.65f;
                    modalSequence.Join(modalRoot.DOScale(Vector3.one, entranceDuration).SetEase(Ease.OutBack));
                }
            }

            // Backdrop dimming
            if (backdropDim != null)
            {
                backdropDim.transform.SetAsFirstSibling();
                Color targetDim = new Color(0f, 0f, 0f, 0.78f);
                backdropDim.color = new Color(0f, 0f, 0f, 0f);
                modalSequence.Join(backdropDim.DOColor(targetDim, 0.20f).SetEase(Ease.Linear));
            }

            modalSequence.SetUpdate(true);
            modalSequence.OnComplete(() => isAnimating = false);
        }

        private void HandleOkClicked()
        {
            if (isAnimating) return;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTap();
            }

            // Save acknowledgement in PlayerPrefs ONLY when OK is pressed
            if (currentFeature != null)
            {
                var config = FeatureProgressionConfig.Instance;
                if (config != null)
                {
                    config.AcknowledgeTutorial(currentFeature.FeatureId);
                }
                else
                {
                    PlayerPrefs.SetInt("TutorialSeen_" + currentFeature.FeatureId, 1);
                    PlayerPrefs.Save();
                }
            }

            // Button feedback
            if (okButton != null && okButton.gameObject.activeInHierarchy)
            {
                isAnimating = true;
                okButton.transform.DOKill();
                okButton.transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.12f, 1, 0f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        AnimateDismissTransition();
                    });
            }
            else
            {
                AnimateDismissTransition();
            }
        }

        private void AnimateDismissTransition()
        {
            modalSequence?.Kill();
            modalSequence = DOTween.Sequence();

            if (customPrefabContainer != null && customPrefabContainer.gameObject.activeSelf)
            {
                modalSequence.Join(customPrefabContainer.DOScale(Vector3.one * 0.65f, exitDuration).SetEase(Ease.InBack));
            }

            if (modalRoot != null && modalRoot.gameObject.activeSelf)
            {
                modalSequence.Join(modalRoot.DOScale(Vector3.one * 0.65f, exitDuration).SetEase(Ease.InBack));
            }

            if (backdropDim != null)
            {
                modalSequence.Join(backdropDim.DOColor(new Color(0f, 0f, 0f, 0f), exitDuration).SetEase(Ease.Linear));
            }

            modalSequence.SetUpdate(true);
            modalSequence.OnComplete(() =>
            {
                isAnimating = false;
                if (pendingQueue.Count > 0)
                {
                    ShowNextInQueue();
                }
                else
                {
                    DismissAll();
                }
            });
        }

        private void DismissAll()
        {
            modalSequence?.Kill();

            if (customPrefabContainer != null)
            {
                for (int i = customPrefabContainer.childCount - 1; i >= 0; i--)
                {
                    SafeDestroy(customPrefabContainer.GetChild(i).gameObject);
                }
                customPrefabContainer.gameObject.SetActive(false);
            }

            if (modalRoot != null)
            {
                modalRoot.gameObject.SetActive(true);
            }

            if (illustrationImage != null)
            {
                illustrationImage.sprite = null;
                illustrationImage.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
            currentFeature = null;

            onAllTutorialsDismissed?.Invoke();
            onAllTutorialsDismissed = null;
        }

        private static void SafeDestroy(GameObject obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
