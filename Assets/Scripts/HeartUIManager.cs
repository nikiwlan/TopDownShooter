using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUIManager : MonoBehaviour
{
    [Header("References")]
    public List<Image> hearts;            // Liste der Herz-Icons (0..n)
    public GameObject centerHeartEffect;  // optionaler Effekt beim Heilen

    void Awake()
    {
        Debug.Log($"[HeartUIManager/Awake] heartsList={(hearts != null ? hearts.Count : 0)}, centerFX={(centerHeartEffect != null)}");

        if (centerHeartEffect != null)
            centerHeartEffect.SetActive(false);

        ValidateSetup();
    }

    // ❌ Entfernt: automatisches Update beim OnEnable
    // Der Player ist jetzt verantwortlich für das erste Update.
    // void OnEnable() { ... }

    public void UpdateHearts(int currentHealth)
    {
        if (hearts == null || hearts.Count == 0)
        {
            Debug.LogError("[HeartUIManager/UpdateHearts] hearts-Liste ist leer oder NULL. Zieh Heart (0..n) in die Liste im Inspector!");
            return;
        }

        Debug.Log($"[HeartUIManager/UpdateHearts] set visible hearts = {currentHealth}");
        for (int i = 0; i < hearts.Count; i++)
        {
            bool shouldShow = (i < currentHealth);
            if (hearts[i] == null)
            {
                Debug.LogError($"[HeartUIManager/UpdateHearts] hearts[{i}] ist NULL.");
                continue;
            }

            hearts[i].enabled = shouldShow;
        }

        // Canvas-Prüfung (optional)
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("[HeartUIManager] Kein Canvas gefunden! UI-Objekt muss unter einem Canvas liegen.");
    }

    public void PlayHeartPickupEffect()
    {
        if (centerHeartEffect == null) return;

        centerHeartEffect.SetActive(true);

        RectTransform rect = centerHeartEffect.GetComponent<RectTransform>();
        CanvasGroup cg = centerHeartEffect.GetComponent<CanvasGroup>();

        // Startwerte
        rect.anchoredPosition = new Vector2(-50f, -50f);
        rect.localScale = Vector3.one * 0.8f;
        if (cg != null) cg.alpha = 1f;

        // Zielpunkte
        Vector2 midPoint = new Vector2(-150f, 150f);
        Vector2 endPoint = new Vector2(-450f, 230f);
        float duration = 1.2f;

        // Flugbahn
        LeanTween.move(rect, midPoint, duration * 0.5f).setEaseOutQuad().setOnComplete(() =>
        {
            LeanTween.move(rect, endPoint, duration * 0.5f).setEaseInCubic();
        });

        // Pulsieren
        LeanTween.scale(rect, Vector3.one * 1.1f, duration * 0.5f).setEaseOutBack().setLoopPingPong(1);

        // Verblassen
        LeanTween.delayedCall(duration, () =>
        {
            if (cg != null)
            {
                LeanTween.alphaCanvas(cg, 0f, 0.4f).setOnComplete(() =>
                {
                    cg.alpha = 1f;
                    centerHeartEffect.SetActive(false);
                });
            }
            else
            {
                centerHeartEffect.SetActive(false);
            }
        });
    }

    // Setup-Prüfungen
    void ValidateSetup()
    {
        if (hearts != null)
        {
            for (int i = 0; i < hearts.Count; i++)
            {
                var img = hearts[i];
                if (img == null)
                {
                    Debug.LogError($"[HeartUIManager/Validate] hearts[{i}] ist NULL.");
                    continue;
                }

                if (img.GetComponent<RectTransform>() == null)
                    Debug.LogError($"[HeartUIManager/Validate] {img.name} hat kein RectTransform.");

                if (img.sprite == null)
                    Debug.LogError($"[HeartUIManager/Validate] {img.name} hat kein Sprite (Image.SourceImage).");

                var canvas = img.GetComponentInParent<Canvas>();
                if (canvas == null)
                    Debug.LogError($"[HeartUIManager/Validate] {img.name} liegt nicht unter einem Canvas.");
            }
        }

        if (centerHeartEffect != null)
        {
            if (centerHeartEffect.GetComponent<RectTransform>() == null)
                Debug.LogError("[HeartUIManager/Validate] CenterHeartEffect hat kein RectTransform.");

            var img = centerHeartEffect.GetComponent<Image>();
            if (img == null) Debug.LogError("[HeartUIManager/Validate] CenterHeartEffect hat kein Image.");
            else if (img.sprite == null) Debug.LogError("[HeartUIManager/Validate] CenterHeartEffect/Image hat kein Sprite.");
        }
        else
        {
            Debug.LogWarning("[HeartUIManager/Validate] CenterHeartEffect nicht zugewiesen (optional).");
        }
    }
}
