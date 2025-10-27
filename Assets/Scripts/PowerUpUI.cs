using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PowerUpUI : MonoBehaviour
{
    public TextMeshProUGUI powerUpText;
    public Slider durationSlider;

    private Coroutine currentRoutine;

    void Start()
    {
        powerUpText.gameObject.SetActive(false);
        durationSlider.gameObject.SetActive(false);
    }

    public void ShowPowerUp(string message, float duration = 0f)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowPowerUpRoutine(message, duration));
    }

    private IEnumerator ShowPowerUpRoutine(string message, float duration)
    {
        powerUpText.text = message;
        powerUpText.gameObject.SetActive(true);

        if (duration > 0)
        {
            durationSlider.gameObject.SetActive(true);
            durationSlider.maxValue = duration;
            durationSlider.value = duration;

            float timeLeft = duration;
            while (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                durationSlider.value = timeLeft;
                yield return null;
            }

            durationSlider.gameObject.SetActive(false);
        }

        powerUpText.gameObject.SetActive(false);
    }
}
