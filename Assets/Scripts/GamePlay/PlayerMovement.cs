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

    private float _speedMultiplier = 1f;
    private float _slowEndTime;
    private Vector2 _moveDirection;
    private bool _movementUnlocked;

    // Anim yönü için: durunca son baktığı yön
    private Vector2 _lastLookDir = Vector2.down;

    // Remote oyuncularda rb.linearVelocity güvenilmez olabiliyor -> transform delta fallback
    private Vector3 _prevWorldPos;

    // Animator param hash
    private int _moveXHash;
    private int _moveYHash;
    private int _speedHash;

    private static readonly Vector2[] SpawnPositions =
    {
        new Vector2(10f,  4f),
        new Vector2(10f, -4f),
        new Vector2(-10f,-4f),
        new Vector2(-10f, 4f),
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
        // Input sadece owner
        if (IsOwner)
        {
            if (inputReader != null)
                inputReader.MoveEvent += OnMove;

            // Eğer zaten Game sahnesinde spawn olduysa kilidi aç
            if (SceneManager.GetActiveScene().name == gameSceneName)
                _movementUnlocked = true;
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
        if (!IsOwner) return;
        if (inputReader != null)
            inputReader.MoveEvent -= OnMove;
    }

    private void OnMove(Vector2 direction)
    {
        // İlk kez hareket denemesinde sahneyi kontrol et
        if (!_movementUnlocked)
        {
            if (SceneManager.GetActiveScene().name == gameSceneName)
            {
                _movementUnlocked = true;
            }
            else
            {
                _moveDirection = Vector2.zero;
                return;
            }
        }

        // inputReader genelde normalize eder ama garanti olsun:
        if (direction.sqrMagnitude > 1f) direction.Normalize();
        _moveDirection = direction;
    }

    private void Update()
    {
        // Animasyon/flip herkesde çalışsın (owner+remote)
        Vector2 visualVel = GetVisualVelocity();
        UpdateAnimatorAndFlip(visualVel);
    }

    private void FixedUpdate()
    {
        // Hareketi sadece owner kontrol eder
        if (!IsOwner) return;

        if (!_movementUnlocked)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // Yavaşlama süresi bittiyse hızı normale çek
        if (_speedMultiplier < 1f && Time.time >= _slowEndTime)
        {
            _speedMultiplier = 1f;
        }

        float finalSpeed = speed * _speedMultiplier;

        if (rb != null)
            rb.linearVelocity = _moveDirection * finalSpeed;
    }

    private Vector2 GetVisualVelocity()
    {
        // 1) Rigidbody'den oku
        if (rb != null)
        {
            Vector2 v = rb.linearVelocity;
            // bazen remote'da 0 döner, o yüzden fallback da var
            if (v.sqrMagnitude > 0.0001f)
            {
                _prevWorldPos = transform.position;
                return v;
            }
        }

        // 2) Transform delta fallback
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

        // Sağ anim yoksa: sağa giderken aynala
        if (spriteRenderer != null && flipSpriteForRight)
        {
            if (dirForAnim.x > 0.01f) spriteRenderer.flipX = true;       // sağ
            else if (dirForAnim.x < -0.01f) spriteRenderer.flipX = false; // sol
            // x ~ 0 iken flip'i değiştirme (yukarı/aşağı)
        }
    }

    [ClientRpc]
    public void ApplySlowClientRpc(float multiplier, float duration)
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
        else
        {
            transform.position = pos;
        }

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
