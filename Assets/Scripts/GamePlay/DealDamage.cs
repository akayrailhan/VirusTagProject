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
        // 1. Sadece sunucuda çalışır
        if (!NetworkManager.Singleton.IsServer) return;

        // 2. Mermi bir oyuncuya çarptı mı?
        if (other.TryGetComponent(out NetworkObject hitNetworkObject))
        {
            // Kendini vurmayı engelle
            if (hitNetworkObject.OwnerClientId == _ownerClientId) return;

            Debug.Log($"[Server] Projectile from {_ownerClientId} hit {hitNetworkObject.OwnerClientId}");

            // --- VİRÜS MANTIĞI BAŞLANGICI ---

            // A) Vuran Oyuncuyu Bul (Merminin Sahibi)
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(_ownerClientId, out NetworkClient shooterClient))
            {
                var shooterState = shooterClient.PlayerObject.GetComponent<PlayerState>();

                // Eğer vuran kişi ZATEN virüslüyse, virüsü devreder!
                // (Eğer virüslü değilse mermi atamaz kuralı varsa buraya eklenir)
                if (shooterState.CurrentState.Value.IsInfected)
                {
                    // Vuran kişi temizlenir
                    shooterState.SetInfectionStatus(false);

                    // B) Vurulan Oyuncuyu Bul
                    if (hitNetworkObject.TryGetComponent(out PlayerState hitPlayerState))
                    {
                        // Vurulan kişi virüslü olur
                        hitPlayerState.SetInfectionStatus(true);
                        Debug.Log("VIRUS TRANSFERRED!");
                    }
                }
            }

            // --- VİRÜS MANTIĞI BİTİŞİ ---

            // Mermiyi yok et
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}