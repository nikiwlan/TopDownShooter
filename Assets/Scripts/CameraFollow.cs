using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float height = 10f;       // Höhe über Bodenebene (Y)
    public float distance = 0f;      // Versatz in Z-Richtung (optional)
    public float smoothSpeed = 10f;  // Glättung

    void LateUpdate()
    {
        if (target == null) return;

        // Zielposition oberhalb des Spielers (XZ-Ebene)
        Vector3 desiredPosition = new Vector3(
            target.position.x,
            height,
            target.position.z - distance
        );

        // Sanfte Bewegung der Kamera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Nach unten schauen (Top-Down)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
