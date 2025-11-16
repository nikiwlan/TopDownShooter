using UnityEngine;

public class GateTriggerSound : MonoBehaviour
{
    public AudioSource gateAudio;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger HIT: " + other.name);
        if (other.CompareTag("Enemy"))
        {
            gateAudio.Play();
        }
    }
}
