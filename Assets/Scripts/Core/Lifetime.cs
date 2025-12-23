using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class Lifetime : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        var netObj = GetComponent<NetworkObject>();

        // Networked object → server controls lifetime via Despawn
        if (netObj != null && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                StartCoroutine(DespawnAfter(netObj));
            }
            // clients do nothing; server despawn will auto‑destroy replicas
            return;
        }

        // Non‑networked object (like ClientProjectile) → normal destroy
        Destroy(gameObject, lifetime);
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