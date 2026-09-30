using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace AntsJam.UI
{
    public class HardLevelIntroOverlay : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image backdropDim;
        [SerializeField] private RectTransform emblemRoot;
        [SerializeField] private Image emblemImage;

        [Header("Animation Timings (Inspector Configurable)")]
        [Tooltip("Duration of the pop-in entrance scaling")]
        [SerializeField] private float popInDuration = 0.35f;

        [Tooltip("Duration the emblem is held centered over the board")]
        [SerializeField] private float holdDuration = 1.15f;

        [Tooltip("Duration of the fade-out and dismiss transition")]
        [SerializeField] private float fadeOutDuration = 0.30f;

        private Sequence introSequence;
        private Action onCompleteCallback;

        public bool IsShowing => gameObject.activeSelf;
        public float PopInDuration { get => popInDuration; set => popInDuration = value; }
        public float HoldDuration { get => holdDuration; set => holdDuration = value; }
        public float FadeOutDuration { get => fadeOutDuration; set => fadeOutDuration = value; }

        private void OnDestroy()
        {
            introSequence?.Kill();
        }

        /// <summary>
        /// Plays the Hard Level intro sequence (pop-in, hold, fade-out) using unscaled time.
        /// Pauses game timer and input while active.
        /// </summary>
        public void PlayHardIntro(Action onComplete)
        {
            this.onCompleteCallback = onComplete;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            introSequence?.Kill();
            introSequence = DOTween.Sequence();

            // Backdrop dimming
            if (backdropDim != null)
            {
                Color targetDim = new Color(0f, 0f, 0f, 0.72f);
                backdropDim.color = new Color(0f, 0f, 0f, 0f);
                introSequence.Join(backdropDim.DOColor(targetDim, popInDuration * 0.7f).SetEase(Ease.Linear));
            }

            // Pop-in scale with slight bounce
            if (emblemRoot != null)
            {
                emblemRoot.localScale = Vector3.one * 0.45f;
                introSequence.Join(emblemRoot.DOScale(Vector3.one, popInDuration).SetEase(Ease.OutBack));
                // Subtle pulse during hold
                introSequence.Insert(popInDuration + 0.1f, emblemRoot.DOScale(Vector3.one * 1.05f, 0.45f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
            }

            // Fade-out and scale down after hold duration
            float fadeStartTime = popInDuration + holdDuration;
            if (backdropDim != null)
            {
                introSequence.Insert(fadeStartTime, backdropDim.DOColor(new Color(0f, 0f, 0f, 0f), fadeOutDuration).SetEase(Ease.InQuad));
            }
            if (emblemRoot != null)
            {
                introSequence.Insert(fadeStartTime, emblemRoot.DOScale(Vector3.one * 0.60f, fadeOutDuration).SetEase(Ease.InBack));
            }
            if (emblemImage != null)
            {
                introSequence.Insert(fadeStartTime, emblemImage.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
            }

            introSequence.SetUpdate(true);
            introSequence.OnComplete(() =>
            {
                Dismiss();
            });
        }

        public void Dismiss()
        {
            introSequence?.Kill();
            gameObject.SetActive(false);

            if (emblemImage != null)
            {
                emblemImage.color = Color.white;
            }
            if (emblemRoot != null)
            {
                emblemRoot.localScale = Vector3.one;
            }

            var cb = onCompleteCallback;
            onCompleteCallback = null;
            cb?.Invoke();
        }
    }
}
