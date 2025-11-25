using UnityEngine;
using TMPro;

public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager Instance;

    [Header("Settings")]
    public GameObject scorePopupPrefab;    // TMP UI Prefab
    public Transform scoreTarget; // <-- Zielpunkt (z. B. UI-Score-Text)

    [Header("Endpunkt Offset")]
    public Vector2 endOffset = new Vector2(0f, 0f);

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

        // --- Endposition (MIT OFFSET) ---
        Vector2 end = (Vector2)scoreTarget.position + endOffset;

        // --- Apex: höchster Punkt ---
        Vector2 apex = (start + end) * 0.5f;
        apex.y += 120f;
        apex.x += 40f;

        float duration = 1.2f;

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
                LeanTween.alphaCanvas(cg, 0f, 1.2f)
                    .setEaseOutQuad()
                    .setOnComplete(() =>
                    {
                        Destroy(popup);
                    });
            });

        LeanTween.scale(rect, Vector3.one * 0.8f, duration)
            .setEaseInOutSine();
    }
}
