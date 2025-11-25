using UnityEngine;

public class PowerUpUIManager : MonoBehaviour
{
    [Header("Zuweisungen der UI-Slots")]
    public PowerUpUI uiFireRate;
    public PowerUpUI uiScoreBoost;
    public PowerUpUI uiSpeedBoost;
    public PowerUpUI uiTimeSlow;

    public void ShowUI(PowerUp.PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUp.PowerUpType.FireRate:
                uiFireRate.ShowPowerUp(type, "FIRE RATE BOOST", duration);
                break;
            case PowerUp.PowerUpType.ScoreBoost:
                uiScoreBoost.ShowPowerUp(type, "SCORE BOOST", duration);
                break;
            case PowerUp.PowerUpType.SpeedBoost:
                uiSpeedBoost.ShowPowerUp(type, "SPEED BOOST", duration);
                break;
            case PowerUp.PowerUpType.TimeSlow:
                uiTimeSlow.ShowPowerUp(type, "TIME SLOW", duration);
                break;
                // Health braucht keine UI-Anzeige
        }
    }
}
