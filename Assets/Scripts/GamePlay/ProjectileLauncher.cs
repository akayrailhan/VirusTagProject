using Unity.Netcode;
using UnityEngine;

public class ProjectileLauncher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader; // Input okuyucu
    [SerializeField] private Transform projectileSpawnPoint; // Namlu ucu
    [SerializeField] private GameObject serverProjectilePrefab; // Hasar veren mermi
    [SerializeField] private GameObject clientProjectilePrefab; // Görsel mermi
    [SerializeField] private Collider2D playerCollider; // Kendi collider'ımız (Kendimizi vurmayalım)

    [Header("Settings")]
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float fireRate = 0.5f; // Saniyede 2 mermi

    private bool _shouldFire;
    private float _previousFireTime;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.FireEvent += HandleFire;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.FireEvent -= HandleFire;
    }

    private void HandleFire()
    {
        _shouldFire = true;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (_shouldFire && Time.time >= _previousFireTime + fireRate)
        {
            // 1. Client Tarafı (Görsel - Anında Tepki)
            SpawnDummyProjectile(projectileSpawnPoint.position, projectileSpawnPoint.up);

            // 2. Server Tarafı (Mantık - Yetkili)
            PrimaryFireServerRpc(projectileSpawnPoint.position, projectileSpawnPoint.up);

            _previousFireTime = Time.time;
        }

        _shouldFire = false;
    }

    [ServerRpc]
    private void PrimaryFireServerRpc(Vector3 spawnPos, Vector3 direction)
    {
        // Sunucuda gerçek mermiyi yarat
        GameObject projectileInstance = Instantiate(serverProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        // Hız ver (Unity 6: velocity -> linearVelocity)
        if (projectileInstance.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = direction * projectileSpeed;
        }

        // Kendini vurmayı engelle (Collision Ignore)
        if (projectileInstance.TryGetComponent(out Collider2D projectileCollider) && playerCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, projectileCollider);
        }

        // Mermiyi kimin attığını işaretle
        if (projectileInstance.TryGetComponent(out DealDamage dealDamageScript))
        {
            dealDamageScript.SetOwner(OwnerClientId);
        }

        // Mermiyi ağda spawn et
        projectileInstance.GetComponent<NetworkObject>().Spawn();

        // Diğer oyunculara görsel efekti yaratmaları için haber ver
        SpawnDummyProjectileClientRpc(spawnPos, direction);
    }

    [ClientRpc]
    private void SpawnDummyProjectileClientRpc(Vector3 spawnPos, Vector3 direction)
    {
        // Ateş eden kişi zaten yarattı, tekrar yaratmasın
        if (IsOwner) return;

        SpawnDummyProjectile(spawnPos, direction);
    }

    private void SpawnDummyProjectile(Vector3 spawnPos, Vector3 direction)
    {
        GameObject projectileInstance = Instantiate(clientProjectilePrefab, spawnPos, Quaternion.identity);
        projectileInstance.transform.up = direction;

        if (projectileInstance.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = direction * projectileSpeed;
        }

        // Görsel merminin de kendimize çarpmasını engelleyelim
        if (projectileInstance.TryGetComponent(out Collider2D projectileCollider) && playerCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, projectileCollider);
        }
    }
}