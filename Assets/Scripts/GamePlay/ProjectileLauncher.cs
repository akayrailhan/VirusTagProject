using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject serverProjectilePrefab;
    [SerializeField] private GameObject clientProjectilePrefab;
    [SerializeField] private Collider2D playerCollider;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float fireRate = 0.5f;

    [Header("Scene Gate")]
    [SerializeField] private string gameSceneName = "Game";

    private bool _shouldFire;
    private float _previousFireTime;
    private bool _fireUnlocked;

    private void Awake()
    {
        if (playerCollider == null)
            playerCollider = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        if (inputReader == null) return;

        inputReader.FireEvent += HandleFire;

        if (SceneManager.GetActiveScene().name == gameSceneName)
            _fireUnlocked = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        if (inputReader != null)
            inputReader.FireEvent -= HandleFire;
    }

    private void HandleFire()
    {
        if (!_fireUnlocked)
        {
            if (SceneManager.GetActiveScene().name == gameSceneName)
                _fireUnlocked = true;
            else
            {
                _shouldFire = false;
                return;
            }
        }

        _shouldFire = true;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!_fireUnlocked)
        {
            _shouldFire = false;
            return;
        }

        if (projectileSpawnPoint == null) return;
        if (serverProjectilePrefab == null || clientProjectilePrefab == null) return;

        if (_shouldFire && Time.time >= _previousFireTime + fireRate)
        {
            SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up);
            PrimaryFireServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.up);

            _previousFireTime = Time.time;
        }

        _shouldFire = false;
    }

    [ServerRpc]
    private void PrimaryFireServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        GameObject projectileInstance = Instantiate(serverProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        if (projectileInstance.TryGetComponent(out Rigidbody2D rb))
            rb.linearVelocity = direction * projectileSpeed;

        if (projectileInstance.TryGetComponent(out Collider2D projectileCollider) && playerCollider != null)
            Physics2D.IgnoreCollision(playerCollider, projectileCollider);

        if (projectileInstance.TryGetComponent(out DealDamage dealDamageScript))
            dealDamageScript.SetOwner(OwnerClientId);

        projectileInstance.GetComponent<NetworkObject>().Spawn();
        SpawnDummyProjectileClientRpc(spawnPos, direction);
    }

    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction)
    {
        if (IsOwner) return;
        SpawnDummyProjectile(spawnPos, direction);
    }

    private void SpawnDummyProjectile(Vector3 spawnPos, Vector3 direction)
    {
        GameObject projectileInstance = Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        if (projectileInstance.TryGetComponent(out Rigidbody2D rb))
            rb.linearVelocity = direction * projectileSpeed;

        if (projectileInstance.TryGetComponent(out Collider2D projectileCollider) && playerCollider != null)
            Physics2D.IgnoreCollision(playerCollider, projectileCollider);
    }
}
