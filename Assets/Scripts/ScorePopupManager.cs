using UnityEngine;
using TMPro;

public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager Instance;

    [Header("Settings")]
    public GameObject scorePopupPrefab;    // TMP UI Prefab
    public Transform scoreTarget; // <-- Im Inspector zuweisen!

    private Canvas canvas;

    void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();
    }

    public void SpawnPopup(int amount, Vector3 worldPos)
    {
        if (scorePopupPrefab == null || scoreTarget == null)
        {
            Debug.LogError("[ScorePopupManager] Prefab oder ScoreTarget fehlt!");
            return;
        }

        GameObject popup = Instantiate(scorePopupPrefab, transform);

        RectTransform rect = popup.GetComponent<RectTransform>();
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        TMP_Text text = popup.GetComponentInChildren<TMP_Text>();

        text.text = $"+{amount}";
        cg.alpha = 1f;

        // --- Startposition ---
        Vector2 start = Camera.main.WorldToScreenPoint(worldPos);
        rect.position = start + new Vector2(25f, 40f);

        // --- Endposition ---
        Vector2 end = scoreTarget.position;

        // --- Apex: der höchste Punkt des Bogens ---
        Vector2 apex = (start + end) * 0.5f;
        apex.y += 120f;    // Höhe des Bogens
        apex.x += 40f;     // etwas nach rechts schieben, für diagonalen Bogen

        float duration = 1.2f; // schön langsam

        // --- Animation: t von 0 → 1 ---
        LeanTween.value(rect.gameObject, 0f, 1f, duration)
             .setEaseOutSine()
             .setOnUpdate((float t) =>
             {
                 Vector2 pos =
                     (1 - t) * (1 - t) * start +
                     2 * (1 - t) * t * apex +
                     (t * t) * end;

                 rect.position = pos;
             })
             .setOnComplete(() =>
             {
                 // NICHT sofort löschen: Erst langsam ausblassen
                 LeanTween.alphaCanvas(cg, 0f, 1.2f)   // ← LANGSAM ausblassen (1.2 Sekunden)
                     .setEaseOutQuad()
                     .setOnComplete(() =>
                     {
                         Destroy(popup);
                     });
             });

        // Optional: leichte Größenveränderung während des Flugs
        LeanTween.scale(rect, Vector3.one * 0.8f, duration)
            .setEaseInOutSine();
    }

}
