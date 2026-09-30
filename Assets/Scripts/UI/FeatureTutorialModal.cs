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
            if (okButton != null)
            {
                okButton.onClick.AddListener(HandleOkClicked);
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

            // Configure Texts
            if (titleText != null)
            {
                titleText.text = feature.TutorialTitle;
            }
            if (descriptionText != null)
            {
                descriptionText.text = feature.TutorialDescription;
            }
            if (okButtonText != null)
            {
                okButtonText.text = "OK";
            }

            // Configure Illustration artwork
            if (illustrationImage != null)
            {
                if (feature.TutorialArtwork != null)
                {
                    illustrationImage.sprite = feature.TutorialArtwork;
                    illustrationImage.gameObject.SetActive(true);
                }
                else
                {
                    illustrationImage.gameObject.SetActive(false);
                }
            }

            // Custom prefab if assigned
            if (customPrefabContainer != null)
            {
                for (int i = customPrefabContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(customPrefabContainer.GetChild(i).gameObject);
                }
                if (feature.TutorialPrefab != null)
                {
                    Instantiate(feature.TutorialPrefab, customPrefabContainer);
                }
            }

            // Entrance animation (unscaled time)
            modalSequence?.Kill();
            modalSequence = DOTween.Sequence();

            if (backdropDim != null)
            {
                Color targetDim = new Color(0f, 0f, 0f, 0.78f);
                backdropDim.color = new Color(0f, 0f, 0f, 0f);
                modalSequence.Join(backdropDim.DOColor(targetDim, 0.20f).SetEase(Ease.Linear));
            }

            if (modalRoot != null)
            {
                modalRoot.localScale = Vector3.one * 0.65f;
                modalSequence.Join(modalRoot.DOScale(Vector3.one, entranceDuration).SetEase(Ease.OutBack));
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
            if (okButton != null)
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

            if (modalRoot != null)
            {
                modalSequence.Join(modalRoot.DOScale(Vector3.one * 0.65f, exitDuration).SetEase(Ease.InBack));
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
            gameObject.SetActive(false);
            currentFeature = null;

            onAllTutorialsDismissed?.Invoke();
            onAllTutorialsDismissed = null;
        }
    }
}
