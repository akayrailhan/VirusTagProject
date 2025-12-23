using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Scene Gate")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Spawn Occupancy Check")]
    [SerializeField] private LayerMask playerLayer;     // Player layer'ını seç
    [SerializeField] private float checkRadius = 0.5f;  // spawn noktası kontrol yarıçapı

    private Vector2 _moveDirection;
    private bool _movementUnlocked;

    // Sabit 4 spawn noktası (senin verdiğin koordinatlar)
    private static readonly Vector2[] SpawnPositions =
    {
        new Vector2(10f,  4f),
        new Vector2(10f, -4f),
        new Vector2(-10f,-4f),
        new Vector2(-10f, 4f),
    };

    public override void OnNetworkSpawn()
    {
        // Input sadece owner
        if (IsOwner)
        {
            inputReader.MoveEvent += OnMove;

            // Eğer zaten Game sahnesinde spawn olduysa kilidi aç
            if (SceneManager.GetActiveScene().name == gameSceneName)
                _movementUnlocked = true;
        }

        // Spawn kararını server versin (race condition yaşamamak için)
        if (IsServer)
        {
            Vector2 pos = PickRandomFreeSpawn();
            TeleportOnServer(pos);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.MoveEvent -= OnMove;
    }

    private void OnMove(Vector2 direction)
    {
        // İlk kez hareket denemesinde sahneyi kontrol et
        if (!_movementUnlocked)
        {
            if (SceneManager.GetActiveScene().name == gameSceneName)
            {
                _movementUnlocked = true; // bir kere açıldı mı, artık kontrol yok
            }
            else
            {
                _moveDirection = Vector2.zero;
                return;
            }
        }

        _moveDirection = direction;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (!_movementUnlocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = _moveDirection * speed;
    }

    // --- Spawn seçimi: rastgele sırala, boş olanı bul ---
    private Vector2 PickRandomFreeSpawn()
    {
        int n = SpawnPositions.Length;

        // 0..n-1 karıştır
        List<int> idxs = new List<int>(n);
        for (int i = 0; i < n; i++) idxs.Add(i);

        for (int i = 0; i < n; i++)
        {
            int j = Random.Range(i, n);
            (idxs[i], idxs[j]) = (idxs[j], idxs[i]);
        }

        // Sırayla boş arıyoruz
        foreach (int idx in idxs)
        {
            Vector2 p = SpawnPositions[idx];
            bool occupied = Physics2D.OverlapCircle(p, checkRadius, playerLayer) != null;
            if (!occupied)
                return p;
        }

        // Hepsi doluysa: rastgele fallback
        return SpawnPositions[Random.Range(0, n)];
    }

    // --- Teleport: server setler, clientlara yayar ---
    private void TeleportOnServer(Vector2 pos)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = pos;
        }
        else
        {
            transform.position = pos;
        }

        // NetworkTransform yoksa gerekli; varsa bile sorun çıkarmaz
        TeleportClientRpc(pos);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector2 pos)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = pos;
        }
        else
        {
            transform.position = pos;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        foreach (var p in SpawnPositions)
            Gizmos.DrawWireSphere(p, checkRadius);
    }
#endif
}
