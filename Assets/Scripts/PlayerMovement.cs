using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;

    private Rigidbody rb;
    private Vector3 input;
    private Vector3 moveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Sicherheit
        rb.useGravity = false; // 2.5D → keine Schwerkraft nötig
    }

    void Update()
    {
        // Eingaben erfassen (WASD oder Pfeiltasten)
        input.x = Input.GetAxisRaw("Horizontal");
        input.z = Input.GetAxisRaw("Vertical");
        input.y = 0f;

        // Diagonale normalisieren (gleiches Tempo in alle Richtungen)
        moveDirection = input.normalized;
    }

    void FixedUpdate()
    {
        // Bewegung in XZ-Ebene
        Vector3 velocity = moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + velocity);

        // Optional: Spieler schaut in Bewegungsrichtung
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.2f));
        }
    }
}
