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

    // ✅ SpawnPoints (Game sahnesinden toplanacak)
    private SpawnPoint[] _spawnPoints;
    private bool _spawnedInGame; // DontDestroy olduğumuz için aynı Game girişinde 1 kere teleport et

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
        // Scene gate
        _movementUnlocked = SceneManager.GetActiveScene().name == gameSceneName;

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

        // ✅ DontDestroy ile sahne değişince spawn işini sahne yüklenince yapacağız
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Eğer zaten Game sahnesindeysek (nadir), hemen dene
        TrySpawnIfInGame(SceneManager.GetActiveScene());
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputReader != null)
            inputReader.MoveEvent -= OnMove;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySpawnIfInGame(scene);
    }

    private void TrySpawnIfInGame(Scene scene)
    {
        _movementUnlocked = scene.name == gameSceneName;

        // Game değilse flag’i sıfırla ki sonraki Game girişinde tekrar spawnlayabilelim
        if (scene.name != gameSceneName)
        {
            _spawnedInGame = false;
            return;
        }

        // Spawn kararını sadece server versin
        if (!IsServer) return;

        if (_spawnedInGame) return;
        _spawnedInGame = true;

        // Game sahnesindeki SpawnPoint'leri topla ve boş olana ışınla
        CollectSpawnPointsFromScene(scene);
        Vector2 pos = PickRandomFreeSpawnFromScene(scene);
        TeleportOnServer(pos);
    }

    private void CollectSpawnPointsFromScene(Scene scene)
    {
        // Unity 2022+ : FindObjectsByType
        var all = Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        if (all == null || all.Length == 0)
        {
            _spawnPoints = null;
            return;
        }

        // Sadece o sahnedekileri al (DontDestroy sahnesindeki objeler karışmasın)
        List<SpawnPoint> list = new List<SpawnPoint>(all.Length);
        foreach (var sp in all)
        {
            if (sp == null) continue;
            if (sp.gameObject.scene == scene)
                list.Add(sp);
        }

        _spawnPoints = list.ToArray();
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

    // --- Spawn seçimi: Game sahnesindeki SpawnPoint'lerden boş olanı bul ---
    private Vector2 PickRandomFreeSpawnFromScene(Scene scene)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            // SpawnPoint bulunamazsa: mevcut konumda kal
            return transform.position;
        }

        // basit shuffle
        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            int j = Random.Range(i, _spawnPoints.Length);
            (_spawnPoints[i], _spawnPoints[j]) = (_spawnPoints[j], _spawnPoints[i]);
        }

        foreach (var sp in _spawnPoints)
        {
            if (sp == null) continue;

            float r = Mathf.Max(checkRadius, sp.radius);
            bool occupied = Physics2D.OverlapCircle(sp.transform.position, r, playerLayer) != null;

            if (!occupied)
                return sp.transform.position;
        }

        // hepsi doluysa rastgele
        return _spawnPoints[Random.Range(0, _spawnPoints.Length)].transform.position;
    }

    // --- Teleport: server setler, clientlara yayar ---
    private void TeleportOnServer(Vector2 pos)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = pos;
        }

        // NetworkTransform sync'leyecek ama garanti olsun:
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
}
