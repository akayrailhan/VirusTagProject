using Unity.Netcode;
using UnityEngine;

public class DealDamage : MonoBehaviour
{
    private ulong _ownerClientId;

    public void SetOwner(ulong ownerClientId)
    {
        _ownerClientId = ownerClientId;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Sadece sunucuda çalışır
        if (!NetworkManager.Singleton.IsServer) return;

        // Mermi bir oyuncuya çarptı mı?
        if (other.TryGetComponent(out NetworkObject hitNetworkObject))
        {
            // Kendini vurmayı engelle
            if (hitNetworkObject.OwnerClientId == _ownerClientId) return;

            Debug.Log($"[Server] Hit Player: {hitNetworkObject.OwnerClientId}");

            // Mermiyi ağ üzerinden yok et
            DespawnOrDestroy();
        }
        else if (other.CompareTag("Wall")) // Duvara çarptıysa da yok et
        {
            DespawnOrDestroy();
        }
    }

    private void DespawnOrDestroy()
    {
        var netObj = GetComponent<NetworkObject>();

        // NetworkObject ise -> Despawn (sunucu tüm client’lardan kaldırır)
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        // Normal obje ise -> klasik Destroy
        else
        {
            Destroy(gameObject);
        }
    }
}