using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Rigidbody2D rb;

    [Header("Visual (Animator)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Sağ yürüme animasyonu yoksa, sola yürüme animasyonunu sağa giderken aynalamak için aç.")]
    [SerializeField] private bool flipSpriteForRight = true;

    [Header("Animator Parameter Names")]
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string speedParam = "Speed";

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Scene Gate")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Spawn Occupancy Check")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float checkRadius = 0.5f;

    [Header("Slow / Stun")]
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float slowDuration = 2f;

    // Owner tarafı: input + anim için
    private Vector2 _moveDirection;
    private bool _movementUnlocked;

    // Server tarafı: gerçek fizik hareketi burada
    private Vector2 _serverMoveDirection;
    private float _serverSpeedMultiplier = 1f;
    private float _serverSlowEndTime;

    // Owner tarafı (görsel/prediction için slow)
    private float _speedMultiplier = 1f;
    private float _slowEndTime;

    // Anim yönü için: durunca son baktığı yön
    private Vector2 _lastLookDir = Vector2.down;

    // Remote oyuncularda rb.velocity güvenilmez olabiliyor -> transform delta fallback
    private Vector3 _prevWorldPos;

    // Animator param hash
    private int _moveXHash;
    private int _moveYHash;
    private int _speedHash;

    private static readonly Vector2[] SpawnPositions =
    {
        new Vector2(7f,  4f),
        new Vector2(7f, -4f),
        new Vector2(-7f,-4f),
        new Vector2(-7f, 4f),
    };

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        _moveXHash = Animator.StringToHash(moveXParam);
        _moveYHash = Animator.StringToHash(moveYParam);
        _speedHash = Animator.StringToHash(speedParam);

        _prevWorldPos = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        // Hem server hem client tarafında sahne adı uygunsa kilidi aç
        if (SceneManager.GetActiveScene().name == gameSceneName)
            _movementUnlocked = true;

        // Input sadece owner
        if (IsOwner)
        {
            if (inputReader != null)
                inputReader.MoveEvent += OnMove;
        }

        // Server-authoritative: client tarafında fizik simülasyonunu kapat
        if (!IsServer && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Spawn kararını server versin
        if (IsServer)
        {
            Vector2 pos = PickRandomFreeSpawn();
            TeleportOnServer(pos);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputReader != null)
            inputReader.MoveEvent -= OnMove;
    }

    private void OnMove(Vector2 direction)
    {
        // Scene gate
        if (!_movementUnlocked)
        {
            if (SceneManager.GetActiveScene().name == gameSceneName)
                _movementUnlocked = true;
            else
            {
                _moveDirection = Vector2.zero;
                SubmitMoveServerRpc(Vector2.zero);
                return;
            }
        }

        if (direction.sqrMagnitude > 1f) direction.Normalize();
        _moveDirection = direction;

        // input'u server'a yolla (gerçek hareket server'da)
        SubmitMoveServerRpc(_moveDirection);
    }

    [ServerRpc]
    private void SubmitMoveServerRpc(Vector2 direction)
    {
        if (direction.sqrMagnitude > 1f) direction.Normalize();
        _serverMoveDirection = direction;
    }

    private void Update()
    {
        // Animasyon/flip herkesde çalışsın (owner+remote)
        Vector2 visualVel = GetVisualVelocity();
        UpdateAnimatorAndFlip(visualVel);
    }

    private void FixedUpdate()
    {
        // Gerçek hareket SADECE server'da
        if (!IsServer) return;

        // Server tarafında da sahne kilidini kontrol et
        if (!_movementUnlocked)
        {
            if (SceneManager.GetActiveScene().name == gameSceneName)
                _movementUnlocked = true;
            else
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        // slow süresi bittiyse normale dön
        if (_serverSpeedMultiplier < 1f && Time.time >= _serverSlowEndTime)
            _serverSpeedMultiplier = 1f;

        float finalSpeed = speed * _serverSpeedMultiplier;

        if (rb != null)
            rb.linearVelocity = _serverMoveDirection * finalSpeed;
    }

    private Vector2 GetVisualVelocity()
    {
        // 1) Rigidbody'den oku (simulated ise)
        if (rb != null && rb.simulated)
        {
            Vector2 v = rb.linearVelocity;
            if (v.sqrMagnitude > 0.0001f)
            {
                _prevWorldPos = transform.position;
                return v;
            }
        }

        // 2) Transform delta fallback (client'ta rb.simulated kapalı olabiliyor)
        Vector3 pos = transform.position;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector2 vel = (Vector2)((pos - _prevWorldPos) / dt);
        _prevWorldPos = pos;
        return vel;
    }

    private void UpdateAnimatorAndFlip(Vector2 velocity)
    {
        if (animator == null) return;

        bool isMoving = velocity.sqrMagnitude > 0.001f;

        if (isMoving)
            _lastLookDir = velocity.normalized;

        // Durunca son baktığı yön
        Vector2 dirForAnim = isMoving ? velocity.normalized : _lastLookDir;

        animator.SetFloat(_moveXHash, dirForAnim.x);
        animator.SetFloat(_moveYHash, dirForAnim.y);
        animator.SetFloat(_speedHash, isMoving ? 1f : 0f);

        if (spriteRenderer != null && flipSpriteForRight)
        {
            if (dirForAnim.x > 0.01f) spriteRenderer.flipX = true;
            else if (dirForAnim.x < -0.01f) spriteRenderer.flipX = false;
        }
    }

    // Dışarıdan (DealDamage vs) ÇAĞIRACAĞIN fonksiyon bu:
    // Server'da slow uygular + owner'a görsel/prediction için haber verir
    public void ApplySlowOnServer(float multiplier, float duration)
    {
        if (!IsServer) return;

        _serverSpeedMultiplier = multiplier;
        _serverSlowEndTime = Time.time + duration;

        ApplySlowClientRpc(multiplier, duration);
    }

    [ClientRpc]
    private void ApplySlowClientRpc(float multiplier, float duration)
    {
        if (!IsOwner) return;

        _speedMultiplier = multiplier;
        _slowEndTime = Time.time + duration;
    }

    // --- Spawn seçimi: rastgele sırala, boş olanı bul ---
    private Vector2 PickRandomFreeSpawn()
    {
        int n = SpawnPositions.Length;

        List<int> idxs = new List<int>(n);
        for (int i = 0; i < n; i++) idxs.Add(i);

        for (int i = 0; i < n; i++)
        {
            int j = Random.Range(i, n);
            (idxs[i], idxs[j]) = (idxs[j], idxs[i]);
        }

        foreach (int idx in idxs)
        {
            Vector2 p = SpawnPositions[idx];
            bool occupied = Physics2D.OverlapCircle(p, checkRadius, playerLayer) != null;
            if (!occupied)
                return p;
        }

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

        // NetworkTransform bunu da sync'leyecek ama garanti olsun:
        transform.position = pos;

        TeleportClientRpc(pos);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector2 pos)
    {
        // Client'ta rb.simulated kapalı olsa bile transform güncelle
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = pos;
        }
        transform.position = pos;
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
