using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;

    private Rigidbody rb;
    private Vector3 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Bewegungseingaben (WASD oder Pfeiltasten)
        input.x = Input.GetAxisRaw("Horizontal");
        input.z = Input.GetAxisRaw("Vertical");
        input.y = 0f; // 🟢 bleibt auf Bodenebene
        input = input.normalized; // verhindert schnellere Diagonalen
    }

    void FixedUpdate()
    {
        // Bewegung in XZ-Ebene
        Vector3 velocity = input * moveSpeed;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }
}
