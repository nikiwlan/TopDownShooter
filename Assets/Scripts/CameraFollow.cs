using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        if (target == null) return;
        // Harte Position (ohne Lerp), damit wir sofort sehen, ob Follow klappt
        transform.position = new Vector3(target.position.x, target.position.y, -10f);
    }
}
