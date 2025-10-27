using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PowerUpUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI powerUpText;
    [SerializeField] Slider durationSlider;
    [SerializeField] Image fillImage;   // -> Slider/Fill Area/Fill
    [SerializeField] Image fillGlow;    // optional
    [SerializeField] RectTransform shine; // optional

    [Header("Look & Feel")]
    // WICHTIG: Gradient: 0 = ROT, 1 = GRÜN  (so passt die Logik unten)
    [SerializeField] Gradient fillGradient;
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
        // Weiß/Background sicher AUS (damit nie was Weißes zu sehen ist)
        if (durationSlider)
        {
            durationSlider.wholeNumbers = false;
            durationSlider.interactable = false;
            durationSlider.transition = Selectable.Transition.None;
            durationSlider.targetGraphic = null;
            durationSlider.handleRect = null;

            // Background-Image aus
            var imgs = durationSlider.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
                if (img.gameObject.name == "Background") img.enabled = false;
        }

        // Fill korrekt als "Filled/Horizontal"
        if (fillImage)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0; // Left
            fillImage.color = Color.white; // wird per Gradient eingefärbt
            fillImage.enabled = false;     // bis wir >0 haben
        }

        if (fillGlow) fillGlow.enabled = false;
        if (shine) shine.gameObject.SetActive(false);

        if (powerUpText) powerUpText.gameObject.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!durationSlider || !durationSlider.gameObject.activeSelf) return;

        // smooth von target01 -> current01
        current01 = Mathf.Lerp(current01, target01, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        // Slider bekommt „vollen Start, wird kleiner“:
        durationSlider.value = current01 * durationSlider.maxValue;

        // Farbe: 1 = grün, 0 = rot (Gradient: 0=rot,1=grün)
        if (fillImage)
        {
            if (current01 <= 0.001f)
            {
                fillImage.enabled = false; // komplett weg bei 0
            }
            else
            {
                if (!fillImage.enabled) fillImage.enabled = true;
                fillImage.color = (fillGradient != null) ? fillGradient.Evaluate(current01) : Color.white;
            }
        }

        // Warn-Puls (optional)
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

    public void ShowPowerUp(string label, float duration)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShowRoutine(label, duration));
    }

    IEnumerator ShowRoutine(string label, float duration)
    {
        duration = Mathf.Max(0.01f, duration);

        if (powerUpText) { powerUpText.text = label; powerUpText.gameObject.SetActive(true); }
        if (durationSlider)
        {
            durationSlider.maxValue = duration;
            durationSlider.value = duration;           // START: VOLL
            durationSlider.gameObject.SetActive(true);
        }

        // Startzustand: voll
        target01 = 1f;
        current01 = 1f;

        if (sfxSource && sfxStart) sfxSource.PlayOneShot(sfxStart);
        if (shine && durationSlider) StartCoroutine(ShineWipeOnce());

        // Zeit läuft runter
        float t = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            target01 = Mathf.Clamp01(t / duration);    // 1 -> 0
            yield return null;
        }

        if (sfxSource && sfxEnd) sfxSource.PlayOneShot(sfxEnd);
        yield return null;

        if (powerUpText) powerUpText.gameObject.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
        if (fillGlow) fillGlow.enabled = false;
        if (shine) shine.gameObject.SetActive(false);

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
