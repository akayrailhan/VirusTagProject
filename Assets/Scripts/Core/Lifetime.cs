using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class Lifetime : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private string wallTag = "Wall";

    private NetworkObject _netObj;

    private void Awake()
    {
        _netObj = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        // Networked object → server controls lifetime via Despawn
        if (_netObj != null && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartCoroutine(DespawnAfter(_netObj));
            }
            // clients do nothing; server despawn will auto-destroy replicas
            return;
        }

        // Non-networked object (like ClientProjectile) → normal destroy
        Destroy(gameObject, lifetime);
    }

    // Trigger ile çarpışma gelirse
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag(wallTag))
            DespawnOrDestroyNow();
    }

    // Trigger değil collision gelirse
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.collider != null && collision.collider.CompareTag(wallTag))
            DespawnOrDestroyNow();
    }

    private void DespawnOrDestroyNow()
    {
        // Networked projectile: ONLY server despawns
        if (_netObj != null && _netObj.IsSpawned && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer)
                _netObj.Despawn(true);

            return; // client asla Destroy etmesin
        }

        // Non-networked (client visual): normal destroy
        Destroy(gameObject);
    }

    private IEnumerator DespawnAfter(NetworkObject netObj)
    {
        yield return new WaitForSeconds(lifetime);
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
    }
}
