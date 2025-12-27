using UnityEngine;

public class ShieldPulse : MonoBehaviour
{
    [SerializeField] private float scaleAmp = 0.06f;
    [SerializeField] private float speed = 3.5f;

    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * speed) * scaleAmp;
        transform.localScale = baseScale * s;
    }
}
