using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PowerUpUI : MonoBehaviour
{
    // === [NEU] ICON-FELDER ===================================================
    [Header("Icon")]
    [SerializeField] private Image icon;                // UI/PowerUpUI/Icon

    [Header("Icon Sprites")]
    [SerializeField] private Sprite flashSprite;        // ⚡
    [SerializeField] private Sprite cashSprite;         // 💰
    [SerializeField] private Sprite scoreSprite;        // ⭐ optional
    // ========================================================================

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI powerUpText;
    [SerializeField] Slider durationSlider;
    [SerializeField] Image fillImage;           // -> Slider/Fill Area/Fill
    [SerializeField] Image fillGlow;            // optional
    [SerializeField] RectTransform shine;       // optional

    [Header("Look & Feel")]
    [SerializeField] Gradient fillGradient;     // 0=rot, 1=grün
    [SerializeField] float warnThreshold = 0.15f;
    [SerializeField] float smooth = 10f;
    [SerializeField] float shineSpeed = 400f;
    [SerializeField] float glowAlpha = 0.3f;
    [SerializeField] float glowPulseSpeed = 8f;

    [Header("Audio (optional)")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip sfxStart;
    [SerializeField] AudioClip sfxEnd;

    Coroutine running;
    float target01;   // 1 -> 0 (voll -> leer)
    float current01;

    void Start()
    {
        // [NEU] Icon zunächst verbergen
        if (icon) icon.enabled = false;

        // -- dein bestehender Start-Code --
        if (durationSlider)
        {
            durationSlider.wholeNumbers = false;
            durationSlider.interactable = false;
            durationSlider.transition = Selectable.Transition.None;
            durationSlider.targetGraphic = null;
            durationSlider.handleRect = null;

            var imgs = durationSlider.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
                if (img.gameObject.name == "Background") img.enabled = false;
        }

        if (fillImage)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0; // Left
            fillImage.color = Color.white;
            fillImage.enabled = false;
        }

        if (fillGlow) fillGlow.enabled = false;
        if (shine) shine.gameObject.SetActive(false);

        if (powerUpText) powerUpText.gameObject.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!durationSlider || !durationSlider.gameObject.activeSelf) return;

        current01 = Mathf.Lerp(current01, target01, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        durationSlider.value = current01 * durationSlider.maxValue;

        if (fillImage)
        {
            if (current01 <= 0.001f) fillImage.enabled = false;
            else
            {
                if (!fillImage.enabled) fillImage.enabled = true;
                fillImage.color = (fillGradient != null) ? fillGradient.Evaluate(current01) : Color.white;
            }
        }

        if (fillGlow)
        {
            if (current01 < warnThreshold && current01 > 0f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed);
                fillGlow.enabled = true;
                var c = fillGlow.color; c.a = glowAlpha * pulse; fillGlow.color = c;
            }
            else fillGlow.enabled = false;
        }
    }

    // === deine bestehende API (ohne Icon) bleibt erhalten ====================
    public void ShowPowerUp(string label, float duration)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShowRoutine(label, duration));
    }
    // ========================================================================

    // === [NEU] Overload: zeigt Text + wählt automatisch das Icon ============
    public void ShowPowerUp(PowerUp.PowerUpType type, string label, float duration)
    {
        // Icon auswählen
        if (icon)
        {
            icon.sprite = GetSprite(type);
            icon.enabled = icon.sprite != null;   // nur anzeigen, wenn Sprite vorhanden
        }

        // den bestehenden Flow (Text + Slider) verwenden
        ShowPowerUp(label, duration);
    }

    // Sprite-Zuweisung je nach PowerUp-Typ
    private Sprite GetSprite(PowerUp.PowerUpType t)
    {
        switch (t)
        {
            case PowerUp.PowerUpType.FireRate: return flashSprite;   // ⚡
            case PowerUp.PowerUpType.ScoreBoost: return cashSprite;    // 💰 (oder scoreSprite)
            // case PowerUp.PowerUpType.Health:  return heartSprite;   // falls du willst
            default: return null;
        }
    }
    // ========================================================================

    IEnumerator ShowRoutine(string label, float duration)
    {
        duration = Mathf.Max(0.01f, duration);

        if (powerUpText) { powerUpText.text = label; powerUpText.gameObject.SetActive(true); }
        if (durationSlider)
        {
            durationSlider.maxValue = duration;
            durationSlider.value = duration;  // START: VOLL
            durationSlider.gameObject.SetActive(true);
        }

        target01 = 1f;
        current01 = 1f;

        if (sfxSource && sfxStart) sfxSource.PlayOneShot(sfxStart);
        if (shine && durationSlider) StartCoroutine(ShineWipeOnce());

        float t = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            target01 = Mathf.Clamp01(t / duration);
            yield return null;
        }

        if (sfxSource && sfxEnd) sfxSource.PlayOneShot(sfxEnd);
        yield return null;

        if (powerUpText) powerUpText.gameObject.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
        if (fillGlow) fillGlow.enabled = false;
        if (shine) shine.gameObject.SetActive(false);

        // [NEU] Icon wieder ausblenden & Sprite löschen
        if (icon) { icon.enabled = false; icon.sprite = null; }

        running = null;
    }

    IEnumerator ShineWipeOnce()
    {
        if (shine == null || durationSlider == null) yield break;

        var rect = durationSlider.GetComponent<RectTransform>().rect;
        float width = rect.width;

        shine.gameObject.SetActive(true);
        shine.anchoredPosition = new Vector2(-width, shine.anchoredPosition.y);

        while (shine.anchoredPosition.x < width)
        {
            shine.anchoredPosition += Vector2.right * shineSpeed * Time.deltaTime;
            yield return null;
        }

        shine.gameObject.SetActive(false);
    }
}
