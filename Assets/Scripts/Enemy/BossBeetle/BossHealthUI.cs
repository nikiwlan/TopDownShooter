using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Wichtig für Coroutinen
using System.Collections.Generic;

public class BossHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Image healthFillImage;
    public RectTransform separatorContainer;
    public GameObject separatorPrefab;

    [Header("Position & Abstand")]
    public float heightAboveGround = 4f;
    public float distanceHorizontal = 1.5f;
    public float distanceVertical = 3.0f;

    [Header("Damage Feedback (NEU)")]
    public Color flashColor = Color.white; // Farbe beim Treffer
    public float shakeDuration = 0.2f;     // Wie lange wackelt es?
    public float shakeStrength = 0.2f;     // Wie stark wackelt es?

    [Header("Rotation")]
    public bool keepRotationFixed = true;

    private Quaternion _fixedRotation;
    private List<GameObject> _spawnedSeparators = new List<GameObject>();
    private Transform _targetBoss;

    // Variablen für den Effekt
    private int _lastHealth = -1;
    private Vector3 _currentShakeOffset = Vector3.zero;
    private Color _defaultColor;
    private Coroutine _damageCoroutine;

    void Awake()
    {
        _fixedRotation = Quaternion.Euler(90f, 0f, 0f);

        if (transform.parent != null)
            _targetBoss = transform.parent;

        // Speichere die normale rote Farbe
        if (healthFillImage != null)
            _defaultColor = healthFillImage.color;
    }

    void LateUpdate()
    {
        // 1. ROTATION
        if (keepRotationFixed)
            transform.rotation = _fixedRotation;

        // 2. POSITION + SHAKE (Das Wackeln wird hier draufgerechnet)
        if (_targetBoss != null)
        {
            float verticalFactor = Mathf.Abs(_targetBoss.forward.z);
            float currentZOffset = Mathf.Lerp(distanceHorizontal, distanceVertical, verticalFactor);

            Vector3 targetPos = _targetBoss.position;
            targetPos.y += heightAboveGround;
            targetPos.z += currentZOffset;

            // Hier addieren wir das Wackeln (Shake Offset) zur Position
            transform.position = targetPos + _currentShakeOffset;
        }
    }

    public void Initialize(int currentHealth, int maxHealth)
    {
        // Setze Startwert, damit es beim Spawnen nicht blitzt
        _lastHealth = currentHealth;

        UpdateHealth(currentHealth, maxHealth);
        CreatePhaseSeparators(maxHealth);
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        // PRÜFUNG: Haben wir Leben verloren?
        if (_lastHealth != -1 && currentHealth < _lastHealth)
        {
            // Ja! Starte den Effekt
            if (_damageCoroutine != null) StopCoroutine(_damageCoroutine);
            _damageCoroutine = StartCoroutine(PlayDamageEffect());
        }
        _lastHealth = currentHealth;

        // Balken füllen
        if (healthFillImage != null)
        {
            float fillAmount = (float)currentHealth / (float)maxHealth;
            healthFillImage.fillAmount = fillAmount;
        }
    }

    // Die "Magie": Wackeln und Färben
    IEnumerator PlayDamageEffect()
    {
        if (healthFillImage == null) yield break;

        // 1. Farbe auf Weiß setzen
        healthFillImage.color = flashColor;

        float timer = 0f;
        while (timer < shakeDuration)
        {
            // 2. Zufälliges Wackeln erzeugen
            _currentShakeOffset = Random.insideUnitSphere * shakeStrength;

            timer += Time.deltaTime;
            yield return null; // Warten bis zum nächsten Frame
        }

        // 3. Alles zurücksetzen
        _currentShakeOffset = Vector3.zero;
        healthFillImage.color = _defaultColor;
    }

    private void CreatePhaseSeparators(int maxHealth)
    {
        foreach (var sep in _spawnedSeparators) { if (sep != null) Destroy(sep); }
        _spawnedSeparators.Clear();

        if (separatorPrefab == null || separatorContainer == null) return;

        float width = separatorContainer.rect.width;

        for (int i = 10; i < maxHealth; i += 10)
        {
            float normalizedPos = (float)i / (float)maxHealth;
            GameObject sep = Instantiate(separatorPrefab, separatorContainer);
            RectTransform rt = sep.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(width * normalizedPos, 0);
            rt.localScale = Vector3.one;

            _spawnedSeparators.Add(sep);
        }
    }
}