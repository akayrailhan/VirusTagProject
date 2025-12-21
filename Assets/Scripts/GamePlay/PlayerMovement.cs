using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float speed = 5f;

    private Vector2 _moveDirection;

    // Sadece sahibi olan (Owner) oyuncu input dinler
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.MoveEvent += OnMove;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.MoveEvent -= OnMove;
    }

    private void OnMove(Vector2 direction)
    {
        _moveDirection = direction;
    }

    private void FixedUpdate()
    {
        // Hareket yetkisi Client'tadır (Client Authoritative)
        if (!IsOwner) return;

        rb.linearVelocity = _moveDirection * speed;
    }
}