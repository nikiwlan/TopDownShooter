using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PowerUpUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI powerUpText;
    [SerializeField] private Slider durationSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image fillGlow;
    [SerializeField] private RectTransform shine;

    [Header("Look & Feel")]
    [SerializeField] private Gradient fillGradient;
    [SerializeField] private float warnThreshold = 0.15f;
    [SerializeField] private float smooth = 10f;
    [SerializeField] private float glowAlpha = 0.3f;
    [SerializeField] private float glowPulseSpeed = 8f;

    Coroutine running;
    float target01;
    float current01;

    void Start()
    {
        // Sicherstellen, dass alle UI-Elemente am Anfang ausgeblendet sind
        if (powerUpText) powerUpText.gameObject.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
        if (fillImage) fillImage.enabled = false;
        if (fillGlow) fillGlow.enabled = false;
        if (shine) shine.gameObject.SetActive(false);
        if (iconImage) iconImage.enabled = false;

        // Slider korrekt konfigurieren
        if (durationSlider)
        {
            durationSlider.wholeNumbers = false;
            durationSlider.interactable = false;
            durationSlider.transition = Selectable.Transition.None;
            durationSlider.targetGraphic = null;
            durationSlider.handleRect = null;

            foreach (var img in durationSlider.GetComponentsInChildren<Image>(true))
                if (img.gameObject.name == "Background") img.enabled = false;
        }

        if (fillImage)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.color = Color.white;
        }
    }

    void Update()
    {
        if (!durationSlider || !durationSlider.gameObject.activeSelf) return;

        current01 = Mathf.Lerp(current01, target01, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        durationSlider.value = current01 * durationSlider.maxValue;

        // Farbverlauf aktualisieren
        if (fillImage)
        {
            if (current01 <= 0.001f) fillImage.enabled = false;
            else
            {
                if (!fillImage.enabled) fillImage.enabled = true;
                fillImage.color = (fillGradient != null) ? fillGradient.Evaluate(current01) : Color.white;
            }
        }

        // Pulsierender Glow-Effekt
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

    // ======================================================
    // Öffentliche API
    // ======================================================

    public void ShowPowerUp(PowerUp.PowerUpType type, string label, float duration)
    {
        if (running != null) StopCoroutine(running);
        gameObject.SetActive(true); // sicherstellen, dass das GO aktiv ist
        running = StartCoroutine(ShowRoutine(label, duration));
    }

    IEnumerator ShowRoutine(string label, float duration)
    {
        duration = Mathf.Max(0.01f, duration);

        // Einschalten aller UI-Komponenten
        if (iconImage) iconImage.enabled = true;
        if (powerUpText)
        {
            powerUpText.text = label;
            powerUpText.gameObject.SetActive(true);
        }

        if (durationSlider)
        {
            durationSlider.maxValue = duration;
            durationSlider.value = duration;
            durationSlider.gameObject.SetActive(true);
        }

        if (fillImage) fillImage.enabled = true;
        if (fillGlow) fillGlow.enabled = true;

        target01 = 1f;
        current01 = 1f;

        float t = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            target01 = Mathf.Clamp01(t / duration);
            yield return null;
        }

        // Ausschalten nach Ablauf
        if (iconImage) iconImage.enabled = false;
        if (powerUpText) powerUpText.gameObject.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
        if (fillImage) fillImage.enabled = false;
        if (fillGlow) fillGlow.enabled = false;
        if (shine) shine.gameObject.SetActive(false);

        running = null;
        gameObject.SetActive(true); // Objekt bleibt aktiv für künftige PowerUps
    }
}
