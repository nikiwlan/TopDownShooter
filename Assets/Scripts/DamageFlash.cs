using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    [Header("UI Element")]
    public Image overlayImage;   // EIN einziges Image!

    [Header("Effect Settings")]
    public float flashDuration = 0.25f;
    public float fadeDuration = 0.7f;

    [Range(0f, 1f)] public float centerAlpha = 0.25f;
    [Range(0f, 1f)] public float edgeAlpha = 0.75f;

    [Header("Death Effect")]
    public float deathOverlayAlpha = 1f;

    Material mat;
    Coroutine flashRoutine;

    void Awake()
    {
        // 🔥 Material dynamisch erzeugen – du musst nichts in Unity anlegen!
        Shader shader = Shader.Find("UI/Default");
        mat = new Material(shader);
        overlayImage.material = mat;

        // Mitte = fast transparent
        // Rand = stärkeres Rot
        mat.SetColor("_Color", new Color(1f, 0f, 0f, 0f));
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float t = 0f;

        // Sofort den Effekt aktivieren
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float lerp = t / flashDuration;

            ApplyRadialGradient(Mathf.Lerp(edgeAlpha, 0f, lerp));

            yield return null;
        }

        // Fade-Out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / fadeDuration;

            ApplyRadialGradient(Mathf.Lerp(0f, 0f, lerp));

            yield return null;
        }

        ApplyRadialGradient(0f);
    }

    // ⚰️ Wird vom Player aufgerufen wenn er stirbt
    public void DeathFlash()
    {
        ApplyRadialGradient(deathOverlayAlpha);
    }

    void ApplyRadialGradient(float strength)
    {
        // Mitte: wenig Alpha → innen hell
        // Rand: viel Alpha → außen dunkel
        float finalCenter = centerAlpha * strength;
        float finalEdge = edgeAlpha * strength;

        // Wir mischen einfach Farben basierend auf der Distanz (Pseudo-Vignette)
        mat.SetColor("_Color",
            new Color(1f,
                      Mathf.Lerp(0.3f, 0f, strength),
                      Mathf.Lerp(0.3f, 0f, strength),
                      finalEdge)
         );

        overlayImage.color = new Color(1f, 1f, 1f, 1f);
    }
}
