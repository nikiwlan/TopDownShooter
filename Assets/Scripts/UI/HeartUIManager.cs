using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUIManager : MonoBehaviour
{
    [Header("References")]
    public List<Image> hearts;            // Liste der Herz-Icons (0..n)
    public GameObject centerHeartEffect;  // optionaler Effekt beim Heilen

    [Header("Shield Settings (2 Charges, 2 Sprites)")]
    public int maxShieldCharges = 2;

    [Tooltip("Voller Schild (CompleteShield)")]
    [SerializeField] private GameObject completeShieldGO;

    [Tooltip("Gebrochener Schild (ShieldPiece)")]
    [SerializeField] private GameObject brokenShieldGO;

    void Awake()
    {
        // UI initial sauber aus
        UpdateShield(0);

        if (centerHeartEffect != null)
            centerHeartEffect.SetActive(false);

        ValidateSetup();
    }

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

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("[HeartUIManager] Kein Canvas gefunden! UI-Objekt muss unter einem Canvas liegen.");
    }

    public void UpdateShield(int charges)
    {
        Debug.Log($"[HeartUIManager] UpdateShield charges={charges}");
        charges = Mathf.Clamp(charges, 0, maxShieldCharges);

        // Shield UI ist optional
        if (completeShieldGO == null && brokenShieldGO == null) return;

        if (charges <= 0)
        {
            if (completeShieldGO) completeShieldGO.SetActive(false);
            if (brokenShieldGO) brokenShieldGO.SetActive(false);
            return;
        }

        if (charges >= maxShieldCharges) // bei 2/2
        {
            if (completeShieldGO) completeShieldGO.SetActive(true);
            if (brokenShieldGO) brokenShieldGO.SetActive(false);
        }
        else // bei 1/2
        {
            if (completeShieldGO) completeShieldGO.SetActive(false);
            if (brokenShieldGO) brokenShieldGO.SetActive(true);
        }
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
        Vector2 endPoint = new Vector2(-450f, 365f);
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

    public void PlayBossHeartEffect(Vector3 bossWorldPos)
    {
        if (centerHeartEffect == null) return;

        // 1. Aktivieren
        centerHeartEffect.SetActive(true);
        RectTransform rect = centerHeartEffect.GetComponent<RectTransform>();
        CanvasGroup cg = centerHeartEffect.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        // 2. Startposition berechnen (Boss 3D -> UI 2D)
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(bossWorldPos);

        // Umrechnen in den lokalen Koordinatenraum des Canvas (Wichtig!)
        RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, Camera.main, out localPoint);

        // HIER ist der Unterschied: Wir setzen den Startpunkt auf den Boss
        rect.anchoredPosition = localPoint;
        rect.localScale = Vector3.one * 0.8f;

        // 3. Deine Animation abspielen (Copy-Paste von deiner Logik, nur Startpunkt ist jetzt anders)
        Vector2 midPoint = new Vector2(-150f, 150f);
        Vector2 endPoint = new Vector2(-450f, 365f); // Ziel oben links
        float duration = 1.2f;

        // Flugbahn
        LeanTween.move(rect, midPoint, duration * 0.5f).setEaseOutQuad().setOnComplete(() =>
        {
            LeanTween.move(rect, endPoint, duration * 0.5f).setEaseInCubic();
        });

        // Pulsieren
        LeanTween.scale(rect, Vector3.one * 1.1f, duration * 0.5f).setEaseOutBack().setLoopPingPong(1);

        // Ausblenden am Ende
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

        // Shield optional, aber wenn gesetzt, warnen bei fehlenden References
        if (completeShieldGO == null)
            Debug.LogWarning("[HeartUIManager/Validate] completeShieldGO ist nicht gesetzt (optional).");
        if (brokenShieldGO == null)
            Debug.LogWarning("[HeartUIManager/Validate] brokenShieldGO ist nicht gesetzt (optional).");
    }
}
