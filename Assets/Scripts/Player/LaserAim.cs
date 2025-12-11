using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserAim : MonoBehaviour
{
    public PlayerShooting playerShooting;   // Referenz zu deinem Script
    public Transform firePoint;            // Mündung
    public float maxDistance = 20f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
    }

    void Update()
    {
        // Wenn Spieler eingefroren ist, Linie aus
        if (playerShooting != null && playerShooting.isFrozen)
        {
            lr.enabled = false;
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (!plane.Raycast(ray, out float dist))
        {
            lr.enabled = false;
            return;
        }

        lr.enabled = true;

        Vector3 target = ray.GetPoint(dist);

        Vector3 origin = firePoint.position;
        Vector3 dir = target - origin;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            dir = firePoint.forward;
        else
            dir.Normalize();

        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin + dir * maxDistance);
    }
}
