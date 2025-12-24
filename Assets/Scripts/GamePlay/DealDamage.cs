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
                bool shooterInfected = shooterState.CurrentState.Value.IsInfected;

                // B) Vurulan Oyuncuyu Bul
                if (hitNetworkObject.TryGetComponent(out PlayerState hitPlayerState))
                {
                    bool hitInfected = hitPlayerState.CurrentState.Value.IsInfected;

                    // 1) Eğer vuran kişi ZATEN virüslüyse, virüsü devreder (eski kural)
                    if (shooterInfected)
                    {
                        // Vuran kişi temizlenir
                        shooterState.SetInfectionStatus(false);

                        // Vurulan kişi virüslü olur
                        hitPlayerState.SetInfectionStatus(true);
                        Debug.Log("VIRUS TRANSFERRED!");
                    }
                    // 2) Eğer vuran TEMİZ (mavi) ve vurulan VİRÜSLÜ (kırmızı) ise -> yavaşlat
                    else if (!shooterInfected && hitInfected)
                    {
                        if (hitNetworkObject.TryGetComponent(out PlayerMovement hitMovement))
                        {
                            // 0.5f = %50 hız, 2f = 2 saniye
                            hitMovement.ApplySlowClientRpc(0.5f, 2f);
                            Debug.Log("INFECTED PLAYER SLOWED!");
                        }
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