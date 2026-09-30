using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AntLockBadge : MonoBehaviour
{
    private Canvas badgeCanvas;
    private Text counterText;
    private Image padlockIcon;
    private RectTransform badgeRoot;
    private Transform targetTransform;
    private Vector3 offset = new Vector3(0f, 0.95f, 0f);

    private static Sprite s_CachedPadlockSprite;

    public void Initialize(Transform target, int initialLockAmount)
    {
        this.targetTransform = target;
        CreateBadgeUI();
        UpdateCounter(initialLockAmount, animate: false);
    }

    private void CreateBadgeUI()
    {
        // Create WorldSpace Canvas for clean mobile rendering
        GameObject canvasObj = new GameObject("LockBadgeCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = offset;

        badgeCanvas = canvasObj.AddComponent<Canvas>();
        badgeCanvas.renderMode = RenderMode.WorldSpace;
        badgeCanvas.sortingOrder = 50;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        badgeRoot = canvasObj.GetComponent<RectTransform>();
        badgeRoot.sizeDelta = new Vector2(100, 50);
        badgeRoot.localScale = Vector3.one * 0.008f;

        // Face slightly toward camera (pitch 88 deg)
        badgeRoot.localRotation = Quaternion.Euler(60f, 0f, 0f);

        // Background pill
        GameObject bgObj = new GameObject("PillBackground");
        bgObj.transform.SetParent(badgeRoot, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.92f);

        // Padlock Icon
        GameObject iconObj = new GameObject("PadlockIcon");
        iconObj.transform.SetParent(badgeRoot, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.08f, 0.15f);
        iconRect.anchorMax = new Vector2(0.45f, 0.85f);
        iconRect.sizeDelta = Vector2.zero;

        padlockIcon = iconObj.AddComponent<Image>();
        padlockIcon.sprite = GetOrCreatePadlockSprite();
        padlockIcon.color = new Color(1.0f, 0.85f, 0.2f); // Gold lock

        // Counter Text
        GameObject textObj = new GameObject("CounterText");
        textObj.transform.SetParent(badgeRoot, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.45f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.sizeDelta = Vector2.zero;

        counterText = textObj.AddComponent<Text>();
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counterText.fontSize = 28;
        counterText.fontStyle = FontStyle.Bold;
        counterText.alignment = TextAnchor.MiddleCenter;
        counterText.color = Color.white;
    }

    public void UpdateCounter(int amount, bool animate = true)
    {
        if (counterText != null)
        {
            counterText.text = amount.ToString();
        }

        if (animate && badgeRoot != null)
        {
            badgeRoot.DOKill();
            badgeRoot.localScale = Vector3.one * 0.008f;
            badgeRoot.DOPunchScale(Vector3.one * 0.0025f, 0.25f, 8, 0.5f);
        }
    }

    public void PlayUnlockAndDismiss(Action onComplete)
    {
        if (badgeRoot != null)
        {
            badgeRoot.DOKill();
            badgeRoot.DOScale(Vector3.one * 0.012f, 0.18f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    badgeRoot.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            onComplete?.Invoke();
                            Destroy(gameObject);
                        });
                });
        }
        else
        {
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;
        }
    }

    private static Sprite GetOrCreatePadlockSprite()
    {
        if (s_CachedPadlockSprite != null) return s_CachedPadlockSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        Color transparent = Color.clear;
        Color shackleCol = new Color(0.85f, 0.88f, 0.92f, 1f); // Silver shackle
        Color bodyCol = new Color(1.0f, 0.82f, 0.15f, 1f);    // Gold body
        Color keyholeCol = new Color(0.18f, 0.14f, 0.05f, 1f); // Dark keyhole

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                cols[idx] = transparent;

                // Body: rectangle from y: 8..34, x: 14..50
                if (x >= 14 && x <= 50 && y >= 8 && y <= 34)
                {
                    // Keyhole: small dot and slot
                    float dx = x - 32;
                    float dy = y - 23;
                    if (dx * dx + dy * dy <= 12f || (Mathf.Abs(dx) <= 2 && y >= 15 && y <= 23))
                    {
                        cols[idx] = keyholeCol;
                    }
                    else
                    {
                        cols[idx] = bodyCol;
                    }
                    continue;
                }

                // Shackle: arch from y: 34..56, outer radius 15, inner radius 9 centered at (32, 38)
                float sx = x - 32;
                float sy = y - 38;
                float distSq = sx * sx + sy * sy;
                if (y >= 34 && distSq <= 16f * 16f && distSq >= 8f * 8f)
                {
                    cols[idx] = shackleCol;
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        s_CachedPadlockSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return s_CachedPadlockSprite;
    }
}
