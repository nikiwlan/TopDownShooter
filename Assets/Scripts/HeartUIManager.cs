using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUIManager : MonoBehaviour
{
    [Header("References")]
    public List<Image> hearts;            // Heart (0..2) – UI Images!
    public GameObject centerHeartEffect;  // UI Image in der Mitte (deaktiviert)

    void Awake()
    {
        Debug.Log($"[HeartUIManager/Awake] heartsList={(hearts != null ? hearts.Count : 0)}, centerFX={(centerHeartEffect != null)}");

        if (centerHeartEffect != null)
            centerHeartEffect.SetActive(false);

        ValidateSetup();
    }

    void OnEnable()
    {
        // Versuche beim Aktivieren sofort mit PlayerHealth zu syncen
        var player = FindObjectOfType<PlayerHealth>();
        Debug.Log($"[HeartUIManager/OnEnable] Player found? {player != null}");
        if (player != null) UpdateHearts(player.currentHealth);
    }

    public void UpdateHearts(int currentHealth)
    {
        if (hearts == null || hearts.Count == 0)
        {
            Debug.LogError("[HeartUIManager/UpdateHearts] hearts-Liste ist leer oder NULL. Zieh Heart (0..2) in die Liste im Inspector!");
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
            Debug.Log($"   - hearts[{i}] name={hearts[i].name}, enabled={shouldShow}, activeInHierarchy={hearts[i].gameObject.activeInHierarchy}");
        }

        // Zusatz: prüfe ob der Canvas sichtbar ist
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) Debug.LogError("[HeartUIManager] KEIN Canvas gefunden. UI-Objekt muss unter einem Canvas liegen!");
        else Debug.Log($"[HeartUIManager] Canvas mode={canvas.renderMode}, enabled={canvas.enabled}, gameObjectActive={canvas.gameObject.activeInHierarchy}");
    }

    public void PlayHeartPickupEffect()
    {
        if (centerHeartEffect == null) return;

        centerHeartEffect.SetActive(true);

        RectTransform rect = centerHeartEffect.GetComponent<RectTransform>();
        CanvasGroup cg = centerHeartEffect.GetComponent<CanvasGroup>();

        // Startwerte – leicht über dem Spieler
        rect.anchoredPosition = new Vector2(-50f, -50f);
        rect.localScale = Vector3.one * 0.8f;

        if (cg != null)
            cg.alpha = 1f;

        // Zielpunkte für Kurve
        Vector2 midPoint = new Vector2(-150f, 150f);   // fliegt erst nach oben links
        Vector2 endPoint = new Vector2(-450f, 230f);   // dann zur Herzleiste

        float duration = 1.2f; // etwas langsamer

        // Wir nutzen einen einfachen "Bogen"-Effekt über zwei Tweens
        LeanTween.move(rect, midPoint, duration * 0.5f).setEaseOutQuad().setOnComplete(() =>
        {
            LeanTween.move(rect, endPoint, duration * 0.5f).setEaseInCubic();
        });

        // Herz leicht pulsieren lassen
        LeanTween.scale(rect, Vector3.one * 1.1f, duration * 0.5f).setEaseOutBack().setLoopPingPong(1);

        // Danach verblassen
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




    // --- harte Setup-Prüfungen mit klaren Hinweisen ---
    void ValidateSetup()
    {
        // Hearts müssen UI Images unter einem RectTransform sein
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
                    Debug.LogError($"[HeartUIManager/Validate] {img.name} hat KEIN RectTransform (kein UI-Objekt).");

                if (img.sprite == null)
                    Debug.LogError($"[HeartUIManager/Validate] {img.name} hat KEIN Sprite (Image.SourceImage).");

                // Liegt das Objekt im Canvas?
                var canvas = img.GetComponentInParent<Canvas>();
                if (canvas == null)
                    Debug.LogError($"[HeartUIManager/Validate] {img.name} liegt NICHT unter einem Canvas.");
            }
        }

        if (centerHeartEffect != null)
        {
            if (centerHeartEffect.GetComponent<RectTransform>() == null)
                Debug.LogError("[HeartUIManager/Validate] CenterHeartEffect hat KEIN RectTransform (kein UI-Image).");

            var img = centerHeartEffect.GetComponent<Image>();
            if (img == null) Debug.LogError("[HeartUIManager/Validate] CenterHeartEffect hat KEIN Image-Component.");
            else if (img.sprite == null) Debug.LogError("[HeartUIManager/Validate] CenterHeartEffect/Image hat KEIN Sprite (Source Image).");
        }
        else
        {
            Debug.LogWarning("[HeartUIManager/Validate] CenterHeartEffect nicht zugewiesen.");
        }
    }
}
